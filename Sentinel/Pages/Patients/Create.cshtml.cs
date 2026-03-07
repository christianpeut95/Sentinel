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
    [Authorize(Policy = "Permission.Patient.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeocodingService _geocoder;
        private readonly IPatientDuplicateCheckService _duplicateChecker;
        private readonly IPatientCustomFieldService _customFieldService;
        private readonly IAuditService _auditService;
        private readonly IPatientIdGeneratorService _patientIdGenerator;
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IServiceProvider _serviceProvider;

        public CreateModel(ApplicationDbContext context, IGeocodingService geocoder, IPatientDuplicateCheckService duplicateChecker, IPatientCustomFieldService customFieldService, IAuditService auditService, IPatientIdGeneratorService patientIdGenerator, ICaseIdGeneratorService caseIdGenerator, IJurisdictionService jurisdictionService, IServiceProvider serviceProvider)
        {
            _context = context;
            _geocoder = geocoder;
            _duplicateChecker = duplicateChecker;
            _customFieldService = customFieldService;
            _auditService = auditService;
            _patientIdGenerator = patientIdGenerator;
            _caseIdGenerator = caseIdGenerator;
            _jurisdictionService = jurisdictionService;
            _serviceProvider = serviceProvider;
        }

        public List<PotentialDuplicate> PotentialDuplicates { get; set; } = new();
        public bool ShowDuplicateWarning { get; set; } = false;
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();
        
        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Mode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnContext { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? DiseaseId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Show lookup display names instead of IDs - ordered alphabetically by default, or by DisplayOrder if specified
            ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
            ViewData["AncestryId"] = new SelectList(_context.Ancestries.OrderBy(e => e.DisplayOrder).ThenBy(e => e.Name), "Id", "Name");
            ViewData["LanguageSpokenAtHomeId"] = new SelectList(_context.Languages.OrderBy(l => l.Name), "Id", "Name");
            ViewData["AtsiStatusId"] = new SelectList(_context.AtsiStatuses.Where(a => a.IsActive).OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name), "Id", "Name");
            ViewData["SexAtBirthId"] = new SelectList(_context.SexAtBirths.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name), "Id", "Name");
            ViewData["GenderId"] = new SelectList(_context.Genders.Where(g => g.IsActive).OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name), "Id", "Name");
            // Occupation and Jurisdictions now use autocomplete, no ViewData needed
            
            // Load only jurisdiction type metadata (not all 567 jurisdictions!)
            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            
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
                ViewData["EthnicityId"] = new SelectList(_context.Ancestries.OrderBy(e => e.Name), "Id", "Name");
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
                        ViewData["EthnicityId"] = new SelectList(_context.Ancestries.OrderBy(e => e.Name), "Id", "Name");
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

                // Retry logic for ID generation with race condition handling
                int maxRetries = 5;
                bool saved = false;
                
                for (int attempt = 1; attempt <= maxRetries && !saved; attempt++)
                {
                    try
                    {
                        // Generate unique FriendlyId if not already set
                        if (string.IsNullOrWhiteSpace(Patient.FriendlyId))
                        {
                            Patient.FriendlyId = await _patientIdGenerator.GenerateNextPatientIdAsync();
                        }

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

                        // Save patient first - don't make user wait for jurisdiction detection
                        _context.Patients.Add(Patient);
                        await _context.SaveChangesAsync();
                        saved = true; // Success!

                        // Auto-detect jurisdictions in background (fire-and-forget)
                        // This won't block the user's response
                        _ = Task.Run(async () => await AutoDetectJurisdictionsInBackgroundAsync(Patient.Id, Patient.Latitude, Patient.Longitude));
                    }
                    catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx 
                                                        && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
                    {
                        // Duplicate key error - clear the FriendlyId and retry
                        _context.Entry(Patient).State = EntityState.Detached;
                        Patient.FriendlyId = null;
                        
                        if (attempt == maxRetries)
                        {
                            throw new InvalidOperationException($"Could not generate unique Patient ID after {maxRetries} attempts. Please try again.");
                        }
                        
                        await Task.Delay(50 * attempt); // Exponential backoff
                    }
                }

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

                // Handle popup mode for creating contacts from exposure workflow
                if (Mode == "popup" && ReturnContext == "exposure" && DiseaseId.HasValue)
                {
                    // Auto-create a Contact case for this patient
                    var newCase = new Case
                    {
                        Id = Guid.NewGuid(),
                        PatientId = Patient.Id,
                        Type = CaseType.Contact,
                        DiseaseId = DiseaseId.Value,
                        DateOfNotification = DateTime.Today,
                        FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync()
                    };

                    // Set default status to first Contact-applicable status
                    var defaultStatus = await _context.CaseStatuses
                        .Where(s => s.IsActive && 
                                   (s.ApplicableTo == CaseTypeApplicability.Contact || 
                                    s.ApplicableTo == CaseTypeApplicability.Both))
                        .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                        .ThenBy(s => s.Name)
                        .FirstOrDefaultAsync();

                    if (defaultStatus != null)
                    {
                        newCase.ConfirmationStatusId = defaultStatus.Id;
                    }

                    _context.Cases.Add(newCase);
                    await _context.SaveChangesAsync();

                    // Return script to close popup and notify parent
                    var patientName = $"{Patient.GivenName} {Patient.FamilyName}";
                    return Content($@"
                        <html>
                        <head><title>Contact Created</title></head>
                        <body>
                            <script>
                                if (window.opener && window.opener.contactCreated) {{
                                    window.opener.contactCreated('{newCase.Id}', '{newCase.FriendlyId}', '{patientName.Replace("'", "\\'")}');
                                }}
                                window.close();
                            </script>
                            <p>Contact created successfully. This window should close automatically.</p>
                            <p>If it doesn't, <a href='javascript:window.close()'>click here to close</a>.</p>
                        </body>
                        </html>
                    ", "text/html");
                }

                TempData["SuccessMessage"] = $"Patient {Patient.GivenName} {Patient.FamilyName} has been created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                ViewData["CountryOfBirthId"] = new SelectList(_context.Countries.OrderBy(c => c.Name), "Id", "Name");
                ViewData["EthnicityId"] = new SelectList(_context.Ancestries.OrderBy(e => e.Name), "Id", "Name");
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

        // Handler for linking existing patient as contact
        public async Task<IActionResult> OnPostLinkExistingPatientAsync(Guid patientId, Guid diseaseId)
        {
            if (diseaseId == Guid.Empty)
            {
                return BadRequest("DiseaseId is required");
            }

            // Check if patient exists
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.Id == patientId && !p.IsDeleted);

            if (patient == null)
            {
                return NotFound("Patient not found");
            }

            // Check if contact case already exists for this patient and disease
            var existingContact = await _context.Cases
                .FirstOrDefaultAsync(c => 
                    c.PatientId == patientId && 
                    c.DiseaseId == diseaseId && 
                    c.Type == CaseType.Contact &&
                    !c.IsDeleted);

            if (existingContact != null)
            {
                // Contact case already exists, use it
                var patientName = $"{patient.GivenName} {patient.FamilyName}";
                return Content($@"
                    <html>
                    <head><title>Contact Linked</title></head>
                    <body>
                        <script>
                            if (window.opener && window.opener.contactCreated) {{
                                window.opener.contactCreated('{existingContact.Id}', '{existingContact.FriendlyId}', '{patientName.Replace("'", "\\'")}');
                            }}
                            window.close();
                        </script>
                        <p>Existing contact case linked successfully. This window should close automatically.</p>
                    </body>
                    </html>
                ", "text/html");
            }

            // Create new contact case for existing patient
            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                Type = CaseType.Contact,
                DiseaseId = diseaseId,
                DateOfNotification = DateTime.Today,
                FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync()
            };

            // Set default status
            var defaultStatus = await _context.CaseStatuses
                .Where(s => s.IsActive && 
                           (s.ApplicableTo == CaseTypeApplicability.Contact || 
                            s.ApplicableTo == CaseTypeApplicability.Both))
                .OrderBy(s => s.DisplayOrder ?? int.MaxValue)
                .ThenBy(s => s.Name)
                .FirstOrDefaultAsync();

            if (defaultStatus != null)
            {
                newCase.ConfirmationStatusId = defaultStatus.Id;
            }

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync();

            // Return script to close popup and notify parent
            var name = $"{patient.GivenName} {patient.FamilyName}";
            return Content($@"
                <html>
                <head><title>Contact Created</title></head>
                <body>
                    <script>
                        if (window.opener && window.opener.contactCreated) {{
                            window.opener.contactCreated('{newCase.Id}', '{newCase.FriendlyId}', '{name.Replace("'", "\\'")}');
                        }}
                        window.close();
                    </script>
                    <p>Contact created successfully for existing patient. This window should close automatically.</p>
                </body>
                </html>
            ", "text/html");
        }

        // AJAX handler for jurisdiction detection
        public async Task<JsonResult> OnPostDetectJurisdictionsAsync([FromBody] DetectionRequest request)
        {
            try
            {
                var jurisdictions = await _jurisdictionService.FindJurisdictionsContainingPointAsync(
                    request.Latitude, 
                    request.Longitude
                );

                var result = jurisdictions.Select(j => new
                {
                    jurisdictionId = j.Id,
                    jurisdictionName = j.Name,
                    jurisdictionTypeId = j.JurisdictionTypeId,
                    fieldNumber = j.JurisdictionType?.FieldNumber ?? 0
                }).ToList();

                return new JsonResult(new { success = true, jurisdictions = result });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, error = ex.Message });
            }
        }

        // AJAX handler for jurisdiction search/autocomplete
        public async Task<JsonResult> OnGetSearchJurisdictionsAsync(string term, int? jurisdictionTypeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                    return new JsonResult(new List<object>());

                var query = _context.Jurisdictions
                    .Include(j => j.JurisdictionType)
                    .Where(j => j.IsActive && 
                           (j.Name.Contains(term) || (j.Code != null && j.Code.Contains(term))));

                // Filter by jurisdiction type if specified
                if (jurisdictionTypeId.HasValue)
                {
                    query = query.Where(j => j.JurisdictionTypeId == jurisdictionTypeId.Value);
                }

                var results = await query
                    .OrderBy(j => j.Name)
                    .Take(20)
                    .Select(j => new
                    {
                        id = j.Id,
                        label = j.Code != null ? $"{j.Name} ({j.Code})" : j.Name,
                        value = j.Name,
                        code = j.Code,
                        jurisdictionTypeId = j.JurisdictionTypeId,
                        fieldNumber = j.JurisdictionType != null ? j.JurisdictionType.FieldNumber : 0
                    })
                    .ToListAsync();

                return new JsonResult(results);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching jurisdictions: {ex.Message}");
                return new JsonResult(new List<object>());
            }
        }

        public class DetectionRequest
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
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


