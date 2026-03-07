using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.LookupTables
{
    [Authorize(Policy = "Permission.Settings.ManageCustomLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<LookupTableViewModel> LookupTables { get; set; } = new();

        public class LookupTableViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public int ValueCount { get; set; }
            public int UsedByFieldsCount { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public async Task OnGetAsync()
        {
            var tables = await _context.LookupTables
                .Include(l => l.Values)
                .OrderBy(l => l.DisplayName)
                .ToListAsync();

            foreach (var table in tables)
            {
                var usedByFields = await _context.CustomFieldDefinitions
                    .CountAsync(f => f.LookupTableId == table.Id);

                LookupTables.Add(new LookupTableViewModel
                {
                    Id = table.Id,
                    Name = table.Name,
                    DisplayName = table.DisplayName,
                    Description = table.Description,
                    IsActive = table.IsActive,
                    ValueCount = table.Values.Count,
                    UsedByFieldsCount = usedByFields,
                    CreatedAt = table.CreatedAt
                });
            }
        }
    }
}
