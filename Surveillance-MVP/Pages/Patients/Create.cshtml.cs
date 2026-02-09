using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocoder;
        private readonly IPatientDuplicateCheckService _duplicateChecker;
        private readonly IPatientCustomFieldService _customFieldService;
        private readonly IAuditService _auditService;
        private readonly IPatientIdGeneratorService _patientIdGenerator;

        public CreateModel(ApplicationDbContext context, IGeocodingService geocoder, IPatientDuplicateCheckService duplicateChecker, IPatientCustomFieldService customFieldService, IAuditService auditService, IPatientIdGeneratorService patientIdGenerator)
        {
            _context = context;
            _geocoder = geocoder;
            _duplicateChecker = duplicateChecker;
            _customFieldService = customFieldService;
            _auditService = auditService;
            _patientIdGenerator = patientIdGenerator;
        }

        public List<PotentialDuplicate> PotentialDuplicates { get; set; } = new();
        public bool ShowDuplicateWarning { get; set; } = false;
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // Show lookup display names instead of IDs - ordered alphabetically by default, or by DisplayOrder if specified
            ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
            ViewData["EthnicityId"] = new SelectList(_context.Ethnicities.OrderBy(e => e.Name), "Id", "Name");
            ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
            ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
            ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
            ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
            // Occupation now uses autocomplete, no ViewData needed
            
            CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
            FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
            
            return Page();
        }

        [BindProperty]
        public Patient Patient { get; set; } = default!;

        [BindProperty]
        public bool ConfirmDuplicate { get; set; } = false;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                ViewData["EthnicityId"] = new SelectList(_context.Ethnicities.OrderBy(e => e.Name), "Id", "Name");
                ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
                ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
                ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
                ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
                // Occupation uses autocomplete, no ViewData needed
                
                // Reload custom fields
                CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Check for potential duplicates unless user has confirmed
                if (!ConfirmDuplicate)
                {
                    PotentialDuplicates = await _duplicateChecker.FindPotentialDuplicatesAsync(Patient);
                    
                    if (PotentialDuplicates.Any())
                    {
                        // Show duplicate warning and stay on page
                        ShowDuplicateWarning = true;
                        ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                        ViewData["EthnicityId"] = new SelectList(_context.Ethnicities.OrderBy(e => e.Name), "Id", "Name");
                        ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
                        ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
                        ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
                        ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
                        // Occupation uses autocomplete, no ViewData needed
                        
                        // Reload custom fields
                        CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
                        FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                        
                        return Page();
                    }
                }

                // Set CreatedAt and CreatedBy
                Patient.CreatedAt = DateTime.UtcNow;
                Patient.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Build address string
                var address = string.Join(", ",
                    new[] { Patient.AddressLine, Patient.City, Patient.State, Patient.PostalCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                if (!string.IsNullOrWhiteSpace(address))
                {
                    try
                    {
                        var (lat, lon) = await _geocoder.GeocodeAsync(address);
                        Patient.Latitude = lat;
                        Patient.Longitude = lon;
                    }
                    catch
                    {
                        // don't block save on geocoding failure; log in real app
                    }
                }

                _context.Patients.Add(Patient);
                await _context.SaveChangesAsync();

                // Log patient creation
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogChangeAsync("Patient", Patient.Id.ToString(), "Patient Record", null, "Created", userId, ipAddress);

                // Save custom field values
                try
                {
                    var customFieldValues = Request.Form
                        .Where(kvp => kvp.Key.StartsWith("customfield_"))
                        .ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value.ToString());
                    
                    if (customFieldValues.Any())
                    {
                        await _customFieldService.SavePatientFieldValuesAsync(Patient.Id, customFieldValues, userId, ipAddress);
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception cfEx)
                {
                    // Log custom field error but don't fail the patient creation
                    TempData["WarningMessage"] = $"Patient created but some custom fields failed to save: {cfEx.Message}";
                }

                TempData["SuccessMessage"] = $"Patient {Patient.GivenName} {Patient.FamilyName} has been created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                ViewData["EthnicityId"] = new SelectList(_context.Ethnicities.OrderBy(e => e.Name), "Id", "Name");
                ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
                ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
                ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
                ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
                
                // Reload custom fields
                CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                
                TempData["ErrorMessage"] = $"An error occurred while creating the patient: {ex.Message}";
                return Page();
            }
        }
    }
}
