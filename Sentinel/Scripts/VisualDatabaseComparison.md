# Visual Comparison: Database vs Expected Structure

## Case Definition 11 (ACTIVE) - What the Database Actually Contains

```
DATABASE STRUCTURE:
═══════════════════════════════════════════════════════════════════

[19] ROOT ────────────────┐
│ Orion Marker A         │
│ LogicalOperator: AND   │ ◄── Parent's operator (NOT USED by evaluator)
│ GroupExitOperator: NULL│ ◄── ❌ Cannot connect to next root!
└────────────────────────┘
         │
         │ (parent-child relationship)
         ▼
[20] CHILD ───────────────┐
│ Orion Marker B         │
│ LogicalOperator: OR    │ ◄── ❌ EVALUATOR USES THIS for group logic!
│ GroupExitOperator: NULL│
└────────────────────────┘

❌ NO MARKER C EXISTS

EVALUATOR INTERPRETATION:
  children.First().LogicalOperator = OR (from [20])
  Result: A OR B
  Missing: No second root for C
```

---

## Case Definition 10 (INACTIVE) - Has Marker C but Wrong Operator

```
DATABASE STRUCTURE:
═══════════════════════════════════════════════════════════════════

[16] ROOT ────────────────┐
│ Orion Marker A         │
│ LogicalOperator: AND   │ ◄── Parent's operator (NOT USED by evaluator)
│ GroupExitOperator: OR  │ ◄── ✅ Connects to next root with OR
└────────────────────────┘
         │
         │ (parent-child relationship)
         ▼
[20] ROOT ────────────────┐
│ Orion Marker C         │
│ LogicalOperator: OR    │
│ GroupExitOperator: NULL│
└────────────────────────┘
         ┌────────────────────────┐
         │                        │
         ▼                        ▼
[17] CHILD ──────────────┐
│ Orion Marker B        │
│ LogicalOperator: OR   │ ◄── ❌ EVALUATOR USES THIS for group logic!
│ GroupExitOperator: NULL│
└───────────────────────┘

EVALUATOR INTERPRETATION:
  Root[0] = [16] with children → Group
    children.First().LogicalOperator = OR (from [17])
    Group result: A OR B ❌
  Root[1] = [21]
  Combine: (A OR B) OR C
  Not equivalent to: (A AND B) OR C
```

---

## What You INTENDED (Expected Structure)

```
INTENDED LOGIC: (A AND B) OR C

CORRECT DATABASE STRUCTURE:
═══════════════════════════════════════════════════════════════════

[X] ROOT ────────────────┐
│ Orion Marker A         │
│ LogicalOperator: AND   │ ◄── Parent's operator (NOT USED)
│ GroupExitOperator: OR  │ ◄── ✅ Connects to Marker C with OR
└────────────────────────┘
         │
         │ (parent-child relationship)
         ▼
[Y] CHILD ───────────────┐
│ Orion Marker B         │
│ LogicalOperator: AND   │ ◄── ✅ EVALUATOR USES THIS = AND!
│ GroupExitOperator: NULL│
└────────────────────────┘

[Z] ROOT ────────────────┐
│ Orion Marker C         │
│ LogicalOperator: AND   │
│ GroupExitOperator: NULL│
└────────────────────────┘

EVALUATOR INTERPRETATION:
  Root[0] = [X] with children → Group
    children.First().LogicalOperator = AND (from [Y])
    Group result: A AND B ✅
  Root[1] = [Z]
  Combine using Root[0].GroupExitOperator = OR
  Final: (A AND B) OR C ✅
```

---

## Side-by-Side Comparison

| Element | Case Def 11 (Active) | Case Def 10 (Inactive) | Expected |
|---------|---------------------|------------------------|----------|
| **Criterion Count** | 2 | 3 | 3 |
| **Has Marker A** | ✅ [19] ROOT | ✅ [16] ROOT | ✅ ROOT |
| **Has Marker B** | ✅ [20] CHILD | ✅ [17] CHILD | ✅ CHILD |
| **Has Marker C** | ❌ Missing | ✅ [21] ROOT | ✅ ROOT |
| **Marker A GroupExitOperator** | ❌ NULL | ✅ OR | ✅ OR |
| **Marker B LogicalOperator** | ❌ OR | ❌ OR | ✅ AND |
| **Evaluates To** | `A OR B` | `(A OR B) OR C` | `(A AND B) OR C` |
| **Matches Intended Logic** | ❌ No | ❌ No | ✅ Yes |

---

## The Core Bug

### In `TreeBasedCriteriaEvaluator.cs` Line 200:
```csharp
var internalOperator = children.First().LogicalOperator;
```

This line causes the evaluator to use the **first child's LogicalOperator** for the group's internal logic, but:

