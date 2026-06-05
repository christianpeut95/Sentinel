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
using System.Text.Json;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize]
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

            // Load existing criteria
            Criteria = Definition.Criteria?.OrderBy(c => c.GroupNumber).ThenBy(c => c.DisplayOrder).ToList() ?? new();

            // Load lookup data for modals
            await LoadLookupDataAsync();

            return Page();
        }

        [IgnoreAntiforgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> OnPostAddLabCriterionAsync([FromBody] LabCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == DefinitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Build ValueJson for lab criterion including storage preferences
            var valueObj = new
            {
                specimenTypeIds = input.SpecimenTypeIds,
                pathogenNames = input.PathogenNames,
                testMethodIds = input.TestMethodIds,
                resultValues = input.ResultValues,
                timeConstraint = input.TimeConstraint,
                // Storage preferences
                specimenStoragePreference = input.SpecimenStoragePreference,
                canonicalSpecimenTypeId = input.CanonicalSpecimenTypeId,
                pathogenStoragePreference = input.PathogenStoragePreference,
                canonicalPathogenId = input.CanonicalPathogenId,
                testMethodStoragePreference = input.TestMethodStoragePreference,
                canonicalTestMethodId = input.CanonicalTestMethodId,
                resultStoragePreference = input.ResultStoragePreference,
                canonicalResultValue = input.CanonicalResultValue
            };

            var criterion = new CaseDefinitionCriteria
            {
                CaseDefinitionId = DefinitionId,
                CriterionType = CriterionType.Laboratory,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                FieldPath = "LabResults",
                Operator = ComparisonOperator.InList,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = definition.Criteria?.Count() ?? 0,
                // Populate lab-specific columns for HL7 matching
                AcceptableSpecimenTypesJson = JsonSerializer.Serialize(input.SpecimenTypeIds ?? new List<int>()),
                SpecimenStoragePreference = input.SpecimenStoragePreference,
                CanonicalSpecimenTypeId = input.CanonicalSpecimenTypeId,
                AcceptablePathogensJson = JsonSerializer.Serialize(input.PathogenNames ?? new List<string>()),
                BiomarkerStoragePreference = input.PathogenStoragePreference,
                CanonicalPathogenId = input.CanonicalPathogenId,
                AcceptableTestMethodsJson = JsonSerializer.Serialize(input.TestMethodIds ?? new List<int>()),
                TestMethodStoragePreference = input.TestMethodStoragePreference,
                CanonicalTestMethodId = input.CanonicalTestMethodId,
                AcceptableResultsJson = JsonSerializer.Serialize(input.ResultValues ?? new List<string>()),
                ResultStoragePreference = input.ResultStoragePreference,
                Description = input.CanonicalResultValue,
                IsRequired = true,
                RequireAllElementsMatch = false
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, criterionId = criterion.Id });
        }

        [IgnoreAntiforgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> OnPostAddClinicalCriterionAsync([FromBody] ClinicalCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == DefinitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Build ValueJson for clinical criterion
            var valueObj = new
            {
                symptomIds = input.SymptomIds,
                requireAll = input.RequireAll,
                minCount = input.MinCount,
                severityFilter = input.SeverityFilter
            };

            var criterion = new CaseDefinitionCriteria
            {
                CaseDefinitionId = DefinitionId,
                CriterionType = CriterionType.Clinical,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                FieldPath = "Symptoms",
                Operator = input.RequireAll ? ComparisonOperator.Equals : ComparisonOperator.InList,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = definition.Criteria?.Count() ?? 0
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, criterionId = criterion.Id });
        }

        [IgnoreAntiforgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> OnPostAddCustomFieldCriterionAsync([FromBody] CustomFieldCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == DefinitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Load custom field to get type
            var customField = await _context.CustomFieldDefinitions
                .FirstOrDefaultAsync(cf => cf.Id == input.CustomFieldId);

            if (customField == null)
            {
                return BadRequest("Custom field not found");
            }

            // Build ValueJson for custom field criterion
            var valueObj = new
            {
                customFieldId = input.CustomFieldId,
                customFieldName = customField.Name,
                customFieldLabel = customField.Label,
                fieldType = customField.FieldType.ToString(),
                value = input.Value,
                @operator = input.Operator.ToString()
            };

            var criterion = new CaseDefinitionCriteria
            {
                CaseDefinitionId = DefinitionId,
                CriterionType = CriterionType.CustomField,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                FieldPath = $"CustomFields.{customField.Name}",
                Operator = input.Operator,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = definition.Criteria?.Count() ?? 0
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, criterionId = criterion.Id });
        }

        [IgnoreAntiforgeryToken]
        [Consumes("application/json")]
        public async Task<IActionResult> OnPostAddCaseFieldCriterionAsync([FromBody] CaseFieldCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == DefinitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Build ValueJson for case field criterion
            var valueObj = new
            {
                fieldPath = input.FieldPath,
                value = input.Value,
                @operator = input.Operator.ToString()
            };

            var criterion = new CaseDefinitionCriteria
            {
                CaseDefinitionId = DefinitionId,
                CriterionType = CriterionType.Demographic, // Using Demographic for case fields
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                FieldPath = input.FieldPath,
                Operator = input.Operator,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = definition.Criteria?.Count() ?? 0
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, criterionId = criterion.Id });
        }

        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> OnPostDeleteCriterionAsync(int criterionId)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId);

            if (criterion == null)
            {
                return NotFound();
            }

            _context.CaseDefinitionCriteria.Remove(criterion);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
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

        // Input models for AJAX posts
        public class LabCriterionInput
        {
            public LogicalOperator LogicalOperator { get; set; }
            public int GroupNumber { get; set; }
            public List<int> SpecimenTypeIds { get; set; } = new();
            public List<string> PathogenNames { get; set; } = new();
            public List<int> TestMethodIds { get; set; } = new();
            public List<string> ResultValues { get; set; } = new();
            public TimeConstraintInput? TimeConstraint { get; set; }
            public string DisplayText { get; set; } = string.Empty;

            // Storage preferences
            public DataStoragePreference SpecimenStoragePreference { get; set; }
            public int? CanonicalSpecimenTypeId { get; set; }
            public DataStoragePreference PathogenStoragePreference { get; set; }
            public Guid? CanonicalPathogenId { get; set; }
            public DataStoragePreference TestMethodStoragePreference { get; set; }
            public int? CanonicalTestMethodId { get; set; }
            public DataStoragePreference ResultStoragePreference { get; set; }
            public string? CanonicalResultValue { get; set; }
        }

        public class TimeConstraintInput
        {
            public int Days { get; set; }
            public string RelativeTo { get; set; } = "OnsetDate"; // OnsetDate, ReportDate, DiagnosisDate
            public string Direction { get; set; } = "before"; // before, after
        }

        public class ClinicalCriterionInput
        {
            public LogicalOperator LogicalOperator { get; set; }
            public int GroupNumber { get; set; }
            public List<int> SymptomIds { get; set; } = new();
            public bool RequireAll { get; set; }
            public int? MinCount { get; set; }
            public string? SeverityFilter { get; set; }
            public string DisplayText { get; set; } = string.Empty;
        }

        public class CustomFieldCriterionInput
        {
            public LogicalOperator LogicalOperator { get; set; }
            public int GroupNumber { get; set; }
            public int CustomFieldId { get; set; }
            public ComparisonOperator Operator { get; set; }
            public string Value { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
        }

        public class CaseFieldCriterionInput
        {
            public LogicalOperator LogicalOperator { get; set; }
            public int GroupNumber { get; set; }
            public string FieldPath { get; set; } = string.Empty;
            public ComparisonOperator Operator { get; set; }
            public string Value { get; set; } = string.Empty;
            public string DisplayText { get; set; } = string.Empty;
        }
    }
}
