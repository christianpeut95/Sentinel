using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
