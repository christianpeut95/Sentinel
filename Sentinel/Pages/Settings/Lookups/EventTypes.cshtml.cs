using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class EventTypesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EventTypesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<EventType> EventTypes { get; set; } = default!;
        public Dictionary<int, int> EventCounts { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? IsActive { get; set; }

        public async Task OnGetAsync()
        {
            var query = _context.EventTypes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                query = query.Where(e => e.Name.Contains(SearchTerm) || 
                                        (e.Description != null && e.Description.Contains(SearchTerm)));
            }

            if (IsActive.HasValue)
            {
                query = query.Where(e => e.IsActive == IsActive.Value);
            }

            EventTypes = await query
                .OrderBy(e => e.DisplayOrder)
                .ThenBy(e => e.Name)
                .ToListAsync();

            // Get usage counts
            var counts = await _context.Events
                .Where(e => e.EventTypeId != null)
                .GroupBy(e => e.EventTypeId!.Value)
                .Select(g => new { TypeId = g.Key, Count = g.Count() })
                .ToListAsync();

            EventCounts = counts.ToDictionary(x => x.TypeId, x => x.Count);
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            var eventType = await _context.EventTypes.FindAsync(id);

            if (eventType == null)
            {
                TempData["ErrorMessage"] = "Event type not found.";
                return RedirectToPage();
            }

            // Check if it's being used
            var usageCount = await _context.Events
                .CountAsync(e => e.EventTypeId == id);

            if (usageCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{eventType.Name}' because it is used by {usageCount} event(s). Deactivate it instead.";
                return RedirectToPage();
            }

            try
            {
                _context.EventTypes.Remove(eventType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event type '{eventType.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the event type: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
