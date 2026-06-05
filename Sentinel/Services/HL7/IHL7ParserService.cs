using Sentinel.Models;

namespace Sentinel.Services.HL7;

public interface IHL7ParserService
{
    /// <summary>
    /// Parse an HL7 message from raw text and save to database
    /// </summary>
    Task<HL7Message> ParseMessageAsync(string rawMessage, Guid? configurationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse an HL7 message without saving to database (for validation/testing)
    /// </summary>
    Task<HL7ParseResult> ParseMessagePreviewAsync(string rawMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an HL7 message structure
    /// </summary>
    Task<HL7ValidationResult> ValidateMessageAsync(string rawMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extract a specific segment from parsed message
    /// </summary>
    string? GetSegmentValue(HL7Message message, string segmentPath);
}

public class HL7ParseResult
{
    public bool IsValid { get; set; }
    public string? MessageType { get; set; }
    public string? MessageControlId { get; set; }
    public string? SendingFacility { get; set; }
    public string? SendingApplication { get; set; }
    public DateTime? MessageDateTime { get; set; }
    public string? HL7Version { get; set; }
    public Dictionary<string, string> PatientData { get; set; } = new();
    public Dictionary<string, string> OrderData { get; set; } = new();
    public Dictionary<string, string> SpecimenData { get; set; } = new();
    public List<Dictionary<string, string>> ResultData { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class HL7ValidationResult
{
    public bool IsValid { get; set; }
    public string? MessageType { get; set; }
    public string? HL7Version { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
