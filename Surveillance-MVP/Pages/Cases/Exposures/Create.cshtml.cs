using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Cases.Exposures
{
    [Authorize(Policy = "Permission.Exposure.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public Guid CaseId { get; set; }

        public string CaseFriendlyId { get; set; } = string.Empty;
        public Guid? CurrentDiseaseId { get; set; }

        [BindProperty]
        public ExposureEvent Exposure { get; set; } = new ExposureEvent 
        { 
            ExposureStatus = ExposureStatus.PotentialExposure
        };

        public SelectList EventsList { get; set; } = default!;
        public SelectList LocationsList { get; set; } = default!;
        public SelectList CasesList { get; set; } = default!;
        public SelectList CountriesList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            if (CaseId == Guid.Empty)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases.FindAsync(CaseId);
            if (caseEntity == null)
            {
                return NotFound();
            }

            CaseFriendlyId = caseEntity.FriendlyId;
            CurrentDiseaseId = caseEntity.DiseaseId;
            Exposure.CaseId = CaseId;

            await LoadSelectLists();
            return Page();
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
                    .Where(e => e.CaseId == CaseId && e.IsReportingExposure)
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
                    .CountAsync(e => e.CaseId == CaseId);
                
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
                _context.ExposureEvents.Add(Exposure);
                await _context.SaveChangesAsync();

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

            // Load other cases (excluding current case)
            var cases = await _context.Cases
                .Include(c => c.Patient)
                .Where(c => c.Id != CaseId)
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
                .Select(c => new
                {
                    c.Id,
                    DisplayText = c.FriendlyId + " - " + 
                                  (c.Patient != null ? c.Patient.GivenName + " " + c.Patient.FamilyName : "Unknown")
                })
                .Take(100) // Limit to recent 100 cases
                .ToListAsync();

            CasesList = new SelectList(cases, "Id", "DisplayText");

            // Load countries
            CountriesList = new SelectList(
                await _context.Countries
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                "Code", "Name");
        }
    }
}
