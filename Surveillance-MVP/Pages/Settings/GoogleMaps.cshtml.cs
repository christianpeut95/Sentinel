using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;

namespace Surveillance_MVP.Pages.Settings
{
    [Authorize(Policy = "Permission.Settings.ManageOrganization")]
    public class GoogleMapsModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public GoogleMapsModel(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        [BindProperty]
        public string ApiKey { get; set; } = string.Empty;

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            ApiKey = _config["Geocoding:ApiKey"] ?? string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var appsettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(appsettingsPath);
                var node = JsonNode.Parse(txt) ?? new JsonObject();
                if (node["Geocoding"] == null) node["Geocoding"] = new JsonObject();
                node["Geocoding"]["ApiKey"] = string.IsNullOrWhiteSpace(ApiKey) ? JsonValue.Create((string?)null) : JsonValue.Create(ApiKey);

                var opts = new JsonSerializerOptions { WriteIndented = true };
                var outTxt = node.ToJsonString(opts);
                await System.IO.File.WriteAllTextAsync(appsettingsPath, outTxt);

                StatusMessage = "Saved Google Maps API key.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to save API key: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
