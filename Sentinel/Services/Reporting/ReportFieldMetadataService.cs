using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Tools;
using System.ComponentModel.DataAnnotations;
using System.Collections.Concurrent;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for discovering and providing metadata about fields available for reporting
/// Uses FieldInventoryGenerator 
/// </summary>
public class ReportFieldMetadataService : IReportFieldMetadataService
{
    private readonly ApplicationDbContext _context;

    // Thread-safe cache to avoid regenerating field metadata on every request
    private static readonly ConcurrentDictionary<string, List<ReportFieldMetadata>> _fieldCache = new();
    private static DateTime _cacheExpiry = DateTime.MinValue;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public ReportFieldMetadataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReportFieldMetadata>> GetFieldsForEntityAsync(string entityType)
    {
        // Check cache first
        if (_fieldCache.ContainsKey(entityType) && DateTime.UtcNow < _cacheExpiry)
        {
            return _fieldCache[entityType];
        }

        var fields = new List<ReportFieldMetadata>();

        // Get regular fields from database schema
        var regularFields = GetRegularFieldsFromSchema(entityType);
        fields.AddRange(regularFields);

        // Get custom fields
        var customFields = await GetCustomFieldsForEntityAsync(entityType);
        fields.AddRange(customFields);

        // Update cache
        _fieldCache[entityType] = fields;
        _cacheExpiry = DateTime.UtcNow.Add(CacheDuration);

        return fields;
    }

    public async Task<Dictionary<string, List<ReportFieldMetadata>>> GetFieldsByCategoryAsync(string entityType)
    {
        var allFields = await GetFieldsForEntityAsync(entityType);

        return allFields
            .GroupBy(f => f.Category)
            .OrderBy(g => GetCategoryOrder(g.Key))
            .ToDictionary(g => g.Key, g => g.OrderBy(f => f.DisplayName).ToList());
    }

    public async Task<List<ReportFieldMetadata>> GetCustomFieldsForEntityAsync(string entityType)
    {
        var customFields = new List<ReportFieldMetadata>();

        // Query active custom field definitions
        var definitions = await _context.CustomFieldDefinitions
            .Where(cfd => cfd.IsActive)
            .ToListAsync();

        // Check which are applicable to this entity type
        foreach (var definition in definitions)
        {
            bool isApplicable = false;

            // Both Case and Contact use the ShowOnCaseForm flag
            if ((entityType == "Case" || entityType == "Contact") && definition.ShowOnCaseForm)
            {
                isApplicable = true;
            }
            else if (entityType == "Patient" && definition.ShowOnPatientForm)
            {
                isApplicable = true;
            }

            if (!isApplicable)
                continue;

            var metadata = new ReportFieldMetadata
            {
                EntityType = entityType,
                FieldPath = $"CustomField:{definition.Name}",  // ? FIXED: Use colon separator
                DisplayName = definition.Label,
                DataType = GetCustomFieldDataType(definition.FieldType),
                Category = !string.IsNullOrEmpty(definition.Category) 
                    ? definition.Category 
                    : "Custom Fields",
                IsNullable = !definition.IsRequired,
                IsNavigationProperty = false,
                IsCollection = false,
                IsPrimaryKey = false,
                IsForeignKey = false,
                IsCustomField = true,
                CustomFieldDefinitionId = definition.Id,
                CustomFieldType = definition.FieldType.ToString(),
                Description = $"Custom field: {definition.Label}",
                IsFilterable = definition.IsSearchable || true, // Most custom fields are filterable
                IsGroupable = definition.FieldType != CustomFieldType.TextArea, // Long text not groupable
                IsAggregatable = definition.FieldType == CustomFieldType.Number,
                SuggestedAggregations = GetSuggestedAggregationsForCustomField(definition.FieldType),
                NavigationDepth = 0
            };

            customFields.Add(metadata);
        }

        return customFields;
    }

    public async Task<ReportFieldMetadata?> GetFieldMetadataAsync(string entityType, string fieldPath)
    {
        var allFields = await GetFieldsForEntityAsync(entityType);
        return allFields.FirstOrDefault(f => f.FieldPath == fieldPath);
    }

    public async Task<bool> ValidateFieldPathAsync(string entityType, string fieldPath)
    {
        var metadata = await GetFieldMetadataAsync(entityType, fieldPath);
        return metadata != null;
    }

