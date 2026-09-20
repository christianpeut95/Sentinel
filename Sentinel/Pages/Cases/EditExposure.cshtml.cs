using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Exposure.Edit")]
    [Authorize(Policy = "Permission.Case.Edit")]
    public class EditExposureModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthorizationService _authorizationService;

        public EditExposureModel(ApplicationDbContext context, IAuthorizationService authorizationService)
        {
            _context = context;
            _authorizationService = authorizationService;
        }

        [BindProperty(SupportsGet = true)]
        public Guid Id { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid CaseId { get; set; }

        public Case Case { get; set; } = default!;

        [BindProperty]
        public ExposureEvent Exposure { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            if (Id == Guid.Empty || CaseId == Guid.Empty)
            {
                return NotFound();
            }

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == CaseId);

            if (Case == null)
            {
                return NotFound();
            }

            // Check both ExposedCaseId (acquisition) and SourceCaseId (transmission)
            // only after the selected case has passed the case visibility boundary.
            Exposure = await _context.ExposureEvents
                .FirstOrDefaultAsync(e => e.Id == Id &&
                    (e.ExposedCaseId == CaseId || e.SourceCaseId == CaseId));

            if (Exposure == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Resolve the parent case first. It is the source of truth for both
            // object-level and hierarchy-aware disease access.
            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == CaseId);

            if (Case == null)
            {
                return NotFound();
            }

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
                return Page();
            }

            // Check both ExposedCaseId (acquisition) and SourceCaseId (transmission)
            var existingExposure = await _context.ExposureEvents
                .FirstOrDefaultAsync(e => e.Id == Id && 
                    (e.ExposedCaseId == CaseId || e.SourceCaseId == CaseId));

            if (existingExposure == null)
            {
                return NotFound();
            }

            // Validate exposure type-specific required fields
            if (!ValidateExposureTypeFields())
            {
                return Page();
            }

            // Validate dates
            if (Exposure.ExposureEndDate.HasValue && Exposure.ExposureEndDate.Value <= Exposure.ExposureStartDate)
            {
                ModelState.AddModelError("Exposure.ExposureEndDate", "End date/time must be after start date/time.");
                return Page();
            }

            // Handle reporting exposure flag
            if (Exposure.IsReportingExposure)
            {
                // Unset any existing reporting exposure for this case
                var existingReportingExposures = await _context.ExposureEvents
                    .Where(e => e.ExposedCaseId == CaseId && e.IsReportingExposure && e.Id != Id)
                    .ToListAsync();

                foreach (var existing in existingReportingExposures)
                {
                    existing.IsReportingExposure = false;
                }
            }

            // Update properties
            existingExposure.ExposureType = Exposure.ExposureType;
            existingExposure.ExposureStartDate = Exposure.ExposureStartDate;
            existingExposure.ExposureEndDate = Exposure.ExposureEndDate;
            existingExposure.EventId = Exposure.EventId;
            existingExposure.LocationId = Exposure.LocationId;
            existingExposure.SourceCaseId = Exposure.SourceCaseId;
            existingExposure.CountryCode = Exposure.CountryCode;
            existingExposure.ExposureStatus = Exposure.ExposureStatus;
            existingExposure.Description = Exposure.Description;
            existingExposure.IsReportingExposure = Exposure.IsReportingExposure;

            await _context.SaveChangesAsync();

            // Close window and refresh parent
            return Content(
                "<script>window.opener.location.reload(); window.close();</script>",
                "text/html"
            );
        }

        private bool ValidateExposureTypeFields()
        {
            switch (Exposure.ExposureType)
            {
                case ExposureType.Event:
                    if (!Exposure.EventId.HasValue)
                    {
                        ModelState.AddModelError("Exposure.EventId", "Event is required for Event exposure type.");
                        return false;
                    }
                    break;

                case ExposureType.Location:
                    if (!Exposure.LocationId.HasValue)
                    {
                        ModelState.AddModelError("Exposure.LocationId", "Location is required for Location exposure type.");
                        return false;
                    }
                    break;

                case ExposureType.Contact:
                    if (!Exposure.SourceCaseId.HasValue)
                    {
                        ModelState.AddModelError("Exposure.SourceCaseId", "Source case is required for Contact exposure type.");
                        return false;
                    }
                    break;

                case ExposureType.Travel:
                    if (string.IsNullOrEmpty(Exposure.CountryCode))
                    {
                        ModelState.AddModelError("Exposure.CountryCode", "Country is required for Travel exposure type.");
                        return false;
                    }
                    break;
            }

            return true;
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
    }
}
