using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Pages.Settings.CaseStatuses
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public CaseStatus CaseStatus { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseStatus = await _context.CaseStatuses.FirstOrDefaultAsync(m => m.Id == id);
            if (caseStatus == null)
            {
                return NotFound();
            }
            else
            {
                CaseStatus = caseStatus;
            }
            return Page();
        }
    }
}
