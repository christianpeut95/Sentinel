using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Events
{
    [Authorize(Policy = "Permission.Event.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Event> Events { get; set; } = default!;
        public IList<EventType> EventTypes { get; set; } = default!;
        public IList<Location> Locations { get; set; } = default!;
        public Dictionary<Guid, int> ExposureCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EventTypeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? LocationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool IndoorOnly { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool ActiveOnly { get; set; } = true;

        public async Task OnGetAsync()
        {
            // Load filter dropdown data
            EventTypes = await _context.EventTypes
                .Where(et => et.IsActive)
                .OrderBy(et => et.DisplayOrder)
                .ThenBy(et => et.Name)
                .ToListAsync();

            Locations = await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            // Build query
            var query = _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Location)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(e =>
                    e.Name.Contains(SearchTerm) ||
                    (e.Description != null && e.Description.Contains(SearchTerm)));
            }

            if (EventTypeId.HasValue)
            {
                query = query.Where(e => e.EventTypeId == EventTypeId.Value);
            }

            if (LocationId.HasValue)
            {
                query = query.Where(e => e.LocationId == LocationId.Value);
            }

            if (StartDate.HasValue)
            {
                query = query.Where(e => e.StartDateTime >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                var endOfDay = EndDate.Value.AddDays(1);
                query = query.Where(e => e.StartDateTime < endOfDay);
            }

            if (IndoorOnly)
            {
                query = query.Where(e => e.IsIndoor.HasValue && e.IsIndoor.Value);
            }

            if (ActiveOnly)
            {
                query = query.Where(e => e.IsActive);
            }

            Events = await query
                .OrderByDescending(e => e.StartDateTime)
                .ToListAsync();

            // Get exposure counts
            var exposureCounts = await _context.ExposureEvents
                .Where(ee => ee.EventId != null)
                .GroupBy(ee => ee.EventId!.Value)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToListAsync();

            ExposureCounts = exposureCounts.ToDictionary(x => x.EventId, x => x.Count);
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id)
        {
            var evt = await _context.Events
                .Include(e => e.ExposureEvents)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evt == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToPage();
            }

            // Check for dependencies
            if (evt.ExposureEvents.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete '{evt.Name}' because it has {evt.ExposureEvents.Count} exposure(s) associated with it.";
                return RedirectToPage();
            }

            try
            {
                _context.Events.Remove(evt);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{evt.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the event: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
