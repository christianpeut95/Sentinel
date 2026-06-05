using Sentinel.Models;

namespace Sentinel.Services.HL7;

public interface IHL7DataExtractionService
{
    /// <summary>
    /// Process parsed HL7 message and extract/create all entities (LEGACY direct path)
    /// Returns the created/updated entities or errors
    /// </summary>
    Task<DataExtractionResult> ExtractAndCreateEntitiesAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process parsed HL7 message using staging workflow (NEW recommended path)
    /// Stages all entities first, then commits atomically with duplicate detection and case matching
    /// </summary>
    Task<DataExtractionResult> ExtractAndCreateEntitiesWithStagingAsync(
        HL7Message message,
        HL7Configuration? configuration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find or create patient from HL7 PID segment
    /// Uses configured matching strategy
    /// </summary>
    Task<PatientMatchResult> FindOrCreatePatientAsync(
        HL7Message message,
        PatientMatchingStrategy strategy,
        bool autoCreate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Find or create organization (lab or provider) from HL7 data
    /// </summary>
    Task<Organization?> FindOrCreateOrganizationAsync(
        string organizationName,
        string organizationTypeName,
        bool autoCreate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update LabResult with markers from HL7 OBX segments
    /// If accession exists, append new markers; if not, create new LabResult
    /// Returns null if message is a duplicate
    /// </summary>
    Task<LabResult?> CreateOrUpdateLabResultAsync(
        HL7Message message,
        Patient patient,
        Organization laboratory,
        Organization? orderingProvider,
        CancellationToken cancellationToken = default);
}

public class DataExtractionResult
{
    public bool Success { get; set; }
    public Patient? Patient { get; set; }
    public Organization? Laboratory { get; set; }
    public Organization? OrderingProvider { get; set; }
    public LabResult? LabResult { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public bool RequiresManualReview { get; set; }
    public string? ManualReviewReason { get; set; }
}

public class PatientMatchResult
{
    public Patient? Patient { get; set; }
    public bool IsNewPatient { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? MatchMethod { get; set; }
    public string? ConflictReason { get; set; }
    public List<Patient> ConflictingPatients { get; set; } = new();
}
