# ?? Survey Template Library - Implementation COMPLETE ?

## Overview
Implemented a centralized Survey Template Library where surveys can be created once and reused across multiple task templates. Surveys can be tagged with "Applicable Diseases" to help organize and find relevant surveys.

---

## ? COMPLETED

### **1. Database Schema** ?
**Tables Created:**
```sql
CREATE TABLE SurveyTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000),
    Category NVARCHAR(100),
    SurveyDefinitionJson NVARCHAR(MAX) NOT NULL,
    DefaultInputMappingJson NVARCHAR(MAX),
    DefaultOutputMappingJson NVARCHAR(MAX),
    Version INT DEFAULT 1,
    Tags NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    IsSystemTemplate BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(MAX),
    ModifiedAt DATETIME2,
    ModifiedBy NVARCHAR(MAX),
    UsageCount INT DEFAULT 0,
    LastUsedAt DATETIME2
);

CREATE TABLE SurveyTemplateDiseases (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SurveyTemplateId UNIQUEIDENTIFIER NOT NULL,
    DiseaseId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (SurveyTemplateId) REFERENCES SurveyTemplates(Id) ON DELETE CASCADE,
    FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE CASCADE
);

ALTER TABLE TaskTemplates 
ADD SurveyTemplateId UNIQUEIDENTIFIER NULL,
CONSTRAINT FK_TaskTemplates_SurveyTemplates 
FOREIGN KEY (SurveyTemplateId) REFERENCES SurveyTemplates(Id) ON DELETE SET NULL;
```

**Migration:** `20260207034754_AddSurveyTemplateLibrary` ? Applied

---

### **2. Models** ?
**Created:**
- `SurveyTemplate.cs` - Main survey template entity
- `SurveyTemplateDisease.cs` - Junction table for disease associations

**Updated:**
- `TaskTemplate.cs` - Added `SurveyTemplateId` FK

---

### **3. ApplicationDbContext** ?
**Added:**
- `DbSet<SurveyTemplate> SurveyTemplates`
- `DbSet<SurveyTemplateDisease> SurveyTemplateDiseases`
- Relationship configurations with proper indexes

---

### **4. Admin UI - Survey Template Library** ?
**Pages Created:**
- `SurveyTemplates/Index` - List all survey templates with filtering
- `SurveyTemplates/Create` - Create new survey template
- `SurveyTemplates/Edit` - Edit existing survey template ? **NEW**
- `SurveyTemplates/Details` - View survey template details ? **NEW**

**Features:**
- ? Card-based layout showing templates
- ? Category filtering
- ? Active/Inactive filtering
- ? Search by name, description, tags
- ? Shows usage count (how many tasks use it)
- ? Shows applicable diseases count
- ? Disease multi-select with checkboxes
- ? JSON validation
- ? Survey preview
- ? System template protection
- ? Edit with version increment
- ? Delete with protection checks
- ? Details page with usage statistics
- ? Links to task templates using survey

**Settings Page Updated:** ?
- Added link to Survey Template Library under "Task Management & Surveys"

---

### **5. Edit Survey Template Page** ?
**Files Created:**
- `EditSurveyTemplate.cshtml`
- `EditSurveyTemplate.cshtml.cs`

**Features:**
- ? Load existing template
- ? Update survey definition
- ? Update default mappings
- ? Update applicable diseases
- ? Version increment logic when survey definition changes
- ? Delete protection if in use
- ? System template protection
- ? Shows usage warning
- ? JSON validation
- ? Survey preview

---

### **6. Survey Template Details Page** ?
**Files Created:**
- `SurveyTemplateDetails.cshtml`
- `SurveyTemplateDetails.cshtml.cs`

**Features:**
- ? Read-only view of template
- ? Show all task templates using it (with links)
- ? Show all applicable diseases
- ? Usage statistics (usage count, last used date)
- ? Survey preview
- ? Edit button (if not system template)
- ? Formatted JSON display

---

### **7. Update EditTaskTemplate Page** ?
**Modified:**
- `EditTaskTemplate.cshtml`
- `EditTaskTemplate.cshtml.cs`

