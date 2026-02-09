using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Events
{
    [Authorize(Policy = "Permission.Event.Create")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Event Event { get; set; } = new Event { IsActive = true, IsIndoor = true };

        public SelectList EventTypesList { get; set; } = default!;
        public SelectList LocationsList { get; set; } = default!;
        public SelectList OrganizationsList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
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

            try
            {
                _context.Events.Add(Event);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Event '{Event.Name}' created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the event: {ex.Message}";
                await LoadSelectLists();
                return Page();
            }
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
