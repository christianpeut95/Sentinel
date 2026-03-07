namespace Sentinel.Models.Reporting;

/// <summary>
/// Metadata about a field available for reporting
/// Generated from actual database schema to prevent hallucinated fields
/// </summary>
public class ReportFieldMetadata
{
    /// <summary>
    /// Entity type this field belongs to (Case, Outbreak, Patient, etc.)
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Path to access this field (e.g., "Patient.Age", "Jurisdiction1.Name")
    /// </summary>
    public string FieldPath { get; set; } = string.Empty;

    /// <summary>
    /// Display name for UI
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Data type: String, Int32, DateTime, Boolean, Decimal, etc.
    /// </summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping fields in UI (Demographics, Lab, Jurisdiction, etc.)
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Is this field nullable?
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Is this a navigation property?
    /// </summary>
    public bool IsNavigationProperty { get; set; }

    /// <summary>
    /// Is this a collection navigation (one-to-many)?
    /// </summary>
    public bool IsCollection { get; set; }

    /// <summary>
    /// Is this a primary key?
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Is this a foreign key?
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// Is this a custom field (EAV pattern)?
    /// </summary>
    public bool IsCustomField { get; set; }

    /// <summary>
    /// If custom field, the CustomFieldDefinition ID
    /// </summary>
    public int? CustomFieldDefinitionId { get; set; }

    /// <summary>
    /// Custom field type (if applicable): Boolean, String, Number, Date, Lookup
    /// </summary>
    public string? CustomFieldType { get; set; }

    /// <summary>
    /// Description/help text
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Can this field be used in filters?
    /// </summary>
    public bool IsFilterable { get; set; } = true;

    /// <summary>
    /// Can this field be grouped/aggregated?
    /// </summary>
    public bool IsGroupable { get; set; } = true;

    /// <summary>
    /// Can this field be aggregated (sum, avg, etc.)?
    /// </summary>
    public bool IsAggregatable { get; set; } = true;

    /// <summary>
    /// Suggested aggregation types for this field
    /// </summary>
    public List<string> SuggestedAggregations { get; set; } = new();

    /// <summary>
    /// Navigation depth (0 = direct property, 1 = one hop, etc.)
    /// </summary>
    public int NavigationDepth { get; set; }

    /// <summary>
    /// Performance warning if this field is expensive to query
    /// </summary>
    public string? PerformanceWarning { get; set; }

    /// <summary>
    /// For collections: The type of elements in the collection (e.g., "LabResult")
    /// </summary>
    public string? CollectionElementType { get; set; }

    /// <summary>
    /// For collection element types: Fields available for sub-filtering
    /// </summary>
    public List<string>? CollectionSubFields { get; set; }
    
    /// <summary>
    /// For enum fields: List of valid enum values with display names
    /// </summary>
    public List<EnumValueOption>? EnumValues { get; set; }
    
    /// <summary>
    /// Is this field an enum type?
    /// </summary>
    public bool IsEnum { get; set; }
}

/// <summary>
/// Represents an enum value option with its code and display name
/// </summary>
public class EnumValueOption
{
    /// <summary>
    /// The numeric code or name of the enum value (e.g., "3" or "Contact")
    /// </summary>
    public string Value { get; set; } = string.Empty;
    
    /// <summary>
    /// The display name from the [Display(Name = "...")] attribute
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// The numeric code (for enums that use numeric values)
    /// </summary>
    public int? Code { get; set; }
}
