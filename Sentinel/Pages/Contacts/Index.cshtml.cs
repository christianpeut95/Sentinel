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

namespace Sentinel.Pages.Contacts
{
    [Authorize(Policy = "Permission.Case.View")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IDiseaseAccessService _diseaseAccessService;

        public IndexModel(ApplicationDbContext context, IDiseaseAccessService diseaseAccessService)
        {
            _context = context;
            _diseaseAccessService = diseaseAccessService;
        }

        public IList<Case> Contacts { get; set; } = default!;
        public Dictionary<Guid, List<ExposureEvent>> ContactExposures { get; set; } = new();
        
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

            // Load contacts and their exposure events where THEY are the RelatedCaseId
            // This shows which cases they are contacts OF
            var contactIds = await _context.Cases
                .Where(c => c.Type == CaseType.Contact)
                .Where(c => c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value))
                .Select(c => c.Id)
                .ToListAsync();

            var query = _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .AsSplitQuery()
                .Where(c => c.Type == CaseType.Contact)
                .Where(c => c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value))
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
                        query = query.Where(c => c.DateOfNotification >= DateTime.UtcNow.AddDays(-14) || c.DateOfOnset >= DateTime.UtcNow.AddDays(-14));
                        break;
                    case "Today":
                        var today = DateTime.UtcNow.Date;
                        query = query.Where(c => c.DateOfNotification >= today || c.DateOfOnset >= today);
                        break;
                    case "ThisMonth":
                        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        query = query.Where(c => c.DateOfNotification >= firstDayOfMonth || c.DateOfOnset >= firstDayOfMonth);
                        break;
                    case "Symptomatic":
                        query = query.Where(c => c.DateOfOnset.HasValue);
                        break;
                    case "Asymptomatic":
                        query = query.Where(c => !c.DateOfOnset.HasValue);
                        break;
                    case "WithStatus":
                        query = query.Where(c => c.ConfirmationStatusId.HasValue);
                        break;
                    case "WithExposures":
                        var contactsWithTransmissions = _context.ExposureEvents
                            .Where(e => e.SourceCaseId.HasValue && e.ExposureType == ExposureType.Contact)
                            .Select(e => e.SourceCaseId!.Value)
                            .Distinct();
                        query = query.Where(c => contactsWithTransmissions.Contains(c.Id));
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
            Contacts = await query
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();
            
            // Load exposure events where these contacts are the SOURCE (transmissions)
            // This shows which cases these contacts exposed (downstream)
            var contactIdsOnPage = Contacts.Select(c => c.Id).ToList();
            var exposures = await _context.ExposureEvents
                .Include(e => e.ExposedCase)
                    .ThenInclude(c => c!.Patient)
                .Include(e => e.ContactClassification)
                .Where(e => e.SourceCaseId.HasValue && contactIdsOnPage.Contains(e.SourceCaseId.Value))
                .Where(e => e.ExposureType == ExposureType.Contact)
                .OrderByDescending(e => e.ExposureStartDate)
                .ToListAsync();
            
            // Group exposures by the contact (SourceCaseId)
            ContactExposures = exposures
                .GroupBy(e => e.SourceCaseId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());
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
