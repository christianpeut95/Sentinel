using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services.HL7;

public class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DuplicateDetectionService> _logger;

    public DuplicateDetectionService(
        ApplicationDbContext context,
        ILogger<DuplicateDetectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DuplicateDetectionResult> CheckForDuplicateAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default)
    {
        var result = new DuplicateDetectionResult();

        // Get configuration strategy
        var strategy = configuration?.DuplicateDetectionStrategy ?? DuplicateDetectionStrategy.Combined;

        if (strategy == DuplicateDetectionStrategy.Disabled)
        {
            return result; // Not a duplicate, no checking
        }

        var duplicateCheckWindowHours = configuration?.DuplicateDetectionWindowHours ?? 2160; // Default 90 days
        var cutoffDate = DateTime.UtcNow.AddHours(-duplicateCheckWindowHours);

        try
        {
            switch (strategy)
            {
                case DuplicateDetectionStrategy.MessageControlId:
                    return await CheckByMessageControlIdAsync(message, cutoffDate, cancellationToken);

                case DuplicateDetectionStrategy.AccessionAndLab:
                    return await CheckByAccessionAndContentAsync(message, cutoffDate, cancellationToken);

                case DuplicateDetectionStrategy.PatientSpecimenTest:
                    return await CheckByPatientSpecimenAsync(message, cutoffDate, cancellationToken);

                case DuplicateDetectionStrategy.Combined:
                    // Try strategies in order of reliability
                    result = await CheckByMessageControlIdAsync(message, cutoffDate, cancellationToken);
                    if (result.IsDuplicate)
                        return result;

                    result = await CheckByAccessionAndContentAsync(message, cutoffDate, cancellationToken);
                    if (result.IsDuplicate || result.IsRelatedResult)
                        return result;

                    result = await CheckByPatientSpecimenAsync(message, cutoffDate, cancellationToken);
                    return result;

                default:
                    return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for duplicate: {MessageControlId}", message.MessageControlId);
            result.RequiresManualReview = true;
            result.DifferenceDescription = $"Error during duplicate check: {ex.Message}";
            return result;
        }
    }

    public async Task<bool> HasSameContentAsync(
        HL7Message message1,
        HL7Message message2,
        CancellationToken cancellationToken = default)
    {
        var results1 = ExtractTestResults(message1);
        var results2 = ExtractTestResults(message2);

        // Must have same number of tests
        if (results1.Count != results2.Count)
            return false;

        // Check each test result
        foreach (var result1 in results1)
        {
            var match = results2.FirstOrDefault(r =>
                r.TestCode == result1.TestCode &&
                r.ResultValue == result1.ResultValue &&
                NormalizeResultStatus(r.ResultStatus) == NormalizeResultStatus(result1.ResultStatus));

            if (match == null)
                return false; // Different content
        }

        return true; // Same content
    }

    public async Task<List<HL7Message>> FindRelatedMessagesAsync(
        HL7Message message,
        CancellationToken cancellationToken = default)
    {
        var accessionNumber = ExtractAccessionNumber(message);
        if (string.IsNullOrEmpty(accessionNumber))
            return new List<HL7Message>();

        var relatedMessages = await _context.HL7Messages
            .Include(m => m.Segments)
            .Where(m => m.Id != message.Id &&
                        m.LaboratoryOrganizationId == message.LaboratoryOrganizationId &&
                        m.Status != HL7ProcessingStatus.ParsingFailed &&
                        !m.IsDeleted)
            .ToListAsync(cancellationToken);

        // Filter by accession number (must parse from raw message)
        var related = relatedMessages
            .Where(m => ExtractAccessionNumber(m) == accessionNumber)
            .OrderBy(m => m.ReceivedAt)
            .ToList();

        return related;
    }

    public List<TestResultSummary> ExtractTestResults(HL7Message message)
    {
        var results = new List<TestResultSummary>();

        var obxSegments = message.Segments
            .Where(s => s.SegmentType == "OBX")
            .OrderBy(s => s.SequenceNumber)
            .ToList();

        foreach (var segment in obxSegments)
        {
            var fields = segment.RawSegment.Split('|');
            if (fields.Length < 5)
                continue;

            var observationIdentifier = fields[3]; // OBX-3
            var observationValue = fields.Length > 5 ? fields[5] : ""; // OBX-5

            // Parse observation identifier (code^text^system)
            var identifierParts = observationIdentifier.Split('^');
            var testCode = identifierParts.Length > 0 ? identifierParts[0] : "";
            var testName = identifierParts.Length > 1 ? identifierParts[1] : "";

            results.Add(new TestResultSummary
            {
                TestCode = testCode,
                TestName = testName,
                ResultValue = observationValue,
                Units = fields.Length > 6 ? fields[6] : null, // OBX-6
                AbnormalFlag = fields.Length > 8 ? fields[8] : null, // OBX-8
                ResultStatus = fields.Length > 11 ? fields[11] : null, // OBX-11
                ObservationDateTime = ParseHL7DateTime(fields.Length > 14 ? fields[14] : null) // OBX-14
            });
        }

        return results;
    }

    #region Private Helper Methods

    private async Task<DuplicateDetectionResult> CheckByMessageControlIdAsync(
        HL7Message message,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var result = new DuplicateDetectionResult
        {
            DetectionMethod = "MessageControlId"
        };

        if (string.IsNullOrEmpty(message.MessageControlId))
        {
            return result; // Can't check without message control ID
        }

        var duplicate = await _context.HL7Messages
            .Where(m => m.MessageControlId == message.MessageControlId &&
                        m.SendingFacility == message.SendingFacility &&
                        m.Id != message.Id &&
                        m.ReceivedAt >= cutoffDate &&
                        !m.IsDeleted)
            .OrderByDescending(m => m.ReceivedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (duplicate != null)
        {
            result.IsDuplicate = true;
            result.OriginalMessageId = duplicate.Id;
            result.OriginalReceivedAt = duplicate.ReceivedAt;
            result.MatchCriteria.Add($"MessageControlId: {message.MessageControlId}");
            result.MatchCriteria.Add($"SendingFacility: {message.SendingFacility}");
        }

        return result;
    }

    private async Task<DuplicateDetectionResult> CheckByAccessionAndContentAsync(
        HL7Message message,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var result = new DuplicateDetectionResult
        {
            DetectionMethod = "AccessionAndLabAndContent"
        };

        var accessionNumber = ExtractAccessionNumber(message);
        if (string.IsNullOrEmpty(accessionNumber))
        {
            return result; // Can't check without accession number
        }

        result.AccessionNumber = accessionNumber;

        // Find messages with same lab and within time window
        var candidates = await _context.HL7Messages
            .Include(m => m.Segments)
            .Where(m => m.LaboratoryOrganizationId == message.LaboratoryOrganizationId &&
                        m.Id != message.Id &&
                        m.ReceivedAt >= cutoffDate &&
                        m.Status != HL7ProcessingStatus.ParsingFailed &&
                        !m.IsDeleted)
            .ToListAsync(cancellationToken);

        // Filter by accession number and compare content
        var messageResults = ExtractTestResults(message);

        foreach (var candidate in candidates)
        {
            var candidateAccession = ExtractAccessionNumber(candidate);
            if (candidateAccession != accessionNumber)
                continue;

            // Found message with same accession
            result.RelatedMessageIds.Add(candidate.Id);
            result.IsRelatedResult = true;

            // Check if content is identical
            var candidateResults = ExtractTestResults(candidate);

            if (AreTestResultsIdentical(messageResults, candidateResults))
            {
                // Exact duplicate
                result.IsDuplicate = true;
                result.OriginalMessageId = candidate.Id;
                result.OriginalReceivedAt = candidate.ReceivedAt;
                result.MatchCriteria.Add($"AccessionNumber: {accessionNumber}");
                result.MatchCriteria.Add("Identical test results");
                return result;
            }
            else
            {
                // Same accession, different content = follow-up result
                result.DifferenceDescription = DescribeTestDifferences(messageResults, candidateResults);
                result.MatchCriteria.Add($"AccessionNumber: {accessionNumber}");
                result.MatchCriteria.Add("Different test results - likely follow-up");
            }
        }

        return result;
    }

    private async Task<DuplicateDetectionResult> CheckByPatientSpecimenAsync(
        HL7Message message,
        DateTime cutoffDate,
        CancellationToken cancellationToken)
    {
        var result = new DuplicateDetectionResult
        {
            DetectionMethod = "PatientSpecimenTestAndContent"
        };

        if (message.PatientId == null)
        {
            return result; // Can't check without patient
        }

        var specimenDate = ExtractSpecimenDate(message);
        if (specimenDate == null)
        {
            result.RequiresManualReview = true;
            result.DifferenceDescription = "No specimen date found";
            return result;
        }

        var messageResults = ExtractTestResults(message);
        if (messageResults.Count == 0)
        {
            return result;
        }

        // Find messages for same patient around same specimen date
        var dateCutoff = specimenDate.Value.Date.AddDays(-1);
        var dateCutoffEnd = specimenDate.Value.Date.AddDays(2);

        var candidates = await _context.HL7Messages
            .Include(m => m.Segments)
            .Where(m => m.PatientId == message.PatientId &&
                        m.Id != message.Id &&
                        m.ReceivedAt >= cutoffDate &&
                        m.Status != HL7ProcessingStatus.ParsingFailed &&
                        !m.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var candidateSpecimenDate = ExtractSpecimenDate(candidate);
            if (candidateSpecimenDate == null)
                continue;

            // Check if specimen dates are within 1 day
            if (candidateSpecimenDate.Value < dateCutoff || candidateSpecimenDate.Value > dateCutoffEnd)
                continue;

            var candidateResults = ExtractTestResults(candidate);

            // Check for exact match on specific test codes
            foreach (var messageResult in messageResults)
            {
                var match = candidateResults.FirstOrDefault(r =>
                    r.TestCode == messageResult.TestCode &&
                    r.ResultValue == messageResult.ResultValue &&
                    NormalizeResultStatus(r.ResultStatus) == NormalizeResultStatus(messageResult.ResultStatus));

                if (match != null)
                {
                    // Found exact match for this test
                    result.IsDuplicate = true;
                    result.OriginalMessageId = candidate.Id;
                    result.OriginalReceivedAt = candidate.ReceivedAt;
                    result.MatchCriteria.Add($"PatientId: {message.PatientId}");
                    result.MatchCriteria.Add($"SpecimenDate: {specimenDate:yyyy-MM-dd}");
                    result.MatchCriteria.Add($"TestCode: {messageResult.TestCode}");
                    result.MatchCriteria.Add($"ResultValue: {messageResult.ResultValue}");
                    return result;
                }
            }
        }

        return result;
    }

    private bool AreTestResultsIdentical(List<TestResultSummary> results1, List<TestResultSummary> results2)
    {
        if (results1.Count != results2.Count)
            return false;

        foreach (var result1 in results1)
        {
            var match = results2.FirstOrDefault(r =>
                r.TestCode == result1.TestCode &&
                r.ResultValue == result1.ResultValue &&
                NormalizeResultStatus(r.ResultStatus) == NormalizeResultStatus(result1.ResultStatus));

            if (match == null)
                return false;
        }

        return true;
    }

    private string DescribeTestDifferences(List<TestResultSummary> results1, List<TestResultSummary> results2)
    {
        var differences = new List<string>();

        var codes1 = results1.Select(r => r.TestCode).ToHashSet();
        var codes2 = results2.Select(r => r.TestCode).ToHashSet();

        var newTests = codes1.Except(codes2).ToList();
        if (newTests.Any())
        {
            differences.Add($"New tests: {string.Join(", ", newTests)}");
        }

        var changedResults = new List<string>();
        foreach (var result1 in results1)
        {
            var result2 = results2.FirstOrDefault(r => r.TestCode == result1.TestCode);
            if (result2 != null && result1.ResultValue != result2.ResultValue)
            {
                changedResults.Add($"{result1.TestCode}: {result2.ResultValue} → {result1.ResultValue}");
            }
        }

        if (changedResults.Any())
        {
            differences.Add($"Changed results: {string.Join("; ", changedResults)}");
        }

        return differences.Any() ? string.Join(". ", differences) : "Different test content";
    }

    private string? ExtractAccessionNumber(HL7Message message)
    {
        var obrSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "OBR");
        if (obrSegment == null)
            return null;

        var fields = obrSegment.RawSegment.Split('|');
        if (fields.Length < 4)
            return null;

        // OBR-3 is Filler Order Number (Accession Number)
        var fillerOrderNumber = fields[3];
        var parts = fillerOrderNumber.Split('^');
        return parts.Length > 0 ? parts[0] : null;
    }

    private DateTime? ExtractSpecimenDate(HL7Message message)
    {
        var obrSegment = message.Segments.FirstOrDefault(s => s.SegmentType == "OBR");
        if (obrSegment == null)
            return null;

        var fields = obrSegment.RawSegment.Split('|');

        // Try OBR-7 (Observation Date/Time) or OBR-14 (Specimen Received Date/Time)
        var dateField = fields.Length > 7 ? fields[7] : null;
        if (string.IsNullOrEmpty(dateField) && fields.Length > 14)
        {
            dateField = fields[14];
        }

        return ParseHL7DateTime(dateField);
    }

    private string NormalizeResultStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "FINAL";

        status = status.ToUpperInvariant().Trim();

        // Treat preliminary and final as different, but normalize variations
        return status switch
        {
            "P" => "PRELIMINARY",
            "PRELIM" => "PRELIMINARY",
            "F" => "FINAL",
            "C" => "CORRECTED",
            "X" => "CANCELLED",
            _ => status
        };
    }

    private DateTime? ParseHL7DateTime(string? hl7DateTime)
    {
        if (string.IsNullOrWhiteSpace(hl7DateTime))
            return null;

        try
        {
            // HL7 datetime format: YYYYMMDDHHMMSS
            if (hl7DateTime.Length >= 8)
            {
                var year = int.Parse(hl7DateTime.Substring(0, 4));
                var month = int.Parse(hl7DateTime.Substring(4, 2));
                var day = int.Parse(hl7DateTime.Substring(6, 2));

                var hour = hl7DateTime.Length >= 10 ? int.Parse(hl7DateTime.Substring(8, 2)) : 0;
                var minute = hl7DateTime.Length >= 12 ? int.Parse(hl7DateTime.Substring(10, 2)) : 0;
                var second = hl7DateTime.Length >= 14 ? int.Parse(hl7DateTime.Substring(12, 2)) : 0;

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse HL7 datetime: {DateTime}", hl7DateTime);
        }

        return null;
    }

    #endregion
}
