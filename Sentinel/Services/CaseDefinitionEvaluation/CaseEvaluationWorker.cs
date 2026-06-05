using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Background service that processes case evaluation jobs from the queue
    /// </summary>
    public class CaseEvaluationWorker : BackgroundService
    {
        private readonly ICaseEvaluationQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CaseEvaluationWorker> _logger;

        public CaseEvaluationWorker(
            ICaseEvaluationQueue queue,
            IServiceProvider serviceProvider,
            ILogger<CaseEvaluationWorker> logger)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Case Evaluation Worker started");

            // Wait a bit for app to fully start
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var job = await _queue.DequeueAsync(stoppingToken);

                    if (job == null)
                    {
                        // Queue was closed
                        break;
                    }

                    await ProcessJobAsync(job, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in case evaluation worker main loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }

            _logger.LogInformation("Case Evaluation Worker stopped");
        }

        private async Task ProcessJobAsync(CaseEvaluationJob job, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Processing evaluation for case {CaseId}. Reason: {Reason}. Queued: {QueuedAt}",
                job.CaseId, job.ChangeReason, job.QueuedAt);

            try
            {
                // Create a scope for scoped services
                using var scope = _serviceProvider.CreateScope();
                var evaluationService = scope.ServiceProvider.GetRequiredService<ICaseDefinitionEvaluationService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var reviewService = scope.ServiceProvider.GetRequiredService<IDataReviewService>();

                // Check if case has manual override
                var caseEntity = await context.Cases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == job.CaseId, cancellationToken);

                if (caseEntity == null)
                {
                    _logger.LogWarning("Case {CaseId} not found, skipping evaluation", job.CaseId);
                    return;
                }

                if (caseEntity.ConfirmationStatusManualOverride)
                {
                    _logger.LogInformation(
                        "Case {CaseId} has manual override, skipping auto-evaluation",
                        job.CaseId);
                    return;
                }

                // Evaluate all definitions for this case
                var results = await evaluationService.EvaluateAllDefinitionsForCaseAsync(job.CaseId);

                if (results == null || !results.Any())
                {
                    _logger.LogInformation("No definitions found for case {CaseId}", job.CaseId);
                    return;
                }

                // Process matching definitions
                var matchCount = 0;
                var appliedCount = 0;

                foreach (var result in results.Where(r => r.IsMatch))
                {
                    matchCount++;

                    // Get the case definition to check if auto-evaluation is enabled
                    var definition = await context.CaseDefinitions
                        .Include(d => d.ConfirmationStatus)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == result.CaseDefinitionId, cancellationToken);

                    if (definition == null || !definition.EnableAutoEvaluation)
                    {
                        _logger.LogInformation(
                            "Skipping case {CaseId} - definition {DefinitionId} has auto-evaluation disabled",
                            job.CaseId, result.CaseDefinitionId);
                        continue;
                    }

                    if (result.RecommendedAction == RecommendedAction.AutoClassify)
                    {
                        // Auto-apply classification
                        var applied = await evaluationService.ApplyClassificationAsync(
                            job.CaseId, 
                            result, 
                            "system");

                        if (applied)
                        {
                            appliedCount++;
                            _logger.LogInformation(
                                "Auto-applied classification for case {CaseId} using definition {DefinitionName}",
                                job.CaseId, result.CaseDefinitionName);
                        }
                    }
                    else if (result.RecommendedAction == RecommendedAction.SuggestClassification)
                    {
                        // Just record, don't apply
                        await evaluationService.RecordEvaluationAsync(job.CaseId, result);

                        // Create review queue item for suggested classification
                        var reviewId = await reviewService.QueueForReviewAsync(
                            entityType: "Case",
                            entityId: 0, // Not used for case entity type
                            diseaseId: caseEntity.DiseaseId,
                            caseId: job.CaseId,
                            patientId: caseEntity.PatientId,
                            changeType: "Suggested Classification",
                            triggerField: $"Definition: {result.CaseDefinitionName}",
                            changeSnapshot: new
                            {
                                DefinitionId = result.CaseDefinitionId,
                                DefinitionName = result.CaseDefinitionName,
                                SuggestedStatusId = definition.ConfirmationStatusId,
                                SuggestedStatusName = definition.ConfirmationStatus?.Name ?? "Unknown",
                                Reason = result.Rationale,
                                EvaluatedAt = result.EvaluationDate
                            },
                            autoCreateTask: false);

                        _logger.LogInformation(
                            "Created review queue item {ReviewId} for suggested classification on case {CaseId} using definition {DefinitionName}",
                            reviewId, job.CaseId, result.CaseDefinitionName);
                    }
                    else if (result.RecommendedAction == RecommendedAction.FlagForReview)
                    {
                        // Record and flag for review
                        await evaluationService.RecordEvaluationAsync(job.CaseId, result);

                        // Create review queue item with higher priority for flagged cases
                        var reviewId = await reviewService.QueueForReviewAsync(
                            entityType: "Case",
                            entityId: 0, // Not used for case entity type
                            diseaseId: caseEntity.DiseaseId,
                            caseId: job.CaseId,
                            patientId: caseEntity.PatientId,
                            changeType: "Flagged for Review",
                            triggerField: $"Definition: {result.CaseDefinitionName}",
                            changeSnapshot: new
                            {
                                DefinitionId = result.CaseDefinitionId,
                                DefinitionName = result.CaseDefinitionName,
                                FlaggedStatusId = definition.ConfirmationStatusId,
                                FlaggedStatusName = definition.ConfirmationStatus?.Name ?? "Unknown",
                                Reason = result.Rationale,
                                EvaluatedAt = result.EvaluationDate
                            },
                            autoCreateTask: true); // Auto-create task for flagged items

                        _logger.LogInformation(
                            "Created review queue item {ReviewId} with task for flagged case {CaseId} using definition {DefinitionName}",
                            reviewId, job.CaseId, result.CaseDefinitionName);
                    }
                }

                var duration = DateTime.UtcNow - startTime;

                _logger.LogInformation(
                    "Completed evaluation for case {CaseId}. {MatchCount} matches found, {AppliedCount} applied. Duration: {Duration}ms",
                    job.CaseId, matchCount, appliedCount, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to process evaluation for case {CaseId}. Reason: {Reason}",
                    job.CaseId, job.ChangeReason);
            }
        }
    }
}
