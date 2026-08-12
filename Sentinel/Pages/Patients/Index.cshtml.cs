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
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class IndexModel : PageModel
    {
        private readonly Sentinel.Data.ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public IndexModel(Sentinel.Data.ApplicationDbContext context, IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        public IList<Patient> Patients { get;set; } = default!;
        
        // Pagination properties
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public bool CanCreatePatient { get; private set; }
        public bool CanEditPatient { get; private set; }
        public bool CanSearchPatients { get; private set; }
        
        // Sorting properties
        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "Name";
        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "asc";
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string? FilterBy { get; set; }

        public async Task OnGetAsync()
        {
            await LoadPermissionsAsync();

            var query = _context.Patients
                .Include(p => p.CountryOfBirth)
                .Include(p => p.Ancestry)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.SexAtBirth)
                .Include(p => p.Gender)
                .Include(p => p.Occupation)
                .AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(p => 
                    (p.GivenName != null && p.GivenName.ToLower().Contains(searchLower)) ||
                    (p.FamilyName != null && p.FamilyName.ToLower().Contains(searchLower)) ||
                    (p.FriendlyId != null && p.FriendlyId.ToLower().Contains(searchLower)) ||
                    (p.City != null && p.City.ToLower().Contains(searchLower))
                );
            }

            // Apply quick filters
            if (!string.IsNullOrWhiteSpace(FilterBy))
            {
                switch (FilterBy)
                {
                    case "Recent":
                        query = query.Where(p => p.CreatedAt >= DateTime.UtcNow.AddDays(-7));
                        break;
                    case "Today":
                        var today = DateTime.UtcNow.Date;
                        query = query.Where(p => p.CreatedAt >= today);
                        break;
                    case "ThisMonth":
                        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                        query = query.Where(p => p.CreatedAt >= firstDayOfMonth);
                        break;
                    case "Geocoded":
                        query = query.Where(p => p.Latitude.HasValue && p.Longitude.HasValue);
                        break;
                    case "NotGeocoded":
                        query = query.Where(p => !p.Latitude.HasValue || !p.Longitude.HasValue);
                        break;
                    case "Deceased":
                        query = query.Where(p => p.IsDeceased);
                        break;
                }
            }

            // Get total count for pagination
            TotalCount = await query.CountAsync();

            // Apply sorting
            query = SortBy switch
            {
                "FriendlyId" => SortOrder == "asc" ? query.OrderBy(p => p.FriendlyId) : query.OrderByDescending(p => p.FriendlyId),
                "Name" => SortOrder == "asc" ? query.OrderBy(p => p.FamilyName).ThenBy(p => p.GivenName) : query.OrderByDescending(p => p.FamilyName).ThenByDescending(p => p.GivenName),
                "DateOfBirth" => SortOrder == "asc" ? query.OrderBy(p => p.DateOfBirth ?? DateTime.MaxValue) : query.OrderByDescending(p => p.DateOfBirth ?? DateTime.MinValue),
                "Gender" => SortOrder == "asc" ? query.OrderBy(p => p.Gender != null ? p.Gender.Name : "") : query.OrderByDescending(p => p.Gender != null ? p.Gender.Name : ""),
                _ => query.OrderBy(p => p.FamilyName).ThenBy(p => p.GivenName)
            };

            // Calculate pagination
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            CurrentPage = Math.Max(1, Math.Min(CurrentPage, Math.Max(1, TotalPages)));

            // Apply pagination
            Patients = await query
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

        private async Task LoadPermissionsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            CanCreatePatient = await _permissionService.HasPermissionAsync(userId, PermissionModule.Patient, PermissionAction.Create);
            CanEditPatient = await _permissionService.HasPermissionAsync(userId, PermissionModule.Patient, PermissionAction.Edit);
            CanSearchPatients = await _permissionService.HasPermissionAsync(userId, PermissionModule.Patient, PermissionAction.Search);
        }
    }
}