**Features Added:**
- ? Radio buttons to select "Use Library Survey" vs "Custom Survey"
- ? Library survey dropdown populated from active templates
- ? Show selected template with links to view/edit
- ? Hide custom survey JSON field when library selected
- ? Show custom JSON editor when custom selected
- ? Allow override of mappings for both options
- ? JavaScript to toggle sections dynamically
- ? Validation for both survey types

---

### **8. Update SurveyService** ?
**Modified:** `SurveyService.cs`

**Changes:**
```csharp
public async Task<SurveyDefinitionWithData> GetSurveyForTaskAsync(Guid taskId)
{
    // 1. Check if TaskTemplate uses Survey Library
    if (taskTemplate.SurveyTemplateId != null)
    {
        var surveyTemplate = await _context.SurveyTemplates
            .FirstOrDefaultAsync(st => st.Id == taskTemplate.SurveyTemplateId);
        
        surveyJson = surveyTemplate.SurveyDefinitionJson;
        defaultInputMappings = surveyTemplate.DefaultInputMappingJson;
        defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;
        
        // Update usage tracking
        template.UsageCount++;
        template.LastUsedAt = DateTime.UtcNow;
    }
    // 2. Fall back to embedded survey (backwards compatible)
    else if (!string.IsNullOrEmpty(taskTemplate.SurveyDefinitionJson))
    {
        surveyJson = taskTemplate.SurveyDefinitionJson;
        defaultInputMappings = taskTemplate.DefaultInputMappingJson;
        defaultOutputMappings = taskTemplate.DefaultOutputMappingJson;
    }

    // 3. Disease-specific overrides still work
    if (diseaseTaskTemplate != null)
    {
        if (!string.IsNullOrEmpty(diseaseTaskTemplate.InputMappingJson))
            inputMappings = diseaseTaskTemplate.InputMappingJson;
        if (!string.IsNullOrEmpty(diseaseTaskTemplate.OutputMappingJson))
            outputMappings = diseaseTaskTemplate.OutputMappingJson;
    }
}
```

**Features:**
- ? Checks Survey Library first
- ? Falls back to embedded survey (backwards compatible)
- ? Updates usage tracking (UsageCount, LastUsedAt)
- ? Disease-specific overrides work for both library and custom surveys
- ? Applies to both GetSurveyForTaskAsync and SaveSurveyResponseAsync

---

### **9. Delete Handler** ?
**Added to:** `SurveyTemplatesModel`

**Implementation:**
```csharp
public async Task<IActionResult> OnPostDeleteAsync(Guid id)
{
    var template = await _context.SurveyTemplates.FindAsync(id);
    
    if (template == null)
        return NotFound();
    
    // Check if system template
    if (template.IsSystemTemplate)
    {
        TempData["ErrorMessage"] = "Cannot delete system templates";
        return RedirectToPage();
    }
    
    // Check if in use
    var usageCount = await _context.TaskTemplates
        .CountAsync(tt => tt.SurveyTemplateId == id);
    
    if (usageCount > 0)
    {
        TempData["ErrorMessage"] = $"Cannot delete: Template is used by {usageCount} task template(s)";
        return RedirectToPage();
    }
    
    _context.SurveyTemplates.Remove(template);
    await _context.SaveChangesAsync();
    
    TempData["SuccessMessage"] = "Survey template deleted successfully";
    return RedirectToPage();
}
```

**Features:**
- ? Protection for system templates
- ? Protection if template is in use
- ? Success/error messages
- ? Delete modal in UI

---

### **10. Usage Tracking** ?
**Implemented in:** `SurveyService.cs`

**Updates:**
- ? `UsageCount` incremented when survey is loaded
- ? `LastUsedAt` updated when survey is used
- ? Automatic tracking (no manual intervention needed)
- ? Displayed in Index, Edit, and Details pages

---

## Example Use Cases

### **Use Case 1: Food History Survey**
```
Survey Template: "Comprehensive Food History"
  ?? Category: Foodborne
  ?? Applicable Diseases:
  ?   ?? Salmonella
  ?   ?? Shigella
  ?   ?? E. coli O157
  ?   ?? Campylobacter
  ?   ?? Listeria
  ?? Survey: 72-hour food recall with meal history
  ?? Default Input: Patient name, case onset date
  ?? Default Output: Save exposure data to Case.CustomFields

Task Template: "Salmonella Investigation"
  ?? Uses: "Comprehensive Food History" (from library)

Task Template: "E. coli Investigation"
  ?? Uses: "Comprehensive Food History" (from library)

? Update survey once, all investigations updated!
```

