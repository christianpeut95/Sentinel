using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class EditEventTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditEventTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EventType EventType { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var eventType = await _context.EventTypes.FirstOrDefaultAsync(m => m.Id == id);
            if (eventType == null)
            {
                return NotFound();
            }
            EventType = eventType;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check for duplicate name (excluding current record)
            var exists = await _context.EventTypes
                .AnyAsync(e => e.Name == EventType.Name && e.Id != EventType.Id);

            if (exists)
            {
                ModelState.AddModelError("EventType.Name", "An event type with this name already exists.");
                return Page();
            }

            _context.Attach(EventType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Event type '{EventType.Name}' updated successfully.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventTypeExists(EventType.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./EventTypes");
        }

        private bool EventTypeExists(int id)
        {
            return _context.EventTypes.Any(e => e.Id == id);
        }
    }
}
