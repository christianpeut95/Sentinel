using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Events
{
    [Authorize(Policy = "Permission.Event.View")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DetailsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public Event Event { get; set; } = default!;
        public IList<ExposureEvent> Exposures { get; set; } = default!;
        public Dictionary<Guid, string> CaseIdentifiers { get; set; } = new();
        public int UniqueCaseCount { get; set; }
        public double? AttackRate { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var evt = await _context.Events
                .Include(e => e.EventType)
                .Include(e => e.Location)
                    .ThenInclude(l => l.LocationType)
                .Include(e => e.OrganizerOrganization)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (evt == null)
            {
                return NotFound();
            }

            Event = evt;

            // Load exposures for this event
            Exposures = await _context.ExposureEvents
                .Include(ee => ee.Case)
                    .ThenInclude(c => c.Patient)
                .Include(ee => ee.Case)
                    .ThenInclude(c => c.Disease)
                .Where(ee => ee.EventId == id)
                .OrderBy(ee => ee.ExposureStartDate)
                .ToListAsync();

            // Get case identifiers
            var caseIds = Exposures.Select(e => e.CaseId).Distinct().ToList();
            var cases = await _context.Cases
                .Where(c => caseIds.Contains(c.Id))
                .Select(c => new { c.Id, c.FriendlyId })
                .ToListAsync();

            CaseIdentifiers = cases.ToDictionary(c => c.Id, c => c.FriendlyId);
            UniqueCaseCount = caseIds.Count;

            // Calculate attack rate
            if (Event.EstimatedAttendees.HasValue && Event.EstimatedAttendees > 0)
            {
                AttackRate = (UniqueCaseCount / (double)Event.EstimatedAttendees.Value) * 100;
            }

            return Page();
        }
    }
}
