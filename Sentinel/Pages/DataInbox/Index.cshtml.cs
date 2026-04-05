using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using System.Text.Json;
using Sentinel.Models;
using Newtonsoft.Json.Linq;

namespace Sentinel.Pages.DataInbox;

[Authorize(Policy = "Permission.Case.View")]
public class IndexModel : PageModel
{
    private readonly IDataReviewService _reviewService;
    private readonly ApplicationDbContext _context;
    private readonly ICollectionMappingService _collectionMappingService;
    private readonly ISurveyMappingService _surveyMappingService;

    public IndexModel(
        IDataReviewService reviewService, 
        ApplicationDbContext context,
        ICollectionMappingService collectionMappingService,
        ISurveyMappingService surveyMappingService)
    {
        _reviewService = reviewService;
        _context = context;
        _collectionMappingService = collectionMappingService;
        _surveyMappingService = surveyMappingService;
    }

    public ReviewQueueResult ReviewQueue { get; set; } = new();
    public List<EntityTypeOption> EntityTypes { get; set; } = new();
    public List<DiseaseOption> Diseases { get; set; } = new();
    public int PendingCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EntityType { get; set; }

    [BindProperty(SupportsGet = true)]
    public List<Guid>? DiseaseIds { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TimeRange { get; set; } = "24h";

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; } = "Pending";

    public async Task OnGetAsync()
    {
        // Load filter options
        await LoadFilterOptionsAsync();

        // Calculate date range based on selection
        DateTime? fromDate = TimeRange switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "48h" => DateTime.UtcNow.AddHours(-48),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => null
        };

        // Load review queue - if no status filter, default to showing pending first, then recent completed
        if (string.IsNullOrEmpty(Status) || Status == "Pending")
        {
            // Get pending items
            ReviewQueue = await _reviewService.GetReviewQueueAsync(
                entityType: EntityType,
                diseaseIds: DiseaseIds,
                fromDate: fromDate,
                toDate: null,
                reviewStatus: "Pending",
                skip: 0,
                take: 50
            );

            // Visual grouping: Group items by Case + Time Window for display
            // This groups multiple changes to the SAME case within a time window
            // (even if they're different fields and have different GroupKeys in the database)
            var caseGroupingWindow = TimeSpan.FromHours(6);
            var caseGroups = ReviewQueue.Items
                .Where(i => i.CaseId.HasValue)
                .GroupBy(i => new 
                { 
                    CaseId = i.CaseId!.Value,
                    // Round time to nearest hour for grouping
                    TimeBucket = new DateTime(
                        i.CreatedDate.Year,
                        i.CreatedDate.Month,
                        i.CreatedDate.Day,
                        i.CreatedDate.Hour,
                        0, 0, DateTimeKind.Utc)
                })
                .Where(g => g.Count() > 1) // Only groups with multiple items
                .ToList();

            // Create visual groups and mark items
            var visualGroups = new Dictionary<string, List<ReviewQueueItem>>();
            foreach (var group in caseGroups)
            {
                // Check if items are within the grouping window
                var orderedItems = group.OrderBy(i => i.CreatedDate).ToList();
                var firstItemTime = orderedItems.First().CreatedDate;
                
                var itemsInWindow = orderedItems
                    .Where(i => (i.CreatedDate - firstItemTime) <= caseGroupingWindow)
                    .ToList();

                if (itemsInWindow.Count > 1)
                {
                    var groupId = $"CASE_{group.Key.CaseId}_{group.Key.TimeBucket:yyyyMMddHH}";
                    visualGroups[groupId] = itemsInWindow;

                    // Mark the representative item (earliest one)
                    var representative = itemsInWindow.First();
                    representative.VisualGroupId = groupId;
                    representative.VisualGroupCount = itemsInWindow.Count;
                    representative.VisualGroupMembers = itemsInWindow
                        .Select(i => new VisualGroupMember
                        {
                            Id = i.Id,
                            EntityType = i.EntityType,
                            TriggerField = i.TriggerField,
                            ChangeType = i.ChangeType,
                            ChangeSummary = GetChangeSummary(i),
                            CreatedDate = i.CreatedDate
                        })
                        .ToList();

                    // Mark other items as part of this visual group (won't be displayed)
                    foreach (var item in itemsInWindow.Skip(1))
                    {
                        item.IsPartOfVisualGroup = true;
                        item.VisualGroupId = groupId;
                    }
                }
            }

            // Remove items that are part of a visual group (except the representative)
            ReviewQueue.Items = ReviewQueue.Items
                .Where(i => !i.IsPartOfVisualGroup)
                .ToList();

            // Also get recent completed items to show at bottom
            var completedQueue = await _reviewService.GetReviewQueueAsync(
                entityType: EntityType,
                diseaseIds: DiseaseIds,
                fromDate: DateTime.UtcNow.AddDays(-7), // Last 7 days
                toDate: null,
                reviewStatus: "Reviewed",
                skip: 0,
                take: 10
            );

            // Combine pending and completed
            ReviewQueue = new ReviewQueueResult
            {
                Items = ReviewQueue.Items.Concat(completedQueue.Items).ToList(),
                TotalCount = ReviewQueue.TotalCount,
                PendingCount = ReviewQueue.PendingCount,
                HasMore = ReviewQueue.HasMore
            };
        }
        else
        {
            // Load based on filter
            ReviewQueue = await _reviewService.GetReviewQueueAsync(
                entityType: EntityType,
                diseaseIds: DiseaseIds,
                fromDate: fromDate,
                toDate: null,
                reviewStatus: Status,
                skip: 0,
                take: 50
            );
        }