---

### **Use Case 2: Contact Investigation**
```
Survey Template: "COVID-19 Contact Investigation"
  ?? Category: Respiratory
  ?? Applicable Diseases: COVID-19, Influenza
  ?? Survey: Contact list, exposure locations, dates
  ?? Mappings: Save contacts, exposure dates

Task Template: "COVID Contact Tracing"
  ?? Uses: "COVID-19 Contact Investigation"
```

---

### **Use Case 3: Symptom Screening**
```
Survey Template: "Respiratory Symptom Screen"
  ?? Category: Respiratory
  ?? Applicable Diseases: COVID, Flu, RSV, TB
  ?? Survey: Symptom checklist, onset dates
  ?? Mappings: Save symptoms to CaseSymptoms table
```

---

## UI Flow

### **Admin Creating Survey Template:**
```
1. Settings ? Survey Template Library
2. Click "Create Survey Template"
3. Fill in:
   - Name: "Food History Survey"
   - Category: Foodborne
   - Description: "72-hour food recall..."
   - Select Diseases: Salmonella, Shigella, E. coli
   - Tags: food, history, recall, 72hours
   - Survey JSON: {...}
   - Input Mapping: {...}
   - Output Mapping: {...}
4. Preview survey (optional)
5. Save
```

### **Admin Configuring Task Template:**
```
1. Settings ? Task Templates
2. Edit "Salmonella Investigation"
3. Survey Configuration tab
4. Choose: [Use Survey Library ?]
   - Select "Food History Survey" from dropdown
   - Shows: "? Using library survey"
   - Option to override mappings
OR
   Choose: [Custom Survey]
   - Enter JSON directly
5. Save
```

---

## Benefits

? **Reusability** - Create once, use many times
? **Consistency** - All diseases use same survey version
? **Easy Updates** - Update survey in one place
? **Organization** - Categorize & tag surveys
? **Disease Filtering** - Find surveys by applicable disease
? **Backwards Compatible** - Existing embedded surveys still work
? **Flexibility** - Can still create custom surveys per task
? **Version Control** - Survey versions tracked automatically
? **Usage Tracking** - See which tasks use each template
? **Protection** - System templates and in-use templates can't be deleted

---

## Database Queries

### **Find all tasks using a survey template:**
```sql
SELECT tt.Name, tt.Description
FROM TaskTemplates tt
WHERE tt.SurveyTemplateId = 'GUID-HERE'
```

### **Find all surveys applicable to a disease:**
```sql
SELECT st.*
FROM SurveyTemplates st
INNER JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
WHERE std.DiseaseId = 'DISEASE-GUID-HERE'
AND st.IsActive = 1
```

### **Get usage statistics:**
```sql
SELECT 
    st.Name,
    st.Category,
    COUNT(DISTINCT tt.Id) as TaskTemplateCount,
    COUNT(DISTINCT std.DiseaseId) as ApplicableDiseaseCount,
    st.UsageCount,
    st.LastUsedAt
FROM SurveyTemplates st
LEFT JOIN TaskTemplates tt ON st.Id = tt.SurveyTemplateId
LEFT JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
GROUP BY st.Id, st.Name, st.Category, st.UsageCount, st.LastUsedAt
```

---

## Technical Architecture

### **Data Flow:**
```
1. Admin creates Survey Template
   ??> Stored in SurveyTemplates table
   ??> Associated with diseases via SurveyTemplateDiseases

2. Admin configures Task Template
   ??> Sets SurveyTemplateId (library) OR SurveyDefinitionJson (custom)
   ??> Optional: Override default mappings

3. Case created with disease
   ??> Tasks auto-created from templates
   ??> Each task knows its survey source

4. User completes task
   ??> SurveyService checks: Library? Custom? Embedded?
   ??> Loads appropriate survey + mappings
   ??> Pre-populates from case/patient data
   ??> Updates usage tracking

5. User submits survey
   ??> Saves responses
   ??> Applies output mappings
   ??> Updates case/patient data
```

