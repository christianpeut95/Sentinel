# ?? Survey Template Library - Quick Reference

## ?? What It Does
Centralized library for reusable survey templates that can be shared across multiple task templates. No more copy-pasting survey JSON!

---

## ?? Quick Start

### **Creating a Survey Template**
1. **Settings ? Survey Template Library ? Create Survey Template**
2. Fill in:
   - **Name**: "Food History Survey"
   - **Category**: Foodborne
   - **Description**: "72-hour food recall survey"
   - **Applicable Diseases**: Select all diseases this applies to
   - **Tags**: food, history, recall
   - **Survey JSON**: Paste your SurveyJS definition
   - **Default Input/Output Mappings**: (optional) Set defaults
3. **Preview** to test
4. **Save**

### **Using in Task Template**
1. **Settings ? Task Templates ? Edit [Template]**
2. Go to **Survey Configuration** tab
3. Select: **"Use Survey Library"**
4. Choose template from dropdown
5. (Optional) Override mappings
6. **Save**

### **Editing Survey Template**
1. **Settings ? Survey Template Library**
2. Find your template
3. Click **Edit**
4. Make changes
5. **Save** (version auto-increments if survey JSON changed)

---

## ?? Key Features

| Feature | Description |
|---------|-------------|
| **Reusability** | Create once, use many times across different task templates |
| **Version Control** | Auto-increment version when survey definition changes |
| **Usage Tracking** | See how many times a template is used |
| **Protection** | Can't delete system templates or templates in use |
| **Disease Tagging** | Tag templates with applicable diseases for easy filtering |
| **Backwards Compatible** | Existing custom surveys still work |

---

## ?? UI Locations

### **Survey Template Library Pages**
- **List**: `/Settings/Surveys/SurveyTemplates`
- **Create**: `/Settings/Surveys/CreateSurveyTemplate`
- **Edit**: `/Settings/Surveys/EditSurveyTemplate?id={guid}`
- **Details**: `/Settings/Surveys/SurveyTemplateDetails?id={guid}`

### **Task Template Integration**
- **Edit Task Template**: `/Settings/Lookups/EditTaskTemplate?id={guid}`
  - Survey Configuration Tab ? Choose Library or Custom

---

## ?? Data Flow

```
Survey Template Library
  ? (Select in Task Template)
Task Template
  ? (Auto-create tasks)
Case Task
  ? (User completes)
Survey Response
  ? (Apply mappings)
Updated Case/Patient Data
```

---

## ?? Configuration Options

### **Survey Template Properties**
- **Name**: Template name
- **Description**: What the survey is for
- **Category**: Foodborne, Respiratory, etc.
- **Tags**: Searchable keywords
- **Applicable Diseases**: Which diseases can use this
- **Survey JSON**: SurveyJS definition
- **Default Input Mapping**: Pre-populate fields from case/patient
- **Default Output Mapping**: Save responses back to case/patient
- **IsActive**: Enable/disable template
- **IsSystemTemplate**: Protect from edits/deletes

### **Task Template Survey Options**
1. **Use Survey Library**
   - Select template from dropdown
   - Optionally override default mappings
   - Changes to template affect all tasks

2. **Custom Survey**
   - Enter SurveyJS JSON directly
   - Set custom mappings
   - Independent of library

---

## ??? Protection Rules

### **Cannot Delete If:**
- ? System template (`IsSystemTemplate = true`)
- ? Used by any task template (`UsageCount > 0`)

### **Cannot Edit If:**
- ? System template (blocked in UI)

### **Version Increment:**
- ? Automatically increments when `SurveyDefinitionJson` changes
- ?? Other fields (name, description, mappings) don't increment version

---

## ?? Example Workflow

### **Scenario: Multi-Disease Food Survey**

#### **Step 1: Create Template**
```
Name: "Comprehensive Food History"
Category: Foodborne
Diseases: Salmonella, Shigella, E. coli, Campylobacter
Tags: food, history, recall, 72hours
Survey: {72-hour food recall JSON}
Input Mapping: {"patientName": "Patient.GivenName", ...}
Output Mapping: {"exposureData": "Case.CustomFields.FoodHistory", ...}
```

#### **Step 2: Use in Multiple Templates**
- "Salmonella Investigation" ? Uses "Comprehensive Food History"
- "E. coli Investigation" ? Uses "Comprehensive Food History"
- "Shigella Investigation" ? Uses "Comprehensive Food History"

#### **Step 3: Update Once**
- Edit "Comprehensive Food History" template
- Add new question about water sources
- Save ? Version 1 ? Version 2
- All three investigation templates now use updated survey!

---

## ?? Search & Filter

### **In Survey Template Library:**
- **Search**: Name, description, tags
- **Category Filter**: Foodborne, Respiratory, etc.
- **Status Filter**: Active, Inactive
- **Usage Count**: How many tasks use it
- **Disease Count**: How many diseases tagged

---

## ?? Best Practices

### **When to Use Library:**
? Survey used by multiple diseases
? Survey needs consistent updates across tasks
? Want centralized management
? Complex surveys with many questions

### **When to Use Custom:**
? One-off unique survey
? Rarely changes
? Disease-specific questions
? Quick prototype/test

---

## ?? Troubleshooting

### **Survey not appearing in dropdown?**
- Check `IsActive = true`
- Template must be saved before use

### **Changes not reflecting in tasks?**
- Library surveys update automatically
- Custom surveys don't change (they're embedded)
- Check you're using library, not custom

### **Can't delete template?**
- Check if used by task templates
- Check if system template
- View details page for usage info

### **Version not incrementing?**
- Only `SurveyDefinitionJson` changes trigger version bump
- Other fields don't increment version

---

## ?? Related Documentation
- [Survey System Complete Guide](SURVEY_SYSTEM_100_PERCENT_COMPLETE.md)
- [Survey System Quick Reference](SURVEY_SYSTEM_QUICK_REF.md)
- [Task Management System](TASK_MANAGEMENT_SYSTEM_COMPLETE.md)
- [SurveyJS Documentation](https://surveyjs.io/form-library/documentation)

---

## ?? Quick Tips

?? **Tip 1**: Use descriptive names and tags for easy searching
?? **Tip 2**: Test surveys with Preview before saving
?? **Tip 3**: Use Categories to organize by disease type
?? **Tip 4**: Check Details page to see which tasks use a template
?? **Tip 5**: Mark rarely-used templates as Inactive instead of deleting

---

**Status**: ? Fully Implemented and Ready to Use!
