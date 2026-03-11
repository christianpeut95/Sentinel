using Sentinel.DTOs;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Service to provide metadata about collections (related entities) for reporting
/// Defines which operations and fields are available for each collection
/// </summary>
public interface ICollectionMetadataService
{
    /// <summary>
    /// Get metadata for all collections available for an entity type
    /// </summary>
    Dictionary<string, CollectionMetadata> GetCollectionMetadata(string entityType);

    /// <summary>
    /// Get metadata for a specific collection
    /// </summary>
    CollectionMetadata? GetCollectionMetadata(string entityType, string collectionName);
}

public class CollectionMetadataService : ICollectionMetadataService
{
    public Dictionary<string, CollectionMetadata> GetCollectionMetadata(string entityType)
    {
        return entityType switch
        {
            "Case" => GetCaseCollections(),
            "Contact" => GetCaseCollections(), // Same as Case
            "Outbreak" => GetOutbreakCollections(),
            "Patient" => GetPatientCollections(),
            _ => new Dictionary<string, CollectionMetadata>()
        };
    }

    public CollectionMetadata? GetCollectionMetadata(string entityType, string collectionName)
    {
        var collections = GetCollectionMetadata(entityType);
        return collections.GetValueOrDefault(collectionName);
    }

    private Dictionary<string, CollectionMetadata> GetCaseCollections()
    {
        return new Dictionary<string, CollectionMetadata>
        {
            ["LabResults"] = new()
            {
                Label = "Lab Results",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count", "Min", "Max", "Sum", "Average" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>
                {
                    ["SpecimenCollectionDate"] = new()
                    {
                        Label = "Specimen Collection Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["TestDate"] = new()
                    {
                        Label = "Test Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["QuantitativeResult"] = new()
                    {
                        Label = "Quantitative Result",
                        DataType = "Decimal",
                        AllowedOperations = new List<string> { "Sum", "Average", "Min", "Max" }
                    }
                },
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "TestType.Name", Label = "Test Type", DataType = "String" },
                    new() { Name = "TestResult.Name", Label = "Test Result", DataType = "String" },
                    new() { Name = "SpecimenType.Name", Label = "Specimen Type", DataType = "String" },
                    new() { Name = "SpecimenCollectionDate", Label = "Collection Date", DataType = "DateTime" },
                    new() { Name = "QuantitativeResult", Label = "Quantitative Result", DataType = "Decimal" }
                }
            },
            
            ["ExposureEvents"] = new()
            {
                Label = "Exposures",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count", "Min", "Max" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>
                {
                    ["ExposureStartDate"] = new()
                    {
                        Label = "Exposure Start Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["ExposureEndDate"] = new()
                    {
                        Label = "Exposure End Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    }
                },
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "ExposureType", Label = "Exposure Type", DataType = "Enum" },
                    new() { Name = "Location.Name", Label = "Location", DataType = "String" },
                    new() { Name = "Event.Name", Label = "Event", DataType = "String" },
                    new() { Name = "ExposureStartDate", Label = "Start Date", DataType = "DateTime" },
                    new() { Name = "ExposureEndDate", Label = "End Date", DataType = "DateTime" }
                }
            },
            
            ["CaseTasks"] = new()
            {
                Label = "Tasks",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count", "Min", "Max" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>
                {
                    ["DueDate"] = new()
                    {
                        Label = "Due Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["CreatedAt"] = new()
                    {
                        Label = "Created Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["CompletedAt"] = new()
                    {
                        Label = "Completed Date",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    }
                },
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "TaskType.Name", Label = "Task Type", DataType = "String" },
                    new() { Name = "Status", Label = "Status", DataType = "Enum" },
                    new() { Name = "Priority", Label = "Priority", DataType = "Enum" },
                    new() { Name = "AssignedToUser.Email", Label = "Assigned To", DataType = "String" },
                    new() { Name = "DueDate", Label = "Due Date", DataType = "DateTime" }
                }
            },
            
            ["DiseaseSymptoms"] = new()
            {
                Label = "Symptoms",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count" }, // No aggregation
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>(), // No aggregatable fields
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "Symptom.Name", Label = "Symptom", DataType = "String" },
                    new() { Name = "Severity", Label = "Severity", DataType = "Enum" },
                    new() { Name = "OnsetDate", Label = "Onset Date", DataType = "DateTime" }
                }
            },
            
            ["Contacts"] = new()
            {
                Label = "Contacts",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>(),
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "ContactType", Label = "Contact Type", DataType = "Enum" },
                    new() { Name = "DateIdentified", Label = "Date Identified", DataType = "DateTime" }
                }
            }
        };
    }

    private Dictionary<string, CollectionMetadata> GetOutbreakCollections()
    {
        return new Dictionary<string, CollectionMetadata>
        {
            ["Cases"] = new()
            {
                Label = "Cases",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>(),
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "ConfirmationStatus.Name", Label = "Status", DataType = "String" },
                    new() { Name = "DateOfOnset", Label = "Date of Onset", DataType = "DateTime" }
                }
            }
        };
    }

    private Dictionary<string, CollectionMetadata> GetPatientCollections()
    {
        return new Dictionary<string, CollectionMetadata>
        {
            ["Cases"] = new()
            {
                Label = "Cases",
                AllowedOperations = new List<string> { "HasAny", "HasAll", "Count", "Min", "Max" },
                AggregatableFields = new Dictionary<string, AggregatableFieldInfo>
                {
                    ["DateOfOnset"] = new()
                    {
                        Label = "Date of Onset",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    },
                    ["DateOfNotification"] = new()
                    {
                        Label = "Date of Notification",
                        DataType = "DateTime",
                        AllowedOperations = new List<string> { "Min", "Max" }
                    }
                },
                FilterableFields = new List<CollectionFieldInfo>
                {
                    new() { Name = "Disease.Name", Label = "Disease", DataType = "String" },
                    new() { Name = "ConfirmationStatus.Name", Label = "Status", DataType = "String" },
                    new() { Name = "DateOfOnset", Label = "Date of Onset", DataType = "DateTime" }
                }
            }
        };
    }
}
