# ? Manual Task Creation with Survey - Implementation Complete!

## ?? Summary

Successfully added the ability to **include surveys when manually creating tasks** on a case. Users can now choose to add a survey from the Survey Template Library or enter custom SurveyJS JSON when creating a manual task.

---

## ? What Was Implemented

### **1. Backend Changes**
**File**: `Surveillance-MVP/Pages/Cases/Details.cshtml.cs`

#### Added Properties:
```csharp
public SelectList SurveyTemplatesList { get; set; } = default!;
```

#### Updated Method: `LoadTaskTemplates()`
- Loads active survey templates
- Creates SelectList with category display
- Format: "Survey Name (Category)"

#### Updated Handler: `OnPostAddTaskManualAsync()`
Added parameters:
- `Guid? surveyTemplateId` - For library survey selection
- `string? customSurveyJson` - For custom survey JSON

New logic:
1. Creates temporary inactive `TaskTemplate` if survey specified
2. Links template to survey (library or custom)
3. Associates task with temporary template
4. Allows survey completion via SurveyService

---

### **2. Frontend Changes**
**File**: `Surveillance-MVP/Pages/Cases/Details.cshtml`

#### Added UI Section: Survey Configuration
Located in Manual Entry tab of Add Task modal

**Components:**
- ? Checkbox to enable survey
- ? Radio buttons: Library vs Custom
- ? Survey Template dropdown
- ? Custom JSON textarea
- ? Links to Survey Library and SurveyJS docs
- ? Helper text and examples

#### Added JavaScript:
- Toggle survey options on checkbox change
- Switch between library/custom sections
- Clear form on modal close
- Show/hide sections with slide animation

---

## ?? Features

### **Survey Options**
1. **No Survey** (default)
   - Creates normal manual task
   - No template associated

2. **Survey from Library**
   - Select from active survey templates
   - Dropdown shows: "Name (Category)"
   - Links to Survey Library page
   - Uses template's default mappings

3. **Custom Survey JSON**
   - Enter SurveyJS JSON directly
   - Text area with monospace font
   - Link to SurveyJS examples
   - No pre-defined mappings

---

## ?? Technical Implementation

### **Temporary Task Templates**
When a survey is selected, the system:
1. Creates a new `TaskTemplate` with:
   - Name: `[Manual] {task title}`
   - IsActive: `false` (hidden from template lists)
   - Linked to survey (via `SurveyTemplateId` or `SurveyDefinitionJson`)
2. Associates the task with this template
3. SurveyService uses the template to load survey

### **Survey Service Integration**
- Existing `SurveyService` automatically handles these tasks
- Checks for `SurveyTemplateId` first (library)
- Falls back to `SurveyDefinitionJson` (custom)
- Pre-population and output mappings work automatically

### **Data Flow**
```
User creates manual task with survey
   ?
Temporary TaskTemplate created
   ?
Task linked to temporary template
   ?
User clicks "Complete Survey"
   ?
SurveyService loads survey from template
   ?
User fills out survey
   ?
Responses saved to case (via mappings)
```

---

## ?? UI/UX Design

### **Layout**
```
Manual Entry Tab
?? Task Details (existing)
?  ?? Title
?  ?? Description
?  ?? Task Type
?  ?? Priority
?  ?? Due Date
?
?? Survey Configuration (NEW)
   ?? [Checkbox] Include a survey with this task
   ?? Options (hidden until checked)
      ?? ( ) Use Survey Library
      ?   ?? [Dropdown] Select Survey Template
      ?       ?? Link: View Survey Library
      ?
      ?? ( ) Custom Survey JSON
          ?? [Textarea] Enter SurveyJS JSON
              ?? Link: SurveyJS Examples
```

### **User Experience**
- Survey section collapsed by default
- Check box to expand options
- Smart defaults (Library selected)
- Clear visual hierarchy
- Helpful links to resources
- Form clears on modal close

---

## ?? Use Cases

### **1. Disease Investigation with Standard Survey**
```
Task: "Complete Food History Interview"
Survey: "Comprehensive Food History" (from library)
? Investigator uses standardized 72-hour recall
? Responses auto-mapped to case exposure data
```

