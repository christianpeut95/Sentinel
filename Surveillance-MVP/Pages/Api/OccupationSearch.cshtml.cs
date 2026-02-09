using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;

namespace Surveillance_MVP.Pages.Api
{
    public class OccupationSearchModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public OccupationSearchModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnGetAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return new JsonResult(new object[] { });
            }

            var occupations = await _context.Occupations
                .Where(o => o.IsActive)
                .Where(o => o.Name.Contains(term) || o.Code.Contains(term))
                .OrderBy(o => o.Name)
                .Take(20)
                .Select(o => new
                {
                    id = o.Id,
                    label = o.Name,
                    value = o.Name,
                    code = o.Code,
                    majorGroup = o.MajorGroupName,
                    minorGroup = o.MinorGroupName
                })
                .ToListAsync();

            return new JsonResult(occupations);
        }
    }
}
