namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Queue interface for background case evaluation
    /// </summary>
    public interface ICaseEvaluationQueue
    {
        /// <summary>
        /// Queue a case for evaluation in the background
        /// </summary>
        /// <param name="caseId">The case ID to evaluate</param>
        /// <param name="changeReason">Reason for evaluation (e.g., "Lab result added")</param>
        Task QueueEvaluationAsync(Guid caseId, string changeReason);

        /// <summary>
        /// Dequeue the next case for evaluation
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The next case evaluation job, or null if queue is empty</returns>
        ValueTask<CaseEvaluationJob?> DequeueAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Get the current queue count
        /// </summary>
        int Count { get; }
    }
}
