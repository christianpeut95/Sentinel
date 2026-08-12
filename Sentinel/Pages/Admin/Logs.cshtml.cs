using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sentinel.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class LogsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? LevelFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public List<SelectListItem> LogLevels { get; set; } = new()
        {
            new SelectListItem { Value = "Information", Text = "Information" },
            new SelectListItem { Value = "Warning", Text = "Warning" },
            new SelectListItem { Value = "Error", Text = "Error" },
            new SelectListItem { Value = "Critical", Text = "Critical" },
            new SelectListItem { Value = "Debug", Text = "Debug" }
        };

        public void OnGet()
        {
            // Set default date range if not provided
            if (!FromDate.HasValue)
            {
                FromDate = DateTime.UtcNow.AddDays(-1);
            }
            if (!ToDate.HasValue)
            {
                ToDate = DateTime.UtcNow;
            }
        }
    }
}