### **Backwards Compatibility:**
- Existing tasks with embedded surveys continue to work
- No migration needed for existing data
- Can gradually migrate to library surveys
- Both approaches can coexist

---

## Files Created/Modified

### **Created:**
1. `Pages/Settings/Surveys/SurveyTemplates.cshtml` - Index page
2. `Pages/Settings/Surveys/SurveyTemplates.cshtml.cs` - Index logic + Delete handler
3. `Pages/Settings/Surveys/CreateSurveyTemplate.cshtml` - Create page
4. `Pages/Settings/Surveys/CreateSurveyTemplate.cshtml.cs` - Create logic
5. `Pages/Settings/Surveys/EditSurveyTemplate.cshtml` - Edit page ?
6. `Pages/Settings/Surveys/EditSurveyTemplate.cshtml.cs` - Edit logic ?
7. `Pages/Settings/Surveys/SurveyTemplateDetails.cshtml` - Details page ?
8. `Pages/Settings/Surveys/SurveyTemplateDetails.cshtml.cs` - Details logic ?

### **Modified:**
1. `Services/SurveyService.cs` - Added library lookup ?
2. `Pages/Settings/Lookups/EditTaskTemplate.cshtml` - Added library UI ?
3. `Pages/Settings/Lookups/EditTaskTemplate.cshtml.cs` - Added library logic ?

---

## Current Status: 100% Complete ?

? Database & Models
? Migration Applied
? Create Page Built
? Index/List Page Built
? Edit Page Built ?
? Details Page Built ?
? Integration with TaskTemplate ?
? Service Layer Updates ?
? Delete Handler ?
? Usage Tracking ?

**All tasks completed! System is fully functional and ready for testing!** ????

## Overview
Implementing a centralized Survey Template Library where surveys can be created once and reused across multiple task templates. Surveys can be tagged with "Applicable Diseases" to help organize and find relevant surveys.

---

## ? COMPLETED

### **1. Database Schema** ?
**Tables Created:**
```sql
CREATE TABLE SurveyTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000),
    Category NVARCHAR(100),
    SurveyDefinitionJson NVARCHAR(MAX) NOT NULL,
    DefaultInputMappingJson NVARCHAR(MAX),
    DefaultOutputMappingJson NVARCHAR(MAX),
    Version INT DEFAULT 1,
    Tags NVARCHAR(500),
    IsActive BIT DEFAULT 1,
    IsSystemTemplate BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    CreatedBy NVARCHAR(MAX),
    ModifiedAt DATETIME2,
    ModifiedBy NVARCHAR(MAX),
    UsageCount INT DEFAULT 0,
    LastUsedAt DATETIME2
);

CREATE TABLE SurveyTemplateDiseases (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SurveyTemplateId UNIQUEIDENTIFIER NOT NULL,
    DiseaseId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (SurveyTemplateId) REFERENCES SurveyTemplates(Id) ON DELETE CASCADE,
    FOREIGN KEY (DiseaseId) REFERENCES Diseases(Id) ON DELETE CASCADE
);

ALTER TABLE TaskTemplates 
ADD SurveyTemplateId UNIQUEIDENTIFIER NULL,
CONSTRAINT FK_TaskTemplates_SurveyTemplates 
FOREIGN KEY (SurveyTemplateId) REFERENCES SurveyTemplates(Id) ON DELETE SET NULL;
```

**Migration:** `20260207034754_AddSurveyTemplateLibrary` ? Applied

---

### **2. Models** ?
**Created:**
- `SurveyTemplate.cs` - Main survey template entity
- `SurveyTemplateDisease.cs` - Junction table for disease associations

**Updated:**
- `TaskTemplate.cs` - Added `SurveyTemplateId` FK

---

### **3. ApplicationDbContext** ?
**Added:**
- `DbSet<SurveyTemplate> SurveyTemplates`
- `DbSet<SurveyTemplateDisease> SurveyTemplateDiseases`
- Relationship configurations with proper indexes

---

### **4. Admin UI - Survey Template Library** ?
**Pages Created:**
- `SurveyTemplates/Index` - List all survey templates with filtering
- `SurveyTemplates/Create` - Create new survey template

**Features:**
- ? Card-based layout showing templates
- ? Category filtering
- ? Active/Inactive filtering
- ? Search by name, description, tags
- ? Shows usage count (how many tasks use it)
- ? Shows applicable diseases count
- ? Disease multi-select with checkboxes
- ? JSON validation
- ? Survey preview
- ? System template protection

