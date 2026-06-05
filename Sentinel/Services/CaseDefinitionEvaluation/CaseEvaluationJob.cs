namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Represents a case evaluation job in the background queue
    /// </summary>
    public class CaseEvaluationJob
    {
        public Guid CaseId { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;
    }
}
