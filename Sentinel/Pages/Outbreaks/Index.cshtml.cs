using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using System.Security.Claims;

namespace Sentinel.Pages.Outbreaks;

[Authorize(Policy = "Permission.Outbreak.View")]
public class IndexModel : PageModel
{
    private readonly IOutbreakService _outbreakService;
    private readonly ApplicationDbContext _context;

    public IndexModel(IOutbreakService outbreakService, ApplicationDbContext context)
    {
        _outbreakService = outbreakService;
        _context = context;
    }

    public List<Outbreak> Outbreaks { get; set; } = new();
    public Dictionary<int, int> CaseCounts { get; set; } = new();
    public Dictionary<int, int> ContactCounts { get; set; } = new();
    
    // Pagination properties
    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    
    // Sorting properties
    [BindProperty(SupportsGet = true)]
    public string SortBy { get; set; } = "StartDate";
    [BindProperty(SupportsGet = true)]
    public string SortOrder { get; set; } = "desc";
    
    // Search and filter properties
    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? FilterBy { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public bool ShowInactive { get; set; }

    public async Task OnGetAsync()
    {
        var query = _context.Outbreaks
            .Include(o => o.PrimaryDisease)
            .Include(o => o.PrimaryLocation)
            .AsQueryable();

        // Apply show inactive filter
        if (!ShowInactive)
        {
            query = query.Where(o => !o.IsDeleted);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower();
            query = query.Where(o => 
                o.Name.Contains(searchLower) ||
                (o.PrimaryDisease != null && o.PrimaryDisease.Name.Contains(searchLower)) ||
                (o.PrimaryLocation != null && o.PrimaryLocation.Name.Contains(searchLower))
            );
        }

        // Apply quick filters
        if (!string.IsNullOrWhiteSpace(FilterBy))
        {
            switch (FilterBy)
            {
                case "Active":
                    query = query.Where(o => o.Status == OutbreakStatus.Active);
                    break;
                case "Monitoring":
                    query = query.Where(o => o.Status == OutbreakStatus.Monitoring);
                    break;
                case "Resolved":
                    query = query.Where(o => o.Status == OutbreakStatus.Resolved);
                    break;
                case "Closed":
                    query = query.Where(o => o.Status == OutbreakStatus.Closed);
                    break;
                case "Recent":
                    query = query.Where(o => o.StartDate >= DateTime.UtcNow.AddDays(-30));
                    break;
                case "ThisYear":
                    var firstDayOfYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
                    query = query.Where(o => o.StartDate >= firstDayOfYear);
                    break;
            }
        }

        // Get total count for pagination
        TotalCount = await query.CountAsync();

        // Apply sorting
        query = SortBy switch
        {
            "Name" => SortOrder == "asc" ? query.OrderBy(o => o.Name) : query.OrderByDescending(o => o.Name),
            "Disease" => SortOrder == "asc" ? query.OrderBy(o => o.PrimaryDisease != null ? o.PrimaryDisease.Name : "") : query.OrderByDescending(o => o.PrimaryDisease != null ? o.PrimaryDisease.Name : ""),
            "Location" => SortOrder == "asc" ? query.OrderBy(o => o.PrimaryLocation != null ? o.PrimaryLocation.Name : "") : query.OrderByDescending(o => o.PrimaryLocation != null ? o.PrimaryLocation.Name : ""),
            "StartDate" => SortOrder == "asc" ? query.OrderBy(o => o.StartDate) : query.OrderByDescending(o => o.StartDate),
            "Status" => SortOrder == "asc" ? query.OrderBy(o => o.Status) : query.OrderByDescending(o => o.Status),
            _ => query.OrderByDescending(o => o.StartDate)
        };

        // Calculate pagination
        TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
        CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));

        // Apply pagination
        Outbreaks = await query
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
        
        // Get outbreak IDs
        var outbreakIds = Outbreaks.Select(o => o.Id).ToList();
        
        // Load case counts (where case type = Case)
        var caseCounts = await _context.OutbreakCases
            .Where(oc => outbreakIds.Contains(oc.OutbreakId) && oc.IsActive)
            .Include(oc => oc.Case)
            .Where(oc => oc.Case.Type == CaseType.Case)
            .GroupBy(oc => oc.OutbreakId)
            .Select(g => new { OutbreakId = g.Key, Count = g.Count() })
            .ToListAsync();
        
        CaseCounts = caseCounts.ToDictionary(x => x.OutbreakId, x => x.Count);
        
        // Load contact counts (where case type = Contact)
        var contactCounts = await _context.OutbreakCases
            .Where(oc => outbreakIds.Contains(oc.OutbreakId) && oc.IsActive)
            .Include(oc => oc.Case)
            .Where(oc => oc.Case.Type == CaseType.Contact)
            .GroupBy(oc => oc.OutbreakId)
            .Select(g => new { OutbreakId = g.Key, Count = g.Count() })
            .ToListAsync();
        
        ContactCounts = contactCounts.ToDictionary(x => x.OutbreakId, x => x.Count);
    }
    
    public string GetSortIcon(string columnName)
    {
        if (SortBy != columnName) return "bi-chevron-expand";
        return SortOrder == "asc" ? "bi-chevron-up" : "bi-chevron-down";
    }
    
    public string GetSortUrl(string columnName)
    {
        var newSortOrder = (SortBy == columnName && SortOrder == "asc") ? "desc" : "asc";
        return $"?SortBy={columnName}&SortOrder={newSortOrder}&CurrentPage=1&SearchTerm={SearchTerm}&FilterBy={FilterBy}&ShowInactive={ShowInactive}";
    }

    [Authorize(Policy = "Permission.Outbreak.Delete")]
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? string.Empty;

        var success = await _outbreakService.DeleteAsync(id, userId);

        TempData["SuccessMessage"] = success ? "Outbreak deleted successfully." : null;
        TempData["ErrorMessage"] = success ? null : "Failed to delete outbreak.";

        return RedirectToPage();
    }
}
