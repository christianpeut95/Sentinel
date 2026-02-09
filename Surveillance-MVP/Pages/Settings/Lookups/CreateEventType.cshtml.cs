using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.Create")]
    public class CreateEventTypeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateEventTypeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public EventType EventType { get; set; } = new EventType { IsActive = true };

        public IActionResult OnGet()
        {
            if (EventType == null)
            {
                EventType = new EventType { IsActive = true };
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // Check for duplicate name
            var exists = await _context.EventTypes
                .AnyAsync(e => e.Name == EventType.Name);

            if (exists)
            {
                ModelState.AddModelError("EventType.Name", "An event type with this name already exists.");
                TempData["ErrorMessage"] = "An event type with this name already exists.";
                return Page();
            }

            try
            {
                _context.EventTypes.Add(EventType);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event type '{EventType.Name}' created successfully.";
                return RedirectToPage("./EventTypes");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the event type: {ex.Message}";
                return Page();
            }
        }
    }
}
