using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Models;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class BulkActionsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;
    private readonly IOutbreakAccessService _outbreakAccessService;
    private readonly IPermissionService _permissionService;

    public BulkActionsModel(
        ApplicationDbContext context,
        IOutbreakService outbreakService,
        IOutbreakAccessService outbreakAccessService,
        IPermissionService permissionService)
    {
        _context = context;
        _outbreakService = outbreakService;
        _outbreakAccessService = outbreakAccessService;
        _permissionService = permissionService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<Case> SelectedRecords { get; set; } = new();
    public SelectList TaskTemplates { get; set; } = null!;
    public SelectList SurveyTemplates { get; set; } = null!;

    [BindProperty]
    public string ActionType { get; set; } = string.Empty;

    [BindProperty]
    public Guid? TemplateId { get; set; }

    [BindProperty]
    public List<Guid> CaseIds { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, string caseIds)
    {
        if (!await _outbreakAccessService.CanAccessOutbreakAsync(id))
        {
            return NotFound();
        }

        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;

        // Parse case IDs
        var ids = caseIds.Split(',')
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();

        if (!ids.Any())
        {
            ErrorMessage = "No cases or contacts selected.";
            return RedirectToPage("Details", new { id });
        }

        var linkedCaseIds = await _context.OutbreakCases
            .Where(oc => oc.OutbreakId == id && oc.IsActive && ids.Contains(oc.CaseId))
            .Select(oc => oc.CaseId)
            .Distinct()
            .ToListAsync();

        if (linkedCaseIds.Count != ids.Distinct().Count())
        {
            return NotFound();
        }

        CaseIds = linkedCaseIds;

        // Load selected cases
        SelectedRecords = await _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .Where(c => linkedCaseIds.Contains(c.Id))
            .ToListAsync();

        await LoadTemplatesAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await _outbreakAccessService.CanAccessOutbreakAsync(id))
        {
            return NotFound();
        }

        if ((ActionType == "task" || ActionType == "survey") &&
            !await CanCreateCaseTasksAsync())
        {
            return Forbid();
        }

        if (!CaseIds.Any())
        {
            ErrorMessage = "No cases or contacts selected.";
            return RedirectToPage("Details", new { id });
        }

        var distinctCaseIds = CaseIds.Distinct().ToList();
        var linkedCaseCount = await _context.OutbreakCases
            .Where(oc => oc.OutbreakId == id && oc.IsActive && distinctCaseIds.Contains(oc.CaseId))
            .Select(oc => oc.CaseId)
            .Distinct()
            .CountAsync();

        if (linkedCaseCount != distinctCaseIds.Count)
        {
            return NotFound();
        }

        CaseIds = distinctCaseIds;

        if (!TemplateId.HasValue)
        {
            ErrorMessage = "Please select a template.";
            var outbreak = await _outbreakService.GetByIdAsync(id);
            if (outbreak != null)
            {
                Outbreak = outbreak;
                SelectedRecords = await _context.Cases
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .Where(c => CaseIds.Contains(c.Id))
                    .ToListAsync();
                await LoadTemplatesAsync();
            }
            return Page();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        bool success = false;

        if (ActionType == "task")
        {
            success = await _outbreakService.BulkAssignTaskAsync(id, TemplateId.Value, CaseIds, userId);
            SuccessMessage = success 
                ? $"Task assigned to {CaseIds.Count} case(s)/contact(s)." 
                : "Failed to assign tasks.";
        }
        else if (ActionType == "survey")
        {
            success = await _outbreakService.BulkAssignSurveyAsync(id, TemplateId.Value, CaseIds, userId);
            SuccessMessage = success 
                ? $"Survey assigned to {CaseIds.Count} case(s)/contact(s)." 
                : "Failed to assign surveys.";
        }

        return RedirectToPage("Details", new { id });
    }

    private async Task<bool> CanCreateCaseTasksAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
               await _permissionService.HasPermissionAsync(
                   userId,
                   PermissionModule.Case,
                   PermissionAction.Edit) &&
               await _permissionService.HasPermissionAsync(
                   userId,
                   PermissionModule.Task,
                   PermissionAction.Create);
    }

    private async Task LoadTemplatesAsync()
    {
        TaskTemplates = new SelectList(
            await _context.TaskTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.Name)
                .ToListAsync(),
            "Id",
            "Name");

        // Only show the current published version of each survey
        SurveyTemplates = new SelectList(
            await _context.SurveyTemplates
                .Where(s => s.IsActive && s.VersionStatus == SurveyVersionStatus.Active)
                .OrderBy(s => s.Name)
                .ToListAsync(),
            "Id",
            "Name");
    }
}
