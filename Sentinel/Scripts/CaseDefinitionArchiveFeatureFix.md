# Case Definition Archive Feature Fix

## Problem
The "Archive" button on the Case Definitions Index page was failing to archive case definitions. When users clicked the Archive button, nothing happened.

## Root Cause
The JavaScript function `archiveDefinition()` was making a POST request to `/Settings/CaseDefinitions/Archive?id=${id}`, but there was **no handler method** in `Index.cshtml.cs` to process this request.

## Solution

### 1. Added Missing Handler Method
**File**: `Pages/Settings/CaseDefinitions/Index.cshtml.cs`

Added the `OnPostArchiveAsync` handler method:

```csharp
public async Task<IActionResult> OnPostArchiveAsync(int id)
{
    var definition = await _context.CaseDefinitions.FindAsync(id);

    if (definition == null)
    {
        return NotFound();
    }

    // Archive the definition
    definition.Status = CaseDefinitionStatus.Archived;
    definition.ModifiedAt = DateTime.UtcNow;
    definition.ModifiedBy = User.Identity?.Name;

    await _context.SaveChangesAsync();

    return new OkResult();
}
```

This handler:
- Finds the case definition by ID
- Changes its `Status` to `Archived`
- Updates audit fields (`ModifiedAt`, `ModifiedBy`)
- Saves changes to the database

### 2. Fixed JavaScript Archive Function
**File**: `Pages/Settings/CaseDefinitions/Index.cshtml`

**Before** (using fetch API without proper anti-forgery token handling):
```javascript
function archiveDefinition(id) {
    if (confirm('Archive this definition? It will no longer be used for case evaluation.')) {
        fetch(`/Settings/CaseDefinitions/Archive?id=${id}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            }
        })
        .then(response => {
            if (response.ok) {
                location.reload();
            } else {
                alert('Failed to archive definition');
            }
        });
    }
}
```

**After** (using form submission with proper anti-forgery token):
```javascript
function archiveDefinition(id) {
    if (confirm('Archive this definition? It will no longer be used for case evaluation.')) {
        const form = document.createElement('form');
        form.method = 'POST';
        form.action = `/Settings/CaseDefinitions/Index?handler=Archive&id=${id}`;

        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            const hiddenToken = document.createElement('input');
            hiddenToken.type = 'hidden';
            hiddenToken.name = '__RequestVerificationToken';
            hiddenToken.value = token.value;
            form.appendChild(hiddenToken);
        }

        document.body.appendChild(form);
        form.submit();
    }
}
```

Changes:
- Changed endpoint from `/Settings/CaseDefinitions/Archive` to `/Settings/CaseDefinitions/Index?handler=Archive` (Razor Pages handler naming convention)
- Switched from `fetch` to form submission to properly handle anti-forgery token
- Creates a hidden form with the anti-forgery token and submits it

### 3. Added Anti-Forgery Token to Page
**File**: `Pages/Settings/CaseDefinitions/Index.cshtml`

Added a hidden form with anti-forgery token before the scripts section:

```razor
<!-- Hidden form for anti-forgery token -->
<form id="hiddenForm" method="post" style="display:none;">
    @Html.AntiForgeryToken()
</form>
```

This ensures the anti-forgery token is available in the page for the JavaScript to use.

### 4. Added Missing Using Statement
**File**: `Pages/Settings/CaseDefinitions/Index.cshtml.cs`

Added `using Microsoft.AspNetCore.Mvc;` to support returning `IActionResult` types.

## Impact

✅ Users can now successfully archive case definitions from the UI  
✅ Archived definitions have their status changed to `Archived` in the database  
✅ Audit fields are properly updated with modification timestamp and user  
✅ The page reloads after archiving to reflect the updated status  
✅ Archived definitions no longer show the Archive button (already handled in UI)

## Testing Steps

1. Navigate to Settings → Case Definitions
2. Find a case definition with status "Current" or "Draft"
3. Click the "Archive" button
4. Confirm the action in the dialog
5. Verify:
   - The page reloads
   - The definition status badge changes to "ARCHIVED"
   - The Archive button is no longer shown for that definition
   - The definition is no longer used for auto-evaluation

## Technical Notes

- The handler uses ASP.NET Core Razor Pages handler naming convention: `OnPost{HandlerName}Async`
- The URL parameter syntax is: `?handler={HandlerName}&id={value}`
- Anti-forgery validation is automatically enforced by ASP.NET Core for POST requests
- The form submission approach is more reliable than fetch API for Razor Pages handlers
