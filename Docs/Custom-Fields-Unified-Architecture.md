# Custom Fields - Unified Architecture

## Overview
Implemented a unified custom fields system that allows the same field definitions to be used for both Patient and Case entities, eliminating duplication and allowing better reusability.

## Architecture Changes

### 1. Unified CustomFieldDefinition Model
**File:** `Models\CustomFieldDefinition.cs`

**Key Changes:**
- Added `ShowOnPatientForm` (bool) - indicates if field appears on Patient forms
- Added `ShowOnCaseForm` (bool) - indicates if field appears on Case forms  
- Changed `ShowOnPatientList` ? `ShowOnList` (generic, applies to both contexts)
- Added navigation property: `ICollection<DiseaseCustomField> DiseaseCustomFields`

**Benefits:**
- Single source of truth for field definitions
- Fields can be used on Patients only, Cases only, or both
- Shared LookupTables for dropdown values

### 2. New DiseaseCustomField Junction Table
**File:** `Models\DiseaseCustomField.cs`

**Purpose:** Links diseases to custom field definitions with inheritance support

**Properties:**
- `DiseaseId` - Which disease this field applies to
- `CustomFieldDefinitionId` - Which field definition to use
- `InheritToChildDiseases` (bool) - If true, child diseases inherit this field

**Example Use Cases:**
- **Disease-specific field:** Link "CD4 Count" to HIV disease only
- **Category-level field:** Link "Outbreak Related" to root Infectious Diseases with inheritance
- **Multiple diseases:** Link "Vaccination Status" to Measles, COVID-19, Hepatitis B

### 3. Case Model Updates
**File:** `Models\Case.cs`

**Added Navigation Properties:**
```csharp
public ICollection<CaseCustomFieldString> CustomFieldStrings { get; set; }
public ICollection<CaseCustomFieldNumber> CustomFieldNumbers { get; set; }
public ICollection<CaseCustomFieldDate> CustomFieldDates { get; set; }
public ICollection<CaseCustomFieldBoolean> CustomFieldBooleans { get; set; }
public ICollection<CaseCustomFieldLookup> CustomFieldLookups { get; set; }
```

### 4. Disease Model Updates
**File:** `Models\Lookups\Disease.cs`

**Added Navigation Property:**
```csharp
public ICollection<DiseaseCustomField> DiseaseCustomFields { get; set; }
```

### 5. Updated CaseCustomField* Models
**Files:**
- `Models\CaseCustomFieldString.cs`
- `Models\CaseCustomFieldNumber.cs`
- `Models\CaseCustomFieldDate.cs`
- `Models\CaseCustomFieldBoolean.cs`
- `Models\CaseCustomFieldLookup.cs`

**Changed:**
- Reference `CustomFieldDefinition` instead of `DiseaseCustomFieldDefinition`
- Now shares the same field definitions as Patient custom fields

### 6. Removed DiseaseCustomFieldDefinition
**Deleted:** `Models\DiseaseCustomFieldDefinition.cs`

**Reason:** Replaced by combination of `CustomFieldDefinition` + `DiseaseCustomField` junction table

### 7. Database Context Updates
**File:** `Data\ApplicationDbContext.cs`

**Changes:**
- Replaced `DbSet<DiseaseCustomFieldDefinition>` with `DbSet<DiseaseCustomField>`
- Added indexes for DiseaseCustomField junction table
- Added indexes for CaseCustomField* value tables
- Added relationship configurations for junction table

## How It Works

### For Patient Forms:
```csharp
var patientFields = await _context.CustomFieldDefinitions
    .Where(f => f.ShowOnPatientForm && f.IsActive)
    .OrderBy(f => f.Category)
    .ThenBy(f => f.DisplayOrder)
    .ToListAsync();
```

### For Case Forms:
```csharp
// Get fields for this case's disease (including inherited)
var caseFields = await GetEffectiveFieldsForDisease(caseItem.DiseaseId);

// Helper method to get fields including inherited ones
private async Task<List<CustomFieldDefinition>> GetEffectiveFieldsForDisease(Guid diseaseId)
{
    var disease = await _context.Diseases
        .Include(d => d.DiseaseCustomFields)
            .ThenInclude(dcf => dcf.CustomFieldDefinition)
        .FirstOrDefaultAsync(d => d.Id == diseaseId);
    
    // Get direct fields
    var fields = disease.DiseaseCustomFields
        .Select(dcf => dcf.CustomFieldDefinition)
        .ToList();
    
    // Get inherited fields from parent diseases
    var parentIds = disease.PathIds.Split('|', StringSplitOptions.RemoveEmptyEntries)
        .Select(Guid.Parse)
        .Where(id => id != diseaseId);
    
    var inheritedFields = await _context.DiseaseCustomFields
        .Where(dcf => parentIds.Contains(dcf.DiseaseId) 
            && dcf.InheritToChildDiseases)
        .Select(dcf => dcf.CustomFieldDefinition)
        .ToListAsync();
    
    return fields.Union(inheritedFields).Distinct().ToList();
}
```

