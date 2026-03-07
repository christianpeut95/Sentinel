using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Settings.Lookups
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class SymptomsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SymptomsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Symptom> Symptoms { get; set; } = new List<Symptom>();

        public async Task OnGetAsync()
        {
            Symptoms = await _context.Symptoms
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }
    }
}
