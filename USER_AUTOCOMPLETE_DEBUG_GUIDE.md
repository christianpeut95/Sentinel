# User Autocomplete Debugging Guide ??

## Current Status
- ? Form saves changes successfully
- ? User dropdown autocomplete not appearing

---

## Step-by-Step Debugging

### Step 1: Check Browser Console
Open browser DevTools (F12) and look for:

#### Expected Console Output (When Working):
```
Edit task modal opened, initializing autocomplete...
Searching users for: jo
User search results: [{id: "...", email: "john@...", displayName: "john@..."}]
```

#### Common Error Messages:
```
? $.fn.autocomplete is not a function
   ? jQuery UI not loaded

? User search error: 404
   ? API endpoint doesn't exist

? User search results: []
   ? No users in database or search term doesn't match

? Uncaught TypeError: Cannot read property 'autocomplete'
   ? Element doesn't exist in DOM
```

### Step 2: Verify jQuery UI is Loaded

**In Console, type:**
```javascript
typeof $.fn.autocomplete
```

**Expected:** `"function"`  
**If:** `"undefined"` ? jQuery UI not loaded

**Check in Network tab:**
- Look for: `jquery-ui.min.js`
- Status should be: 200 OK
- If 404: Library not loading

### Step 3: Verify Element Exists

**In Console, type:**
```javascript
$('#assignedToUserAutocomplete').length
```

**Expected:** `1`  
**If:** `0` ? Element doesn't exist (check if modal is open)

**After opening Edit Task modal, type:**
```javascript
$('#assignedToUserAutocomplete').length
```

**Should be:** `1`

### Step 4: Verify API Endpoint

**In Console, type:**
```javascript
$.get('/api/users/search?term=test', function(data) {
    console.log('API Response:', data);
});
```

**Expected Response:**
```json
[
  {"id": "user-guid", "email": "test@example.com", "displayName": "test@example.com"}
]
```

**If empty array `[]`:**
- No users match "test"
- Try: `$.get('/api/users/search?term=a')`

**If 404 error:**
- API endpoint not registered
- Check Program.cs for `/api/users/search`

### Step 5: Manual Autocomplete Test

**After opening Edit Task modal, type in console:**
```javascript
$("#assignedToUserAutocomplete").autocomplete({
    source: [
        { label: "Test User 1", value: "Test User 1", id: "test1" },
        { label: "Test User 2", value: "Test User 2", id: "test2" }
    ],
    minLength: 0,
    select: function(event, ui) {
        console.log('Selected:', ui.item);
        $("#assignedToUserId").val(ui.item.id);
    }
});

// Then click in the field to trigger
$("#assignedToUserAutocomplete").autocomplete("search", "");
```

**If dropdown appears:** jQuery UI works, issue is with AJAX source  
**If nothing happens:** jQuery UI not working properly

---

## Common Issues & Fixes

### Issue 1: jQuery UI Not Loading
**Symptom:** `$.fn.autocomplete is not a function`

**Check:**
```html
<!-- Should be in @section Scripts -->
<script src="https://code.jquery.com/ui/1.13.2/jquery-ui.min.js"></script>
```

**Fix:** Ensure jQuery UI is loaded AFTER jQuery and BEFORE your script

### Issue 2: Modal Event Not Firing
**Symptom:** No console log "Edit task modal opened..."

**Test:**
```javascript
// Run this in console, then open modal
$('#editTaskModal').on('shown.bs.modal', function() {
    alert('Modal opened!');
});
```

**If alert doesn't show:** Bootstrap modal events not working

**Alternative Fix:** Initialize on page load instead:
```javascript
$(document).ready(function() {
    // Initialize immediately, even if modal hidden
    $("#assignedToUserAutocomplete").autocomplete({
        // ... config
    });
});
```

### Issue 3: Element Hidden/Not Accessible
**Symptom:** Element exists but autocomplete doesn't attach

**Check:**
```javascript
$('#assignedToUserAutocomplete').is(':visible')
```

**If false:** Element is hidden, autocomplete might not initialize

**Fix:** Add a delay:
```javascript
$('#editTaskModal').on('shown.bs.modal', function() {
    setTimeout(function() {
        $("#assignedToUserAutocomplete").autocomplete({
            // ... config
        });
    }, 100); // 100ms delay
});
```

### Issue 4: API Returns Empty
**Symptom:** "User search results: []"

**Check Database:**
```sql
SELECT Id, Email FROM AspNetUsers
```

**If empty:** No users exist

**Create Test User:**
- Go to Register page
- Create a test account
- Or run SQL:
```sql
-- Check existing users
SELECT COUNT(*) FROM AspNetUsers;
```

### Issue 5: Search Term Too Strict
**Symptom:** Autocomplete works but shows "No users found"

**Try typing the FULL email address** instead of partial

**Or Lower minLength:**
```javascript
minLength: 1,  // Was: 2
```

---

## Quick Fixes to Try

### Fix 1: Simpler Initialization (Fallback)
Replace the modal event approach with direct initialization:

