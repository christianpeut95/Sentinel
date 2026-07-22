using Microsoft.Extensions.Logging;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation;

/// <summary>
/// Unified tree-based evaluator for case definition criteria.
/// Shared by both manual and HL7 evaluation paths.
/// Evaluates nested groups through ParentCriteriaId, uses LogicalOperator for internal group logic,
/// and uses GroupExitOperator to combine a group with the following root criterion.
/// </summary>
public class TreeBasedCriteriaEvaluator
{
    private readonly ILogger<TreeBasedCriteriaEvaluator> _logger;

    public TreeBasedCriteriaEvaluator(ILogger<TreeBasedCriteriaEvaluator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Evaluates a list of root-level criteria using tree-based logic.
    /// Returns true if the entire expression evaluates to true.
    /// </summary>
    /// <param name="allCriteria">Complete criteria tree (all criteria for the case definition)</param>
    /// <param name="criterionEvaluator">Function to evaluate individual criterion match status</param>
    /// <param name="contextName">Context name for logging (e.g., "CaseDefId=5" or "Manual Eval")</param>
    public async Task<bool> EvaluateAsync(
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName = "")
    {
        if (!allCriteria.Any())
        {
            _logger.LogDebug("[TREE EVAL {Context}] No criteria to evaluate", contextName);
            return false;
        }

        // Get root-level criteria (no parent)
        var rootCriteria = allCriteria
            .Where(c => c.ParentCriteriaId == null)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        if (!rootCriteria.Any())
        {
            _logger.LogWarning("[TREE EVAL {Context}] No root criteria found", contextName);
            return false;
        }

        _logger.LogInformation("[TREE EVAL {Context}] Evaluating {Count} root criteria", contextName, rootCriteria.Count);

        // Evaluate root criteria sequentially with group-exit operators
        return await EvaluateRootLevelAsync(rootCriteria, allCriteria, criterionEvaluator, contextName);
    }

    /// <summary>
    /// Evaluates root-level criteria, respecting GroupExitOperator to combine group results with following criteria.
    /// </summary>
    private async Task<bool> EvaluateRootLevelAsync(
        List<CaseDefinitionCriteria> rootCriteria,
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName)
    {
        if (rootCriteria.Count == 1)
        {
            // Single root criterion - evaluate it (may be a group or individual)
            return await EvaluateCriterionOrGroupAsync(rootCriteria[0], allCriteria, criterionEvaluator, contextName, 0);
        }

        // Multiple root criteria - evaluate sequentially with GroupExitOperator
        bool accumulated = await EvaluateCriterionOrGroupAsync(rootCriteria[0], allCriteria, criterionEvaluator, contextName, 0);

        _logger.LogDebug("[TREE EVAL {Context}] Root[0] result: {Result}", contextName, accumulated);

        for (int i = 1; i < rootCriteria.Count; i++)
        {
            var previousCriterion = rootCriteria[i - 1];
            var currentResult = await EvaluateCriterionOrGroupAsync(rootCriteria[i], allCriteria, criterionEvaluator, contextName, 0);

            // Use GroupExitOperator if available (for groups), otherwise LogicalOperator
            var operatorToUse = previousCriterion.GroupExitOperator ?? previousCriterion.LogicalOperator;

            _logger.LogDebug(
                "[TREE EVAL {Context}] Combining Root[{Prev}] with Root[{Curr}] using {Operator} (GroupExit={GE}, Logical={L})",
                contextName, i - 1, i, operatorToUse,
                previousCriterion.GroupExitOperator?.ToString() ?? "null",
                previousCriterion.LogicalOperator);

            accumulated = operatorToUse switch
            {
                LogicalOperator.AND => accumulated && currentResult,
                LogicalOperator.OR => accumulated || currentResult,
                LogicalOperator.NOT => accumulated && !currentResult,
                _ => accumulated && currentResult // Default to AND
            };

            _logger.LogDebug("[TREE EVAL {Context}] Accumulated result after Root[{Index}]: {Result}", contextName, i, accumulated);

            // Short-circuit optimization
            if (!accumulated && operatorToUse == LogicalOperator.AND)
            {
                _logger.LogInformation("[TREE EVAL {Context}] Short-circuit: AND failed at root index {Index}", contextName, i);
                return false;
            }
            if (accumulated && operatorToUse == LogicalOperator.OR)
            {
                _logger.LogInformation("[TREE EVAL {Context}] Short-circuit: OR succeeded at root index {Index}", contextName, i);
                return true;
            }
        }

        _logger.LogInformation("[TREE EVAL {Context}] Final root-level result: {Result}", contextName, accumulated);
        return accumulated;
    }

    /// <summary>
    /// Evaluates a single criterion or group (with children).
    /// </summary>
    private async Task<bool> EvaluateCriterionOrGroupAsync(
        CaseDefinitionCriteria criterion,
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName,
        int depth)
    {
        var indent = new string(' ', depth * 2);
        var children = allCriteria
            .Where(c => c.ParentCriteriaId == criterion.Id)
            .OrderBy(c => c.DisplayOrder)
            .ToList();

        var hasChildren = children.Any();
        var hasType = criterion.CriterionType != 0 && criterion.CriterionType != null;

        if (!hasChildren && !hasType)
        {
            // Empty container - treat as false
            _logger.LogWarning("{Indent}[TREE EVAL {Context}] Criterion {Id} has no type and no children - treating as false", indent, contextName, criterion.Id);
            return false;
        }

        if (!hasChildren)
        {
            // Leaf criterion - evaluate it directly
            var result = await criterionEvaluator(criterion);
            _logger.LogDebug("{Indent}[TREE EVAL {Context}] Criterion {Id} ({Type}): {Result}", indent, contextName, criterion.Id, criterion.CriterionType, result);
            return result;
        }

        // Has children - evaluate as a group
        _logger.LogInformation("{Indent}[TREE EVAL {Context}] Evaluating group: Criterion {Id} with {Count} children", indent, contextName, criterion.Id, children.Count);

        // Evaluate group children with internal LogicalOperator
        var groupResult = await EvaluateGroupChildrenAsync(children, allCriteria, criterionEvaluator, contextName, depth + 1);

        if (!hasType)
        {
            // Pure group container - return group result
            _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group {Id} (container): {Result}", indent, contextName, criterion.Id, groupResult);
            return groupResult;
        }

        // Dual-purpose: evaluate both the criterion and its children
        var criterionResult = await criterionEvaluator(criterion);
        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Criterion {Id} self: {Result}, children: {GroupResult}", indent, contextName, criterion.Id, criterionResult, groupResult);

        // Combine using the criterion's LogicalOperator
        var combined = criterion.LogicalOperator switch
        {
            LogicalOperator.AND => criterionResult && groupResult,
            LogicalOperator.OR => criterionResult || groupResult,
            LogicalOperator.NOT => criterionResult && !groupResult,
            _ => criterionResult && groupResult // Default to AND
        };

        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Dual-purpose Criterion {Id} combined with {Operator}: {Result}", indent, contextName, criterion.Id, criterion.LogicalOperator, combined);
        return combined;
    }

    /// <summary>
    /// Evaluates children of a group using the group's internal LogicalOperator.
    /// All children at this level should have the same operator.
    /// </summary>
    private async Task<bool> EvaluateGroupChildrenAsync(
        List<CaseDefinitionCriteria> children,
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName,
        int depth)
    {
        if (!children.Any())
            return false;

        var indent = new string(' ', depth * 2);

        // Determine the internal operator for this group
        // All children should have the same LogicalOperator (this is the group's internal logic)
        var internalOperator = children.First().LogicalOperator;

        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group internal operator: {Operator}", indent, contextName, internalOperator);

        // Evaluate all children
        var results = new List<bool>();
        for (int i = 0; i < children.Count; i++)
        {
            var result = await EvaluateCriterionOrGroupAsync(children[i], allCriteria, criterionEvaluator, contextName, depth);
            results.Add(result);

            _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group child [{Index}] (Criterion {Id}): {Result}", indent, contextName, i, children[i].Id, result);
        }

        // Combine results using internal operator
        var groupResult = internalOperator switch
        {
            LogicalOperator.AND => results.All(r => r),
            LogicalOperator.OR => results.Any(r => r),
            LogicalOperator.NOT => !results.Any(r => r),
            _ => results.All(r => r) // Default to AND
        };

        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group result ({Operator}): {Result} (Children: [{Results}])", indent, contextName, internalOperator, groupResult, string.Join(", ", results.Select(r => r ? "T" : "F")));
        return groupResult;
    }
}
