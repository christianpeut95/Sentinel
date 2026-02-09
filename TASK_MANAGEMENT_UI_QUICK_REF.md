# Task Management UI - Quick Reference ??

## Task Actions Available

### ? Complete Task
**Button:** Green check icon  
**When:** Task is Pending or In Progress  
**What it does:**
- Opens modal to add completion notes
- Sets status to "Completed"
- Records who completed it and when

### ?? Edit Task  
**Button:** Blue pencil icon  
**When:** Always available  
**What it does:**
- Change status (Pending, In Progress, Completed, etc.)
- Change priority (Low, Medium, High, Urgent)
- Change due date
- Assign to specific user (with search)

### ?? Cancel Task
**Button:** Yellow X icon  
**When:** Task not already cancelled  
**What it does:**
- Opens modal to add cancellation reason
- Sets status to "Cancelled"
- Task no longer counts as active

### ??? Delete Task
**Button:** Red trash icon  
**When:** Always available  
**What it does:**
- Shows confirmation dialog
- Permanently removes task from database
- Cannot be undone

---

## User Assignment

### How to Assign a Task to a User
1. Click **Edit** button on task
2. In "Assign To User" field, type email (min 2 chars)
3. Select user from dropdown
4. Click **Save Changes**

### User Search
- Searches by email address
- Shows up to 20 results
- Requires at least 2 characters

---

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Open modal | Click button |
| Close modal | ESC or click X |
| Submit form | ENTER (when in modal) |
| Confirm delete | ENTER (in confirmation) |

---

## Status Meanings

| Status | Meaning | Badge Color |
|--------|---------|-------------|
| Pending | Not started | Grey |
| In Progress | Being worked on | Blue |
| Completed | Finished | Green |
| Cancelled | No longer needed | Dark |
| Overdue | Past due date | Red |
| Waiting for Patient | Blocked | Yellow |

---

## Priority Meanings

| Priority | When to Use | Badge Color |
|----------|-------------|-------------|
| Low | Can wait | Blue |
| Medium | Normal importance | Grey |
| High | Important | Yellow |
| Urgent | Immediate action needed | Red |

---

## Tips

### Best Practices
- ? Add completion notes when marking complete
- ? Provide cancellation reason when cancelling
- ? Assign urgent tasks to specific users
- ? Update status as work progresses
- ? Set realistic due dates

### Things to Avoid
- ? Don't delete tasks without good reason (cancel instead)
- ? Don't skip completion notes for complex tasks
- ? Don't leave tasks in "In Progress" indefinitely
- ? Don't assign tasks to wrong users

---

## Common Workflows

### Workflow 1: Complete a Simple Task
```
1. Click Complete ?
2. (Optional) Add notes
3. Click "Mark Complete"
Done! ?
```

### Workflow 2: Reassign an Urgent Task
```
1. Click Edit ??
2. Change Priority to "Urgent"
3. Search and select user
4. Click "Save Changes"
Done! ?
```

### Workflow 3: Cancel Unnecessary Task
```
1. Click Cancel ??
2. Enter reason (e.g., "Patient recovered")
3. Click "Cancel Task"
Done! ?
```

---

## Permissions Required

All task management requires:
- **Case Edit** permission
- Access to the case's disease

---

## What Gets Logged

All actions are audited:
- ? Task completed - who, when, notes
- ? Task updated - what changed
- ? Task cancelled - who, why
- ? Task deleted - what was deleted

Check **Audit Log** to see task history.

---

## Quick Troubleshooting

**Q: Can't see Complete button?**  
A: Task is already completed or cancelled

**Q: User search not working?**  
A: Type at least 2 characters

**Q: Changes not saving?**  
A: Check you have Case Edit permission

**Q: Delete not working?**  
A: Make sure to click OK in confirmation dialog

---

## Need Help?

Check the full documentation: `TASK_MANAGEMENT_UI_COMPLETE.md`

---

*Quick Reference Guide*  
*Last Updated: February 6, 2026*
