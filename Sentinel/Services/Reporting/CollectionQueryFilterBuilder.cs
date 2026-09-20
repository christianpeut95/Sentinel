using Sentinel.DTOs;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Globalization;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Builds SQL-level filters (Dynamic LINQ) for collection queries
/// Converts collection query DTOs into WHERE clauses that execute at the database level
/// This avoids N+1 queries and in-memory filtering for large datasets
/// </summary>
public class CollectionQueryFilterBuilder
{
    private readonly ApplicationDbContext _context;
    private readonly IDynamicDateResolver _dynamicDateResolver;
    private readonly IReportFieldMetadataService _fieldMetadataService;

    public CollectionQueryFilterBuilder(
        ApplicationDbContext context,
        IDynamicDateResolver dynamicDateResolver,
        IReportFieldMetadataService fieldMetadataService)
    {
        _context = context;
        _dynamicDateResolver = dynamicDateResolver;
        _fieldMetadataService = fieldMetadataService;
    }

    /// <summary>
    /// Validates a persisted collection query against server-discovered metadata before
    /// building the Dynamic LINQ clause. Report JSON must never choose a member path.
    /// </summary>
    public async Task<string?> BuildCollectionFilterClauseAsync(CollectionQueryDto query, string entityType)
    {
        if (query.DisplayAsColumn)
        {
            return null;
        }

        // Patient Cases/Contacts are calculated after extraction and have no queryable
        // navigation property, so retain the existing post-processing behaviour.
        if (entityType == "Patient" && (query.CollectionName == "Cases" || query.CollectionName == "Contacts"))
        {
            return null;
        }

        var fields = await _fieldMetadataService.GetFieldsForEntityAsync(entityType);
        var collection = fields.FirstOrDefault(field =>
            field.FieldPath == query.CollectionName && field.IsCollection && field.IsFilterable);

        if (collection == null)
        {
            throw new ArgumentException($"Collection '{query.CollectionName}' is not available for {entityType} reports.");
        }

        ValidateCollectionQuery(query, collection);
        return BuildCollectionFilterClause(query, entityType);
    }

    /// <summary>
    /// Builds a Dynamic LINQ WHERE clause for collection query filters
    /// </summary>
    /// <param name="query">Collection query definition</param>
    /// <param name="entityType">Entity type (Case, Patient, Outbreak)</param>
    /// <returns>Dynamic LINQ WHERE clause string or null if not applicable</returns>
    private string? BuildCollectionFilterClause(CollectionQueryDto query, string entityType)
    {
        // Only build filters for queries that are NOT displayed as columns
        if (query.DisplayAsColumn)
        {
            return null;
        }

        // Skip SQL-level filtering for Patient virtual collections (Cases/Contacts)
        // These don't have navigation properties on Patient and must use post-processing
        if (entityType == "Patient" && (query.CollectionName == "Cases" || query.CollectionName == "Contacts"))
        {
            return null;
        }

        // SQL-level filters use SubFilters (e.g., HasAny with conditions)
        // Post-processing filters use Comparator (e.g., Count > 5)
        // This method handles SQL-level filters; post-processing happens in ApplyCollectionQueryFilters
        var isSqlFilter = query.SubFilters?.Any() == true;
        var isPostProcessingFilter = !string.IsNullOrEmpty(query.Comparator);

        if (!isSqlFilter)
        {
            // Not a SQL-level filter, skip it
            return null;
        }


        return query.Operation switch
        {
            "HasAny" => BuildExistsClause(query, entityType),
            "Count" => BuildCountClause(query, entityType),
            "Sum" => BuildAggregateClause(query, entityType, "Sum"),
            "Average" => BuildAggregateClause(query, entityType, "Average"),
            "Min" => BuildAggregateClause(query, entityType, "Min"),
            "Max" => BuildAggregateClause(query, entityType, "Max"),
            _ => null
        };
    }

