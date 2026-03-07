using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.DiseaseAccess
{
    [Authorize(Policy = "Permission.Settings.ManagePermissions")]
    public class ManageRolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ManageRolesModel(
            ApplicationDbContext context,
            IDiseaseAccessService diseaseAccessService,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
            _roleManager = roleManager;
        }

        [BindProperty]
        public Guid? SelectedDiseaseId { get; set; }

        [BindProperty]
        public string? SelectedRoleId { get; set; }

        [BindProperty]
        public bool ApplyToChildren { get; set; }

        public List<DiseaseHierarchyNode> RestrictedDiseases { get; set; } = new();
        public List<IdentityRole> Roles { get; set; } = new();
        public List<RoleAccessGrant> RoleGrants { get; set; } = new();
        public Disease? SelectedDisease { get; set; }
        public bool SelectedDiseaseHasChildren { get; set; }

        public async Task OnGetAsync(Guid? diseaseId)
        {
            await LoadData();

            if (diseaseId.HasValue)
            {
                SelectedDiseaseId = diseaseId;
                SelectedDisease = await _context.Diseases
                    .Include(d => d.DiseaseCategory)
                    .Include(d => d.SubDiseases)
                    .FirstOrDefaultAsync(d => d.Id == diseaseId);

                SelectedDiseaseHasChildren = SelectedDisease?.SubDiseases.Any() ?? false;

                RoleGrants = await _context.RoleDiseaseAccess
                    .Include(rda => rda.Role)
                    .Include(rda => rda.CreatedByUser)
                    .Where(rda => rda.DiseaseId == diseaseId && rda.IsAllowed)
                    .Select(rda => new RoleAccessGrant
                    {
                        RoleId = rda.RoleId,
                        RoleName = rda.Role!.Name!,
                        GrantedAt = rda.CreatedAt,
                        GrantedByUserName = rda.CreatedByUser != null ? rda.CreatedByUser.Email : "System",
                        ApplyToChildren = rda.ApplyToChildren,
                        IsInherited = rda.InheritedFromDiseaseId != null,
                        InheritedFromDiseaseId = rda.InheritedFromDiseaseId
                    })
                    .OrderBy(g => g.RoleName)
                    .ToListAsync();

                // Load parent disease name for inherited grants
                foreach (var grant in RoleGrants.Where(g => g.IsInherited && g.InheritedFromDiseaseId.HasValue))
                {
                    var parentDisease = await _context.Diseases.FindAsync(grant.InheritedFromDiseaseId.Value);
                    grant.InheritedFromDiseaseName = parentDisease?.Name;
                }
            }
        }

        public async Task<IActionResult> OnPostGrantAsync()
        {
            if (!SelectedDiseaseId.HasValue || string.IsNullOrEmpty(SelectedRoleId))
            {
                TempData["ErrorMessage"] = "Please select both a disease and a role.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var disease = await _context.Diseases
                .Include(d => d.SubDiseases)
                .FirstOrDefaultAsync(d => d.Id == SelectedDiseaseId.Value);
                
            if (disease == null)
            {
                TempData["ErrorMessage"] = "Disease not found.";
                return RedirectToPage();
            }

            if (disease.AccessLevel != DiseaseAccessLevel.Restricted)
            {
                TempData["ErrorMessage"] = "Can only grant access to restricted diseases.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            // Check if already has inherited access
            var hasInherited = await _diseaseAccessService.HasInheritedAccessAsync(SelectedRoleId, SelectedDiseaseId.Value);
            if (hasInherited)
            {
                TempData["ErrorMessage"] = "This disease already has inherited access from a parent disease. Cannot grant directly.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var role = await _roleManager.FindByIdAsync(SelectedRoleId);
            if (role == null)
            {
                TempData["ErrorMessage"] = "Role not found.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            try
            {
                await _diseaseAccessService.GrantDiseaseAccessToRoleAsync(SelectedRoleId, SelectedDiseaseId.Value, currentUserId, ApplyToChildren);
                
                var childInfo = ApplyToChildren && disease.SubDiseases.Any() 
                    ? $" (and {await CountDescendantsAsync(disease.Id)} child disease(s))" 
                    : "";
                TempData["SuccessMessage"] = $"Access to '{disease.Name}'{childInfo} granted to role '{role.Name}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error granting access: {ex.Message}";
            }

            return RedirectToPage(new { diseaseId = SelectedDiseaseId });
        }

        public async Task<IActionResult> OnPostRevokeAsync(string roleId)
        {
            if (!SelectedDiseaseId.HasValue || string.IsNullOrEmpty(roleId))
            {
                TempData["ErrorMessage"] = "Invalid request.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var disease = await _context.Diseases.FindAsync(SelectedDiseaseId.Value);
            var role = await _roleManager.FindByIdAsync(roleId);

            try
            {
                await _diseaseAccessService.RevokeDiseaseAccessFromRoleAsync(roleId, SelectedDiseaseId.Value, true);
                TempData["SuccessMessage"] = $"Access to '{disease?.Name}' revoked from role '{role?.Name}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error revoking access: {ex.Message}";
            }

            return RedirectToPage(new { diseaseId = SelectedDiseaseId });
        }

        private async Task<int> CountDescendantsAsync(Guid diseaseId)
        {
            var children = await _diseaseAccessService.GetAllChildDiseaseIdsAsync(diseaseId);
            return children.Count;
        }

        private async Task LoadData()
        {
            var allDiseases = await _context.Diseases
                .Include(d => d.DiseaseCategory)
                .Include(d => d.SubDiseases)
                .Where(d => d.IsActive && d.AccessLevel == DiseaseAccessLevel.Restricted)
                .OrderBy(d => d.PathIds)
                .ToListAsync();

            // Build hierarchy
            RestrictedDiseases = BuildHierarchy(allDiseases, null);

            Roles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        private List<DiseaseHierarchyNode> BuildHierarchy(List<Disease> allDiseases, Guid? parentId)
        {
            var nodes = new List<DiseaseHierarchyNode>();
            
            var diseases = allDiseases.Where(d => d.ParentDiseaseId == parentId).OrderBy(d => d.Name);
            
            foreach (var disease in diseases)
            {
                var node = new DiseaseHierarchyNode
                {
                    Disease = disease,
                    Children = BuildHierarchy(allDiseases, disease.Id),
                    Level = disease.Level
                };
                nodes.Add(node);
            }
            
            return nodes;
        }
    }

    public class RoleAccessGrant
    {
        public string RoleId { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public string? GrantedByUserName { get; set; }
        public bool ApplyToChildren { get; set; }
        public bool IsInherited { get; set; }
        public Guid? InheritedFromDiseaseId { get; set; }
        public string? InheritedFromDiseaseName { get; set; }
    }

    public class DiseaseHierarchyNode
    {
        public Disease Disease { get; set; } = default!;
        public List<DiseaseHierarchyNode> Children { get; set; } = new();
        public int Level { get; set; }
    }
}
