using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Settings.HL7
{
    [Authorize]
    public class DiagnosticsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public Guid? LabResultId { get; set; }

        public void OnGet()
        {
        }
    }
}