    private void ValidateCollectionQuery(CollectionQueryDto query, ReportFieldMetadata collection)
    {
        if (query.Operation is not ("HasAny" or "Count" or "Sum" or "Average" or "Min" or "Max"))
        {
            throw new ArgumentException($"Collection operation '{query.Operation}' is not supported.");
        }

        if (!string.IsNullOrEmpty(query.Comparator) && query.Comparator is not
            ("Equals" or "NotEquals" or "GreaterThan" or "LessThan" or "GreaterThanOrEqual" or "LessThanOrEqual" or "IsNull" or "IsNotNull"))
        {
            throw new ArgumentException($"Collection comparator '{query.Comparator}' is not supported.");
        }

        var allowedFields = GetAllowedSubFields(collection, query.SubCollectionName);
        if (allowedFields.Count == 0)
        {
            throw new ArgumentException($"Nested collection '{query.CollectionName}.{query.SubCollectionName}' is not supported.");
        }

        foreach (var subFilter in query.SubFilters ?? [])
        {
            if (!allowedFields.TryGetValue(subFilter.Field, out var metadata))
            {
                throw new ArgumentException($"Field '{subFilter.Field}' is not available in collection '{query.CollectionName}'.");
            }

            if (!IsSubFilterOperatorAllowed(metadata.DataType, subFilter.Operator))
            {
                throw new ArgumentException($"Operator '{subFilter.Operator}' is not allowed for '{subFilter.Field}'.");
            }

            ValidateSubFilterValue(subFilter, metadata.DataType);

            // Do not trust type information stored in a report definition.
            subFilter.DataType = metadata.DataType;
        }

        if (query.Operation is "Sum" or "Average" or "Min" or "Max")
        {
            if (string.IsNullOrEmpty(query.AggregateField) ||
                !allowedFields.TryGetValue(query.AggregateField, out var aggregateMetadata) ||
                !IsNumericType(aggregateMetadata.DataType))
            {
                throw new ArgumentException($"A numeric aggregate field is required for '{query.Operation}'.");
            }
        }
    }

    private IReadOnlyDictionary<string, CollectionSubFieldMetadata> GetAllowedSubFields(
        ReportFieldMetadata collection,
        string? subCollectionName)
    {
        IEnumerable<CollectionSubFieldMetadata> fields;

        if (string.IsNullOrEmpty(subCollectionName))
        {
            fields = collection.CollectionSubFieldsMetadata ?? [];
        }
        else if (collection.FieldPath == "LabResults" && subCollectionName == "Markers")
        {
            fields = GetLabResultMarkerSubFields();
        }
        else
        {
            return new Dictionary<string, CollectionSubFieldMetadata>(StringComparer.Ordinal);
        }

        return fields.ToDictionary(field => field.FieldPath, StringComparer.Ordinal);
    }

    private IEnumerable<CollectionSubFieldMetadata> GetLabResultMarkerSubFields()
    {
        var markerType = _context.Model.FindEntityType(typeof(LabResultMarker));
        if (markerType == null)
        {
            return [];
        }

        var fields = markerType.GetProperties()
            .Where(property => !property.IsShadowProperty() && !property.IsPrimaryKey() && !property.IsForeignKey())
            .Select(property => new CollectionSubFieldMetadata
            {
                FieldPath = property.Name,
                Name = property.Name,
                DataType = GetDataTypeName(property.ClrType)
            })
            .ToList();

        foreach (var navigation in markerType.GetNavigations().Where(navigation => !navigation.IsCollection))
        {
            foreach (var property in navigation.TargetEntityType.GetProperties().Where(property =>
                         property.Name is "Name" or "DisplayName" or "Code"))
            {
                fields.Add(new CollectionSubFieldMetadata
                {
                    FieldPath = $"{navigation.Name}.{property.Name}",
                    Name = $"{navigation.Name} - {property.Name}",
                    DataType = GetDataTypeName(property.ClrType)
                });
            }
        }

        return fields;
    }

