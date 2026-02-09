using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Pages.Patients
{
    public class SearchModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SearchModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? GivenName { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? FamilyName { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateOfBirthFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? DateOfBirthTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SexAtBirthId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GenderId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Phone { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? State { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PostalCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CountryOfBirthId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? LanguageSpokenAtHomeId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? EthnicityId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? AtsiStatusId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? OccupationId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CreatedFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? CreatedTo { get; set; }

        public IList<Patient> SearchResults { get; set; } = new List<Patient>();
        public bool HasSearched { get; set; }
        public int TotalResults { get; set; }

        public SelectList SexAtBirths { get; set; } = default!;
        public SelectList Genders { get; set; } = default!;
        public SelectList Countries { get; set; } = default!;
        public SelectList Languages { get; set; } = default!;
        public SelectList Ethnicities { get; set; } = default!;
        public SelectList AtsiStatuses { get; set; } = default!;
        public SelectList Occupations { get; set; } = default!;

        public async Task OnGetAsync()
        {
            await LoadDropdownsAsync();

            if (HasAnySearchCriteria())
            {
                HasSearched = true;
                await PerformSearchAsync();
            }
        }

        private bool HasAnySearchCriteria()
        {
            return !string.IsNullOrWhiteSpace(GivenName)
                || !string.IsNullOrWhiteSpace(FamilyName)
                || DateOfBirthFrom.HasValue
                || DateOfBirthTo.HasValue
                || SexAtBirthId.HasValue
                || GenderId.HasValue
                || !string.IsNullOrWhiteSpace(Phone)
                || !string.IsNullOrWhiteSpace(Email)
                || !string.IsNullOrWhiteSpace(City)
                || !string.IsNullOrWhiteSpace(State)
                || !string.IsNullOrWhiteSpace(PostalCode)
                || CountryOfBirthId.HasValue
                || LanguageSpokenAtHomeId.HasValue
                || EthnicityId.HasValue
                || AtsiStatusId.HasValue
                || OccupationId.HasValue
                || CreatedFrom.HasValue
                || CreatedTo.HasValue;
        }

        private async Task PerformSearchAsync()
        {
            var query = _context.Patients
                .Include(p => p.SexAtBirth)
                .Include(p => p.Gender)
                .Include(p => p.CountryOfBirth)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Ethnicity)
                .Include(p => p.AtsiStatus)
                .Include(p => p.Occupation)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(GivenName))
            {
                query = query.Where(p => p.GivenName.Contains(GivenName));
            }

            if (!string.IsNullOrWhiteSpace(FamilyName))
            {
                query = query.Where(p => p.FamilyName.Contains(FamilyName));
            }

            if (DateOfBirthFrom.HasValue)
            {
                query = query.Where(p => p.DateOfBirth >= DateOfBirthFrom.Value);
            }

            if (DateOfBirthTo.HasValue)
            {
                query = query.Where(p => p.DateOfBirth <= DateOfBirthTo.Value);
            }

            if (SexAtBirthId.HasValue)
            {
                query = query.Where(p => p.SexAtBirthId == SexAtBirthId.Value);
            }

            if (GenderId.HasValue)
            {
                query = query.Where(p => p.GenderId == GenderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(Phone))
            {
                query = query.Where(p => 
                    (p.HomePhone != null && p.HomePhone.Contains(Phone)) ||
                    (p.MobilePhone != null && p.MobilePhone.Contains(Phone)));
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                query = query.Where(p => p.EmailAddress != null && p.EmailAddress.Contains(Email));
            }

            if (!string.IsNullOrWhiteSpace(City))
            {
                query = query.Where(p => p.City != null && p.City.Contains(City));
            }

            if (!string.IsNullOrWhiteSpace(State))
            {
                query = query.Where(p => p.State != null && p.State.Contains(State));
            }

            if (!string.IsNullOrWhiteSpace(PostalCode))
            {
                query = query.Where(p => p.PostalCode != null && p.PostalCode.Contains(PostalCode));
            }

            if (CountryOfBirthId.HasValue)
            {
                query = query.Where(p => p.CountryOfBirthId == CountryOfBirthId.Value);
            }

            if (LanguageSpokenAtHomeId.HasValue)
            {
                query = query.Where(p => p.LanguageSpokenAtHomeId == LanguageSpokenAtHomeId.Value);
            }

            if (EthnicityId.HasValue)
            {
                query = query.Where(p => p.EthnicityId == EthnicityId.Value);
            }

            if (AtsiStatusId.HasValue)
            {
                query = query.Where(p => p.AtsiStatusId == AtsiStatusId.Value);
            }

            if (OccupationId.HasValue)
            {
                query = query.Where(p => p.OccupationId == OccupationId.Value);
            }

            if (CreatedFrom.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= CreatedFrom.Value);
            }

            if (CreatedTo.HasValue)
            {
                var createdToEndOfDay = CreatedTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedAt <= createdToEndOfDay);
            }

            SearchResults = await query
                .OrderBy(p => p.FamilyName)
                .ThenBy(p => p.GivenName)
                .ToListAsync();

            TotalResults = SearchResults.Count;
        }

        private async Task LoadDropdownsAsync()
        {
            SexAtBirths = new SelectList(await _context.SexAtBirths.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
            Genders = new SelectList(await _context.Genders.OrderBy(g => g.Name).ToListAsync(), "Id", "Name");
            Countries = new SelectList(await _context.Countries.OrderBy(c => c.Name).ToListAsync(), "Id", "Name");
            Languages = new SelectList(await _context.Languages.OrderBy(l => l.Name).ToListAsync(), "Id", "Name");
            Ethnicities = new SelectList(await _context.Ethnicities.OrderBy(e => e.Name).ToListAsync(), "Id", "Name");
            AtsiStatuses = new SelectList(await _context.AtsiStatuses.OrderBy(a => a.Name).ToListAsync(), "Id", "Name");
            Occupations = new SelectList(await _context.Occupations
                .Where(o => o.Code.Length == 6 && o.IsActive)
                .OrderBy(o => o.Name)
                .ToListAsync(), "Id", "Name");
        }

        public IActionResult OnPostClear()
        {
            return RedirectToPage();
        }
    }
}