**Settings Page Updated:** ?
- Added link to Survey Template Library under "Task Management & Surveys"

---

## ?? IN PROGRESS / TODO

### **5. Edit Survey Template Page** ??
Need to create:
- `EditSurveyTemplate.cshtml`
- `EditSurveyTemplate.cshtml.cs`

Features needed:
- Load existing template
- Update survey definition
- Update default mappings
- Update applicable diseases
- Version increment logic
- Delete protection if in use

---

### **6. Survey Template Details Page** ??
Need to create:
- `SurveyTemplateDetails.cshtml`
- `SurveyTemplateDetails.cshtml.cs`

Features needed:
- Read-only view of template
- Show all task templates using it
- Show all applicable diseases
- Usage history

---

### **7. Update EditTaskTemplate Page** ??
Need to modify:
- Add dropdown to select from Survey Template Library
- Show option: "Use Library Survey" vs "Custom Survey"
- If library survey selected:
  - Hide custom survey JSON field
  - Show selected template name & preview link
  - Allow override of mappings
- If custom survey:
  - Show existing JSON editor

---

### **8. Update SurveyService** ??
Need to modify `GetSurveyForTaskAsync`:

```csharp
// 1. Check if TaskTemplate uses Survey Library
if (taskTemplate.SurveyTemplateId != null)
{
    var surveyTemplate = await _context.SurveyTemplates
        .FirstOrDefaultAsync(st => st.Id == taskTemplate.SurveyTemplateId);
    
    surveyJson = surveyTemplate.SurveyDefinitionJson;
    defaultInputMappings = surveyTemplate.DefaultInputMappingJson;
    defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;
}
// 2. Fall back to embedded survey (backwards compatible)
else if (!string.IsNullOrEmpty(taskTemplate.SurveyDefinitionJson))
{
    surveyJson = taskTemplate.SurveyDefinitionJson;
    defaultInputMappings = taskTemplate.DefaultInputMappingJson;
    defaultOutputMappings = taskTemplate.DefaultOutputMappingJson;
}

// 3. Disease-specific overrides still work
if (diseaseTaskTemplate != null)
{
    if (!string.IsNullOrEmpty(diseaseTaskTemplate.InputMappingJson))
        inputMappings = diseaseTaskTemplate.InputMappingJson;
    if (!string.IsNullOrEmpty(diseaseTaskTemplate.OutputMappingJson))
        outputMappings = diseaseTaskTemplate.OutputMappingJson;
}
```

---

### **9. Delete Handler** ??
Add delete functionality to `SurveyTemplatesModel`:

```csharp
public async Task<IActionResult> OnPostDeleteAsync(Guid id)
{
    var template = await _context.SurveyTemplates.FindAsync(id);
    
    if (template == null)
        return NotFound();
    
    // Check if system template
    if (template.IsSystemTemplate)
    {
        TempData["ErrorMessage"] = "Cannot delete system templates";
        return RedirectToPage();
    }
    
    // Check if in use
    var usageCount = await _context.TaskTemplates
        .CountAsync(tt => tt.SurveyTemplateId == id);
    
    if (usageCount > 0)
    {
        TempData["ErrorMessage"] = $"Cannot delete: Template is used by {usageCount} task template(s)";
        return RedirectToPage();
    }
    
    _context.SurveyTemplates.Remove(template);
    await _context.SaveChangesAsync();
    
    TempData["SuccessMessage"] = "Survey template deleted successfully";
    return RedirectToPage();
}
```

---

### **10. Usage Tracking** ??
Update `UsageCount` when:
- TaskTemplate.SurveyTemplateId is set/updated
- TaskTemplate is deleted
- Survey is completed

---

## Example Use Cases

### **Use Case 1: Food History Survey**
```
Survey Template: "Comprehensive Food History"
  ?? Category: Foodborne
  ?? Applicable Diseases:
  ?   ?? Salmonella
  ?   ?? Shigella
  ?   ?? E. coli O157
  ?   ?? Campylobacter
  ?   ?? Listeria
  ?? Survey: 72-hour food recall with meal history
  ?? Default Input: Patient name, case onset date
  ?? Default Output: Save exposure data to Case.CustomFields

Task Template: "Salmonella Investigation"
  ?? Uses: "Comprehensive Food History" (from library)

Task Template: "E. coli Investigation"
  ?? Uses: "Comprehensive Food History" (from library)

? Update survey once, all investigations updated!
```

