using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class DeleteModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeleteModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LookupTable LookupTable { get; set; } = null!;
        public int ValuesCount { get; set; }
        public int UsedByFieldsCount { get; set; }
        public List<string> UsedByFields { get; set; } = new();
        public bool CanDelete { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
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
            ValuesCount = lookupTable.Values.Count;

            var usedByFields = await _context.CustomFieldDefinitions
                .Where(f => f.LookupTableId == id)
                .Select(f => f.Label)
                .ToListAsync();

            UsedByFieldsCount = usedByFields.Count;
            UsedByFields = usedByFields;
            CanDelete = UsedByFieldsCount == 0;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            // Check if still in use
            var usedByCount = await _context.CustomFieldDefinitions
                .CountAsync(f => f.LookupTableId == id);

            if (usedByCount > 0)
            {
                TempData["ErrorMessage"] = $"Cannot delete: This lookup table is used by {usedByCount} custom field(s). Remove those references first.";
                return RedirectToPage("./Index");
            }

            var displayName = lookupTable.DisplayName;
            _context.LookupTables.Remove(lookupTable);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lookup table '{displayName}' has been deleted successfully.";
            return RedirectToPage("./Index");
        }
    }
}
