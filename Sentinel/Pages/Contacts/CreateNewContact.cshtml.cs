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
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Contacts
{
    [Authorize(Policy = "Permission.Case.Create")]
    public class CreateNewContactModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly IAuditService _auditService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly ITaskService _taskService;

        public CreateNewContactModel(
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
        public List<ContactClassificationDto> ContactClassifications { get; set; } = new();

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
                .Where(cs => cs.IsActive && 
                            (cs.ApplicableTo == CaseTypeApplicability.Contact || 
                             cs.ApplicableTo == CaseTypeApplicability.Both))
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

            ContactClassifications = await _context.ContactClassifications
                .Where(cc => cc.IsActive)
                .OrderBy(cc => cc.DisplayOrder)
                .ThenBy(cc => cc.Name)
                .Select(cc => new ContactClassificationDto { Id = cc.Id, Name = cc.Name })
                .ToListAsync();

            return Page();
        }

        // AJAX handler to save initial contact case and return Case ID
        public async Task<IActionResult> OnPostSaveInitialContactAsync([FromBody] SaveInitialContactRequest request)
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

                // Create the contact case
                var newContact = new Case
                {
                    Id = Guid.NewGuid(),
                    FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync(),
                    Type = CaseType.Contact,
                    PatientId = request.PatientId,
                    DiseaseId = request.DiseaseId.Value,
                    ConfirmationStatusId = request.ConfirmationStatusId,
                    DateOfOnset = request.DateOfOnset,
                    DateOfNotification = request.DateOfNotification ?? DateTime.Today
                };

                _context.Cases.Add(newContact);
                await _context.SaveChangesAsync();

                // Task auto-creation now handled by CaseCreationInterceptor
                
                // Log audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogChangeAsync(
                    "Case",
                    newContact.Id.ToString(),
                    "Created",
                    null,
                    $"Contact {newContact.FriendlyId} created (initial save)",
                    userId,
                    ipAddress
                );

                return new JsonResult(new
                {
                    success = true,
                    contactId = newContact.Id,
                    friendlyId = newContact.FriendlyId
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Handler for final step - creates exposure record linking to source case
        public async Task<IActionResult> OnPostLinkToSourceCaseAsync([FromBody] LinkToSourceCaseRequest request)
        {
            try
            {
                if (request.ContactCaseId == Guid.Empty || request.SourceCaseId == Guid.Empty)
                {
                    return new JsonResult(new { success = false, message = "Both contact and source case IDs are required." });
                }

                // Create the exposure event that links the contact to the source case
                var exposure = new ExposureEvent
                {
                    Id = Guid.NewGuid(),
                    SourceCaseId = request.SourceCaseId,
                    ExposedCaseId = request.ContactCaseId,
                    ExposureType = ExposureType.Contact,
                    ContactClassificationId = request.ContactClassificationId,
                    ExposureStartDate = request.ExposureStartDate ?? DateTime.Today,
                    ExposureEndDate = request.ExposureEndDate,
                    Description = request.Description,
                    ExposureStatus = ExposureStatus.ConfirmedExposure
                };

                _context.ExposureEvents.Add(exposure);
                await _context.SaveChangesAsync();

                // Log audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogChangeAsync(
                    "ExposureEvent",
                    exposure.Id.ToString(),
                    "Created",
                    null,
                    $"Exposure created linking contact to source case",
                    userId,
                    ipAddress
                );

                return new JsonResult(new
                {
                    success = true,
                    contactId = request.ContactCaseId,
                    redirectUrl = $"/Contacts/Details?id={request.ContactCaseId}"
                });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        // Handler for Step 5 - just returns the contact ID for redirect
        public IActionResult OnPostCompleteContact(Guid contactId)
        {
            if (contactId == Guid.Empty)
            {
                return new JsonResult(new { success = false, message = "Invalid contact ID" });
            }

            return new JsonResult(new
            {
                success = true,
                contactId = contactId,
                redirectUrl = $"/Contacts/Details?id={contactId}"
            });
        }

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

        public class ContactClassificationDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class SaveInitialContactRequest
        {
            public Guid PatientId { get; set; }
            public Guid? DiseaseId { get; set; }
            public int? ConfirmationStatusId { get; set; }
            public DateTime? DateOfOnset { get; set; }
            public DateTime? DateOfNotification { get; set; }
            public string? ClinicalNotes { get; set; }
        }

        public class LinkToSourceCaseRequest
        {
            public Guid ContactCaseId { get; set; }
            public Guid SourceCaseId { get; set; }
            public int? ContactClassificationId { get; set; }
            public DateTime? ExposureStartDate { get; set; }
            public DateTime? ExposureEndDate { get; set; }
            public string? Description { get; set; }
        }
    }
}
