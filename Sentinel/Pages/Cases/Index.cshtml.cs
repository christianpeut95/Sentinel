using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Case> Cases { get; set; } = default!;
        
        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        
        // Sorting properties
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "DateOfNotification";
        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "desc";
        
        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? FilterBy { get; set; }

        public async Task OnGetAsync()
        {
            // Note: Disease access filtering now handled by global query filter in DbContext
            // No need to manually filter by accessibleDiseaseIds

            var query = _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .AsSplitQuery()
                .Where(c => c.Type == CaseType.Case)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(c => 
                    c.FriendlyId.Contains(searchLower) ||
                    (c.Patient != null && (c.Patient.GivenName.Contains(searchLower) || c.Patient.FamilyName.Contains(searchLower))) ||
                    (c.Disease != null && c.Disease.Name.Contains(searchLower))
                );
            }

            // Apply quick filters
            if (!string.IsNullOrWhiteSpace(FilterBy))
            {
                switch (FilterBy)
                {
                    case "Recent":
                        query = query.Where(c => c.DateOfNotification >= DateTime.UtcNow.AddDays(-7) || c.DateOfOnset >= DateTime.UtcNow.AddDays(-7));
                        break;
                    case "Today":
                        var today = DateTime.UtcNow.Date;
                        query = query.Where(c => c.DateOfNotification >= today || c.DateOfOnset >= today);
                        break;
                    case "ThisMonth":
                        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        query = query.Where(c => c.DateOfNotification >= firstDayOfMonth || c.DateOfOnset >= firstDayOfMonth);
                        break;
                    case "WithStatus":
                        query = query.Where(c => c.ConfirmationStatusId.HasValue);
                        break;
                    case "NoStatus":
                        query = query.Where(c => !c.ConfirmationStatusId.HasValue);
                        break;
                    case "WithDisease":
                        query = query.Where(c => c.DiseaseId.HasValue);
                        break;
                }
            }

            // Get total count for pagination
            TotalCount = await query.CountAsync();
            
            // Apply sorting
            query = SortBy switch
            {
                "CaseId" => SortOrder == "asc" ? query.OrderBy(c => c.FriendlyId) : query.OrderByDescending(c => c.FriendlyId),
                "Patient" => SortOrder == "asc" ? 
                    query.OrderBy(c => c.Patient != null ? c.Patient.FamilyName ?? "" : "").ThenBy(c => c.Patient != null ? c.Patient.GivenName ?? "" : "") : 
                    query.OrderByDescending(c => c.Patient != null ? c.Patient.FamilyName ?? "" : "").ThenByDescending(c => c.Patient != null ? c.Patient.GivenName ?? "" : ""),
                "Disease" => SortOrder == "asc" ? 
                    query.OrderBy(c => c.Disease != null ? c.Disease.Name ?? "" : "") : 
                    query.OrderByDescending(c => c.Disease != null ? c.Disease.Name ?? "" : ""),
                "DateOfOnset" => SortOrder == "asc" ? query.OrderBy(c => c.DateOfOnset ?? DateTime.MinValue) : query.OrderByDescending(c => c.DateOfOnset ?? DateTime.MinValue),
                "DateOfNotification" => SortOrder == "asc" ? query.OrderBy(c => c.DateOfNotification ?? DateTime.MinValue) : query.OrderByDescending(c => c.DateOfNotification ?? DateTime.MinValue),
                "Status" => SortOrder == "asc" ? 
                    query.OrderBy(c => c.ConfirmationStatus != null ? c.ConfirmationStatus.Name ?? "" : "") : 
                    query.OrderByDescending(c => c.ConfirmationStatus != null ? c.ConfirmationStatus.Name ?? "" : ""),
                _ => query.OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MinValue)
            };
            
            // Calculate pagination
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));
            
            // Apply pagination
            Cases = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
        }
        
        public string GetSortIcon(string columnName)
        {
            if (SortBy != columnName) return "bi-chevron-expand";
            return SortOrder == "asc" ? "bi-chevron-up" : "bi-chevron-down";
        }
        
        public string GetSortUrl(string columnName)
        {
            var newSortOrder = (SortBy == columnName && SortOrder == "asc") ? "desc" : "asc";
            return $"?SortBy={columnName}&SortOrder={newSortOrder}&CurrentPage=1";
        }
    }
}

