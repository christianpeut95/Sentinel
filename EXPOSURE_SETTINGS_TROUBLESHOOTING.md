# Exposure Settings Not Saving - Troubleshooting Guide

## Latest Fix Applied

### Added formaction Attribute to Button
**File:** `Surveillance-MVP\Pages\Settings\Diseases\Edit.cshtml`

Changed the submit button from:
```html
<button type="submit" class="btn btn-primary">
```

To:
```html
<button type="submit" class="btn btn-primary" 
        formaction="?handler=SaveExposureTracking" 
        formmethod="post">
```

This explicitly tells the browser to call the SaveExposureTracking handler, overriding the form's asp-page-handler attribute if there's any conflict.

## If Still Not Working - Debugging Steps

### Step 1: Check Browser Developer Tools
1. Open browser Dev Tools (F12)
2. Go to Network tab
3. Click "Save Exposure Settings"
4. Look for the POST request
5. Check the request URL - should contain: `?handler=SaveExposureTracking`
6. Check Form Data - verify all fields are being sent

### Step 2: Add Breakpoint in Handler
1. Open `Edit.cshtml.cs`
2. Set breakpoint on line: `public async Task<IActionResult> OnPostSaveExposureTrackingAsync()`
3. Click "Save Exposure Settings"
4. If breakpoint doesn't hit ? handler not being called
5. If breakpoint hits ? check what values are in Disease object

### Step 3: Check TempData Messages
After clicking save, check if there's a message in TempData:
- Success: "Exposure tracking settings have been updated successfully"
- Error: Should show validation errors or exception message

### Step 4: Check Database Directly
Run this query to see current values:
```sql
SELECT 
    Id,
    Name,
    ExposureTrackingMode,
    DefaultToResidentialAddress,
    AlwaysPromptForLocation,
    ExposureGuidanceText,
    ModifiedAt
FROM Diseases
WHERE Name = 'Your Disease Name'
```

### Step 5: Verify Handler Signature
The handler method name must match exactly:
- Form: `asp-page-handler="SaveExposureTracking"`
- Button: `formaction="?handler=SaveExposureTracking"`
- Method: `public async Task<IActionResult> OnPostSaveExposureTrackingAsync()`

Note: "SaveExposureTracking" becomes "OnPostSaveExposureTrackingAsync"

## Common Issues & Solutions

### Issue 1: App is Running (Debug Mode)
**Symptom:** Changes don't apply
**Solution:** 
- Stop debugging (Shift+F5)
- Rebuild solution
- Start debugging again

### Issue 2: Hot Reload Not Working
**Symptom:** Code changes don't reflect
**Solution:**
- Stop app completely
- Clean solution
- Rebuild
- Run

### Issue 3: Browser Cache
**Symptom:** Old page loads
**Solution:**
- Hard refresh: Ctrl+Shift+R or Ctrl+F5
- Clear browser cache
- Open in incognito/private window

### Issue 4: ModelState Validation Failure
**Symptom:** Form doesn't submit, no feedback
**Solution:**
- Check browser console for validation errors
- Ensure all required Disease properties have hidden fields:
  - Disease.Id ?
  - Disease.Name ?
  - Disease.Code ?
  - Disease.ExportCode ?
- Handler removes validation for these:
  ```csharp
  ModelState.Remove(nameof(Disease.Name));
  ModelState.Remove(nameof(Disease.Code));
  ModelState.Remove(nameof(Disease.ExportCode));
  ModelState.Remove(nameof(Disease.PathIds));
  ModelState.Remove(nameof(Disease.DiseaseCategoryId));
  ModelState.Remove(nameof(Disease.ParentDiseaseId));
  ```

### Issue 5: Wrong Handler Being Called
**Symptom:** Basic Info saves instead
**Solution:**
- Check button has `formaction="?handler=SaveExposureTracking"`
- Verify form has `asp-page-handler="SaveExposureTracking"`
- Ensure handler method exists: `OnPostSaveExposureTrackingAsync()`

### Issue 6: Entity Framework Tracking Issue
**Symptom:** Changes don't persist to database
**Solution:**
- Handler now loads fresh entity:
  ```csharp
  var existingDisease = await _context.Diseases.FindAsync(Disease.Id);
  // Update properties
  await _context.SaveChangesAsync();
  ```

