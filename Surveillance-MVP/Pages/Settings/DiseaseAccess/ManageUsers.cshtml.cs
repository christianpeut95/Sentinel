using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using Surveillance_MVP.Services;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Settings.DiseaseAccess
{
    [Authorize(Policy = "Permission.Settings.ManagePermissions")]
    public class ManageUsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManageUsersModel(
            ApplicationDbContext context,
            IDiseaseAccessService diseaseAccessService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
            _userManager = userManager;
        }

        [BindProperty]
        public Guid? SelectedDiseaseId { get; set; }

        [BindProperty]
        public string? SelectedUserId { get; set; }

        [BindProperty]
        public DateTime? ExpiresAt { get; set; }

        [BindProperty]
        public string? Reason { get; set; }

        [BindProperty]
        public bool ApplyToChildren { get; set; }

        public List<DiseaseHierarchyNode> AllDiseases { get; set; } = new();
        public List<ApplicationUser> Users { get; set; } = new();
        public List<UserAccessGrant> UserGrants { get; set; } = new();
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

                UserGrants = await _context.UserDiseaseAccess
                    .Include(uda => uda.User)
                    .Include(uda => uda.GrantedByUser)
                    .Where(uda => uda.DiseaseId == diseaseId && uda.IsAllowed)
                    .Select(uda => new UserAccessGrant
                    {
                        UserId = uda.UserId,
                        UserEmail = uda.User!.Email!,
                        GrantedAt = uda.CreatedAt,
                        ExpiresAt = uda.ExpiresAt,
                        GrantedByUserName = uda.GrantedByUser != null ? uda.GrantedByUser.Email : "System",
                        Reason = uda.Reason,
                        IsExpired = uda.ExpiresAt.HasValue && uda.ExpiresAt < DateTime.UtcNow,
                        ApplyToChildren = uda.ApplyToChildren,
                        IsInherited = uda.InheritedFromDiseaseId != null
                    })
                    .OrderBy(g => g.UserEmail)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostGrantAsync()
        {
            if (!SelectedDiseaseId.HasValue || string.IsNullOrEmpty(SelectedUserId))
            {
                TempData["ErrorMessage"] = "Please select both a disease and a user.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var disease = await _context.Diseases.FindAsync(SelectedDiseaseId.Value);
            if (disease == null)
            {
                TempData["ErrorMessage"] = "Disease not found.";
                return RedirectToPage();
            }

            var user = await _userManager.FindByIdAsync(SelectedUserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            if (ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow)
            {
                TempData["ErrorMessage"] = "Expiration date must be in the future.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            try
            {
                await _diseaseAccessService.GrantDiseaseAccessToUserAsync(
                    SelectedUserId, 
                    SelectedDiseaseId.Value, 
                    currentUserId, 
                    ExpiresAt, 
                    Reason);

                var expirationInfo = ExpiresAt.HasValue 
                    ? $" (expires {ExpiresAt.Value.ToLocalTime():MMM dd, yyyy})" 
                    : " (permanent)";
                TempData["SuccessMessage"] = $"Access to '{disease.Name}' granted to '{user.Email}'{expirationInfo}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error granting access: {ex.Message}";
            }

            return RedirectToPage(new { diseaseId = SelectedDiseaseId });
        }

        public async Task<IActionResult> OnPostRevokeAsync(string userId)
        {
            if (!SelectedDiseaseId.HasValue || string.IsNullOrEmpty(userId))
            {
                TempData["ErrorMessage"] = "Invalid request.";
                return RedirectToPage(new { diseaseId = SelectedDiseaseId });
            }

            var disease = await _context.Diseases.FindAsync(SelectedDiseaseId.Value);
            var user = await _userManager.FindByIdAsync(userId);

            try
            {
                await _diseaseAccessService.RevokeDiseaseAccessFromUserAsync(userId, SelectedDiseaseId.Value);
                TempData["SuccessMessage"] = $"Access to '{disease?.Name}' revoked from user '{user?.Email}'.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error revoking access: {ex.Message}";
            }

            return RedirectToPage(new { diseaseId = SelectedDiseaseId });
        }

        private async Task LoadData()
        {
            var allDiseases = await _context.Diseases
                .Include(d => d.DiseaseCategory)
                .Include(d => d.SubDiseases)
                .Where(d => d.IsActive)
                .OrderBy(d => d.PathIds)
                .ToListAsync();

            // Build hierarchy
            AllDiseases = BuildHierarchy(allDiseases, null);

            Users = await _userManager.Users
                .OrderBy(u => u.Email)
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

    public class UserAccessGrant
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? GrantedByUserName { get; set; }
        public string? Reason { get; set; }
        public bool IsExpired { get; set; }
        public bool ApplyToChildren { get; set; }
        public bool IsInherited { get; set; }
    }
}
