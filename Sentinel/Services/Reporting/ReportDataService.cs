using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.DTOs;
using System.Linq.Dynamic.Core;
using System.Text.Json;

namespace Sentinel.Services.Reporting;

/// <summary>
/// Service for extracting data from database for reports
/// Handles custom fields (EAV pattern), navigation properties, and dynamic filtering
/// </summary>
public class ReportDataService : IReportDataService
{
    private readonly ApplicationDbContext _context;
    private readonly IReportFieldMetadataService _fieldMetadataService;

    public ReportDataService(
        ApplicationDbContext context,
        IReportFieldMetadataService fieldMetadataService)
    {
        _context = context;
        _fieldMetadataService = fieldMetadataService;
    }

    public async Task<List<Dictionary<string, object?>>> GetReportDataAsync(ReportDefinition reportDefinition)
    {
        var options = new DataExtractionOptions
        {
            IncludeCustomFields = true,
            IncludeNavigationProperties = true
        };

        return await ExtractDataAsync(reportDefinition, options);
    }

    public async Task<List<Dictionary<string, object?>>> GetReportPreviewAsync(ReportDefinition reportDefinition)
    {
        var options = new DataExtractionOptions
        {
            MaxRows = 100,
            IncludeCustomFields = true,
            IncludeNavigationProperties = true
        };

        return await ExtractDataAsync(reportDefinition, options);
    }

    public async Task<List<Dictionary<string, object?>>> GetReportPreviewAsync(
        ReportDefinition reportDefinition,
        List<CollectionQueryDto> collectionQueries)
    {
        // First get the base data
        var data = await GetReportPreviewAsync(reportDefinition);
        
        // Then add collection query columns
        if (collectionQueries?.Any() == true)
        {
            data = await AddCollectionColumnsAsync(data, collectionQueries, reportDefinition.EntityType);
        }
        
        return data;
    }


    public async Task<List<Dictionary<string, object?>>> GetAggregatedDataAsync(
        ReportDefinition reportDefinition,
        DateTime startDate,
        DateTime endDate,
        string groupByField)
    {
        // Add date range filter
        var dateFilter = new ReportFilter
        {
            FieldPath = "OnsetDate", // Or whichever date field
            Operator = "Between",
            Value = $"{startDate:yyyy-MM-dd}|{endDate:yyyy-MM-dd}"
        };

        // Temporarily add filter
        var originalFilters = reportDefinition.Filters.ToList();
        reportDefinition.Filters.Add(dateFilter);

        try
        {
            var data = await GetReportDataAsync(reportDefinition);
            return data;
        }
        finally
        {
            // Restore original filters
            reportDefinition.Filters = originalFilters;
        }
    }

