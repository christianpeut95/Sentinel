# Bug Fix: SurveyJS Expressions Not Evaluating & JSON Parse Error

## Problem Summary

1. **JSON Parse Error**: When completing a survey, the browser threw: 
   ```
   Uncaught (in promise) SyntaxError: Unexpected token '<', "<!DOCTYPE "... is not valid JSON
   ```
   
2. **SurveyJS Expressions Not Working**: Expressions like `today()`, `=today()`, `currentDate()`, `=currentDate()`, age calculations, min/max were not evaluating.

## Root Causes

### 1. JSON Parse Error
The issue was caused by **antiforgery token validation middleware conflict**:
- The `app.UseAntiforgery()` middleware (required for Blazor) was intercepting JSON POST requests to Razor Pages
- Razor Pages with `[FromBody]` parameters don't work well with the global antiforgery middleware
- The `[IgnoreAntiforgeryToken]` attribute on Razor Page handlers doesn't reliably bypass the middleware
- When the request was blocked/redirected, the browser tried to parse HTML as JSON, causing the error

### 2. SurveyJS Expression Issues
Multiple problems:
- Custom functions (`today`, `currentDate`, `age`) were not fully registered
- Missing validation checks for invalid dates
- No `currentDate()` alias (commonly used in SurveyJS)
- No `age()` function for birth date calculations
- Functions weren't verifying that Survey.FunctionFactory was available before registration

## Solutions Applied

### 1. Created Dedicated API Controller

**File**: `Controllers/Api/SurveyCompletionApiController.cs` (NEW)

Created a proper API controller to handle survey submissions. API controllers automatically bypass antiforgery validation and are designed for JSON requests:

```csharp
[Authorize(Policy = "Permission.Survey.Complete")]
[ApiController]
[Route("api/surveys")]
public class SurveyCompletionApiController : ControllerBase
{
    [HttpPost("complete/{taskId}")]
    public async Task<IActionResult> CompleteSurvey(Guid taskId, [FromBody] Dictionary<string, object> responses)
    {
        // Survey completion logic moved here
    }
}
```

**Why this works**:
- `[ApiController]` attribute automatically disables antiforgery validation for JSON requests
- Cleaner separation of concerns (API vs UI)
- Better for AJAX/fetch requests
- Follows RESTful API conventions

### 2. Updated Frontend to Use API Endpoint

**File**: `Pages/Tasks/CompleteSurvey.cshtml`

Changed the fetch URL from Razor Page handler to API endpoint:

```javascript
// OLD (Razor Page handler - had antiforgery issues):
// const response = await fetch(`/Tasks/CompleteSurvey/${taskId}`, {

// NEW (API controller - no antiforgery issues):
const response = await fetch(`/api/surveys/complete/${taskId}`, {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json'
    },
    body: JSON.stringify(survey.data)
});
```

### 3. Enhanced SurveyJS Custom Functions

**File**: `Pages/Shared/_Layout.cshtml`

Registered comprehensive custom functions with proper validation:

```javascript
if (typeof Survey !== 'undefined' && Survey.FunctionFactory && Survey.FunctionFactory.Instance) {
    // today() - returns current date as YYYY-MM-DD
    Survey.FunctionFactory.Instance.register("today", function() {
        const today = new Date();
        return today.toISOString().split('T')[0];
    });

    // currentDate() - alias for today()
    Survey.FunctionFactory.Instance.register("currentDate", function() {
        const today = new Date();
        return today.toISOString().split('T')[0];
    });

    // age(birthDate) - calculate age from birth date
    Survey.FunctionFactory.Instance.register("age", function(params) {
        if (params.length < 1 || !params[0]) return null;
        const birthDate = new Date(params[0]);
        if (isNaN(birthDate.getTime())) return null;
        
        const today = new Date();
        let age = today.getFullYear() - birthDate.getFullYear();
        const monthDiff = today.getMonth() - birthDate.getMonth();
        
        if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
            age--;
        }
        
        return age;
    });

    // addDays(date, days) - add days to a date (with validation)
    // dateDiff(date1, date2) - calculate difference in days (with validation)
}
```

**Key Improvements**:
- Added `currentDate()` function (commonly used in SurveyJS)
- Added `age()` function for calculating age from birth date
- Added null/invalid date checks to all date functions
- Added console logging to verify functions are registered
- Added safety check for `Survey.FunctionFactory.Instance` existence

### 4. Added Debug Logging

**File**: `Pages/Tasks/CompleteSurvey.cshtml`

Added diagnostic logging to help troubleshoot future issues:

```javascript
// Verify custom functions are registered
if (Survey.FunctionFactory && Survey.FunctionFactory.Instance) {
    console.log('SurveyJS FunctionFactory is available');
    console.log('Registered functions:', Survey.FunctionFactory.Instance.getAll());
} else {
    console.warn('SurveyJS FunctionFactory is not available');
}
```

## Testing Recommendations

1. **Test Survey Completion**:
   - Complete a survey with calculated fields
   - Verify no JSON parse errors appear in console
   - Confirm survey saves successfully

2. **Test SurveyJS Expressions**:
   - Use `today()` or `=today()` in default values
   - Use `currentDate()` or `=currentDate()` in expressions
   - Use `age({dob})` to calculate age from a date field
   - Use `addDays(today(), 7)` for date arithmetic
   - Use `dateDiff({startDate}, {endDate})` for date comparisons
   - Verify min/max validation works with calculated dates

3. **Test Edge Cases**:
   - Test with invalid dates
   - Test with empty date fields
   - Test session expiration during survey completion
   - Check browser console for any warnings or errors

## Usage Examples for Survey Designers

### Using today() in Default Values
```json
{
  "name": "reportDate",
  "type": "text",
  "inputType": "date",
  "defaultValueExpression": "today()"
}
```

### Using age() for Age Calculation
```json
{
  "name": "calculatedAge",
  "type": "expression",
  "expression": "age({dateOfBirth})"
}
```

### Using Date Arithmetic
```json
{
  "name": "followUpDate",
  "type": "text",
  "inputType": "date",
  "defaultValueExpression": "addDays(today(), 14)"
}
```

### Using Date Validation
```json
{
  "name": "eventDate",
  "type": "text",
  "inputType": "date",
  "validators": [
    {
      "type": "expression",
      "expression": "{eventDate} <= today()",
      "text": "Event date cannot be in the future"
    }
  ]
}
```

## Files Modified

1. **`Controllers/Api/SurveyCompletionApiController.cs`** (NEW) - Created dedicated API controller for survey submissions
2. **`Pages/Tasks/CompleteSurvey.cshtml.cs`** - Kept legacy handler with comment (deprecated)
3. **`Pages/Tasks/CompleteSurvey.cshtml`** - Updated to use API endpoint and added debug logging
4. **`Pages/Shared/_Layout.cshtml`** - Enhanced custom function registration with validation

## Build Status

? Build successful - all changes compile without errors.

## IMPORTANT: Testing Required

**You need to stop debugging and restart the application for these changes to take effect.**

The code changes have NOT been applied to the running app since hot reload cannot apply:
- New controller files
- Route changes
- API endpoint additions

Please:
1. **Stop the debugger**
2. **Rebuild the solution**
3. **Start debugging again**
4. **Test survey completion**

## After Restart, Expected Behavior

1. Open browser console
2. Navigate to a survey
3. Fill in the form
4. Click "Complete"
5. Console should show:
   - "Submitting survey data: {...}"
   - "Response status: 200"
   - "Response content-type: application/json"
6. Survey should save successfully
7. No JSON parse errors
