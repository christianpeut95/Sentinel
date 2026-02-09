using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.DiseaseAccess
{
    [Authorize(Policy = "Permission.Settings.ManagePermissions")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DiseaseAccessSummary> DiseaseSummaries { get; set; } = new();

        public async Task OnGetAsync()
        {
            var diseases = await _context.Diseases
                .Include(d => d.DiseaseCategory)
                .Include(d => d.RoleDiseaseAccess)
                    .ThenInclude(rda => rda.Role)
                .Include(d => d.UserDiseaseAccess)
                    .ThenInclude(uda => uda.User)
                .Where(d => d.IsActive)
                .OrderBy(d => d.DiseaseCategory!.Name)
                .ThenBy(d => d.Name)
                .ToListAsync();

            DiseaseSummaries = diseases.Select(d => new DiseaseAccessSummary
            {
                Disease = d,
                RoleAccessCount = d.RoleDiseaseAccess.Count(rda => rda.IsAllowed),
                UserAccessCount = d.UserDiseaseAccess.Count(uda => uda.IsAllowed && (uda.ExpiresAt == null || uda.ExpiresAt > DateTime.UtcNow)),
                IsRestricted = d.AccessLevel == DiseaseAccessLevel.Restricted
            }).ToList();
        }
    }

    public class DiseaseAccessSummary
    {
        public Disease Disease { get; set; } = default!;
        public int RoleAccessCount { get; set; }
        public int UserAccessCount { get; set; }
        public bool IsRestricted { get; set; }
    }
}
