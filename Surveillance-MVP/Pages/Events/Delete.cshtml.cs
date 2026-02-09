using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Events
{
    [Authorize(Policy = "Permission.Event.Delete")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Event Event { get; set; } = default!;
        public int ExposureCount { get; set; }
        public bool CanDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evt = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evt == null)
            {
                return NotFound();
            }

            Event = evt;

            // Check dependencies
            ExposureCount = await _context.ExposureEvents.CountAsync(ee => ee.EventId == id);
            CanDelete = ExposureCount == 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evt = await _context.Events.FindAsync(id);

            if (evt == null)
            {
                return NotFound();
            }

            // Final check for dependencies
            var exposureCount = await _context.ExposureEvents.CountAsync(ee => ee.EventId == id);

            if (exposureCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{evt.Name}'. It has {exposureCount} exposure(s) associated with it.";
                return RedirectToPage("./Index");
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

            return RedirectToPage("./Index");
        }
    }
}