    private static bool IsSubFilterOperatorAllowed(string dataType, string operation)
    {
        var normalizedType = dataType.ToLowerInvariant();
        var allowed = normalizedType switch
        {
            "datetime" or "date" or "dateonly" => new[]
            {
                "Equals", "On", "NotEquals", "GreaterThan", "After", "LessThan", "Before",
                "GreaterThanOrEqual", "LessThanOrEqual", "InLast", "InNext", "IsNull", "IsNotNull"
            },
            "int32" or "int64" or "decimal" or "double" or "float" or "number" => new[]
            {
                "Equals", "NotEquals", "GreaterThan", "LessThan", "GreaterThanOrEqual", "LessThanOrEqual",
                "IsNull", "IsNotNull"
            },
            "boolean" or "bool" => new[] { "Equals", "NotEquals" },
            _ => new[]
            {
                "Equals", "NotEquals", "Contains", "NotContains", "StartsWith", "EndsWith",
                "IsNull", "IsNotNull", "IsEmpty", "IsNotEmpty"
            }
        };

        return allowed.Contains(operation, StringComparer.Ordinal);
    }

    private static bool IsNumericType(string dataType) =>
        dataType is "Int32" or "Int64" or "Decimal" or "Double" or "Float" or "Number";

    private static void ValidateSubFilterValue(CollectionSubFilter subFilter, string dataType)
    {
        if (subFilter.Operator is "IsNull" or "IsNotNull" or "IsEmpty" or "IsNotEmpty" || subFilter.IsDynamicDate)
        {
            return;
        }

        if (IsNumericType(dataType) &&
            !double.TryParse(subFilter.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
        {
            throw new ArgumentException($"Invalid numeric value for '{subFilter.Field}'.");
        }

        if (dataType is "Boolean" or "Bool" && !bool.TryParse(subFilter.Value, out _))
        {
            throw new ArgumentException($"Invalid boolean value for '{subFilter.Field}'.");
        }

        if (dataType is "DateTime" or "Date" or "DateOnly" &&
            !DateTime.TryParse(subFilter.Value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out _))
        {
            throw new ArgumentException($"Invalid date value for '{subFilter.Field}'.");
        }
    }

    private static string GetDataTypeName(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.Name switch
        {
            "Int32" => "Int32",
            "Int64" => "Int64",
            "Decimal" => "Decimal",
            "Double" => "Double",
            "Single" => "Float",
            "Boolean" => "Boolean",
            "DateTime" => "DateTime",
            "DateOnly" => "DateOnly",
            "String" => "String",
            _ => underlyingType.Name
        };
    }

    /// <summary>
    /// Builds an EXISTS clause for HasAny operations
    /// Generates: WHERE EXISTS (SELECT 1 FROM Collection WHERE FK = Id AND [conditions])
    /// </summary>
    public string? BuildExistsClause(CollectionQueryDto query, string entityType)
    {
        var (tableName, foreignKeyField) = GetCollectionMapping(query.CollectionName, entityType, query.SubCollectionName);

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(foreignKeyField))
        {
            return null;
        }

        // Build sub-filter conditions if present
        string? subFilterCondition = null;
        if (query.SubFilters?.Any() == true)
        {
            subFilterCondition = BuildSubFilterExpression(query.SubFilters, "x");
        }

        // Build the complete EXISTS clause
        // For nested collections like LabResults.Markers, tableName will handle the join
        string existsClause = $"{tableName}.Any(x => x.{foreignKeyField} == Id";

        if (!string.IsNullOrEmpty(subFilterCondition))
        {
            existsClause += $" && ({subFilterCondition})";
        }

        existsClause += ")";

        // Handle comparator for HasAny (usually checking if true/false)
        if (query.Comparator == "Equals")
        {
            if (query.Value == 1.0)
            {
                // HasAny == true (exists)
                return existsClause;
            }
            else if (query.Value == 0.0)
            {
                // HasAny == false (does not exist)
                return $"!({existsClause})";
            }
        }

        // Default: just return the exists check
        return existsClause;
    }

    /// <summary>
    /// Builds a COUNT clause for Count operations
    /// Generates: WHERE Collection.Where(x => FK == Id [&& conditions]).Count() [comparator] [value]
    /// </summary>
    public string? BuildCountClause(CollectionQueryDto query, string entityType)
    {
        var (collectionPath, foreignKeyField) = GetCollectionMapping(query.CollectionName, entityType, query.SubCollectionName);

        if (string.IsNullOrEmpty(collectionPath) || string.IsNullOrEmpty(foreignKeyField))
        {
            return null;
        }

        // Build sub-filter conditions if present
        string? subFilterCondition = null;
        if (query.SubFilters?.Any() == true)
        {
            subFilterCondition = BuildSubFilterExpression(query.SubFilters, "x");
        }

        // Build the COUNT expression
        string countExpression = $"{collectionPath}.Count(x => x.{foreignKeyField} == Id";

        if (!string.IsNullOrEmpty(subFilterCondition))
        {
            countExpression += $" && ({subFilterCondition})";
        }

        countExpression += ")";

        // Apply comparator
        if (string.IsNullOrEmpty(query.Comparator))
        {
            return null; // Count operations need a comparator for filtering
        }

        return BuildComparisonExpression(countExpression, query.Comparator, query.Value);
    }

    /// <summary>
    /// Builds an aggregate clause for Sum/Average/Min/Max operations
    /// Generates: WHERE Collection.Where(x => FK == Id [&& conditions]).[Aggregate](x => x.Field) [comparator] [value]
    /// </summary>
    public string? BuildAggregateClause(CollectionQueryDto query, string entityType, string aggregateFunction)
    {
        var (collectionPath, foreignKeyField) = GetCollectionMapping(query.CollectionName, entityType, query.SubCollectionName);

        if (string.IsNullOrEmpty(collectionPath) || string.IsNullOrEmpty(foreignKeyField))
        {
            return null;
        }

        if (string.IsNullOrEmpty(query.AggregateField))
        {
            return null;
        }

        // Build sub-filter conditions if present
        string? subFilterCondition = null;
        if (query.SubFilters?.Any() == true)
        {
            subFilterCondition = BuildSubFilterExpression(query.SubFilters, "x");
        }

        // Build the aggregate expression
        string whereClause = $"x => x.{foreignKeyField} == Id";
        if (!string.IsNullOrEmpty(subFilterCondition))
        {
            whereClause += $" && ({subFilterCondition})";
        }

        string aggregateExpression;

        // For Average, we need to handle potential null/empty collections
        if (aggregateFunction == "Average")
        {
            aggregateExpression = $"({collectionPath}.Where({whereClause}).Any() ? " +
                                 $"{collectionPath}.Where({whereClause}).Average(x => x.{query.AggregateField}) : 0)";
        }
        else if (aggregateFunction == "Sum")
        {
            aggregateExpression = $"{collectionPath}.Where({whereClause}).Sum(x => x.{query.AggregateField})";
        }
        else if (aggregateFunction == "Min")
        {
            aggregateExpression = $"({collectionPath}.Where({whereClause}).Any() ? " +
                                 $"{collectionPath}.Where({whereClause}).Min(x => x.{query.AggregateField}) : 0)";
        }
        else if (aggregateFunction == "Max")
        {
            aggregateExpression = $"({collectionPath}.Where({whereClause}).Any() ? " +
                                 $"{collectionPath}.Where({whereClause}).Max(x => x.{query.AggregateField}) : 0)";
        }
        else
        {
            return null;
        }

        // Apply comparator
        if (string.IsNullOrEmpty(query.Comparator))
        {
            return null; // Aggregate operations need a comparator for filtering
        }

        return BuildComparisonExpression(aggregateExpression, query.Comparator, query.Value);
    }

    /// <summary>
    /// Builds a Dynamic LINQ expression from collection sub-filters
    /// Converts list of sub-filters to a combined expression using AND logic
    /// </summary>
    public string? BuildSubFilterExpression(List<CollectionSubFilter> subFilters, string collectionAlias)
    {
        if (subFilters == null || !subFilters.Any())
        {
            return null;
        }

        var conditions = new List<string>();

        foreach (var subFilter in subFilters)
        {
            string? condition = BuildSingleSubFilterCondition(subFilter, collectionAlias);
            if (!string.IsNullOrEmpty(condition))
            {
                conditions.Add(condition);
            }
        }

        return conditions.Any() ? string.Join(" && ", conditions) : null;
    }

    /// <summary>
    /// Builds a single sub-filter condition
    /// </summary>
    private string? BuildSingleSubFilterCondition(CollectionSubFilter subFilter, string collectionAlias)
    {
        string fieldAccess = $"{collectionAlias}.{subFilter.Field}";

        // Handle dynamic dates
        if (subFilter.IsDynamicDate)
        {
            // Handle InLast / InNext operators (days-only)
            if (subFilter.Operator == "InLast" && subFilter.DynamicDateOffset.HasValue)
            {
                var startDate = DateTime.UtcNow.Date.AddDays(-subFilter.DynamicDateOffset.Value);
                var endDate = DateTime.UtcNow.Date.AddDays(1);
                return $"({fieldAccess} >= DateTime({startDate.Year}, {startDate.Month}, {startDate.Day}) " +
                       $"&& {fieldAccess} < DateTime({endDate.Year}, {endDate.Month}, {endDate.Day}))";
            }
            else if (subFilter.Operator == "InNext" && subFilter.DynamicDateOffset.HasValue)
            {
                var startDate = DateTime.UtcNow.Date;
                var endDate = DateTime.UtcNow.Date.AddDays(subFilter.DynamicDateOffset.Value + 1);
                return $"({fieldAccess} >= DateTime({startDate.Year}, {startDate.Month}, {startDate.Day}) " +
                       $"&& {fieldAccess} < DateTime({endDate.Year}, {endDate.Month}, {endDate.Day}))";
            }
            // Handle common dynamic date types for other operators
            else if (!string.IsNullOrEmpty(subFilter.DynamicDateType) && 
                     (subFilter.Operator == "Equals" || subFilter.Operator == "On" ||
                      subFilter.Operator == "GreaterThan" || subFilter.Operator == "After" ||
                      subFilter.Operator == "LessThan" || subFilter.Operator == "Before" ||
                      subFilter.Operator == "GreaterThanOrEqual" || subFilter.Operator == "LessThanOrEqual"))
            {
                try
                {
                    var resolvedDate = _dynamicDateResolver.ResolveDate(
                        subFilter.DynamicDateType,
                        subFilter.DynamicDateOffset,
                        subFilter.DynamicDateOffsetUnit
                    );

                    // Use the resolved date with the operator
                    return BuildDateCondition(fieldAccess, subFilter.Operator, resolvedDate.ToString("yyyy-MM-dd"));
                }
                catch (Exception ex)
                {
                    // Fall through to regular condition building
                }
            }
        }

        // Handle different data types and operators
        return subFilter.DataType switch
        {
            "String" => BuildStringCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "DateTime" or "DateOnly" => BuildDateCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "Int32" or "Int64" or "Double" or "Float" or "Decimal" or "Number" => BuildNumericCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "Boolean" => BuildBooleanCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            _ => null
        };
    }

    private string? BuildStringCondition(string fieldAccess, string op, string value)
    {
        // Backslashes must be escaped before quotes to preserve literal boundaries.
        string escapedValue = value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return op switch
        {
            "Equals" => $"{fieldAccess} == \"{escapedValue}\"",
            "NotEquals" => $"{fieldAccess} != \"{escapedValue}\"",
            "Contains" => $"{fieldAccess} != null && {fieldAccess}.Contains(\"{escapedValue}\")",
            "NotContains" => $"{fieldAccess} == null || !{fieldAccess}.Contains(\"{escapedValue}\")",
            "StartsWith" => $"{fieldAccess} != null && {fieldAccess}.StartsWith(\"{escapedValue}\")",
            "EndsWith" => $"{fieldAccess} != null && {fieldAccess}.EndsWith(\"{escapedValue}\")",
            "IsNull" => $"{fieldAccess} == null",
            "IsNotNull" => $"{fieldAccess} != null",
            "IsEmpty" => $"({fieldAccess} == null || {fieldAccess} == \"\")",
            "IsNotEmpty" => $"({fieldAccess} != null && {fieldAccess} != \"\")",
            _ => null
        };
    }

    private string? BuildDateCondition(string fieldAccess, string op, string value)
    {
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateValue))
        {
            return null;
        }

