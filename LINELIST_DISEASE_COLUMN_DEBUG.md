# Line List Disease Column Not Showing Data - Debug & Fix

## Problem
The Disease column in the line list shows no data even though the field is selected.

## Possible Causes

### 1. **Cases Don't Have Disease Assigned**
Most likely cause - the cases in the outbreak may not have a Disease set on the Case record itself.

### 2. **EF Core Include Not Working**
The Disease navigation property might not be loaded despite the Include statement.

### 3. **Data Serialization Issue**
The data might not be serializing correctly from server to client.

## Debugging Steps

### Step 1: Check Browser Console
1. Open the Line List page
2. Open browser Developer Tools (F12)
3. Go to Console tab
4. Look for these debug messages:
   ```
   Line list data loaded: X rows
   Sample row: {caseId: "...", values: {...}}
   Sample values: {"Case.Disease.Name": "...", ...}
   ```

### Step 2: Check if Disease Data Exists
Look at the console output for `Sample values`. Check if `"Case.Disease.Name"` key exists and what its value is:
- **If key doesn't exist**: Issue with field extraction
- **If value is `null`**: Cases don't have Disease assigned
- **If value exists but not showing**: Grid rendering issue

### Step 3: Check Network Request
1. Open Developer Tools ? Network tab
2. Filter by "data"
3. Click on the POST request to `/api/LineList/data`
4. Click "Response" tab
5. Inspect the returned JSON - look for Disease values

## Quick Fixes

### Fix 1: Ensure Cases Have Disease Assigned

Check your database:
```sql
-- Check which cases don't have a disease
SELECT c.Id, c.FriendlyId, oc.OutbreakId
FROM Cases c
INNER JOIN OutbreakCases oc ON c.Id = oc.CaseId
WHERE oc.IsActive = 1 
  AND c.DiseaseId IS NULL;
```

If cases are missing Disease:
```sql
-- Option 1: Set to Outbreak's primary disease
UPDATE c
SET c.DiseaseId = o.PrimaryDiseaseId
FROM Cases c
INNER JOIN OutbreakCases oc ON c.Id = oc.CaseId
INNER JOIN Outbreaks o ON oc.OutbreakId = o.Id
WHERE oc.IsActive = 1 
  AND c.DiseaseId IS NULL
  AND o.PrimaryDiseaseId IS NOT NULL;
```

### Fix 2: Add Null Handling in Service

The service already has null-safe access, but let's add a fallback to use the Outbreak's primary disease:

```csharp
// In LineListService.cs - ExtractFieldValue method
"Disease.Name" => caseData.Disease?.Name ?? 
                  outbreakCase.Outbreak?.PrimaryDisease?.Name ?? 
                  "(No Disease)",
```

### Fix 3: Add Additional Includes

Ensure the Outbreak's PrimaryDisease is loaded:

```csharp
.Include(oc => oc.Outbreak)
    .ThenInclude(o => o.PrimaryDisease)
```

## Implementation of Fixes

### Apply Fix 2 (Fallback Logic)
