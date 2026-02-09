using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Cases
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
                    .Where(cs => cs.IsActive)
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
                        DisplayName = new string('—', d.Level) + " " + d.Name 
                    })
                    .ToListAsync(),
                "Id", "DisplayName");

            // Initialize Case with defaults
            Case = new Case 
            { 
                Type = CaseType.Case,
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
                        .Where(cs => cs.IsActive)
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
                            DisplayName = new string('—', d.Level) + " " + d.Name 
                        })
                        .ToListAsync(),
                    "Id", "DisplayName");

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Set default Date of Onset to today if not provided
                if (!Case.DateOfOnset.HasValue)
                {
                    Case.DateOfOnset = DateTime.Today;
                }

                // Validate exposure requirements if disease requires it
                if (Case.DiseaseId.HasValue)
                {
                    var requirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                    
                    if (requirements != null && 
                        (requirements.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
                         requirements.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired))
                    {
                        // Check if case will have exposure data (we can't check yet since case isn't saved)
                        // Instead, we'll show a soft warning via TempData
                        TempData["ExposureWarning"] = $"This disease requires exposure data. Please add exposure information after creating the case.";
                    }
                }

                Case.Id = Guid.NewGuid();
                Case.FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync();

                _context.Cases.Add(Case);
                await _context.SaveChangesAsync();

                // Auto-create tasks for this case
                if (Case.DiseaseId.HasValue)
                {
                    try
                    {
                        await _taskService.CreateTasksForCase(Case.Id, TaskTrigger.OnCaseCreation);
                        System.Diagnostics.Debug.WriteLine($"Tasks auto-created for case {Case.FriendlyId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating tasks: {ex.Message}");
                        // Don't fail case creation if task creation fails
                    }
                }

                // Auto-create exposure from residential address if configured
                System.Diagnostics.Debug.WriteLine($"=== Auto-Create Exposure Check ===");
                System.Diagnostics.Debug.WriteLine($"Case.DiseaseId: {Case.DiseaseId}");
                System.Diagnostics.Debug.WriteLine($"Case.PatientId: {Case.PatientId}");
                System.Diagnostics.Debug.WriteLine($"PatientId != Empty: {Case.PatientId != Guid.Empty}");
                
                if (Case.DiseaseId.HasValue && Case.PatientId != Guid.Empty)
                {
                    System.Diagnostics.Debug.WriteLine("Checking disease requirements...");
                    var requirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                    System.Diagnostics.Debug.WriteLine($"Requirements found: {requirements != null}");
                    System.Diagnostics.Debug.WriteLine($"DefaultToResidentialAddress: {requirements?.DefaultToResidentialAddress}");
                    
                    if (requirements != null && requirements.DefaultToResidentialAddress)
                    {
                        System.Diagnostics.Debug.WriteLine("Loading patient...");
                        var patient = await _context.Patients.FindAsync(Case.PatientId);
                        System.Diagnostics.Debug.WriteLine($"Patient found: {patient != null}");
                        System.Diagnostics.Debug.WriteLine($"Patient AddressLine: {patient?.AddressLine}");
                        
                        if (patient != null && !string.IsNullOrWhiteSpace(patient.AddressLine))
                        {
                            System.Diagnostics.Debug.WriteLine("Creating exposure event...");
                            // Create exposure event from residential address
                            var exposureEvent = new ExposureEvent
                            {
                                Id = Guid.NewGuid(),
                                CaseId = Case.Id,
                                ExposureType = ExposureType.Location,
                                ExposureStartDate = Case.DateOfOnset ?? DateTime.Today,
                                ExposureStatus = ExposureStatus.PotentialExposure,
                                
                                // Structured address fields
                                AddressLine = patient.AddressLine,
                                City = patient.City,
                                State = patient.State,
                                PostalCode = patient.PostalCode,
                                Country = "Australia", // Default to Australia
                                
                                // Legacy free-text field for backward compatibility
                                FreeTextLocation = $"{patient.AddressLine}, {patient.City}, {patient.State} {patient.PostalCode}".Trim(),
                                
                                // Geocoding from patient if available (convert double to decimal)
                                Latitude = patient.Latitude.HasValue ? (decimal?)patient.Latitude.Value : null,
                                Longitude = patient.Longitude.HasValue ? (decimal?)patient.Longitude.Value : null,
                                
                                // Flags
                                IsDefaultedFromResidentialAddress = true,
                                IsReportingExposure = true, // First exposure is the reporting exposure
                                
                                Description = "Automatically populated from patient's residential address",
                                CreatedDate = DateTime.UtcNow,
                                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            };

                            _context.ExposureEvents.Add(exposureEvent);
                            await _context.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($"Exposure created with ID: {exposureEvent.Id}");

                            TempData["ExposureInfo"] = "An exposure record has been automatically created from the patient's residential address.";
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Patient or address not found - exposure NOT created");
                            if (patient == null)
                                TempData["ExposureWarning"] = "Could not auto-create exposure: Patient not found.";
                            else
                                TempData["ExposureWarning"] = "Could not auto-create exposure: Patient has no residential address.";
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Disease not configured for auto-create exposure");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DiseaseId or PatientId missing - cannot auto-create exposure");
                }

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Created",
                    oldValue: null,
                    newValue: $"Case {Case.FriendlyId} created",
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Case {Case.FriendlyId} created successfully.";
                return RedirectToPage("./Details", new { id = Case.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the case.";
                
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
