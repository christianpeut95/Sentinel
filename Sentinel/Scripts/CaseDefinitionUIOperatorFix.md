# Case Definition UI Operator Display Fix

## Problem Summary

The case definition UI displayed logical operators **inconsistently** across three different pages (BuildCriteria, Review, and Edit), causing confusion about which operators connect which criteria. Additionally, BuildCriteria showed **duplicate** operators after groups.

## Issues Fixed

### Issue #1: Review.cshtml - Wrong operator source between root criteria
**Problem**: Showed CURRENT criterion's operator instead of PREVIOUS criterion's operator.

### Issue #2: Edit.cshtml - Wrong operator source and missing first child operator
**Problems**: 
- Showed CURRENT criterion's operator instead of PREVIOUS criterion's between root criteria
- Did not show any operator before the first child in a group
- Showed child's own operator instead of previous child's operator

### Issue #3: BuildCriteria.cshtml - Duplicate operator display after groups (Issue #12)
**Problem**: After a group with a group exit operator, showed the operator **twice** when followed by a non-grouped criterion.

### Issue #4: BuildCriteria.cshtml - Incorrect operator when group follows group
**Problem**: When a group followed another group, the operator shown used `LogicalOperator` instead of the previous group's `GroupExitOperator`.

## Root Cause

The pages used different logic to determine which operator to display and didn't properly handle `GroupExitOperator` transitions.

### Before Fix

| Page | Between Root Criteria | First Child in Group | Subsequent Children |
|------|----------------------|---------------------|---------------------|
| **BuildCriteria.cshtml** ✅ | Previous criterion's `LogicalOperator` | Parent's `LogicalOperator` | Previous child's `LogicalOperator` |
| **Review.cshtml** ❌ | **CURRENT** criterion's `GroupExitOperator/LogicalOperator` | Parent's `LogicalOperator` | Child's own `LogicalOperator` |
| **Edit.cshtml** ❌ | **CURRENT** criterion's `LogicalOperator` | **Not shown** | Child's own `LogicalOperator` |

The **semantic rule** should be:
- An operator displayed **before** a criterion should show how it connects **from the previous** criterion/group.
- For groups, `GroupExitOperator` (if set) overrides `LogicalOperator` when exiting the group.

## Solution

### Fixed Pages

#### Review.cshtml (Lines 305-327)
**Before:**
```razor
@if (!isFirst)
{
    @* For groups, use GroupExitOperator if available, otherwise fall back to LogicalOperator *@
    @if (hasChildren && criterion.GroupExitOperator.HasValue)
    {
        @(criterion.GroupExitOperator == LogicalOperator.AND ? "AND" : "OR")
    }
    else
    {
        @(criterion.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")
    }
}
```

**After:**
```razor
var previousCriterion = !isFirst ? rootCriteriaList[i - 1] : null;

@if (!isFirst)
{
    @* Show the operator that connects from PREVIOUS to CURRENT criterion *@
    var previousHasChildren = previousCriterion?.ChildCriteria?.Any() == true;
    var operatorToShow = previousHasChildren && previousCriterion.GroupExitOperator.HasValue
        ? previousCriterion.GroupExitOperator.Value
        : previousCriterion.LogicalOperator;

    @(operatorToShow == LogicalOperator.AND ? "AND" : "OR")
}
```

#### Edit.cshtml (Lines 357-377, 420-442)
**Before (Between Root Criteria):**
```razor
@if (!isFirst)
{
    <div class="logical-operator-toggle" style="cursor: default;">
        <span class="operator-label">@(criterion.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")</span>
    </div>
}
```

**After:**
```razor
var previousCriterion = !isFirst ? rootCriteria[i - 1] : null;

@if (!isFirst)
{
    var previousHasChildren = previousCriterion?.ChildCriteria?.Any() == true;
    var operatorToShow = previousHasChildren && previousCriterion.GroupExitOperator.HasValue
        ? previousCriterion.GroupExitOperator.Value
        : previousCriterion.LogicalOperator;

    <span class="operator-label">@(operatorToShow == LogicalOperator.AND ? "AND" : "OR")</span>
}
```

**Before (Within Groups):**
```razor
@if (j > 0)  // Only showed for subsequent children
{
    <span class="operator-label">@(child.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")</span>
}
```

**After:**
```razor
var childIsFirst = j == 0;
var previousChild = !childIsFirst ? children[j - 1] : null;

// Always show operator before each child
@if (childIsFirst)
{
    <span class="operator-label">@(criterion.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")</span>
}
else
{
    <span class="operator-label">@(previousChild.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")</span>
}
```

## After Fix

