using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Contacts
{
    [Authorize(Policy = "Permission.Case.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly IAuditService _auditService;
        private readonly CustomFieldService _customFieldService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly IExposureRequirementService _exposureRequirementService;
        private readonly ITaskService _taskService;

        public CreateModel(
            ApplicationDbContext context, 
            ICaseIdGeneratorService caseIdGenerator, 
            IAuditService auditService,
            CustomFieldService customFieldService,
            IDiseaseAccessService diseaseAccessService,
            IExposureRequirementService exposureRequirementService,
            ITaskService taskService)
        {
            _context = context;
            _caseIdGenerator = caseIdGenerator;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _diseaseAccessService = diseaseAccessService;
            _exposureRequirementService = exposureRequirementService;
            _taskService = taskService;
        }

        public Disease? DiseaseRequirements { get; set; }
        public bool ShouldPromptForExposure { get; set; }
        public Patient? SelectedPatient { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? patientId)
        {
            ViewData["PatientId"] = new SelectList(
                await _context.Patients
                    .OrderBy(p => p.FamilyName)
                    .ThenBy(p => p.GivenName)
                    .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                    .ToListAsync(),
                "Id", "FullName", patientId);

            ViewData["ConfirmationStatusId"] = new SelectList(
                await _context.CaseStatuses
                    .Where(cs => cs.IsActive && 
                                (cs.ApplicableTo == CaseTypeApplicability.Contact || 
                                 cs.ApplicableTo == CaseTypeApplicability.Both))
                    .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                    .ThenBy(cs => cs.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Filter diseases based on access
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

            ViewData["DiseaseId"] = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id))
                    .OrderBy(d => d.Level)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .Select(d => new { 
                        d.Id, 
                        DisplayName = new string('?', d.Level) + " " + d.Name 
                    })
                    .ToListAsync(),
                "Id", "DisplayName");

            // Load genders for patient edit functionality
            ViewData["GenderId"] = new SelectList(
                await _context.Genders
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.DisplayOrder ?? int.MaxValue)
                    .ThenBy(g => g.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Initialize Case with Contact type
            Case = new Case 
            { 
                Type = CaseType.Contact,
                DateOfNotification = DateTime.Today
            };

            if (patientId.HasValue)
            {
                Case.PatientId = patientId.Value;
                SelectedPatient = await _context.Patients.FindAsync(patientId.Value);
            }

            return Page();
        }

        [BindProperty]
        public Case Case { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.FamilyName)
                        .ThenBy(p => p.GivenName)
                        .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                        .ToListAsync(),
                    "Id", "FullName");

                ViewData["ConfirmationStatusId"] = new SelectList(
                    await _context.CaseStatuses
                        .Where(cs => cs.IsActive && 
                                    (cs.ApplicableTo == CaseTypeApplicability.Contact || 
                                     cs.ApplicableTo == CaseTypeApplicability.Both))
                        .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                        .ThenBy(cs => cs.Name)
                        .ToListAsync(),
                    "Id", "Name");

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

                ViewData["DiseaseId"] = new SelectList(
                    await _context.Diseases
                        .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id))
                        .OrderBy(d => d.Level)
                        .ThenBy(d => d.DisplayOrder)
                        .ThenBy(d => d.Name)
                        .Select(d => new { 
                            d.Id, 
                            DisplayName = new string('?', d.Level) + " " + d.Name 
                        })
                        .ToListAsync(),
                    "Id", "DisplayName");

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Ensure Type is set to Contact
                Case.Type = CaseType.Contact;

                // Set default Date of Onset to today if not provided
                if (!Case.DateOfOnset.HasValue)
                {
                    Case.DateOfOnset = DateTime.Today;
                }

                Case.Id = Guid.NewGuid();
                Case.FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync();

                _context.Cases.Add(Case);
                await _context.SaveChangesAsync();

                // Auto-create tasks based on disease configuration
                if (Case.DiseaseId.HasValue)
                {
                    try
                    {
                        var tasksCreated = await _taskService.AutoCreateTasksForNewCase(Case.Id);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating tasks: {ex.Message}");
                    }
                }

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Created",
                    oldValue: null,
                    newValue: $"Contact {Case.FriendlyId} created",
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Contact {Case.FriendlyId} created successfully.";
                return RedirectToPage("./Details", new { id = Case.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the contact.";
                
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.FamilyName)
                        .ThenBy(p => p.GivenName)
                        .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                        .ToListAsync(),
                    "Id", "FullName");

                ViewData["ConfirmationStatusId"] = new SelectList(
                    await _context.CaseStatuses
                        .Where(cs => cs.IsActive)
                        .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                        .ThenBy(cs => cs.Name)
                        .ToListAsync(),
                    "Id", "Name");

                return Page();
            }
        }
    }
}
