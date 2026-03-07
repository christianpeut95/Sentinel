using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.DiseaseAccess
{
    [Authorize(Policy = "Permission.Settings.ManagePermissions")]
    public class ViewGrantsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ViewGrantsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DiseaseGrantSummary> DiseaseGrants { get; set; } = new();

        public async Task OnGetAsync()
        {
            var diseases = await _context.Diseases
                .Include(d => d.DiseaseCategory)
                .Include(d => d.RoleDiseaseAccess)
                    .ThenInclude(rda => rda.Role)
                .Include(d => d.RoleDiseaseAccess)
                    .ThenInclude(rda => rda.CreatedByUser)
                .Include(d => d.UserDiseaseAccess)
                    .ThenInclude(uda => uda.User)
                .Include(d => d.UserDiseaseAccess)
                    .ThenInclude(uda => uda.GrantedByUser)
                .Where(d => d.IsActive && (
                    d.RoleDiseaseAccess.Any(rda => rda.IsAllowed) ||
                    d.UserDiseaseAccess.Any(uda => uda.IsAllowed)
                ))
                .OrderBy(d => d.Name)
                .ToListAsync();

            DiseaseGrants = diseases.Select(d => new DiseaseGrantSummary
            {
                Disease = d,
                RoleGrants = d.RoleDiseaseAccess
                    .Where(rda => rda.IsAllowed)
                    .Select(rda => new RoleGrantInfo
                    {
                        RoleName = rda.Role?.Name ?? "Unknown",
                        GrantedAt = rda.CreatedAt,
                        GrantedBy = rda.CreatedByUser?.Email ?? "System"
                    })
                    .OrderBy(r => r.RoleName)
                    .ToList(),
                UserGrants = d.UserDiseaseAccess
                    .Where(uda => uda.IsAllowed)
                    .Select(uda => new UserGrantInfo
                    {
                        UserEmail = uda.User?.Email ?? "Unknown",
                        GrantedAt = uda.CreatedAt,
                        ExpiresAt = uda.ExpiresAt,
                        GrantedBy = uda.GrantedByUser?.Email ?? "System",
                        Reason = uda.Reason,
                        IsExpired = uda.ExpiresAt.HasValue && uda.ExpiresAt < DateTime.UtcNow
                    })
                    .OrderBy(u => u.UserEmail)
                    .ToList()
            }).ToList();
        }
    }

    public class DiseaseGrantSummary
    {
        public Disease Disease { get; set; } = default!;
        public List<RoleGrantInfo> RoleGrants { get; set; } = new();
        public List<UserGrantInfo> UserGrants { get; set; } = new();
    }

    public class RoleGrantInfo
    {
        public string RoleName { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public string GrantedBy { get; set; } = string.Empty;
    }

    public class UserGrantInfo
    {
        public string UserEmail { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string GrantedBy { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public bool IsExpired { get; set; }
    }
}
