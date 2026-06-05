using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.HL7.Messages
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private const int PAGE_SIZE = 50;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<HL7Message> Messages { get; set; } = new();
        public List<string> Facilities { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FacilityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFromFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateToFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ErrorCount { get; set; }
        public int ReviewCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasFilters => !string.IsNullOrWhiteSpace(StatusFilter) || 
                                   !string.IsNullOrWhiteSpace(FacilityFilter) ||
                                   DateFromFilter.HasValue ||
                                   DateToFilter.HasValue;

        public async Task<IActionResult> OnGetAsync()
        {
            // Load facilities for filter dropdown
            Facilities = await _context.HL7Messages
                .Select(m => m.SendingFacility)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();

            // Build query with filters
            var query = _context.HL7Messages
                .Include(m => m.Configuration)
                .Include(m => m.Case)
                .Include(m => m.Patient)
                .Include(m => m.Segments)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(StatusFilter) && Enum.TryParse<HL7ProcessingStatus>(StatusFilter, out var status))
            {
                query = query.Where(m => m.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(FacilityFilter))
            {
                query = query.Where(m => m.SendingFacility == FacilityFilter);
            }

            if (DateFromFilter.HasValue)
            {
                query = query.Where(m => m.ReceivedAt >= DateFromFilter.Value);
            }

            if (DateToFilter.HasValue)
            {
                var toDate = DateToFilter.Value.AddDays(1); // Include entire day
                query = query.Where(m => m.ReceivedAt < toDate);
            }

            // Get counts for stats
            TotalCount = await query.CountAsync();
            ProcessedCount = await query.CountAsync(m => m.Status == HL7ProcessingStatus.ProcessedSuccessfully);
            ErrorCount = await query.CountAsync(m => m.Status == HL7ProcessingStatus.ProcessingFailed);
            ReviewCount = await query.CountAsync(m => m.Status == HL7ProcessingStatus.AwaitingManualReview);

            // Calculate pagination
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PAGE_SIZE);
            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Get page of messages
            Messages = await query
                .OrderByDescending(m => m.ReceivedAt)
                .Skip((CurrentPage - 1) * PAGE_SIZE)
                .Take(PAGE_SIZE)
                .ToListAsync();

            return Page();
        }
    }
}
