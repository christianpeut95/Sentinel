using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Cases.Exposures
{
    [Authorize(Policy = "Permission.Exposure.Create")]
    public class NaturalEntryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NaturalEntryModel> _logger;

        public NaturalEntryModel(ApplicationDbContext context, ILogger<NaturalEntryModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        public Guid CaseId { get; set; }

        public string CaseFriendlyId { get; set; } = string.Empty;
        public string? PatientName { get; set; }
        public Guid? DiseaseId { get; set; }
        public string? DiseaseName { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (CaseId == Guid.Empty)
            {
                return NotFound();
            }

            // Load case details
            var caseData = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == CaseId);

            if (caseData == null)
            {
                return NotFound();
            }

            CaseFriendlyId = caseData.FriendlyId ?? CaseId.ToString();
            PatientName = $"{caseData.Patient?.FirstName} {caseData.Patient?.LastName}".Trim();
            DiseaseId = caseData.DiseaseId;
            DiseaseName = caseData.Disease?.Name;

            return Page();
        }
    }
}
