using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    public class BuildCriteriaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BuildCriteriaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public int DefinitionId { get; set; }

        public CaseDefinition Definition { get; set; } = null!;
        public List<CaseDefinitionCriteria> Criteria { get; set; } = new();

        // For modals/dropdowns
        public List<SpecimenType> SpecimenTypes { get; set; } = new();
        public List<Pathogen> Pathogens { get; set; } = new();
        public List<TestMethod> TestMethods { get; set; } = new();
        public List<Symptom> Symptoms { get; set; } = new();
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();

        public SelectList FieldPathsForCaseData { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            DefinitionId = id;

            // Load definition
            Definition = await _context.CaseDefinitions
                .Include(cd => cd.Disease)
                .Include(cd => cd.ConfirmationStatus)
                .Include(cd => cd.Criteria)
                    .ThenInclude(c => c.ChildCriteria)
                .FirstOrDefaultAsync(cd => cd.Id == id);

            if (Definition == null)
            {
                return NotFound();
            }

            if (Definition.Status != CaseDefinitionStatus.Draft)
            {
                TempData["ErrorMessage"] = "Criteria can be changed only while a case definition is a draft. Create a new draft version to revise an active or archived definition.";
                return RedirectToPage("./Review", new { id });
            }

            // Load existing criteria
            Criteria = Definition.Criteria?.OrderBy(c => c.GroupNumber).ThenBy(c => c.DisplayOrder).ToList() ?? new();

            // Load lookup data for modals
            await LoadLookupDataAsync();

            return Page();
        }

        private async Task LoadLookupDataAsync()
        {
            SpecimenTypes = await _context.SpecimenTypes
                .Where(st => st.IsActive)
                .OrderBy(st => st.Name)
                .ToListAsync();

            Pathogens = await _context.Pathogens
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();

            TestMethods = await _context.TestMethods
                .Where(tm => tm.IsActive)
                .OrderBy(tm => tm.Name)
                .ToListAsync();

            Symptoms = await _context.Symptoms
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();

            // Load custom fields for the current disease
            if (Definition?.DiseaseId != null)
            {
                CustomFields = await _context.CustomFieldDefinitions
                    .Where(cf => cf.IsActive && 
                                 cf.ShowOnCaseForm &&
                                 cf.DiseaseCustomFields.Any(dcf => dcf.DiseaseId == Definition.DiseaseId))
                    .OrderBy(cf => cf.Category)
                    .ThenBy(cf => cf.DisplayOrder)
                    .ToListAsync();
            }

            // Build case field paths
            var caseFields = new[]
            {
                new { Value = "ReportDate", Text = "Report Date" },
                new { Value = "OnsetDate", Text = "Onset Date" },
                new { Value = "DiagnosisDate", Text = "Diagnosis Date" },
                new { Value = "HospitalizedDate", Text = "Hospitalized Date" },
                new { Value = "IsHospitalized", Text = "Is Hospitalized" },
                new { Value = "IsFatal", Text = "Is Fatal" },
                new { Value = "DateOfDeath", Text = "Date of Death" },
                new { Value = "Patient.DateOfBirth", Text = "Patient Age" },
                new { Value = "Patient.Gender", Text = "Patient Gender" },
                new { Value = "Patient.CountryOfBirth", Text = "Patient Country of Birth" }
            };

            FieldPathsForCaseData = new SelectList(caseFields, "Value", "Text");
        }

        
    }
}
