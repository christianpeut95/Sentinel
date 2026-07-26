using Microsoft.Extensions.Logging;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Services.HL7;

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
    /// <param name="markerTracker">Optional: Dictionary to track which marker IDs satisfied which criterion IDs</param>
    public async Task<bool> EvaluateAsync(
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName = "",
        Dictionary<int, List<Guid>>? markerTracker = null)
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
        return await EvaluateRootLevelAsync(rootCriteria, allCriteria, criterionEvaluator, contextName, markerTracker);
    }

    /// <summary>
    /// Evaluates root-level criteria, respecting GroupExitOperator to combine group results with following criteria.
    /// </summary>
    private async Task<bool> EvaluateRootLevelAsync(
        List<CaseDefinitionCriteria> rootCriteria,
        List<CaseDefinitionCriteria> allCriteria,
        Func<CaseDefinitionCriteria, Task<bool>> criterionEvaluator,
        string contextName,
        Dictionary<int, List<Guid>>? markerTracker)
    {
        if (rootCriteria.Count == 1)
        {
            // Single root criterion - evaluate it (may be a group or individual)
            return await EvaluateCriterionOrGroupAsync(rootCriteria[0], allCriteria, criterionEvaluator, contextName, 0, markerTracker);
        }

        // Multiple root criteria - evaluate sequentially with GroupExitOperator
        var rootSnapshots = new List<(int rootIndex, HashSet<int> keysBeforeEval, bool result)>();
        var snapshotKeys = markerTracker?.Keys.ToHashSet() ?? new HashSet<int>();
        bool accumulated = await EvaluateCriterionOrGroupAsync(rootCriteria[0], allCriteria, criterionEvaluator, contextName, 0, markerTracker);

        rootSnapshots.Add((0, snapshotKeys, accumulated));
        _logger.LogDebug("[TREE EVAL {Context}] Root[0] result: {Result}", contextName, accumulated);

        // If first root fails and we're in AND mode, clean up markers
        if (!accumulated)
        {
            var keysAdded = markerTracker?.Keys.Except(snapshotKeys).ToList() ?? new List<int>();
            if (markerTracker != null)
            {
                foreach (var key in keysAdded)
                {
                    markerTracker.Remove(key);
                }
            }
        }

        for (int i = 1; i < rootCriteria.Count; i++)
        {
            var previousCriterion = rootCriteria[i - 1];

            // Use GroupExitOperator if available (for groups), otherwise LogicalOperator
            var operatorToUse = previousCriterion.GroupExitOperator ?? previousCriterion.LogicalOperator;

            // Snapshot before evaluating current branch
            snapshotKeys = markerTracker?.Keys.ToHashSet() ?? new HashSet<int>();
            var currentResult = await EvaluateCriterionOrGroupAsync(rootCriteria[i], allCriteria, criterionEvaluator, contextName, 0, markerTracker);

            rootSnapshots.Add((i, snapshotKeys, currentResult));

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

            // Clean up markers from failed branches
            if (!currentResult && markerTracker != null)
            {
                var keysAdded = markerTracker.Keys.Except(snapshotKeys).ToList();
                foreach (var key in keysAdded)
                {
                    markerTracker.Remove(key);
                    _logger.LogDebug("[TREE EVAL {Context}] Removing marker for criterion {CritId} (failed root branch)", contextName, key);
                }
            }

            // Short-circuit optimization
            if (!accumulated && operatorToUse == LogicalOperator.AND)
            {
                _logger.LogInformation("[TREE EVAL {Context}] Short-circuit: AND failed at root index {Index}", contextName, i);
                return false;
            }
            if (accumulated && operatorToUse == LogicalOperator.OR)
            {
                // OR succeeded - clean up markers from ALL previous failed roots
                if (markerTracker != null && !rootSnapshots[0].result)
                {
                    // Root[0] failed but current root succeeded
                    // Keep only markers added by current root, remove markers from Root[0]
                    var keysAddedByCurrentRoot = markerTracker.Keys.Except(snapshotKeys).ToHashSet();
                    var keysToRemove = markerTracker.Keys
                        .Where(k => !rootSnapshots[0].keysBeforeEval.Contains(k) && !keysAddedByCurrentRoot.Contains(k))
                        .ToList();

                    foreach (var key in keysToRemove)
                    {
                        markerTracker.Remove(key);
                        _logger.LogWarning("[TREE EVAL {Context}] 🧹 OR cleanup: Removed criterion {CritId} from failed Root[0]", contextName, key);
                    }

                    if (keysToRemove.Any())
                    {
                        _logger.LogWarning("[TREE EVAL {Context}] 🧹 Cleaned {Count} marker(s) from failed Root[0], kept {Kept} from Root[{Index}]", 
                            contextName, keysToRemove.Count, keysAddedByCurrentRoot.Count, i);
                    }
                }

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
        int depth,
        Dictionary<int, List<Guid>>? markerTracker)
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
        var groupResult = await EvaluateGroupChildrenAsync(children, allCriteria, criterionEvaluator, contextName, depth + 1, markerTracker);

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
        int depth,
        Dictionary<int, List<Guid>>? markerTracker)
    {
        if (!children.Any())
            return false;

        var indent = new string(' ', depth * 2);

        // Determine the internal operator for this group
        // All children should have the same LogicalOperator (this is the group's internal logic)
        var internalOperator = children.First().LogicalOperator;

        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group internal operator: {Operator}", indent, contextName, internalOperator);

        // Evaluate all children, rolling back marker tracker on failed branches
        var results = new List<bool>();
        var succeededIndices = new List<int>();

        for (int i = 0; i < children.Count; i++)
        {
            // Snapshot marker tracker before evaluating this branch
            var snapshotKeys = markerTracker?.Keys.ToHashSet() ?? new HashSet<int>();

            var result = await EvaluateCriterionOrGroupAsync(children[i], allCriteria, criterionEvaluator, contextName, depth, markerTracker);
            results.Add(result);

            _logger.LogDebug("{Indent}[TREE EVAL {Context}] Group child [{Index}] (Criterion {Id}): {Result}", indent, contextName, i, children[i].Id, result);

            if (result)
            {
                succeededIndices.Add(i);

                // For OR: if we succeeded, we can short-circuit and remove markers from previous failed branches
                if (internalOperator == LogicalOperator.OR && markerTracker != null)
                {
                    // Keep only markers from successful branch (remove any from previous failed attempts)
                    var keysToRemove = markerTracker.Keys.Except(snapshotKeys).ToList();
                    var currentBranchKeys = new HashSet<int>(keysToRemove);

                    // Remove markers from all previously failed branches
                    foreach (var key in markerTracker.Keys.Where(k => !currentBranchKeys.Contains(k) && !snapshotKeys.Contains(k)).ToList())
                    {
                        markerTracker.Remove(key);
                    }

                    _logger.LogDebug("{Indent}[TREE EVAL {Context}] OR short-circuit: keeping {Count} markers from successful branch", indent, contextName, currentBranchKeys.Count);
                    break; // Short-circuit on OR success
                }
            }
            else
            {
                // Branch failed - remove any markers it added
                if (markerTracker != null)
                {
                    var keysAdded = markerTracker.Keys.Except(snapshotKeys).ToList();
                    foreach (var key in keysAdded)
                    {
                        markerTracker.Remove(key);
                        _logger.LogDebug("{Indent}[TREE EVAL {Context}] Removing marker for criterion {CritId} (failed branch)", indent, contextName, key);
                    }
                }
            }
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
