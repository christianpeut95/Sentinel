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

            // Filter diseases based on access
            var accessUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(accessUserId);

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

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ensure Type stays as Contact
            Case.Type = CaseType.Contact;

            _context.Attach(Case).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                
                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Updated",
                    oldValue: null,
                    newValue: $"Contact {Case.FriendlyId} updated",
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Contact {Case.FriendlyId} updated successfully.";
                return RedirectToPage("./Details", new { id = Case.Id });
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
    }
}
