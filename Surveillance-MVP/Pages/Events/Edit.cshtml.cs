using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Events
{
    [Authorize(Policy = "Permission.Event.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Event Event { get; set; } = default!;

        public SelectList EventTypesList { get; set; } = default!;
        public SelectList LocationsList { get; set; } = default!;
        public SelectList OrganizationsList { get; set; } = default!;

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
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Validate end date is after start date
            if (Event.EndDateTime.HasValue && Event.EndDateTime.Value <= Event.StartDateTime)
            {
                ModelState.AddModelError("Event.EndDateTime", "End date/time must be after start date/time.");
                await LoadSelectLists();
                return Page();
            }

            _context.Attach(Event).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event '{Event.Name}' updated successfully.";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(Event.Id))
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
                TempData["ErrorMessage"] = $"An error occurred while updating the event: {ex.Message}";
                await LoadSelectLists();
                return Page();
            }
        }

        private bool EventExists(Guid id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private async Task LoadSelectLists()
        {
            EventTypesList = new SelectList(
                await _context.EventTypes.Where(et => et.IsActive).OrderBy(et => et.DisplayOrder).ToListAsync(),
                "Id", "Name");

            LocationsList = new SelectList(
                await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(),
                "Id", "Name");

            OrganizationsList = new SelectList(
                await _context.Organizations.Where(o => o.IsActive).OrderBy(o => o.Name).ToListAsync(),
                "Id", "Name");
        }
    }
}
