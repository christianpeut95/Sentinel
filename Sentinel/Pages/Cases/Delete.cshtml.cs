using System;
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
    [Authorize(Policy = "Permission.Case.Delete")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public DeleteModel(ApplicationDbContext context, IAuditService auditService, IDiseaseAccessService diseaseAccessService)
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
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id);

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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (caseEntity != null)
            {
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
                var friendlyId = Case.FriendlyId;

                await _context.SoftDeleteAsync(Case);

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Deleted",
                    oldValue: friendlyId,
                    newValue: null,
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Case {friendlyId} deleted successfully.";
            }

            return RedirectToPage("./Index");
        }
    }
}
