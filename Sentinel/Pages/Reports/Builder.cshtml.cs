using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;
using System.Text.Json;

namespace Sentinel.Pages.Reports;

[Authorize(Policy = "Permission.Report.Create")]
public class BuilderModel : PageModel
{
    private readonly IReportFieldMetadataService _fieldMetadataService;
    private readonly IReportDataService _reportDataService;
    private readonly ApplicationDbContext _context;

    public BuilderModel(
        IReportFieldMetadataService fieldMetadataService,
        IReportDataService reportDataService,
        ApplicationDbContext context)
    {
        _fieldMetadataService = fieldMetadataService;
        _reportDataService = reportDataService;
        _context = context;
    }

    [BindProperty(SupportsGet = true)]
    public int? ReportId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string EntityType { get; set; } = "Case";

    public ReportDefinition? ReportDefinition { get; set; }
    public Dictionary<string, List<ReportFieldMetadata>>? AvailableFields { get; set; }
    public List<Dictionary<string, object?>>? ReportData { get; set; }
    public string? CollectionQueriesJson { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Disable browser caching - always fetch fresh data
        Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        Response.Headers.Append("Pragma", "no-cache");
        Response.Headers.Append("Expires", "0");
        
        // Load existing report if editing
        if (ReportId.HasValue)
        {
            ReportDefinition = await _context.ReportDefinitions
                .AsNoTracking()
                .Include(rd => rd.Fields)
                .Include(rd => rd.Filters)
                .FirstOrDefaultAsync(rd => rd.Id == ReportId.Value);

            if (ReportDefinition == null)
            {
                return NotFound();
            }

            // Override entity type with the one from saved report
            EntityType = ReportDefinition.EntityType;

            // Pass collection queries to the page for JavaScript initialization
            CollectionQueriesJson = ReportDefinition.CollectionQueriesJson;

            // Load report data
            ReportData = await _reportDataService.GetReportPreviewAsync(ReportDefinition);
        }

        // Load available fields for entity type (after potentially overriding from saved report)
        AvailableFields = await _fieldMetadataService.GetFieldsByCategoryAsync(EntityType);

        return Page();
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostSaveReportAsync([FromBody] SaveReportRequest request)
    {
        try
        {
            ReportDefinition reportDef;

            if (request.ReportId.HasValue)
            {
                // Update existing
                reportDef = await _context.ReportDefinitions
                    .Include(rd => rd.Fields)
                    .Include(rd => rd.Filters)
                    .FirstOrDefaultAsync(rd => rd.Id == request.ReportId.Value);

                if (reportDef == null)
                {
                    return NotFound();
                }

                // Clear existing fields and filters
                _context.ReportFields.RemoveRange(reportDef.Fields);
                _context.ReportFilters.RemoveRange(reportDef.Filters);
            }
            else
            {
                // Create new
                reportDef = new ReportDefinition
                {
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = User.Identity?.Name
                };
                _context.ReportDefinitions.Add(reportDef);
            }

            // Update properties
            reportDef.Name = request.Name;
            reportDef.Description = request.Description;
            reportDef.EntityType = request.EntityType;
            reportDef.Category = request.Category;
            reportDef.IsPublic = request.IsPublic;
            reportDef.PivotConfiguration = request.PivotConfiguration;
            reportDef.ModifiedAt = DateTime.UtcNow;

            // Save collection queries as JSON
            if (request.CollectionQueries != null && request.CollectionQueries.Any())
            {
                reportDef.CollectionQueriesJson = JsonSerializer.Serialize(request.CollectionQueries);
            }
            else
            {
                reportDef.CollectionQueriesJson = null;
            }

            // Add fields
            foreach (var field in request.Fields)
            {
                reportDef.Fields.Add(new ReportField
                {
                    FieldPath = field.FieldPath,
                    DisplayName = field.DisplayName,
                    DataType = field.DataType,
                    PivotArea = field.PivotArea,
                    AggregationType = field.AggregationType,
                    DisplayOrder = field.DisplayOrder,
                    IsCustomField = field.IsCustomField,
                    CustomFieldDefinitionId = field.CustomFieldDefinitionId
                });
            }

            // Add filters
            foreach (var filter in request.Filters)
            {
                reportDef.Filters.Add(new ReportFilter
                {
                    FieldPath = filter.FieldPath,
                    Operator = filter.Operator,
                    Value = filter.Value,
                    DataType = filter.DataType,
                    DisplayOrder = filter.DisplayOrder,
                    IsCustomField = filter.IsCustomField
                });
            }

            await _context.SaveChangesAsync();

            return new JsonResult(new
            {
                success = true,
                reportId = reportDef.Id,
                message = "Report saved successfully"
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new
            {
                success = false,
                error = ex.Message
            })
            {
                StatusCode = 500
            };
        }
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostPreviewReportAsync([FromBody] PreviewReportRequest request)
    {
        try
        {
            // Validate request
            if (request == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Request is null"
                });
            }

            if (string.IsNullOrEmpty(request.EntityType))
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "Entity type is required"
                });
            }

