using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Locations
{
    [Authorize(Policy = "Permission.Location.Delete")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Location Location { get; set; } = default!;
        public int EventCount { get; set; }
        public int ExposureCount { get; set; }
        public bool CanDelete { get; set; }

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

            // Check dependencies
            EventCount = await _context.Events.CountAsync(e => e.LocationId == id);
            ExposureCount = await _context.ExposureEvents.CountAsync(ee => ee.LocationId == id);
            CanDelete = EventCount == 0 && ExposureCount == 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            // Final check for dependencies
            var eventCount = await _context.Events.CountAsync(e => e.LocationId == id);
            var exposureCount = await _context.ExposureEvents.CountAsync(ee => ee.LocationId == id);

            if (eventCount > 0 || exposureCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{location.Name}'. It has {eventCount} event(s) and {exposureCount} exposure(s) associated with it.";
                return RedirectToPage("./Index");
            }

            try
            {
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Location '{location.Name}' deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while deleting the location: {ex.Message}";
            }

            return RedirectToPage("./Index");
        }
    }
}
