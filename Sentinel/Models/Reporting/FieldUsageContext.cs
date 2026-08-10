namespace Sentinel.Models.Reporting;

/// <summary>
/// Indicates the context in which database fields are being used
/// Used to apply different field filtering rules for different scenarios
/// </summary>
public enum FieldUsageContext
{
    /// <summary>
    /// General purpose - minimal filtering (default)
    /// </summary>
    General = 0,

    /// <summary>
    /// Fields are being used for survey mapping
    /// Hides: primary keys, foreign keys, audit fields, navigation collections
    /// </summary>
    Mapper = 1,

    /// <summary>
    /// Fields are being used in report builder
    /// Hides: primary keys, foreign keys, navigation collections
    /// Shows: audit fields (for technical reporting)
    /// </summary>
    Report = 2
}
