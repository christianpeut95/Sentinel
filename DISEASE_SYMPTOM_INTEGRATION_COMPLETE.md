# Disease Symptom Management Integration - Complete

## ? Features Implemented

### 1. Symptoms Tab in Disease Settings

**Location:** Settings ? Diseases ? Edit ? Symptoms Tab

The symptom management is now integrated directly into the disease edit page as a tab, making it easier to configure disease-specific symptoms.

**Tab Structure:**
- **Basic Information** - Disease details, category, parent, etc.
- **Symptoms** ? NEW! Manage disease-symptom associations
- **Custom Fields** - Configure custom fields for the disease

### 2. Apply Symptoms to Child Diseases

**Feature:** Hierarchical symptom profile propagation

When editing a disease's symptoms, you can now apply the entire symptom profile to all child diseases in one action.

**How it works:**
1. Configure symptoms for a parent disease
2. Check "Apply to child diseases" option
3. Save
4. All child diseases (and their children recursively) will inherit the symptom profile

**Example:**
```
Salmonella (Parent)
?? Symptom: Fever (Common)
?? Symptom: Diarrhea (Common)
?? Symptom: Vomiting (Not Common)

Click "Apply to children" ?

Salmonella Typhimurium (Child)
?? Symptom: Fever (Common) ? Inherited
?? Symptom: Diarrhea (Common) ? Inherited
?? Symptom: Vomiting (Not Common) ? Inherited

Salmonella Enteritidis (Child)
?? Symptom: Fever (Common) ? Inherited
?? Symptom: Diarrhea (Common) ? Inherited
?? Symptom: Vomiting (Not Common) ? Inherited
```

### 3. Database Migrations

**Note:** We're using the existing database tables created by SQL scripts:
- `Symptoms`
- `CaseSymptoms`
- `DiseaseSymptoms`

The tables already exist and are working correctly. EF Core migrations were attempted but skipped since the schema is already in place.

## ?? User Interface

### Symptoms Tab Features

**Table View:**
- ? Checkbox to select/deselect symptoms
- ? Symptom name and description
- ? Symptom code display
- ? "Common Symptom" dropdown (Yes/No)
- ? Display order input
- ? "Select All" checkbox

**Visual Feedback:**
- Selected symptoms have highlighted rows (table-active)
- Detail fields (common/order) are disabled until symptom is checked
- Hover effects on rows

**Child Disease Section:**
- Only shown if the disease has child diseases
- Warning message listing all affected child diseases
- Single checkbox to apply changes

## ?? Workflow

### Configure Disease Symptoms

1. Navigate to Settings ? Diseases
2. Click Edit on any disease
3. Click the **Symptoms** tab
4. Select symptoms by checking boxes
5. For each selected symptom:
   - Mark as "Common" or not (affects display priority on case forms)
   - Set display order (lower = shown first)
6. Optionally check "Apply to child diseases"
7. Click "Save Symptoms"

### Result on Case Forms

When creating/editing a case for this disease:
- **Common symptoms** appear as checkboxes at the top
- **Sorted by display order** (0 first, then 1, 2, etc.)
- Non-common symptoms still available via search
- Users can quickly record the most typical symptoms

## ?? Business Value

### Improved Data Quality
- Disease-specific symptom lists guide users to record relevant symptoms
- Reduces errors (e.g., recording inappropriate symptoms for a disease)
- Ensures consistency across cases of the same disease

### Time Savings
- No need to search for common symptoms - they're already displayed
- Bulk configuration for disease hierarchies
- One-click propagation to child diseases

### Better Clinical Accuracy
- "Common" vs "Rare" distinction helps clinicians identify typical presentations
- Supports early outbreak detection (unusual symptom patterns)
- Facilitates epidemiological analysis

## ?? Technical Details

### Code-Behind (`Edit.cshtml.cs`)

**New Properties:**
```csharp
public List<Symptom> AllSymptoms { get; set; }
public List<DiseaseSymptom> DiseaseSymptoms { get; set; }
public List<Disease> ChildDiseases { get; set; }
```

**New Methods:**
- `LoadSymptoms()` - Loads all symptoms and current disease-symptom associations
- `LoadChildDiseases()` - Loads direct children of the disease
- `OnPostSaveSymptomsAsync()` - Saves symptom associations
- `ApplySymptomsToChildrenAsync()` - Propagates symptoms to children
- `GetAllChildDiseaseIdsAsync()` - Recursively gets all child disease IDs

