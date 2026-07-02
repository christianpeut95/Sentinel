using Sentinel.HL7Generator.Models;
using Sentinel.Models;

namespace Sentinel.Services.HL7;

public interface IHL7TestMessageService
{
    /// <summary>
    /// Generate an HL7 message and save to file
    /// </summary>
    Task<GenerateMessageResult> GenerateAndSaveMessageAsync(
        HL7MessageRequest request,
        string outputPath,
        string? testComment = null,
        bool autoProcess = false,
        Guid? templateId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate multiple messages with variations
    /// </summary>
    Task<BatchGenerateResult> GenerateMultipleMessagesAsync(
        HL7MessageRequest baseRequest,
        string outputPath,
        int count,
        bool varyPatient,
        bool varyProvider,
        bool varyAccession,
        string? testComment = null,
        bool autoProcess = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save a configuration as a reusable template
    /// </summary>
    Task<HL7TestMessageTemplate> SaveTemplateAsync(
        string templateName,
        HL7MessageRequest request,
        string? description = null,
        string? testComment = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a saved template
    /// </summary>
    Task<HL7MessageRequest?> LoadTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all saved templates
    /// </summary>
    Task<List<HL7TestMessageTemplate>> GetTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a template
    /// </summary>
    Task DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recent message history
    /// </summary>
    Task<List<HL7TestMessageHistory>> GetRecentHistoryAsync(
        int count = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get processing result for a generated message
    /// </summary>
    Task<TestMessageProcessingResult?> GetProcessingResultAsync(
        Guid historyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerate a message with a new patient (keeping all other settings)
    /// </summary>
    Task<GenerateMessageResult> RegenerateWithNewPatientAsync(
        Guid historyId,
        string outputPath,
        bool autoProcess = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clone a message exactly as it was
    /// </summary>
    Task<GenerateMessageResult> CloneMessageAsync(
        Guid historyId,
        string outputPath,
        bool autoProcess = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get random patient data
    /// </summary>
    PatientInfo GenerateRandomPatient();

    /// <summary>
    /// Get random provider data
    /// </summary>
    ProviderInfo GenerateRandomProvider();

    /// <summary>
    /// Generate unique accession number
    /// </summary>
    string GenerateAccessionNumber();

    /// <summary>
    /// Get patients from database for selection
    /// </summary>
    Task<List<PatientSelectionItem>> GetPatientsForSelectionAsync(
        string? searchTerm = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get providers from database for selection
    /// </summary>
    Task<List<ProviderSelectionItem>> GetProvidersForSelectionAsync(
        string? searchTerm = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get HL7 configurations for output path selection
    /// </summary>
    Task<List<HL7Configuration>> GetActiveConfigurationsAsync(CancellationToken cancellationToken = default);
}

public class GenerateMessageResult
{
    public bool Success { get; set; }
    public Guid HistoryId { get; set; }
    public string? FilePath { get; set; }
    public string? AccessionNumber { get; set; }
    public string? PatientMRN { get; set; }
    public string? MessageControlId { get; set; }
    public string? ErrorMessage { get; set; }
    public string RawHL7 { get; set; } = string.Empty;
}

public class BatchGenerateResult
{
    public bool Success { get; set; }
    public int TotalGenerated { get; set; }
    public int TotalFailed { get; set; }
    public List<GenerateMessageResult> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class TestMessageProcessingResult
{
    public Guid HistoryId { get; set; }
    public Guid? HL7MessageId { get; set; }
    public HL7ProcessingStatus? Status { get; set; }
    public Guid? PatientId { get; set; }
    public string? PatientName { get; set; }
    public Guid? ProviderId { get; set; }
    public string? ProviderName { get; set; }
    public Guid? LabResultId { get; set; }
    public List<BiomarkerMapping> BiomarkersMapped { get; set; } = new();
    public List<CaseCreation> CasesCreated { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime? ProcessedAt { get; set; }
}

public class BiomarkerMapping
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public Guid? PathogenId { get; set; }
    public string? PathogenName { get; set; }
    public Guid? DiseaseId { get; set; }
    public string? DiseaseName { get; set; }
    public bool WasMapped { get; set; }
}

public class CaseCreation
{
    public Guid CaseId { get; set; }
    public string CaseFriendlyId { get; set; } = string.Empty;
    public string DiseaseName { get; set; } = string.Empty;
    public bool WasAutoCreated { get; set; }
    public string? CaseDefinitionApplied { get; set; }
}

public class PatientSelectionItem
{
    public Guid Id { get; set; }
    public string MRN { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
}

public class ProviderSelectionItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NPI { get; set; }
    public string OrganizationType { get; set; } = string.Empty;
}
