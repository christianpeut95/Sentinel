using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.Edit")]
    public class EditModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;
        private readonly IGeocodingService _geocoder;
        private readonly IPatientCustomFieldService _customFieldService;
        private readonly IAuditService _auditService;
        private readonly IPatientIdGeneratorService _patientIdGenerator;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IServiceProvider _serviceProvider;

        public EditModel(Sentinel.Data.ApplicationDbContext context, IGeocodingService geocoder, IPatientCustomFieldService customFieldService, IAuditService auditService, IPatientIdGeneratorService patientIdGenerator, IJurisdictionService jurisdictionService, IServiceProvider serviceProvider)
        {
            _context = context;
            _geocoder = geocoder;
            _customFieldService = customFieldService;
            _auditService = auditService;
            _patientIdGenerator = patientIdGenerator;
            _jurisdictionService = jurisdictionService;
            _serviceProvider = serviceProvider;
        }

        [BindProperty]
        public Patient Patient { get; set; } = default!;

        public string? OriginalAddress { get; set; }
        public string? OriginalCity { get; set; }
        public string? OriginalState { get; set; }
        public string? OriginalPostalCode { get; set; }
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();
        public Dictionary<int, string?> CustomFieldValues { get; set; } = new();
        
        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();
        public Dictionary<int, List<Jurisdiction>> JurisdictionsByType { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.CountryOfBirth)
                .Include(p => p.Ancestry)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Occupation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null) return NotFound();

            Patient = patient;

            // Store original address for comparison
            OriginalAddress = patient.AddressLine;
            OriginalCity = patient.City;
            OriginalState = patient.State;
            OriginalPostalCode = patient.PostalCode;

            ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
            ViewData["AncestryId"] = new SelectList(_context.Ancestries.OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name), "Id", "Name");
            ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
            ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
            ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
            ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
            // Occupation uses autocomplete, no ViewData needed

            // Load jurisdiction types only (not all jurisdictions - using autocomplete)
            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

            CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
            FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
            CustomFieldValues = await _customFieldService.GetPatientFieldValuesAsync(id.Value);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                ViewData["AncestryId"] = new SelectList(_context.Ancestries.OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name), "Id", "Name");
                ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
                ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
                ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
                ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
                // Occupation uses autocomplete, no ViewData needed
                
                // Reload jurisdiction data
                ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
                JurisdictionsByType = await _jurisdictionService.GetGroupedJurisdictionsAsync();
                
                // Reload custom fields
                CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                CustomFieldValues = await _customFieldService.GetPatientFieldValuesAsync(Patient.Id);
                
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Get the original patient from database to compare addresses
                var originalPatient = await _context.Patients.AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == Patient.Id);

                if (originalPatient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToPage("./Index");
                }

                // Check if any address field changed
                bool addressChanged = originalPatient.AddressLine != Patient.AddressLine ||
                                     originalPatient.City != Patient.City ||
                                     originalPatient.State != Patient.State ||
                                     originalPatient.PostalCode != Patient.PostalCode;

                // Build current address string
                var address = string.Join(", ",
                    new[] { Patient.AddressLine, Patient.City, Patient.State, Patient.PostalCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                bool geocodingSucceeded = false;
                bool geocodingAttempted = false;

                // Only geocode if address has changed and we have coordinates to update
                if (addressChanged && !string.IsNullOrWhiteSpace(address))
                {
                    geocodingAttempted = true;
                    try
                    {
                        var (lat, lon) = await _geocoder.GeocodeAsync(address);
                        Patient.Latitude = lat;
                        Patient.Longitude = lon;
                        geocodingSucceeded = true;
                    }
                    catch
                    {
                        // Don't block save if geocoding fails
                        // Keep existing coordinates if geocoding fails
                    }
                }
                else if (!addressChanged)
                {
                    // Preserve existing coordinates if address didn't change
                    Patient.Latitude = originalPatient.Latitude;
                    Patient.Longitude = originalPatient.Longitude;
                }

                // Load the tracked entity from the database and update its properties
                // This allows EF Core to properly detect which properties changed
                var trackedPatient = await _context.Patients.FindAsync(Patient.Id);
                if (trackedPatient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToPage("./Index");
                }

                // Preserve the original FriendlyId - it should NEVER change after creation
                Patient.FriendlyId = originalPatient.FriendlyId;
                
                // If for some reason the original didn't have one, generate it now
                if (string.IsNullOrWhiteSpace(Patient.FriendlyId))
                {
                    Patient.FriendlyId = await _patientIdGenerator.GenerateNextPatientIdAsync();
                }

                // Update only the properties from the posted Patient
                _context.Entry(trackedPatient).CurrentValues.SetValues(Patient);
                
                await _context.SaveChangesAsync();

                // Auto-detect jurisdictions in background (fire-and-forget) - don't make user wait
                _ = Task.Run(async () => await AutoDetectJurisdictionsInBackgroundAsync(Patient.Id, Patient.Latitude, Patient.Longitude));

                // Log all field changes for audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await LogPatientChangesAsync(originalPatient, Patient, userId, ipAddress);


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
                    // Log custom field error but don't fail the patient update
                    TempData["WarningMessage"] = $"Patient updated but some custom fields failed to save: {cfEx.Message}";
                }

                // Build success message with geocoding info
                var successMsg = $"Patient {Patient.GivenName} {Patient.FamilyName} has been updated successfully.";
                if (addressChanged)
                {
                    if (geocodingSucceeded)
                    {
                        successMsg += " Address has been re-geocoded.";
                    }
                    else if (geocodingAttempted)
                    {
                        successMsg += " Note: Address geocoding failed. Location may be inaccurate.";
                    }
                }

                TempData["SuccessMessage"] = successMsg;
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PatientExists(Patient.Id))
                {
                    TempData["ErrorMessage"] = "The patient was not found. It may have been deleted.";
                    return RedirectToPage("./Index");
                }
                else
                {
                    TempData["ErrorMessage"] = "A concurrency error occurred. The patient may have been modified by another user.";
                    throw;
                }
            }
            catch (Exception ex)
            {
                ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                ViewData["AncestryId"] = new SelectList(_context.Ancestries.OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name), "Id", "Name");
                ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
                ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
                ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
                ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
                
                // Reload custom fields
                CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                CustomFieldValues = await _customFieldService.GetPatientFieldValuesAsync(Patient.Id);
                
                TempData["ErrorMessage"] = $"An error occurred while updating the patient: {ex.Message}";
                return Page();
            }
        }

        private bool PatientExists(Guid id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        private async Task LogPatientChangesAsync(Patient oldPatient, Patient newPatient, string? userId, string? ipAddress)
        {
            var patientIdString = newPatient.Id.ToString();
            
            // Compare and log each field that changed
            if (oldPatient.GivenName != newPatient.GivenName)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Given Name", oldPatient.GivenName, newPatient.GivenName, userId, ipAddress);

            if (oldPatient.FamilyName != newPatient.FamilyName)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Family Name", oldPatient.FamilyName, newPatient.FamilyName, userId, ipAddress);

            if (oldPatient.DateOfBirth != newPatient.DateOfBirth)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Date of Birth", 
                    oldPatient.DateOfBirth?.ToString("dd MMM yyyy"), 
                    newPatient.DateOfBirth?.ToString("dd MMM yyyy"), userId, ipAddress);

            if (oldPatient.SexAtBirthId != newPatient.SexAtBirthId)
            {
                var oldSex = oldPatient.SexAtBirthId.HasValue ? (await _context.SexAtBirths.FindAsync(oldPatient.SexAtBirthId))?.Name : null;
                var newSex = newPatient.SexAtBirthId.HasValue ? (await _context.SexAtBirths.FindAsync(newPatient.SexAtBirthId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Sex at Birth", oldSex, newSex, userId, ipAddress);
            }

            if (oldPatient.GenderId != newPatient.GenderId)
            {
                var oldGender = oldPatient.GenderId.HasValue ? (await _context.Genders.FindAsync(oldPatient.GenderId))?.Name : null;
                var newGender = newPatient.GenderId.HasValue ? (await _context.Genders.FindAsync(newPatient.GenderId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Gender", oldGender, newGender, userId, ipAddress);
            }

            if (oldPatient.CountryOfBirthId != newPatient.CountryOfBirthId)
            {
                var oldCountry = oldPatient.CountryOfBirthId.HasValue ? (await _context.Countries.FindAsync(oldPatient.CountryOfBirthId))?.Name : null;
                var newCountry = newPatient.CountryOfBirthId.HasValue ? (await _context.Countries.FindAsync(newPatient.CountryOfBirthId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Country of Birth", oldCountry, newCountry, userId, ipAddress);
            }

            if (oldPatient.LanguageSpokenAtHomeId != newPatient.LanguageSpokenAtHomeId)
            {
                var oldLang = oldPatient.LanguageSpokenAtHomeId.HasValue ? (await _context.Languages.FindAsync(oldPatient.LanguageSpokenAtHomeId))?.Name : null;
                var newLang = newPatient.LanguageSpokenAtHomeId.HasValue ? (await _context.Languages.FindAsync(newPatient.LanguageSpokenAtHomeId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Language Spoken at Home", oldLang, newLang, userId, ipAddress);
            }

            if (oldPatient.AncestryId != newPatient.AncestryId)
            {
                var oldAnc = oldPatient.AncestryId.HasValue ? (await _context.Ancestries.FindAsync(oldPatient.AncestryId))?.Name : null;
                var newAnc = newPatient.AncestryId.HasValue ? (await _context.Ancestries.FindAsync(newPatient.AncestryId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Ancestry", oldAnc, newAnc, userId, ipAddress);
            }

            if (oldPatient.AtsiStatusId != newPatient.AtsiStatusId)
            {
                var oldAtsi = oldPatient.AtsiStatusId.HasValue ? (await _context.AtsiStatuses.FindAsync(oldPatient.AtsiStatusId))?.Name : null;
                var newAtsi = newPatient.AtsiStatusId.HasValue ? (await _context.AtsiStatuses.FindAsync(newPatient.AtsiStatusId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "ATSI Status", oldAtsi, newAtsi, userId, ipAddress);
            }

            if (oldPatient.OccupationId != newPatient.OccupationId)
            {
                var oldOcc = oldPatient.OccupationId.HasValue ? (await _context.Occupations.FindAsync(oldPatient.OccupationId))?.Name : null;
                var newOcc = newPatient.OccupationId.HasValue ? (await _context.Occupations.FindAsync(newPatient.OccupationId))?.Name : null;
                await _auditService.LogChangeAsync("Patient", patientIdString, "Occupation", oldOcc, newOcc, userId, ipAddress);
            }

            if (oldPatient.HomePhone != newPatient.HomePhone)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Home Phone", oldPatient.HomePhone, newPatient.HomePhone, userId, ipAddress);

            if (oldPatient.MobilePhone != newPatient.MobilePhone)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Mobile Phone", oldPatient.MobilePhone, newPatient.MobilePhone, userId, ipAddress);

            if (oldPatient.EmailAddress != newPatient.EmailAddress)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Email Address", oldPatient.EmailAddress, newPatient.EmailAddress, userId, ipAddress);

            if (oldPatient.AddressLine != newPatient.AddressLine)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Address Line", oldPatient.AddressLine, newPatient.AddressLine, userId, ipAddress);

            if (oldPatient.City != newPatient.City)
                await _auditService.LogChangeAsync("Patient", patientIdString, "City", oldPatient.City, newPatient.City, userId, ipAddress);

            if (oldPatient.State != newPatient.State)
                await _auditService.LogChangeAsync("Patient", patientIdString, "State", oldPatient.State, newPatient.State, userId, ipAddress);

            if (oldPatient.PostalCode != newPatient.PostalCode)
                await _auditService.LogChangeAsync("Patient", patientIdString, "Postal Code", oldPatient.PostalCode, newPatient.PostalCode, userId, ipAddress);
        }

        private async Task AutoDetectJurisdictionsInBackgroundAsync(Guid patientId, double? latitude, double? longitude)
        {
            // Only auto-detect if coordinates exist
            if (!latitude.HasValue || !longitude.HasValue)
                return;

            try
            {
                // Create a new scope for background work - this ensures proper DI and DbContext lifecycle
                using var scope = _serviceProvider.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedJurisdictionService = scope.ServiceProvider.GetRequiredService<IJurisdictionService>();

                // Reload the patient in this scope
                var patient = await scopedContext.Patients.FindAsync(patientId);
                if (patient == null) return;

                // Check if ANY jurisdiction fields are already populated - if so, don't override user's selection
                if (patient.Jurisdiction1Id.HasValue || patient.Jurisdiction2Id.HasValue || 
                    patient.Jurisdiction3Id.HasValue || patient.Jurisdiction4Id.HasValue || 
                    patient.Jurisdiction5Id.HasValue)
                    return;

                var detectedJurisdictions = await scopedJurisdictionService.FindJurisdictionsContainingPointAsync(
                    latitude.Value,
                    longitude.Value
                );

                // Auto-assign to appropriate jurisdiction fields based on JurisdictionType.FieldNumber
                bool anyAssigned = false;
                foreach (var jurisdiction in detectedJurisdictions)
                {
                    var fieldNumber = jurisdiction.JurisdictionType?.FieldNumber;
                    if (!fieldNumber.HasValue) continue;

                    switch (fieldNumber.Value)
                    {
                        case 1:
                            if (!patient.Jurisdiction1Id.HasValue)
                            {
                                patient.Jurisdiction1Id = jurisdiction.Id;
                                anyAssigned = true;
                            }
                            break;
                        case 2:
                            if (!patient.Jurisdiction2Id.HasValue)
                            {
                                patient.Jurisdiction2Id = jurisdiction.Id;
                                anyAssigned = true;
                            }
                            break;
                        case 3:
                            if (!patient.Jurisdiction3Id.HasValue)
                            {
                                patient.Jurisdiction3Id = jurisdiction.Id;
                                anyAssigned = true;
                            }
                            break;
                        case 4:
                            if (!patient.Jurisdiction4Id.HasValue)
                            {
                                patient.Jurisdiction4Id = jurisdiction.Id;
                                anyAssigned = true;
                            }
                            break;
                        case 5:
                            if (!patient.Jurisdiction5Id.HasValue)
                            {
                                patient.Jurisdiction5Id = jurisdiction.Id;
                                anyAssigned = true;
                            }
                            break;
                    }
                }

                if (anyAssigned)
                {
                    // Save the updated jurisdictions
                    await scopedContext.SaveChangesAsync();
                    Console.WriteLine($"? Background task: Auto-detected and saved {detectedJurisdictions.Count} jurisdictions for patient {patientId}");
                }
            }
            catch (Exception ex)
            {
                // Don't fail - just log the error
                Console.WriteLine($"? Background task error: Failed to auto-detect jurisdictions: {ex.Message}");
            }
        }
    }
}
