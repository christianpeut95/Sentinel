using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using System.Text.Json;

namespace Sentinel.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/case-definitions/{definitionId}/criteria")]
    public class CaseDefinitionCriteriaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CaseDefinitionCriteriaController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("laboratory")]
        public async Task<IActionResult> AddLabCriterion(int definitionId, [FromBody] LabCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == definitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Calculate display order based on siblings (same parent context)
            var maxDisplayOrder = await _context.CaseDefinitionCriteria
                .Where(c => c.CaseDefinitionId == definitionId && c.ParentCriteriaId == input.ParentCriteriaId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? -1;

            var newDisplayOrder = maxDisplayOrder + 1;
            Console.WriteLine($"Creating lab criterion with DisplayOrder: {newDisplayOrder}");

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
                CaseDefinitionId = definitionId,
                CriterionType = CriterionType.Laboratory,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                ParentCriteriaId = input.ParentCriteriaId,
                FieldPath = "LabResults",
                Operator = ComparisonOperator.InList,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = newDisplayOrder,
                // Lab-specific fields
                AcceptableSpecimenTypesJson = JsonSerializer.Serialize(input.SpecimenTypeIds),
                SpecimenStoragePreference = input.SpecimenStoragePreference,
                CanonicalSpecimenTypeId = input.CanonicalSpecimenTypeId,
                AcceptablePathogensJson = JsonSerializer.Serialize(input.PathogenNames),
                BiomarkerStoragePreference = input.PathogenStoragePreference,
                CanonicalPathogenId = input.CanonicalPathogenId,
                AcceptableTestMethodsJson = JsonSerializer.Serialize(input.TestMethodIds),
                TestMethodStoragePreference = input.TestMethodStoragePreference,
                CanonicalTestMethodId = input.CanonicalTestMethodId,
                AcceptableResultsJson = JsonSerializer.Serialize(input.ResultValues),
                ResultStoragePreference = input.ResultStoragePreference,
                CanonicalTestResultId = null, // Result values are strings, not IDs
                Description = input.CanonicalResultValue, // Store canonical result value
                IsRequired = true,
                RequireAllElementsMatch = false
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            Console.WriteLine($"Created unified CaseDefinitionCriteria with ID: {criterion.Id}, DisplayOrder: {criterion.DisplayOrder}");
            Console.WriteLine($"Storage prefs: Specimen={criterion.SpecimenStoragePreference}, Pathogen={criterion.BiomarkerStoragePreference}, TestMethod={criterion.TestMethodStoragePreference}, Result={criterion.ResultStoragePreference}");

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPut("{criterionId}/laboratory")]
        public async Task<IActionResult> UpdateLabCriterion(int definitionId, int criterionId, [FromBody] LabCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {

                var criterion = await _context.CaseDefinitionCriteria
                    .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

                if (criterion == null)
                {
                    return NotFound();
                }

                System.Diagnostics.Debug.WriteLine($"===== UpdateLabCriterion START: CriterionId={criterionId} =====");
                System.Diagnostics.Debug.WriteLine($"Input storage prefs: Specimen={input.SpecimenStoragePreference}, Pathogen={input.PathogenStoragePreference}");

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

                var valueJson = JsonSerializer.Serialize(valueObj);
                System.Diagnostics.Debug.WriteLine($"ValueJson length: {valueJson.Length}");

                // Update the criterion
                criterion.LogicalOperator = input.LogicalOperator;
                criterion.GroupNumber = input.GroupNumber;
                criterion.ValueJson = valueJson;
                criterion.DisplayText = input.DisplayText;

                System.Diagnostics.Debug.WriteLine($"Updating lab-specific fields on unified criterion...");

                // Update lab-specific fields directly on the criterion
                criterion.AcceptableSpecimenTypesJson = JsonSerializer.Serialize(input.SpecimenTypeIds);
                criterion.SpecimenStoragePreference = input.SpecimenStoragePreference;
                criterion.CanonicalSpecimenTypeId = input.CanonicalSpecimenTypeId;
                criterion.AcceptablePathogensJson = JsonSerializer.Serialize(input.PathogenNames);
                criterion.BiomarkerStoragePreference = input.PathogenStoragePreference;
                criterion.CanonicalPathogenId = input.CanonicalPathogenId;
                criterion.AcceptableTestMethodsJson = JsonSerializer.Serialize(input.TestMethodIds);
                criterion.TestMethodStoragePreference = input.TestMethodStoragePreference;
                criterion.CanonicalTestMethodId = input.CanonicalTestMethodId;
                criterion.AcceptableResultsJson = JsonSerializer.Serialize(input.ResultValues);
                criterion.ResultStoragePreference = input.ResultStoragePreference;
                criterion.Description = input.CanonicalResultValue;

                System.Diagnostics.Debug.WriteLine($"About to save changes...");

                var changes = await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"SaveChanges returned: {changes} rows affected");
                System.Diagnostics.Debug.WriteLine($"===== UpdateLabCriterion END =====");

                return Ok(new { 
                    success = true, 
                    criterionId = criterion.Id,
                    rowsAffected = changes
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in UpdateLabCriterion: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpPost("clinical")]
        public async Task<IActionResult> AddClinicalCriterion(int definitionId, [FromBody] ClinicalCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == definitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Calculate display order based on siblings (same parent context)
            var maxDisplayOrder = await _context.CaseDefinitionCriteria
                .Where(c => c.CaseDefinitionId == definitionId && c.ParentCriteriaId == input.ParentCriteriaId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? -1;

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
                CaseDefinitionId = definitionId,
                CriterionType = CriterionType.Clinical,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                ParentCriteriaId = input.ParentCriteriaId,
                FieldPath = "Symptoms",
                Operator = input.RequireAll ? ComparisonOperator.Equals : ComparisonOperator.InList,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = maxDisplayOrder + 1
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPost("custom-field")]
        public async Task<IActionResult> AddCustomFieldCriterion(int definitionId, [FromBody] CustomFieldCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == definitionId);

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

            // Calculate display order based on siblings (same parent context)
            var maxDisplayOrder = await _context.CaseDefinitionCriteria
                .Where(c => c.CaseDefinitionId == definitionId && c.ParentCriteriaId == input.ParentCriteriaId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? -1;

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
                CaseDefinitionId = definitionId,
                CriterionType = CriterionType.CustomField,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                ParentCriteriaId = input.ParentCriteriaId,
                FieldPath = $"CustomFields.{customField.Name}",
                Operator = input.Operator,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = maxDisplayOrder + 1
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPost("case-field")]
        public async Task<IActionResult> AddCaseFieldCriterion(int definitionId, [FromBody] CaseFieldCriterionInput input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var definition = await _context.CaseDefinitions
                .Include(cd => cd.Criteria)
                .FirstOrDefaultAsync(cd => cd.Id == definitionId);

            if (definition == null)
            {
                return NotFound();
            }

            // Calculate display order based on siblings (same parent context)
            var maxDisplayOrder = await _context.CaseDefinitionCriteria
                .Where(c => c.CaseDefinitionId == definitionId && c.ParentCriteriaId == input.ParentCriteriaId)
                .MaxAsync(c => (int?)c.DisplayOrder) ?? -1;

            // Build ValueJson for case field criterion
            var valueObj = new
            {
                fieldPath = input.FieldPath,
                value = input.Value,
                @operator = input.Operator.ToString()
            };

            var criterion = new CaseDefinitionCriteria
            {
                CaseDefinitionId = definitionId,
                CriterionType = CriterionType.Demographic,
                LogicalOperator = input.LogicalOperator,
                GroupNumber = input.GroupNumber,
                ParentCriteriaId = input.ParentCriteriaId,
                FieldPath = input.FieldPath,
                Operator = input.Operator,
                ValueJson = JsonSerializer.Serialize(valueObj),
                DisplayText = input.DisplayText,
                DisplayOrder = maxDisplayOrder + 1
            };

            _context.CaseDefinitionCriteria.Add(criterion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpGet("{criterionId}")]
        public async Task<IActionResult> GetCriterion(int definitionId, int criterionId)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            System.Diagnostics.Debug.WriteLine($"========== GetCriterion called for ID {criterionId} ==========");
            System.Diagnostics.Debug.WriteLine($"Criterion DisplayOrder: {criterion.DisplayOrder}, Type: {criterion.CriterionType}");

            // If it's a laboratory criterion, also get storage preferences
            object? storagePreferences = null;
            if (criterion.CriterionType == CriterionType.Laboratory)
            {
                System.Diagnostics.Debug.WriteLine($"Lab criterion found - reading storage preferences from unified model");
                System.Diagnostics.Debug.WriteLine($"  Specimen: pref={criterion.SpecimenStoragePreference}, canonical={criterion.CanonicalSpecimenTypeId}");
                System.Diagnostics.Debug.WriteLine($"  Pathogen: pref={criterion.BiomarkerStoragePreference}, canonical={criterion.CanonicalPathogenId}");
                System.Diagnostics.Debug.WriteLine($"  TestMethod: pref={criterion.TestMethodStoragePreference}, canonical={criterion.CanonicalTestMethodId}");
                System.Diagnostics.Debug.WriteLine($"  Result: pref={criterion.ResultStoragePreference}, canonical={criterion.Description}");

                storagePreferences = new
                {
                    specimenStoragePreference = (int)(criterion.SpecimenStoragePreference ?? DataStoragePreference.StoreAsReceived),
                    canonicalSpecimenTypeId = criterion.CanonicalSpecimenTypeId,
                    pathogenStoragePreference = (int)(criterion.BiomarkerStoragePreference ?? DataStoragePreference.StoreAsReceived),
                    canonicalPathogenId = criterion.CanonicalPathogenId,
                    testMethodStoragePreference = (int)(criterion.TestMethodStoragePreference ?? DataStoragePreference.StoreAsReceived),
                    canonicalTestMethodId = criterion.CanonicalTestMethodId,
                    resultStoragePreference = (int)(criterion.ResultStoragePreference ?? DataStoragePreference.StoreAsReceived),
                    canonicalResultValue = criterion.Description
                };
            }

            System.Diagnostics.Debug.WriteLine($"Returning storagePreferences: {storagePreferences != null}");
            System.Diagnostics.Debug.WriteLine($"=========================================");

            var result = new
            {
                id = criterion.Id,
                caseDefinitionId = criterion.CaseDefinitionId,
                criterionType = (int)criterion.CriterionType,
                logicalOperator = (int)criterion.LogicalOperator,
                groupNumber = criterion.GroupNumber,
                parentCriteriaId = criterion.ParentCriteriaId,
                fieldPath = criterion.FieldPath,
                @operator = (int)criterion.Operator,
                valueJson = criterion.ValueJson,
                displayText = criterion.DisplayText,
                displayOrder = criterion.DisplayOrder,
                storagePreferences = storagePreferences
            };

            return Ok(result);
        }

        [HttpDelete("{criterionId}")]
        public async Task<IActionResult> DeleteCriterion(int definitionId, int criterionId)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .Include(c => c.ChildCriteria)
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            // If this criterion has children, delete them recursively
            if (criterion.ChildCriteria.Any())
            {
                await DeleteCriteriaRecursive(criterion.ChildCriteria.ToList());
            }

            _context.CaseDefinitionCriteria.Remove(criterion);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private async Task DeleteCriteriaRecursive(List<CaseDefinitionCriteria> criteria)
        {
            foreach (var child in criteria)
            {
                // Load the child's children
                await _context.Entry(child)
                    .Collection(c => c.ChildCriteria)
                    .LoadAsync();

                // Recursively delete grandchildren
                if (child.ChildCriteria.Any())
                {
                    await DeleteCriteriaRecursive(child.ChildCriteria.ToList());
                }

                _context.CaseDefinitionCriteria.Remove(child);
            }
        }

        [HttpPatch("{criterionId}/operator")]
        public async Task<IActionResult> UpdateOperator(int definitionId, int criterionId, [FromBody] UpdateOperatorInput input)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            criterion.LogicalOperator = input.LogicalOperator;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{criterionId}/create-group")]
        public async Task<IActionResult> CreateGroup(int definitionId, int criterionId)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .Include(c => c.ChildCriteria)
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            if (criterion.ParentCriteriaId != null)
            {
                return BadRequest("Cannot create a group from a nested criterion");
            }

            if (criterion.ChildCriteria?.Any() == true)
            {
                return BadRequest("This criterion is already a group");
            }

            // This criterion is now marked as a group (it already exists)
            // The UI will allow adding child criteria to it
            // No changes needed to the criterion itself - just return success

            return Ok(new { success = true });
        }

        [HttpPatch("{criterionId}/move-to-parent")]
        public async Task<IActionResult> MoveToParent(int definitionId, int criterionId, [FromBody] MoveToParentInput input)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .Include(c => c.ChildCriteria)
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            // Validate the parent exists and is in the same definition
            if (input.ParentCriteriaId.HasValue)
            {
                var parent = await _context.CaseDefinitionCriteria
                    .FirstOrDefaultAsync(c => c.Id == input.ParentCriteriaId.Value && c.CaseDefinitionId == definitionId);

                if (parent == null)
                {
                    return BadRequest("Parent criterion not found");
                }

                // Can't nest under itself
                if (criterion.Id == input.ParentCriteriaId.Value)
                {
                    return BadRequest("Cannot nest a criterion under itself");
                }

                // Can't nest under one of its own children
                if (criterion.ChildCriteria?.Any(c => c.Id == input.ParentCriteriaId.Value) == true)
                {
                    return BadRequest("Cannot nest a parent under its own child");
                }
            }

            // Move the criterion
            criterion.ParentCriteriaId = input.ParentCriteriaId;

            // Reset display order since it's moving to a new context
            criterion.DisplayOrder = 0;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPatch("{criterionId}/reorder")]
        public async Task<IActionResult> ReorderCriterion(int definitionId, int criterionId, [FromBody] ReorderInput input)
        {
            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            // Get siblings (criteria with the same parent)
            var siblings = await _context.CaseDefinitionCriteria
                .Where(c => c.CaseDefinitionId == definitionId && c.ParentCriteriaId == criterion.ParentCriteriaId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            Console.WriteLine($"Reordering criterion {criterionId}, direction: {input.Direction}");
            Console.WriteLine($"Current DisplayOrder: {criterion.DisplayOrder}");
            Console.WriteLine($"Siblings count: {siblings.Count}");
            foreach (var sib in siblings)
            {
                Console.WriteLine($"  - Criterion {sib.Id}: DisplayOrder={sib.DisplayOrder}");
            }

            var currentIndex = siblings.IndexOf(criterion);
            Console.WriteLine($"Current index in siblings: {currentIndex}");

            if (input.Direction.ToLower() == "up" && currentIndex > 0)
            {
                // Swap with previous sibling
                var previous = siblings[currentIndex - 1];
                var tempOrder = criterion.DisplayOrder;
                criterion.DisplayOrder = previous.DisplayOrder;
                previous.DisplayOrder = tempOrder;

                Console.WriteLine($"Swapped {criterion.Id} (now {criterion.DisplayOrder}) with {previous.Id} (now {previous.DisplayOrder})");
            }
            else if (input.Direction.ToLower() == "down" && currentIndex < siblings.Count - 1)
            {
                // Swap with next sibling
                var next = siblings[currentIndex + 1];
                var tempOrder = criterion.DisplayOrder;
                criterion.DisplayOrder = next.DisplayOrder;
                next.DisplayOrder = tempOrder;

                Console.WriteLine($"Swapped {criterion.Id} (now {criterion.DisplayOrder}) with {next.Id} (now {next.DisplayOrder})");
            }
            else
            {
                Console.WriteLine($"No swap performed - already at boundary or invalid direction");
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    // Input DTOs
    public class LabCriterionInput
    {
        public LogicalOperator LogicalOperator { get; set; }
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public List<int> SpecimenTypeIds { get; set; } = new();
        public List<string> PathogenNames { get; set; } = new(); // Legacy: now stores GUIDs as strings (not names)
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
        public string RelativeTo { get; set; } = "OnsetDate";
        public string Direction { get; set; } = "before";
    }

    public class ClinicalCriterionInput
    {
        public LogicalOperator LogicalOperator { get; set; }
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
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
        public int? ParentCriteriaId { get; set; }
        public int CustomFieldId { get; set; }
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }

    public class CaseFieldCriterionInput
    {
        public LogicalOperator LogicalOperator { get; set; }
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public string FieldPath { get; set; } = string.Empty;
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }

    public class UpdateOperatorInput
    {
        public LogicalOperator LogicalOperator { get; set; }
    }

    public class ReorderInput
    {
        public string Direction { get; set; } = string.Empty;
    }

    public class MoveToParentInput
    {
        public int? ParentCriteriaId { get; set; }
    }
}
