using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.Edit")]
public class LinkCasesModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IOutbreakService _outbreakService;

    public LinkCasesModel(ApplicationDbContext context, IOutbreakService outbreakService)
    {
        _context = context;
        _outbreakService = outbreakService;
    }

    public Outbreak Outbreak { get; set; } = null!;
    public List<OutbreakSearchQuery> SavedQueries { get; set; } = new();
    public List<Case> SearchResults { get; set; } = new();
    
    [BindProperty]
    public SearchCriteria Criteria { get; set; } = new();
    
    [BindProperty]
    public string? SavedQueryName { get; set; }
    
    [BindProperty]
    public bool AutoLink { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, int? queryId = null)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        SavedQueries = await _outbreakService.GetSearchQueriesAsync(id);

        // Load saved query if specified
        if (queryId.HasValue)
        {
            var query = SavedQueries.FirstOrDefault(q => q.Id == queryId.Value);
            if (query != null)
            {
                Criteria = JsonSerializer.Deserialize<SearchCriteria>(query.QueryJson) ?? new();
                await PerformSearchAsync();
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSearchAsync(int id)
    {
        var outbreak = await _outbreakService.GetByIdAsync(id);
        if (outbreak == null)
        {
            return NotFound();
        }

        Outbreak = outbreak;
        SavedQueries = await _outbreakService.GetSearchQueriesAsync(id);
        
        await PerformSearchAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSaveQueryAsync(int id)
    {
        if (string.IsNullOrWhiteSpace(SavedQueryName))
        {
            ErrorMessage = "Please provide a name for the search query.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var queryJson = JsonSerializer.Serialize(Criteria);

        var query = new OutbreakSearchQuery
        {
            OutbreakId = id,
            QueryName = SavedQueryName,
            QueryJson = queryJson,
            IsAutoLink = AutoLink
        };

        await _outbreakService.CreateSearchQueryAsync(query, userId);

        SuccessMessage = $"Search query '{SavedQueryName}' saved successfully.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostLinkCasesAsync(int id, List<Guid> selectedCaseIds, CaseClassification? classification)
    {
        if (!selectedCaseIds.Any())
        {
            ErrorMessage = "Please select at least one case or contact to link.";
            return RedirectToPage(new { id });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var linkedCount = 0;

        foreach (var caseId in selectedCaseIds)
        {
            try
            {
                await _outbreakService.LinkCaseAsync(
                    id, 
                    caseId, 
                    classification, 
                    LinkMethod.Manual, 
                    userId);
                linkedCount++;
            }
            catch
            {
                // Skip duplicates or errors
            }
        }

        SuccessMessage = $"Successfully linked {linkedCount} case(s)/contact(s) to the outbreak.";
        return RedirectToPage("/Outbreaks/Details", new { id });
    }

    private async Task PerformSearchAsync()
    {
        var query = _context.Cases
            .Include(c => c.Patient)
            .Include(c => c.Disease)
            .Where(c => !c.IsDeleted);

        // Filter by case type
        if (Criteria.CaseType.HasValue)
        {
            query = query.Where(c => c.Type == Criteria.CaseType.Value);
        }

        // Filter by disease
        if (Criteria.DiseaseId.HasValue)
        {
            query = query.Where(c => c.DiseaseId == Criteria.DiseaseId.Value);
        }

        // Filter by date range
        if (Criteria.StartDate.HasValue)
        {
            query = query.Where(c => 
                (c.DateOfOnset.HasValue && c.DateOfOnset.Value >= Criteria.StartDate.Value) ||
                (c.DateOfNotification.HasValue && c.DateOfNotification.Value >= Criteria.StartDate.Value));
        }

        if (Criteria.EndDate.HasValue)
        {
            query = query.Where(c => 
                (c.DateOfOnset.HasValue && c.DateOfOnset.Value <= Criteria.EndDate.Value) ||
                (c.DateOfNotification.HasValue && c.DateOfNotification.Value <= Criteria.EndDate.Value));
        }

        // Filter by patient name
        if (!string.IsNullOrWhiteSpace(Criteria.PatientName))
        {
            var searchTerm = Criteria.PatientName.ToLower();
            query = query.Where(c => 
                c.Patient != null && 
                (c.Patient.GivenName.ToLower().Contains(searchTerm) || 
                 c.Patient.FamilyName.ToLower().Contains(searchTerm)));
        }

        // Filter by case ID
        if (!string.IsNullOrWhiteSpace(Criteria.CaseId))
        {
            query = query.Where(c => c.FriendlyId.Contains(Criteria.CaseId));
        }

        // Exclude already linked cases
        var linkedCaseIds = await _context.OutbreakCases
            .Where(oc => oc.OutbreakId == Outbreak.Id && oc.IsActive)
            .Select(oc => oc.CaseId)
            .ToListAsync();

        query = query.Where(c => !linkedCaseIds.Contains(c.Id));

        SearchResults = await query
            .OrderByDescending(c => c.DateOfOnset ?? c.DateOfNotification)
            .Take(100)
            .ToListAsync();
    }
}

public class SearchCriteria
{
    public CaseType? CaseType { get; set; }
    public Guid? DiseaseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? PatientName { get; set; }
    public string? CaseId { get; set; }
}
