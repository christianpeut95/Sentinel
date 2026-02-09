using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Services;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class ClassifyCasesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public ClassifyCasesModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<OutbreakCase> UnclassifiedCases { get; set; } = new();
    public List<OutbreakCase> ClassifiedCases { get; set; } = new();
    public OutbreakCaseDefinition? ActiveDefinition { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        
        // Get active case definition
        ActiveDefinition = await _context.OutbreakCaseDefinitions
            .Where(d => d.OutbreakId == id && d.IsActive)
            .OrderByDescending(d => d.Version)
            .FirstOrDefaultAsync();

        // Load all outbreak cases (type = Case only, not contacts)
        var allCases = await _context.OutbreakCases
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Disease)
            .Where(oc => oc.OutbreakId == id && oc.IsActive && oc.Case.Type == CaseType.Case)
            .OrderBy(oc => oc.LinkedDate)
            .ToListAsync();

        // Split into classified and unclassified
        UnclassifiedCases = allCases.Where(oc => !oc.Classification.HasValue).ToList();
        ClassifiedCases = allCases.Where(oc => oc.Classification.HasValue).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostClassifyAsync(int id, int outbreakCaseId, CaseClassification classification, string? notes)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        
        var success = await _outbreakService.ClassifyCaseAsync(outbreakCaseId, classification, notes, userId);
        
        if (success)
        {
            SuccessMessage = "Case classified successfully.";
        }
        else
        {
            ErrorMessage = "Failed to classify case.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostBulkClassifyAsync(int id, List<int> selectedCaseIds, CaseClassification classification)
    {
        if (!selectedCaseIds.Any())
        {
            ErrorMessage = "No cases selected.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var successCount = 0;

        foreach (var outbreakCaseId in selectedCaseIds)
        {
            var success = await _outbreakService.ClassifyCaseAsync(outbreakCaseId, classification, "Bulk classification", userId);
            if (success) successCount++;
        }

        SuccessMessage = $"Successfully classified {successCount} of {selectedCaseIds.Count} cases.";
        return RedirectToPage(new { id });
    }
}