---

### **Use Case 2: Contact Investigation**
```
Survey Template: "COVID-19 Contact Investigation"
  ?? Category: Respiratory
  ?? Applicable Diseases: COVID-19, Influenza
  ?? Survey: Contact list, exposure locations, dates
  ?? Mappings: Save contacts, exposure dates

Task Template: "COVID Contact Tracing"
  ?? Uses: "COVID-19 Contact Investigation"
```

---

### **Use Case 3: Symptom Screening**
```
Survey Template: "Respiratory Symptom Screen"
  ?? Category: Respiratory
  ?? Applicable Diseases: COVID, Flu, RSV, TB
  ?? Survey: Symptom checklist, onset dates
  ?? Mappings: Save symptoms to CaseSymptoms table
```

---

## UI Flow

### **Admin Creating Survey Template:**
```
1. Settings ? Survey Template Library
2. Click "Create Survey Template"
3. Fill in:
   - Name: "Food History Survey"
   - Category: Foodborne
   - Description: "72-hour food recall..."
   - Select Diseases: Salmonella, Shigella, E. coli
   - Tags: food, history, recall, 72hours
   - Survey JSON: {...}
   - Input Mapping: {...}
   - Output Mapping: {...}
4. Preview survey (optional)
5. Save
```

### **Admin Configuring Task Template:**
```
1. Settings ? Task Templates
2. Edit "Salmonella Investigation"
3. Survey Configuration tab
4. Choose: [Use Survey Library ?]
   - Select "Food History Survey" from dropdown
   - Shows: "? Using library survey"
   - Option to override mappings
OR
   Choose: [Custom Survey]
   - Enter JSON directly
5. Save
```

---

## Benefits

? **Reusability** - Create once, use many times
? **Consistency** - All diseases use same survey version
? **Easy Updates** - Update survey in one place
? **Organization** - Categorize & tag surveys
? **Disease Filtering** - Find surveys by applicable disease
? **Backwards Compatible** - Existing embedded surveys still work
? **Flexibility** - Can still create custom surveys per task

---

## Database Queries

### **Find all tasks using a survey template:**
```sql
SELECT tt.Name, tt.Description
FROM TaskTemplates tt
WHERE tt.SurveyTemplateId = 'GUID-HERE'
```

### **Find all surveys applicable to a disease:**
```sql
SELECT st.*
FROM SurveyTemplates st
INNER JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
WHERE std.DiseaseId = 'DISEASE-GUID-HERE'
AND st.IsActive = 1
```

### **Get usage statistics:**
```sql
SELECT 
    st.Name,
    st.Category,
    COUNT(DISTINCT tt.Id) as TaskTemplateCount,
    COUNT(DISTINCT std.DiseaseId) as ApplicableDiseaseCount
FROM SurveyTemplates st
LEFT JOIN TaskTemplates tt ON st.Id = tt.SurveyTemplateId
LEFT JOIN SurveyTemplateDiseases std ON st.Id = std.SurveyTemplateId
GROUP BY st.Id, st.Name, st.Category
```

---

## Next Steps

1. **Create Edit page** - EditSurveyTemplate.cshtml + code-behind
2. **Create Details page** - SurveyTemplateDetails.cshtml + code-behind
3. **Update EditTaskTemplate** - Add survey library dropdown
4. **Update SurveyService** - Add library survey lookup logic
5. **Add Delete handler** - OnPostDeleteAsync in Index page
6. **Test end-to-end** - Create library survey ? use in task ? complete survey
7. **Seed sample data** - Create common survey templates
8. **Documentation** - Update user guides

---

## Current Status: 60% Complete

? Database & Models
? Migration Applied
? Create Page Built
? Index/List Page Built
?? Edit Page (Next)
?? Details Page
?? Integration with TaskTemplate
?? Service Layer Updates

**Estimated Time to Complete:** 2-3 hours

**Ready to continue with Edit/Details pages and service integration!** ??
