using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LookupTable LookupTable { get; set; } = null!;
        public List<LookupValue> Values { get; set; } = new();
        public int UsedByFieldsCount { get; set; }
        public List<string> UsedByFields { get; set; } = new();

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

            var usedByFields = await _context.CustomFieldDefinitions
                .Where(f => f.LookupTableId == id)
                .Select(f => f.Label)
                .ToListAsync();

            UsedByFieldsCount = usedByFields.Count;
            UsedByFields = usedByFields;

            return Page();
        }
    }
}