    public List<string> GetSuggestedAggregations(string dataType)
    {
        return dataType.ToLower() switch
        {
            "int32" or "int64" or "decimal" or "double" or "float" or "number" => 
                new List<string> { "Count", "Sum", "Average", "Min", "Max", "DistinctCount" },
            "datetime" or "datetimeoffset" or "date" => 
                new List<string> { "Count", "Min", "Max", "DistinctCount" },
            "boolean" or "bool" => 
                new List<string> { "Count", "DistinctCount" },
            _ => 
                new List<string> { "Count", "DistinctCount" }
        };
    }

    #region Private Helper Methods

    private List<ReportFieldMetadata> GetRegularFieldsFromSchema(string entityType)
    {
        var fields = new List<ReportFieldMetadata>();

        // Get entity type from EF Core model
        var entityTypeObj = _context.Model.FindEntityType(GetClrType(entityType));
        if (entityTypeObj == null)
        {
            return fields;
        }

        // Get all properties from the entity
        foreach (var property in entityTypeObj.GetProperties())
        {
            // Skip shadow properties ONLY (keep foreign keys - they're important for mappings)
            if (property.IsShadowProperty())
                continue;

            var clrType = property.ClrType;
            var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;
            var isEnum = underlyingType.IsEnum;
            
            var field = new ReportFieldMetadata
            {
                EntityType = entityType,
                FieldPath = property.Name,
                DisplayName = GetDisplayName(property),
                DataType = GetDataTypeName(property.ClrType),
                Category = CategorizeProperty(property),
                IsNullable = property.IsNullable,
                IsNavigationProperty = false,
                IsCollection = false,
                IsPrimaryKey = property.IsPrimaryKey(),
                IsForeignKey = property.IsForeignKey(),
                IsCustomField = false,
                IsEnum = isEnum,
                EnumValues = isEnum ? GetEnumValues(underlyingType) : null,
                SuggestedAggregations = GetSuggestedAggregations(GetDataTypeName(property.ClrType)),
                IsFilterable = true,
                IsGroupable = true,
                IsAggregatable = IsNumericType(property.ClrType)
            };

            fields.Add(field);
        }

        // Add navigation properties
        foreach (var navigation in entityTypeObj.GetNavigations())
        {
            // Skip collections (we only want single-valued navigations)
            if (navigation.IsCollection)
                continue;

            var targetType = navigation.TargetEntityType;
            
            // Add key properties from navigation
            foreach (var targetProp in targetType.GetProperties())
            {
                // Skip shadow props, FKs, and IDs
                if (targetProp.IsShadowProperty() || 
                    targetProp.IsForeignKey() || 
                    targetProp.IsPrimaryKey())
                    continue;

                // Dynamically include properties based on characteristics
                // EXCLUDE properties that shouldn't be in reports
                var excludedPropertyTypes = new[]
                {
                    typeof(ICollection<>),  // Collections (handled separately)
                    typeof(byte[])          // Binary data
                };
                
                var excludedPropertyNames = new[]
                {
                    "Id",                   // Primary keys
                    "CreatedAt", "CreatedDate", "CreatedByUserId", "CreatedByUser",  // Audit fields
                    "LastModified", "LastModifiedByUserId", "LastModifiedByUser",     // Audit fields
                    "IsDeleted", "DeletedAt", "DeletedByUserId",                      // Soft delete fields
                    "Latitude", "Longitude",                                          // Coordinates (too technical)
                    "PathIds", "Level"                                                // Hierarchy fields (internal)
                };
                
                // Skip if property name is in exclusion list
                if (excludedPropertyNames.Contains(targetProp.Name))
                    continue;
                
                // Skip if property type is excluded
                var propType = targetProp.ClrType;
                var isCollection = propType.IsGenericType && 
                                 propType.GetGenericTypeDefinition() == typeof(ICollection<>);
                var isByteArray = propType == typeof(byte[]);
                
                if (isCollection || isByteArray)
                    continue;
                
                // INCLUDE all other simple value properties dynamically

                var field = new ReportFieldMetadata
                {
                    EntityType = entityType,
                    FieldPath = $"{navigation.Name}.{targetProp.Name}",
                    DisplayName = $"{GetDisplayName(navigation)} - {GetDisplayName(targetProp)}",
                    DataType = GetDataTypeName(targetProp.ClrType),
                    Category = "Related Data",
                    IsNullable = true,
                    IsNavigationProperty = true,
                    NavigationDepth = 1,
                    IsCollection = false,
                    IsPrimaryKey = false,
                    IsCustomField = false,
                    SuggestedAggregations = GetSuggestedAggregations(GetDataTypeName(targetProp.ClrType)),
                    IsFilterable = true,
                    IsGroupable = true,
                    IsAggregatable = false
                };

                fields.Add(field);
            }
        }

        // Add collection navigation properties (NEW - for complex queries)
        foreach (var navigation in entityTypeObj.GetNavigations().Where(n => n.IsCollection))
        {
            var targetType = navigation.TargetEntityType;
            var collectionName = navigation.Name;

            // Get queryable sub-fields from collection element type
            var subFields = GetCollectionSubFields(targetType);

            var collectionField = new ReportFieldMetadata
            {
                EntityType = entityType,
                FieldPath = collectionName,
                DisplayName = $"{GetDisplayName(navigation)} (Collection)",
                DataType = "Collection",
                Category = "Related Collections",
                IsNullable = true,
                IsNavigationProperty = true,
                IsCollection = true,
                NavigationDepth = 1,
                CollectionElementType = targetType.ClrType.Name,
                CollectionSubFields = subFields,
                Description = $"Query related {collectionName} (e.g., 'Has any {collectionName} where...')",
                IsFilterable = true,
                IsGroupable = false,
                IsAggregatable = false,
                PerformanceWarning = subFields.Count > 10 ? "Large collection - may impact performance" : null
            };

            fields.Add(collectionField);
        }

        return fields;
    }

