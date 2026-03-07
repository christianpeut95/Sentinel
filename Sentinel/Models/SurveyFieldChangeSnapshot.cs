using System.Text.Json.Serialization;

namespace Sentinel.Models;

/// <summary>
/// Represents the snapshot data stored in ReviewQueue.ChangeSnapshot
/// for SurveyFieldChange entity type
/// </summary>
public class SurveyFieldChangeSnapshot
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;
    
    [JsonPropertyName("surveyQuestion")]
    public string SurveyQuestion { get; set; } = string.Empty;
    
    [JsonPropertyName("oldValue")]
    public object? OldValue { get; set; }
    
    [JsonPropertyName("newValue")]
    public object? NewValue { get; set; }
    
    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; set; }
    
    [JsonPropertyName("mappingId")]
    public Guid MappingId { get; set; }
}
