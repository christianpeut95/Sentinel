using System;
using System.ComponentModel.DataAnnotations;
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
    public class OrganizationModel : PageModel
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public OrganizationModel(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        [BindProperty]
        [Display(Name = "Organization Name")]
        public string OrganizationName { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "State/Province")]
        public string? State { get; set; }

        [BindProperty]
        [Display(Name = "City/Region")]
        public string? City { get; set; }

        [BindProperty]
        [Display(Name = "Postal Code")]
        public string? PostalCode { get; set; }

        [BindProperty]
        [Display(Name = "Country Code")]
        [StringLength(2, MinimumLength = 2, ErrorMessage = "Country code must be exactly 2 characters")]
        [RegularExpression(@"^[A-Z]{2}$", ErrorMessage = "Country code must be 2 uppercase letters")]
        public string? CountryCode { get; set; }

        [BindProperty]
        [Display(Name = "Timezone")]
        public string? Timezone { get; set; }

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            OrganizationName = _config["Organization:Name"] ?? string.Empty;
            Country = _config["Organization:Country"] ?? string.Empty;
            State = _config["Organization:State"];
            City = _config["Organization:City"];
            PostalCode = _config["Organization:PostalCode"];
            CountryCode = _config["Organization:CountryCode"];
            Timezone = _config["Organization:Timezone"];
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var appsettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
            
            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(appsettingsPath);
                var node = JsonNode.Parse(txt) ?? new JsonObject();
                
                // Ensure Organization section exists
                if (node["Organization"] == null)
                {
                    node["Organization"] = new JsonObject();
                }

                // Update organization settings
                node["Organization"]!["Name"] = string.IsNullOrWhiteSpace(OrganizationName) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(OrganizationName);
                    
                node["Organization"]!["Country"] = string.IsNullOrWhiteSpace(Country) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(Country);
                    
                node["Organization"]!["State"] = string.IsNullOrWhiteSpace(State) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(State);
                    
                node["Organization"]!["City"] = string.IsNullOrWhiteSpace(City) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(City);
                    
                node["Organization"]!["PostalCode"] = string.IsNullOrWhiteSpace(PostalCode) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(PostalCode);
                    
                node["Organization"]!["CountryCode"] = string.IsNullOrWhiteSpace(CountryCode) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(CountryCode?.ToUpperInvariant());
                    
                node["Organization"]!["Timezone"] = string.IsNullOrWhiteSpace(Timezone) 
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(Timezone);

                var opts = new JsonSerializerOptions { WriteIndented = true };
                var outTxt = node.ToJsonString(opts);
                await System.IO.File.WriteAllTextAsync(appsettingsPath, outTxt);

                StatusMessage = "Organization settings saved successfully. Address lookups will now prioritize your configured location.";
            }
            catch (Exception ex)
            {
                StatusMessage = "Failed to save organization settings: " + ex.Message;
            }

            return RedirectToPage();
        }
    }
}
