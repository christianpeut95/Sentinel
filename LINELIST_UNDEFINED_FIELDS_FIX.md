# Line List "Undefined" Fields Fix

## Problem
All available fields in the line list were showing as "undefined" instead of their actual names.

## Root Cause
**ASP.NET Core JSON Serialization Default Behavior**

ASP.NET Core's default JSON serializer converts C# PascalCase property names to JavaScript camelCase:
- C# Model: `FieldPath` ? JSON: `fieldPath`
- C# Model: `DisplayName` ? JSON: `displayName`
- C# Model: `Category` ? JSON: `category`

The JavaScript code was trying to access properties using PascalCase (e.g., `f.FieldPath`) but the actual JSON properties were camelCase (e.g., `f.fieldPath`).

## Solution Applied

### Changed All Property Access from PascalCase to camelCase

**File: `Pages/Outbreaks/LineList.cshtml`**

#### 1. Field Rendering
```javascript
// BEFORE (Wrong - PascalCase)
${f.FieldPath}
${f.DisplayName}
${f.Category}

// AFTER (Correct - camelCase)
${f.fieldPath}
${f.displayName}
${f.category}
```

#### 2. groupByCategory Function
```javascript
// BEFORE
if (!grouped[field.Category]) {
    grouped[field.Category] = [];
}

// AFTER
if (!grouped[field.category]) {
    grouped[field.category] = [];
}
```

#### 3. addAllFromCategory Function
```javascript
// BEFORE
.filter(f => f.Category === category)
.map(f => f.FieldPath)

// AFTER
.filter(f => f.category === category)
.map(f => f.fieldPath)
```

#### 4. renderSelectedFields Function
```javascript
// BEFORE
const field = availableFields.find(f => f.FieldPath === fp);
${field.DisplayName}
${field.Category}

// AFTER
const field = availableFields.find(f => f.fieldPath === fp);
${field.displayName}
${field.category}
```

#### 5. renderGrid Function
```javascript
// BEFORE
const field = availableFields.find(f => f.FieldPath === fp);
headerName: field ? field.DisplayName : fp

// AFTER
const field = availableFields.find(f => f.fieldPath === fp);
headerName: field ? field.displayName : fp
```

## Testing Steps

1. **Navigate to Line List**
   - Go to Outbreak Details page
   - Click dropdown ? "Line List"

2. **Verify Field Display**
   - Click "Configure Fields"
   - Verify field categories appear (Patient, Case, Outbreak, etc.)
   - Verify individual fields show proper names:
     - "Last Name" instead of "undefined"
     - "Date of Birth" instead of "undefined"
     - "Date of Onset" instead of "undefined"

3. **Test Field Selection**
   - Click + button next to any field
   - Verify field appears in "Selected Fields" panel with proper name
   - Verify category shows below field name

4. **Test "Add all" from Category**
   - Click "Add all" next to any category
   - Verify all fields from that category appear with names

5. **Test Grid Rendering**
   - Select several fields
   - Click "Apply Changes"
   - Verify grid shows with proper column headers

## Key Takeaway

**Always use camelCase when accessing JSON properties from ASP.NET Core APIs in JavaScript**, unless you've explicitly configured the serializer to use PascalCase.

### Alternative Solution (Not Recommended)
You could configure ASP.NET Core to use PascalCase JSON serialization:

```csharp
// In Program.cs
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keeps PascalCase
    });
```

However, camelCase is the JavaScript convention and most front-end libraries expect it, so it's better to follow the default.

## Files Modified
- `Surveillance-MVP\Pages\Outbreaks\LineList.cshtml` - Fixed all JavaScript property access

## Status
? **FIXED** - All fields now display correctly with their proper names