**Form Handler:**
```csharp
[HttpPost]
public async Task<IActionResult> OnPostSaveSymptomsAsync()
{
    // 1. Get selected symptoms from form
    // 2. Update disease-symptom associations
    // 3. If "applyToChildren" checked, propagate to all children
    // 4. Save changes
    // 5. Redirect with success message
}
```

### Recursive Propagation

The `GetAllChildDiseaseIdsAsync` method walks the entire disease hierarchy:

```csharp
Disease A
?? Disease B
?  ?? Disease D
?  ?? Disease E
?? Disease C
   ?? Disease F

GetAllChildDiseaseIdsAsync(A) returns: [B, C, D, E, F]
```

All descendants receive the symptom profile, not just direct children.

### Database Operations

**Soft Delete Pattern:**
- Unchecked symptoms are soft-deleted (`IsDeleted = true`)
- If re-checked later, they're restored
- Full audit trail maintained (CreatedBy, UpdatedBy, DeletedBy)

**Batch Operations:**
When applying to children:
1. Single transaction for all updates
2. Efficient: One query per child disease
3. Preserves symptom order and "common" flags

## ?? Database Schema

### DiseaseSymptoms Table
```sql
Id (PK)
DiseaseId (FK ? Diseases)
SymptomId (FK ? Symptoms)
IsCommon (bit) - Is this a common symptom?
SortOrder (int) - Display order
IsDeleted (bit) - Soft delete flag
DeletedAt (datetime2)
DeletedByUserId (nvarchar)
CreatedAt (datetime2)
CreatedBy (nvarchar)
UpdatedAt (datetime2)
UpdatedBy (nvarchar)
```

### Key Indexes
- `IX_DiseaseSymptoms_DiseaseId_SymptomId` (Unique, filtered on IsDeleted = 0)
- `IX_DiseaseSymptoms_DiseaseId_IsCommon_SortOrder` (Performance)

## ?? Testing Scenarios

### Scenario 1: Configure Symptoms for New Disease
1. Create disease "Influenza"
2. Go to Symptoms tab
3. Select: Fever, Cough, Fatigue, Body Aches
4. Mark Fever and Cough as Common
5. Set display order: Fever=0, Cough=1, Fatigue=2, Body Aches=3
6. Save
7. ? Result: Cases for Influenza show Fever and Cough checkboxes first

### Scenario 2: Apply to Child Diseases
1. Parent: "Salmonella" with symptoms configured
2. Children: "S. Typhimurium", "S. Enteritidis", "S. Paratyphi"
3. Edit Salmonella symptoms
4. Check "Apply to child diseases"
5. Save
6. ? Result: All three children inherit the symptom profile

### Scenario 3: Modify Child After Inheritance
1. Child disease inherited symptoms from parent
2. Edit child disease directly
3. Add/remove symptoms specific to this variant
4. Save
5. ? Result: Child has custom symptom profile, parent unchanged

### Scenario 4: Deep Hierarchy
```
Bacterial Infections (Level 0)
?? Salmonella (Level 1)
   ?? S. Typhimurium (Level 2)
   ?  ?? S. Typhimurium DT104 (Level 3)
   ?? S. Enteritidis (Level 2)
```
1. Configure symptoms for "Bacterial Infections"
2. Apply to children
3. ? Result: All 4 descendants inherit symptoms (recursive)

## ? Status

**Build:** ? SUCCESS  
**Integration:** ? COMPLETE  
**Features:** ? ALL IMPLEMENTED

### Completed:
- [x] Symptoms tab in disease edit page
- [x] Symptom selection with checkboxes
- [x] Common/Rare designation
- [x] Display order control
- [x] Apply to children option
- [x] Recursive hierarchy propagation
- [x] Visual feedback and validation
- [x] Success/warning messages
- [x] Tab persistence (remembers last tab)

## ?? Next Steps

1. **Restart debugger** to see the new Symptoms tab
2. Navigate to Settings ? Diseases ? Edit any disease
3. Click the Symptoms tab
4. Configure symptoms and test "Apply to children"

## ?? Removed Files

The standalone `ManageSymptoms.cshtml` and `.cs` files are no longer needed and can be removed:
- `Surveillance-MVP/Pages/Settings/Diseases/ManageSymptoms.cshtml`
- `Surveillance-MVP/Pages/Settings/Diseases/ManageSymptoms.cshtml.cs`

The functionality is now integrated into the main Edit page.

## ?? Usage Tips

1. **Configure parent diseases first** - Then propagate to children
2. **Use display order strategically** - Most important symptoms = lower numbers
3. **Mark rare symptoms as non-common** - Keeps case forms clean
4. **Test on a single child first** - Before applying to all children
5. **Tab persistence** - The page remembers which tab you were on

Everything is ready to use! ??