        string dateExpression = $"DateTime({dateValue.Year}, {dateValue.Month}, {dateValue.Day})";

        return op switch
        {
            "Equals" or "On" => $"{fieldAccess}.Date == {dateExpression}.Date",
            "NotEquals" => $"{fieldAccess}.Date != {dateExpression}.Date",
            "GreaterThan" or "After" => $"{fieldAccess} > {dateExpression}",
            "LessThan" or "Before" => $"{fieldAccess} < {dateExpression}",
            "GreaterThanOrEqual" => $"{fieldAccess} >= {dateExpression}",
            "LessThanOrEqual" => $"{fieldAccess} <= {dateExpression}",
            "IsNull" => $"{fieldAccess} == null",
            "IsNotNull" => $"{fieldAccess} != null",
            _ => null
        };
    }

    private string? BuildNumericCondition(string fieldAccess, string op, string value)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var numValue))
        {
            return null;
        }

        return op switch
        {
            "Equals" => $"{fieldAccess} == {numValue.ToString(CultureInfo.InvariantCulture)}",
            "NotEquals" => $"{fieldAccess} != {numValue.ToString(CultureInfo.InvariantCulture)}",
            "GreaterThan" => $"{fieldAccess} > {numValue.ToString(CultureInfo.InvariantCulture)}",
            "LessThan" => $"{fieldAccess} < {numValue.ToString(CultureInfo.InvariantCulture)}",
            "GreaterThanOrEqual" => $"{fieldAccess} >= {numValue.ToString(CultureInfo.InvariantCulture)}",
            "LessThanOrEqual" => $"{fieldAccess} <= {numValue.ToString(CultureInfo.InvariantCulture)}",
            "IsNull" => $"{fieldAccess} == null",
            "IsNotNull" => $"{fieldAccess} != null",
            _ => null
        };
    }

    private string? BuildBooleanCondition(string fieldAccess, string op, string value)
    {
        if (!bool.TryParse(value, out var boolValue))
        {
            return null;
        }

        return op switch
        {
            "Equals" => $"{fieldAccess} == {boolValue.ToString().ToLowerInvariant()}",
            "NotEquals" => $"{fieldAccess} != {boolValue.ToString().ToLowerInvariant()}",
            _ => null
        };
    }

    /// <summary>
    /// Gets the collection property name and foreign key field for a given collection
    /// Returns a tuple of (collectionPropertyName, foreignKeyField)
    /// For nested collections, returns the navigation path
    /// </summary>
    private (string? collectionPath, string? foreignKeyField) GetCollectionMapping(
        string collectionName,
        string entityType,
        string? subCollectionName = null)
    {
        // Handle nested collections first (e.g., LabResults -> Markers)
        if (!string.IsNullOrEmpty(subCollectionName))
        {
            if (collectionName == "LabResults" && subCollectionName == "Markers")
            {
                // Flattened access for nested collection
                return ("LabResults.SelectMany(lr => lr.Markers)", "LabResultId");
            }
            // Add more nested collection mappings as needed
        }

        // Map collection names to entity collection properties and foreign keys
        if (entityType == "Case" || entityType == "Contact")
        {
            return collectionName switch
            {
                // Core case collections
                "LabResults" => ("LabResults", "CaseId"),
                "ExposureEvents" or "Exposures" => ("ExposureEvents", "ExposedCaseId"),
                "Tasks" or "CaseTasks" => ("Tasks", "CaseId"),
                "Symptoms" or "CaseSymptoms" or "CaseSymptomTracking" => ("CaseSymptoms", "CaseId"),
                "Notes" => ("Notes", "CaseId"),
                "ClassificationHistory" => ("ClassificationHistory", "CaseId"),

                // Custom fields
                "CustomFieldStrings" => ("CustomFieldStrings", "CaseId"),
                "CustomFieldNumbers" => ("CustomFieldNumbers", "CaseId"),
                "CustomFieldDates" => ("CustomFieldDates", "CaseId"),
                "CustomFieldBooleans" => ("CustomFieldBooleans", "CaseId"),
                "CustomFieldLookups" => ("CustomFieldLookups", "CaseId"),

                _ => (null, null)
            };
        }

        if (entityType == "Patient")
        {
            return collectionName switch
            {
                // Patient collections
                "Cases" => ("Cases", "PatientId"),
                "Contacts" => ("Cases", "PatientId"), // Filter on CaseType in sub-filter
                "LabResults" => ("LabResults", "PatientId"),
                "Notes" => ("Notes", "PatientId"),

                // Custom fields
                "CustomFieldStrings" or "PatientCustomFieldStrings" => ("PatientCustomFieldStrings", "PatientId"),
                "CustomFieldNumbers" or "PatientCustomFieldNumbers" => ("PatientCustomFieldNumbers", "PatientId"),
                "CustomFieldDates" or "PatientCustomFieldDates" => ("PatientCustomFieldDates", "PatientId"),
                "CustomFieldBooleans" or "PatientCustomFieldBooleans" => ("PatientCustomFieldBooleans", "PatientId"),
                "CustomFieldLookups" or "PatientCustomFieldLookups" => ("PatientCustomFieldLookups", "PatientId"),

                _ => (null, null)
            };
        }

        if (entityType == "Outbreak")
        {
            return collectionName switch
            {
                // Outbreak collections
                "OutbreakCases" => ("OutbreakCases", "OutbreakId"),
                "TeamMembers" or "OutbreakTeamMembers" => ("TeamMembers", "OutbreakId"),
                "CaseDefinitions" or "OutbreakCaseDefinitions" => ("CaseDefinitions", "OutbreakId"),
                "TimelineEvents" or "OutbreakTimelines" => ("TimelineEvents", "OutbreakId"),
                "SavedSearches" or "OutbreakSearchQueries" => ("SavedSearches", "OutbreakId"),
                "ChildOutbreaks" => ("ChildOutbreaks", "ParentOutbreakId"),
                "Notes" => ("Notes", "OutbreakId"),

                _ => (null, null)
            };
        }

        return (null, null);
    }

    /// <summary>
    /// Evaluates a comparator expression for use in WHERE clauses
    /// </summary>
    private string BuildComparisonExpression(string leftSide, string comparator, double? value)
    {
        if (!value.HasValue && comparator != "IsNull" && comparator != "IsNotNull")
        {
            return "false"; // Invalid comparison
        }

        return comparator switch
        {
            "Equals" => $"{leftSide} == {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "NotEquals" => $"{leftSide} != {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "GreaterThan" => $"{leftSide} > {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "LessThan" => $"{leftSide} < {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "GreaterThanOrEqual" => $"{leftSide} >= {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "LessThanOrEqual" => $"{leftSide} <= {value!.Value.ToString(CultureInfo.InvariantCulture)}",
            "IsNull" => $"{leftSide} == null",
            "IsNotNull" => $"{leftSide} != null",
            _ => "false"
        };
    }
}
