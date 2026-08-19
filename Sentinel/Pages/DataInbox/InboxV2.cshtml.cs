using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using System.Text.Json;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.DataInbox;

[Authorize(Policy = "Permission.Case.View")]
public class InboxV2Model : PageModel
{
    private readonly IDataReviewService _reviewService;
    private readonly ApplicationDbContext _context;

    public InboxV2Model(
        IDataReviewService reviewService, 
        ApplicationDbContext context)
    {
        _reviewService = reviewService;
        _context = context;
    }

    public ReviewQueueResult ReviewQueue { get; set; } = new();
    public ReviewQueueItem? SelectedItem { get; set; }
    public List<LabResult> SelectedItemLabResults { get; set; } = new();
    public int PendingCount { get; set; }
    public int? SelectedItemId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? TimeRange { get; set; } = "24h";

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; } = "Pending";

    [BindProperty(SupportsGet = true)]
    public int? SelectedId { get; set; }

    public async Task OnGetAsync()
    {
        // Calculate date range based on selection
        DateTime? fromDate = TimeRange switch
        {
            "24h" => DateTime.UtcNow.AddHours(-24),
            "48h" => DateTime.UtcNow.AddHours(-48),
            "7d" => DateTime.UtcNow.AddDays(-7),
            "30d" => DateTime.UtcNow.AddDays(-30),
            _ => null
        };

        // Load review queue
        ReviewQueue = await _reviewService.GetReviewQueueAsync(
            entityType: null,
            diseaseIds: null,
            fromDate: fromDate,
            toDate: null,
            reviewStatus: Status == "Pending" ? "Pending" : null,
            skip: 0,
            take: 100
        );

        // Visual grouping: Group items by Case + Time Window for display
        var caseGroupingWindow = TimeSpan.FromHours(6);
        var caseGroups = ReviewQueue.Items
            .Where(i => i.CaseId.HasValue && (Status == "Pending" ? i.ReviewStatus == "Pending" : true))
            .GroupBy(i => new
            { 
                CaseId = i.CaseId!.Value,
                TimeBucket = new DateTime(
                    i.CreatedDate.Year,
                    i.CreatedDate.Month,
                    i.CreatedDate.Day,
                    i.CreatedDate.Hour,
                    0, 0, DateTimeKind.Utc)
            })
            .Where(g => g.Count() > 1)
            .ToList();

        // Create visual groups and mark items
        foreach (var group in caseGroups)
        {
            var orderedItems = group.OrderBy(i => i.CreatedDate).ToList();
            var firstItemTime = orderedItems.First().CreatedDate;

            var itemsInWindow = orderedItems
                .Where(i => (i.CreatedDate - firstItemTime) <= caseGroupingWindow)
                .ToList();

            if (itemsInWindow.Count > 1)
            {
                var groupId = $"CASE_{group.Key.CaseId}_{group.Key.TimeBucket:yyyyMMddHH}";

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
                        ChangeSummary = GetChangeSummaryForMember(i),
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

        PendingCount = ReviewQueue.Items.Count(i => i.ReviewStatus == "Pending");

        // Select an item
        if (SelectedId.HasValue)
        {
            SelectedItemId = SelectedId.Value;
            SelectedItem = ReviewQueue.Items.FirstOrDefault(i => i.Id == SelectedId.Value);
        }
        else if (Status == "Pending" && ReviewQueue.Items.Any(i => i.ReviewStatus == "Pending"))
        {
            // Auto-select first pending item when in Pending view
            SelectedItem = ReviewQueue.Items.First(i => i.ReviewStatus == "Pending");
            SelectedItemId = SelectedItem.Id;
        }
        else if (ReviewQueue.Items.Any())
        {
            // Auto-select first item when in All view
            SelectedItem = ReviewQueue.Items.First();
            SelectedItemId = SelectedItem.Id;
        }

        // Load lab results for the selected case (if applicable)
        if (SelectedItem?.CaseId.HasValue == true)
        {
            SelectedItemLabResults = await _context.LabResults
                .Include(l => l.TestedDisease)
                .Include(l => l.SpecimenType)
                .Include(l => l.Markers)
                    .ThenInclude(m => m.Pathogen)
                .Include(l => l.Markers)
                    .ThenInclude(m => m.TestResult)
                .Where(l => l.CaseId == SelectedItem.CaseId && !l.IsDeleted)
                .OrderByDescending(l => l.ResultDate ?? l.SpecimenCollectionDate ?? l.CreatedAt)
                .Take(5) // Show up to 5 most recent lab results
                .ToListAsync();
        }
    }

    public async Task<IActionResult> OnPostConfirmAsync(int id, string? note)
    {
        var item = await _context.ReviewQueue
            .Include(r => r.Case)
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (item == null)
            return NotFound();

        item.ReviewStatus = ReviewStatuses.Reviewed;
        item.ReviewAction = ReviewActions.Confirmed;
        item.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        item.ReviewedDate = DateTime.UtcNow;
        item.ReviewNotes = note;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostDismissAsync(int id, string? note)
    {
        var item = await _context.ReviewQueue
            .FirstOrDefaultAsync(r => r.Id == id);

        if (item == null)
            return NotFound();

        item.ReviewStatus = ReviewStatuses.Dismissed;
        item.ReviewAction = ReviewActions.Dismissed;
        item.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        item.ReviewedDate = DateTime.UtcNow;
        item.ReviewNotes = note;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostKeepAsNewAsync(int id, string? note)
    {
        var item = await _context.ReviewQueue
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (item == null)
            return NotFound();

        // For duplicate detection, keeping as new means confirming the creation
        item.ReviewStatus = ReviewStatuses.Reviewed;
        item.ReviewAction = ReviewActions.Confirmed;
        item.ReviewedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        item.ReviewedDate = DateTime.UtcNow;
        item.ReviewNotes = note;
        item.SelectedExistingEntityId = null; // Clear any selected duplicate

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(int id, string taskTitle, string? taskDescription)
    {
        if (string.IsNullOrWhiteSpace(taskTitle))
        {
            return new JsonResult(new { success = false, error = "Task title is required" });
        }

        var taskId = await _reviewService.CreateTaskForReviewAsync(
            id,
            taskTitle,
            taskDescription
        );

        if (taskId.HasValue)
        {
            return new JsonResult(new { success = true, taskId });
        }

        return new JsonResult(new { success = false, error = "Failed to create task" });
    }

    private string GetChangeSummaryForMember(ReviewQueueItem item)
    {
        if (!string.IsNullOrEmpty(item.TriggerField) && !string.IsNullOrEmpty(item.ChangeSnapshot))
        {
            try
            {
                var changes = JsonSerializer.Deserialize<Dictionary<string, object>>(item.ChangeSnapshot);
                if (changes != null && changes.ContainsKey(item.TriggerField))
                {
                    var newValue = changes[item.TriggerField]?.ToString() ?? "";
                    return newValue;
                }
            }
            catch
            {
                // Fall through to default
            }
        }

        return item.ChangeType == "PotentialDuplicate" 
            ? "Possible duplicate detected" 
            : "Data changed";
    }
}
