using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public int LookupTableId { get; set; }
        public int UsedByFieldsCount { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Display Name")]
            public string DisplayName { get; set; } = string.Empty;

            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lookupTable = await _context.LookupTables.FindAsync(id);
            if (lookupTable == null)
            {
                return NotFound();
            }

            LookupTableId = lookupTable.Id;
            Input = new InputModel
            {
                DisplayName = lookupTable.DisplayName,
                Description = lookupTable.Description,
                IsActive = lookupTable.IsActive
            };

            UsedByFieldsCount = await _context.CustomFieldDefinitions
                .CountAsync(f => f.LookupTableId == id);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                LookupTableId = id.Value;
                UsedByFieldsCount = await _context.CustomFieldDefinitions
                    .CountAsync(f => f.LookupTableId == id);
                return Page();
            }

            var lookupTable = await _context.LookupTables.FindAsync(id);
            if (lookupTable == null)
            {
                return NotFound();
            }

            // Check if deactivating a table that's in use
            if (!Input.IsActive && lookupTable.IsActive)
            {
                var usedCount = await _context.CustomFieldDefinitions
                    .CountAsync(f => f.LookupTableId == id && f.IsActive);
                
                if (usedCount > 0)
                {
                    ModelState.AddModelError("Input.IsActive", 
                        $"Cannot deactivate: This lookup table is used by {usedCount} active custom field(s). Deactivate those fields first.");
                    LookupTableId = id.Value;
                    UsedByFieldsCount = usedCount;
                    return Page();
                }
            }

            lookupTable.DisplayName = Input.DisplayName;
            lookupTable.Description = Input.Description;
            lookupTable.IsActive = Input.IsActive;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lookup table '{lookupTable.DisplayName}' has been updated successfully.";
            return RedirectToPage("./Index");
        }
    }
}
