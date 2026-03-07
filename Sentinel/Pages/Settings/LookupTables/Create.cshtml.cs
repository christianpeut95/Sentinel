using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Name must be lowercase letters, numbers, and underscores only")]
            [Display(Name = "Internal Name")]
            public string Name { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Display Name")]
            public string DisplayName { get; set; } = string.Empty;

            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; } = true;
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if name already exists
            if (await _context.LookupTables.AnyAsync(t => t.Name == Input.Name))
            {
                ModelState.AddModelError("Input.Name", "A lookup table with this name already exists");
                return Page();
            }

            var lookupTable = new LookupTable
            {
                Name = Input.Name,
                DisplayName = Input.DisplayName,
                Description = Input.Description,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.LookupTables.Add(lookupTable);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lookup table '{lookupTable.DisplayName}' has been created successfully.";
            return RedirectToPage("./ManageValues", new { id = lookupTable.Id });
        }
    }
}
