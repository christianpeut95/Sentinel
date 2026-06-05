using System.Threading.Channels;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// In-memory queue for case evaluation jobs using System.Threading.Channels
    /// </summary>
    public class CaseEvaluationQueue : ICaseEvaluationQueue
    {
        private readonly Channel<CaseEvaluationJob> _queue;
        private readonly ILogger<CaseEvaluationQueue> _logger;
        private int _count;

        public CaseEvaluationQueue(ILogger<CaseEvaluationQueue> logger)
        {
            _logger = logger;

            // Unbounded channel - no limit on queue size
            // For production, consider BoundedChannel with capacity limit
            _queue = Channel.CreateUnbounded<CaseEvaluationJob>(new UnboundedChannelOptions
            {
                SingleReader = true, // Only one worker will read
                SingleWriter = false // Multiple threads may write
            });
        }

        public int Count => _count;

        public async Task QueueEvaluationAsync(Guid caseId, string changeReason)
        {
            var job = new CaseEvaluationJob
            {
                CaseId = caseId,
                ChangeReason = changeReason,
                QueuedAt = DateTime.UtcNow
            };

            await _queue.Writer.WriteAsync(job);
            Interlocked.Increment(ref _count);

            _logger.LogInformation(
                "Queued case {CaseId} for evaluation. Reason: {Reason}. Queue size: {QueueSize}",
                caseId, changeReason, _count);
        }

        public async ValueTask<CaseEvaluationJob?> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                var job = await _queue.Reader.ReadAsync(cancellationToken);
                Interlocked.Decrement(ref _count);
                return job;
            }
            catch (ChannelClosedException)
            {
                _logger.LogWarning("Evaluation queue channel was closed");
                return null;
            }
        }
    }
}
