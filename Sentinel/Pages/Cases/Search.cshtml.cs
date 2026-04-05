using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.View")]
    public class SearchModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SearchModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Search Parameters
        [BindProperty(SupportsGet = true)]
        public string? CaseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PatientName { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? DiseaseId { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? DiseaseCategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? ConfirmationStatusId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? OnsetDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? OnsetDateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? NotificationDateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? NotificationDateTo { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? HasSymptoms { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool? HasLabResults { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? City { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? PostalCode { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? State { get; set; }

        // Dropdowns
        public SelectList Diseases { get; set; } = default!;
        public SelectList DiseaseCategories { get; set; } = default!;
        public SelectList ConfirmationStatuses { get; set; } = default!;

        // Custom Field Search
        public List<CustomFieldDefinition> SearchableCustomFields { get; set; } = new();
        public Dictionary<int, string> CustomFieldSearchValues { get; set; } = new();

        // Results
        public List<Case> SearchResults { get; set; } = new();
        public int TotalResults { get; set; }
        public bool HasSearched { get; set; }

        public async Task OnGetAsync()
        {
            await LoadDropdowns();
            await LoadCustomFields();

            // Check if any search criteria provided
            HasSearched = !string.IsNullOrEmpty(CaseId) ||
                         !string.IsNullOrEmpty(PatientName) ||
                         DiseaseId.HasValue ||
                         DiseaseCategoryId.HasValue ||
                         ConfirmationStatusId.HasValue ||
                         OnsetDateFrom.HasValue ||
                         OnsetDateTo.HasValue ||
                         NotificationDateFrom.HasValue ||
                         NotificationDateTo.HasValue ||
                         HasSymptoms.HasValue ||
                         HasLabResults.HasValue ||
                         !string.IsNullOrEmpty(City) ||
                         !string.IsNullOrEmpty(PostalCode) ||
                         !string.IsNullOrEmpty(State) ||
                         Request.Query.Keys.Any(k => k.StartsWith("customfield_"));

            if (HasSearched)
            {
                await PerformSearch();
            }
        }

        public async Task<IActionResult> OnGetClearAsync()
        {
            return RedirectToPage();
        }

        private async Task PerformSearch()
        {
            var query = _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                    .ThenInclude(d => d!.DiseaseCategory)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.CaseSymptoms)
                .Include(c => c.LabResults)
                .AsQueryable();

            // Case ID filter
            if (!string.IsNullOrEmpty(CaseId))
            {
                query = query.Where(c => c.FriendlyId.Contains(CaseId));
            }

            // Patient name filter
            if (!string.IsNullOrEmpty(PatientName))
            {
                var nameParts = PatientName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length == 1)
                {
                    var name = nameParts[0];
                    query = query.Where(c => c.Patient != null && 
                        (c.Patient.GivenName.Contains(name) || 
                         c.Patient.FamilyName.Contains(name)));
                }
                else if (nameParts.Length >= 2)
                {
                    var firstName = nameParts[0];
                    var lastName = nameParts[1];
                    query = query.Where(c => c.Patient != null &&
                        c.Patient.GivenName.Contains(firstName) &&
                        c.Patient.FamilyName.Contains(lastName));
                }
            }

            // Disease filter
            if (DiseaseId.HasValue)
            {
                query = query.Where(c => c.DiseaseId == DiseaseId.Value);
            }

            // Disease Category filter
            if (DiseaseCategoryId.HasValue)
            {
                query = query.Where(c => c.Disease != null && c.Disease.DiseaseCategoryId == DiseaseCategoryId.Value);
            }

            // Confirmation Status filter
            if (ConfirmationStatusId.HasValue)
            {
                query = query.Where(c => c.ConfirmationStatusId == ConfirmationStatusId);
            }

            // Date filters
            if (OnsetDateFrom.HasValue)
            {
                query = query.Where(c => c.DateOfOnset >= OnsetDateFrom.Value);
            }

            if (OnsetDateTo.HasValue)
            {
                query = query.Where(c => c.DateOfOnset <= OnsetDateTo.Value);
            }

            if (NotificationDateFrom.HasValue)
            {
                query = query.Where(c => c.DateOfNotification >= NotificationDateFrom.Value);
            }

            if (NotificationDateTo.HasValue)
            {
                query = query.Where(c => c.DateOfNotification <= NotificationDateTo.Value);
            }

            // Note: Case model doesn't have CreatedAt property, so these filters are removed
            // If you need to add this, please add a CreatedAt property to the Case model first

            // Symptoms filter
            if (HasSymptoms.HasValue)
            {
                if (HasSymptoms.Value)
                {
                    query = query.Where(c => c.CaseSymptoms.Any());
                }
                else
                {
                    query = query.Where(c => !c.CaseSymptoms.Any());
                }
            }

            // Lab Results filter
            if (HasLabResults.HasValue)
            {
                if (HasLabResults.Value)
                {
                    query = query.Where(c => c.LabResults.Any());
                }
                else
                {
                    query = query.Where(c => !c.LabResults.Any());
                }
            }

            // Location filters (from patient)
            if (!string.IsNullOrEmpty(City))
            {
                query = query.Where(c => c.Patient != null && c.Patient.City != null && c.Patient.City.Contains(City));
            }

            if (!string.IsNullOrEmpty(PostalCode))
            {
                query = query.Where(c => c.Patient != null && c.Patient.PostalCode != null && c.Patient.PostalCode.Contains(PostalCode));
            }

            if (!string.IsNullOrEmpty(State))
            {
                query = query.Where(c => c.Patient != null && c.Patient.State != null && c.Patient.State.Code.Contains(State));
            }

            // Custom Field Filters
            var customFieldFilters = Request.Query.Keys.Where(k => k.StartsWith("customfield_")).ToList();
            
            foreach (var key in customFieldFilters)
            {
                var fieldIdStr = key.Replace("customfield_", "");
                if (int.TryParse(fieldIdStr, out int fieldId))
                {
                    var searchValue = Request.Query[key].ToString();
                    if (!string.IsNullOrWhiteSpace(searchValue))
                    {
                        CustomFieldSearchValues[fieldId] = searchValue;

                        // Get field definition to determine type
                        var fieldDef = await _context.CustomFieldDefinitions.FindAsync(fieldId);
                        if (fieldDef != null)
                        {
                            switch (fieldDef.FieldType)
                            {
                                case CustomFieldType.Text:
                                case CustomFieldType.Email:
                                case CustomFieldType.Phone:
                                case CustomFieldType.TextArea:
                                    query = query.Where(c => c.CustomFieldStrings
                                        .Any(cf => cf.FieldDefinitionId == fieldId && cf.Value.Contains(searchValue)));
                                    break;

                                case CustomFieldType.Number:
                                    if (decimal.TryParse(searchValue, out var numValue))
                                    {
                                        query = query.Where(c => c.CustomFieldNumbers
                                            .Any(cf => cf.FieldDefinitionId == fieldId && cf.Value == numValue));
                                    }
                                    break;

                                case CustomFieldType.Date:
                                    if (DateTime.TryParse(searchValue, out var dateValue))
                                    {
                                        query = query.Where(c => c.CustomFieldDates
                                            .Any(cf => cf.FieldDefinitionId == fieldId && cf.Value == dateValue));
                                    }
                                    break;

                                case CustomFieldType.Checkbox:
                                    if (bool.TryParse(searchValue, out var boolValue))
                                    {
                                        query = query.Where(c => c.CustomFieldBooleans
                                            .Any(cf => cf.FieldDefinitionId == fieldId && cf.Value == boolValue));
                                    }
                                    break;

                                case CustomFieldType.Dropdown:
                                    if (int.TryParse(searchValue, out var lookupId))
                                    {
                                        query = query.Where(c => c.CustomFieldLookups
                                            .Any(cf => cf.FieldDefinitionId == fieldId && cf.LookupValueId == lookupId));
                                    }
                                    break;
                            }
                        }
                    }
                }
            }

            // Execute query
            SearchResults = await query
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset ?? DateTime.MaxValue)
                .Take(500) // Limit results
                .ToListAsync();

            TotalResults = SearchResults.Count;
        }

        private async Task LoadDropdowns()
        {
            Diseases = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync(),
                "Id", "Name");

            DiseaseCategories = new SelectList(
                await _context.DiseaseCategories
                    .Where(dc => dc.IsActive)
                    .OrderBy(dc => dc.DisplayOrder)
                    .ThenBy(dc => dc.Name)
                    .ToListAsync(),
                "Id", "Name");

            ConfirmationStatuses = new SelectList(
                await _context.CaseStatuses
                    .Where(cs => cs.IsActive)
                    .OrderBy(cs => cs.DisplayOrder)
                    .ToListAsync(),
                "Id", "Name");
        }

        private async Task LoadCustomFields()
        {
            SearchableCustomFields = await _context.CustomFieldDefinitions
                .Where(f => f.ShowOnCaseForm && f.IsActive)
                .Include(f => f.LookupTable)
                    .ThenInclude(lt => lt!.Values)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
        }
    }
}