    public async Task<(bool isValid, string? errorMessage)> ValidateReportDefinitionAsync(ReportDefinition reportDefinition)
    {
        try
        {
            // Validate entity type
            if (string.IsNullOrEmpty(reportDefinition.EntityType))
            {
                return (false, "Entity type is required");
            }

            // Validate fields exist
            foreach (var field in reportDefinition.Fields)
            {
                var isValid = await _fieldMetadataService.ValidateFieldPathAsync(
                    reportDefinition.EntityType,
                    field.FieldPath);

                if (!isValid)
                {
                    return (false, $"Field '{field.FieldPath}' does not exist or is not accessible");
                }
            }

            // Validate filters
            foreach (var filter in reportDefinition.Filters)
            {
                var isValid = await _fieldMetadataService.ValidateFieldPathAsync(
                    reportDefinition.EntityType,
                    filter.FieldPath);

                if (!isValid)
                {
                    return (false, $"Filter field '{filter.FieldPath}' does not exist or is not accessible");
                }
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Validation error: {ex.Message}");
        }
    }

    public async Task<int> GetReportRowCountAsync(ReportDefinition reportDefinition)
    {
        // Load custom field definitions upfront if needed
        Dictionary<int, CustomFieldDefinition>? customFieldDefinitions = null;
        var customFieldFilters = reportDefinition.Filters.Where(f => f.IsCustomField).ToList();
        if (customFieldFilters.Any())
        {
            var customFieldIds = customFieldFilters
                .Where(f => f.CustomFieldDefinitionId.HasValue)
                .Select(f => f.CustomFieldDefinitionId!.Value)
                .Distinct()
                .ToList();

            if (customFieldIds.Any())
            {
                customFieldDefinitions = await _context.CustomFieldDefinitions
                    .Where(cfd => customFieldIds.Contains(cfd.Id))
                    .ToDictionaryAsync(cfd => cfd.Id, cfd => cfd);
            }
        }

        // Build base query
        var baseQuery = BuildBaseQuery(reportDefinition.EntityType);
        
        // Apply filters (synchronous, uses pre-loaded definitions)
        baseQuery = ApplyFiltersAsync(baseQuery, reportDefinition, customFieldDefinitions);

        return await baseQuery.CountAsync();
    }

    #region Private Methods

    private async Task<List<Dictionary<string, object?>>> ExtractDataAsync(
        ReportDefinition reportDefinition,
        DataExtractionOptions options)
    {
        Console.WriteLine($"[ReportData] Starting data extraction for {reportDefinition.EntityType}");

        // STEP 1: Load ALL metadata upfront (completes all DB operations)
        var fieldMetadata = await GetFieldMetadataForReport(reportDefinition);
        
        // STEP 2: Load custom field definitions upfront if needed
        Dictionary<int, CustomFieldDefinition>? customFieldDefinitions = null;
        var customFieldFilters = reportDefinition.Filters.Where(f => f.IsCustomField).ToList();
        if (customFieldFilters.Any())
        {
            var customFieldIds = customFieldFilters
                .Where(f => f.CustomFieldDefinitionId.HasValue)
                .Select(f => f.CustomFieldDefinitionId!.Value)
                .Distinct()
                .ToList();

            if (customFieldIds.Any())
            {
                customFieldDefinitions = await _context.CustomFieldDefinitions
                    .Where(cfd => customFieldIds.Contains(cfd.Id))
                    .ToDictionaryAsync(cfd => cfd.Id, cfd => cfd);
                
                Console.WriteLine($"[ReportData] Loaded {customFieldDefinitions.Count} custom field definitions");
            }
        }

        // STEP 3: Now build queries (no more DB operations from this point)
        var baseQuery = BuildBaseQuery(reportDefinition.EntityType);
        Console.WriteLine($"[ReportData] Base query built for {reportDefinition.EntityType}");

        // STEP 4: Apply filters (passes pre-loaded custom field definitions)
        baseQuery = ApplyFiltersAsync(baseQuery, reportDefinition, customFieldDefinitions);
        Console.WriteLine($"[ReportData] Filters applied: {reportDefinition.Filters.Count}");

        // STEP 5: Apply row limit if specified
        if (options.MaxRows.HasValue)
        {
            baseQuery = baseQuery.Take(options.MaxRows.Value);
            Console.WriteLine($"[ReportData] Row limit applied: {options.MaxRows.Value}");
        }

        // STEP 6: Execute query based on entity type
        var result = new List<Dictionary<string, object?>>();

        switch (reportDefinition.EntityType)
        {
            case "Case":
                result = await ExtractCaseDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Contact":
                result = await ExtractCaseDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Outbreak":
                result = await ExtractOutbreakDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Patient":
                result = await ExtractPatientDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Task":
                result = await ExtractTaskDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Location":
                result = await ExtractLocationDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            case "Event":
                result = await ExtractEventDataAsync(baseQuery, reportDefinition, fieldMetadata, options);
                break;
            
            // Flattened views (no custom fields - use generic extraction)
            case "CaseContactTasksFlattened":
            case "OutbreakTasksFlattened":
            case "CaseTimelineAll":
            case "ContactTracingMindMapNodes":
            case "ContactTracingMindMapEdges":
            case "ContactsListSimple":
                result = await ExtractGenericDataAsync(baseQuery, reportDefinition, fieldMetadata);
                break;
                
            default:
                throw new NotSupportedException($"Entity type '{reportDefinition.EntityType}' is not supported");
        }
        
        Console.WriteLine($"[ReportData] Extracted {result.Count} rows with {reportDefinition.Fields.Count} fields each");

        return result;
    }

    private IQueryable<object> BuildBaseQuery(string entityType)
    {
        return entityType switch
        {
            // Core entities - Filter to only actual cases (exclude contacts which are also stored in Cases table)
            "Case" => _context.Cases
                .Where(c => c.Type == CaseType.Case)
                .AsQueryable()
                .Cast<object>(),
            
            // Filter to only contacts
            "Contact" => _context.Cases
                .Where(c => c.Type == CaseType.Contact)
                .AsQueryable()
                .Cast<object>(),
            
            "Outbreak" => _context.Outbreaks.AsQueryable().Cast<object>(),
            "Patient" => _context.Patients.AsQueryable().Cast<object>(),
            "Task" => _context.CaseTasks.AsQueryable().Cast<object>(),
            "Location" => _context.Locations.AsQueryable().Cast<object>(),
            "Event" => _context.Events.AsQueryable().Cast<object>(),
            
            // Flattened report views (no soft delete filtering - already in view)
            "CaseContactTasksFlattened" => _context.CaseContactTasksFlattened.AsQueryable().Cast<object>(),
            "OutbreakTasksFlattened" => _context.OutbreakTasksFlattened.AsQueryable().Cast<object>(),
            "CaseTimelineAll" => _context.CaseTimelineAll.AsQueryable().Cast<object>(),
            "ContactTracingMindMapNodes" => _context.ContactTracingMindMapNodes.AsQueryable().Cast<object>(),
            "ContactTracingMindMapEdges" => _context.ContactTracingMindMapEdges.AsQueryable().Cast<object>(),
            "ContactsListSimple" => _context.ContactsListSimple.AsQueryable().Cast<object>(),
            
            _ => throw new NotSupportedException($"Entity type '{entityType}' is not supported")
        };
    }

    private IQueryable<object> ApplyFiltersAsync(
        IQueryable<object> query,
        ReportDefinition reportDefinition,
        Dictionary<int, CustomFieldDefinition>? customFieldDefinitions)
    {
        if (!reportDefinition.Filters.Any())
            return query;

        // Separate custom field filters from regular filters
        var regularFilters = reportDefinition.Filters.Where(f => !f.IsCustomField).ToList();
        var customFieldFilters = reportDefinition.Filters.Where(f => f.IsCustomField).ToList();

        // Apply regular filters first (these use Dynamic LINQ on the entity)
        if (regularFilters.Any())
        {
            query = ApplyRegularFilters(query, regularFilters, reportDefinition.EntityType);
        }

        // Apply custom field filters (using pre-loaded definitions)
        if (customFieldFilters.Any() && customFieldDefinitions != null)
        {
            query = ApplyCustomFieldFilters(query, customFieldFilters, reportDefinition.EntityType, customFieldDefinitions);
        }

        return query;
    }

    private IQueryable<object> ApplyRegularFilters(
        IQueryable<object> query,
        List<ReportFilter> filters,
        string entityType)
    {
        // Group filters by GroupId
        var groupedFilters = filters
            .OrderBy(f => f.GroupId ?? int.MaxValue)
            .ThenBy(f => f.DisplayOrder)
            .GroupBy(f => f.GroupId)
            .ToList();

        foreach (var group in groupedFilters)
        {
            var groupFilters = group.ToList();
            
            if (group.Key.HasValue)
            {
                // Filters in a group - apply as one combined condition
                query = ApplyFilterGroup(query, groupFilters, entityType);
            }
            else
            {
                // Ungrouped filters - apply individually
                foreach (var filter in groupFilters)
                {
                    query = ApplyFilter(query, filter, entityType);
                }
            }
        }

        return query;
    }

    private IQueryable<object> ApplyCustomFieldFilters(
        IQueryable<object> query,
        List<ReportFilter> customFilters,
        string entityType,
        Dictionary<int, CustomFieldDefinition> customFieldDefinitions)
    {
        try
        {
            // Cast to specific type
            var clrType = GetClrType(entityType);
            
            foreach (var filter in customFilters.OrderBy(f => f.DisplayOrder))
            {
                query = ApplyCustomFieldFilter(query, filter, entityType, clrType, customFieldDefinitions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying custom field filters: {ex.Message}");
        }

        return query;
    }

    private IQueryable<object> ApplyCustomFieldFilter(
        IQueryable<object> query,
        ReportFilter filter,
        string entityType,
        Type clrType,
        Dictionary<int, CustomFieldDefinition> customFieldDefinitions)
    {
        if (!filter.CustomFieldDefinitionId.HasValue)
        {
            Console.WriteLine($"Warning: Custom field filter missing CustomFieldDefinitionId");
            return query;
        }

        var customFieldId = filter.CustomFieldDefinitionId.Value;
        var filterValue = filter.Value ?? "";

        // Get the custom field definition from the pre-loaded dictionary
        if (!customFieldDefinitions.TryGetValue(customFieldId, out var customFieldDef))
        {
            Console.WriteLine($"Warning: Custom field definition {customFieldId} not found");
            return query;
        }

        // Apply filter based on entity type and custom field type
        if (entityType == "Case" || entityType == "Contact")
        {
            query = ApplyCaseCustomFieldFilter(query, customFieldId, customFieldDef.FieldType, filter.Operator, filterValue);
        }
        else if (entityType == "Patient")
        {
            query = ApplyPatientCustomFieldFilter(query, customFieldId, customFieldDef.FieldType, filter.Operator, filterValue);
        }

        return query;
    }

    private IQueryable<object> ApplyCaseCustomFieldFilter(
        IQueryable<object> query,
        int customFieldId,
        CustomFieldType fieldType,
        string operatorType,
        string value)
    {
        var caseQuery = query.Cast<Case>();

        // Filter based on custom field type
        switch (fieldType)
        {
            case CustomFieldType.Text:
            case CustomFieldType.TextArea:
            case CustomFieldType.Email:
            case CustomFieldType.Phone:
                caseQuery = ApplyCaseStringFilter(caseQuery, customFieldId, operatorType, value);
                break;

            case CustomFieldType.Number:
                if (decimal.TryParse(value, out var numericValue))
                {
                    caseQuery = ApplyCaseNumberFilter(caseQuery, customFieldId, operatorType, numericValue);
                }
                break;

            case CustomFieldType.Date:
                if (DateTime.TryParse(value, out var dateValue))
                {
                    caseQuery = ApplyCaseDateFilter(caseQuery, customFieldId, operatorType, dateValue);
                }
                break;

            case CustomFieldType.Checkbox:
                if (bool.TryParse(value, out var boolValue))
                {
                    caseQuery = ApplyCaseBooleanFilter(caseQuery, customFieldId, operatorType, boolValue);
                }
                break;

            case CustomFieldType.Dropdown:
                if (int.TryParse(value, out var lookupId))
                {
                    caseQuery = ApplyCaseLookupFilter(caseQuery, customFieldId, operatorType, lookupId);
                }
                break;
        }

        return caseQuery.Cast<object>();
    }

    private IQueryable<Case> ApplyCaseStringFilter(IQueryable<Case> query, int fieldId, string op, string value)
    {
        return op switch
        {
            "Equals" => query.Where(c => _context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(c => !_context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "Contains" => query.Where(c => _context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.Contains(value))),
            
            "NotContains" => query.Where(c => !_context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.Contains(value))),
            
            "StartsWith" => query.Where(c => _context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.StartsWith(value))),
            
            "EndsWith" => query.Where(c => _context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.EndsWith(value))),
            
            "IsNull" => query.Where(c => !_context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(c => _context.CaseCustomFieldStrings
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Case> ApplyCaseNumberFilter(IQueryable<Case> query, int fieldId, string op, decimal value)
    {
        return op switch
        {
            "Equals" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value != value)),
            
            "GreaterThan" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value > value)),
            
            "LessThan" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value < value)),
            
            "GreaterThanOrEqual" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value >= value)),
            
            "LessThanOrEqual" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value <= value)),
            
            "IsNull" => query.Where(c => !_context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(c => _context.CaseCustomFieldNumbers
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Case> ApplyCaseDateFilter(IQueryable<Case> query, int fieldId, string op, DateTime value)
    {
        return op switch
        {
            "Equals" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value.Date == value.Date)),
            
            "NotEquals" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value.Date != value.Date)),
            
            "GreaterThan" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value > value)),
            
            "LessThan" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value < value)),
            
            "GreaterThanOrEqual" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value >= value)),
            
            "LessThanOrEqual" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value <= value)),
            
            "IsNull" => query.Where(c => !_context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(c => _context.CaseCustomFieldDates
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Case> ApplyCaseBooleanFilter(IQueryable<Case> query, int fieldId, string op, bool value)
    {
        return op switch
        {
            "Equals" => query.Where(c => _context.CaseCustomFieldBooleans
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(c => _context.CaseCustomFieldBooleans
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.Value != value)),
            
            "IsNull" => query.Where(c => !_context.CaseCustomFieldBooleans
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(c => _context.CaseCustomFieldBooleans
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Case> ApplyCaseLookupFilter(IQueryable<Case> query, int fieldId, string op, int lookupId)
    {
        return op switch
        {
            "Equals" => query.Where(c => _context.CaseCustomFieldLookups
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.LookupValueId == lookupId)),
            
            "NotEquals" => query.Where(c => _context.CaseCustomFieldLookups
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId && cf.LookupValueId != lookupId)),
            
            "IsNull" => query.Where(c => !_context.CaseCustomFieldLookups
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(c => _context.CaseCustomFieldLookups
                .Any(cf => cf.CaseId == c.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<object> ApplyPatientCustomFieldFilter(
        IQueryable<object> query,
        int customFieldId,
        CustomFieldType fieldType,
        string operatorType,
        string value)
    {
        var patientQuery = query.Cast<Patient>();

        // Similar implementation for Patient custom fields
        switch (fieldType)
        {
            case CustomFieldType.Text:
            case CustomFieldType.TextArea:
            case CustomFieldType.Email:
            case CustomFieldType.Phone:
                patientQuery = ApplyPatientStringFilter(patientQuery, customFieldId, operatorType, value);
                break;

            case CustomFieldType.Number:
                if (decimal.TryParse(value, out var numericValue))
                {
                    patientQuery = ApplyPatientNumberFilter(patientQuery, customFieldId, operatorType, numericValue);
                }
                break;

            case CustomFieldType.Date:
                if (DateTime.TryParse(value, out var dateValue))
                {
                    patientQuery = ApplyPatientDateFilter(patientQuery, customFieldId, operatorType, dateValue);
                }
                break;

            case CustomFieldType.Checkbox:
                if (bool.TryParse(value, out var boolValue))
                {
                    patientQuery = ApplyPatientBooleanFilter(patientQuery, customFieldId, operatorType, boolValue);
                }
                break;

            case CustomFieldType.Dropdown:
                if (int.TryParse(value, out var lookupId))
                {
                    patientQuery = ApplyPatientLookupFilter(patientQuery, customFieldId, operatorType, lookupId);
                }
                break;
        }

        return patientQuery.Cast<object>();
    }

    private IQueryable<Patient> ApplyPatientStringFilter(IQueryable<Patient> query, int fieldId, string op, string value)
    {
        return op switch
        {
            "Equals" => query.Where(p => _context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(p => !_context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "Contains" => query.Where(p => _context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.Contains(value))),
            
            "NotContains" => query.Where(p => !_context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.Contains(value))),
            
            "StartsWith" => query.Where(p => _context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.StartsWith(value))),
            
            "EndsWith" => query.Where(p => _context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.EndsWith(value))),
            
            "IsNull" => query.Where(p => !_context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(p => _context.PatientCustomFieldStrings
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Patient> ApplyPatientNumberFilter(IQueryable<Patient> query, int fieldId, string op, decimal value)
    {
        return op switch
        {
            "Equals" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value != value)),
            
            "GreaterThan" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value > value)),
            
            "LessThan" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value < value)),
            
            "GreaterThanOrEqual" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value >= value)),
            
            "LessThanOrEqual" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value <= value)),
            
            "IsNull" => query.Where(p => !_context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(p => _context.PatientCustomFieldNumbers
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Patient> ApplyPatientDateFilter(IQueryable<Patient> query, int fieldId, string op, DateTime value)
    {
        return op switch
        {
            "Equals" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value.Date == value.Date)),
            
            "NotEquals" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value.Date != value.Date)),
            
            "GreaterThan" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value > value)),
            
            "LessThan" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value < value)),
            
            "GreaterThanOrEqual" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value >= value)),
            
            "LessThanOrEqual" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value.HasValue && cf.Value.Value <= value)),
            
            "IsNull" => query.Where(p => !_context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(p => _context.PatientCustomFieldDates
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Patient> ApplyPatientBooleanFilter(IQueryable<Patient> query, int fieldId, string op, bool value)
    {
        return op switch
        {
            "Equals" => query.Where(p => _context.PatientCustomFieldBooleans
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value == value)),
            
            "NotEquals" => query.Where(p => _context.PatientCustomFieldBooleans
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.Value != value)),
            
            "IsNull" => query.Where(p => !_context.PatientCustomFieldBooleans
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(p => _context.PatientCustomFieldBooleans
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<Patient> ApplyPatientLookupFilter(IQueryable<Patient> query, int fieldId, string op, int lookupId)
    {
        return op switch
        {
            "Equals" => query.Where(p => _context.PatientCustomFieldLookups
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.LookupValueId == lookupId)),
            
            "NotEquals" => query.Where(p => _context.PatientCustomFieldLookups
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId && cf.LookupValueId != lookupId)),
            
            "IsNull" => query.Where(p => !_context.PatientCustomFieldLookups
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            "IsNotNull" => query.Where(p => _context.PatientCustomFieldLookups
                .Any(cf => cf.PatientId == p.Id && cf.FieldDefinitionId == fieldId)),
            
            _ => query
        };
    }

    private IQueryable<object> ApplyFilterGroup(
        IQueryable<object> query,
        List<ReportFilter> groupFilters,
        string entityType)
    {
        try
        {
            // Cast query back to specific type for filtering
            var clrType = GetClrType(entityType);
            var castMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(clrType);
            var typedQuery = castMethod.Invoke(null, new object[] { query });

            // Build combined clause for the group
            var filterClauses = new List<string>();
            
            for (int i = 0; i < groupFilters.Count; i++)
            {
                var filter = groupFilters[i];
                var clause = BuildWhereClause(filter, entityType);
                
                if (!string.IsNullOrEmpty(clause))
                {
                    filterClauses.Add(clause);
                    
                    // Add logic operator if not last filter
                    if (i < groupFilters.Count - 1)
                    {
                        filterClauses.Add(filter.LogicOperator == "OR" ? "||" : "&&");
                    }
                }
            }

            if (filterClauses.Any())
            {
                // Combine all clauses into one group: (clause1 AND/OR clause2 AND/OR clause3)
                var combinedClause = "(" + string.Join(" ", filterClauses) + ")";
                
                Console.WriteLine($"[Filter] Group clause: {combinedClause}");

                // Apply combined group clause
                var whereMethod = typeof(System.Linq.Dynamic.Core.DynamicQueryableExtensions)
                    .GetMethod("Where", new[] { typeof(IQueryable), typeof(string), typeof(object[]) })!;

                typedQuery = whereMethod.Invoke(null, new object[] { typedQuery!, combinedClause, Array.Empty<object>() });
            }

            // Cast back to object
            var castToObjectMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(typeof(object));
            query = (IQueryable<object>)castToObjectMethod.Invoke(null, new[] { typedQuery })!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error applying filter group: {ex.Message}");
        }

        return query;
    }

    private IQueryable<object> ApplyFilter(
        IQueryable<object> query,
        ReportFilter filter,
        string entityType)
    {
        try
        {
            // Handle collection queries (NEW)
            if (filter.IsCollectionQuery)
            {
                return ApplyCollectionFilter(query, filter, entityType);
            }

            // Regular filter logic (existing)
            // Cast query back to specific type for filtering
            var clrType = GetClrType(entityType);
            var castMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(clrType);
            var typedQuery = castMethod.Invoke(null, new object[] { query });
            
            // Build dynamic LINQ expression based on operator
            var whereClause = BuildWhereClause(filter, entityType);
            
            if (!string.IsNullOrEmpty(whereClause))
            {
                // Apply where clause to typed query
                var whereMethod = typeof(System.Linq.Dynamic.Core.DynamicQueryableExtensions)
                    .GetMethod("Where", new[] { typeof(IQueryable), typeof(string), typeof(object[]) })!;
                
                typedQuery = whereMethod.Invoke(null, new object[] { typedQuery!, whereClause, Array.Empty<object>() });
            }
            
            // Cast back to object
            var castToObjectMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(typeof(object));
            query = (IQueryable<object>)castToObjectMethod.Invoke(null, new[] { typedQuery })!;
        }
        catch (Exception ex)
        {
            // Log filter application error WITH inner exception details
            Console.WriteLine($"Error applying filter {filter.FieldPath}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Stack Trace: {ex.InnerException.StackTrace}");
            }
        }

        return query;
    }

    private string BuildWhereClause(ReportFilter filter, string entityType)
    {
        var fieldPath = filter.FieldPath;
        var value = filter.Value ?? "";
        var dataType = filter.DataType ?? "String";

        // Escape quotes in value for string comparisons
        if (dataType == "String")
        {
            value = value.Replace("\"", "\\\"");
        }

        // Determine if the field is nullable
        bool isNullable = IsFieldNullable(entityType, fieldPath);

        // Handle navigation properties (e.g., "Disease.Name")
        var fieldExpression = fieldPath.Contains(".") 
            ? fieldPath 
            : fieldPath;

        // Build appropriate comparison based on data type
        string whereClause = "";
        if (dataType == "DateTime" || dataType == "Date" || dataType == "DateOnly")
        {
            whereClause = BuildDateWhereClause(fieldExpression, filter.Operator, value, isNullable);
        }
        else if (dataType == "Int32" || dataType == "Decimal" || dataType == "Double" || dataType == "Number")
        {
            whereClause = BuildNumericWhereClause(fieldExpression, filter.Operator, value);
        }
        else
        {
            whereClause = BuildStringWhereClause(fieldExpression, filter.Operator, value);
        }
        
        // Log the generated WHERE clause for debugging
        Console.WriteLine($"[WHERE CLAUSE] Field: {fieldPath}, Operator: {filter.Operator}, Value: {value}, DataType: {dataType}, IsNullable: {isNullable}");
        Console.WriteLine($"[WHERE CLAUSE] Generated: {whereClause}");
        
        return whereClause;
    }

    private bool IsFieldNullable(string entityType, string fieldPath)
    {
        try
        {
            var clrType = GetClrType(entityType);
            var property = clrType.GetProperty(fieldPath);
            
            if (property == null)
                return true; // Default to nullable for safety
            
            var propertyType = property.PropertyType;
            
            // Check if it's a nullable value type (e.g., DateTime?, int?)
            return Nullable.GetUnderlyingType(propertyType) != null || !propertyType.IsValueType;
        }
        catch
        {
            return true; // Default to nullable for safety
        }
    }

    /// <summary>
    /// Apply collection filter (e.g., LabResults.Any(...), Tasks.Count() > 5)
    /// </summary>
    private IQueryable<object> ApplyCollectionFilter(
        IQueryable<object> query,
        ReportFilter filter,
        string entityType)
    {
        try
        {
            var collectionPath = filter.FieldPath; // e.g., "LabResults", "Tasks"
            var collectionOperator = filter.CollectionOperator ?? "HasAny";
            
            Console.WriteLine($"[Collection Filter] {collectionPath} ? {collectionOperator}");

            // Parse sub-filters from JSON
            var subFilters = string.IsNullOrEmpty(filter.CollectionSubFilters)
                ? new List<CollectionSubFilter>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CollectionSubFilter>>(filter.CollectionSubFilters);

            // Build Dynamic LINQ where clause
            var whereClause = BuildCollectionWhereClause(collectionPath, collectionOperator, subFilters, filter);

            if (string.IsNullOrEmpty(whereClause))
            {
                Console.WriteLine($"[Collection Filter] Empty where clause, skipping");
                return query;
            }

            Console.WriteLine($"[Collection Filter] Generated: {whereClause}");

            // Apply to typed query
            var clrType = GetClrType(entityType);
            var castMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(clrType);
            var typedQuery = castMethod.Invoke(null, new object[] { query });

            var whereMethod = typeof(System.Linq.Dynamic.Core.DynamicQueryableExtensions)
                .GetMethod("Where", new[] { typeof(IQueryable), typeof(string), typeof(object[]) })!;

            typedQuery = whereMethod.Invoke(null, new object[] { typedQuery!, whereClause, Array.Empty<object>() });

            var castToObjectMethod = typeof(Queryable).GetMethod("Cast")!.MakeGenericMethod(typeof(object));
            return (IQueryable<object>)castToObjectMethod.Invoke(null, new[] { typedQuery })!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Collection Filter] Error: {ex.Message}");
            return query;
        }
    }

    /// <summary>
    /// Build Dynamic LINQ where clause for collection queries
    /// </summary>
    private string BuildCollectionWhereClause(
        string collectionPath,
        string collectionOperator,
        List<CollectionSubFilter>? subFilters,
        ReportFilter filter)
    {
        // Build sub-filter conditions
        var subFilterConditions = subFilters?.Select(sf =>
            BuildSubFilterCondition(sf.Field, sf.Operator, sf.Value, sf.DataType)
        ).Where(c => !string.IsNullOrEmpty(c)).ToList() ?? new List<string>();

        var combinedCondition = subFilterConditions.Any()
            ? string.Join(" && ", subFilterConditions)
            : "true"; // No sub-filters means "any" item in collection

        // Build collection query based on operator
        return collectionOperator switch
        {
            "HasAny" => $"{collectionPath}.Any({combinedCondition})",
            "HasAll" => subFilterConditions.Any() 
                ? $"{collectionPath}.All({combinedCondition})" 
                : "", // HasAll requires sub-filters
            "None" => $"!{collectionPath}.Any({combinedCondition})",
            "Count" => BuildCountCondition(collectionPath, filter.Operator, filter.Value),
            _ => $"{collectionPath}.Any({combinedCondition})"
        };
    }

    /// <summary>
    /// Build condition for a single sub-filter within a collection
    /// </summary>
    private string BuildSubFilterCondition(string field, string op, string value, string dataType)
    {
        // Escape quotes in value
        var escapedValue = value.Replace("\"", "\\\"");

        // Build condition based on data type
        if (dataType == "DateTime" || dataType == "Date")
        {
            return op switch
            {
                "Equals" => $"{field}.HasValue && {field}.Value.Date == DateTime.Parse(\"{escapedValue}\").Date",
                "GreaterThan" => $"{field}.HasValue && {field}.Value > DateTime.Parse(\"{escapedValue}\")",
                "LessThan" => $"{field}.HasValue && {field}.Value < DateTime.Parse(\"{escapedValue}\")",
                "GreaterThanOrEqual" => $"{field}.HasValue && {field}.Value >= DateTime.Parse(\"{escapedValue}\")",
                "LessThanOrEqual" => $"{field}.HasValue && {field}.Value <= DateTime.Parse(\"{escapedValue}\")",
                _ => $"{field}.HasValue && {field}.Value == DateTime.Parse(\"{escapedValue}\")"
            };
        }
        else if (dataType == "Int32" || dataType == "Number" || dataType == "Decimal")
        {
            return op switch
            {
                "Equals" => $"{field} == {value}",
                "NotEquals" => $"{field} != {value}",
                "GreaterThan" => $"{field} > {value}",
                "LessThan" => $"{field} < {value}",
                "GreaterThanOrEqual" => $"{field} >= {value}",
                "LessThanOrEqual" => $"{field} <= {value}",
                _ => $"{field} == {value}"
            };
        }
        else // String
        {
            return op switch
            {
                "Equals" => $"{field} == \"{escapedValue}\"",
                "NotEquals" => $"{field} != \"{escapedValue}\"",
                "Contains" => $"{field} != null && {field}.Contains(\"{escapedValue}\")",
                "NotContains" => $"{field} == null || !{field}.Contains(\"{escapedValue}\")",
                "StartsWith" => $"{field} != null && {field}.StartsWith(\"{escapedValue}\")",
                "EndsWith" => $"{field} != null && {field}.EndsWith(\"{escapedValue}\")",
                _ => $"{field} == \"{escapedValue}\""
            };
        }
    }

    /// <summary>
    /// Build count condition for collection queries
    /// </summary>
    private string BuildCountCondition(string collectionPath, string? op, string? value)
    {
        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var count))
            return $"{collectionPath}.Count() > 0";

        return op switch
        {
            "Equals" => $"{collectionPath}.Count() == {count}",
            "NotEquals" => $"{collectionPath}.Count() != {count}",
            "GreaterThan" => $"{collectionPath}.Count() > {count}",
            "LessThan" => $"{collectionPath}.Count() < {count}",
            "GreaterThanOrEqual" => $"{collectionPath}.Count() >= {count}",
            "LessThanOrEqual" => $"{collectionPath}.Count() <= {count}",
            _ => $"{collectionPath}.Count() > {count}"
        };
    }

    private string BuildDateWhereClause(string fieldExpression, string operatorType, string value, bool isNullable)
    {
        // Dynamic LINQ can't resolve DbFunctions - use datetime ranges with ISO 8601 format
        // MUST use "yyyy-MM-ddTHH:mm:ss" format (T separator) for Dynamic LINQ to parse correctly
        
        if (!DateTime.TryParse(value, out var dateValue))
        {
            Console.WriteLine($"[WHERE CLAUSE ERROR] Could not parse date: {value}");
            return "";
        }
        
        var startOfDay = dateValue.Date;
        var endOfDay = startOfDay.AddDays(1);
        
        // ISO 8601 format with T separator
        var startStr = startOfDay.ToString("yyyy-MM-ddTHH:mm:ss");
        var endStr = endOfDay.ToString("yyyy-MM-ddTHH:mm:ss");
        
        // For non-nullable DateTime fields, don't check .HasValue
        if (!isNullable)
        {
            return operatorType switch
            {
                "Equals" => $"{fieldExpression} >= DateTime.Parse(\"{startStr}\") && {fieldExpression} < DateTime.Parse(\"{endStr}\")",
                "NotEquals" => $"{fieldExpression} < DateTime.Parse(\"{startStr}\") || {fieldExpression} >= DateTime.Parse(\"{endStr}\")",
                "GreaterThan" => $"{fieldExpression} >= DateTime.Parse(\"{endStr}\")",
                "LessThan" => $"{fieldExpression} < DateTime.Parse(\"{startStr}\")",
                "GreaterThanOrEqual" => $"{fieldExpression} >= DateTime.Parse(\"{startStr}\")",
                "LessThanOrEqual" => $"{fieldExpression} < DateTime.Parse(\"{endStr}\")",
                "Between" => BuildDateBetweenClause(fieldExpression, value, isNullable),
                "InLast" => BuildInLastDaysClause(fieldExpression, value, isNullable),
                "InNext" => BuildInNextDaysClause(fieldExpression, value, isNullable),
                "IsNull" => "false", // Non-nullable DateTime can never be null
                "IsNotNull" => "true", // Non-nullable DateTime is always not null
                _ => ""
            };
        }
        
        // For nullable DateTime fields, check .HasValue
        return operatorType switch
        {
            "Equals" => $"{fieldExpression}.HasValue && {fieldExpression}.Value >= DateTime.Parse(\"{startStr}\") && {fieldExpression}.Value < DateTime.Parse(\"{endStr}\")",
            "NotEquals" => $"!{fieldExpression}.HasValue || {fieldExpression}.Value < DateTime.Parse(\"{startStr}\") || {fieldExpression}.Value >= DateTime.Parse(\"{endStr}\")",
            "GreaterThan" => $"{fieldExpression}.HasValue && {fieldExpression}.Value >= DateTime.Parse(\"{endStr}\")",
            "LessThan" => $"{fieldExpression}.HasValue && {fieldExpression}.Value < DateTime.Parse(\"{startStr}\")",
            "GreaterThanOrEqual" => $"{fieldExpression}.HasValue && {fieldExpression}.Value >= DateTime.Parse(\"{startStr}\")",
            "LessThanOrEqual" => $"{fieldExpression}.HasValue && {fieldExpression}.Value < DateTime.Parse(\"{endStr}\")",
            "Between" => BuildDateBetweenClause(fieldExpression, value, isNullable),
            "InLast" => BuildInLastDaysClause(fieldExpression, value, isNullable),
            "InNext" => BuildInNextDaysClause(fieldExpression, value, isNullable),
            "IsNull" => $"!{fieldExpression}.HasValue",
            "IsNotNull" => $"{fieldExpression}.HasValue",
            _ => ""
        };
    }

    private string BuildDateBetweenClause(string fieldPath, string value, bool isNullable)
    {
        var parts = value.Split('|');
        if (parts.Length != 2) return "";

        if (!DateTime.TryParse(parts[0], out var startDate) || !DateTime.TryParse(parts[1], out var endDate))
            return "";
        
        var startOfDay = startDate.Date;
        var endOfDay = endDate.Date.AddDays(1);

        var startStr = startOfDay.ToString("yyyy-MM-ddTHH:mm:ss");
        var endStr = endOfDay.ToString("yyyy-MM-ddTHH:mm:ss");

        if (!isNullable)
        {
            return $"{fieldPath} >= DateTime.Parse(\"{startStr}\") && {fieldPath} < DateTime.Parse(\"{endStr}\")";
        }
        
        return $"{fieldPath}.HasValue && {fieldPath}.Value >= DateTime.Parse(\"{startStr}\") && {fieldPath}.Value < DateTime.Parse(\"{endStr}\")";
    }

    private string BuildInLastDaysClause(string fieldPath, string value, bool isNullable)
    {
        if (!int.TryParse(value, out var days)) return "";
        
        var startDate = DateTime.UtcNow.Date.AddDays(-days);
        var startStr = startDate.ToString("yyyy-MM-ddTHH:mm:ss");
        
        if (!isNullable)
        {
            return $"{fieldPath} >= DateTime.Parse(\"{startStr}\")";
        }
        
        return $"{fieldPath}.HasValue && {fieldPath}.Value >= DateTime.Parse(\"{startStr}\")";
    }

    private string BuildInNextDaysClause(string fieldPath, string value, bool isNullable)
    {
        if (!int.TryParse(value, out var days)) return "";
        
        var endDate = DateTime.UtcNow.Date.AddDays(days + 1);
        var endStr = endDate.ToString("yyyy-MM-ddTHH:mm:ss");
        
        if (!isNullable)
        {
            return $"{fieldPath} < DateTime.Parse(\"{endStr}\")";
        }
        
        return $"{fieldPath}.HasValue && {fieldPath}.Value < DateTime.Parse(\"{endStr}\")";
    }

    private string BuildNumericWhereClause(string fieldExpression, string operatorType, string value)
    {
        // For numeric fields, don't wrap value in quotes
        return operatorType switch
        {
            "Equals" => $"{fieldExpression} == {value}",
            "NotEquals" => $"{fieldExpression} != {value}",
            "GreaterThan" => $"{fieldExpression} > {value}",
            "LessThan" => $"{fieldExpression} < {value}",
            "GreaterThanOrEqual" => $"{fieldExpression} >= {value}",
            "LessThanOrEqual" => $"{fieldExpression} <= {value}",
            "Between" => BuildNumericBetweenClause(fieldExpression, value),
            "IsNull" => $"{fieldExpression} == null",
            "IsNotNull" => $"{fieldExpression} != null",
            _ => ""
        };
    }

    private string BuildNumericBetweenClause(string fieldPath, string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 2) return "";

        return $"{fieldPath} >= {parts[0]} && {fieldPath} <= {parts[1]}";
    }

    private string BuildStringWhereClause(string fieldExpression, string operatorType, string value)
    {
        return operatorType switch
        {
            "Equals" => $"{fieldExpression} == \"{value}\"",
            "NotEquals" => $"{fieldExpression} != \"{value}\"",
            "Contains" => $"{fieldExpression} != null && {fieldExpression}.Contains(\"{value}\")",
            "NotContains" => $"{fieldExpression} == null || !{fieldExpression}.Contains(\"{value}\")",
            "StartsWith" => $"{fieldExpression} != null && {fieldExpression}.StartsWith(\"{value}\")",
            "EndsWith" => $"{fieldExpression} != null && {fieldExpression}.EndsWith(\"{value}\")",
            "IsNull" => $"{fieldExpression} == null",
            "IsNotNull" => $"{fieldExpression} != null",
            "GreaterThan" => $"{fieldExpression} != null && string.Compare({fieldExpression}, \"{value}\") > 0",
            "LessThan" => $"{fieldExpression} != null && string.Compare({fieldExpression}, \"{value}\") < 0",
            "GreaterThanOrEqual" => $"{fieldExpression} != null && string.Compare({fieldExpression}, \"{value}\") >= 0",
            "LessThanOrEqual" => $"{fieldExpression} != null && string.Compare({fieldExpression}, \"{value}\") <= 0",
            "Between" => BuildStringBetweenClause(fieldExpression, value),
            _ => ""
        };
    }

    private string BuildStringBetweenClause(string fieldPath, string value)
    {
        var parts = value.Split('|');
        if (parts.Length != 2) return "";

        return $"{fieldPath} != null && string.Compare({fieldPath}, \"{parts[0]}\") >= 0 && string.Compare({fieldPath}, \"{parts[1]}\") <= 0";
    }

    private async Task<Dictionary<string, ReportFieldMetadata>> GetFieldMetadataForReport(
        ReportDefinition reportDefinition)
    {
        var allFields = await _fieldMetadataService.GetFieldsForEntityAsync(reportDefinition.EntityType);
        
        return allFields.ToDictionary(f => f.FieldPath, f => f);
    }

    private Type GetClrType(string entityType)
    {
        return entityType switch
        {
            "Case" => typeof(Case),
            "Contact" => typeof(Case), // Contacts use Case table
            "Outbreak" => typeof(Outbreak),
            "Patient" => typeof(Patient),
            "Task" => typeof(CaseTask),
            "Location" => typeof(Location),
            "Event" => typeof(Event),
            
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

    #endregion

    #region Entity-Specific Extraction

    private async Task<List<Dictionary<string, object?>>> ExtractCaseDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Case query
        var caseQuery = baseQuery.Cast<Case>();
        
        Console.WriteLine($"[ExtractCase] Query cast to Case");

        // Dynamically include navigation properties based on fields used in report
        caseQuery = IncludeNavigationProperties(caseQuery, reportDefinition.Fields);
        
        Console.WriteLine($"[ExtractCase] Navigation properties included");

        // Load data
        var cases = await caseQuery.ToListAsync();
        
        Console.WriteLine($"[ExtractCase] Loaded {cases.Count} cases from database");

        // Get custom field definitions needed
        var customFieldIds = reportDefinition.Fields
            .Where(f => f.IsCustomField && f.CustomFieldDefinitionId.HasValue)
            .Select(f => f.CustomFieldDefinitionId!.Value)
            .Distinct()
            .ToList();

        // Load custom fields for all cases
        var caseIds = cases.Select(c => c.Id).ToList();
        var customFieldData = await LoadCaseCustomFieldsAsync(caseIds, customFieldIds);
        
        Console.WriteLine($"[ExtractCase] Loaded custom fields for {caseIds.Count} cases");

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var caseEntity in cases)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field (needed for collection queries)
            row["Id"] = caseEntity.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    caseEntity,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    customFieldData);

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }
        
        Console.WriteLine($"[ExtractCase] Transformed {result.Count} rows");

        return result;
    }

    private IQueryable<Case> IncludeNavigationProperties(IQueryable<Case> query, ICollection<ReportField> fields)
    {
        // Get unique navigation paths (only first level)
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        // Include each navigation property
        foreach (var navPath in navigationPaths)
        {
            try
            {
                // Validate this is a valid navigation property on Case entity
                var caseType = typeof(Case);
                var property = caseType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    private bool IsNavigationProperty(System.Reflection.PropertyInfo property)
    {
        // A navigation property is typically a reference type (class) that's not a primitive or string
        var type = property.PropertyType;
        return !type.IsPrimitive && 
               !type.IsValueType && 
               type != typeof(string) && 
               type != typeof(byte[]);
    }

    private async Task<List<Dictionary<string, object?>>> ExtractOutbreakDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Outbreak query
        var outbreakQuery = baseQuery.Cast<Outbreak>();

        // Dynamically include navigation properties
        outbreakQuery = IncludeNavigationPropertiesForOutbreak(outbreakQuery, reportDefinition.Fields);

        // Load data
        var outbreaks = await outbreakQuery.ToListAsync();

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var outbreak in outbreaks)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field (needed for collection queries)
            row["Id"] = outbreak.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    outbreak,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    null); // Outbreaks don't have custom fields yet

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private IQueryable<Outbreak> IncludeNavigationPropertiesForOutbreak(IQueryable<Outbreak> query, ICollection<ReportField> fields)
    {
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        foreach (var navPath in navigationPaths)
        {
            try
            {
                var outbreakType = typeof(Outbreak);
                var property = outbreakType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    private async Task<List<Dictionary<string, object?>>> ExtractPatientDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Patient query
        var patientQuery = baseQuery.Cast<Patient>();

        // Dynamically include navigation properties
        patientQuery = IncludeNavigationPropertiesForPatient(patientQuery, reportDefinition.Fields);

        // Load data
        var patients = await patientQuery.ToListAsync();

        // Get custom field definitions needed
        var customFieldIds = reportDefinition.Fields
            .Where(f => f.IsCustomField && f.CustomFieldDefinitionId.HasValue)
            .Select(f => f.CustomFieldDefinitionId!.Value)
            .Distinct()
            .ToList();

        // Load custom fields
        var patientIds = patients.Select(p => p.Id).ToList();
        var customFieldData = await LoadPatientCustomFieldsAsync(patientIds, customFieldIds);

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var patient in patients)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field (needed for collection queries)
            row["Id"] = patient.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    patient,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    customFieldData);

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private IQueryable<Patient> IncludeNavigationPropertiesForPatient(IQueryable<Patient> query, ICollection<ReportField> fields)
    {
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        foreach (var navPath in navigationPaths)
        {
            try
            {
                var patientType = typeof(Patient);
                var property = patientType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    private async Task<List<Dictionary<string, object?>>> ExtractTaskDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Task query
        var taskQuery = baseQuery.Cast<CaseTask>();

        // Dynamically include navigation properties
        taskQuery = IncludeNavigationPropertiesForTask(taskQuery, reportDefinition.Fields);

        // Load data
        var tasks = await taskQuery.ToListAsync();

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var task in tasks)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field (needed for collection queries)
            row["Id"] = task.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    task,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    null); // Tasks don't have custom fields

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private IQueryable<CaseTask> IncludeNavigationPropertiesForTask(IQueryable<CaseTask> query, ICollection<ReportField> fields)
    {
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        foreach (var navPath in navigationPaths)
        {
            try
            {
                var taskType = typeof(CaseTask);
                var property = taskType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    private async Task<List<Dictionary<string, object?>>> ExtractLocationDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Location query
        var locationQuery = baseQuery.Cast<Location>();

        // Dynamically include navigation properties
        locationQuery = IncludeNavigationPropertiesForLocation(locationQuery, reportDefinition.Fields);

        // Load data
        var locations = await locationQuery.ToListAsync();

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var location in locations)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field
            row["Id"] = location.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    location,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    null); // Locations don't have custom fields

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private IQueryable<Location> IncludeNavigationPropertiesForLocation(IQueryable<Location> query, ICollection<ReportField> fields)
    {
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        foreach (var navPath in navigationPaths)
        {
            try
            {
                var locationType = typeof(Location);
                var property = locationType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    private async Task<List<Dictionary<string, object?>>> ExtractEventDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata,
        DataExtractionOptions options)
    {
        // Cast to Event query
        var eventQuery = baseQuery.Cast<Event>();

        // Dynamically include navigation properties
        eventQuery = IncludeNavigationPropertiesForEvent(eventQuery, reportDefinition.Fields);

        // Load data
        var events = await eventQuery.ToListAsync();

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var evt in events)
        {
            var row = new Dictionary<string, object?>();

            // Always include the ID field
            row["Id"] = evt.Id;

            foreach (var field in reportDefinition.Fields)
            {
                var value = await ExtractFieldValueAsync(
                    evt,
                    field,
                    fieldMetadata.GetValueOrDefault(field.FieldPath),
                    null); // Events don't have custom fields

                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    private IQueryable<Event> IncludeNavigationPropertiesForEvent(IQueryable<Event> query, ICollection<ReportField> fields)
    {
        var navigationPaths = fields
            .Where(f => f.FieldPath.Contains(".") && !f.IsCustomField)
            .Select(f => f.FieldPath.Split('.')[0])
            .Distinct()
            .ToList();

        foreach (var navPath in navigationPaths)
        {
            try
            {
                var eventType = typeof(Event);
                var property = eventType.GetProperty(navPath);
                
                if (property != null && IsNavigationProperty(property))
                {
                    query = query.Include(navPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not include navigation property '{navPath}': {ex.Message}");
            }
        }

        return query;
    }

    /// <summary>
    /// Generic extraction for flattened views (no custom fields, no navigation includes needed)
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> ExtractGenericDataAsync(
        IQueryable<object> baseQuery,
        ReportDefinition reportDefinition,
        Dictionary<string, ReportFieldMetadata> fieldMetadata)
    {
        // Views are already flattened - just load the data
        var data = await baseQuery.ToListAsync();

        // Transform to dictionaries
        var result = new List<Dictionary<string, object?>>();

        foreach (var entity in data)
        {
            var row = new Dictionary<string, object?>();

            foreach (var field in reportDefinition.Fields)
            {
                var value = ExtractDirectPropertyValue(entity, field.FieldPath);
                row[field.DisplayName] = value;
            }

            result.Add(row);
        }

        return result;
    }

    #endregion

    #region Field Value Extraction

    private async Task<object?> ExtractFieldValueAsync(
        object entity,
        ReportField field,
        ReportFieldMetadata? metadata,
        Dictionary<Guid, Dictionary<int, object?>>? customFieldData)
    {
        try
        {
            // Handle custom fields
            if (field.IsCustomField && field.CustomFieldDefinitionId.HasValue)
            {
                return ExtractCustomFieldValue(entity, field, customFieldData);
            }

            // Handle navigation properties
            if (field.FieldPath.Contains('.'))
            {
                return ExtractNavigationPropertyValue(entity, field.FieldPath);
            }

            // Handle direct properties
            return ExtractDirectPropertyValue(entity, field.FieldPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting field {field.FieldPath}: {ex.Message}");
            return null;
        }
    }

    private object? ExtractCustomFieldValue(
        object entity,
        ReportField field,
        Dictionary<Guid, Dictionary<int, object?>>? customFieldData)
    {
        if (customFieldData == null || !field.CustomFieldDefinitionId.HasValue)
            return null;

        // Get entity ID
        Guid entityId = entity switch
        {
            Case c => c.Id,
            Patient p => p.Id,
            _ => Guid.Empty
        };

        if (entityId == Guid.Empty)
            return null;

        // Get custom field value
        if (customFieldData.TryGetValue(entityId, out var entityCustomFields))
        {
            if (entityCustomFields.TryGetValue(field.CustomFieldDefinitionId.Value, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private object? ExtractNavigationPropertyValue(object entity, string fieldPath)
    {
        var parts = fieldPath.Split('.');
        object? current = entity;

        foreach (var part in parts)
        {
            if (current == null) return null;

            var property = current.GetType().GetProperty(part);
            if (property == null) return null;

            current = property.GetValue(current);
        }

        return current;
    }

    private object? ExtractDirectPropertyValue(object entity, string fieldPath)
    {
        var property = entity.GetType().GetProperty(fieldPath);
        return property?.GetValue(entity);
    }

    #endregion

    #region Custom Field Loading

    private async Task<Dictionary<Guid, Dictionary<int, object?>>> LoadCaseCustomFieldsAsync(
        List<Guid> caseIds,
        List<int> fieldDefinitionIds)
    {
        var result = new Dictionary<Guid, Dictionary<int, object?>>();

        if (!caseIds.Any() || !fieldDefinitionIds.Any())
            return result;

        // Load all custom field types SEQUENTIALLY (can't run in parallel on same DbContext)
        var stringFields = await _context.CaseCustomFieldStrings
            .AsNoTracking()
            .Where(cf => caseIds.Contains(cf.CaseId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var numberFields = await _context.CaseCustomFieldNumbers
            .AsNoTracking()
            .Where(cf => caseIds.Contains(cf.CaseId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var dateFields = await _context.CaseCustomFieldDates
            .AsNoTracking()
            .Where(cf => caseIds.Contains(cf.CaseId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var booleanFields = await _context.CaseCustomFieldBooleans
            .AsNoTracking()
            .Where(cf => caseIds.Contains(cf.CaseId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var lookupFields = await _context.CaseCustomFieldLookups
            .AsNoTracking()
            .Include(cf => cf.LookupValue)
            .Where(cf => caseIds.Contains(cf.CaseId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        Console.WriteLine($"[LoadCustomFields] Loaded {stringFields.Count + numberFields.Count + dateFields.Count + booleanFields.Count + lookupFields.Count} custom field values");

        // Pivot data
        foreach (var caseId in caseIds)
        {
            var caseFields = new Dictionary<int, object?>();

            // String fields
            foreach (var field in stringFields.Where(f => f.CaseId == caseId))
            {
                caseFields[field.FieldDefinitionId] = field.Value;
            }

            // Number fields
            foreach (var field in numberFields.Where(f => f.CaseId == caseId))
            {
                caseFields[field.FieldDefinitionId] = field.Value;
            }

            // Date fields
            foreach (var field in dateFields.Where(f => f.CaseId == caseId))
            {
                caseFields[field.FieldDefinitionId] = field.Value;
            }

            // Boolean fields
            foreach (var field in booleanFields.Where(f => f.CaseId == caseId))
            {
                caseFields[field.FieldDefinitionId] = field.Value;
            }

            // Lookup fields (use display text)
            foreach (var field in lookupFields.Where(f => f.CaseId == caseId))
            {
                caseFields[field.FieldDefinitionId] = field.LookupValue?.DisplayText;
            }

            result[caseId] = caseFields;
        }

        return result;
    }

    private async Task<Dictionary<Guid, Dictionary<int, object?>>> LoadPatientCustomFieldsAsync(
        List<Guid> patientIds,
        List<int> fieldDefinitionIds)
    {
        var result = new Dictionary<Guid, Dictionary<int, object?>>();

        if (!patientIds.Any() || !fieldDefinitionIds.Any())
            return result;

        // Load all custom field types SEQUENTIALLY (can't run in parallel on same DbContext)
        var stringFields = await _context.PatientCustomFieldStrings
            .AsNoTracking()
            .Where(cf => patientIds.Contains(cf.PatientId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var numberFields = await _context.PatientCustomFieldNumbers
            .AsNoTracking()
            .Where(cf => patientIds.Contains(cf.PatientId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var dateFields = await _context.PatientCustomFieldDates
            .AsNoTracking()
            .Where(cf => patientIds.Contains(cf.PatientId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var booleanFields = await _context.PatientCustomFieldBooleans
            .AsNoTracking()
            .Where(cf => patientIds.Contains(cf.PatientId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        var lookupFields = await _context.PatientCustomFieldLookups
            .AsNoTracking()
            .Include(cf => cf.LookupValue)
            .Where(cf => patientIds.Contains(cf.PatientId) && fieldDefinitionIds.Contains(cf.FieldDefinitionId))
            .ToListAsync();

        Console.WriteLine($"[LoadPatientCustomFields] Loaded {stringFields.Count + numberFields.Count + dateFields.Count + booleanFields.Count + lookupFields.Count} custom field values");

        // Pivot data
        foreach (var patientId in patientIds)
        {
            var patientFields = new Dictionary<int, object?>();

            // String fields
            foreach (var field in stringFields.Where(f => f.PatientId == patientId))
            {
                patientFields[field.FieldDefinitionId] = field.Value;
            }

            // Number fields
            foreach (var field in numberFields.Where(f => f.PatientId == patientId))
            {
                patientFields[field.FieldDefinitionId] = field.Value;
            }

            // Date fields
            foreach (var field in dateFields.Where(f => f.PatientId == patientId))
            {
                patientFields[field.FieldDefinitionId] = field.Value;
            }

            // Boolean fields
            foreach (var field in booleanFields.Where(f => f.PatientId == patientId))
            {
                patientFields[field.FieldDefinitionId] = field.Value;
            }

            // Lookup fields (use display text)
            foreach (var field in lookupFields.Where(f => f.PatientId == patientId))
            {
                patientFields[field.FieldDefinitionId] = field.LookupValue?.DisplayText;
            }

            result[patientId] = patientFields;
        }

        return result;
    }

    #endregion

    #region Collection Query Column Methods

    /// <summary>
    /// Adds calculated columns from collection queries to the result set
    /// </summary>
    private async Task<List<Dictionary<string, object?>>> AddCollectionColumnsAsync(
        List<Dictionary<string, object?>> rows,
        List<CollectionQueryDto> collectionQueries,
        string entityType)
    {
        Console.WriteLine($"[CollectionColumns] Processing {collectionQueries.Count} collection queries for {rows.Count} rows");
        
        // Get only queries that should be displayed as columns
        var displayQueries = collectionQueries
            .Where(q => q.DisplayAsColumn)
            .ToList();
            
        if (!displayQueries.Any())
        {
            Console.WriteLine($"[CollectionColumns] No display queries found");
            return rows;
        }
        
        Console.WriteLine($"[CollectionColumns] {displayQueries.Count} queries will be displayed as columns");
        
        // For each row
        foreach (var row in rows)
        {
            var entityId = GetEntityIdObjectFromRow(row, entityType);
            
            if (entityId == null)
            {
                Console.WriteLine($"[CollectionColumns] Warning: Could not extract entity ID from row");
                continue;
            }
            
            // For each collection query
            foreach (var query in displayQueries)
            {
                var columnName = query.ColumnName ?? $"{query.CollectionName} {query.Operation}";
                var value = await CalculateCollectionValueAsync(entityId, query, entityType);
                row[columnName] = value;
                
                Console.WriteLine($"[CollectionColumns] Added column '{columnName}' = '{value}' for entity {entityId}");
            }
        }
        
        Console.WriteLine($"[CollectionColumns] Finished adding collection columns");
        return rows;
    }

    /// <summary>
    /// Extracts the entity ID from a row dictionary (supports both int and Guid)
    /// </summary>
    private object? GetEntityIdObjectFromRow(Dictionary<string, object?> row, string entityType)
    {
        // Try common ID field names
        var idFields = new[] { "Id", "id", "ID", entityType + "Id", entityType + "ID" };
        
        foreach (var field in idFields)
        {
            if (row.ContainsKey(field) && row[field] != null)
            {
                return row[field];
            }
        }
        
        return null;
    }

    /// <summary>
    /// Calculates the value for a collection query
    /// </summary>
    private async Task<object?> CalculateCollectionValueAsync(
        object entityId,
        CollectionQueryDto query,
        string entityType)
    {
        try
        {
            return query.Operation switch
            {
                "HasAny" => await HasAnyAsync(entityId, query, entityType) ? "Yes" : "No",
                "HasAll" => await HasAllAsync(entityId, query, entityType) ? "Yes" : "No",
                "Count" => await CountAsync(entityId, query, entityType),
                "Sum" => await SumAsync(entityId, query, entityType),
                "Average" => await AverageAsync(entityId, query, entityType),
                "Min" => await MinAsync(entityId, query, entityType),
                "Max" => await MaxAsync(entityId, query, entityType),
                _ => null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error calculating {query.Operation} for {query.CollectionName}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Checks if any items in the collection match the sub-filters
    /// </summary>
    private async Task<bool> HasAnyAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        switch (entityType)
        {
            case "Case":
            case "Contact":
                return await HasAnyCaseCollectionAsync(entityId, query);
            case "Patient":
                return await HasAnyPatientCollectionAsync(entityId, query);
            case "Outbreak":
                return await HasAnyOutbreakCollectionAsync(entityId, query);
            default:
                return false;
        }
    }

    private async Task<bool> HasAnyCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Cases use Guid IDs
        if (!(entityId is Guid caseGuid))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Case: {entityId.GetType()}");
            return false;
        }

        switch (query.CollectionName)
        {
            case "LabResults":
                var labQuery = _context.LabResults
                    .Include(lr => lr.TestResult)
                    .Include(lr => lr.TestType)
                    .Include(lr => lr.SpecimenType)
                    .Where(lr => lr.CaseId == caseGuid);
                return await ApplySubFiltersAndCheckAnyAsync(labQuery, query.SubFilters);
                
            case "ExposureEvents":
            case "Exposures":
                var exposureQuery = _context.ExposureEvents
                    .Include(e => e.Location)
                    .Include(e => e.ExposureType)
                    .Where(e => e.ExposedCaseId == caseGuid);
                return await ApplySubFiltersAndCheckAnyAsync(exposureQuery, query.SubFilters);
                
            case "Tasks":
                var taskQuery = _context.CaseTasks.Where(t => t.CaseId == caseGuid);
                return await ApplySubFiltersAndCheckAnyAsync(taskQuery, query.SubFilters);
                
            case "Symptoms":
            case "CaseSymptomTracking":
                var symptomQuery = _context.CaseSymptoms
                    .Include(s => s.Symptom)
                    .Where(s => s.CaseId == caseGuid);
                return await ApplySubFiltersAndCheckAnyAsync(symptomQuery, query.SubFilters);
                
            default:
                return false;
        }
    }

    private async Task<bool> HasAnyPatientCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Patients use Guid IDs
        if (!(entityId is Guid patientGuid))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Patient: {entityId.GetType()}");
            return false;
        }

        switch (query.CollectionName)
        {
            case "Cases":
                var caseQuery = _context.Cases
                    .Include(c => c.Disease)
                    .Include(c => c.ConfirmationStatus)
                    .Where(c => c.PatientId == patientGuid && c.Type == CaseType.Case);
                return await ApplySubFiltersAndCheckAnyAsync(caseQuery, query.SubFilters);
                
            case "Contacts":
                var contactQuery = _context.Cases
                    .Include(c => c.Disease)
                    .Include(c => c.ConfirmationStatus)
                    .Where(c => c.PatientId == patientGuid && c.Type == CaseType.Contact);
                return await ApplySubFiltersAndCheckAnyAsync(contactQuery, query.SubFilters);
                
            default:
                return false;
        }
    }

    private async Task<bool> HasAnyOutbreakCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Outbreaks use int IDs
        if (!(entityId is int outbreakId))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Outbreak: {entityId.GetType()}");
            return false;
        }

        switch (query.CollectionName)
        {
            case "OutbreakCases":
                var outcaseQuery = _context.OutbreakCases.Where(oc => oc.OutbreakId == outbreakId);
                return await ApplySubFiltersAndCheckAnyAsync(outcaseQuery, query.SubFilters);
                
            case "Tasks":
                // Outbreak tasks not currently supported in the data model
                Console.WriteLine($"[CollectionColumns] Outbreak tasks not yet implemented");
                return false;
                
            default:
                return false;
        }
    }

    /// <summary>
    /// Applies sub-filters to a query and checks if any items match
    /// </summary>
    private async Task<bool> ApplySubFiltersAndCheckAnyAsync<T>(IQueryable<T> query, List<CollectionSubFilter> subFilters) where T : class
    {
        if (!subFilters.Any())
            return await query.AnyAsync();
        
        // Build Dynamic LINQ where clause from sub-filters
        var whereClauses = new List<string>();
        
        foreach (var filter in subFilters)
        {
            var clause = BuildSubFilterWhereClause(filter);
            if (!string.IsNullOrEmpty(clause))
            {
                whereClauses.Add(clause);
            }
        }
        
        if (!whereClauses.Any())
            return await query.AnyAsync();
        
        // Combine with AND
        var combinedWhere = string.Join(" && ", whereClauses);
        
        try
        {
            Console.WriteLine($"[CollectionColumns] Generated Dynamic LINQ query: {combinedWhere}");
            return await query.Where(combinedWhere).AnyAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error applying sub-filters: {ex.Message}");
            Console.WriteLine($"[CollectionColumns] Failed query: {combinedWhere}");
            Console.WriteLine($"[CollectionColumns] Exception details: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Builds a Dynamic LINQ where clause from a sub-filter
    /// </summary>
    private string BuildSubFilterWhereClause(CollectionSubFilter filter)
    {
        var field = filter.Field;
        var value = filter.Value?.Replace("\"", "\\\"") ?? "";
        
        // Determine if this is likely a numeric comparison
        bool isNumericValue = double.TryParse(value, out _);
        
        // Handle navigation properties (e.g., TestResult.Name)
        // Navigation properties are typically strings (Name, Code, etc.)
        if (field.Contains('.'))
        {
            var parts = field.Split('.');
            var navigationProp = parts[0];
            var propertyAccess = field;
            
            // For navigation properties, use case-insensitive string comparisons
            var valueLower = value.ToLower();
            
            switch (filter.Operator)
            {
                case "Equals":
                    return $"{navigationProp} != null && {propertyAccess} != null && {propertyAccess}.ToLower() == \"{valueLower}\"";
                case "NotEquals":
                    return $"{navigationProp} != null && {propertyAccess} != null && {propertyAccess}.ToLower() != \"{valueLower}\"";
                case "Contains":
                    return $"{navigationProp} != null && {propertyAccess} != null && {propertyAccess}.ToLower().Contains(\"{valueLower}\")";
                case "StartsWith":
                    return $"{navigationProp} != null && {propertyAccess} != null && {propertyAccess}.ToLower().StartsWith(\"{valueLower}\")";
                case "EndsWith":
                    return $"{navigationProp} != null && {propertyAccess} != null && {propertyAccess}.ToLower().EndsWith(\"{valueLower}\")";
                case "GreaterThan":
                    return $"{navigationProp} != null && {propertyAccess} > {value}";
                case "LessThan":
                    return $"{navigationProp} != null && {propertyAccess} < {value}";
                case "GreaterThanOrEqual":
                    return $"{navigationProp} != null && {propertyAccess} >= {value}";
                case "LessThanOrEqual":
                    return $"{navigationProp} != null && {propertyAccess} <= {value}";
                default:
                    return "";
            }
        }
        
        // Direct property access (no navigation)
        // These could be strings, numbers, or dates - need to be smart
        switch (filter.Operator)
        {
            case "Equals":
                if (isNumericValue)
                    return $"{field} == {value}";
                else
                {
                    var valueLower = value.ToLower();
                    return $"{field} != null && {field}.ToString().ToLower() == \"{valueLower}\"";
                }
                
            case "NotEquals":
                if (isNumericValue)
                    return $"{field} != {value}";
                else
                {
                    var valueLower = value.ToLower();
                    return $"{field} != null && {field}.ToString().ToLower() != \"{valueLower}\"";
                }
                
            case "Contains":
            case "StartsWith":
            case "EndsWith":
                // These only work on strings, use case-insensitive
                var valLower = value.ToLower();
                var methodName = filter.Operator;
                return $"{field} != null && {field}.ToString().ToLower().{methodName}(\"{valLower}\")";
                
            case "GreaterThan":
                return $"{field} > {value}";
                
            case "LessThan":
                return $"{field} < {value}";
                
            case "GreaterThanOrEqual":
                return $"{field} >= {value}";
                
            case "LessThanOrEqual":
                return $"{field} <= {value}";
                
            default:
                return "";
        }
    }

    private async Task<bool> HasAllAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        // HasAll means: if there are any items, ALL of them match the filters
        // This is equivalent to: Count(all items) == Count(filtered items)
        
        // For now, return false - can implement if needed
        Console.WriteLine($"[CollectionColumns] HasAll not fully implemented yet");
        return false;
    }

    private async Task<int> CountAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        switch (entityType)
        {
            case "Case":
            case "Contact":
                return await CountCaseCollectionAsync(entityId, query);
            case "Patient":
                return await CountPatientCollectionAsync(entityId, query);
            case "Outbreak":
                return await CountOutbreakCollectionAsync(entityId, query);
            default:
                return 0;
        }
    }

    private async Task<int> CountCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Cases use Guid IDs
        if (!(entityId is Guid caseGuid))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Case: {entityId.GetType()}");
            return 0;
        }

        switch (query.CollectionName)
        {
            case "LabResults":
                var labQuery = _context.LabResults
                    .Include(lr => lr.TestResult)
                    .Include(lr => lr.TestType)
                    .Include(lr => lr.SpecimenType)
                    .Where(lr => lr.CaseId == caseGuid);
                return await ApplySubFiltersAndCountAsync(labQuery, query.SubFilters);
                
            case "ExposureEvents":
            case "Exposures":
                var exposureQuery = _context.ExposureEvents
                    .Include(e => e.Location)
                    .Include(e => e.ExposureType)
                    .Where(e => e.ExposedCaseId == caseGuid);
                return await ApplySubFiltersAndCountAsync(exposureQuery, query.SubFilters);
                
            case "Tasks":
                var taskQuery = _context.CaseTasks.Where(t => t.CaseId == caseGuid);
                return await ApplySubFiltersAndCountAsync(taskQuery, query.SubFilters);
                
            case "Symptoms":
            case "CaseSymptomTracking":
                var symptomQuery = _context.CaseSymptoms
                    .Include(s => s.Symptom)
                    .Where(s => s.CaseId == caseGuid);
                return await ApplySubFiltersAndCountAsync(symptomQuery, query.SubFilters);
                
            default:
                return 0;
        }
    }

    private async Task<int> CountPatientCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Patients use Guid IDs
        if (!(entityId is Guid patientGuid))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Patient: {entityId.GetType()}");
            return 0;
        }

        switch (query.CollectionName)
        {
            case "Cases":
                var caseQuery = _context.Cases.Where(c => c.PatientId == patientGuid && c.Type == CaseType.Case);
                return await ApplySubFiltersAndCountAsync(caseQuery, query.SubFilters);
                
            case "Contacts":
                var contactQuery = _context.Cases.Where(c => c.PatientId == patientGuid && c.Type == CaseType.Contact);
                return await ApplySubFiltersAndCountAsync(contactQuery, query.SubFilters);
                
            default:
                return 0;
        }
    }

    private async Task<int> CountOutbreakCollectionAsync(object entityId, CollectionQueryDto query)
    {
        // Outbreaks use int IDs
        if (!(entityId is int outbreakId))
        {
            Console.WriteLine($"[CollectionColumns] Invalid entityId type for Outbreak: {entityId.GetType()}");
            return 0;
        }

        switch (query.CollectionName)
        {
            case "OutbreakCases":
                var outcaseQuery = _context.OutbreakCases.Where(oc => oc.OutbreakId == outbreakId);
                return await ApplySubFiltersAndCountAsync(outcaseQuery, query.SubFilters);
                
            case "Tasks":
                // Outbreak tasks not currently supported in the data model
                Console.WriteLine($"[CollectionColumns] Outbreak tasks not yet implemented");
                return 0;
                
            default:
                return 0;
        }
    }

    private async Task<int> ApplySubFiltersAndCountAsync<T>(IQueryable<T> query, List<CollectionSubFilter> subFilters) where T : class
    {
        if (!subFilters.Any())
            return await query.CountAsync();
        
        // Build Dynamic LINQ where clause from sub-filters
        var whereClauses = new List<string>();
        
        foreach (var filter in subFilters)
        {
            var clause = BuildSubFilterWhereClause(filter);
            if (!string.IsNullOrEmpty(clause))
            {
                whereClauses.Add(clause);
            }
        }
        
        if (!whereClauses.Any())
            return await query.CountAsync();
        
        // Combine with AND
        var combinedWhere = string.Join(" && ", whereClauses);
        
        try
        {
            return await query.Where(combinedWhere).CountAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error applying sub-filters for count: {ex.Message}");
            return 0;
        }
    }

    private async Task<object?> SumAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        if (string.IsNullOrEmpty(query.AggregateField))
        {
            Console.WriteLine($"[CollectionColumns] Sum requires AggregateField");
            return null;
        }

        try
        {
            switch (entityType)
            {
                case "Case":
                case "Contact":
                    return await SumCaseCollectionAsync(entityId, query);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error calculating sum: {ex.Message}");
            return null;
        }
    }

    private async Task<object?> AverageAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        if (string.IsNullOrEmpty(query.AggregateField))
        {
            Console.WriteLine($"[CollectionColumns] Average requires AggregateField");
            return null;
        }

        try
        {
            switch (entityType)
            {
                case "Case":
                case "Contact":
                    return await AverageCaseCollectionAsync(entityId, query);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error calculating average: {ex.Message}");
            return null;
        }
    }

    private async Task<object?> MinAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        if (string.IsNullOrEmpty(query.AggregateField))
        {
            Console.WriteLine($"[CollectionColumns] Min requires AggregateField");
            return null;
        }

        try
        {
            switch (entityType)
            {
                case "Case":
                case "Contact":
                    return await MinCaseCollectionAsync(entityId, query);
                case "Patient":
                    return await MinPatientCollectionAsync(entityId, query);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error calculating min: {ex.Message}");
            return null;
        }
    }

    private async Task<object?> MaxAsync(object entityId, CollectionQueryDto query, string entityType)
    {
        if (string.IsNullOrEmpty(query.AggregateField))
        {
            Console.WriteLine($"[CollectionColumns] Max requires AggregateField");
            return null;
        }

        try
        {
            switch (entityType)
            {
                case "Case":
                case "Contact":
                    return await MaxCaseCollectionAsync(entityId, query);
                case "Patient":
                    return await MaxPatientCollectionAsync(entityId, query);
                default:
                    return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CollectionColumns] Error calculating max: {ex.Message}");
            return null;
        }
    }

    // Aggregate implementations for Case collections
    private async Task<object?> SumCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid caseGuid)) return null;

        switch (query.CollectionName)
        {
            case "LabResults":
                if (query.AggregateField == "QuantitativeResult")
                {
                    var labQuery = _context.LabResults.Where(lr => lr.CaseId == caseGuid);
                    return await labQuery.SumAsync(lr => lr.QuantitativeResult);
                }
                break;
        }

        return null;
    }

    private async Task<object?> AverageCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid caseGuid)) return null;

        switch (query.CollectionName)
        {
            case "LabResults":
                if (query.AggregateField == "QuantitativeResult")
                {
                    var labQuery = _context.LabResults.Where(lr => lr.CaseId == caseGuid && lr.QuantitativeResult.HasValue);
                    
                    if (!await labQuery.AnyAsync()) return null;
                    
                    return await labQuery.AverageAsync(lr => lr.QuantitativeResult!.Value);
                }
                break;
        }

        return null;
    }

    private async Task<object?> MinCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid caseGuid)) return null;

        switch (query.CollectionName)
        {
            case "LabResults":
                var labQuery = _context.LabResults.Where(lr => lr.CaseId == caseGuid);

                if (!await labQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "SpecimenCollectionDate" => await labQuery.Where(lr => lr.SpecimenCollectionDate.HasValue).MinAsync(lr => lr.SpecimenCollectionDate),
                    "ResultDate" => await labQuery.Where(lr => lr.ResultDate.HasValue).MinAsync(lr => lr.ResultDate),
                    "QuantitativeResult" => await labQuery.Where(lr => lr.QuantitativeResult.HasValue).MinAsync(lr => lr.QuantitativeResult),
                    _ => null
                };

            case "ExposureEvents":
                var expQuery = _context.ExposureEvents.Where(e => e.ExposedCaseId == caseGuid);

                if (!await expQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "ExposureStartDate" => await expQuery.MinAsync(e => e.ExposureStartDate),
                    "ExposureEndDate" => await expQuery.Where(e => e.ExposureEndDate.HasValue).MinAsync(e => e.ExposureEndDate),
                    _ => null
                };

            case "CaseTasks":
                var taskQuery = _context.CaseTasks.Where(t => t.CaseId == caseGuid);

                if (!await taskQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "DueDate" => await taskQuery.Where(t => t.DueDate.HasValue).MinAsync(t => t.DueDate),
                    "CreatedAt" => await taskQuery.MinAsync(t => t.CreatedAt),
                    "CompletedAt" => await taskQuery.Where(t => t.CompletedAt.HasValue).MinAsync(t => t.CompletedAt),
                    _ => null
                };
        }

        return null;
    }

    private async Task<object?> MaxCaseCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid caseGuid)) return null;

        switch (query.CollectionName)
        {
            case "LabResults":
                var labQuery = _context.LabResults.Where(lr => lr.CaseId == caseGuid);

                if (!await labQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "SpecimenCollectionDate" => await labQuery.Where(lr => lr.SpecimenCollectionDate.HasValue).MaxAsync(lr => lr.SpecimenCollectionDate),
                    "ResultDate" => await labQuery.Where(lr => lr.ResultDate.HasValue).MaxAsync(lr => lr.ResultDate),
                    "QuantitativeResult" => await labQuery.Where(lr => lr.QuantitativeResult.HasValue).MaxAsync(lr => lr.QuantitativeResult),
                    _ => null
                };

            case "ExposureEvents":
                var expQuery = _context.ExposureEvents.Where(e => e.ExposedCaseId == caseGuid);

                if (!await expQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "ExposureStartDate" => await expQuery.MaxAsync(e => e.ExposureStartDate),
                    "ExposureEndDate" => await expQuery.Where(e => e.ExposureEndDate.HasValue).MaxAsync(e => e.ExposureEndDate),
                    _ => null
                };

            case "CaseTasks":
                var taskQuery = _context.CaseTasks.Where(t => t.CaseId == caseGuid);

                if (!await taskQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "DueDate" => await taskQuery.Where(t => t.DueDate.HasValue).MaxAsync(t => t.DueDate),
                    "CreatedAt" => await taskQuery.MaxAsync(t => t.CreatedAt),
                    "CompletedAt" => await taskQuery.Where(t => t.CompletedAt.HasValue).MaxAsync(t => t.CompletedAt),
                    _ => null
                };
        }

        return null;
    }

    private async Task<object?> MinPatientCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid patientGuid)) return null;

        switch (query.CollectionName)
        {
            case "Cases":
                var caseQuery = _context.Cases.Where(c => c.PatientId == patientGuid);

                if (!await caseQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "DateOfOnset" => await caseQuery.Where(c => c.DateOfOnset.HasValue).MinAsync(c => c.DateOfOnset),
                    "DateOfNotification" => await caseQuery.Where(c => c.DateOfNotification.HasValue).MinAsync(c => c.DateOfNotification),
                    _ => null
                };
        }

        return null;
    }

    private async Task<object?> MaxPatientCollectionAsync(object entityId, CollectionQueryDto query)
    {
        if (!(entityId is Guid patientGuid)) return null;

        switch (query.CollectionName)
        {
            case "Cases":
                var caseQuery = _context.Cases.Where(c => c.PatientId == patientGuid);

                if (!await caseQuery.AnyAsync()) return null;

                return query.AggregateField switch
                {
                    "DateOfOnset" => await caseQuery.Where(c => c.DateOfOnset.HasValue).MaxAsync(c => c.DateOfOnset),
                    "DateOfNotification" => await caseQuery.Where(c => c.DateOfNotification.HasValue).MaxAsync(c => c.DateOfNotification),
                    _ => null
                };
        }

        return null;
    }

    #endregion
}

