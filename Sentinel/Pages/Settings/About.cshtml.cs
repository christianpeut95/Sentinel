using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings
{
    public class AboutModel : PageModel
    {
        private readonly IApplicationVersionProvider _applicationVersion;

        public AboutModel(IApplicationVersionProvider applicationVersion)
        {
            _applicationVersion = applicationVersion;
        }

        public string Version => _applicationVersion.DisplayVersion;
        public string InformationalVersion => _applicationVersion.InformationalVersion;
        public string? CommitHash => _applicationVersion.CommitHash;
        public string ProductName => _applicationVersion.ProductName;
        
        public void OnGet()
        {
        }
    }
}