| Page | Between Root Criteria | First Child in Group | Subsequent Children |
|------|----------------------|---------------------|---------------------|
| **BuildCriteria.cshtml** ✅ | Previous criterion's `GroupExitOperator ?? LogicalOperator` | Parent's `LogicalOperator` | Previous child's `LogicalOperator` |
| **Review.cshtml** ✅ | Previous criterion's `GroupExitOperator ?? LogicalOperator` | Parent's `LogicalOperator` | Previous child's `LogicalOperator` |
| **Edit.cshtml** ✅ | Previous criterion's `GroupExitOperator ?? LogicalOperator` | Parent's `LogicalOperator` | Previous child's `LogicalOperator` |

All three pages now display operators **consistently** and correctly reflect the tree evaluation semantics used by `TreeBasedCriteriaEvaluator`.

## Impact

✅ Users will now see **consistent** operator display across all case definition pages.  
✅ The displayed operators now **match** the actual evaluation logic in `TreeBasedCriteriaEvaluator`.  
✅ The first child in a group now correctly shows the parent's internal operator.  
✅ Group exit operators (OR/AND after a group) are now correctly displayed when transitioning to the next root criterion.

## Additional Fixes: BuildCriteria Operator Handling

### Fix #1: Duplicate Operator Display (Issue #12)

**Problem**: When a group was followed by a non-grouped criterion, BuildCriteria displayed the operator **twice**:
1. Once as the group exit operator (correct)
2. Again before the next non-grouped criterion (duplicate)

**Example visual bug:**
```
(A AND B)
OR         <- Group exit operator (correct)
C
AND        <- DUPLICATE! (should not show)
D
```

**Root Cause**: Lines 343-356 unconditionally showed an operator before any non-grouped criterion if it wasn't first, without checking if the previous criterion was a group that already displayed its exit operator.

**Fix**: Added a check to **skip** the operator display when previous criterion was a group.

**BuildCriteria.cshtml Lines 343-362 (After Fix):**
```razor
@* Show logical operator toggle between root criteria *@
@* Skip if previous was a group (groups show their own exit operator) *@
@if (!isFirst && !hasChildren)
{
    var previousHasChildren = previousCriterion?.ChildCriteria?.Any() == true;
    @* Only show if previous was NOT a group *@
    @if (!previousHasChildren)
    {
        <div class="logical-operator-toggle" 
             data-criterion-id="@previousCriterion.Id" 
             data-operator="@((int)previousCriterion.LogicalOperator)"
             onclick="toggleLogicalOperator(@previousCriterion.Id, @((int)previousCriterion.LogicalOperator))">
            <span class="operator-label">@(previousCriterion.LogicalOperator == LogicalOperator.AND ? "AND" : "OR")</span>
            <i class="bi bi-arrow-down-up" style="font-size: 0.75rem; margin-left: 0.25rem;"></i>
        </div>
    }
}
```

Now the visual flow correctly shows:
```
(A AND B)
OR         <- Group exit operator (only one, correct!)
C
AND        <- Normal operator between C and D
D
```

### Fix #2: Group-to-Group Operator Toggle

**Problem**: When a group followed another group, the operator shown before the second group used the first group's `LogicalOperator` instead of its `GroupExitOperator`, and clicking it toggled the wrong field.

**Example:**
```
(A AND B)  <- First group has GroupExitOperator = OR
OR         <- Should show this and toggle GroupExitOperator
(C AND D)  <- Second group
```

**Fix**: Updated lines 369-390 to:
1. Check if previous criterion was a group
2. If yes, show and toggle `GroupExitOperator`
3. If no, show and toggle `LogicalOperator`

**BuildCriteria.cshtml Lines 369-390 (After Fix):**
```razor
@* Show operator before group if not first *@
@if (!isFirst)
{
    var previousHasChildren = previousCriterion?.ChildCriteria?.Any() == true;
    var operatorToShow = previousHasChildren && previousCriterion.GroupExitOperator.HasValue
        ? previousCriterion.GroupExitOperator.Value
        : previousCriterion.LogicalOperator;

    <div class="logical-operator-toggle" 
         data-criterion-id="@previousCriterion.Id" 
         data-operator="@((int)operatorToShow)"
         onclick="@(previousHasChildren ? $"toggleGroupExitOperator({previousCriterion.Id}, {(int)operatorToShow})" : $"toggleLogicalOperator({previousCriterion.Id}, {(int)operatorToShow})")"
         style="background: white;">
        <span class="operator-label">@(operatorToShow == LogicalOperator.AND ? "AND" : "OR")</span>
        <i class="bi bi-arrow-down-up"></i>
    </div>
}
```

## Testing Recommendation

1. Create a case definition with multiple root criteria and groups: `(A AND B) OR C AND D`
2. Verify BuildCriteria **does not** show duplicate operators after groups
3. Verify the Edit page shows the same operators as BuildCriteria
4. Verify the Review page matches both Edit and BuildCriteria
5. Verify the `TreeBasedCriteriaEvaluator` evaluates using the same logic
