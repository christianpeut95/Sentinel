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
using Sentinel.Services;

namespace Sentinel.Pages.Settings
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
        [StringLength(200)]
        public string OrganizationName { get; set; } = string.Empty;

        [BindProperty]
        [Display(Name = "State/Province")]
        [StringLength(100)]
        public string? State { get; set; }

        [BindProperty]
        [Display(Name = "City/Region")]
        [StringLength(100)]
        public string? City { get; set; }

        [BindProperty]
        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string? PostalCode { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Country/region is required")]
        [Display(Name = "Country/Region")]
        public string CountryCode { get; set; } = "AU";

        [BindProperty]
        [Required(ErrorMessage = "Application timezone is required")]
        [Display(Name = "Application Timezone")]
        public string TimeZoneId { get; set; } = OrganizationRegionalSettings.DefaultTimeZoneId;

        [BindProperty]
        [Required(ErrorMessage = "Date, number and language format is required")]
        [Display(Name = "Date, Number and Language Format")]
        public string Locale { get; set; } = OrganizationRegionalSettings.DefaultLocale;

        public IReadOnlyList<OrganizationRegionalSettings.TimeZoneOption> TimeZoneOptions { get; private set; } = Array.Empty<OrganizationRegionalSettings.TimeZoneOption>();
        public IReadOnlyList<OrganizationRegionalSettings.LocaleOption> LocaleOptions { get; private set; } = Array.Empty<OrganizationRegionalSettings.LocaleOption>();
        public IReadOnlyList<OrganizationRegionalSettings.RegionOption> RegionOptions { get; private set; } = Array.Empty<OrganizationRegionalSettings.RegionOption>();

        [TempData]
        public string StatusMessage { get; set; } = string.Empty;

        public void OnGet()
        {
            LoadOptions();
            OrganizationName = _config["Organization:Name"] ?? string.Empty;
            State = _config["Organization:State"];
            City = _config["Organization:City"];
            PostalCode = _config["Organization:PostalCode"];
            CountryCode = ResolveCountryCode(_config["Organization:CountryCode"], _config["Organization:Country"]);
            TimeZoneId = NormalizeTimeZone(_config["Organization:TimeZoneId"]);
            Locale = NormalizeLocale(_config["Organization:Locale"]);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            LoadOptions();
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (!OrganizationRegionalSettings.TryResolveTimeZone(TimeZoneId, out var timeZone))
            {
                ModelState.AddModelError(nameof(TimeZoneId), "Select a valid time zone.");
            }

            if (!OrganizationRegionalSettings.TryGetCulture(Locale, out var culture))
            {
                ModelState.AddModelError(nameof(Locale), "Select a valid date, number and language format.");
            }

            if (!OrganizationRegionalSettings.TryGetRegion(CountryCode, out var region) || region is null)
            {
                ModelState.AddModelError(nameof(CountryCode), "Select a valid country or region.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            TimeZoneId = OrganizationRegionalSettings.GetCanonicalTimeZoneId(timeZone);
            Locale = culture.Name;
            CountryCode = region.TwoLetterISORegionName;
            var appsettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");

            try
            {
                var txt = await System.IO.File.ReadAllTextAsync(appsettingsPath);
                var node = JsonNode.Parse(txt) ?? new JsonObject();

                // Ensure Organization section exists
                if (node["Organization"] is not JsonObject organization)
                {
                    organization = new JsonObject();
                    node["Organization"] = organization;
                }

                // Update organization settings
                organization["Name"] = string.IsNullOrWhiteSpace(OrganizationName)
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(OrganizationName);

                organization["Country"] = JsonValue.Create(region.EnglishName);

                organization["State"] = string.IsNullOrWhiteSpace(State)
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(State);

                organization["City"] = string.IsNullOrWhiteSpace(City)
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(City);

                organization["PostalCode"] = string.IsNullOrWhiteSpace(PostalCode)
                    ? JsonValue.Create((string?)null) 
                    : JsonValue.Create(PostalCode);

                organization["CountryCode"] = JsonValue.Create(CountryCode);
                organization["TimeZoneId"] = JsonValue.Create(TimeZoneId);
                organization["Locale"] = JsonValue.Create(Locale);
                // Retire the historic duplicate setting as part of every valid save.
                organization.Remove("Timezone");

                var opts = new JsonSerializerOptions { WriteIndented = true };
                var outTxt = node.ToJsonString(opts);
                await System.IO.File.WriteAllTextAsync(appsettingsPath, outTxt);

                StatusMessage = "Organization settings saved successfully. The date, time and number format updates are now active.";
            }
            catch (Exception ex)
            {
                StatusMessage = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
            }

            return RedirectToPage();
        }

        private void LoadOptions()
        {
            TimeZoneOptions = OrganizationRegionalSettings.GetTimeZoneOptions();
            LocaleOptions = OrganizationRegionalSettings.GetLocaleOptions();
            RegionOptions = OrganizationRegionalSettings.GetRegionOptions();
        }

        private static string NormalizeTimeZone(string? configuredTimeZoneId) =>
            OrganizationRegionalSettings.TryResolveTimeZone(configuredTimeZoneId, out var timeZone)
                ? OrganizationRegionalSettings.GetCanonicalTimeZoneId(timeZone)
                : OrganizationRegionalSettings.DefaultTimeZoneId;

        private static string NormalizeLocale(string? configuredLocale) =>
            OrganizationRegionalSettings.TryGetCulture(configuredLocale, out var culture)
                ? culture.Name
                : OrganizationRegionalSettings.DefaultLocale;

        private static string ResolveCountryCode(string? configuredCode, string? configuredCountry)
        {
            if (OrganizationRegionalSettings.TryGetRegion(configuredCode, out var region) && region is not null)
            {
                return region.TwoLetterISORegionName;
            }

            var option = OrganizationRegionalSettings.GetRegionOptions().FirstOrDefault(candidate =>
                string.Equals(candidate.DisplayName, configuredCountry, StringComparison.OrdinalIgnoreCase));
            return option?.Code ?? "AU";
        }
    }
}
