# ?? Manual Task Creation with Survey - Quick Guide

## ?? What's New

You can now **add surveys to manually created tasks** on a case! This gives you flexibility to create custom tasks with either:
1. **Survey from Library** - Select a pre-built survey template
2. **Custom Survey** - Enter your own SurveyJS JSON

---

## ?? How to Use

### **Step 1: Open Case Details**
Navigate to a case details page

### **Step 2: Add Task**
1. Click the **"Add Task"** button in the Tasks card
2. Switch to the **"Manual Entry"** tab

### **Step 3: Fill in Task Details**
- **Title** (required): e.g., "Food History Interview"
- **Description** (optional): Additional details
- **Task Type** (required): Select type from dropdown
- **Priority** (required): Low, Medium, High, or Urgent
- **Due Date** (optional): Set deadline

### **Step 4: Add Survey (Optional)**
1. Check the box: **"Include a survey with this task"**
2. Choose your survey source:

#### **Option A: Use Survey Library** (Recommended)
- Select a template from the **"Select Survey Template"** dropdown
- Templates are organized by category (e.g., "Food History (Foodborne)")
- Click "View Survey Library" to see all available templates

#### **Option B: Custom Survey JSON**
- Select **"Custom Survey JSON"**
- Paste your SurveyJS JSON in the text area
- See [SurveyJS Examples](https://surveyjs.io/form-library/examples/overview/reactjs) for help

### **Step 5: Create Task**
Click **"Create Task"** - Done!

---

## ?? What Happens Behind the Scenes

### **If No Survey Selected:**
- Task is created as a normal manual task
- No template or survey associated

### **If Survey from Library Selected:**
- A temporary inactive task template is created
- Template is linked to the selected survey template
- Task uses survey from library (including mappings)
- Survey can be completed just like template-based tasks

### **If Custom Survey JSON Provided:**
- A temporary inactive task template is created
- Custom JSON is stored in the template
- Task can be completed with the custom survey

---

## ?? Example Use Cases

### **Use Case 1: Food History Survey**
```
Task: "Complete Food History"
Type: Interview
Priority: High
Survey: "Comprehensive Food History" (from library)
? When user completes task, they fill out 72-hour food recall
```

### **Use Case 2: Follow-up Phone Call with Custom Questions**
```
Task: "Follow-up Call - Symptom Check"
Type: Follow-up
Priority: Medium
Custom Survey: {"title": "Symptom Check", "elements": [...]}
? User completes custom symptom checklist
```

### **Use Case 3: Contact Investigation**
```
Task: "Contact Tracing Interview"
Type: Investigation
Priority: Urgent
Survey: "COVID-19 Contact Investigation" (from library)
? Standardized contact tracing form
```

---

## ?? UI Features

### **Survey Configuration Section**
- Collapsible section (hidden by default)
- Checkbox to enable survey
- Radio buttons to choose library vs custom
- Dropdown for survey templates (with categories)
- Text area for custom JSON
- Links to Survey Library and SurveyJS docs

### **Smart Defaults**
- Survey section hidden until checkbox checked
- Library option selected by default
- Form clears when modal closes

---

## ? Benefits

| Feature | Benefit |
|---------|---------|
| **Flexibility** | Can add surveys to manual tasks on-the-fly |
| **Reusability** | Use existing survey templates from library |
| **Customization** | Can create one-off custom surveys |
| **Consistency** | Library surveys maintain standardization |
| **No Pre-config** | Don't need to create task template first |

---

## ?? Technical Details

### **Database Changes**
- When survey is selected, a temporary `TaskTemplate` is created
- Template is marked as **inactive** (`IsActive = false`)
- Template is linked to survey (via `SurveyTemplateId` or `SurveyDefinitionJson`)
- Task is linked to the temporary template

### **Form Parameters**
```csharp
OnPostAddTaskManualAsync(
    Guid id,                    // Case ID
    string title,               // Task title
    string? description,        // Task description
    Guid taskTypeId,           // Task type
    int priority,              // Priority (0-3)
    DateTime? dueDate,         // Due date
    Guid? surveyTemplateId,    // Survey from library
    string? customSurveyJson   // OR custom JSON
)
```

### **Survey Service Integration**
- `SurveyService` checks for `SurveyTemplateId` first
- Falls back to embedded `SurveyDefinitionJson`
- Works exactly like regular template-based tasks
- Mappings and pre-population work automatically

---

## ?? Complete Workflow

```
1. User opens case
   ?
2. Clicks "Add Task" ? "Manual Entry" tab
   ?
3. Fills in task details
   ?
4. Checks "Include a survey" (optional)
   ?
5. Selects survey source:
   • Library: Pick from dropdown
   • Custom: Enter JSON
   ?
6. Clicks "Create Task"
   ?
7. Temporary task template created (if survey selected)
   ?
8. Task created and appears in list
   ?
9. User can complete survey via "Complete Survey" button
   ?
10. Survey responses saved to case
```

---

## ?? Testing Checklist

- [ ] Create manual task **without** survey
- [ ] Create manual task **with library survey**
- [ ] Create manual task **with custom survey**
- [ ] Complete task with library survey
- [ ] Complete task with custom survey
- [ ] Verify pre-population works (library survey)
- [ ] Verify output mappings work (library survey)
- [ ] Check temporary template is created and inactive
- [ ] Verify task shows "Complete Survey" button
- [ ] Test validation (required fields)

---

## ?? Related Features

- **Survey Template Library**: `/Settings/Surveys/SurveyTemplates`
- **Task Templates**: `/Settings/Lookups/TaskTemplates`
- **Survey System**: Complete task ? "Complete Survey"
- **My Tasks Dashboard**: View all your assigned tasks

---

## ?? Resources

- **Survey Library**: Create reusable survey templates
- **SurveyJS Docs**: https://surveyjs.io/form-library/documentation
- **SurveyJS Examples**: https://surveyjs.io/form-library/examples
- **Task Management Guide**: `TASK_MANAGEMENT_UI_QUICK_REF.md`
- **Survey System Guide**: `SURVEY_SYSTEM_QUICK_REF.md`

---

## ?? Pro Tips

1. **Use Library Surveys**: Faster and more consistent than custom JSON
2. **Create Templates First**: For recurring survey needs, create a template
3. **Test JSON**: Validate custom JSON before creating task
4. **Set Priorities**: Higher priority tasks appear first
5. **Assign Due Dates**: Helps track task completion

---

## ? FAQ

**Q: Can I edit the survey after creating the task?**
A: No, the survey is fixed at creation. You'd need to create a new task.

**Q: Can I use the same survey for multiple manual tasks?**
A: Yes! Use a library survey and select it each time.

**Q: What if I make a mistake in custom JSON?**
A: Task will be created but survey won't render. Check JSON validation.

**Q: Do temporary templates clutter the database?**
A: They're marked inactive and only used for that specific task.

**Q: Can I convert a manual task to use a different survey?**
A: No, you'd need to delete and recreate the task.

---

## ? Status

**Feature Status**: ? Fully Implemented and Ready to Use!

**Build Status**: ? Successful

**Last Updated**: February 7, 2026
