using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public IndexModel(ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public List<CaseDefinition> CaseDefinitions { get; set; } = new();
        public List<Disease> Diseases { get; set; } = new();
        public List<CaseStatus> CaseStatuses { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load all case definitions with related data
            CaseDefinitions = await _context.CaseDefinitions
                .Include(cd => cd.Disease)
                .Include(cd => cd.ConfirmationStatus)
                .Include(cd => cd.Criteria)
                .OrderByDescending(cd => cd.Status == CaseDefinitionStatus.Current)
                .ThenBy(cd => cd.Disease!.Name)
                .ThenBy(cd => cd.ConfirmationStatus!.Name)
                .ToListAsync();

            // Load diseases for filter
            Diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Load case statuses for filter
            CaseStatuses = await _context.CaseStatuses
                .OrderBy(cs => cs.Name)
                .ToListAsync();
        }

        // Helper methods for statistics (placeholder - will implement with background service)
        public int GetEvaluatedCaseCount(int definitionId)
        {
            // TODO: Implement when background evaluation service is ready
            return 0;
        }

        public int GetMatchingCaseCount(int definitionId)
        {
            // TODO: Implement when background evaluation service is ready
            return 0;
        }

        public string GetLastEvaluatedTime(int definitionId)
        {
            // TODO: Implement when background evaluation service is ready
            return "Not yet evaluated";
        }

        public async Task<IActionResult> OnPostArchiveAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) ||
                !await _permissionService.HasPermissionAsync(
                    userId,
                    PermissionModule.Settings,
                    PermissionAction.Edit))
            {
                return Forbid();
            }

            var definition = await _context.CaseDefinitions.FindAsync(id);

            if (definition == null)
            {
                return NotFound();
            }

            if (!CaseDefinitionWorkflowPolicy.CanArchive(definition.Status))
            {
                return BadRequest(new { error = "This case definition has already been archived." });
            }

            // Archive the definition
            definition.Status = CaseDefinitionStatus.Archived;
            definition.ModifiedAt = DateTime.UtcNow;
            definition.ModifiedBy = User.Identity?.Name;

            await _context.SaveChangesAsync();

            return new OkResult();
        }
    }
}
