using Sentinel.DTOs;
using Sentinel.Data;
using Sentinel.Services.Reporting;

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

    public CollectionQueryFilterBuilder(
        ApplicationDbContext context,
        IDynamicDateResolver dynamicDateResolver)
    {
        _context = context;
        _dynamicDateResolver = dynamicDateResolver;
    }

    /// <summary>
    /// Builds a Dynamic LINQ WHERE clause for collection query filters
    /// </summary>
    /// <param name="query">Collection query definition</param>
    /// <param name="entityType">Entity type (Case, Patient, Outbreak)</param>
    /// <returns>Dynamic LINQ WHERE clause string or null if not applicable</returns>
    public string? BuildCollectionFilterClause(CollectionQueryDto query, string entityType)
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
            Console.WriteLine($"[CollectionFilterBuilder] Skipping SQL filter for Patient virtual collection: {query.CollectionName}");
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

        Console.WriteLine($"[CollectionFilterBuilder] Building SQL filter for {query.Operation} on {query.CollectionName} with {query.SubFilters?.Count ?? 0} subfilters");

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

    /// <summary>
    /// Builds an EXISTS clause for HasAny operations
    /// Generates: WHERE EXISTS (SELECT 1 FROM Collection WHERE FK = Id AND [conditions])
    /// </summary>
    public string? BuildExistsClause(CollectionQueryDto query, string entityType)
    {
        var (tableName, foreignKeyField) = GetCollectionMapping(query.CollectionName, entityType, query.SubCollectionName);

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(foreignKeyField))
        {
            Console.WriteLine($"[CollectionFilterBuilder] Unknown collection: {query.CollectionName} for {entityType}");
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
            Console.WriteLine($"[CollectionFilterBuilder] Unknown collection: {query.CollectionName} for {entityType}");
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
            Console.WriteLine($"[CollectionFilterBuilder] Unknown collection: {query.CollectionName} for {entityType}");
            return null;
        }

        if (string.IsNullOrEmpty(query.AggregateField))
        {
            Console.WriteLine($"[CollectionFilterBuilder] AggregateField required for {aggregateFunction} operation");
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
                    Console.WriteLine($"[CollectionFilterBuilder] Failed to resolve dynamic date {subFilter.DynamicDateType}: {ex.Message}");
                    // Fall through to regular condition building
                }
            }
        }

        // Handle different data types and operators
        return subFilter.DataType switch
        {
            "String" => BuildStringCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "DateTime" or "DateOnly" => BuildDateCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "Int32" or "Double" or "Decimal" => BuildNumericCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            "Boolean" => BuildBooleanCondition(fieldAccess, subFilter.Operator, subFilter.Value),
            _ => null
        };
    }

    private string? BuildStringCondition(string fieldAccess, string op, string value)
    {
        // Escape quotes in value
        string escapedValue = value.Replace("\"", "\\\"");

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
        if (!DateTime.TryParse(value, out var dateValue))
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
        if (!double.TryParse(value, out var numValue))
        {
            return null;
        }

        return op switch
        {
            "Equals" => $"{fieldAccess} == {numValue}",
            "NotEquals" => $"{fieldAccess} != {numValue}",
            "GreaterThan" => $"{fieldAccess} > {numValue}",
            "LessThan" => $"{fieldAccess} < {numValue}",
            "GreaterThanOrEqual" => $"{fieldAccess} >= {numValue}",
            "LessThanOrEqual" => $"{fieldAccess} <= {numValue}",
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
            "Equals" => $"{fieldAccess} == {boolValue.ToString().ToLower()}",
            "NotEquals" => $"{fieldAccess} != {boolValue.ToString().ToLower()}",
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

        Console.WriteLine($"[CollectionFilterBuilder] Unknown collection mapping: {collectionName} for {entityType}");
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
            "Equals" => $"{leftSide} == {value}",
            "NotEquals" => $"{leftSide} != {value}",
            "GreaterThan" => $"{leftSide} > {value}",
            "LessThan" => $"{leftSide} < {value}",
            "GreaterThanOrEqual" => $"{leftSide} >= {value}",
            "LessThanOrEqual" => $"{leftSide} <= {value}",
            "IsNull" => $"{leftSide} == null",
            "IsNotNull" => $"{leftSide} != null",
            _ => "false"
        };
    }
}