```javascript
$(document).ready(function() {
    // Initialize on page load, not modal open
    function initUserAutocomplete() {
        if ($("#assignedToUserAutocomplete").length === 0) {
            console.log('Element not found, will retry...');
            return;
        }
        
        console.log('Initializing user autocomplete...');
        $("#assignedToUserAutocomplete").autocomplete({
            source: function(request, response) {
                console.log('Searching for:', request.term);
                $.ajax({
                    url: "/api/users/search",
                    data: { term: request.term },
                    success: function(data) {
                        console.log('Results:', data);
                        response(data.map(function(item) {
                            return {
                                label: item.displayName,
                                value: item.displayName,
                                id: item.id
                            };
                        }));
                    }
                });
            },
            minLength: 2
        });
    }
    
    // Try to initialize
    initUserAutocomplete();
    
    // Retry when modal opens
    $('#editTaskModal').on('shown.bs.modal', function() {
        initUserAutocomplete();
    });
});
```

### Fix 2: Focus Trigger
Sometimes autocomplete needs focus to activate:

```javascript
$('#editTaskModal').on('shown.bs.modal', function() {
    // Initialize
    $("#assignedToUserAutocomplete").autocomplete({ /* config */ });
    
    // Focus the field
    $("#assignedToUserAutocomplete").focus();
});
```

### Fix 3: Use Select2 Instead
If jQuery UI continues to be problematic, use Select2 (already loaded on page):

```javascript
$("#assignedToUserAutocomplete").select2({
    ajax: {
        url: '/api/users/search',
        dataType: 'json',
        delay: 250,
        data: function(params) {
            return { term: params.term };
        },
        processResults: function(data) {
            return {
                results: data.map(function(item) {
                    return { id: item.id, text: item.displayName };
                })
            };
        }
    },
    minimumInputLength: 2,
    placeholder: 'Type to search users...'
});
```

---

## Testing Procedure

### 1. Open Case Details Page
- Navigate to any case
- Open browser console (F12)

### 2. Click Edit on a Task
- Watch console for: "Edit task modal opened..."
- If you DON'T see this ? Modal event not firing

### 3. Type in "Assign To User" Field
- Type 2+ characters
- Watch console for: "Searching users for: xx"
- If you DON'T see this ? Autocomplete not initialized

### 4. Check Network Tab
- Type in field
- Look for request to: `/api/users/search?term=xx`
- If NO request ? AJAX not firing
- If 404 error ? Endpoint doesn't exist
- If 200 OK ? Check response body for data

### 5. Check Response
- In Network tab, click on the request
- Check "Response" tab
- Should see JSON array with users
- If empty `[]` ? No users match search term

---

## What to Report

If still not working, provide:

1. **Console Output:**
   - Any errors?
   - What logs appear when you type?

2. **Network Tab:**
   - Does `/api/users/search` request appear?
   - What's the status code?
   - What's the response body?

3. **Element Check:**
   ```javascript
   // Run these in console:
   console.log('jQuery loaded:', typeof $);
   console.log('jQuery UI loaded:', typeof $.fn.autocomplete);
   console.log('Element exists:', $('#assignedToUserAutocomplete').length);
   console.log('Element visible:', $('#assignedToUserAutocomplete').is(':visible'));
   ```

4. **Modal Check:**
   ```javascript
   // Run this, then open modal:
   $('#editTaskModal').on('shown.bs.modal', function() {
       console.log('*** MODAL OPENED ***');
   });
   ```

---

## Nuclear Option: Hardcoded Test

If nothing works, try this simple hardcoded dropdown:

```javascript
$(document).ready(function() {
    $('#editTaskModal').on('shown.bs.modal', function() {
        $("#assignedToUserAutocomplete").autocomplete({
            source: ["user1@example.com", "user2@example.com", "test@example.com"],
            minLength: 0
        });
        
        // Trigger on focus
        $("#assignedToUserAutocomplete").on('focus', function() {
            $(this).autocomplete("search", "");
        });
    });
});
```

**If this works:** AJAX/API is the problem  
**If this doesn't work:** jQuery UI or element access is the problem

---

## Expected Working Flow

```
1. User clicks Edit button
   ?
2. openEditTaskModal() called
   ??> Sets form values
   ??> Opens modal: $('#editTaskModal').modal('show')
   ?
3. Bootstrap fires: 'shown.bs.modal' event
   ?
4. Event handler runs
   ??> console.log('Edit task modal opened...')
   ??> Destroys old autocomplete
   ??> Creates new autocomplete with AJAX source
   ?
5. User types in field (2+ chars)
   ?
6. Autocomplete source function called
   ??> console.log('Searching users for: xx')
   ??> AJAX GET to /api/users/search?term=xx
   ?
7. Server returns JSON
   ?
8. Success callback runs
   ??> console.log('User search results:', data)
   ??> Maps data to autocomplete format
   ??> Calls response() with items
   ?
9. jQuery UI shows dropdown
   ?
10. User clicks item
    ?
11. Select callback runs
    ??> console.log('User selected:', item)
    ??> Sets hidden field: $("#assignedToUserId").val(item.id)
```

**Where is YOUR flow breaking?**

---

*Debugging Guide*  
*Last Updated: February 6, 2026*
