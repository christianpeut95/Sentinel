using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for managing HL7 message review workflow
    /// </summary>
    public class HL7ReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HL7ReviewService> _logger;

        public HL7ReviewService(
            ApplicationDbContext context,
            ILogger<HL7ReviewService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get messages requiring manual review or no-surveillance items, ordered by priority
        /// </summary>
        public async Task<List<HL7Message>> GetReviewQueueAsync(
            int pageNumber = 1,
            int pageSize = 20,
            string? facilityFilter = null,
            DateTime? dateFromFilter = null,
            DateTime? dateToFilter = null,
            HL7ReviewOutcome? outcomeFilter = null,
            string sortBy = "date-desc")
        {
            var query = _context.HL7Messages
                .Include(m => m.Patient)
                .Include(m => m.Case)
                    .ThenInclude(c => c!.Disease)
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                        .ThenInclude(marker => marker.Pathogen)
                .Include(m => m.Configuration)
                .Include(m => m.ManualReviewByUser)
                .Where(m => (m.RequiresManualReview && !m.ManualReviewCompleted) || 
                           (m.NoSurveillanceItem && !m.ManualReviewCompleted))
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(facilityFilter))
            {
                query = query.Where(m => m.SendingFacility == facilityFilter);
            }

            if (dateFromFilter.HasValue)
            {
                query = query.Where(m => m.ReceivedAt >= dateFromFilter.Value);
            }

            if (dateToFilter.HasValue)
            {
                var endOfDay = dateToFilter.Value.Date.AddDays(1);
                query = query.Where(m => m.ReceivedAt < endOfDay);
            }

            if (outcomeFilter.HasValue)
            {
                query = query.Where(m => m.ReviewOutcome == outcomeFilter.Value);
            }

            // Apply sorting
            query = sortBy.ToLower() switch
            {
                "date-asc" => query.OrderBy(m => m.ReceivedAt),
                "priority" => query
                    .OrderByDescending(m => m.Status == HL7ProcessingStatus.ProcessingFailed)
                    .ThenByDescending(m => m.ReceivedAt),
                _ => query.OrderByDescending(m => m.ReceivedAt) // date-desc is default
            };

            // Paginate
            return await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        /// <summary>
        /// Get count of messages awaiting review (including no-surveillance items)
        /// </summary>
        public async Task<int> GetPendingReviewCountAsync()
        {
            return await _context.HL7Messages
                .CountAsync(m => (m.RequiresManualReview && !m.ManualReviewCompleted) ||
                                 (m.NoSurveillanceItem && !m.ManualReviewCompleted));
        }

        /// <summary>
        /// Get message with full context for review
        /// </summary>
        public async Task<HL7Message?> GetMessageForReviewAsync(Guid messageId)
        {
            return await _context.HL7Messages
                .Include(m => m.Patient)
                .Include(m => m.Case)
                    .ThenInclude(c => c!.Disease)
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                        .ThenInclude(marker => marker.Pathogen)
                .Include(m => m.Configuration)
                .Include(m => m.Segments)
                .Include(m => m.ParsingIssues)
                .Include(m => m.ManualReviewByUser)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        /// <summary>
        /// Complete review of a message
        /// </summary>
        public async Task<bool> CompleteReviewAsync(
            Guid messageId,
            string userId,
            HL7ReviewOutcome outcome,
            string? notes = null)
        {
            var message = await _context.HL7Messages.FindAsync(messageId);
            if (message == null)
            {
                _logger.LogWarning("Cannot complete review: Message {MessageId} not found", messageId);
                return false;
            }

            message.ManualReviewCompleted = true;
            message.ManualReviewByUserId = userId;
            message.ManualReviewDate = DateTime.UtcNow;
            message.ManualReviewNotes = notes;
            message.ReviewOutcome = outcome;
            message.ModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Review completed for message {MessageId} by user {UserId} with outcome {Outcome}",
                    messageId, userId, outcome);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing review for message {MessageId}", messageId);
                return false;
            }
        }

        /// <summary>
        /// Reopen a completed review
        /// </summary>
        public async Task<bool> ReopenReviewAsync(Guid messageId, string userId)
        {
            var message = await _context.HL7Messages.FindAsync(messageId);
            if (message == null)
            {
                _logger.LogWarning("Cannot reopen review: Message {MessageId} not found", messageId);
                return false;
            }

            message.ManualReviewCompleted = false;
            message.ReviewOutcome = HL7ReviewOutcome.NotReviewed;
            message.ModifiedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Review reopened for message {MessageId} by user {UserId}",
                    messageId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reopening review for message {MessageId}", messageId);
                return false;
            }
        }

        /// <summary>
        /// Get review queue statistics
        /// </summary>
        public async Task<ReviewQueueStats> GetReviewQueueStatsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.HL7Messages.AsQueryable();

            if (fromDate.HasValue)
            {
                query = query.Where(m => m.ReceivedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1);
                query = query.Where(m => m.ReceivedAt < endOfDay);
            }

            var stats = new ReviewQueueStats
            {
                TotalPending = await query.CountAsync(m => (m.RequiresManualReview && !m.ManualReviewCompleted) || 
                                                           (m.NoSurveillanceItem && !m.ManualReviewCompleted)),
                TotalErrors = await query.CountAsync(m => m.Status == HL7ProcessingStatus.ProcessingFailed && m.RequiresManualReview && !m.ManualReviewCompleted),
                TotalCompleted = await query.CountAsync(m => (m.RequiresManualReview || m.NoSurveillanceItem) && m.ManualReviewCompleted),
                AvgReviewTimeMinutes = await CalculateAvgReviewTimeAsync(query)
            };

            // Breakdown by outcome
            var completedMessages = await query
                .Where(m => (m.RequiresManualReview || m.NoSurveillanceItem) && m.ManualReviewCompleted)
                .GroupBy(m => m.ReviewOutcome)
                .Select(g => new { Outcome = g.Key, Count = g.Count() })
                .ToListAsync();

            stats.OutcomeBreakdown = completedMessages.ToDictionary(x => x.Outcome, x => x.Count);

            return stats;
        }

        private async Task<double?> CalculateAvgReviewTimeAsync(IQueryable<HL7Message> query)
        {
            var reviewedMessages = await query
                .Where(m => (m.RequiresManualReview || m.NoSurveillanceItem) && m.ManualReviewCompleted && m.ManualReviewDate.HasValue)
                .Select(m => new { m.ReceivedAt, m.ManualReviewDate })
                .ToListAsync();

            if (!reviewedMessages.Any())
                return null;

            var avgMinutes = reviewedMessages
                .Select(m => (m.ManualReviewDate!.Value - m.ReceivedAt).TotalMinutes)
                .Average();

            return avgMinutes;
        }

        /// <summary>
        /// Diagnostic: Check if a specific message should appear in review queue
        /// </summary>
        public async Task<(bool ShouldAppear, string Reason)> DiagnoseMessageAsync(Guid messageId)
        {
            var message = await _context.HL7Messages.FindAsync(messageId);

            if (message == null)
                return (false, "Message not found in database");

            var reasons = new List<string>();

            reasons.Add($"RequiresManualReview: {message.RequiresManualReview}");
            reasons.Add($"ManualReviewCompleted: {message.ManualReviewCompleted}");
            reasons.Add($"NoSurveillanceItem: {message.NoSurveillanceItem}");
            reasons.Add($"Status: {message.Status}");
            reasons.Add($"ReviewOutcome: {message.ReviewOutcome}");

            bool shouldAppear = (message.RequiresManualReview && !message.ManualReviewCompleted) ||
                               (message.NoSurveillanceItem && !message.ManualReviewCompleted);

            reasons.Add($"\nShould appear in queue: {shouldAppear}");

            if (!shouldAppear)
            {
                if (message.ManualReviewCompleted)
                    reasons.Add("REASON: Manual review is marked as completed");
                else if (!message.RequiresManualReview && !message.NoSurveillanceItem)
                    reasons.Add("REASON: Neither RequiresManualReview nor NoSurveillanceItem is true");
            }

            return (shouldAppear, string.Join("\n", reasons));
        }
    }

    /// <summary>
    /// Statistics for the review queue
    /// </summary>
    public class ReviewQueueStats
    {
        public int TotalPending { get; set; }
        public int TotalErrors { get; set; }
        public int TotalCompleted { get; set; }
        public double? AvgReviewTimeMinutes { get; set; }
        public Dictionary<HL7ReviewOutcome, int> OutcomeBreakdown { get; set; } = new();
    }
}
