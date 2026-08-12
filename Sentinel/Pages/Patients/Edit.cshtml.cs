using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    [Authorize(Policy = "Permission.Patient.View")]
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
        private readonly IPatientAddressService _patientAddressService;

        public EditModel(Sentinel.Data.ApplicationDbContext context, IGeocodingService geocoder, IPatientCustomFieldService customFieldService, IAuditService auditService, IPatientIdGeneratorService patientIdGenerator, IJurisdictionService jurisdictionService, IServiceProvider serviceProvider, IPatientAddressService patientAddressService)
        {
            _context = context;
            _geocoder = geocoder;
            _customFieldService = customFieldService;
            _auditService = auditService;
            _patientIdGenerator = patientIdGenerator;
            _jurisdictionService = jurisdictionService;
            _serviceProvider = serviceProvider;
            _patientAddressService = patientAddressService;
        }

        [BindProperty]
        public PatientEditInputModel Patient { get; set; } = new();

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
            if (id is not Guid patientId || !await LoadPatientForEditAsync(patientId, populateInput: true))
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ValidateInput();
            await ValidateJurisdictionAssignmentsAsync();

            if (!ModelState.IsValid)
            {
                if (!await LoadPatientForEditAsync(Patient.Id, populateInput: false))
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Get the original patient from database to compare addresses
                var originalPatient = await _context.Patients
                    .AsNoTracking()
                    .Include(p => p.State)
                    .FirstOrDefaultAsync(p => p.Id == Patient.Id);

                if (originalPatient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToPage("./Index");
                }

                // Check if any address field changed
                bool addressChanged = originalPatient.AddressLine != Patient.AddressLine ||
                                     originalPatient.City != Patient.City ||
                                     originalPatient.StateId != Patient.StateId ||
                                     originalPatient.PostalCode != Patient.PostalCode;

                var stateCode = Patient.StateId.HasValue
                    ? await _context.States
                        .Where(s => s.Id == Patient.StateId.Value)
                        .Select(s => s.Code)
                        .FirstOrDefaultAsync()
                    : null;

                // Build the address from allow-listed fields only.
                var address = string.Join(", ",
                    new string?[] { Patient.AddressLine, Patient.City, stateCode, Patient.PostalCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

                bool geocodingSucceeded = false;
                bool geocodingAttempted = false;
                var latitude = originalPatient.Latitude;
                var longitude = originalPatient.Longitude;

                // Coordinates are server-managed. Never trust hidden fields submitted by the browser.
                if (addressChanged && !string.IsNullOrWhiteSpace(address))
                {
                    geocodingAttempted = true;
                    try
                    {
                        var (lat, lon) = await _geocoder.GeocodeAsync(address);
                        latitude = lat;
                        longitude = lon;
                        geocodingSucceeded = true;
                    }
                    catch
                    {
                        // Don't block the update if geocoding fails. Preserve existing coordinates.
                    }
                }

                var trackedPatient = await _context.Patients.FindAsync(Patient.Id);
                if (trackedPatient == null)
                {
                    TempData["ErrorMessage"] = "Patient not found.";
                    return RedirectToPage("./Index");
                }

                // Map only the fields explicitly supported by the edit screen.
                // Do not copy the input object wholesale: it intentionally has no audit,
                // deletion, identity, or navigation properties.
                ApplyInputToPatient(trackedPatient, Patient);
                trackedPatient.Latitude = latitude;
                trackedPatient.Longitude = longitude;

                // A FriendlyId remains immutable after creation. Retain the existing value,
                // generating one only for legacy records that do not have one.
                if (string.IsNullOrWhiteSpace(trackedPatient.FriendlyId))
                {
                    trackedPatient.FriendlyId = await _patientIdGenerator.GenerateNextPatientIdAsync();
                }

                await _context.SaveChangesAsync();

                // Process address change for related cases
                PatientAddressUpdateResult? addressUpdateResult = null;
                if (addressChanged)
                {
                    var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    addressUpdateResult = await _patientAddressService.ProcessAddressChangeAsync(
                        trackedPatient,
                        originalPatient.AddressLine,
                        originalPatient.City,
                        originalPatient.StateId,
                        originalPatient.PostalCode,
                        currentUserId);

                    // If there are cases requiring review, show them to the user
                    if (addressUpdateResult.CasesRequiringReview.Any())
                    {
                        // Store in TempData for display on next page
                        TempData["AddressChangeReview"] = System.Text.Json.JsonSerializer.Serialize(
                            addressUpdateResult.CasesRequiringReview);
                    }
                }

                // Auto-detect jurisdictions in background (fire-and-forget) - don't make user wait
                _ = Task.Run(async () => await AutoDetectJurisdictionsInBackgroundAsync(
                    trackedPatient.Id,
                    trackedPatient.Latitude,
                    trackedPatient.Longitude));

                // Log all field changes for audit
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await LogPatientChangesAsync(originalPatient, trackedPatient, userId, ipAddress);


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
                if (!await LoadPatientForEditAsync(Patient.Id, populateInput: false))
                {
                    return NotFound();
                }

                TempData["ErrorMessage"] = $"An error occurred while updating the patient: {ex.Message}";
                return Page();
            }
        }

        private async Task<bool> LoadPatientForEditAsync(Guid patientId, bool populateInput)
        {
            if (patientId == Guid.Empty)
            {
                return false;
            }

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.CountryOfBirth)
                .Include(p => p.State)
                .Include(p => p.Ancestry)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Occupation)
                .Include(p => p.Jurisdiction1).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction2).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction3).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction4).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction5).ThenInclude(j => j!.JurisdictionType)
                .FirstOrDefaultAsync(p => p.Id == patientId);

            if (patient == null)
            {
                return false;
            }

            if (populateInput)
            {
                Patient = PatientEditInputModel.FromPatient(patient);
            }
            else
            {
                // Retain posted allow-listed values after a validation failure while
                // reloading display-only values which are not model-bound.
                Patient.PopulateDisplayValues(patient);
            }

            OriginalAddress = patient.AddressLine;
            OriginalCity = patient.City;
            OriginalState = patient.State?.Code;
            OriginalPostalCode = patient.PostalCode;

            await LoadEditPageDataAsync(patientId);
            return true;
        }

        private async Task LoadEditPageDataAsync(Guid patientId)
        {
            ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
            ViewData["StateId"] = new SelectList(_context.States.Where(s => s.IsActive).OrderBy(s => s.Code), "Id", "Code");
            ViewData["AncestryId"] = new SelectList(_context.Ancestries.OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name), "Id", "Name");
            ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
            ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
            ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
            ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");

            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            JurisdictionsByType = await _jurisdictionService.GetGroupedJurisdictionsAsync();

            CustomFields = await _customFieldService.GetCreateEditFieldsAsync();
            FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
            CustomFieldValues = await _customFieldService.GetPatientFieldValuesAsync(patientId);
        }

        private void ValidateInput()
        {
            if (!Patient.IsDeceased)
            {
                Patient.DateOfDeath = null;
            }

            if (Patient.DateOfBirth.HasValue && Patient.DateOfDeath.HasValue &&
                Patient.DateOfDeath.Value.Date < Patient.DateOfBirth.Value.Date)
            {
                ModelState.AddModelError("Patient.DateOfDeath", "Date of death cannot be before date of birth.");
            }
        }

        private async Task ValidateJurisdictionAssignmentsAsync()
        {
            var assignments = new (int FieldNumber, int? JurisdictionId)[]
            {
                (1, Patient.Jurisdiction1Id),
                (2, Patient.Jurisdiction2Id),
                (3, Patient.Jurisdiction3Id),
                (4, Patient.Jurisdiction4Id),
                (5, Patient.Jurisdiction5Id)
            };

            foreach (var assignment in assignments.Where(a => a.JurisdictionId.HasValue))
            {
                var isValid = await _context.Jurisdictions.AnyAsync(j =>
                    j.Id == assignment.JurisdictionId!.Value &&
                    j.JurisdictionType != null &&
                    j.JurisdictionType.FieldNumber == assignment.FieldNumber);

                if (!isValid)
                {
                    ModelState.AddModelError(
                        $"Patient.Jurisdiction{assignment.FieldNumber}Id",
                        "Select a valid jurisdiction for this field.");
                }
            }
        }

        private static void ApplyInputToPatient(Patient target, PatientEditInputModel input)
        {
            target.GivenName = input.GivenName;
            target.FamilyName = input.FamilyName;
            target.DateOfBirth = input.DateOfBirth;
            target.SexAtBirthId = input.SexAtBirthId;
            target.GenderId = input.GenderId;
            target.HomePhone = input.HomePhone;
            target.MobilePhone = input.MobilePhone;
            target.EmailAddress = input.EmailAddress;
            target.AddressLine = input.AddressLine;
            target.City = input.City;
            target.StateId = input.StateId;
            target.PostalCode = input.PostalCode;
            target.CountryOfBirthId = input.CountryOfBirthId;
            target.LanguageSpokenAtHomeId = input.LanguageSpokenAtHomeId;
            target.AncestryId = input.AncestryId;
            target.AtsiStatusId = input.AtsiStatusId;
            target.OccupationId = input.OccupationId;
            target.IsDeceased = input.IsDeceased;
            target.DateOfDeath = input.DateOfDeath;
            target.Jurisdiction1Id = input.Jurisdiction1Id;
            target.Jurisdiction2Id = input.Jurisdiction2Id;
            target.Jurisdiction3Id = input.Jurisdiction3Id;
            target.Jurisdiction4Id = input.Jurisdiction4Id;
            target.Jurisdiction5Id = input.Jurisdiction5Id;
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

            if (oldPatient.StateId != newPatient.StateId)
                await _auditService.LogChangeAsync("Patient", patientIdString, "State", oldPatient.State?.Code, newPatient.State?.Code, userId, ipAddress);

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

                // ALWAYS update jurisdictions when address changes (don't skip if already populated)
                // The address change means the location changed, so jurisdictions should be re-evaluated

                var detectedJurisdictions = await scopedJurisdictionService.FindJurisdictionsContainingPointAsync(
                    latitude.Value,
                    longitude.Value
                );

                // Auto-assign to appropriate jurisdiction fields based on JurisdictionType.FieldNumber
                // Group by field number to avoid overwriting - take first match for each type
                var jurisdictionsByField = detectedJurisdictions
                    .Where(j => j.JurisdictionType?.FieldNumber != null)
                    .GroupBy(j => j.JurisdictionType!.FieldNumber)
                    .ToDictionary(g => g.Key, g => g.First());

                bool anyAssigned = false;

                foreach (var kvp in jurisdictionsByField)
                {
                    var fieldNumber = kvp.Key;
                    var jurisdiction = kvp.Value;

                    switch (fieldNumber)
                    {
                        case 1:
                            patient.Jurisdiction1Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"✓ Assigned Jurisdiction1: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 2:
                            patient.Jurisdiction2Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"✓ Assigned Jurisdiction2: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 3:
                            patient.Jurisdiction3Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"✓ Assigned Jurisdiction3: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 4:
                            patient.Jurisdiction4Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"✓ Assigned Jurisdiction4: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 5:
                            patient.Jurisdiction5Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"✓ Assigned Jurisdiction5: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
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

    /// <summary>
    /// The explicit allow-list for patient editing. This deliberately excludes
    /// database-managed identity, audit, deletion, navigation, and collection fields.
    /// </summary>
    public sealed class PatientEditInputModel
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string GivenName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Last Name")]
        public string FamilyName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Sex at Birth")]
        public int? SexAtBirthId { get; set; }

        [Display(Name = "Gender")]
        public int? GenderId { get; set; }

        [Display(Name = "Home Phone")]
        [DataType(DataType.PhoneNumber)]
        public string? HomePhone { get; set; }

        [Display(Name = "Mobile Phone")]
        [DataType(DataType.PhoneNumber)]
        public string? MobilePhone { get; set; }

        [Display(Name = "Email Address")]
        [DataType(DataType.EmailAddress)]
        public string? EmailAddress { get; set; }

        [Display(Name = "Address")]
        public string? AddressLine { get; set; }

        [Display(Name = "Suburb")]
        public string? City { get; set; }

        public int? StateId { get; set; }

        [Display(Name = "Postcode")]
        public string? PostalCode { get; set; }

        [Display(Name = "Country of Birth")]
        public int? CountryOfBirthId { get; set; }

        [Display(Name = "Language Spoken at Home")]
        public int? LanguageSpokenAtHomeId { get; set; }

        [Display(Name = "Ancestry")]
        public int? AncestryId { get; set; }

        [Display(Name = "Aboriginal and Torres Strait Islander Status")]
        public int? AtsiStatusId { get; set; }

        [Display(Name = "Occupation")]
        public int? OccupationId { get; set; }

        [Display(Name = "Deceased")]
        public bool IsDeceased { get; set; }

        [Display(Name = "Date of Death")]
        [DataType(DataType.Date)]
        public DateTime? DateOfDeath { get; set; }

        public int? Jurisdiction1Id { get; set; }
        public int? Jurisdiction2Id { get; set; }
        public int? Jurisdiction3Id { get; set; }
        public int? Jurisdiction4Id { get; set; }
        public int? Jurisdiction5Id { get; set; }

        // These values support display only. They are never trusted from a POST.
        [BindNever] public double? Latitude { get; set; }
        [BindNever] public double? Longitude { get; set; }
        [BindNever] public Occupation? Occupation { get; set; }
        [BindNever] public Jurisdiction? Jurisdiction1 { get; set; }
        [BindNever] public Jurisdiction? Jurisdiction2 { get; set; }
        [BindNever] public Jurisdiction? Jurisdiction3 { get; set; }
        [BindNever] public Jurisdiction? Jurisdiction4 { get; set; }
        [BindNever] public Jurisdiction? Jurisdiction5 { get; set; }

        public static PatientEditInputModel FromPatient(Patient patient)
        {
            var input = new PatientEditInputModel
            {
                Id = patient.Id,
                GivenName = patient.GivenName,
                FamilyName = patient.FamilyName,
                DateOfBirth = patient.DateOfBirth,
                SexAtBirthId = patient.SexAtBirthId,
                GenderId = patient.GenderId,
                HomePhone = patient.HomePhone,
                MobilePhone = patient.MobilePhone,
                EmailAddress = patient.EmailAddress,
                AddressLine = patient.AddressLine,
                City = patient.City,
                StateId = patient.StateId,
                PostalCode = patient.PostalCode,
                CountryOfBirthId = patient.CountryOfBirthId,
                LanguageSpokenAtHomeId = patient.LanguageSpokenAtHomeId,
                AncestryId = patient.AncestryId,
                AtsiStatusId = patient.AtsiStatusId,
                OccupationId = patient.OccupationId,
                IsDeceased = patient.IsDeceased,
                DateOfDeath = patient.DateOfDeath,
                Jurisdiction1Id = patient.Jurisdiction1Id,
                Jurisdiction2Id = patient.Jurisdiction2Id,
                Jurisdiction3Id = patient.Jurisdiction3Id,
                Jurisdiction4Id = patient.Jurisdiction4Id,
                Jurisdiction5Id = patient.Jurisdiction5Id
            };

            input.PopulateDisplayValues(patient);
            return input;
        }

        public void PopulateDisplayValues(Patient patient)
        {
            Latitude = patient.Latitude;
            Longitude = patient.Longitude;
            Occupation = patient.Occupation;
            Jurisdiction1 = patient.Jurisdiction1;
            Jurisdiction2 = patient.Jurisdiction2;
            Jurisdiction3 = patient.Jurisdiction3;
            Jurisdiction4 = patient.Jurisdiction4;
            Jurisdiction5 = patient.Jurisdiction5;
        }
    }
}
