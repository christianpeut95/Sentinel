using System;
using System.Collections.Generic;
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
    [Authorize(Policy = "Permission.Case.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public EditModel(
            ApplicationDbContext context, 
            IAuditService auditService,
            IDiseaseAccessService diseaseAccessService)
        {
            _context = context;
            _auditService = auditService;
            _diseaseAccessService = diseaseAccessService;
        }

        [BindProperty]
        public Case Case { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == CaseType.Contact);
            
            if (caseEntity == null)
            {
                return NotFound();
            }

            // Check disease access
            if (caseEntity.DiseaseId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, caseEntity.DiseaseId.Value);
                
                if (!canAccess)
                {
                    return Forbid();
                }
            }

            Case = caseEntity;

            await LoadEditOptionsAsync(caseEntity.PatientId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var caseToUpdate = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == Case.Id && c.Type == CaseType.Contact);

            if (caseToUpdate == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (caseToUpdate.DiseaseId.HasValue &&
                !await _diseaseAccessService.CanAccessDiseaseAsync(userId, caseToUpdate.DiseaseId.Value))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                Case.Patient = caseToUpdate.Patient;
                Case.Disease = caseToUpdate.Disease;
                Case.FriendlyId = caseToUpdate.FriendlyId;
                await LoadEditOptionsAsync(caseToUpdate.PatientId);
                return Page();
            }

            if (Case.DiseaseId.HasValue)
            {
                if (!await _diseaseAccessService.CanAccessDiseaseAsync(userId, Case.DiseaseId.Value))
                {
                    return Forbid();
                }

                var diseaseIsActive = await _context.Diseases
                    .AnyAsync(d => d.Id == Case.DiseaseId.Value && d.IsActive);
                if (!diseaseIsActive)
                {
                    ModelState.AddModelError("Case.DiseaseId", "Select an active disease that you can access.");
                }
            }

            if (Case.ConfirmationStatusId.HasValue)
            {
                var statusIsValidForContact = await _context.CaseStatuses.AnyAsync(cs =>
                    cs.Id == Case.ConfirmationStatusId.Value &&
                    cs.IsActive &&
                    (cs.ApplicableTo == CaseTypeApplicability.Contact ||
                     cs.ApplicableTo == CaseTypeApplicability.Both));
                if (!statusIsValidForContact)
                {
                    ModelState.AddModelError("Case.ConfirmationStatusId", "Select an active status that applies to contacts.");
                }
            }

            if (!ModelState.IsValid)
            {
                Case.Patient = caseToUpdate.Patient;
                Case.Disease = caseToUpdate.Disease;
                Case.FriendlyId = caseToUpdate.FriendlyId;
                await LoadEditOptionsAsync(caseToUpdate.PatientId);
                return Page();
            }

            // Preserve the patient, type, classifications, audit data and all other
            // case fields.  This page deliberately allows only these contact fields.
            caseToUpdate.DiseaseId = Case.DiseaseId;
            caseToUpdate.ConfirmationStatusId = Case.ConfirmationStatusId;
            caseToUpdate.DateOfOnset = Case.DateOfOnset;
            caseToUpdate.DateOfNotification = Case.DateOfNotification;

            try
            {
                await _context.SaveChangesAsync();
                
                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: caseToUpdate.Id.ToString(),
                    fieldName: "Updated",
                    oldValue: null,
                    newValue: $"Contact {caseToUpdate.FriendlyId} updated",
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Contact {caseToUpdate.FriendlyId} updated successfully.";
                return RedirectToPage("./Details", new { id = caseToUpdate.Id });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CaseExists(Case.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool CaseExists(Guid id)
        {
            return _context.Cases.Any(e => e.Id == id);
        }

        private async Task LoadEditOptionsAsync(Guid patientId)
        {
            // A contact's patient is locked, so do not expose every patient in a disabled
            // select element merely to render the current value.
            ViewData["PatientId"] = new SelectList(
                await _context.Patients
                    .Where(p => p.Id == patientId)
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

            var accessUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(accessUserId);

            ViewData["DiseaseId"] = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id))
                    .OrderBy(d => d.Level)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .Select(d => new
                    {
                        d.Id,
                        DisplayName = new string('?', d.Level) + " " + d.Name
                    })
                    .ToListAsync(),
                "Id", "DisplayName");
        }
    }
}
