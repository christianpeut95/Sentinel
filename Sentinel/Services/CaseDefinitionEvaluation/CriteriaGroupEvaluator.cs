using Microsoft.Extensions.Logging;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Evaluates groups of criteria with AND/OR logic
    /// </summary>
    public class CriteriaGroupEvaluator
    {
        private readonly CriterionEvaluator _criterionEvaluator;
        private readonly ILogger<CriteriaGroupEvaluator> _logger;

        public CriteriaGroupEvaluator(
            CriterionEvaluator criterionEvaluator,
            ILogger<CriteriaGroupEvaluator> logger)
        {
            _criterionEvaluator = criterionEvaluator;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates a group of criteria against a case
        /// </summary>
        /// <param name="caseEntity">The case to evaluate</param>
        /// <param name="rootCriteria">All criteria for the case definition (including nested groups)</param>
        /// <param name="parentId">The parent criterion ID for the group to evaluate (null for root level)</param>
        /// <returns>Evaluation result with child results</returns>
        public async Task<CriterionResult> EvaluateGroupAsync(
            Case caseEntity,
            List<CaseDefinitionCriteria> rootCriteria,
            int? parentId = null)
        {
            var indent = parentId.HasValue ? "  " : "";
            var groupLevel = parentId.HasValue ? "NESTED GROUP" : "ROOT LEVEL";

            _logger.LogInformation("{Indent}📁 Evaluating {Level} (ParentId: {ParentId})",
                indent, groupLevel, parentId?.ToString() ?? "null");

            var groupResult = new CriterionResult
            {
                CriterionId = parentId ?? 0,
                DisplayText = parentId.HasValue ? "Nested Group" : "Root Criteria",
                ChildResults = new List<CriterionResult>()
            };

            // Get all criteria at this level (children of parentId)
            var criteriaAtThisLevel = rootCriteria
                .Where(c => c.ParentCriteriaId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ToList();

            _logger.LogInformation("{Indent}Found {Count} criteria at this level", indent, criteriaAtThisLevel.Count);

            if (!criteriaAtThisLevel.Any())
            {
                _logger.LogWarning("{Indent}❌ No criteria found in this group", indent);
                groupResult.IsMatch = false;
                groupResult.ActualValue = "No criteria in group";
                return groupResult;
            }

            // Determine the logical operator for this group
            // Use the first criterion's LogicalOperator (they should all be the same at this level)
            var logicalOperator = criteriaAtThisLevel.First().LogicalOperator;
            groupResult.LogicalOperator = logicalOperator;

            _logger.LogInformation("{Indent}Using {Operator} logic (value: {OperatorValue}) for this group", 
                indent, logicalOperator, (int)logicalOperator);

            // Evaluate each criterion or nested group
            int criterionIndex = 1;
            foreach (var criterion in criteriaAtThisLevel)
            {
                _logger.LogInformation("{Indent}[{Index}/{Total}] Evaluating: {DisplayText} (Type: {Type})",
                    indent, criterionIndex, criteriaAtThisLevel.Count, criterion.DisplayText, criterion.CriterionType);

                CriterionResult childResult;

                // Check if this criterion is a group (has children)
                var hasChildren = rootCriteria.Any(c => c.ParentCriteriaId == criterion.Id);
                var hasCriterionType = criterion.CriterionType != 0; // 0 = default/unset

                // Handle dual-purpose criteria: criteria that are BOTH evaluable AND have children
                if (hasChildren && hasCriterionType)
                {
                    _logger.LogInformation("{Indent}  → This criterion has BOTH a type to evaluate AND children (dual-purpose)", indent);

                    // Create a wrapper result to combine both the criterion and its children
                    childResult = new CriterionResult
                    {
                        CriterionId = criterion.Id,
                        CriterionType = criterion.CriterionType,
                        DisplayText = criterion.DisplayText,
                        LogicalOperator = criterion.LogicalOperator
                    };

                    // 1. Evaluate the criterion itself
                    _logger.LogInformation("{Indent}  → Step 1: Evaluating the criterion itself", indent);
                    var criterionResult = await _criterionEvaluator.EvaluateAsync(caseEntity, criterion);
                    _logger.LogInformation("{Indent}     {Icon} Criterion result: {Result}", 
                        indent,
                        criterionResult.IsMatch ? "✅" : "❌",
                        criterionResult.IsMatch ? "MATCH" : "NO MATCH");

                    // 2. Evaluate the children as a nested group
                    _logger.LogInformation("{Indent}  → Step 2: Evaluating children", indent);
                    var childrenResult = await EvaluateGroupAsync(caseEntity, rootCriteria, criterion.Id);
                    _logger.LogInformation("{Indent}     {Icon} Children result: {Result}", 
                        indent,
                        childrenResult.IsMatch ? "✅" : "❌",
                        childrenResult.IsMatch ? "MATCH" : "NO MATCH");

                    // 3. Combine results based on the criterion's LogicalOperator
                    // The children's logical operator determines how they combine with each other (handled in nested call)
                    // This criterion's logical operator determines how IT combines with its children
                    childResult.ChildResults.Add(criterionResult);
                    childResult.ChildResults.Add(childrenResult);

                    childResult.IsMatch = criterion.LogicalOperator switch
                    {
                        LogicalOperator.AND => criterionResult.IsMatch && childrenResult.IsMatch,
                        LogicalOperator.OR => criterionResult.IsMatch || childrenResult.IsMatch,
                        _ => criterionResult.IsMatch && childrenResult.IsMatch // Default to AND
                    };

                    childResult.ExpectedValue = $"{criterionResult.ExpectedValue} {criterion.LogicalOperator} ({childrenResult.ExpectedValue})";
                    childResult.ActualValue = $"{criterionResult.ActualValue} {criterion.LogicalOperator} ({childrenResult.ActualValue})";

                    _logger.LogInformation("{Indent}  → Combined with {Operator}: {Icon} {Result}", 
                        indent,
                        criterion.LogicalOperator,
                        childResult.IsMatch ? "✅" : "❌",
                        childResult.IsMatch ? "MATCH" : "NO MATCH");
                }
                else if (hasChildren)
                {
                    _logger.LogInformation("{Indent}  → This is a group container (has children, no criterion type)", indent);
                    // Recursively evaluate nested group
                    childResult = await EvaluateGroupAsync(caseEntity, rootCriteria, criterion.Id);
                }
                else
                {
                    // Evaluate individual criterion
                    childResult = await _criterionEvaluator.EvaluateAsync(caseEntity, criterion);
                }

                _logger.LogInformation("{Indent}  {Icon} Result: {Result}", 
                    indent,
                    childResult.IsMatch ? "✅" : "❌",
                    childResult.IsMatch ? "MATCH" : "NO MATCH");
                if (!string.IsNullOrEmpty(childResult.ActualValue))
                {
                    _logger.LogInformation("{Indent}     Actual: {Actual}", indent, childResult.ActualValue);
                }
                if (!string.IsNullOrEmpty(childResult.ExpectedValue))
                {
                    _logger.LogInformation("{Indent}     Expected: {Expected}", indent, childResult.ExpectedValue);
                }

                groupResult.ChildResults.Add(childResult);
                criterionIndex++;
            }

            // Apply logical operator to combine results
            groupResult.IsMatch = logicalOperator switch
            {
                LogicalOperator.AND => groupResult.ChildResults.All(r => r.IsMatch),
                LogicalOperator.OR => groupResult.ChildResults.Any(r => r.IsMatch),
                _ => groupResult.ChildResults.Any(r => r.IsMatch) // Default to OR for safety (invalid operator shouldn't fail all cases)
            };

            // Build descriptive actual/expected values
            var matchCount = groupResult.ChildResults.Count(r => r.IsMatch);
            var totalCount = groupResult.ChildResults.Count;

            groupResult.ActualValue = $"{matchCount} of {totalCount} criteria matched";
            groupResult.ExpectedValue = logicalOperator == LogicalOperator.AND
                ? $"ALL {totalCount} criteria must match"
                : $"AT LEAST ONE of {totalCount} criteria must match";

            _logger.LogInformation("{Indent}📊 GROUP RESULT ({Operator}): {Icon} {Result}",
                indent, 
                logicalOperator,
                groupResult.IsMatch ? "✅" : "❌",
                groupResult.IsMatch ? "PASS" : "FAIL");
            _logger.LogInformation("{Indent}   {MatchCount}/{TotalCount} criteria matched", 
                indent, matchCount, totalCount);

            return groupResult;
        }
    }
}
