using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Cases
{
    [Authorize]
    public class ViewLabResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ViewLabResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public LabResult LabResult { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid labResultId)
        {
            LabResult = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.TestType)
                .Include(lr => lr.TestResult)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId);

            if (LabResult == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}
