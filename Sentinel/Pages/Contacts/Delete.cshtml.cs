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

namespace Sentinel.Pages.Contacts
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
        public Case Contact { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactEntity = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id && m.Type == CaseType.Contact);

            if (contactEntity == null)
            {
                return NotFound();
            }

            // Check disease access
            if (contactEntity.DiseaseId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, contactEntity.DiseaseId.Value);
                
                if (!canAccess)
                {
                    return Forbid();
                }
            }

            Contact = contactEntity;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactEntity = await _context.Cases
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == id && c.Type == CaseType.Contact);
            
            if (contactEntity != null)
            {
                // Check disease access
                if (contactEntity.DiseaseId.HasValue)
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                    var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, contactEntity.DiseaseId.Value);
                    
                    if (!canAccess)
                    {
                        return Forbid();
                    }
                }

                // Soft delete
                contactEntity.IsDeleted = true;
                contactEntity.DeletedAt = DateTime.UtcNow;
                contactEntity.DeletedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                _context.Cases.Update(contactEntity);
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: contactEntity.Id.ToString(),
                    fieldName: "Deleted",
                    oldValue: null,
                    newValue: $"Contact {contactEntity.FriendlyId} deleted",
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Contact {contactEntity.FriendlyId} deleted successfully.";
            }

            return RedirectToPage("./Index");
        }
    }
}
