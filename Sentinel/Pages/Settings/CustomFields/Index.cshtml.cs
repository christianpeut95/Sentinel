using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.CustomFields
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();

        public async Task OnGetAsync()
        {
            var fields = await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();

            FieldsByCategory = fields.GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
    }
}
