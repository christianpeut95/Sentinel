using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sentinel.Pages.Settings
{
    public class AboutModel : PageModel
    {
        public string Version => Constants.AppVersion.DisplayVersion;
        public string ReleaseDate => Constants.AppVersion.ReleaseDate;
        public string ProductName => Constants.AppVersion.ProductName;
        
        public void OnGet()
        {
        }
    }
}
