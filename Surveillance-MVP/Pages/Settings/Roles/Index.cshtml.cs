using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Surveillance_MVP.Pages.Settings.Roles
{
    [Authorize(Policy = "Permission.User.ManagePermissions")]
    public class IndexModel : PageModel
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public List<IdentityRole> Roles { get; set; } = new();

        public void OnGet()
        {
            Roles = _roleManager.Roles.ToList();
        }

        public async Task<IActionResult> OnPostAsync(string roleName, string deleteRole)
        {
            if (!string.IsNullOrEmpty(roleName))
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
            }

            if (!string.IsNullOrEmpty(deleteRole))
            {
                var r = await _roleManager.FindByNameAsync(deleteRole);
                if (r != null)
                {
                    await _roleManager.DeleteAsync(r);
                }
            }

            return RedirectToPage();
        }
    }
}
