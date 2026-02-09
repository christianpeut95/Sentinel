# My Tasks Dashboard - Quick Reference ??

## Access
**URL:** `/Dashboard/MyTasks`  
**Navigation:** Sidebar ? My Tasks

---

## Quick Actions

### ? Start Task
**When:** Task is Pending  
**What:** Changes status to "In Progress"  
**How:** Click play button

### ? Complete Task
**When:** Task is Pending or In Progress  
**What:** Marks task as Complete  
**How:** Click check button ? Add notes ? Submit

### ? Cancel Task
**When:** Task not completed or cancelled  
**What:** Marks task as Cancelled  
**How:** Click X button ? Add reason ? Submit

### ? View Case
**When:** Anytime  
**What:** Opens case details page  
**How:** Click arrow button or case ID link

---

## Filters

### Status
- **All** - Show everything
- **Active** - Pending + In Progress
- **Pending** - Not started
- **In Progress** - Currently working
- **Completed** - Finished
- **Overdue** - Past due date
- **Cancelled** - Cancelled tasks

### Priority
- **Urgent** ?? - Immediate action
- **High** ?? - Important
- **Medium** ? - Normal
- **Low** ?? - When possible

### Due Date
- **Overdue** - Past due
- **Due Today** - Due on current date
- **Due Soon** - Next 3 days
- **This Week** - Next 7 days
- **No Due Date** - No deadline

---

## Sorting

### Options
- **Due Date** - Earliest first (or latest)
- **Priority** - Urgent first (or lowest)
- **Status** - Pending first (or completed)
- **Case** - By case ID alphabetically
- **Created** - Newest first (or oldest)

### How to Sort
Click column header to sort  
Click again to reverse direction  
Chevron shows current direction (??)

---

## Statistics

### Total Tasks
Count of all your assigned tasks

### Pending
Tasks not yet started (Status = Pending)

### Overdue
Tasks past due date and not completed  
Shows count of urgent overdue tasks

### Completed
Tasks marked as complete  
Shows % of total tasks completed

---

## Color Guide

### Row Colors
- **Red Background** = Overdue task
- **Yellow Background** = Due today
- **White Background** = Normal

### Priority Badges
- **Red** = Urgent
- **Yellow** = High
- **Gray** = Medium
- **Blue** = Low

### Status Badges
- **Gray** = Pending
- **Blue** = In Progress
- **Green** = Completed
- **Red** = Overdue
- **Dark** = Cancelled
- **Yellow** = Waiting for Patient

---

## Keyboard Tips

| Action | Keys |
|--------|------|
| Refresh page | F5 |
| Close modal | ESC |
| Submit form | ENTER (in modal) |
| Navigate table | TAB |

---

## Common Workflows

### Complete a Pending Task
```
1. Find task in list
2. Click ? button
3. Add notes (optional)
4. Click "Mark Complete"
Done! ?
```

### Find Overdue Tasks
```
1. Due Date filter: "Overdue"
2. Review red-highlighted tasks
3. Start or complete them
```

### View Tasks Due Today
```
1. Due Date filter: "Due Today"
2. See yellow-highlighted tasks
3. Prioritize your day
```

### Find Urgent Tasks
```
1. Priority filter: "Urgent"
2. Sort by: "Due Date"
3. Work on earliest first
```

---

## Tips & Tricks

### ?? Pro Tip 1: Quick Triage
Filter by "Active" status + Sort by "Due Date"  
Work through tasks in order

### ?? Pro Tip 2: Daily Review
Start day with "Due Today" filter  
Plan your task list

### ?? Pro Tip 3: Weekly Planning
Use "This Week" filter  
See upcoming workload

### ?? Pro Tip 4: Focus Mode
Filter by "Urgent" + "High"  
Hide less important tasks

### ?? Pro Tip 5: Progress Tracking
Check statistics cards daily  
Watch completion % grow

---

## Empty States

### No Tasks Assigned
"No tasks assigned to you yet."  
? Tasks appear when assigned by system or user

### No Filtered Results
"No tasks match the selected filters."  
? Click "Clear Filters" to reset

---

## URL Parameters

Build custom links:

```
/Dashboard/MyTasks?StatusFilter=Pending
/Dashboard/MyTasks?PriorityFilter=Urgent
/Dashboard/MyTasks?DueDateFilter=Overdue
/Dashboard/MyTasks?SortBy=Priority&SortOrder=desc
```

Combine multiple:
```
/Dashboard/MyTasks?StatusFilter=Active&PriorityFilter=Urgent&SortBy=DueDate
```

---

## Mobile Usage

**Responsive Design:**
- Statistics cards stack vertically
- Filters in dropdown menus
- Action buttons stay visible
- Table scrolls horizontally

**Best on:**
- Desktop: Full feature set
- Tablet: Optimized layout
- Mobile: Core functionality

---

## Permissions Required

**View Page:**
- Must be authenticated user

**Quick Actions:**
- Requires: `Permission.Case.Edit`
- Check with admin if actions disabled

---

## Need Help?

**Page not loading?**
? Check authentication

**No tasks showing?**
? Tasks must be assigned to your user

**Filters not working?**
? Refresh page, clear browser cache

**Actions failing?**
? Check permissions with admin

**Statistics incorrect?**
? Refresh page (F5)

---

*Quick Reference Guide*  
*Last Updated: February 6, 2026*  
*Version: 1.0*
