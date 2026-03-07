namespace Sentinel.Services.Reporting;

/// <summary>
/// Represents a parsed field path for query building
/// </summary>
public class ParsedFieldPath
{
    public string FullPath { get; set; } = string.Empty;
    public List<string> PathSegments { get; set; } = new();
    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }
    public string? CustomFieldType { get; set; }
    public bool IsNavigationProperty { get; set; }
    public int NavigationDepth { get; set; }
}

/// <summary>
/// Options for data extraction
/// </summary>
public class DataExtractionOptions
{
    public int? MaxRows { get; set; }
    public bool IncludeCustomFields { get; set; } = true;
    public bool IncludeNavigationProperties { get; set; } = true;
    public int MaxNavigationDepth { get; set; } = 2;
}