## Migration Applied
**Name:** `UnifyCustomFieldsForPatientAndCase`

**What it does:**
1. Adds `ShowOnPatientForm` and `ShowOnCaseForm` columns to `CustomFieldDefinitions`
2. Renames `ShowOnPatientList` to `ShowOnList`
3. Creates `DiseaseCustomFields` junction table
4. Drops `DiseaseCustomFieldDefinitions` table (if exists)
5. Updates foreign keys in `CaseCustomField*` tables to reference `CustomFieldDefinitions`

## UI Updates Needed (Next Steps)

### 1. Custom Fields Management
**Location:** `Pages/Settings/CustomFields/`

**Add to Create/Edit forms:**
- Checkbox: "Show on Patient Form"
- Checkbox: "Show on Case Form"
- Both can be selected simultaneously

### 2. Disease Settings - Custom Fields Tab
**Location:** `Pages/Settings/Diseases/Edit.cshtml`

**New Section:**
```
Custom Fields for [Disease Name] Cases
????????????????????????????????????
Available Fields:              Selected Fields:
? CD4 Count                   ? Vaccination Status
? Outbreak Related        >>> ? Travel History
? Vaccination Status      <<< 
? Travel History              

[Save] [Cancel]
```

**Features:**
- Multi-select list showing all fields where `ShowOnCaseForm = true`
- Ability to link/unlink fields to disease
- Checkbox per field: "Inherit to child diseases"

### 3. Case Create/Edit Pages
**Location:** `Pages/Cases/Create.cshtml`, `Pages/Cases/Edit.cshtml`

**Changes:**
- Load effective custom fields for selected disease
- Display custom fields grouped by category
- Save values to appropriate CaseCustomField* tables

### 4. Case Details Page
**Location:** `Pages/Cases/Details.cshtml`

**Changes:**
- Load and display custom field values
- Show fields grouped by category

## Benefits of This Architecture

? **No Duplication:** Define "Vaccination Status" once, use everywhere
? **Shared Lookups:** Dropdown values reused across Patient and Case
? **Flexible:** Fields can be Patient-only, Case-only, or both
? **Inheritance:** Parent disease fields can cascade to children
? **Type-Safe:** Separate value tables maintain strong typing
? **Performant:** Proper indexes on junction and value tables

## Example Scenarios

### Scenario 1: Patient-Only Field
```
Field: "Primary Language"
  ShowOnPatientForm: true
  ShowOnCaseForm: false
  ? Appears on Patient forms only
```

### Scenario 2: Case-Only, Disease-Specific
```
Field: "CD4 Count"
  ShowOnPatientForm: false
  ShowOnCaseForm: true
  
DiseaseCustomField:
  DiseaseId: [HIV]
  InheritToChildDiseases: true
  ? Appears on HIV cases and sub-types only
```

### Scenario 3: Both Patient and Case
```
Field: "Vaccination Status"
  ShowOnPatientForm: true
  ShowOnCaseForm: true
  LookupTableId: [Vaccination Statuses]
  
DiseaseCustomField:
  ??? Measles
  ??? COVID-19
  ??? Hepatitis B
  ? Appears on Patient form + specific disease cases
```

### Scenario 4: Inherited Field
```
Field: "Outbreak Related"
  ShowOnPatientForm: false
  ShowOnCaseForm: true
  
DiseaseCustomField:
  DiseaseId: [Infectious Diseases root]
  InheritToChildDiseases: true
  ? Appears on all infectious disease cases
```

## Database Schema

```
CustomFieldDefinitions
??? Id (PK)
??? Name, Label, Category
??? FieldType, IsRequired, IsSearchable
??? ShowOnPatientForm ? NEW
??? ShowOnCaseForm ? NEW
??? ShowOnList ? RENAMED from ShowOnPatientList
??? DisplayOrder, IsActive
??? LookupTableId (FK)

DiseaseCustomFields (Junction Table) ? NEW
??? Id (PK)
??? DiseaseId (FK ? Diseases)
??? CustomFieldDefinitionId (FK ? CustomFieldDefinitions)
??? InheritToChildDiseases
??? Unique Index: (DiseaseId, CustomFieldDefinitionId)

CaseCustomFieldString (and other types)
??? Id (PK)
??? CaseId (FK ? Cases)
??? FieldDefinitionId (FK ? CustomFieldDefinitions) ? CHANGED
??? Value
??? Unique Index: (CaseId, FieldDefinitionId)
```

## Next Implementation Priority

1. **Update Custom Fields Create/Edit pages** - Add ShowOnPatientForm/ShowOnCaseForm checkboxes
2. **Create Disease Custom Fields management UI** - Tab on Disease Edit page
3. **Implement GetEffectiveFieldsForDisease helper** - Include inheritance logic
4. **Update Case Create/Edit pages** - Load and save custom field values
5. **Update Case Details page** - Display custom field values

## Migration Notes

To apply the migration:
```bash
cd Surveillance-MVP
dotnet ef database update
```

**Important:** Existing Patient custom field data is preserved. The `ShowOnPatientForm` flag will default to `true` for existing records.
