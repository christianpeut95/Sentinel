using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Surveys
{
    [Authorize]
    public class SubmissionLogModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SubmissionLogModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Filters ──

        [BindProperty(SupportsGet = true)]
        public string DateRange { get; set; } = "7days";

        [BindProperty(SupportsGet = true)]
        public string? OutcomeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? DiseaseFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        // ── Results ──

        public List<SurveySubmissionLog> Submissions { get; set; } = new();
        public SelectList DiseaseOptions { get; set; } = new SelectList(Enumerable.Empty<object>());

        // ── Summary stats ──

        public int TotalCount { get; set; }
        public int CompletedCount { get; set; }
        public int NeedsAttentionCount { get; set; }
        public int ProblemsCount { get; set; }
        public int NotConfiguredCount { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var cutoff = DateRange switch
            {
                "today"  => DateTime.UtcNow.Date,
                "30days" => DateTime.UtcNow.AddDays(-30),
                "90days" => DateTime.UtcNow.AddDays(-90),
                "all"    => DateTime.MinValue,
                _        => DateTime.UtcNow.AddDays(-7)  // default: 7 days
            };

            var query = _context.SurveySubmissionLogs
                .Include(l => l.Case)
                .AsNoTracking()
                .Where(l => l.SubmittedAt >= cutoff);

            if (!string.IsNullOrWhiteSpace(OutcomeFilter) &&
                Enum.TryParse<SurveySubmissionOutcome>(OutcomeFilter, out var outcomeEnum))
            {
                query = query.Where(l => l.Outcome == outcomeEnum);
            }
            else if (OutcomeFilter == "issues")
            {
                query = query.Where(l =>
                    l.Outcome == SurveySubmissionOutcome.ProblemOccurred ||
                    l.Outcome == SurveySubmissionOutcome.PartiallyCompleted);
            }
            else if (OutcomeFilter == "review")
            {
                query = query.Where(l => l.Outcome == SurveySubmissionOutcome.SentForReview);
            }

            if (DiseaseFilter.HasValue)
            {
                var diseaseName = await _context.Diseases
                    .Where(d => d.Id == DiseaseFilter.Value)
                    .Select(d => d.Name)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrEmpty(diseaseName))
                    query = query.Where(l => l.DiseaseName == diseaseName);
            }

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var term = SearchTerm.Trim();
                query = query.Where(l =>
                    (l.PatientName != null && l.PatientName.Contains(term)) ||
                    (l.DiseaseName != null && l.DiseaseName.Contains(term)) ||
                    (l.SurveyName != null && l.SurveyName.Contains(term)) ||
                    (l.CaseReference != null && l.CaseReference.Contains(term)));
            }

            Submissions = await query
                .OrderByDescending(l => l.SubmittedAt)
                .Take(500)
                .ToListAsync();

            // Summary stats over the filtered period
            var allInPeriod = await _context.SurveySubmissionLogs
                .AsNoTracking()
                .Where(l => l.SubmittedAt >= cutoff)
                .GroupBy(l => l.Outcome)
                .Select(g => new { Outcome = g.Key, Count = g.Count() })
                .ToListAsync();

            TotalCount        = allInPeriod.Sum(g => g.Count);
            CompletedCount    = allInPeriod.Where(g => g.Outcome == SurveySubmissionOutcome.Completed).Sum(g => g.Count);
            NeedsAttentionCount = allInPeriod.Where(g => g.Outcome == SurveySubmissionOutcome.SentForReview).Sum(g => g.Count);
            ProblemsCount     = allInPeriod.Where(g =>
                g.Outcome == SurveySubmissionOutcome.ProblemOccurred ||
                g.Outcome == SurveySubmissionOutcome.PartiallyCompleted).Sum(g => g.Count);
            NotConfiguredCount = allInPeriod.Where(g => g.Outcome == SurveySubmissionOutcome.NotConfigured).Sum(g => g.Count);

            // Disease filter dropdown — only diseases that appear in the log
            var diseaseNames = await _context.SurveySubmissionLogs
                .AsNoTracking()
                .Where(l => l.SubmittedAt >= cutoff && l.DiseaseName != null)
                .Select(l => l.DiseaseName!)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();

            var diseaseList = await _context.Diseases
                .AsNoTracking()
                .Where(d => diseaseNames.Contains(d.Name))
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            DiseaseOptions = new SelectList(diseaseList, "Id", "Name", DiseaseFilter);

            return Page();
        }

        /// <summary>
        /// Helper to deserialize the mapping detail JSON for display in the details modal.
        /// Returns null if JSON is missing or invalid.
        /// </summary>
        public static MappingDetailSnapshot? GetMappingDetail(SurveySubmissionLog log)
        {
            if (string.IsNullOrEmpty(log.MappingDetailJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<MappingDetailSnapshot>(log.MappingDetailJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
