using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Exposure.Create")]
    public class AddExposureModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddExposureModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public Guid CaseId { get; set; }

        public Case Case { get; set; } = default!;

        [BindProperty]
        public ExposureEvent Exposure { get; set; } = new ExposureEvent
        {
            ExposureStatus = ExposureStatus.PotentialExposure,
            ExposureStartDate = DateTime.Now
        };

        [BindProperty]
        public string ExposureDirection { get; set; } = "Acquisition";

        public async Task<IActionResult> OnGetAsync()
        {
            if (CaseId == Guid.Empty)
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

            // Default: Current case is the one who got exposed (Acquisition)
            Exposure.ExposedCaseId = CaseId;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Case = await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .FirstOrDefaultAsync(c => c.Id == CaseId);
                return Page();
            }

            // Handle Exposure Direction
            if (ExposureDirection == "Transmission")
            {
                // Current case is the SOURCE (exposed others)
                Exposure.SourceCaseId = CaseId;
                
                // For Transmission with Location/Event, we don't set ExposedCaseId yet
                // It will be set when contacts are created via Bulk Create Contacts
                // For now, create a "placeholder" exposure with just the location/event
                // Set ExposedCaseId to CaseId temporarily to satisfy required constraint
                if (Exposure.ExposureType == ExposureType.Location || Exposure.ExposureType == ExposureType.Event)
                {
                    Exposure.ExposedCaseId = CaseId; // Will be updated when contacts are linked
                }
            }
            else
            {
                // Current case is EXPOSED (got infected) - default behavior
                Exposure.ExposedCaseId = CaseId;
            }

            // Validate exposure type-specific required fields
            if (!ValidateExposureTypeFields())
            {
                Case = await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .FirstOrDefaultAsync(c => c.Id == CaseId);
                return Page();
            }

            // Validate dates
            if (Exposure.ExposureEndDate.HasValue && Exposure.ExposureEndDate.Value <= Exposure.ExposureStartDate)
            {
                ModelState.AddModelError("Exposure.ExposureEndDate", "End date/time must be after start date/time.");
                Case = await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .FirstOrDefaultAsync(c => c.Id == CaseId);
                return Page();
            }

            // Handle reporting exposure flag (only for Acquisitions)
            if (ExposureDirection == "Acquisition" && Exposure.IsReportingExposure)
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

            // Clear navigation properties
            Exposure.ExposedCase = null;
            Exposure.Event = null;
            Exposure.Location = null;
            Exposure.SourceCase = null;

            _context.ExposureEvents.Add(Exposure);
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
    }
}

