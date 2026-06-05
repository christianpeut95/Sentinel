using Microsoft.Extensions.Logging;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using System.Text.Json;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Evaluates individual case definition criteria against case data
    /// </summary>
    public class CriterionEvaluator
    {
        private readonly OperatorEvaluator _operatorEvaluator;
        private readonly FieldResolver _fieldResolver;
        private readonly ILogger<CriterionEvaluator> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new() 
        { 
            PropertyNameCaseInsensitive = true 
        };

        public CriterionEvaluator(
            OperatorEvaluator operatorEvaluator, 
            FieldResolver fieldResolver,
            ILogger<CriterionEvaluator> logger)
        {
            _operatorEvaluator = operatorEvaluator;
            _fieldResolver = fieldResolver;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates a single criterion against a case
        /// </summary>
        public async Task<CriterionResult> EvaluateAsync(Case caseEntity, CaseDefinitionCriteria criterion)
        {
            _logger.LogInformation("    🔍 Evaluating {Type} criterion: {DisplayText}",
                criterion.CriterionType, criterion.DisplayText);

            var result = new CriterionResult
            {
                CriterionId = criterion.Id,
                CriterionType = criterion.CriterionType,
                DisplayText = criterion.DisplayText,
                LogicalOperator = criterion.LogicalOperator
            };

            try
            {
                result.IsMatch = criterion.CriterionType switch
                {
                    CriterionType.Laboratory => await EvaluateLaboratoryCriterionAsync(caseEntity, criterion, result),
                    CriterionType.Clinical => EvaluateClinicalCriterion(caseEntity, criterion, result),
                    CriterionType.CustomField => EvaluateCustomFieldCriterion(caseEntity, criterion, result),
                    CriterionType.Demographic => EvaluateDemographicCriterion(caseEntity, criterion, result),
                    _ => false
                };

                _logger.LogInformation("       {Icon} {Result}", 
                    result.IsMatch ? "✅" : "❌",
                    result.IsMatch ? "MATCHED" : "NOT MATCHED");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    _logger.LogWarning("       ⚠️ Error: {Error}", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "       ❌ Exception during evaluation");
                result.IsMatch = false;
                result.ErrorMessage = $"Evaluation error: {ex.Message}";
            }

            return result;
        }

        #region Laboratory Criterion Evaluation

        private async Task<bool> EvaluateLaboratoryCriterionAsync(Case caseEntity, CaseDefinitionCriteria criterion, CriterionResult result)
        {
            if (caseEntity.LabResults == null || !caseEntity.LabResults.Any())
            {
                result.ActualValue = "No lab results";
                result.ExpectedValue = "At least one lab result matching criteria";
                return false;
            }

            // Parse ValueJson
            var valueData = JsonSerializer.Deserialize<LaboratoryCriterionValue>(criterion.ValueJson, JsonOptions);
            if (valueData == null)
            {
                result.ErrorMessage = "Invalid ValueJson format";
                return false;
            }

            result.ExpectedValue = BuildLabExpectedValue(valueData);

            // Find lab results that match ALL criteria
            var matchingResults = caseEntity.LabResults.Where(lr =>
            {
                // Check specimen type
                if (valueData.SpecimenTypeIds != null && valueData.SpecimenTypeIds.Any())
                {
                    if (!lr.SpecimenTypeId.HasValue || !valueData.SpecimenTypeIds.Contains(lr.SpecimenTypeId.Value))
                    {
                        return false;
                    }
                }

                // Check if any marker matches ALL required criteria
                if (lr.Markers == null || !lr.Markers.Any())
                {
                    return false;
                }

                bool hasMatchingMarker = lr.Markers.Any(m =>
                {
                    // Check pathogen - support both by ID and by name for backward compatibility
                    if (valueData.PathogenNames != null && valueData.PathogenNames.Any())
                    {
                        if (m.Pathogen == null)
                        {
                            return false;
                        }

                        // Try to match by ID first (if PathogenNames contains GUIDs), then fall back to name matching
                        bool matches = valueData.PathogenNames.Any(pathogenIdentifier =>
                        {
                            // Try parsing as GUID first (new behavior)
                            if (Guid.TryParse(pathogenIdentifier, out Guid pathogenId))
                            {
                                return m.PathogenId == pathogenId;
                            }
                            // Fall back to name comparison (legacy behavior)
                            return string.Equals(m.Pathogen.Name, pathogenIdentifier, StringComparison.OrdinalIgnoreCase);
                        });

                        if (!matches)
                        {
                            return false;
                        }
                    }

                    // Check test method
                    if (valueData.TestMethodIds != null && valueData.TestMethodIds.Any())
                    {
                        if (!m.TestMethodId.HasValue || !valueData.TestMethodIds.Contains(m.TestMethodId.Value))
                        {
                            return false;
                        }
                    }

                    // Check result value
                    if (valueData.ResultValues != null && valueData.ResultValues.Any())
                    {
                        if (string.IsNullOrEmpty(m.QualitativeResultText) || !valueData.ResultValues.Contains(m.QualitativeResultText))
                        {
                            return false;
                        }
                    }

                    return true; // This marker matches all criteria
                });

                if (!hasMatchingMarker)
                {
                    return false;
                }

                // Check time constraint
                if (valueData.TimeConstraint != null)
                {
                    DateTime? referenceDate = valueData.TimeConstraint.RelativeTo?.ToLower() switch
                    {
                        "symptomonsetdate" => caseEntity.DateOfOnset,
                        "datereported" => caseEntity.DateOfNotification,
                        "dateentered" => caseEntity.DateOfNotification,
                        _ => caseEntity.DateOfOnset
                    };

                    if (!referenceDate.HasValue || !lr.SpecimenCollectionDate.HasValue)
                    {
                        return false;
                    }

                    DateTime minDate, maxDate;
                    if (valueData.TimeConstraint.Direction?.ToLower() == "before")
                    {
                        minDate = referenceDate.Value.AddDays(-valueData.TimeConstraint.Days);
                        maxDate = referenceDate.Value;
                    }
                    else // after
                    {
                        minDate = referenceDate.Value;
                        maxDate = referenceDate.Value.AddDays(valueData.TimeConstraint.Days);
                    }

                    if (lr.SpecimenCollectionDate.Value < minDate || lr.SpecimenCollectionDate.Value > maxDate)
                    {
                        return false;
                    }
                }

                return true; // This lab result matches all criteria
            }).ToList();

            result.ActualValue = matchingResults.Any() 
                ? $"{matchingResults.Count} matching lab result(s)" 
                : "No matching lab results";

            return matchingResults.Any();
        }

        private string BuildLabExpectedValue(LaboratoryCriterionValue value)
        {
            var parts = new List<string>();

            if (value.PathogenNames?.Any() == true)
            {
                parts.Add($"Pathogen: {string.Join(" OR ", value.PathogenNames)}");
            }

            if (value.ResultValues?.Any() == true)
            {
                parts.Add($"Result: {string.Join(" OR ", value.ResultValues)}");
            }

            if (value.TimeConstraint != null)
            {
                parts.Add($"Within {value.TimeConstraint.Days} days {value.TimeConstraint.Direction} {value.TimeConstraint.RelativeTo}");
            }

            return parts.Any() ? string.Join(", ", parts) : "Lab result present";
        }

        #endregion

        #region Clinical Criterion Evaluation

        private bool EvaluateClinicalCriterion(Case caseEntity, CaseDefinitionCriteria criterion, CriterionResult result)
        {
            if (caseEntity.CaseSymptoms == null || !caseEntity.CaseSymptoms.Any())
            {
                result.ActualValue = "No symptoms recorded";
                result.ExpectedValue = "Clinical symptoms present";
                return false;
            }

            // Parse ValueJson
            var valueData = JsonSerializer.Deserialize<ClinicalCriterionValue>(criterion.ValueJson, JsonOptions);
            if (valueData == null || valueData.SymptomIds == null || !valueData.SymptomIds.Any())
            {
                result.ErrorMessage = "Invalid ValueJson format";
                return false;
            }

            // Get matching symptoms
            var matchingSymptoms = caseEntity.CaseSymptoms
                .Where(s => valueData.SymptomIds.Contains(s.SymptomId))
                .ToList();

            // Apply severity filter if specified
            if (!string.IsNullOrEmpty(valueData.SeverityFilter))
            {
                matchingSymptoms = matchingSymptoms
                    .Where(s => MatchesSeverity(s.Severity, valueData.SeverityFilter))
                    .ToList();
            }

            result.ActualValue = matchingSymptoms.Any() 
                ? $"{matchingSymptoms.Count} symptom(s) present" 
                : "No matching symptoms";

            // Evaluate based on mode
            if (valueData.RequireAll)
            {
                result.ExpectedValue = $"All {valueData.SymptomIds.Count} symptoms required";
                return matchingSymptoms.Count == valueData.SymptomIds.Count;
            }

            if (valueData.MinCount.HasValue && valueData.MinCount > 0)
            {
                result.ExpectedValue = $"At least {valueData.MinCount} symptoms required";
                return matchingSymptoms.Count >= valueData.MinCount.Value;
            }

            // Default: ANY
            result.ExpectedValue = "At least one symptom required";
            return matchingSymptoms.Any();
        }

        private bool MatchesSeverity(string? actualSeverity, string requiredSeverity)
        {
            if (string.IsNullOrEmpty(actualSeverity))
            {
                return false;
            }

            // Define severity hierarchy
            var severityLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Mild", 1 },
                { "Moderate", 2 },
                { "Severe", 3 }
            };

            if (!severityLevels.TryGetValue(actualSeverity, out int actualLevel))
            {
                return false;
            }

            if (!severityLevels.TryGetValue(requiredSeverity, out int requiredLevel))
            {
                return false;
            }

            return actualLevel >= requiredLevel;
        }

        #endregion

        #region Custom Field Criterion Evaluation

        private bool EvaluateCustomFieldCriterion(Case caseEntity, CaseDefinitionCriteria criterion, CriterionResult result)
        {
            // Parse ValueJson
            var valueData = JsonSerializer.Deserialize<CustomFieldCriterionValue>(criterion.ValueJson, JsonOptions);
            if (valueData == null)
            {
                result.ErrorMessage = "Invalid ValueJson format";
                return false;
            }

            result.FieldPath = $"CustomField[{valueData.CustomFieldId}]";
            result.ExpectedValue = $"{valueData.Operator} '{valueData.Value}'";

            // Find the custom field value across all custom field types
            string? actualValue = null;

            actualValue ??= caseEntity.CustomFieldStrings?.FirstOrDefault(cf => cf.FieldDefinitionId == valueData.CustomFieldId)?.Value;
            actualValue ??= caseEntity.CustomFieldNumbers?.FirstOrDefault(cf => cf.FieldDefinitionId == valueData.CustomFieldId)?.Value?.ToString();
            actualValue ??= caseEntity.CustomFieldDates?.FirstOrDefault(cf => cf.FieldDefinitionId == valueData.CustomFieldId)?.Value?.ToString("yyyy-MM-dd");
            actualValue ??= caseEntity.CustomFieldBooleans?.FirstOrDefault(cf => cf.FieldDefinitionId == valueData.CustomFieldId)?.Value?.ToString();
            actualValue ??= caseEntity.CustomFieldLookups?.FirstOrDefault(cf => cf.FieldDefinitionId == valueData.CustomFieldId)?.LookupValueId?.ToString();

            if (actualValue == null)
            {
                result.ActualValue = "Field not set";
                return false;
            }

            result.ActualValue = actualValue;

            // Parse operator from string
            if (!Enum.TryParse<ComparisonOperator>(valueData.Operator, true, out var comparisonOperator))
            {
                result.ErrorMessage = $"Unknown operator: {valueData.Operator}";
                return false;
            }

            return _operatorEvaluator.Evaluate(actualValue, valueData.Value, comparisonOperator);
        }

        #endregion

        #region Demographic/Case Data Criterion Evaluation

        private bool EvaluateDemographicCriterion(Case caseEntity, CaseDefinitionCriteria criterion, CriterionResult result)
        {
            // Parse ValueJson
            var valueData = JsonSerializer.Deserialize<DemographicCriterionValue>(criterion.ValueJson, JsonOptions);
            if (valueData == null || string.IsNullOrEmpty(valueData.FieldPath))
            {
                result.ErrorMessage = "Invalid ValueJson format";
                return false;
            }

            result.FieldPath = valueData.FieldPath;
            result.ExpectedValue = $"{valueData.Operator} '{valueData.Value}'";

            // Resolve field value using FieldResolver
            var actualValue = _fieldResolver.ResolveFieldValue(caseEntity, valueData.FieldPath);
            result.ActualValue = actualValue?.ToString() ?? "null";

            // Parse operator from string
            if (!Enum.TryParse<ComparisonOperator>(valueData.Operator, true, out var comparisonOperator))
            {
                result.ErrorMessage = $"Unknown operator: {valueData.Operator}";
                return false;
            }

            return _operatorEvaluator.Evaluate(actualValue, valueData.Value, comparisonOperator);
        }

        #endregion

        #region Value Classes

        private class LaboratoryCriterionValue
        {
            public List<int>? SpecimenTypeIds { get; set; }
            public List<string>? PathogenNames { get; set; }
            public List<int>? TestMethodIds { get; set; }
            public List<string>? ResultValues { get; set; }
            public TimeConstraint? TimeConstraint { get; set; }
        }

        private class TimeConstraint
        {
            public int Days { get; set; }
            public string? RelativeTo { get; set; }
            public string? Direction { get; set; }
        }

        private class ClinicalCriterionValue
        {
            public List<int>? SymptomIds { get; set; }
            public bool RequireAll { get; set; }
            public int? MinCount { get; set; }
            public string? SeverityFilter { get; set; }
        }

        private class CustomFieldCriterionValue
        {
            public int CustomFieldId { get; set; }
            public string? Operator { get; set; }
            public string? Value { get; set; }
        }

        private class DemographicCriterionValue
        {
            public string? FieldPath { get; set; }
            public string? Operator { get; set; }
            public string? Value { get; set; }
        }

        #endregion
    }
}
