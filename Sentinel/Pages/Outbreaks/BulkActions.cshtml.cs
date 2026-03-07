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

    public BulkActionsModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<Case> SelectedRecords { get; set; } = new();
    public SelectList TaskTemplates { get; set; } = null!;
    public SelectList SurveyTemplates { get; set; } = null!;

    [BindProperty]
    public string ActionType { get; set; } = string.Empty;

    [BindProperty]
    public int? TemplateId { get; set; }

    [BindProperty]
    public List<Guid> CaseIds { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, string caseIds)
    {
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

        CaseIds = ids;

        // Load selected cases
        SelectedRecords = await _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        await LoadTemplatesAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!CaseIds.Any())
        {
            ErrorMessage = "No cases or contacts selected.";
            return RedirectToPage("Details", new { id });
        }

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

    private async Task LoadTemplatesAsync()
    {
        TaskTemplates = new SelectList(
            await _context.TaskTemplates
                .OrderBy(t => t.Name)
                .ToListAsync(),
            "Id",
            "Name");

        SurveyTemplates = new SelectList(
            await _context.SurveyTemplates
                .OrderBy(s => s.Name)
                .ToListAsync(),
            "Id",
            "Name");
    }
}
