using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Models;
using Surveillance_MVP.Services;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Dashboard;

[Authorize(Roles = "Admin,Supervisor")]
public class SuperviseInterviewsModel : PageModel
{
    private readonly ITaskAssignmentService _assignmentService;
    private readonly ILogger<SuperviseInterviewsModel> _logger;

    public SuperviseInterviewsModel(ITaskAssignmentService assignmentService, ILogger<SuperviseInterviewsModel> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    public SupervisorDashboardData? DashboardData { get; set; }
    public List<ApplicationUser> AvailableWorkers { get; set; } = new();
    public List<CaseTask> CurrentlyAssignedTasks { get; set; } = new();
    
    // Pagination properties
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 25;
    
    public int TotalTasks { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalTasks / PageSize);
    
    // Filter properties
    [BindProperty(SupportsGet = true)]
    public string? FilterWorker { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? FilterPriority { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? SortBy { get; set; } = "Priority";
    
    [BindProperty(SupportsGet = true)]
    public string? SortOrder { get; set; } = "asc";

    [BindProperty]
    public Guid SelectedTaskId { get; set; }

    [BindProperty]
    public string? SelectedWorkerId { get; set; }

    [BindProperty]
    public string? ReassignReason { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            // Get summary data (always needed)
            DashboardData = await _assignmentService.GetSupervisorDashboardSummaryAsync();
            
            // Get paginated tasks with filters
            var (tasks, totalCount) = await _assignmentService.GetAssignedInterviewTasksPaginatedAsync(
                pageNumber: PageNumber,
                pageSize: PageSize,
                workerId: FilterWorker,
                priority: FilterPriority,
                searchTerm: SearchTerm,
                sortBy: SortBy,
                sortOrder: SortOrder
            );
            
            CurrentlyAssignedTasks = tasks;
            TotalTasks = totalCount;
            
            // Get available workers for filters and assignment
            AvailableWorkers = await _assignmentService.GetAllInterviewWorkersAsync();
            
            _logger.LogInformation(
                "Loaded supervisor dashboard: Page {Page}/{TotalPages}, Showing {Count}/{Total} tasks", 
                PageNumber, TotalPages, CurrentlyAssignedTasks.Count, TotalTasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading supervisor dashboard data");
            DashboardData = new SupervisorDashboardData();
            CurrentlyAssignedTasks = new List<CaseTask>();
            TotalTasks = 0;
        }
    }

    public async Task<IActionResult> OnPostAssignTaskAsync()
    {
        if (string.IsNullOrEmpty(SelectedWorkerId))
        {
            TempData["ErrorMessage"] = "Please select a worker";
            return RedirectToPage();
        }

        try
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.ManuallyAssignTaskAsync(SelectedTaskId, SelectedWorkerId, supervisorId);

            if (success)
            {
                TempData["SuccessMessage"] = "Task assigned successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to assign task";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning task");
            TempData["ErrorMessage"] = "Failed to assign task";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReassignTaskAsync()
    {
        if (string.IsNullOrEmpty(SelectedWorkerId))
        {
            TempData["ErrorMessage"] = "Please select a worker";
            return RedirectToPage();
        }

        if (string.IsNullOrWhiteSpace(ReassignReason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for reassignment";
            return RedirectToPage();
        }

        try
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.ReassignTaskAsync(SelectedTaskId, SelectedWorkerId, supervisorId, ReassignReason);

            if (success)
            {
                TempData["SuccessMessage"] = "Task reassigned successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reassign task";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning task");
            TempData["ErrorMessage"] = "Failed to reassign task";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUnassignTaskAsync()
    {
        if (string.IsNullOrWhiteSpace(ReassignReason))
        {
            TempData["ErrorMessage"] = "Please provide a reason for unassignment";
            return RedirectToPage();
        }

        try
        {
            var supervisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var success = await _assignmentService.ReassignTaskAsync(SelectedTaskId, null, supervisorId, ReassignReason);

            if (success)
            {
                TempData["SuccessMessage"] = "Task unassigned and returned to pool";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to unassign task";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unassigning task");
            TempData["ErrorMessage"] = "Failed to unassign task";
        }

        return RedirectToPage();
    }
}
