using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public IndexModel(ApplicationDbContext context, IDiseaseAccessService diseaseAccessService)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
        }

        public IList<Case> Cases { get; set; } = default!;

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

            Cases = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .Where(c => c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value))
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
                .ToListAsync();
        }
    }
}