        PendingCount = ReviewQueue.PendingCount;
    }

    private async Task LoadFilterOptionsAsync()
    {
        // Entity types
        EntityTypes = new List<EntityTypeOption>
        {
            new() { Value = "", Text = "All Types" },
            new() { Value = "LabResult", Text = "Lab Results", Icon = "flask" },
            new() { Value = "Exposure", Text = "Exposures", Icon = "geo-alt" },
            new() { Value = "Contact", Text = "Contacts", Icon = "people" },
            new() { Value = "CaseChange", Text = "Case Changes", Icon = "pencil-square" },
            new() { Value = "ClinicalNotification", Text = "Clinical Notifications", Icon = "clipboard-pulse" }
        };

        // Load diseases with pending reviews
        Diseases = await _context.Diseases
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DiseaseOption
            {
                Id = d.Id,
                Name = d.Name
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id)
    {
        var result = await _reviewService.ConfirmReviewAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Review confirmed successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to confirm review.";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDismissAsync(int id)
    {
        var result = await _reviewService.DismissReviewAsync(id);
        if (result)
        {
            TempData["SuccessMessage"] = "Review dismissed successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to dismiss review.";
        }

        return RedirectToPage();
    }

    public async Task<JsonResult> OnGetGroupMembersAsync(string groupKey)
    {
        if (string.IsNullOrEmpty(groupKey))
        {
            return new JsonResult(new List<object>());
        }

        System.Diagnostics.Debug.WriteLine($"[GROUP] ===== GROUP EXPANSION DEBUG =====");
        System.Diagnostics.Debug.WriteLine($"[GROUP] Requested GroupKey: '{groupKey}'");

        // First, let's see ALL pending items that might be related
        var allPendingForDisease = await _context.ReviewQueue
            .Where(r => r.ReviewStatus == "Pending" && r.EntityType == "CaseChange")
            .Select(r => new { r.Id, r.GroupKey, r.GroupCount, CaseId = r.Case != null ? r.Case.FriendlyId : "null" })
            .Take(20)
            .ToListAsync();

        System.Diagnostics.Debug.WriteLine($"[GROUP] All pending CaseChange items:");
        foreach (var item in allPendingForDisease)
        {
            var match = item.GroupKey == groupKey ? "? MATCH" : "";
            System.Diagnostics.Debug.WriteLine($"[GROUP]   - ID: {item.Id}, Case: {item.CaseId}, GroupKey: '{item.GroupKey}', Count: {item.GroupCount} {match}");
        }

        // Get items matching this exact GroupKey
        var members = await _context.ReviewQueue
            .Where(r => r.GroupKey == groupKey && r.ReviewStatus == "Pending")
            .Include(r => r.Case)
            .Include(r => r.Patient)
            .Include(r => r.Disease)
            .OrderBy(r => r.CreatedDate)
            .ToListAsync();

        System.Diagnostics.Debug.WriteLine($"[GROUP] Exact matches for GroupKey '{groupKey}': {members.Count}");
        
        foreach (var m in members)
        {
            System.Diagnostics.Debug.WriteLine($"[GROUP]   ? ID: {m.Id}, Case: {m.Case?.FriendlyId}, Patient: {m.Patient?.FamilyName}, GroupCount: {m.GroupCount}");
        }

        var result = members.Select(m => new
        {
            id = m.Id,
            entityType = m.EntityType,
            caseFriendlyId = m.Case?.FriendlyId,
            patientName = m.Patient != null ? $"{m.Patient.GivenName} {m.Patient.FamilyName}" : null,
            changeSummary = GetChangeSummaryForJson(m),
            createdDate = m.CreatedDate
        }).ToList();

        System.Diagnostics.Debug.WriteLine($"[GROUP] Returning {result.Count} items to client");
        System.Diagnostics.Debug.WriteLine($"[GROUP] ===== END GROUP DEBUG =====");

        return new JsonResult(result);
    }

    public async Task<IActionResult> OnPostQuickConfirmAsync(int id)
    {
        var success = await _reviewService.ConfirmReviewAsync(id, "Quick confirmed from list");
        
        if (success)
        {
            return new JsonResult(new { success = true });
        }

        return new JsonResult(new { success = false });
    }

    /// <summary>
    /// Quick handler for always-review scenarios (PendingCreation) from the queue
    /// Creates the patient and processes related entities - matches Review.cshtml.cs logic
    /// </summary>
    public async Task<IActionResult> OnPostQuickResolveAlwaysReviewAsync(int id)
    {
        try
        {
            // Get the full ReviewQueue entity
            var reviewQueue = await _context.ReviewQueue
                .Include(r => r.Task)
                    .ThenInclude(t => t!.Case)
                .Include(r => r.Task)
                    .ThenInclude(t => t!.TaskTemplate)
                .FirstOrDefaultAsync(r => r.Id == id);
                
            if (reviewQueue == null)
            {
                return new JsonResult(new { success = false, error = "Review item not found" });
            }
            
            // Extract patient data from ProposedEntityDataJson
            var newPatient = await ExtractPatientFromProposedData(reviewQueue.ProposedEntityDataJson);
            if (newPatient == null)
            {
                return new JsonResult(new { success = false, error = "Could not extract patient data" });
            }
            
            // Create patient
            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();
            
            // Link case to patient if needed
            if (reviewQueue.Task?.Case != null && 
                (reviewQueue.Task.Case.PatientId == null || reviewQueue.Task.Case.PatientId == Guid.Empty))
            {
                reviewQueue.Task.Case.PatientId = newPatient.Id;
                await _context.SaveChangesAsync();
            }
            
            // Reprocess collection mappings to create related entities
            try
            {
                await ReprocessCollectionMappingsAsync(reviewQueue, newPatient.Id, patientAlreadyExists: true);
            }
            catch (Exception reprocessEx)
            {
                // Log but continue - patient was created successfully
                System.Diagnostics.Debug.WriteLine($"Reprocessing warning: {reprocessEx.Message}");
            }
            
            // Mark review as confirmed
            var result = await _reviewService.ConfirmReviewAsync(id, 
                $"Quick approved: Created patient {newPatient.FriendlyId}");
            
            return new JsonResult(new { success = result });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in quick resolve: {ex.Message}");
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
    
    private async Task<Patient?> ExtractPatientFromProposedData(string? proposedDataJson)
    {
        if (string.IsNullOrEmpty(proposedDataJson))
            return null;
            
        try
        {
            var proposedData = JsonSerializer.Deserialize<Dictionary<string, object>>(proposedDataJson);
            if (proposedData == null)
                return null;
            
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                GivenName = GetValueFromDict(proposedData, "GivenName") ?? "",
                FamilyName = GetValueFromDict(proposedData, "FamilyName") ?? "",
                DateOfBirth = GetDateFromDict(proposedData, "DateOfBirth"),
                MobilePhone = GetValueFromDict(proposedData, "MobilePhone"),
                EmailAddress = GetValueFromDict(proposedData, "EmailAddress"),
                AddressLine = GetValueFromDict(proposedData, "AddressLine"),
                City = GetValueFromDict(proposedData, "City"),
                StateId = await GetStateIdFromStringAsync(GetValueFromDict(proposedData, "State")),
                PostalCode = GetValueFromDict(proposedData, "PostalCode"),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = User.Identity?.Name
            };
            
            // Generate FriendlyId
            var year = DateTime.UtcNow.Year;
            var prefix = $"P-{year}-";
            var maxId = _context.Patients
                .Where(p => p.FriendlyId.StartsWith(prefix))
                .Select(p => p.FriendlyId)
                .AsEnumerable()
                .Select(id => int.TryParse(id.Split('-').Last(), out var num) ? num : 0)
                .DefaultIfEmpty(0)
                .Max();
            
            patient.FriendlyId = $"{prefix}{(maxId + 1):D4}";
            
            return patient;
        }
        catch
        {
            return null;
        }
    }
    
    private string? GetValueFromDict(Dictionary<string, object> dict, string key)
    {
        return dict.ContainsKey(key) ? dict[key]?.ToString() : null;
    }
    
    private DateTime? GetDateFromDict(Dictionary<string, object> dict, string key)
    {
        if (dict.ContainsKey(key) && DateTime.TryParse(dict[key]?.ToString(), out var date))
            return date;
        return null;
    }
    
    /// <summary>
    /// Re-process collection mappings after patient is created
    /// This creates related entities (Contacts, Exposures, etc.) from the survey data
    /// </summary>
    private async Task ReprocessCollectionMappingsAsync(
        ReviewQueue reviewQueue, 
        Guid resolvedPatientId,
        bool patientAlreadyExists = true)
    {
        try
        {
            if (reviewQueue.Task == null)
            {
                return;
            }
            
            // Extract survey responses from CollectionSourceDataJson
            if (string.IsNullOrEmpty(reviewQueue.CollectionSourceDataJson))
            {
                return;
            }

            // Parse collection source data
            Dictionary<string, object>? sourceData;
            try
            {
                sourceData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    reviewQueue.CollectionSourceDataJson
                );
            }
            catch
            {
                return;
            }

            if (sourceData == null || !sourceData.ContainsKey("QuestionName"))
            {
                return;
            }

            // Get survey response ID if available
            Guid surveyResponseId = Guid.Empty;
            if (sourceData.ContainsKey("SurveyResponseId") && sourceData["SurveyResponseId"] != null)
            {
                var surveyResponseElement = sourceData["SurveyResponseId"] as JsonElement?;
                if (surveyResponseElement.HasValue && 
                    surveyResponseElement.Value.ValueKind == JsonValueKind.String)
                {
                    Guid.TryParse(surveyResponseElement.Value.GetString(), out surveyResponseId);
                }
            }
            
            var questionName = sourceData["QuestionName"]?.ToString() ?? "unknown";

            // Get the collection mapping config for this question
            var diseaseId = reviewQueue.Task.Case?.DiseaseId ?? reviewQueue.DiseaseId;
            
            var mappings = await _surveyMappingService.GetActiveMappingsAsync(
                surveyTemplateId: reviewQueue.Task.TaskTemplate?.SurveyTemplateId,
                taskTemplateId: reviewQueue.Task.TaskTemplateId,
                diseaseId: diseaseId
            );

            var collectionMapping = mappings.FirstOrDefault(m => 
                m.SurveyQuestionName == questionName && 
                !string.IsNullOrEmpty(m.CollectionConfigJson)
            );

            if (collectionMapping == null)
            {
                return;
            }

            // Parse collection config
            var config = JsonSerializer.Deserialize<CollectionMappingConfig>(
                collectionMapping.CollectionConfigJson
            );

            if (config == null)
            {
                return;
            }

            JArray rowData;
            
            // ? CRITICAL FIX: Check if specific row was stored in CollectionSourceDataJson
            if (sourceData.ContainsKey("RowData") && sourceData["RowData"] != null)
            {
                var rowDataElement = sourceData["RowData"] as JsonElement?;
                if (rowDataElement.HasValue && rowDataElement.Value.ValueKind == JsonValueKind.String)
                {
                    var rowJson = rowDataElement.Value.GetString();
                    if (!string.IsNullOrEmpty(rowJson))
                    {
                        var singleRow = JObject.Parse(rowJson);
                        rowData = new JArray { singleRow };
                    }
                    else
                    {
                        rowData = GetAllRowsFromSurveyResponse(reviewQueue.Task, questionName);
                    }
                }
                else
                {
                    rowData = GetAllRowsFromSurveyResponse(reviewQueue.Task, questionName);
                }
            }
            else
            {
                // Legacy: No RowData - use all rows
                rowData = GetAllRowsFromSurveyResponse(reviewQueue.Task, questionName);
            }
            
            if (rowData == null || rowData.Count == 0)
            {
                return;
            }

            // Build context with resolved patient
            var context = new SurveySubmissionContext
            {
                CaseId = reviewQueue.Task.CaseId,
                PatientId = resolvedPatientId,
                TaskId = reviewQueue.Task.Id,
                DiseaseId = reviewQueue.Task.Case?.DiseaseId ?? Guid.Empty,
                JurisdictionId = null,
                SubmittedBy = User.Identity?.Name,
                SubmittedDate = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["ResolvedFromDuplicate"] = true,
                    ["PatientAlreadyExists"] = patientAlreadyExists,
                    ["OriginalReviewId"] = reviewQueue.Id,
                    ["Jurisdiction1Id"] = reviewQueue.Task.Case?.Jurisdiction1Id ?? 0
                }
            };

            // Process the collection with context
            var result = await _collectionMappingService.ProcessCollectionWithContextAsync(
                surveyResponseId: surveyResponseId,
                questionName: questionName,
                rowData: rowData,
                config: config,
                context: context
            );

            // Save all entities created
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reprocessing collection mappings: {ex.Message}");
            throw;
        }
    }
    
    private JArray? GetAllRowsFromSurveyResponse(CaseTask task, string questionName)
    {
        if (string.IsNullOrEmpty(task.SurveyResponseJson))
            return null;

        var surveyData = JsonSerializer.Deserialize<Dictionary<string, object>>(task.SurveyResponseJson);
        if (surveyData == null || !surveyData.ContainsKey(questionName))
            return null;

        var rowDataElement = surveyData[questionName] as JsonElement?;
        if (rowDataElement == null)
            return null;

        if (rowDataElement.Value.ValueKind == JsonValueKind.Array)
        {
            return JArray.Parse(rowDataElement.Value.GetRawText());
        }
        else if (rowDataElement.Value.ValueKind == JsonValueKind.Object)
        {
            var singleRow = JObject.Parse(rowDataElement.Value.GetRawText());
            return new JArray { singleRow };
        }
        
        return null;
    }

    private string GetChangeSummaryForJson(Sentinel.Models.ReviewQueue item)
    {
        if (item.EntityType == "CaseChange" && !string.IsNullOrEmpty(item.TriggerField))
        {
            try
            {
                if (!string.IsNullOrEmpty(item.ChangeSnapshot))
                {
                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(item.ChangeSnapshot);
                    if (snapshot != null)
                    {
                        var oldVal = "";
                        var newVal = "";
                        
                        if (snapshot.ContainsKey("oldValueDisplay") && snapshot["oldValueDisplay"].ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            oldVal = snapshot["oldValueDisplay"].GetString() ?? "";
                        }
                        
                        if (snapshot.ContainsKey("newValueDisplay") && snapshot["newValueDisplay"].ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            newVal = snapshot["newValueDisplay"].GetString() ?? "";
                        }
                        
                        if (!string.IsNullOrEmpty(oldVal) && !string.IsNullOrEmpty(newVal))
                        {
                            var fieldName = item.TriggerField == "ConfirmationStatusId" ? "Status" : 
                                          item.TriggerField == "DiseaseId" ? "Disease" : item.TriggerField;
                            return $"{fieldName}: {oldVal} ? {newVal}";
                        }
                    }
                }
            }
            catch { }
        }
        
        return item.ChangeType switch
        {
            "New" => item.EntityType == "NewCase" ? "New case created" : $"New {item.EntityType} added",
            _ => item.ChangeType
        };
    }

    private string GetChangeSummary(ReviewQueueItem item)
    {
        // ? Enhanced summaries for collection reviews
        if (item.ChangeType == "PendingCreation")
        {
            return "New contact from survey - awaiting patient creation";
        }
        
        if (item.ChangeType == "PotentialDuplicate")
        {
            return "Possible duplicate contact detected - select or create patient";
        }
        
        // Case change summaries
        if (item.EntityType == "CaseChange" && !string.IsNullOrEmpty(item.TriggerField))
        {
            try
            {
                if (!string.IsNullOrEmpty(item.ChangeSnapshot))
                {
                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, System.Text.Json.JsonElement>>(item.ChangeSnapshot);
                    if (snapshot != null)
                    {
                        var oldVal = "";
                        var newVal = "";
                        
                        if (snapshot.ContainsKey("oldValueDisplay") && snapshot["oldValueDisplay"].ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            oldVal = snapshot["oldValueDisplay"].GetString() ?? "";
                        }
                        
                        if (snapshot.ContainsKey("newValueDisplay") && snapshot["newValueDisplay"].ValueKind != System.Text.Json.JsonValueKind.Null)
                        {
                            newVal = snapshot["newValueDisplay"].GetString() ?? "";
                        }
                        
                        if (!string.IsNullOrEmpty(oldVal) && !string.IsNullOrEmpty(newVal))
                        {
                            var fieldName = item.TriggerField == "ConfirmationStatusId" ? "Status" : 
                                          item.TriggerField == "DiseaseId" ? "Disease" : item.TriggerField;
                            return $"{fieldName}: {oldVal} ? {newVal}";
                        }
                    }
                }
            }
            catch { }
        }
        
        return item.ChangeType switch
        {
            "New" => item.EntityType == "NewCase" ? "New case created" : $"New {item.EntityType} added",
            _ => item.ChangeType
        };
    }
    
    /// <summary>
    /// Extract proposed patient demographics from ProposedEntityDataJson for PendingCreation reviews
    /// </summary>
    public async Task<Dictionary<string, string>> GetProposedPatientDataAsync(int reviewQueueId)
    {
        var result = new Dictionary<string, string>();
        
        try
        {
            var reviewQueue = await _context.ReviewQueue
                .FirstOrDefaultAsync(r => r.Id == reviewQueueId);
            
            if (reviewQueue == null || string.IsNullOrEmpty(reviewQueue.ProposedEntityDataJson))
            {
                return result;
            }
            
            var proposedData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                reviewQueue.ProposedEntityDataJson);
            
            if (proposedData != null)
            {
                if (proposedData.ContainsKey("GivenName"))
                    result["GivenName"] = proposedData["GivenName"]?.ToString() ?? "";
                if (proposedData.ContainsKey("FamilyName"))
                    result["FamilyName"] = proposedData["FamilyName"]?.ToString() ?? "";
                if (proposedData.ContainsKey("DateOfBirth"))
                    result["DateOfBirth"] = proposedData["DateOfBirth"]?.ToString() ?? "";
                if (proposedData.ContainsKey("MobilePhone"))
                    result["Phone"] = proposedData["MobilePhone"]?.ToString() ?? "";
                if (proposedData.ContainsKey("City"))
                    result["City"] = proposedData["City"]?.ToString() ?? "";
            }
        }
        catch { }
        
        return result;
    }

    private async Task<int?> GetStateIdFromStringAsync(string? stateString)
    {
        if (string.IsNullOrWhiteSpace(stateString))
        {
            return null;
        }

        // Try to find by code (e.g., "NSW") or name (e.g., "New South Wales")
        var state = await _context.States
            .FirstOrDefaultAsync(s => s.Code == stateString || s.Name == stateString);

        return state?.Id;
    }
}

public class EntityTypeOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = "circle";
}

public class DiseaseOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
