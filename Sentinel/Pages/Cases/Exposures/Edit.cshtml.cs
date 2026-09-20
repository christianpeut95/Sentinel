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
    [Authorize(Policy = "Permission.Case.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public EditModel(ApplicationDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
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

            if (!await CanUseReferencesAsync(exposure))
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
            // Resolve the existing exposure through the normal query filter before
            // using any posted values. This prevents a caller from updating an
            // exposure belonging to a case they cannot access.
            var existingExposure = await _context.ExposureEvents
                .FirstOrDefaultAsync(e => e.Id == Exposure.Id);
            if (existingExposure == null)
            {
                return NotFound();
            }

            var exposedCase = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == existingExposure.ExposedCaseId);
            if (exposedCase == null)
            {
                return NotFound();
            }

            // The exposed case and audit fields are server-owned. Do not let a form
            // move an exposure between cases or overwrite its history.
            Exposure.ExposedCaseId = existingExposure.ExposedCaseId;

            if (Exposure.SourceCaseId.HasValue &&
                !await _context.Cases.AnyAsync(c => c.Id == Exposure.SourceCaseId.Value))
            {
                ModelState.AddModelError(nameof(Exposure.SourceCaseId), "The selected source case is not available.");
            }

            if (!await CanUseReferencesAsync(Exposure))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = exposedCase.FriendlyId;
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Validate exposure type-specific required fields
            if (!ValidateExposureTypeFields())
            {
                await LoadSelectLists();
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = exposedCase.FriendlyId;
                return Page();
            }

            // Validate dates
            if (Exposure.ExposureEndDate.HasValue && Exposure.ExposureEndDate.Value <= Exposure.ExposureStartDate)
            {
                ModelState.AddModelError("Exposure.ExposureEndDate", "End date/time must be after start date/time.");
                await LoadSelectLists();
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = exposedCase.FriendlyId;
                return Page();
            }

            // Update status changed metadata
            var originalExposure = await _context.ExposureEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == existingExposure.Id);

            if (originalExposure != null && originalExposure.ExposureStatus != Exposure.ExposureStatus)
            {
                existingExposure.StatusChangedDate = DateTime.UtcNow;
                existingExposure.StatusChangedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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
                existingExposure.GeocodedDate = DateTime.UtcNow;
            }

            ApplyEditableFields(existingExposure, Exposure);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Exposure updated successfully.";
                return RedirectToPage("/Cases/Details", new { id = existingExposure.ExposedCaseId });
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
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
                await LoadSelectLists();
                CaseId = Exposure.ExposedCaseId;
                CaseFriendlyId = exposedCase.FriendlyId;
                return Page();
            }
        }

        private static void ApplyEditableFields(ExposureEvent target, ExposureEvent input)
        {
            target.ExposureType = input.ExposureType;
            target.ExposureStartDate = input.ExposureStartDate;
            target.ExposureEndDate = input.ExposureEndDate;
            target.EventId = input.EventId;
            target.LocationId = input.LocationId;
            target.SourceCaseId = input.SourceCaseId;
            target.ContactClassificationId = input.ContactClassificationId;
            target.CountryCode = input.CountryCode;
            target.FreeTextLocation = input.FreeTextLocation;
            target.Description = input.Description;
            target.ExposureStatus = input.ExposureStatus;
            target.ConfidenceLevel = input.ConfidenceLevel;
            target.IsReportingExposure = input.IsReportingExposure;
            target.AddressLine = input.AddressLine;
            target.City = input.City;
            target.State = input.State;
            target.PostalCode = input.PostalCode;
            target.Country = input.Country;
            target.Latitude = input.Latitude;
            target.Longitude = input.Longitude;
            target.InvestigationNotes = input.InvestigationNotes;
        }

        private bool ExposureExists(Guid id)
        {
            return _context.ExposureEvents.Any(e => e.Id == id);
        }

        private async Task<bool> CanUseReferencesAsync(ExposureEvent exposure)
        {
            if (exposure.LocationId.HasValue)
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission.Location.View")).Succeeded ||
                    !await _context.Locations.AnyAsync(location =>
                        location.Id == exposure.LocationId.Value && location.IsActive))
                {
                    return false;
                }
            }

            if (exposure.EventId.HasValue)
            {
                if (!(await _authorizationService.AuthorizeAsync(User, "Permission.Event.View")).Succeeded ||
                    !await _context.Events.AnyAsync(@event =>
                        @event.Id == exposure.EventId.Value && @event.IsActive))
                {
                    return false;
                }
            }

            return true;
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
            if ((await _authorizationService.AuthorizeAsync(User, "Permission.Event.View")).Succeeded)
            {
                // Load events with locations and dates only for users who may view events.
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
            }
            else
            {
                EventsList = new SelectList(Array.Empty<object>());
            }

            if ((await _authorizationService.AuthorizeAsync(User, "Permission.Location.View")).Succeeded)
            {
                LocationsList = new SelectList(
                    await _context.Locations
                        .Where(l => l.IsActive)
                        .OrderBy(l => l.Name)
                        .ToListAsync(),
                    "Id", "Name");
            }
            else
            {
                LocationsList = new SelectList(Array.Empty<object>());
            }

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
