using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Models;
using Sentinel.Services.HL7;
using System.Security.Claims;

namespace Sentinel.Pages.Settings.HL7.Messages
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class ReviewQueueModel : PageModel
    {
        private readonly HL7ReviewService _reviewService;
        private readonly ILogger<ReviewQueueModel> _logger;
        private const int PAGE_SIZE = 20;

        public ReviewQueueModel(
            HL7ReviewService reviewService,
            ILogger<ReviewQueueModel> logger)
        {
            _reviewService = reviewService;
            _logger = logger;
        }

        public List<HL7Message> Messages { get; set; } = new();
        public ReviewQueueStats? Stats { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FacilityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateFromFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateToFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public HL7ReviewOutcome? OutcomeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "date-desc"; // date-desc, date-asc, priority

        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;

        public int TotalCount { get; set; }
        public int TotalPages { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Get messages for review queue
                Messages = await _reviewService.GetReviewQueueAsync(
                    CurrentPage,
                    PAGE_SIZE,
                    FacilityFilter,
                    DateFromFilter,
                    DateToFilter,
                    OutcomeFilter,
                    SortBy);

                // Get counts
                TotalCount = await _reviewService.GetPendingReviewCountAsync();

                // Get stats
                Stats = await _reviewService.GetReviewQueueStatsAsync(DateFromFilter, DateToFilter);

                // Calculate pagination
                TotalPages = (int)Math.Ceiling(TotalCount / (double)PAGE_SIZE);
                if (CurrentPage < 1) CurrentPage = 1;
                if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading review queue");
                TempData["Error"] = "Error loading review queue. Please try again.";
                return Page();
            }
        }

        public async Task<IActionResult> OnPostCompleteReviewAsync(
            Guid messageId,
            string notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(notes))
            {
                TempData["Error"] = "Review notes are required.";
                return RedirectToPage();
            }

            // Default to "Reviewed" outcome - notes capture the actual details
            var success = await _reviewService.CompleteReviewAsync(
                messageId, 
                userId, 
                HL7ReviewOutcome.Reviewed, 
                notes);

            if (success)
            {
                TempData["Success"] = "Review completed successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to complete review.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReopenReviewAsync(Guid messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _reviewService.ReopenReviewAsync(messageId, userId);

            if (success)
            {
                TempData["Success"] = "Review reopened successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to reopen review.";
            }

            return RedirectToPage();
        }

        // Diagnostic handler to check specific message
        public async Task<IActionResult> OnGetDiagnoseAsync(Guid messageId)
        {
            var (shouldAppear, reason) = await _reviewService.DiagnoseMessageAsync(messageId);

            _logger.LogInformation("Diagnostic for message {MessageId}: {Reason}", messageId, reason);

            TempData["Success"] = $"Diagnostic for message {messageId}:\n\n{reason}";
            return RedirectToPage();
        }
    }
}
