using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.JurisdictionTypes
{
    [Authorize(Policy = "Permission.Settings.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<JurisdictionType> JurisdictionTypes { get; set; } = new();

        public async Task OnGetAsync()
        {
            JurisdictionTypes = await _context.JurisdictionTypes
                .OrderBy(jt => jt.FieldNumber)
                .ToListAsync();

            // Ensure all 5 field numbers exist
            for (int i = 1; i <= 5; i++)
            {
                if (!JurisdictionTypes.Any(jt => jt.FieldNumber == i))
                {
                    JurisdictionTypes.Add(new JurisdictionType
                    {
                        FieldNumber = i,
                        Name = $"Jurisdiction {i}",
                        IsActive = false,
                        DisplayOrder = i
                    });
                }
            }

            JurisdictionTypes = JurisdictionTypes.OrderBy(jt => jt.FieldNumber).ToList();
        }
    }
}