### Issue 7: Transaction Rollback
**Symptom:** Saves but doesn't persist
**Solution:**
- Check for exceptions in catch block
- Verify `SaveChangesAsync()` is called
- Check database constraints aren't violated

## Manual Test Procedure

1. **Navigate to Edit Page**
   - Settings > Diseases > Click "Edit" on any disease

2. **Click Exposure Tracking Tab**
   - Should see all exposure settings

3. **Change Settings**
   - Change ExposureTrackingMode dropdown
   - Check/uncheck a checkbox
   - Enter text in Guidance field

4. **Click Save**
   - Click "Save Exposure Settings" button
   - **STOP** - Check network request in Dev Tools

5. **Verify Success Message**
   - Should see green success alert at top
   - Message: "Exposure tracking settings have been updated successfully"

6. **Verify Redirect**
   - Should redirect back to same page
   - Tab should stay on Exposure Tracking (localStorage)

7. **Verify Changes Persist**
   - Refresh page (F5)
   - Navigate to different disease then back
   - Changes should still be there

## Advanced Debugging

### Enable Detailed Logging
Add to handler:
```csharp
public async Task<IActionResult> OnPostSaveExposureTrackingAsync()
{
    System.Diagnostics.Debug.WriteLine("=== SaveExposureTracking Handler Called ===");
    System.Diagnostics.Debug.WriteLine($"Disease.Id: {Disease.Id}");
    System.Diagnostics.Debug.WriteLine($"Disease.Name: {Disease.Name}");
    System.Diagnostics.Debug.WriteLine($"ExposureTrackingMode: {Disease.ExposureTrackingMode}");
    
    // ... rest of handler
}
```

Check Output window in Visual Studio for these messages.

### Check Form Data Being Sent
Add JavaScript to log form submission:
```javascript
$('form[asp-page-handler="SaveExposureTracking"]').on('submit', function() {
    console.log('Form submitting to SaveExposureTracking');
    console.log('Form data:', $(this).serialize());
    return true; // Allow form to submit
});
```

### Verify Antiforgery Token
Check that the form includes antiforgery token:
```html
<form method="post" asp-page-handler="SaveExposureTracking">
    @* This should be automatically added: *@
    <input name="__RequestVerificationToken" type="hidden" value="..." />
    
    @* Your form fields *@
</form>
```

## What Should Happen

### Successful Save Flow:
1. User clicks "Save Exposure Settings"
2. Browser sends POST to: `/Settings/Diseases/Edit/{id}?handler=SaveExposureTracking`
3. Handler `OnPostSaveExposureTrackingAsync()` is invoked
4. ModelState validation passes (or required fields are removed)
5. Existing disease loaded from database
6. Exposure properties updated
7. `SaveChangesAsync()` persists to database
8. TempData["SuccessMessage"] is set
9. Redirects to: `/Settings/Diseases/Edit/{id}`
10. Page loads with success message
11. localStorage keeps Exposure tab active

### If It Fails:
- Check TempData["ErrorMessage"] for details
- Validation errors logged to TempData
- Page stays on same tab
- All data reloaded (CustomFields, Symptoms, etc.)
- User can see what went wrong

## Current Implementation Status

### ? Completed
- Handler method exists: `OnPostSaveExposureTrackingAsync()`
- Hidden fields added for required properties
- ModelState validation removed for non-edited properties
- Error message logging added
- Data reloading on error
- formaction attribute on button

### ?? To Verify
- Stop debugging and restart app
- Clear browser cache
- Test in incognito window
- Check database after save

## Files Modified

1. `Edit.cshtml` - Added hidden fields + formaction
2. `Edit.cshtml.cs` - Enhanced handler with validation removal

## Next Steps if Still Failing

1. **Try Different Browser** - Rule out browser-specific issues
2. **Check IIS Express** - Restart IIS Express
3. **Check Antiforgery** - Temporarily disable antiforgery validation
4. **Simplify Handler** - Remove validation checks temporarily
5. **Use Fiddler/Postman** - Test POST request manually

## Contact Points for Help

- Check Output window for exceptions
- Check Application Insights (if configured)
- Check Event Viewer for ASP.NET errors
- Enable detailed error messages in Development

---

**Last Updated:** February 6, 2025  
**Status:** formaction attribute added, app needs restart  
**Next Action:** Stop debugging, rebuild, test
