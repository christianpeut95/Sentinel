using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Pages.Dashboard
{
    [Authorize(Policy = "Permission.Task.View")]
    public class MyTasksModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ITaskService _taskService;

        public MyTasksModel(ApplicationDbContext context, ITaskService taskService)
        {
            _context = context;
            _taskService = taskService;
        }

        public List<CaseTask> AllTasks { get; set; } = new();
        public List<CaseTask> FilteredTasks { get; set; } = new();
        
        // Statistics
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int DueTodayTasks { get; set; }
        public int DueSoonTasks { get; set; }
        public int UrgentTasks { get; set; }

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PriorityFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DueDateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SortBy { get; set; } = "DueDate";

        [BindProperty(SupportsGet = true)]
        public string? SortOrder { get; set; } = "asc";

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return;

            // Get all tasks for user (including completed for statistics)
            AllTasks = await _taskService.GetTasksForUser(userId);

            // Calculate statistics
            CalculateStatistics();

            // Apply filters - by default show only active tasks (exclude completed/cancelled)
            if (string.IsNullOrEmpty(StatusFilter) || StatusFilter == "All")
            {
                // Default filter: show only active tasks
                var activeTasks = AllTasks.Where(t => 
                    t.Status == CaseTaskStatus.Pending || 
                    t.Status == CaseTaskStatus.InProgress ||
                    t.Status == CaseTaskStatus.WaitingForPatient).ToList();
                FilteredTasks = ApplyFilters(activeTasks);
            }
            else
            {
                FilteredTasks = ApplyFilters(AllTasks);
            }

            // Apply sorting
            FilteredTasks = ApplySorting(FilteredTasks);
        }

        private void CalculateStatistics()
        {
            var today = DateTime.Today;
            var soon = DateTime.Today.AddDays(3);

            TotalTasks = AllTasks.Count;
            PendingTasks = AllTasks.Count(t => t.Status == CaseTaskStatus.Pending);
            InProgressTasks = AllTasks.Count(t => t.Status == CaseTaskStatus.InProgress);
            CompletedTasks = AllTasks.Count(t => t.Status == CaseTaskStatus.Completed);
            OverdueTasks = AllTasks.Count(t => t.Status == CaseTaskStatus.Overdue || 
                                              (t.Status != CaseTaskStatus.Completed && 
                                               t.DueDate.HasValue && 
                                               t.DueDate.Value.Date < today));
            DueTodayTasks = AllTasks.Count(t => t.DueDate.HasValue && 
                                               t.DueDate.Value.Date == today &&
                                               t.Status != CaseTaskStatus.Completed);
            DueSoonTasks = AllTasks.Count(t => t.DueDate.HasValue && 
                                              t.DueDate.Value.Date > today && 
                                              t.DueDate.Value.Date <= soon &&
                                              t.Status != CaseTaskStatus.Completed);
            UrgentTasks = AllTasks.Count(t => t.Priority == TaskPriority.Urgent && 
                                             t.Status != CaseTaskStatus.Completed);
        }

        private List<CaseTask> ApplyFilters(List<CaseTask> tasks)
        {
            var filtered = tasks.AsEnumerable();

            // Status filter
            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
            {
                if (Enum.TryParse<CaseTaskStatus>(StatusFilter, out var status))
                {
                    filtered = filtered.Where(t => t.Status == status);
                }
                else if (StatusFilter == "Active")
                {
                    filtered = filtered.Where(t => t.Status == CaseTaskStatus.Pending || 
                                                   t.Status == CaseTaskStatus.InProgress);
                }
            }

            // Priority filter
            if (!string.IsNullOrEmpty(PriorityFilter) && PriorityFilter != "All")
            {
                if (Enum.TryParse<TaskPriority>(PriorityFilter, out var priority))
                {
                    filtered = filtered.Where(t => t.Priority == priority);
                }
            }

            // Due date filter
            if (!string.IsNullOrEmpty(DueDateFilter))
            {
                var today = DateTime.Today;
                filtered = DueDateFilter switch
                {
                    "Overdue" => filtered.Where(t => t.DueDate.HasValue && 
                                                    t.DueDate.Value.Date < today &&
                                                    t.Status != CaseTaskStatus.Completed),
                    "DueToday" => filtered.Where(t => t.DueDate.HasValue && 
                                                     t.DueDate.Value.Date == today),
                    "DueSoon" => filtered.Where(t => t.DueDate.HasValue && 
                                                    t.DueDate.Value.Date > today && 
                                                    t.DueDate.Value.Date <= today.AddDays(3)),
                    "ThisWeek" => filtered.Where(t => t.DueDate.HasValue && 
                                                     t.DueDate.Value.Date <= today.AddDays(7)),
                    "NoDate" => filtered.Where(t => !t.DueDate.HasValue),
                    _ => filtered
                };
            }

            return filtered.ToList();
        }

        private List<CaseTask> ApplySorting(List<CaseTask> tasks)
        {
            var sorted = tasks.AsQueryable();

            bool descending = SortOrder == "desc";

            sorted = SortBy switch
            {
                "DueDate" => descending 
                    ? sorted.OrderByDescending(t => t.DueDate ?? DateTime.MaxValue)
                    : sorted.OrderBy(t => t.DueDate ?? DateTime.MaxValue),
                "Priority" => descending
                    ? sorted.OrderByDescending(t => (int)t.Priority)
                    : sorted.OrderBy(t => (int)t.Priority),
                "Status" => descending
                    ? sorted.OrderByDescending(t => (int)t.Status)
                    : sorted.OrderBy(t => (int)t.Status),
                "Case" => descending
                    ? sorted.OrderByDescending(t => t.Case!.FriendlyId)
                    : sorted.OrderBy(t => t.Case!.FriendlyId),
                "Created" => descending
                    ? sorted.OrderByDescending(t => t.CreatedAt)
                    : sorted.OrderBy(t => t.CreatedAt),
                _ => sorted.OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            };

            return sorted.ToList();
        }

        // Quick actions
        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostCompleteTaskAsync(Guid taskId, string? completionNotes)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToPage();

                await _taskService.CompleteTask(taskId, completionNotes, userId);
                TempData["SuccessMessage"] = "Task marked as completed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error completing task: {ex.Message}";
            }

            return RedirectToPage();
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostStartTaskAsync(Guid taskId)
        {
            try
            {
                var task = await _context.CaseTasks.FindAsync(taskId);
                if (task != null)
                {
                    task.Status = CaseTaskStatus.InProgress;
                    task.ModifiedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Task status updated to In Progress.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating task: {ex.Message}";
            }

            return RedirectToPage();
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostCancelTaskAsync(Guid taskId, string? cancellationReason)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToPage();

                await _taskService.CancelTask(taskId, cancellationReason ?? "Cancelled by user", userId);
                TempData["SuccessMessage"] = "Task cancelled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling task: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