    /// <summary>
    /// Get queryable fields from a collection element type
    /// </summary>
    private List<string> GetCollectionSubFields(IEntityType targetType)
    {
        var subFields = new List<string>();

        foreach (var prop in targetType.GetProperties())
        {
            // Skip shadow properties, PKs, and FKs
            if (prop.IsShadowProperty() || prop.IsPrimaryKey() || prop.IsForeignKey())
                continue;

            // Skip audit fields
            if (prop.Name.StartsWith("Created") || prop.Name.StartsWith("Modified"))
                continue;

            subFields.Add(prop.Name);
        }

        // Also include related lookup names (e.g., TestType.Name for LabResults)
        foreach (var nav in targetType.GetNavigations().Where(n => !n.IsCollection))
        {
            var navTargetType = nav.TargetEntityType;
            var nameProps = navTargetType.GetProperties()
                .Where(p => p.Name == "Name" || p.Name == "DisplayName" || p.Name == "Code")
                .Select(p => $"{nav.Name}.{p.Name}");
            
            subFields.AddRange(nameProps);
        }

        return subFields;
    }

    private Type GetClrType(string entityType)
    {
        return entityType switch
        {
            // Core entities
            "Case" => typeof(Case),
            "Contact" => typeof(Case), // Contacts use Case table
            "Outbreak" => typeof(Outbreak),
            "Patient" => typeof(Patient),
            "Task" => typeof(CaseTask),
            "CaseTask" => typeof(CaseTask),
            "Location" => typeof(Location),
            "Event" => typeof(Event),
            "CaseSymptom" => typeof(CaseSymptom),
            "ExposureEvent" => typeof(ExposureEvent),
            "LabResult" => typeof(LabResult),
            
            // Flattened report views
            "CaseContactTasksFlattened" => typeof(Models.Views.CaseContactTaskFlattened),
            "OutbreakTasksFlattened" => typeof(Models.Views.OutbreakTaskFlattened),
            "CaseTimelineAll" => typeof(Models.Views.CaseTimelineEvent),
            "ContactTracingMindMapNodes" => typeof(Models.Views.ContactTracingMindMapNode),
            "ContactTracingMindMapEdges" => typeof(Models.Views.ContactTracingMindMapEdge),
            "ContactsListSimple" => typeof(Models.Views.ContactListSimple),
            
            _ => throw new NotSupportedException($"Entity type '{entityType}' is not supported")
        };
    }

    private string GetDisplayName(IReadOnlyPropertyBase property)
    {
        // Check for DisplayAttribute
        var displayAttr = property.PropertyInfo?.GetCustomAttributes(typeof(DisplayAttribute), false)
            .FirstOrDefault() as DisplayAttribute;

        if (displayAttr != null && !string.IsNullOrEmpty(displayAttr.Name))
        {
            return displayAttr.Name;
        }

        // Convert PascalCase to space-separated
        return SplitPascalCase(property.Name);
    }

    private string GetDisplayName(IReadOnlyNavigationBase navigation)
    {
        return SplitPascalCase(navigation.Name);
    }

    private string SplitPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Insert space before uppercase letters (except first char)
        var result = System.Text.RegularExpressions.Regex.Replace(
            input, 
            "([a-z])([A-Z])", 
            "$1 $2"
        );