            if (request.Fields == null || !request.Fields.Any())
            {
                return new JsonResult(new
                {
                    success = false,
                    error = "At least one field is required"
                });
            }

            // Build temporary report definition
            var reportDef = new ReportDefinition
            {
                Name = "Preview",
                EntityType = request.EntityType,
                Fields = request.Fields.Select(f => new ReportField
                {
                    FieldPath = f.FieldPath,
                    DisplayName = f.DisplayName,
                    DataType = f.DataType,
                    IsCustomField = f.IsCustomField,
                    CustomFieldDefinitionId = f.CustomFieldDefinitionId
                }).ToList(),
                Filters = request.Filters?.Select(f => new ReportFilter
                {
                    FieldPath = f.FieldPath,
                    Operator = f.Operator,
                    Value = f.Value,
                    DataType = f.DataType,
                    IsCustomField = f.IsCustomField
                }).ToList() ?? new List<ReportFilter>()
            };

            // Get preview data
            var data = await _reportDataService.GetReportPreviewAsync(reportDef);

            return new JsonResult(new
            {
                success = true,
                data = data,
                rowCount = data.Count
            });
        }
        catch (Exception ex)
        {
            // Log the full exception
            Console.WriteLine($"Preview error: {ex}");
            
            return new JsonResult(new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace,
            innerException = ex.InnerException?.Message
            });
        }
    }

    [IgnoreAntiforgeryToken]
    public IActionResult OnPostGetDefaultFields([FromBody] DefaultFieldsRequest request)
    {
        var defaultFields = GetDefaultFieldsForEntityType(request.EntityType);
        return new JsonResult(new { success = true, fields = defaultFields });
    }

    private List<ReportFieldDto> GetDefaultFieldsForEntityType(string entityType)
    {
        return entityType switch
        {
            "Case" => new List<ReportFieldDto>
            {
                new() { FieldPath = "FriendlyId", DisplayName = "Case Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "Patient.GivenName", DisplayName = "First Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "Patient.FamilyName", DisplayName = "Last Name", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "DateOfOnset", DisplayName = "Date of Onset", DataType = "DateTime", DisplayOrder = 4 },
                new() { FieldPath = "DateOfNotification", DisplayName = "Date of Notification", DataType = "DateTime", DisplayOrder = 5 },
                new() { FieldPath = "Disease.Name", DisplayName = "Disease", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "ConfirmationStatus.Name", DisplayName = "Status", DataType = "String", DisplayOrder = 7 },
                new() { FieldPath = "Patient.City", DisplayName = "Suburb", DataType = "String", DisplayOrder = 8 },
                new() { FieldPath = "Jurisdiction1.Name", DisplayName = "Jurisdiction", DataType = "String", DisplayOrder = 9 }
            },

            "Contact" => new List<ReportFieldDto>
            {
                new() { FieldPath = "FriendlyId", DisplayName = "Contact Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "Patient.GivenName", DisplayName = "First Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "Patient.FamilyName", DisplayName = "Last Name", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "DateOfOnset", DisplayName = "Date of Onset", DataType = "DateTime", DisplayOrder = 4 },
                new() { FieldPath = "Disease.Name", DisplayName = "Disease", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "Patient.MobilePhone", DisplayName = "Mobile", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "Patient.City", DisplayName = "Suburb", DataType = "String", DisplayOrder = 7 },
                new() { FieldPath = "Jurisdiction1.Name", DisplayName = "Jurisdiction", DataType = "String", DisplayOrder = 8 }
            },

            "Patient" => new List<ReportFieldDto>
            {
                new() { FieldPath = "FriendlyId", DisplayName = "Patient ID", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "GivenName", DisplayName = "First Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "FamilyName", DisplayName = "Last Name", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "DateOfBirth", DisplayName = "Date of Birth", DataType = "DateTime", DisplayOrder = 4 },
                new() { FieldPath = "MobilePhone", DisplayName = "Mobile", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "EmailAddress", DisplayName = "Email", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "Address", DisplayName = "Address", DataType = "String", DisplayOrder = 7 },
                new() { FieldPath = "City", DisplayName = "Suburb", DataType = "String", DisplayOrder = 8 },
                new() { FieldPath = "State", DisplayName = "State", DataType = "String", DisplayOrder = 9 }
            },

            "Outbreak" => new List<ReportFieldDto>
            {
                new() { FieldPath = "Name", DisplayName = "Outbreak Name", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "StartDate", DisplayName = "Start Date", DataType = "DateTime", DisplayOrder = 2 },
                new() { FieldPath = "EndDate", DisplayName = "End Date", DataType = "DateTime", DisplayOrder = 3 },
                new() { FieldPath = "PrimaryDisease.Name", DisplayName = "Primary Disease", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "PrimaryLocation.Name", DisplayName = "Primary Location", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "ConfirmationStatus.Name", DisplayName = "Status", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "Status", DisplayName = "Outbreak Status", DataType = "Enum", DisplayOrder = 7 }
            },

            "Task" => new List<ReportFieldDto>
            {
                new() { FieldPath = "FriendlyId", DisplayName = "Task Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "Title", DisplayName = "Task Title", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "TaskType.Name", DisplayName = "Task Type", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "Status", DisplayName = "Status", DataType = "Enum", DisplayOrder = 4 },
                new() { FieldPath = "Priority", DisplayName = "Priority", DataType = "Enum", DisplayOrder = 5 },
                new() { FieldPath = "DueDate", DisplayName = "Due Date", DataType = "DateTime", DisplayOrder = 6 },
                new() { FieldPath = "AssignedToUser.Email", DisplayName = "Assigned To", DataType = "String", DisplayOrder = 7 },
                new() { FieldPath = "Case.FriendlyId", DisplayName = "Case Number", DataType = "String", DisplayOrder = 8 }
            },

            "Location" => new List<ReportFieldDto>
            {
                new() { FieldPath = "Name", DisplayName = "Location Name", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "LocationType.Name", DisplayName = "Location Type", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "Address", DisplayName = "Address", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "City", DisplayName = "City", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "State", DisplayName = "State", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "IsHighRisk", DisplayName = "High Risk", DataType = "Boolean", DisplayOrder = 6 },
                new() { FieldPath = "Organization.Name", DisplayName = "Organization", DataType = "String", DisplayOrder = 7 }
            },

            "Event" => new List<ReportFieldDto>
            {
                new() { FieldPath = "Name", DisplayName = "Event Name", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "EventType.Name", DisplayName = "Event Type", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "StartDateTime", DisplayName = "Start Date", DataType = "DateTime", DisplayOrder = 3 },
                new() { FieldPath = "EndDateTime", DisplayName = "End Date", DataType = "DateTime", DisplayOrder = 4 },
                new() { FieldPath = "Location.Name", DisplayName = "Location", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "EstimatedAttendees", DisplayName = "Estimated Attendees", DataType = "Int", DisplayOrder = 6 },
                new() { FieldPath = "IsIndoor", DisplayName = "Indoor", DataType = "Boolean", DisplayOrder = 7 }
            },

            // Flattened Views
            "CaseContactTasksFlattened" => new List<ReportFieldDto>
            {
                new() { FieldPath = "CaseNumber", DisplayName = "Case Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "PatientName", DisplayName = "Patient Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "GenerationNumber", DisplayName = "Generation", DataType = "Int", DisplayOrder = 3 },
                new() { FieldPath = "TransmittedByCase", DisplayName = "Transmitted By (Index Case)", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "ExposureType", DisplayName = "Exposure Type", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "TaskTitle", DisplayName = "Task", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "TaskStatus", DisplayName = "Status", DataType = "String", DisplayOrder = 7 },
                new() { FieldPath = "TaskDueDate", DisplayName = "Due Date", DataType = "DateTime", DisplayOrder = 8 },
                new() { FieldPath = "AssignedToName", DisplayName = "Assigned To", DataType = "String", DisplayOrder = 9 }
            },

            "OutbreakTasksFlattened" => new List<ReportFieldDto>
            {
                new() { FieldPath = "OutbreakName", DisplayName = "Outbreak", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "OutbreakLevel", DisplayName = "Level", DataType = "Int", DisplayOrder = 2 },
                new() { FieldPath = "CaseNumber", DisplayName = "Case Number", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "PatientName", DisplayName = "Patient", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "TaskTitle", DisplayName = "Task", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "TaskStatus", DisplayName = "Status", DataType = "String", DisplayOrder = 6 },
                new() { FieldPath = "TaskDueDate", DisplayName = "Due Date", DataType = "DateTime", DisplayOrder = 7 }
            },

            "CaseTimelineAll" => new List<ReportFieldDto>
            {
                new() { FieldPath = "CaseNumber", DisplayName = "Case Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "PatientName", DisplayName = "Patient", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "EventType", DisplayName = "Event Type", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "EventDate", DisplayName = "Date", DataType = "DateTime", DisplayOrder = 4 },
                new() { FieldPath = "EventDescription", DisplayName = "Description", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "EventUser", DisplayName = "User", DataType = "String", DisplayOrder = 6 }
            },

            "ContactTracingMindMapNodes" => new List<ReportFieldDto>
            {
                new() { FieldPath = "NodeLabel", DisplayName = "Case Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "NodeName", DisplayName = "Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "NodeType", DisplayName = "Type", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "DiseaseName", DisplayName = "Disease", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "OutgoingTransmissions", DisplayName = "Outgoing", DataType = "Int", DisplayOrder = 5 },
                new() { FieldPath = "IncomingExposures", DisplayName = "Incoming", DataType = "Int", DisplayOrder = 6 },
                new() { FieldPath = "FollowUpStatus", DisplayName = "Follow-up Status", DataType = "String", DisplayOrder = 7 }
            },

            "ContactTracingMindMapEdges" => new List<ReportFieldDto>
            {
                new() { FieldPath = "SourceLabel", DisplayName = "Source Case", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "TargetLabel", DisplayName = "Target Case", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "ExposureType", DisplayName = "Exposure Type", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "ExposureStatus", DisplayName = "Status", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "EdgeLabel", DisplayName = "Label", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "ContactClassification", DisplayName = "Classification", DataType = "String", DisplayOrder = 6 }
            },

            "ContactsListSimple" => new List<ReportFieldDto>
            {
                new() { FieldPath = "ContactNumber", DisplayName = "Contact Number", DataType = "String", DisplayOrder = 1 },
                new() { FieldPath = "ContactName", DisplayName = "Name", DataType = "String", DisplayOrder = 2 },
                new() { FieldPath = "ExposedByCase", DisplayName = "Exposed By", DataType = "String", DisplayOrder = 3 },
                new() { FieldPath = "ExposureType", DisplayName = "Exposure Type", DataType = "String", DisplayOrder = 4 },
                new() { FieldPath = "ContactDisease", DisplayName = "Disease", DataType = "String", DisplayOrder = 5 },
                new() { FieldPath = "TotalTasks", DisplayName = "Total Tasks", DataType = "Int", DisplayOrder = 6 },
                new() { FieldPath = "FollowUpStatus", DisplayName = "Follow-up", DataType = "String", DisplayOrder = 7 }
            },

            _ => new List<ReportFieldDto>() // Empty for unknown types
        };
    }
}

public class DefaultFieldsRequest
{
    public string EntityType { get; set; } = "Case";
}

public class SaveReportRequest
{
    public int? ReportId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string EntityType { get; set; } = "Case";
    public string? Category { get; set; }
    public bool IsPublic { get; set; }
    public string? PivotConfiguration { get; set; }
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportFilterDto> Filters { get; set; } = new();
    public List<CollectionQueryDto> CollectionQueries { get; set; } = new();
}

public class PreviewReportRequest
{
    public string EntityType { get; set; } = "Case";
    public List<ReportFieldDto> Fields { get; set; } = new();
    public List<ReportFilterDto> Filters { get; set; } = new();
    public List<CollectionQueryDto> CollectionQueries { get; set; } = new();
}

public class ReportFieldDto
{
    public string FieldPath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? PivotArea { get; set; }
    public string? AggregationType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsCustomField { get; set; }
    public int? CustomFieldDefinitionId { get; set; }
}

public class ReportFilterDto
{
    public string FieldPath { get; set; } = string.Empty;
    public string Operator { get; set; } = "Equals";
    public string? Value { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsCustomField { get; set; }
}
