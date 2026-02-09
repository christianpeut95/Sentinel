# User Autocomplete - Alternative Implementation

## Replace Current Implementation

If the modal event approach isn't working, replace the current autocomplete code with this simpler version:

### Location in File
**File:** `Surveillance-MVP/Pages/Cases/Details.cshtml`  
**Find:** The section starting with `// TASK MANAGEMENT`  
**Replace with:**

```javascript
// ========================================================================
// TASK MANAGEMENT
// ========================================================================

// Simpler approach: Initialize autocomplete on document ready
// and reinitialize when modal is shown
var userAutocompleteInitialized = false;

function initializeUserAutocomplete() {
    var $field = $("#assignedToUserAutocomplete");
    
    // Check if element exists
    if ($field.length === 0) {
        console.log('User autocomplete field not found');
        return false;
    }
    
    console.log('Initializing user autocomplete...');
    
    // Destroy existing if any
    if ($field.hasClass('ui-autocomplete-input')) {
        console.log('Destroying existing autocomplete');
        $field.autocomplete('destroy');
    }
    
    // Initialize
    $field.autocomplete({
        source: function(request, response) {
            console.log('User search initiated for:', request.term);
            
            $.ajax({
                url: "/api/users/search",
                dataType: "json",
                data: { term: request.term },
                success: function(data) {
                    console.log('User search returned ' + data.length + ' results:', data);
                    
                    if (!data || data.length === 0) {
                        response([{
                            label: 'No users found - try typing more characters',
                            value: '',
                            disabled: true
                        }]);
                        return;
                    }
                    
                    var items = data.map(function(user) {
                        return {
                            label: user.displayName || user.email,
                            value: user.displayName || user.email,
                            id: user.id
                        };
                    });
                    
                    response(items);
                },
                error: function(xhr, status, error) {
                    console.error('User search failed:', {
                        status: xhr.status,
                        statusText: xhr.statusText,
                        error: error
                    });
                    response([{
                        label: 'Error loading users - check console',
                        value: '',
                        disabled: true
                    }]);
                }
            });
        },
        minLength: 2,
        delay: 300,
        select: function(event, ui) {
            console.log('User selected:', ui.item);
            
            if (ui.item.disabled) {
                return false;
            }
            
            // Set the hidden field
            $("#assignedToUserId").val(ui.item.id);
            console.log('Hidden field set to:', ui.item.id);
            
            return true;
        },
        change: function(event, ui) {
            // If field is manually cleared or value doesn't match any item
            if (!ui.item) {
                console.log('Autocomplete cleared');
                $("#assignedToUserId").val('');
            }
        },
        focus: function(event, ui) {
            // Prevent value being inserted while navigating with keyboard
            return false;
        }
    });
    
    userAutocompleteInitialized = true;
    console.log('User autocomplete initialized successfully');
    return true;
}

// Try to initialize on page load
$(document).ready(function() {
    console.log('Document ready, checking for user autocomplete field...');
    
    // May not work if modal isn't in DOM yet, but worth trying
    initializeUserAutocomplete();
    
    // Also try when any modal is shown (generic approach)
    $('.modal').on('shown.bs.modal', function() {
        console.log('A modal was shown, checking for user autocomplete...');
        if (!userAutocompleteInitialized || $("#assignedToUserAutocomplete").length > 0) {
            initializeUserAutocomplete();
        }
    });
    
    // Specific to edit task modal
    $('#editTaskModal').on('shown.bs.modal', function() {
        console.log('Edit task modal shown, initializing user autocomplete...');
        initializeUserAutocomplete();
        
        // Focus the field after a tiny delay to ensure it's visible
        setTimeout(function() {
            if ($("#assignedToUserAutocomplete").length > 0) {
                $("#assignedToUserAutocomplete").trigger('focus');
            }
        }, 150);
    });
});
```

---

## Even Simpler: Inline Initialization

If the above still doesn't work, add this directly to the `openEditTaskModal` function:

### Find this function:
```javascript
function openEditTaskModal(taskId, status, priority, dueDate, assignedToUserId, taskName) {
    console.log('Opening edit modal for task:', taskId);
    // ... existing code ...
    $('#editTaskModal').modal('show');
}
```

