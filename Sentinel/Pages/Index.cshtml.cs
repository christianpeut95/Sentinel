using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages
{
    public class IndexModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to the new dashboard
            return Redirect("/dashboard");
        }
    }
}
