using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class ManageValuesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ManageValuesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LookupTable LookupTable { get; set; } = null!;
        public List<LookupValue> Values { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [Display(Name = "Internal Value")]
            [RegularExpression(@"^[a-z0-9_]+$", ErrorMessage = "Value must be lowercase letters, numbers, and underscores only")]
            public string Value { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Display Text")]
            public string DisplayText { get; set; } = string.Empty;

            [Required]
            [Range(0, 10000)]
            [Display(Name = "Display Order")]
            public int DisplayOrder { get; set; } = 0;

            [Display(Name = "Active")]
            public bool IsActive { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lookupTable = await _context.LookupTables
                .Include(l => l.Values.OrderBy(v => v.DisplayOrder))
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lookupTable == null)
            {
                return NotFound();
            }

            LookupTable = lookupTable;
            Values = lookupTable.Values.OrderBy(v => v.DisplayOrder).ToList();

            // Set default display order for new values
            Input.DisplayOrder = Values.Any() ? Values.Max(v => v.DisplayOrder) + 10 : 10;

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lookupTable = await _context.LookupTables
                .Include(l => l.Values)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lookupTable == null)
            {
                return NotFound();
            }

            LookupTable = lookupTable;
            Values = lookupTable.Values.OrderBy(v => v.DisplayOrder).ToList();

            // Validate only the Input properties
            if (string.IsNullOrWhiteSpace(Input.Value) || string.IsNullOrWhiteSpace(Input.DisplayText))
            {
                ModelState.AddModelError("", "Value and Display Text are required.");
                return Page();
            }

            // Check if value already exists in this table
            if (lookupTable.Values.Any(v => v.Value == Input.Value))
            {
                ModelState.AddModelError("Input.Value", "This value already exists in this lookup table.");
                return Page();
            }

            var newValue = new LookupValue
            {
                LookupTableId = lookupTable.Id,
                Value = Input.Value,
                DisplayText = Input.DisplayText,
                DisplayOrder = Input.DisplayOrder,
                IsActive = Input.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.LookupValues.Add(newValue);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Value '{newValue.DisplayText}' has been added.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int? id, int? valueId)
        {
            if (id == null || valueId == null)
            {
                return NotFound();
            }

            var value = await _context.LookupValues
                .Include(v => v.LookupTable)
                .FirstOrDefaultAsync(v => v.Id == valueId && v.LookupTableId == id);

            if (value == null)
            {
                return NotFound();
            }

            // Check if this value is used in any patient data
            var usedInPatients = await _context.PatientCustomFieldLookups
                .AnyAsync(p => p.LookupValueId == valueId);

            if (usedInPatients)
            {
                TempData["ErrorMessage"] = $"Cannot delete '{value.DisplayText}': This value is currently used in patient records.";
                return RedirectToPage(new { id });
            }

            var displayText = value.DisplayText;
            _context.LookupValues.Remove(value);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Value '{displayText}' has been deleted.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostToggleActiveAsync(int? id, int? valueId)
        {
            if (id == null || valueId == null)
            {
                return NotFound();
            }

            var value = await _context.LookupValues.FindAsync(valueId);
            if (value == null || value.LookupTableId != id)
            {
                return NotFound();
            }

            value.IsActive = !value.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Value '{value.DisplayText}' has been {(value.IsActive ? "activated" : "deactivated")}.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostReorderAsync(int? id, int? valueId, string direction)
        {
            if (id == null || valueId == null || string.IsNullOrEmpty(direction))
            {
                return NotFound();
            }

            var value = await _context.LookupValues.FindAsync(valueId);
            if (value == null || value.LookupTableId != id)
            {
                return NotFound();
            }

            var allValues = await _context.LookupValues
                .Where(v => v.LookupTableId == id)
                .OrderBy(v => v.DisplayOrder)
                .ToListAsync();

            var currentIndex = allValues.IndexOf(value);
            if (currentIndex == -1) return RedirectToPage(new { id });

            if (direction == "up" && currentIndex > 0)
            {
                var swapValue = allValues[currentIndex - 1];
                var tempOrder = value.DisplayOrder;
                value.DisplayOrder = swapValue.DisplayOrder;
                swapValue.DisplayOrder = tempOrder;
            }
            else if (direction == "down" && currentIndex < allValues.Count - 1)
            {
                var swapValue = allValues[currentIndex + 1];
                var tempOrder = value.DisplayOrder;
                value.DisplayOrder = swapValue.DisplayOrder;
                swapValue.DisplayOrder = tempOrder;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage(new { id });
        }
    }
}
