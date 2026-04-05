using System;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ICaseIdGeneratorService _caseIdGenerator;
        private readonly IAuditService _auditService;
        private readonly CustomFieldService _customFieldService;
        private readonly IExposureRequirementService _exposureRequirementService;
        private readonly ITaskService _taskService;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IPatientAddressService _patientAddressService;

        public CreateModel(
            ApplicationDbContext context, 
            ICaseIdGeneratorService caseIdGenerator, 
            IAuditService auditService,
            CustomFieldService customFieldService,
            IExposureRequirementService exposureRequirementService,
            ITaskService taskService,
            IJurisdictionService jurisdictionService,
            IPatientAddressService patientAddressService)
        {
            _context = context;
            _caseIdGenerator = caseIdGenerator;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _exposureRequirementService = exposureRequirementService;
            _taskService = taskService;
            _jurisdictionService = jurisdictionService;
            _patientAddressService = patientAddressService;
        }

        public Disease? DiseaseRequirements { get; set; }
        public bool ShouldPromptForExposure { get; set; }
        public Patient? SelectedPatient { get; set; }
        
        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();
        public Dictionary<int, List<Jurisdiction>> JurisdictionsByType { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? patientId)
        {
            ViewData["PatientId"] = new SelectList(
                await _context.Patients
                    .OrderBy(p => p.FamilyName)
                    .ThenBy(p => p.GivenName)
                    .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                    .ToListAsync(),
                "Id", "FullName", patientId);

            ViewData["ConfirmationStatusId"] = new SelectList(
                await _context.CaseStatuses
                    .Where(cs => cs.IsActive && 
                                (cs.ApplicableTo == CaseTypeApplicability.Case || 
                                 cs.ApplicableTo == CaseTypeApplicability.Both))
                    .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                    .ThenBy(cs => cs.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Disease dropdown - filtered by global query filter based on disease access
            ViewData["DiseaseId"] = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Level)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .Select(d => new { 
                        d.Id, 
                        DisplayName = new string('—', d.Level) + " " + d.Name 
                    })
                    .ToListAsync(),
                "Id", "DisplayName");

            // Load genders for patient edit functionality
            ViewData["GenderId"] = new SelectList(
                await _context.Genders
                    .Where(g => g.IsActive)
                    .OrderBy(g => g.DisplayOrder ?? int.MaxValue)
                    .ThenBy(g => g.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Load jurisdiction data
            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            JurisdictionsByType = await _jurisdictionService.GetGroupedJurisdictionsAsync();

            // Initialize Case with defaults
            Case = new Case 
            { 
                Type = CaseType.Case,
                DateOfNotification = DateTime.Today
            };

            if (patientId.HasValue)
            {
                Case.PatientId = patientId.Value;
                SelectedPatient = await _context.Patients.FindAsync(patientId.Value);
            }

            return Page();
        }

        [BindProperty]
        public Case Case { get; set; } = default!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.FamilyName)
                        .ThenBy(p => p.GivenName)
                        .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                    .ToListAsync(),
                    "Id", "FullName");

                ViewData["ConfirmationStatusId"] = new SelectList(
                    await _context.CaseStatuses
                        .Where(cs => cs.IsActive && 
                                    (cs.ApplicableTo == CaseTypeApplicability.Case || 
                                     cs.ApplicableTo == CaseTypeApplicability.Both))
                        .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                        .ThenBy(cs => cs.Name)
                        .ToListAsync(),
                    "Id", "Name");

                // Disease dropdown - filtered by global query filter based on disease access
                ViewData["DiseaseId"] = new SelectList(
                    await _context.Diseases
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.Level)
                        .ThenBy(d => d.DisplayOrder)
                        .ThenBy(d => d.Name)
                        .Select(d => new { 
                            d.Id, 
                            DisplayName = new string('—', d.Level) + " " + d.Name 
                        })
                        .ToListAsync(),
                    "Id", "DisplayName");

                // Reload jurisdiction data
                ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
                JurisdictionsByType = await _jurisdictionService.GetGroupedJurisdictionsAsync();

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                // Set default Date of Onset to today if not provided
                if (!Case.DateOfOnset.HasValue)
                {
                    Case.DateOfOnset = DateTime.Today;
                }

                // Validate exposure requirements if disease requires it
                if (Case.DiseaseId.HasValue)
                {
                    var requirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                    
                    if (requirements != null && 
                        (requirements.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
                         requirements.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired))
                    {
                        // Check if case will have exposure data (we can't check yet since case isn't saved)
                        // Instead, we'll show a soft warning via TempData
                        TempData["ExposureWarning"] = $"This disease requires exposure data. Please add exposure information after creating the case.";
                    }
                }

                Case.Id = Guid.NewGuid();
                Case.FriendlyId = await _caseIdGenerator.GenerateNextCaseIdAsync();

                // Auto-detect jurisdictions from patient's address if not set
                await AutoDetectJurisdictionsFromPatientAsync();

                _context.Cases.Add(Case);
                await _context.SaveChangesAsync();

                // Snapshot patient address to case (if disease configured for it)
                if (Case.DiseaseId.HasValue)
                {
                    var settings = await _patientAddressService.GetEffectiveAddressSettingsAsync(Case.DiseaseId.Value);
                    if (settings.DefaultToResidentialAddress)
                    {
                        await _patientAddressService.CopyAddressToCaseAsync(Case.Id, manualOverride: false);
                    }
                }

                // ? Task auto-creation now handled by CaseCreationInterceptor
                // No need to manually call AutoCreateTasksForNewCase anymore
                // The interceptor will automatically create tasks after SaveChangesAsync completes

                // Auto-create exposure from residential address if configured
                System.Diagnostics.Debug.WriteLine($"=== Auto-Create Exposure Check ===");
                System.Diagnostics.Debug.WriteLine($"Case.DiseaseId: {Case.DiseaseId}");
                System.Diagnostics.Debug.WriteLine($"Case.PatientId: {Case.PatientId}");
                System.Diagnostics.Debug.WriteLine($"PatientId != Empty: {Case.PatientId != Guid.Empty}");
                
                if (Case.DiseaseId.HasValue && Case.PatientId != Guid.Empty)
                {
                    System.Diagnostics.Debug.WriteLine("Checking disease requirements...");
                    var requirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                    System.Diagnostics.Debug.WriteLine($"Requirements found: {requirements != null}");
                    System.Diagnostics.Debug.WriteLine($"DefaultToResidentialAddress: {requirements?.DefaultToResidentialAddress}");
                    
                    if (requirements != null && requirements.DefaultToResidentialAddress)
                    {
                        System.Diagnostics.Debug.WriteLine("Loading patient...");
                        var patient = await _context.Patients.FindAsync(Case.PatientId);
                        System.Diagnostics.Debug.WriteLine($"Patient found: {patient != null}");
                        System.Diagnostics.Debug.WriteLine($"Patient AddressLine: {patient?.AddressLine}");
                        
                        if (patient != null && !string.IsNullOrWhiteSpace(patient.AddressLine))
                        {
                            System.Diagnostics.Debug.WriteLine("Creating exposure event...");
                            // Create exposure event from residential address
                            var exposureEvent = new ExposureEvent
                            {
                                Id = Guid.NewGuid(),
                                ExposedCaseId = Case.Id,
                                ExposureType = ExposureType.Location,
                                ExposureStartDate = Case.DateOfOnset ?? DateTime.Today,
                                ExposureStatus = ExposureStatus.PotentialExposure,
                                
                                // Structured address fields
                                AddressLine = patient.AddressLine,
                                City = patient.City,
                                State = patient.State?.Code,
                                PostalCode = patient.PostalCode,
                                Country = "Australia", // Default to Australia

                                // Legacy free-text field for backward compatibility
                                FreeTextLocation = $"{patient.AddressLine}, {patient.City}, {patient.State?.Code} {patient.PostalCode}".Trim(),
                                
                                // Geocoding from patient if available (convert double to decimal)
                                Latitude = patient.Latitude.HasValue ? (decimal?)patient.Latitude.Value : null,
                                Longitude = patient.Longitude.HasValue ? (decimal?)patient.Longitude.Value : null,
                                
                                // Flags
                                IsDefaultedFromResidentialAddress = true,
                                IsReportingExposure = true, // First exposure is the reporting exposure
                                
                                Description = "Automatically populated from patient's residential address",
                                CreatedDate = DateTime.UtcNow,
                                CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                            };

                            _context.ExposureEvents.Add(exposureEvent);
                            await _context.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine($"Exposure created with ID: {exposureEvent.Id}");

                            TempData["ExposureInfo"] = "An exposure record has been automatically created from the patient's residential address.";
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Patient or address not found - exposure NOT created");
                            if (patient == null)
                                TempData["ExposureWarning"] = "Could not auto-create exposure: Patient not found.";
                            else
                                TempData["ExposureWarning"] = "Could not auto-create exposure: Patient has no residential address.";
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Disease not configured for auto-create exposure");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("DiseaseId or PatientId missing - cannot auto-create exposure");
                }

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Created",
                    oldValue: null,
                    newValue: $"Case {Case.FriendlyId} created",
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Case {Case.FriendlyId} created successfully.";
                return RedirectToPage("./Details", new { id = Case.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the case.";
                
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.FamilyName)
                        .ThenBy(p => p.GivenName)
                        .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                        .ToListAsync(),
                    "Id", "FullName");

                ViewData["ConfirmationStatusId"] = new SelectList(
                    await _context.CaseStatuses
                        .Where(cs => cs.IsActive)
                        .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                        .ThenBy(cs => cs.Name)
                        .ToListAsync(),
                    "Id", "Name");

                return Page();
            }
        }

        private async Task AutoDetectJurisdictionsFromPatientAsync()
        {
            // Check if case jurisdictions are already populated - if so, don't override
            if (Case.Jurisdiction1Id.HasValue || Case.Jurisdiction2Id.HasValue || 
                Case.Jurisdiction3Id.HasValue || Case.Jurisdiction4Id.HasValue || 
                Case.Jurisdiction5Id.HasValue)
                return;

            // Get patient's coordinates
            var patient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == Case.PatientId);

            if (patient == null || !patient.Latitude.HasValue || !patient.Longitude.HasValue)
                return;

            try
            {
                var detectedJurisdictions = await _jurisdictionService.FindJurisdictionsContainingPointAsync(
                    patient.Latitude.Value,
                    patient.Longitude.Value
                );

                // Auto-assign to case jurisdiction fields
                foreach (var jurisdiction in detectedJurisdictions)
                {
                    var fieldNumber = jurisdiction.JurisdictionType?.FieldNumber;
                    if (!fieldNumber.HasValue) continue;

                    switch (fieldNumber.Value)
                    {
                        case 1:
                            if (!Case.Jurisdiction1Id.HasValue)
                                Case.Jurisdiction1Id = jurisdiction.Id;
                            break;
                        case 2:
                            if (!Case.Jurisdiction2Id.HasValue)
                                Case.Jurisdiction2Id = jurisdiction.Id;
                            break;
                        case 3:
                            if (!Case.Jurisdiction3Id.HasValue)
                                Case.Jurisdiction3Id = jurisdiction.Id;
                            break;
                        case 4:
                            if (!Case.Jurisdiction4Id.HasValue)
                                Case.Jurisdiction4Id = jurisdiction.Id;
                            break;
                        case 5:
                            if (!Case.Jurisdiction5Id.HasValue)
                                Case.Jurisdiction5Id = jurisdiction.Id;
                            break;
                    }
                }

                if (detectedJurisdictions.Any())
                {
                    Console.WriteLine($"Auto-detected {detectedJurisdictions.Count} jurisdictions for case from patient address");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error auto-detecting jurisdictions for case: {ex.Message}");
            }
        }
    }
}
