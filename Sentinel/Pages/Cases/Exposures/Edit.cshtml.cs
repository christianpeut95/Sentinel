using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.Cases.Exposures
{
    [Authorize(Policy = "Permission.Exposure.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ExposureEvent Exposure { get; set; } = default!;

        public string CaseFriendlyId { get; set; } = string.Empty;
        public Guid CaseId { get; set; }
        public bool HasOtherReportingExposure { get; set; }

        public SelectList EventsList { get; set; } = default!;
        public SelectList LocationsList { get; set; } = default!;
        public SelectList CasesList { get; set; } = default!;
        public SelectList CountriesList { get; set; } = default!;
        public SelectList ContactClassificationsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var exposure = await _context.ExposureEvents
                .Include(e => e.ExposedCase)
                .Include(e => e.Event)
                .Include(e => e.Location)
                .Include(e => e.SourceCase)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exposure == null)
            {
                return NotFound();
            }

            Exposure = exposure;
            CaseId = exposure.ExposedCaseId;
            CaseFriendlyId = exposure.ExposedCase?.FriendlyId ?? "";

            // Check if another exposure is marked as reporting exposure
            HasOtherReportingExposure = await _context.ExposureEvents
                .AnyAsync(e => e.ExposedCaseId == CaseId && e.Id != id && e.IsReportingExposure);

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(Exposure.ExposedCaseId);
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Validate exposure type-specific required fields
            if (!ValidateExposureTypeFields())
            {
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(Exposure.ExposedCaseId);
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }

            // Validate dates
            if (Exposure.ExposureEndDate.HasValue && Exposure.ExposureEndDate.Value <= Exposure.ExposureStartDate)
            {
                ModelState.AddModelError("Exposure.ExposureEndDate", "End date/time must be after start date/time.");
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(Exposure.ExposedCaseId);
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }

            // Update status changed metadata
            var originalExposure = await _context.ExposureEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == Exposure.Id);

            if (originalExposure != null && originalExposure.ExposureStatus != Exposure.ExposureStatus)
            {
                Exposure.StatusChangedDate = DateTime.UtcNow;
                Exposure.StatusChangedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            // Handle reporting exposure flag
            if (Exposure.IsReportingExposure)
            {
                // Unset any other reporting exposure for this case
                var otherReportingExposures = await _context.ExposureEvents
                    .Where(e => e.ExposedCaseId == Exposure.ExposedCaseId && e.Id != Exposure.Id && e.IsReportingExposure)
                    .ToListAsync();
                
                foreach (var other in otherReportingExposures)
                {
                    other.IsReportingExposure = false;
                }
            }

            // Set geocoding date if coordinates are provided
            if (Exposure.Latitude.HasValue && Exposure.Longitude.HasValue && 
                !string.IsNullOrWhiteSpace(Exposure.GeocodingAccuracy) &&
                originalExposure != null &&
                (originalExposure.Latitude != Exposure.Latitude || originalExposure.Longitude != Exposure.Longitude))
            {
                Exposure.GeocodedDate = DateTime.UtcNow;
            }

            _context.Attach(Exposure).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exposure updated successfully.";
                return RedirectToPage("/Cases/Details", new { id = Exposure.ExposedCaseId });
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExposureExists(Exposure.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the exposure: {ex.Message}";
                await LoadSelectLists();
                var caseEntity = await _context.Cases.FindAsync(Exposure.ExposedCaseId);
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = caseEntity?.FriendlyId ?? "";
                return Page();
            }
        }

        private bool ExposureExists(Guid id)
        {
            return _context.ExposureEvents.Any(e => e.Id == id);
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
                    // SourceCaseId is optional for contacts
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
                .Where(c => c.Id != Exposure.ExposedCaseId)
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
                .Select(c => new
                {
                    c.Id,
                    DisplayText = c.FriendlyId + " - " +
                                  (c.Patient != null ? c.Patient.GivenName + " " + c.Patient.FamilyName : "Unknown")
                })
                .Take(100)
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
