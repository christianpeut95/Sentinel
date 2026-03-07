using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.Create")]
    public class CreateNewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly IAuditService _auditService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly ITaskService _taskService;

        public CreateNewModel(
            ApplicationDbContext context,
            ICaseIdGeneratorService caseIdGenerator,
            IAuditService auditService,
            IDiseaseAccessService diseaseAccessService,
            ITaskService taskService)
        {
            _context = context;
            _caseIdGenerator = caseIdGenerator;
            _auditService = auditService;
            _diseaseAccessService = diseaseAccessService;
            _taskService = taskService;
        }

        public List<DiseaseDto> Diseases { get; set; } = new();
        public List<CaseStatusDto> CaseStatuses { get; set; } = new();
        public List<GenderDto> Genders { get; set; } = new();
        public List<SexAtBirthDto> SexAtBirths { get; set; } = new();
        public List<CountryDto> Countries { get; set; } = new();
        public List<LanguageDto> Languages { get; set; } = new();
        public List<EthnicityDto> Ancestries { get; set; } = new();
        public List<AtsiStatusDto> AtsiStatuses { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get accessible diseases
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

            Diseases = await _context.Diseases
                .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id))
                .OrderBy(d => d.Name)
                .Select(d => new DiseaseDto { Id = d.Id, Name = d.Name })
                .ToListAsync();

            CaseStatuses = await _context.CaseStatuses
                .Where(cs => cs.IsActive)
                .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                .ThenBy(cs => cs.Name)
                .Select(cs => new CaseStatusDto { Id = cs.Id, Name = cs.Name })
                .ToListAsync();

            Genders = await _context.Genders
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder ?? int.MaxValue)
                .ThenBy(g => g.Name)
                .Select(g => new GenderDto { Id = g.Id, Name = g.Name })
                .ToListAsync();

            SexAtBirths = await _context.SexAtBirths
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                .ThenBy(s => s.Name)
                .Select(s => new SexAtBirthDto { Id = s.Id, Name = s.Name })
                .ToListAsync();

            Countries = await _context.Countries
                .OrderBy(c => c.Name)
                .Select(c => new CountryDto { Id = c.Id, Name = c.Name })
                .ToListAsync();

            Languages = await _context.Languages
                .OrderBy(l => l.Name)
                .Select(l => new LanguageDto { Id = l.Id, Name = l.Name })
                .ToListAsync();

            Ancestries = await _context.Ancestries
                .OrderBy(e => e.Name)
                .Select(e => new EthnicityDto { Id = e.Id, Name = e.Name })
                .ToListAsync();

            AtsiStatuses = await _context.AtsiStatuses
                .Where(a => a.IsActive)
                .OrderBy(a => a.DisplayOrder ?? int.MaxValue)
                .ThenBy(a => a.Name)
                .Select(a => new AtsiStatusDto { Id = a.Id, Name = a.Name })
                .ToListAsync();

            return Page();
        }

        // AJAX handler to save initial case and return Case ID
        public async Task<IActionResult> OnPostSaveInitialCaseAsync([FromBody] SaveInitialCaseRequest request)
        {
            try
            {
                if (request.PatientId == Guid.Empty)
                {
                    return new JsonResult(new { success = false, message = "Patient ID is required." });
                }

                if (!request.DiseaseId.HasValue)
                {
                    return new JsonResult(new { success = false, message = "Disease is required." });
                }

                // Create the case
                var newCase = new Case
                {
                    Id = Guid.NewGuid(),
                    FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync(),
                    Type = CaseType.Case,
                    PatientId = request.PatientId,
                    DiseaseId = request.DiseaseId.Value,
                    ConfirmationStatusId = request.ConfirmationStatusId,
                    DateOfOnset = request.DateOfOnset,
                    DateOfNotification = request.DateOfNotification ?? DateTime.Today
                };

                _context.Cases.Add(newCase);
                await _context.SaveChangesAsync();

                // ? Task auto-creation now handled by CaseCreationInterceptor
                // No need to manually call AutoCreateTasksForNewCase anymore
                // The interceptor will automatically create tasks after SaveChangesAsync completes

                // Log audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogChangeAsync(
                    "Case",
                    newCase.Id.ToString(),
                    "Created",
                    null,
                    $"Case {newCase.FriendlyId} created (initial save)",
                    userId,
                    ipAddress
                );

                return new JsonResult(new
                {
                    success = true,
                    caseId = newCase.Id,
                    friendlyId = newCase.FriendlyId
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Handler for Step 5 - just returns the case ID for redirect
        public IActionResult OnPostCompleteCase(Guid caseId)
        {
            if (caseId == Guid.Empty)
            {
                return new JsonResult(new { success = false, message = "Invalid case ID" });
            }

            return new JsonResult(new
            {
                success = true,
                caseId = caseId,
                redirectUrl = $"/Cases/Details?id={caseId}"
            });
        }

        // DISABLED: This method is no longer used in the multi-step workflow
        // The case is created in OnPostSaveInitialCaseAsync (Step 2 -> Step 3 transition)
        // NOTE: Manual task creation call was also removed - now handled by CaseCreationInterceptor
        // Keeping for reference only
        /*
        public async Task<IActionResult> OnPostAsync(
            Guid patientId,
            Guid? diseaseId,
            int? confirmationStatusId,
            DateTime? dateOfOnset,
            DateTime? dateOfNotification,
            string? clinicalNotes)
        {
            if (patientId == Guid.Empty)
            {
                TempData["ErrorMessage"] = "Please select a patient.";
                return RedirectToPage();
            }

            if (!diseaseId.HasValue)
            {
                TempData["ErrorMessage"] = "Please select a disease.";
                return RedirectToPage();
            }

            try
            {
                // Create the case
                var newCase = new Case
                {
                    Id = Guid.NewGuid(),
                    FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync(),
                    Type = CaseType.Case,
                    PatientId = patientId,
                    DiseaseId = diseaseId.Value,
                    ConfirmationStatusId = confirmationStatusId,
                    DateOfOnset = dateOfOnset,
                    DateOfNotification = dateOfNotification ?? DateTime.Today
                };

                _context.Cases.Add(newCase);
                await _context.SaveChangesAsync();

                // Auto-create tasks based on disease configuration
                try
                {
                    await _taskService.AutoCreateTasksForNewCase(newCase.Id);
                }
                catch (Exception taskEx)
                {
                    // Log but don't fail case creation
                    Console.WriteLine($"Failed to auto-create tasks: {taskEx.Message}");
                }

                // Log audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogChangeAsync(
                    "Case",
                    newCase.Id.ToString(),
                    "Created",
                    null,
                    $"Case {newCase.FriendlyId} created",
                    userId,
                    ipAddress
                );

                TempData["SuccessMessage"] = $"Case {newCase.FriendlyId} created successfully!";
                return RedirectToPage("./Details", new { id = newCase.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error creating case: {ex.Message}";
                return RedirectToPage();
            }
        }
        */

        public class DiseaseDto
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class CaseStatusDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class GenderDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class SexAtBirthDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class CountryDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class LanguageDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class EthnicityDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class AtsiStatusDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class SaveInitialCaseRequest
        {
            public Guid PatientId { get; set; }
            public Guid? DiseaseId { get; set; }
            public int? ConfirmationStatusId { get; set; }
            public DateTime? DateOfOnset { get; set; }
            public DateTime? DateOfNotification { get; set; }
            public string? ClinicalNotes { get; set; }
        }
    }
}