### **2. One-Time Custom Follow-up**
```
Task: "Special Follow-up Call"
Survey: Custom JSON for specific questions
? Tailored survey for unique situation
? Responses saved to case
```

### **3. Quick Task with Symptom Check**
```
Task: "Daily Symptom Check"
Survey: "Respiratory Symptoms" (from library)
? Reusable symptom checklist
? Consistent across all cases
```

---

## ? Testing

### **Test Scenarios Covered**
- [x] Create manual task without survey
- [x] Create manual task with library survey
- [x] Create manual task with custom survey  
- [x] Toggle survey checkbox on/off
- [x] Switch between library and custom
- [x] Form clears on modal close
- [x] Build compiles successfully
- [x] Backend handler accepts parameters
- [x] Temporary template created correctly
- [x] Survey dropdown loads templates
- [x] Links to resources work

---

## ?? Files Modified

### **Backend**
1. **Surveillance-MVP/Pages/Cases/Details.cshtml.cs**
   - Added `SurveyTemplatesList` property
   - Updated `LoadTaskTemplates()` method
   - Enhanced `OnPostAddTaskManualAsync()` handler

### **Frontend**
2. **Surveillance-MVP/Pages/Cases/Details.cshtml**
   - Added Survey Configuration section
   - Added radio buttons and dropdown
   - Added custom JSON textarea
   - Added JavaScript for toggling sections

### **Documentation**
3. **MANUAL_TASK_WITH_SURVEY_GUIDE.md** (NEW)
   - Complete user guide
   - Step-by-step instructions
   - Use cases and examples
   - Testing checklist

4. **MANUAL_TASK_WITH_SURVEY_COMPLETE.md** (THIS FILE)
   - Implementation summary
   - Technical details
   - Testing results

---

## ?? How to Use

### **Quick Start**
1. Open case details page
2. Click "Add Task" ? "Manual Entry" tab
3. Fill in task details
4. Check "Include a survey with this task"
5. Select from library OR enter custom JSON
6. Click "Create Task"
7. Complete survey via "Complete Survey" button

### **For Administrators**
- Create survey templates in Survey Library
- Users can select them when creating manual tasks
- Ensures consistency across investigations

### **For Investigators**
- Quickly add tasks with surveys on-the-fly
- No need to pre-configure task templates
- Flexibility for unique situations

---

## ?? Benefits

| Benefit | Description |
|---------|-------------|
| **Flexibility** | Add surveys to manual tasks without pre-configuration |
| **Reusability** | Use existing library surveys for consistency |
| **Customization** | Create one-off surveys for special cases |
| **Simplicity** | All in one place - no template setup required |
| **Integration** | Works with existing survey and task systems |

---

## ?? Future Enhancements (Optional)

Potential future improvements:
- [ ] Survey template preview in dropdown
- [ ] JSON validation before submit
- [ ] Edit survey after task creation
- [ ] Clone task with survey
- [ ] Survey usage analytics
- [ ] Suggest surveys based on disease
- [ ] Template search/filter in dropdown
- [ ] Save custom JSON as new template

---

## ?? Success Metrics

? **Implementation**: 100% Complete
? **Build Status**: Successful
? **Testing**: All scenarios pass
? **Documentation**: Comprehensive guides provided
? **User Experience**: Intuitive and streamlined

---

## ?? Related Documentation

- **User Guide**: `MANUAL_TASK_WITH_SURVEY_GUIDE.md`
- **Survey Library**: `SURVEY_TEMPLATE_LIBRARY_COMPLETE.md`
- **Task Management**: `TASK_MANAGEMENT_UI_QUICK_REF.md`
- **Survey System**: `SURVEY_SYSTEM_100_PERCENT_COMPLETE.md`

---

## ?? Conclusion

The manual task creation feature has been successfully enhanced to support survey selection! Users can now:
- ? Add surveys from the library to manual tasks
- ? Enter custom SurveyJS JSON for one-off surveys
- ? Complete surveys just like template-based tasks
- ? Benefit from existing survey mappings and pre-population

The implementation is **production-ready** and fully integrated with existing survey and task management systems.

---

**Implementation Date**: February 7, 2026  
**Status**: ? Complete and Ready to Use  
**Build**: ? Successful  
**Hot Reload**: ? Available for testing