        return result;
    }

    private string GetDataTypeName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType.IsEnum)
        {
            return underlyingType.Name;
        }

        return underlyingType.Name switch
        {
            "Int32" => "Int32",
            "Int64" => "Int64",
            "Decimal" => "Decimal",
            "Double" => "Double",
            "Boolean" => "Boolean",
            "DateTime" => "DateTime",
            "DateOnly" => "Date",
            "String" => "String",
            "Guid" => "Guid",
            _ => underlyingType.Name
        };
    }

    private string CategorizeProperty(IReadOnlyProperty property)
    {
        var name = property.Name.ToLower();

        // System fields
        if (property.IsPrimaryKey() || 
            name == "id" || 
            name == "createdat" || 
            name == "modifiedat" ||
            name == "type")
        {
            return "System";
        }

        // Identification
        if (name.Contains("friendlyid") || 
            name.Contains("number") || 
            name.Contains("code"))
        {
            return "Identification";
        }

        // Dates
        if (name.Contains("date") || 
            name.Contains("time") || 
            property.ClrType == typeof(DateTime) ||
            property.ClrType == typeof(DateTime?))
        {
            return "Dates & Times";
        }

        // Demographics
        if (name.Contains("name") || 
            name.Contains("gender") || 
            name.Contains("sex") ||
            name.Contains("age") ||
            name.Contains("birth") ||
            name.Contains("ethnicity"))
        {
            return "Demographics";
        }

        // Address
        if (name.Contains("address") || 
            name.Contains("street") || 
            name.Contains("city") ||
            name.Contains("postcode") ||
            name.Contains("state") ||
            name.Contains("country"))
        {
            return "Address";
        }

        // Contact info
        if (name.Contains("phone") || 
            name.Contains("email") || 
            name.Contains("mobile"))
        {
            return "Contact Information";
        }

        // Jurisdiction
        if (name.Contains("jurisdiction"))
        {
            return "Jurisdiction";
        }

        // Clinical
        if (name.Contains("diagnosis") || 
            name.Contains("symptom") || 
            name.Contains("hospital") ||
            name.Contains("deceased") ||
            name.Contains("outcome") ||
            name.Contains("classification") ||
            name.Contains("status"))
        {
            return "Clinical";
        }

        // Laboratory
        if (name.Contains("test") || 
            name.Contains("specimen") || 
            name.Contains("lab") ||
            name.Contains("result"))
        {
            return "Laboratory";
        }

        // Default
        return "General";
    }

    private bool IsNumericType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(decimal) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float);
    }

    private int GetCategoryOrder(string category)
    {
        // Define sort order for categories
        return category switch
        {
            "System" => 0,
            "Identification" => 1,
            "Dates & Times" => 2,
            "Demographics" => 3,
            "Address" => 4,
            "Contact Information" => 5,
            "Jurisdiction" => 6,
            "Clinical" => 7,
            "Laboratory" => 8,
            "Outbreak" => 9,
            "Custom Fields" => 10,
            "Navigation Properties" => 11,
            "Foreign Keys" => 12,
            "Audit" => 13,
            _ => 99
        };
    }

    private string GetCustomFieldDataType(CustomFieldType fieldType)
    {
        return fieldType switch
        {
            CustomFieldType.Checkbox => "Boolean",
            CustomFieldType.Number => "Decimal",
            CustomFieldType.Date => "DateTime",
            CustomFieldType.Dropdown => "String",
            CustomFieldType.Text => "String",
            CustomFieldType.TextArea => "String",
            CustomFieldType.Email => "String",
            CustomFieldType.Phone => "String",
            _ => "String"
        };
    }

    private List<string> GetSuggestedAggregationsForCustomField(CustomFieldType fieldType)
    {
        return fieldType switch
        {
            CustomFieldType.Number => new List<string> { "Count", "Sum", "Average", "Min", "Max", "DistinctCount" },
            CustomFieldType.Date => new List<string> { "Count", "Min", "Max", "DistinctCount" },
            CustomFieldType.Checkbox => new List<string> { "Count", "DistinctCount" },
            _ => new List<string> { "Count", "DistinctCount" }
        };
    }
    
    /// <summary>
    /// Get enum values with display names from [Display] attributes
    /// </summary>
    private List<EnumValueOption> GetEnumValues(Type enumType)
    {
        var values = new List<EnumValueOption>();
        
        foreach (var value in Enum.GetValues(enumType))
        {
            var memberInfo = enumType.GetMember(value.ToString()!).FirstOrDefault();
            var displayAttribute = memberInfo?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .FirstOrDefault() as DisplayAttribute;
            
            values.Add(new EnumValueOption
            {
                Value = value.ToString()!,
                DisplayName = displayAttribute?.Name ?? value.ToString()!,
                Code = Convert.ToInt32(value)
            });
        }
        
        return values;
    }

    #endregion
}

