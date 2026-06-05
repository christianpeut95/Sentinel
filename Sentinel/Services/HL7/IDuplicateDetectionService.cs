using Sentinel.Models;

namespace Sentinel.Services.HL7;

public interface IDuplicateDetectionService
{
    /// <summary>
    /// Check if an HL7 message is a duplicate based on content comparison
    /// </summary>
    Task<DuplicateDetectionResult> CheckForDuplicateAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if two messages have identical test result content
    /// </summary>
    Task<bool> HasSameContentAsync(
        HL7Message message1,
        HL7Message message2,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find related messages (same accession/patient/specimen) that are NOT duplicates
    /// Used to identify follow-up results that should be added to same LabResult
    /// </summary>
    Task<List<HL7Message>> FindRelatedMessagesAsync(
        HL7Message message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract test results from message segments for comparison
    /// </summary>
    List<TestResultSummary> ExtractTestResults(HL7Message message);
}

/// <summary>
/// Simplified test result data for duplicate detection
/// </summary>
public class TestResultSummary
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string ResultValue { get; set; } = string.Empty;
    public string? Units { get; set; }
    public string? AbnormalFlag { get; set; }
    public string? ResultStatus { get; set; }
    public DateTime? ObservationDateTime { get; set; }
}

public class DuplicateDetectionResult
{
    public bool IsDuplicate { get; set; }
    public bool IsRelatedResult { get; set; } // Same specimen/accession, different content
    public bool RequiresManualReview { get; set; } // Uncertain cases
    public Guid? OriginalMessageId { get; set; }
    public List<Guid> RelatedMessageIds { get; set; } = new(); // Follow-up results on same accession
    public string? DetectionMethod { get; set; }
    public string? DifferenceDescription { get; set; } // Why it's NOT a duplicate
    public List<string> MatchCriteria { get; set; } = new();
    public string? AccessionNumber { get; set; }
    public DateTime? OriginalReceivedAt { get; set; }
}
