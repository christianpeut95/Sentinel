using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Locations
{
    [Authorize(Policy = "Permission.Location.View")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Location Location { get; set; } = default!;
        public IList<Event> Events { get; set; } = default!;
        public int DirectExposureCount { get; set; }
        public int TotalExposureCount { get; set; }
        public Dictionary<Guid, int> EventExposureCounts { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations
                .Include(l => l.LocationType)
                .Include(l => l.Organization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (location == null)
            {
                return NotFound();
            }

            Location = location;

            // Load events at this location
            Events = await _context.Events
                .Include(e => e.EventType)
                .Where(e => e.LocationId == id)
                .OrderByDescending(e => e.StartDateTime)
                .ToListAsync();

            // Get exposure counts for each event
            var eventExposureCounts = await _context.ExposureEvents
                .Where(ee => ee.EventId != null && Events.Select(e => e.Id).Contains(ee.EventId.Value))
                .GroupBy(ee => ee.EventId!.Value)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToListAsync();

            EventExposureCounts = eventExposureCounts.ToDictionary(x => x.EventId, x => x.Count);

            // Count direct exposures (not through events)
            DirectExposureCount = await _context.ExposureEvents
                .Where(ee => ee.LocationId == id && ee.EventId == null)
                .CountAsync();

            // Total exposures (direct + through events)
            var eventExposureTotal = EventExposureCounts.Values.Sum();
            TotalExposureCount = DirectExposureCount + eventExposureTotal;

            return Page();
        }
    }
}
