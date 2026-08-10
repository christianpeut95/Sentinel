using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Setup
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        public void OnGet()
        {
            // Blazor component handles all logic
        }
    }
}
