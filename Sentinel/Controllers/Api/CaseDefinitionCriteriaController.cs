using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sentinel.Controllers.Api
{
    [Authorize(Policy = "Permission.Settings.Edit")]
    [ApiController]
    [Route("api/case-definitions/{definitionId}/criteria")]
    [EnableRateLimiting("workflow-api-moderate")] // 60 per minute - case definition criteria management
    public class CaseDefinitionCriteriaController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CaseDefinitionCriteriaController> _logger;

        public CaseDefinitionCriteriaController(
            ApplicationDbContext context,
            ILogger<CaseDefinitionCriteriaController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("laboratory")]
        public async Task<IActionResult> AddLabCriterion(int definitionId, [FromBody] LabCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var placementError = await ValidateCriterionPlacementAsync(
                definitionId,
                input.ParentCriteriaId,
                input.LogicalOperator,
                input.GroupNumber);
            if (placementError is not null)
            {
                return placementError;
            }

            if (!AreDefinedStoragePreferences(input))
            {
                return BadRequest("One or more laboratory storage preferences are invalid.");
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

            // Build ValueJson for lab criterion including storage preferences
            var valueObj = new
            {
                specimenTypeIds = input.SpecimenTypeIds,
                pathogenIds = input.PathogenIds,
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
                AcceptablePathogensJson = JsonSerializer.Serialize(input.PathogenIds),
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


            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPut("{criterionId}/laboratory")]
        public async Task<IActionResult> UpdateLabCriterion(int definitionId, int criterionId, [FromBody] LabCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!Enum.IsDefined(input.LogicalOperator) || input.GroupNumber < 0 ||
                !AreDefinedStoragePreferences(input))
            {
                return BadRequest("The laboratory criterion contains an invalid operator, group number, or storage preference.");
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
                    pathogenIds = input.PathogenIds,
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
                criterion.AcceptablePathogensJson = JsonSerializer.Serialize(input.PathogenIds);
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
                _logger.LogError(ex, "Unable to update a laboratory criterion for case definition {DefinitionId}", definitionId);
                return StatusCode(500, new
                {
                    error = "The laboratory criterion could not be saved. Check the configured values and try again.",
                    traceId = HttpContext.TraceIdentifier
                });
            }
        }

        [HttpPost("clinical")]
        public async Task<IActionResult> AddClinicalCriterion(int definitionId, [FromBody] ClinicalCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var placementError = await ValidateCriterionPlacementAsync(
                definitionId,
                input.ParentCriteriaId,
                input.LogicalOperator,
                input.GroupNumber);
            if (placementError is not null)
            {
                return placementError;
            }

            if (input.SymptomIds.Count == 0 ||
                (input.MinCount.HasValue &&
                 (input.MinCount.Value < 1 || input.MinCount.Value > input.SymptomIds.Count)))
            {
                return BadRequest("Select at least one symptom and use a valid minimum count.");
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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var placementError = await ValidateCriterionPlacementAsync(
                definitionId,
                input.ParentCriteriaId,
                input.LogicalOperator,
                input.GroupNumber);
            if (placementError is not null)
            {
                return placementError;
            }

            if (!Enum.IsDefined(input.Operator))
            {
                return BadRequest("The comparison operator is invalid.");
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
                .FirstOrDefaultAsync(cf => cf.Id == input.CustomFieldId && cf.IsActive);

            if (customField == null)
            {
                return BadRequest("Custom field not found or inactive");
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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var placementError = await ValidateCriterionPlacementAsync(
                definitionId,
                input.ParentCriteriaId,
                input.LogicalOperator,
                input.GroupNumber);
            if (placementError is not null)
            {
                return placementError;
            }

            if (!Enum.IsDefined(input.Operator))
            {
                return BadRequest("The comparison operator is invalid.");
            }

            if (!AllowedCaseFieldPaths.Contains(input.FieldPath, StringComparer.Ordinal))
            {
                return BadRequest("The selected case field is invalid.");
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

        [HttpPut("{criterionId}/clinical")]
        public async Task<IActionResult> UpdateClinicalCriterion(
            int definitionId,
            int criterionId,
            [FromBody] ClinicalCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid ||
                !Enum.IsDefined(input.LogicalOperator) ||
                input.GroupNumber < 0 ||
                input.SymptomIds.Count == 0 ||
                (input.MinCount.HasValue &&
                 (input.MinCount.Value < 1 || input.MinCount.Value > input.SymptomIds.Count)))
            {
                return BadRequest("The clinical criterion contains invalid values.");
            }

            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);
            if (criterion is null || criterion.CriterionType != CriterionType.Clinical)
            {
                return NotFound();
            }

            var value = new
            {
                symptomIds = input.SymptomIds,
                requireAll = input.RequireAll,
                minCount = input.MinCount,
                severityFilter = input.SeverityFilter
            };

            criterion.LogicalOperator = input.LogicalOperator;
            criterion.GroupNumber = input.GroupNumber;
            criterion.Operator = input.RequireAll ? ComparisonOperator.Equals : ComparisonOperator.InList;
            criterion.ValueJson = JsonSerializer.Serialize(value);
            criterion.DisplayText = input.DisplayText;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPut("{criterionId}/custom-field")]
        public async Task<IActionResult> UpdateCustomFieldCriterion(
            int definitionId,
            int criterionId,
            [FromBody] CustomFieldCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid ||
                !Enum.IsDefined(input.LogicalOperator) ||
                !Enum.IsDefined(input.Operator) ||
                input.GroupNumber < 0)
            {
                return BadRequest("The custom-field criterion contains invalid values.");
            }

            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);
            if (criterion is null || criterion.CriterionType != CriterionType.CustomField)
            {
                return NotFound();
            }

            var customField = await _context.CustomFieldDefinitions
                .FirstOrDefaultAsync(cf => cf.Id == input.CustomFieldId && cf.IsActive);
            if (customField is null)
            {
                return BadRequest("Custom field not found or inactive.");
            }

            var value = new
            {
                customFieldId = input.CustomFieldId,
                customFieldName = customField.Name,
                customFieldLabel = customField.Label,
                fieldType = customField.FieldType.ToString(),
                value = input.Value,
                @operator = input.Operator.ToString()
            };

            criterion.LogicalOperator = input.LogicalOperator;
            criterion.GroupNumber = input.GroupNumber;
            criterion.FieldPath = $"CustomFields.{customField.Name}";
            criterion.Operator = input.Operator;
            criterion.ValueJson = JsonSerializer.Serialize(value);
            criterion.DisplayText = input.DisplayText;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, criterionId = criterion.Id });
        }

        [HttpPut("{criterionId}/case-field")]
        public async Task<IActionResult> UpdateCaseFieldCriterion(
            int definitionId,
            int criterionId,
            [FromBody] CaseFieldCriterionInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!ModelState.IsValid ||
                !Enum.IsDefined(input.LogicalOperator) ||
                !Enum.IsDefined(input.Operator) ||
                input.GroupNumber < 0 ||
                !AllowedCaseFieldPaths.Contains(input.FieldPath, StringComparer.Ordinal))
            {
                return BadRequest("The case-field criterion contains invalid values.");
            }

            var criterion = await _context.CaseDefinitionCriteria
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);
            if (criterion is null || criterion.CriterionType != CriterionType.Demographic)
            {
                return NotFound();
            }

            var value = new
            {
                fieldPath = input.FieldPath,
                value = input.Value,
                @operator = input.Operator.ToString()
            };

            criterion.LogicalOperator = input.LogicalOperator;
            criterion.GroupNumber = input.GroupNumber;
            criterion.FieldPath = input.FieldPath;
            criterion.Operator = input.Operator;
            criterion.ValueJson = JsonSerializer.Serialize(value);
            criterion.DisplayText = input.DisplayText;
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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!Enum.IsDefined(input.LogicalOperator))
            {
                return BadRequest("The logical operator is invalid.");
            }

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

        [HttpPatch("{criterionId}/group-exit-operator")]
        public async Task<IActionResult> UpdateGroupExitOperator(int definitionId, int criterionId, [FromBody] UpdateGroupExitOperatorInput input)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!Enum.IsDefined(input.GroupExitOperator))
            {
                return BadRequest("The group exit operator is invalid.");
            }

            var criterion = await _context.CaseDefinitionCriteria
                .Include(c => c.ChildCriteria)
                .FirstOrDefaultAsync(c => c.Id == criterionId && c.CaseDefinitionId == definitionId);

            if (criterion == null)
            {
                return NotFound();
            }

            // Only allow setting GroupExitOperator on parent criteria (groups)
            if (criterion.ChildCriteria?.Any() != true)
            {
                return BadRequest("GroupExitOperator can only be set on parent criteria with children");
            }

            criterion.GroupExitOperator = input.GroupExitOperator;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("{criterionId}/create-group")]
        public async Task<IActionResult> CreateGroup(int definitionId, int criterionId)
        {
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

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
                    .AsNoTracking()
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

                // Walk the proposed parent's ancestry rather than just its
                // direct children. A direct-child check misses a grandchild
                // and permits a cyclic criteria tree via a forged request.
                if (await WouldCreateParentCycleAsync(
                        definitionId,
                        criterion.Id,
                        input.ParentCriteriaId.Value))
                {
                    return BadRequest("Cannot nest a criterion under one of its descendants");
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
            var draftError = await EnsureDefinitionIsDraftAsync(definitionId);
            if (draftError is not null) return draftError;

            if (!string.Equals(input.Direction, "up", StringComparison.Ordinal) &&
                !string.Equals(input.Direction, "down", StringComparison.Ordinal))
            {
                return BadRequest("Direction must be either 'up' or 'down'.");
            }

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

            foreach (var sib in siblings)
            {
            }

            var currentIndex = siblings.IndexOf(criterion);

            if (input.Direction.ToLower() == "up" && currentIndex > 0)
            {
                // Swap with previous sibling
                var previous = siblings[currentIndex - 1];
                var tempOrder = criterion.DisplayOrder;
                criterion.DisplayOrder = previous.DisplayOrder;
                previous.DisplayOrder = tempOrder;

            }
            else if (input.Direction.ToLower() == "down" && currentIndex < siblings.Count - 1)
            {
                // Swap with next sibling
                var next = siblings[currentIndex + 1];
                var tempOrder = criterion.DisplayOrder;
                criterion.DisplayOrder = next.DisplayOrder;
                next.DisplayOrder = tempOrder;

            }
            else
            {
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // These are the only case paths offered by the criteria builder. Keep
        // the allow-list at the server boundary so a forged request cannot add
        // arbitrary reflection paths to a persisted definition.
        private static readonly HashSet<string> AllowedCaseFieldPaths = new(StringComparer.Ordinal)
        {
            "ReportDate",
            "OnsetDate",
            "DiagnosisDate",
            "HospitalizedDate",
            "IsHospitalized",
            "IsFatal",
            "DateOfDeath",
            "Patient.DateOfBirth",
            "Patient.Gender",
            "Patient.CountryOfBirth"
        };

        private async Task<IActionResult?> EnsureDefinitionIsDraftAsync(int definitionId)
        {
            var status = await _context.CaseDefinitions
                .AsNoTracking()
                .Where(definition => definition.Id == definitionId)
                .Select(definition => (CaseDefinitionStatus?)definition.Status)
                .FirstOrDefaultAsync();

            if (status is null)
            {
                return NotFound("Case definition not found.");
            }

            return status == CaseDefinitionStatus.Draft
                ? null
                : Conflict("Case definition criteria can be changed only while the definition is a draft. Create a new draft version to revise an active or archived definition.");
        }

        private async Task<IActionResult?> ValidateCriterionPlacementAsync(
            int definitionId,
            int? parentCriteriaId,
            LogicalOperator logicalOperator,
            int groupNumber)
        {
            if (!Enum.IsDefined(logicalOperator) || groupNumber < 0)
            {
                return BadRequest("The logical operator or group number is invalid.");
            }

            if (!parentCriteriaId.HasValue)
            {
                return null;
            }

            var parentExists = await _context.CaseDefinitionCriteria
                .AsNoTracking()
                .AnyAsync(c => c.Id == parentCriteriaId.Value && c.CaseDefinitionId == definitionId);

            return parentExists
                ? null
                : BadRequest("The selected parent criterion is not part of this case definition.");
        }

        private static bool AreDefinedStoragePreferences(LabCriterionInput input) =>
            Enum.IsDefined(input.SpecimenStoragePreference) &&
            Enum.IsDefined(input.PathogenStoragePreference) &&
            Enum.IsDefined(input.TestMethodStoragePreference) &&
            Enum.IsDefined(input.ResultStoragePreference);

        private async Task<bool> WouldCreateParentCycleAsync(
            int definitionId,
            int criterionId,
            int proposedParentId)
        {
            var visited = new HashSet<int>();
            int? currentId = proposedParentId;

            while (currentId.HasValue)
            {
                if (!visited.Add(currentId.Value) || currentId.Value == criterionId)
                {
                    return true;
                }

                currentId = await _context.CaseDefinitionCriteria
                    .AsNoTracking()
                    .Where(c => c.Id == currentId.Value && c.CaseDefinitionId == definitionId)
                    .Select(c => c.ParentCriteriaId)
                    .SingleOrDefaultAsync();
            }

            return false;
        }
    }

    // Input DTOs
    public class LabCriterionInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator LogicalOperator { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public List<int> SpecimenTypeIds { get; set; } = new();
        public List<Guid> PathogenIds { get; set; } = new();
        public List<int> TestMethodIds { get; set; } = new();
        public List<string> ResultValues { get; set; } = new();
        public TimeConstraintInput? TimeConstraint { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string DisplayText { get; set; } = string.Empty;

        // Storage preferences
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataStoragePreference SpecimenStoragePreference { get; set; }
        public int? CanonicalSpecimenTypeId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataStoragePreference PathogenStoragePreference { get; set; }
        public Guid? CanonicalPathogenId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public DataStoragePreference TestMethodStoragePreference { get; set; }
        public int? CanonicalTestMethodId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
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
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator LogicalOperator { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public List<int> SymptomIds { get; set; } = new();
        public bool RequireAll { get; set; }
        public int? MinCount { get; set; }
        public string? SeverityFilter { get; set; }
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string DisplayText { get; set; } = string.Empty;
    }

    public class CustomFieldCriterionInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator LogicalOperator { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public int CustomFieldId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string DisplayText { get; set; } = string.Empty;
    }

    public class CaseFieldCriterionInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator LogicalOperator { get; set; }
        [System.ComponentModel.DataAnnotations.Range(0, int.MaxValue)]
        public int GroupNumber { get; set; }
        public int? ParentCriteriaId { get; set; }
        public string FieldPath { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.StringLength(500)]
        public string DisplayText { get; set; } = string.Empty;
    }

    public class UpdateOperatorInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator LogicalOperator { get; set; }
    }

    public class UpdateGroupExitOperatorInput
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogicalOperator GroupExitOperator { get; set; }
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
