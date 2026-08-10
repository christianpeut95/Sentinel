using Sentinel.Models.Reporting;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Helper class for filtering system/audit fields based on usage context
/// Implements global exclusion rules for survey mappers and report builders
/// </summary>
public static class SystemFieldFilter
{
    #region Exclusion Lists

    /// <summary>
    /// Primary key field names - always excluded everywhere
    /// </summary>
    private static readonly HashSet<string> PrimaryKeyFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id"
    };

    /// <summary>
    /// Audit/tracking field names - excluded from mappers, available in reports
    /// </summary>
    private static readonly HashSet<string> AuditFields = new(StringComparer.OrdinalIgnoreCase)
    {
        // Creation tracking
        "CreatedAt",
        "CreatedDate",
        "CreatedOn",
        "CreatedBy",
        "CreatedByUserId",
        "CreatedByName",

        // Modification tracking
        "ModifiedAt",
        "ModifiedDate",
        "ModifiedOn",
        "LastModifiedAt",
        "LastModifiedDate",
        "LastModifiedOn",
        "ModifiedBy",
        "ModifiedByUserId",
        "ModifiedByName",

        // Update tracking (alternative names)
        "UpdatedAt",
        "UpdatedDate",
        "UpdatedOn",
        "UpdatedBy",
        "UpdatedByUserId",
        "UpdatedByName",

        // Deletion tracking (soft delete)
        "DeletedAt",
        "DeletedDate",
        "DeletedOn",
        "DeletedBy",
        "DeletedByUserId",
        "DeletedByName",
        "IsDeleted",

        // Archive tracking
        "ArchivedAt",
        "ArchivedDate",
        "ArchivedOn",
        "ArchivedBy",
        "ArchivedByUserId",
        "ArchivedByName",
        "IsArchived"
    };

    /// <summary>
    /// Internal system fields - always excluded everywhere
    /// </summary>
    private static readonly HashSet<string> SystemFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "RowVersion",
        "ConcurrencyStamp",
        "Discriminator",
        "Timestamp",
        "Version"

        //add other fields to hide here

    };

    /// <summary>
    /// Foreign key suffixes to detect (except exclusions)
    /// </summary>
    private static readonly string[] ForeignKeySuffixes = { "Id" };

    /// <summary>
    /// Field names ending in "Id" that should NOT be excluded (they're not FKs)
    /// </summary>
    private static readonly HashSet<string> ForeignKeyExclusions = new(StringComparer.OrdinalIgnoreCase)
    {
        "FriendlyId",
        "ExternalId",
        "UniqueId"
    };

    #endregion

    #region Public Methods

    /// <summary>
    /// Determines if a field should be excluded based on its metadata and usage context
    /// </summary>
    /// <param name="field">Field metadata to evaluate</param>
    /// <param name="context">How the fields will be used (Mapper, Report, General)</param>
    /// <returns>True if the field should be excluded/hidden</returns>
    public static bool ShouldExcludeField(ReportFieldMetadata field, FieldUsageContext context)
    {
        if (field == null)
            return true;

        // ALWAYS EXCLUDE (regardless of context):

        // 1. Primary keys
        if (field.IsPrimaryKey || PrimaryKeyFields.Contains(field.FieldPath))
            return true;

        // 2. Internal system fields
        if (SystemFields.Contains(field.FieldPath))
            return true;

        // 3. Navigation collections (one-to-many relationships)
        if (field.IsCollection)
            return true;

        // 4. Foreign keys (fields ending in "Id" except exclusions)
        if (IsForeignKeyField(field))
            return true;

        // CONTEXT-SPECIFIC EXCLUSIONS:

        // 5. Audit fields - excluded from mappers, available in reports
        if (AuditFields.Contains(field.FieldPath))
        {
            return context switch
            {
                FieldUsageContext.Mapper => true,      // Hide in mappers
                FieldUsageContext.Report => false,     // Show in reports
                FieldUsageContext.General => false,    // Show by default
                _ => false
            };
        }

        // Field passes all filters
        return false;
    }

    /// <summary>
    /// Determines if a field is a foreign key based on naming convention
    /// </summary>
    private static bool IsForeignKeyField(ReportFieldMetadata field)
    {
        // Already marked by EF Core metadata
        if (field.IsForeignKey)
            return true;

        // Check naming convention: ends with "Id" but not in exclusion list
        var fieldName = field.FieldPath;

        // Handle navigation paths (e.g., "Patient.GenderId" -> check "GenderId")
        if (fieldName.Contains('.'))
        {
            fieldName = fieldName.Split('.').Last();
        }

        // Check if it ends with "Id" and is not in the exclusion list
        if (fieldName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
            !ForeignKeyExclusions.Contains(fieldName))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if a field name is an audit/tracking field
    /// Useful for categorization or UI hints
    /// </summary>
    public static bool IsAuditField(string fieldPath)
    {
        return AuditFields.Contains(fieldPath);
    }

    /// <summary>
    /// Checks if a field name is a system-managed field
    /// Useful for categorization or UI hints
    /// </summary>
    public static bool IsSystemField(string fieldPath)
    {
        return SystemFields.Contains(fieldPath) || PrimaryKeyFields.Contains(fieldPath);
    }

    /// <summary>
    /// Gets a human-readable reason why a field was excluded
    /// Useful for debugging or admin tools
    /// </summary>
    public static string GetExclusionReason(ReportFieldMetadata field, FieldUsageContext context)
    {
        if (field.IsPrimaryKey || PrimaryKeyFields.Contains(field.FieldPath))
            return "Primary key field";

        if (SystemFields.Contains(field.FieldPath))
            return "Internal system field";

        if (field.IsCollection)
            return "Navigation collection (one-to-many relationship)";

        if (IsForeignKeyField(field))
            return "Foreign key - use navigation properties instead";

        if (AuditFields.Contains(field.FieldPath))
        {
            return context == FieldUsageContext.Mapper
                ? "Audit field (system-managed, not available for mapping)"
                : "Available in reports for technical analysis";
        }

        return "Not excluded";
    }

    #endregion
}
