using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Data;
using Sentinel.Models;
using Microsoft.EntityFrameworkCore;

namespace Sentinel.Pages.Settings.Users
{
    [Authorize(Policy = "Permission.User.View")]
    public class DetailsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public DetailsModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public ApplicationUser? User { get; set; }
        public List<Group> Groups { get; set; } = new();
        public List<Group> AllGroups { get; set; } = new();
        public List<string> UserRoles { get; set; } = new();
        public List<string> LanguagesSpoken { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            User = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (User == null) return NotFound();

            UserRoles = (await _userManager.GetRolesAsync(User)).ToList();
            
            // Parse languages from JSON
            if (!string.IsNullOrEmpty(User.LanguagesSpokenJson))
            {
                try
                {
                    LanguagesSpoken = JsonSerializer.Deserialize<List<string>>(User.LanguagesSpokenJson) ?? new();
                }
                catch { }
            }
            
            // Get groups - UserGroup is a join table, so we need to get the actual Group entities
            var userGroupIds = await _db.UserGroups
                .Where(ug => ug.UserId == id)
                .Select(ug => ug.GroupId)
                .ToListAsync();
            
            Groups = await _db.Groups
                .Where(g => userGroupIds.Contains(g.Id))
                .ToListAsync();
            
            AllGroups = await _db.Groups.ToListAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id, int? addGroupId, int? removeGroup)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest();
            User = await _userManager.FindByIdAsync(id);
            if (User == null) return NotFound();

            if (addGroupId.HasValue)
            {
                var exists = await _db.UserGroups.FindAsync(User.Id, addGroupId.Value);
                if (exists == null)
                {
                    _db.UserGroups.Add(new UserGroup { UserId = User.Id, GroupId = addGroupId.Value });
                    await _db.SaveChangesAsync();
                    StatusMessage = "User added to group successfully.";
                }
            }

            if (removeGroup.HasValue)
            {
                var ug = await _db.UserGroups.FindAsync(User.Id, removeGroup.Value);
                if (ug != null)
                {
                    _db.UserGroups.Remove(ug);
                    await _db.SaveChangesAsync();
                    StatusMessage = "User removed from group successfully.";
                }
            }

            return RedirectToPage(new { id });
        }
    }
}