1. **BuildCriteria.cshtml** sets the child's `LogicalOperator` based on the toggle BETWEEN criteria
2. **Review.cshtml** displays operators based on different logic (first child uses parent's operator)
3. **The evaluator** uses the first child's operator for internal group logic

### This Creates a Three-Way Mismatch:
- **Builder UI**: Saves child `LogicalOperator` as what appears in the toggle
- **Review UI**: Displays parent's operator for first child
- **Evaluator**: Uses first child's `LogicalOperator` for group logic

---

## SQL to See Exactly What's in Your Database

Run these queries to see the raw data:

```sql
-- Case Definition 11
SELECT 
    Id,
    CASE WHEN ParentCriteriaId IS NULL THEN 'ROOT' ELSE 'CHILD' END AS Level,
    ParentCriteriaId,
    DisplayOrder,
    CASE LogicalOperator WHEN 1 THEN 'AND' WHEN 2 THEN 'OR' ELSE CAST(LogicalOperator AS VARCHAR) END AS LogicOp,
    CASE WHEN GroupExitOperator IS NULL THEN 'NULL' 
         WHEN GroupExitOperator = 1 THEN 'AND' 
         WHEN GroupExitOperator = 2 THEN 'OR' 
         ELSE CAST(GroupExitOperator AS VARCHAR) END AS ExitOp,
    LEFT(DisplayText, 50) AS DisplayText
FROM CaseDefinitionCriteria
WHERE CaseDefinitionId = 11
ORDER BY ISNULL(ParentCriteriaId, 0), DisplayOrder;

-- Case Definition 10
SELECT 
    Id,
    CASE WHEN ParentCriteriaId IS NULL THEN 'ROOT' ELSE 'CHILD' END AS Level,
    ParentCriteriaId,
    DisplayOrder,
    CASE LogicalOperator WHEN 1 THEN 'AND' WHEN 2 THEN 'OR' ELSE CAST(LogicalOperator AS VARCHAR) END AS LogicOp,
    CASE WHEN GroupExitOperator IS NULL THEN 'NULL' 
         WHEN GroupExitOperator = 1 THEN 'AND' 
         WHEN GroupExitOperator = 2 THEN 'OR' 
         ELSE CAST(GroupExitOperator AS VARCHAR) END AS ExitOp,
    LEFT(DisplayText, 50) AS DisplayText
FROM CaseDefinitionCriteria
WHERE CaseDefinitionId = 10
ORDER BY ISNULL(ParentCriteriaId, 0), DisplayOrder;
```

---

## Recommended Fix SQL

```sql
-- OPTION A: Fix Case Definition 11 (currently active)

-- Step 1: Fix child's operator to AND (so group evaluates as A AND B)
UPDATE CaseDefinitionCriteria 
SET LogicalOperator = 1  -- AND
WHERE Id = 20;

-- Step 2: Add GroupExitOperator to parent (to connect with OR to next root)
UPDATE CaseDefinitionCriteria 
SET GroupExitOperator = 2  -- OR
WHERE Id = 19;

-- Step 3: Add Marker C as new root criterion
INSERT INTO CaseDefinitionCriteria (
    CaseDefinitionId, ParentCriteriaId, CriterionType, LogicalOperator, 
    GroupExitOperator, DisplayOrder, DisplayText, AcceptablePathogensJson, 
    AcceptableSpecimenTypesJson, AcceptableResultsJson, CreatedAt
)
VALUES (
    11, NULL, 2, 1, NULL, 2,
    '<strong>Laboratory:</strong> Orion Marker C detected in Stool, result: Detected',
    '["3b16e496-9c93-4f8f-9aae-01427fb1d932"]',
    '[11]', '["Detected"]', GETUTCDATE()
);

-- Verify the fix
SELECT 
    Id, ParentCriteriaId, DisplayOrder,
    CASE LogicalOperator WHEN 1 THEN 'AND' WHEN 2 THEN 'OR' END AS LogicOp,
    CASE WHEN GroupExitOperator IS NULL THEN 'NULL'
         WHEN GroupExitOperator = 1 THEN 'AND' 
         WHEN GroupExitOperator = 2 THEN 'OR' END AS ExitOp,
    LEFT(DisplayText, 60) AS DisplayText
FROM CaseDefinitionCriteria
WHERE CaseDefinitionId = 11
ORDER BY ISNULL(ParentCriteriaId, 0), DisplayOrder;
```

After running this fix, Case Definition 11 will have:
```
[19] ROOT (LogicOp=AND, ExitOp=OR) Marker A
  └─ [20] CHILD (LogicOp=AND) Marker B
[NEW] ROOT (LogicOp=AND, ExitOp=NULL) Marker C

Evaluates to: (A AND B) OR C ✅
```

