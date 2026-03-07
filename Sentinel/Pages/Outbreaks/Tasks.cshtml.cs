using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Pages.Outbreaks;

[Authorize]
public class TasksModel : PageModel
{
    private readonly IOutbreakService _outbreakService;

    public TasksModel(IOutbreakService outbreakService)
    {
        _outbreakService = outbreakService;
    }

    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    public Outbreak? Outbreak { get; set; }
    public OutbreakTaskSummary? TaskSummary { get; set; }
    public List<CaseTask> AllTasks { get; set; } = new();
    public List<CaseTask> FilteredTasks { get; set; } = new();
    
    // Filter properties
    [BindProperty(SupportsGet = true)]
    public string? StatusFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? AssigneeFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? TypeFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public bool ShowOverdueOnly { get; set; }

    // For dropdowns
    public List<SelectListItem> AssigneeOptions { get; set; } = new();
    public List<SelectListItem> TypeOptions { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        // Load outbreak
        Outbreak = await _outbreakService.GetByIdAsync(Id);
        if (Outbreak == null)
        {
            return NotFound();
        }

        // Load task summary
        TaskSummary = await _outbreakService.GetTaskStatusSummaryRecursiveAsync(Id);

        // Load all tasks
        if (ShowOverdueOnly)
        {
            AllTasks = await _outbreakService.GetOverdueTasksRecursiveAsync(Id);
        }
        else
        {
            AllTasks = await _outbreakService.GetAllTasksRecursivelyAsync(Id);
        }

        // Apply filters
        FilteredTasks = ApplyFilters(AllTasks);

        // Build dropdown options
        BuildFilterOptions();

        return Page();
    }

    private List<CaseTask> ApplyFilters(List<CaseTask> tasks)
    {
        var filtered = tasks.AsEnumerable();

        // Status filter
        if (!string.IsNullOrEmpty(StatusFilter) && Enum.TryParse<CaseTaskStatus>(StatusFilter, out var status))
        {
            filtered = filtered.Where(t => t.Status == status);
        }

        // Assignee filter
        if (!string.IsNullOrEmpty(AssigneeFilter))
        {
            filtered = filtered.Where(t => 
                t.AssignedToUser?.Email == AssigneeFilter || 
                t.AssignedToUser?.UserName == AssigneeFilter);
        }

        // Type filter
        if (!string.IsNullOrEmpty(TypeFilter))
        {
            filtered = filtered.Where(t => t.TaskType?.Name == TypeFilter);
        }

        return filtered.ToList();
    }

    private void BuildFilterOptions()
    {
        // Assignee options
        var assignees = AllTasks
            .Where(t => t.AssignedToUser != null)
            .Select(t => t.AssignedToUser!.Email ?? t.AssignedToUser.UserName)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        AssigneeOptions = assignees
            .Select(a => new SelectListItem { Value = a, Text = a })
            .Prepend(new SelectListItem { Value = "", Text = "All Assignees" })
            .ToList();

        // Type options
        var types = AllTasks
            .Where(t => t.TaskType != null)
            .Select(t => t.TaskType!.Name)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        TypeOptions = types
            .Select(t => new SelectListItem { Value = t, Text = t })
            .Prepend(new SelectListItem { Value = "", Text = "All Types" })
            .ToList();
    }

    public string GetStatusBadgeClass(CaseTaskStatus status)
    {
        return status switch
        {
            CaseTaskStatus.Completed => "bg-success",
            CaseTaskStatus.InProgress => "bg-info",
            CaseTaskStatus.Pending => "bg-warning",
            CaseTaskStatus.Cancelled => "bg-secondary",
            _ => "bg-secondary"
        };
    }

    public bool IsOverdue(CaseTask task)
    {
        return task.DueDate.HasValue && 
               task.DueDate.Value < DateTime.UtcNow && 
               task.Status != CaseTaskStatus.Completed;
    }
}