### Replace with:
```javascript
function openEditTaskModal(taskId, status, priority, dueDate, assignedToUserId, taskName) {
    console.log('Opening edit modal for task:', taskId);
    console.log('Parameters:', { status, priority, dueDate, assignedToUserId });
    
    $('#editTaskId').val(taskId);
    $('#editTaskName').text(taskName);
    $('#editTaskStatus').val(status);
    $('#editTaskPriority').val(priority);
    $('#editTaskDueDate').val(dueDate);
    $('#assignedToUserId').val(assignedToUserId || '');
    $('#assignedToUserAutocomplete').val('');
    
    $('#editTaskModal').modal('show');
    
    // Initialize autocomplete after modal is shown
    setTimeout(function() {
        console.log('Initializing autocomplete after modal open...');
        
        var $field = $("#assignedToUserAutocomplete");
        
        if ($field.length === 0) {
            console.error('Autocomplete field not found!');
            return;
        }
        
        // Destroy if exists
        if ($field.hasClass('ui-autocomplete-input')) {
            $field.autocomplete('destroy');
        }
        
        // Initialize
        $field.autocomplete({
            source: function(request, response) {
                console.log('Searching:', request.term);
                $.get('/api/users/search', { term: request.term }, function(data) {
                    console.log('Results:', data);
                    response(data.map(function(u) {
                        return { label: u.displayName, value: u.displayName, id: u.id };
                    }));
                });
            },
            minLength: 2,
            select: function(event, ui) {
                console.log('Selected:', ui.item);
                $("#assignedToUserId").val(ui.item.id);
            }
        });
        
        console.log('Autocomplete initialized');
    }, 300); // Wait 300ms for modal to fully render
}
```

---

## Nuclear Option: Use Regular Dropdown

If autocomplete continues to be problematic, replace with a regular dropdown loaded on modal open:

```javascript
$('#editTaskModal').on('shown.bs.modal', function() {
    // Load users and populate dropdown
    $.get('/api/users/search?term=', function(users) {
        var $select = $('<select class="form-select" name="assignedToUserId" id="assignedToUserId"></select>');
        $select.append('<option value="">-- Select User --</option>');
        
        users.forEach(function(user) {
            $select.append('<option value="' + user.id + '">' + user.displayName + '</option>');
        });
        
        // Replace the autocomplete input
        $('#assignedToUserAutocomplete').replaceWith($select);
        
        // Set selected value if any
        var currentUserId = $('#editTaskId').data('assignedUserId');
        if (currentUserId) {
            $select.val(currentUserId);
        }
    });
});
```

---

## Test API Endpoint Directly

Before doing anything else, test if the API works:

**Open:** https://localhost:5001/api/users/search?term=test

**Expected:** JSON array with users

**If this doesn't work, the API endpoint is the problem, not JavaScript!**

To fix API endpoint, check `Program.cs`:

```csharp
// Should have this:
app.MapGet("/api/users/search", async (string term, Microsoft.AspNetCore.Identity.UserManager<ApplicationUser> userManager) =>
{
    if (string.IsNullOrWhiteSpace(term))
        return Results.Json(Array.Empty<object>());

    var users = userManager.Users
        .Where(u => u.Email != null && u.Email.Contains(term))
        .OrderBy(u => u.Email)
        .Take(20)
        .Select(u => new
        {
            Id = u.Id,
            Email = u.Email,
            DisplayName = u.Email
        })
        .ToList();

    return Results.Json(users);
});
```

---

## Quick Diagnostic Script

Add this temporarily to check everything:

```javascript
$(document).ready(function() {
    // Run diagnostics
    console.log('=== USER AUTOCOMPLETE DIAGNOSTICS ===');
    console.log('jQuery version:', $.fn.jquery);
    console.log('jQuery UI loaded:', typeof $.fn.autocomplete);
    console.log('Edit modal exists:', $('#editTaskModal').length);
    console.log('Autocomplete field exists:', $('#assignedToUserAutocomplete').length);
    
    // Test API
    console.log('Testing API endpoint...');
    $.get('/api/users/search?term=test')
        .done(function(data) {
            console.log('API works! Returned ' + data.length + ' users');
        })
        .fail(function(xhr) {
            console.error('API failed:', xhr.status, xhr.statusText);
        });
    
    // Test when modal opens
    $('#editTaskModal').on('shown.bs.modal', function() {
        console.log('=== MODAL OPENED ===');
        console.log('Field now exists:', $('#assignedToUserAutocomplete').length);
        console.log('Field is visible:', $('#assignedToUserAutocomplete').is(':visible'));
    });
});
```

---

*Alternative Implementation Guide*  
*Last Updated: February 6, 2026*
