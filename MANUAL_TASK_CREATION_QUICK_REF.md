# Manual Task Creation - Quick Reference ??

## How to Add a Task

### Method 1: From Template (Recommended)
```
1. Go to Case Details
2. Click "Add Task" button (top right of Tasks section)
3. Select a template from the list
4. Click "Add Selected Task"
Done! ?
```

**Pros:**
- ? Fast - One click
- ?? Pre-configured (title, description, instructions, priority)
- ?? Smart due dates (auto-calculated)
- ?? Disease-specific

**When to Use:**
- Standard disease procedures
- Templates available
- Need quick creation

---

### Method 2: Manual Entry
```
1. Go to Case Details
2. Click "Add Task" button
3. Click "Manual Entry" tab
4. Fill in:
   - Title
   - Description (optional)
   - Task Type
   - Priority
   - Due Date (optional)
5. Click "Create Task"
Done! ?
```

**Pros:**
- ?? Full control
- ?? Custom content
- ?? Custom due date
- ? Always available

**When to Use:**
- No suitable template
- One-off tasks
- Custom requirements

---

## Task Details

### Automatic Defaults
Both methods automatically set:
- **Assigned To:** You (current user)
- **Status:** Pending
- **Created:** Current timestamp

### Can Change Later
You can edit after creation:
- Reassign to another user
- Change priority
- Update due date
- Update status
- Add completion notes

---

## Template Selection Guide

### How Templates Are Shown
Templates are filtered by:
- ? Case's disease
- ? Template is active
- ? Trigger type matches

### Template Display
Each template shows:
- **Name** - Task title
- **Description** - What it's about
- **Instructions** - How to complete
- **Priority** - Low/Medium/High/Urgent (color-coded)
- **Type** - Task category

### Priority Colors
- ?? **Red** = Urgent
- ?? **Yellow** = High
- ? **Gray** = Medium
- ?? **Blue** = Low

---

## Due Date Calculation

### Template Tasks
Due date calculated from:
- **Symptom Onset** - DateOfOnset + X days
- **Notification Date** - DateOfNotification + X days
- **Task Creation** - Today + X days

Example:
```
Template: "Isolation" 
Due: 4 days from symptom onset
Case onset: Feb 1
? Task due date: Feb 5
```

### Manual Tasks
You pick the due date directly.

**No due date?** Leave blank - task still valid.

---

## Quick Workflows

### Workflow 1: Measles Case Investigation
```
Scenario: New measles case needs standard tasks

1. Create/open measles case
2. Click "Add Task"
3. See templates:
   ? Measles Isolation (High)
   ? Contact Tracing (Urgent)
   ? Rash Assessment (Medium)
4. Select "Measles Isolation"
5. Click "Add Selected Task"

Result:
? Isolation task created
? Due in 4 days from onset
? Assigned to me
? High priority
? Instructions included
```

### Workflow 2: Custom Follow-up
```
Scenario: Need custom follow-up task

1. Open case
2. Click "Add Task"
3. Click "Manual Entry" tab
4. Enter:
   Title: "Follow-up phone call"
   Description: "Check recovery progress"
   Type: "Follow-up"
   Priority: "Medium"
   Due: Tomorrow
5. Click "Create Task"

Result:
? Custom task created
? Due tomorrow
? Assigned to me
? Medium priority
```

### Workflow 3: No Templates Available
```
Scenario: Rare disease, no templates configured

1. Open case
2. Click "Add Task"
3. See "No templates available"
4. Click "Manual Entry" tab
5. Create custom task
6. Submit

Result:
? Task created
? Manual entry always works
```

---

## Field Guide

### Required Fields (*)
**Template Method:**
- Radio button selection

**Manual Method:**
- Title
- Task Type
- Priority

### Optional Fields
- Description
- Due Date
- Instructions (template only)

### Field Limits
- **Title:** 200 characters
- **Description:** No limit
- **Due Date:** Any future date

---

## Tips & Tricks

### ?? Tip 1: Use Templates First
Always check templates first - faster and more consistent.

### ?? Tip 2: Review Template Details
Read instructions before adding - understand what's required.

### ?? Tip 3: Custom for Edge Cases
Use manual entry for:
- One-off situations
- Non-standard procedures
- Follow-ups

### ?? Tip 4: Add Multiple
Add several templates at once by:
- Adding one
- Clicking "Add Task" again
- Selecting next template

### ?? Tip 5: Edit After Creation
Don't stress about perfection - can edit later via:
- Edit button (pencil icon)
- Change any field
- Reassign

---

## Common Scenarios

### Q: Can I add task without a disease?
**A:** Templates won't show, but manual entry works.

### Q: Can I assign to someone else during creation?
**A:** Not yet - creates as yours. Edit after to reassign.

### Q: What if no templates show?
**A:** Use Manual Entry tab - always available.

### Q: Can I create recurring tasks?
**A:** Not from this UI yet. Use templates with recurrence configured.

### Q: Can I add multiple at once?
**A:** Not yet - add one at a time. Quick with templates though!

### Q: Do I have to set due date?
**A:** No - optional. Templates auto-calculate if configured.

---

## Keyboard Shortcuts

| Action | Keys |
|--------|------|
| Open modal | Click button |
| Switch tabs | Click tab |
| Submit form | ENTER (in form) |
| Close modal | ESC |
| Navigate fields | TAB |

---

## Troubleshooting

### Button Not Visible
? Need `Case.Edit` permission  
? Check with admin

### No Templates Showing
? Disease has no templates configured  
? Use Manual Entry tab

### Can't Submit Template
? Must select a radio button  
? Check one is selected

### Can't Submit Manual
? Check required fields (*)  
? Title, Type, Priority needed

### Task Not Appearing
? Refresh page (F5)  
? Check for error message

---

## Success Indicators

**You know it worked when:**
1. ? Modal closes
2. ? Green success message appears
3. ? Task appears in task list
4. ? Task shows "Assigned to: You"
5. ? Task status = Pending

---

## Next Steps After Creating

**What to do with your new task:**
1. **Start** - Change status to "In Progress"
2. **Complete** - Mark as done with notes
3. **Edit** - Modify details if needed
4. **View Case** - See full case context
5. **Go to My Tasks** - See all your tasks

---

*Quick Reference Guide*  
*Last Updated: February 6, 2026*  
*Version: 1.0*
