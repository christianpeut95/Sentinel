using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Cases.Exposures
{
    [Authorize(Policy = "Permission.Exposure.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IExposureRequirementService _exposureRequirementService;

        public CreateModel(ApplicationDbContext context, IExposureRequirementService exposureRequirementService)
        {
            _context = context;
            _exposureRequirementService = exposureRequirementService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid CaseId { get; set; }

        public string CaseFriendlyId { get; set; } = string.Empty;
        public Guid? CurrentDiseaseId { get; set; }
        public string? ExposureGuidanceText { get; set; }
        public bool ShouldPromptForExposure { get; set; }

        [BindProperty]
        public ExposureEvent Exposure { get; set; } = new ExposureEvent 
        { 
            ExposureStatus = ExposureStatus.PotentialExposure
        };

        public SelectList EventsList { get; set; } = default!;
        public SelectList LocationsList { get; set; } = default!;
        public SelectList CasesList { get; set; } = default!;
        public SelectList CountriesList { get; set; } = default!;
        public SelectList ContactClassificationsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            if (CaseId == Guid.Empty)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.CaseSymptoms)
                .Include(c => c.LabResults)
                .Include(c => c.ExposureEvents)
                .FirstOrDefaultAsync(c => c.Id == CaseId);
                
            if (caseEntity == null)
            {
                return NotFound();
            }

            CaseFriendlyId = caseEntity.FriendlyId;
            CurrentDiseaseId = caseEntity.DiseaseId;
            Exposure.ExposedCaseId = CaseId;

            // Calculate smart default for exposure start date
            var earliestDate = CalculateEarliestRelevantDate(caseEntity);
            if (earliestDate.HasValue)
            {
                Exposure.ExposureStartDate = earliestDate.Value;
            }

            // Auto-check "Primary Reporting Exposure" if this is the first exposure
            var existingExposureCount = caseEntity.ExposureEvents?.Count(e => e.Id != Guid.Empty) ?? 0;
            if (existingExposureCount == 0)
            {
                Exposure.IsReportingExposure = true;
            }

            // Load exposure guidance from disease settings
            if (CurrentDiseaseId.HasValue)
            {
                var requirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(CurrentDiseaseId.Value);
                if (requirements != null)
                {
                    ExposureGuidanceText = requirements.ExposureGuidanceText;
                    ShouldPromptForExposure = await _exposureRequirementService.ShouldPromptForExposureAsync(CurrentDiseaseId.Value);
                }
            }

            await LoadSelectLists();
            return Page();
        }

        private DateTime? CalculateEarliestRelevantDate(Case caseEntity)
        {
            var dates = new List<DateTime>();

            // Add symptom onset dates
            if (caseEntity.CaseSymptoms != null && caseEntity.CaseSymptoms.Any())
            {
                var symptomDates = caseEntity.CaseSymptoms
                    .Where(cs => cs.OnsetDate.HasValue)
                    .Select(cs => cs.OnsetDate!.Value);
                dates.AddRange(symptomDates);
            }

            // Add lab result specimen collection dates
            if (caseEntity.LabResults != null && caseEntity.LabResults.Any())
            {
                var labDates = caseEntity.LabResults
                    .Where(lr => lr.SpecimenCollectionDate.HasValue)
                    .Select(lr => lr.SpecimenCollectionDate!.Value);
                dates.AddRange(labDates);
            }

            // Add notification date
            if (caseEntity.DateOfNotification.HasValue)
            {
                dates.Add(caseEntity.DateOfNotification.Value);
            }

            // Add onset date
            if (caseEntity.DateOfOnset.HasValue)
            {
                dates.Add(caseEntity.DateOfOnset.Value);
            }

            // Return earliest date, or null if no dates found
            return dates.Any() ? dates.Min() : null;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(CaseId);
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Validate exposure type-specific required fields
            if (!ValidateExposureTypeFields())
            {
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(CaseId);
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }

            // Validate dates
            if (Exposure.ExposureEndDate.HasValue && Exposure.ExposureEndDate.Value <= Exposure.ExposureStartDate)
            {
                ModelState.AddModelError("Exposure.ExposureEndDate", "End date/time must be after start date/time.");
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(CaseId);
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }

            // Handle reporting exposure flag
            if (Exposure.IsReportingExposure)
            {
                // Unset any existing reporting exposure for this case
                var existingReportingExposures = await _context.ExposureEvents
                    .Where(e => e.ExposedCaseId == CaseId && e.IsReportingExposure)
                    .ToListAsync();
                
                foreach (var existing in existingReportingExposures)
                {
                    existing.IsReportingExposure = false;
                }
            }
            else
            {
                // If this is the first exposure for the case, make it the reporting exposure
                var existingExposureCount = await _context.ExposureEvents
                    .CountAsync(e => e.ExposedCaseId == CaseId);
                
                if (existingExposureCount == 0)
                {
                    Exposure.IsReportingExposure = true;
                }
            }

            // Set status changed metadata
            Exposure.StatusChangedDate = DateTime.UtcNow;
            Exposure.StatusChangedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Set geocoding date if coordinates are provided
            if (Exposure.Latitude.HasValue && Exposure.Longitude.HasValue && !string.IsNullOrWhiteSpace(Exposure.GeocodingAccuracy))
            {
                Exposure.GeocodedDate = DateTime.UtcNow;
            }

            try
            {
                // DIAGNOSTIC LOGGING
                Console.WriteLine("=== ADD EXPOSURE DIAGNOSTIC ===");
                Console.WriteLine($"Exposed Case ID: {Exposure.ExposedCaseId}");
                Console.WriteLine($"Exposure ID: {Exposure.Id}");
                Console.WriteLine($"Exposure Type: {Exposure.ExposureType}");
                Console.WriteLine($"Start Date: {Exposure.ExposureStartDate}");
                Console.WriteLine($"Database: {_context.Database.GetConnectionString()}");
                Console.WriteLine($"DbContext Hash: {_context.GetHashCode()}");

                Console.WriteLine("About to add Exposure to context...");
                _context.ExposureEvents.Add(Exposure);
                
                Console.WriteLine("About to call SaveChangesAsync...");
                var changeCount = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges completed. Changes saved: {changeCount}");
                Console.WriteLine($"Exposure ID after save: {Exposure.Id}");
                
                // Verify it was actually saved
                var verifyResult = await _context.ExposureEvents
                    .Where(e => e.Id == Exposure.Id)
                    .FirstOrDefaultAsync();
                Console.WriteLine($"Verification query result: {(verifyResult != null ? "FOUND" : "NOT FOUND")}");
                
                if (verifyResult == null)
                {
                    Console.WriteLine("ERROR: Exposure not found in database after save!");
                }
                Console.WriteLine("=== END DIAGNOSTIC ===");
                await _context.SaveChangesAsync();

                // Check if in iframe (for CreateNew workflow)
                var isInIframe = Request.Query.ContainsKey("iframe");
                
                if (!isInIframe)
                {
                    var referer = Request.Headers["Referer"].ToString();
                    isInIframe = referer.Contains("/Cases/CreateNew");
                }

                if (isInIframe)
                {
                    // Post message to parent window with success notification
                    return Content(
                        @"<html>
                        <head>
                            <style>
                                body {
                                    display: flex;
                                    align-items: center;
                                    justify-content: center;
                                    height: 100vh;
                                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                                    background: #f0fdf4;
                                    margin: 0;
                                }
                                .success-message {
                                    text-align: center;
                                    padding: 2rem;
                                }
                                .success-icon {
                                    font-size: 4rem;
                                    color: #10b981;
                                    margin-bottom: 1rem;
                                }
                                .success-text {
                                    font-size: 1.25rem;
                                    color: #166534;
                                    font-weight: 600;
                                }
                            </style>
                        </head>
                        <body>
                            <div class='success-message'>
                                <div class='success-icon'>?</div>
                                <div class='success-text'>Exposure Saved Successfully!</div>
                                <p style='color: #16a34a; margin-top: 0.5rem;'>This window will close automatically...</p>
                            </div>
                            <script>
                                console.log('Exposure saved, posting message to parent');
                                if (window.parent && window.parent !== window) {
                                    console.log('Posting message: exposureSaved');
                                    window.parent.postMessage('exposureSaved', '*');
                                }
                                setTimeout(function() {
                                    console.log('Attempting to trigger modal close');
                                    if (window.parent && window.parent !== window) {
                                        window.parent.postMessage('exposureSaved', '*');
                                    }
                                }, 500);
                            </script>
                        </body>
                        </html>",
                        "text/html"
                    );
                }

                TempData["SuccessMessage"] = $"Exposure added successfully.";
                return RedirectToPage("/Cases/Details", new { id = CaseId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while adding the exposure: {ex.Message}";
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(CaseId);
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }
        }

        private bool ValidateExposureTypeFields()
        {
            bool isValid = true;

            switch (Exposure.ExposureType)
            {
                case ExposureType.Event:
                    if (!Exposure.EventId.HasValue)
                    {
                        ModelState.AddModelError("Exposure.EventId", "Event is required for Event-type exposures.");
                        isValid = false;
                    }
                    break;

                case ExposureType.Location:
                    if (!Exposure.LocationId.HasValue && string.IsNullOrWhiteSpace(Exposure.FreeTextLocation))
                    {
                        ModelState.AddModelError("Exposure.LocationId", "Either select a location or enter free-text location.");
                        isValid = false;
                    }
                    break;

                case ExposureType.Travel:
                    if (string.IsNullOrWhiteSpace(Exposure.CountryCode))
                    {
                        ModelState.AddModelError("Exposure.CountryCode", "Country is required for Travel-type exposures.");
                        isValid = false;
                    }
                    break;

                case ExposureType.LocallyAcquired:
                    // Address should be populated from patient's residential address
                    // Validation is optional as it comes from patient record
                    break;

                case ExposureType.Contact:
                    // RelatedCaseId is optional for contacts
                    break;
            }

            return isValid;
        }

        private async Task LoadSelectLists()
        {
            // Load events with locations and dates
            var events = await _context.Events
                .Include(e => e.Location)
                .Where(e => e.IsActive)
                .OrderByDescending(e => e.StartDateTime)
                .Select(e => new
                {
                    e.Id,
                    DisplayText = e.Name + " - " + e.StartDateTime.ToString("dd MMM yyyy") + 
                                  (e.Location != null ? " at " + e.Location.Name : "")
                })
                .ToListAsync();

            EventsList = new SelectList(events, "Id", "DisplayText");

            // Load locations
            LocationsList = new SelectList(
                await _context.Locations
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Load other cases (excluding current case) - Filter to Contacts with same disease
            var cases = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .Where(c => c.Id != CaseId && 
                           c.Type == CaseType.Contact && 
                           c.DiseaseId == CurrentDiseaseId &&
                           !c.IsDeleted)
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
                .Select(c => new
                {
                    c.Id,
                    DisplayText = c.FriendlyId + " - " + 
                                  (c.Patient != null ? c.Patient.GivenName + " " + c.Patient.FamilyName : "Unknown") +
                                  " - " + (c.DateOfOnset.HasValue ? c.DateOfOnset.Value.ToString("dd/MM/yyyy") : "No onset") +
                                  " - " + (c.ConfirmationStatus != null ? c.ConfirmationStatus.Name : "No status")
                })
                .Take(200) // Increased limit for contacts
                .ToListAsync();

            CasesList = new SelectList(cases, "Id", "DisplayText");

            // Load countries
            CountriesList = new SelectList(
                await _context.Countries
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Code", "Name");

            // Load contact classifications
            ContactClassificationsList = new SelectList(
                await _context.ContactClassifications
                    .Where(cc => cc.IsActive)
                    .OrderBy(cc => cc.DisplayOrder)
                    .ThenBy(cc => cc.Name)
                    .ToListAsync(),
                "Id", "Name");
        }
    }
}
