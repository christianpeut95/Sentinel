# Symptom Tracking - Quick Reference

## ?? What Was Implemented

A complete symptom tracking system that allows:
- ? Recording multiple symptoms per case
- ? Individual onset dates for each symptom
- ? Severity tracking (Mild/Moderate/Severe)
- ? "Other" option with free text
- ? Disease-symptom associations for better UX
- ? Full audit trail
- ? Soft delete support
- ? Export codes for external reporting

## ?? How to Deploy

### 1. Run the Database Migration
```sql
USE [SurveillanceMVP]
GO
-- Execute: Migrations/Add_Symptom_Tracking.sql
```

This will:
- Create 3 new tables (Symptoms, CaseSymptoms, DiseaseSymptoms)
- Seed 26 common symptoms
- Set up all indexes and foreign keys

### 2. Build and Deploy Application
The code is ready - just deploy your application. All models and pages are in place.

### 3. Access Symptom Management
Navigate to: **Settings ? Laboratory Lookups ? Symptoms**

Or directly: `/Settings/Lookups/Symptoms`

## ?? Key URLs

| Page | URL | Purpose |
|------|-----|---------|
| Symptom List | `/Settings/Lookups/Symptoms` | View all symptoms |
| Create Symptom | `/Settings/Lookups/CreateSymptom` | Add new symptom |
| Edit Symptom | `/Settings/Lookups/EditSymptom?id={id}` | Modify/delete symptom |

## ?? Database Tables

### Symptoms (Lookup)
Master list of symptoms. Pre-seeded with 26 common symptoms.

### CaseSymptoms (Junction)
Links symptoms to cases with clinical details:
- `OnsetDate` - When symptom started
- `Severity` - Mild/Moderate/Severe
- `Notes` - Additional details
- `OtherSymptomText` - For "Other" symptom

### DiseaseSymptoms (Association)
Optional: Associates common symptoms with diseases for better UI.

## ?? Next Steps (Not Yet Implemented)

### Add Symptom Tracking to Case Pages

You'll want to enhance the Case Create/Edit pages to include symptom tracking:

**Suggested UI:**
1. When creating/editing a case, show a "Symptoms" section
2. Display checkboxes for common symptoms
3. For each selected symptom, show:
   - Onset date picker
   - Severity dropdown
   - Notes textbox
4. Include "Other" checkbox with text input

**Sample Code Location:**
- `Surveillance-MVP/Pages/Cases/Create.cshtml`
- `Surveillance-MVP/Pages/Cases/Edit.cshtml`
- `Surveillance-MVP/Pages/Cases/Details.cshtml` (display)

### Example Query Code

```csharp
// Get symptoms for a case
var symptoms = await _context.CaseSymptoms
    .Include(cs => cs.Symptom)
    .Where(cs => cs.CaseId == caseId)
    .OrderBy(cs => cs.OnsetDate)
    .ToListAsync();

// Find cases with specific symptom
var casesWithFever = await _context.Cases
    .Include(c => c.CaseSymptoms)
        .ThenInclude(cs => cs.Symptom)
    .Where(c => c.CaseSymptoms.Any(cs => cs.Symptom.Code == "FEVER"))
    .ToListAsync();

// Get common symptoms for a disease
var commonSymptoms = await _context.DiseaseSymptoms
    .Include(ds => ds.Symptom)
    .Where(ds => ds.DiseaseId == diseaseId && ds.IsCommon)
    .OrderBy(ds => ds.SortOrder)
    .Select(ds => ds.Symptom)
    .ToListAsync();
```

## ?? Tips

1. **The "Other" Symptom**: Code = "OTHER" (SortOrder = 999)
   - Always appears last in lists
   - Use `OtherSymptomText` field in CaseSymptoms to capture the description

2. **Export Codes**: Map to external systems (SNOMED, ICD-10, etc.)
   - Example: "FEVER" ? "SYM001" ? SNOMED code
   - Edit symptoms to add your organization's codes

3. **Disease-Symptom Associations**: 
   - Optional but improves UX
   - Shows relevant symptoms first when creating cases
   - Example: COVID-19 ? Fever, Cough, SOB, Fatigue

4. **Severity Values**: Standardize on:
   - "Mild"
   - "Moderate"
   - "Severe"
   - Or use custom values that fit your workflow

## ?? Permissions

All symptom management pages require:
- **Policy:** `RequireManagePermissionsPermission`
- Users need appropriate permissions to access Settings

## ?? Files Created

### Models
- `Surveillance-MVP/Models/Lookups/Symptom.cs`
- `Surveillance-MVP/Models/CaseSymptom.cs`
- `Surveillance-MVP/Models/DiseaseSymptom.cs`

### Pages
- `Surveillance-MVP/Pages/Settings/Lookups/Symptoms.cshtml[.cs]`
- `Surveillance-MVP/Pages/Settings/Lookups/CreateSymptom.cshtml[.cs]`
- `Surveillance-MVP/Pages/Settings/Lookups/EditSymptom.cshtml[.cs]`

### Migration
- `Migrations/Add_Symptom_Tracking.sql`

### Documentation
- `SYMPTOM_TRACKING_IMPLEMENTATION.md` (detailed guide)
- `SYMPTOM_TRACKING_QUICK_REFERENCE.md` (this file)

## ? What Works Now

- ? Symptom management (CRUD)
- ? Database schema
- ? Entity Framework models
- ? Soft delete
- ? Audit tracking
- ? Pre-seeded data

## ? What's Next

- ? UI on Case Create/Edit pages
- ? Display symptoms on Case Details
- ? Associate symptoms with diseases
- ? Reporting/analytics
- ? Export functionality

## ?? Troubleshooting

**Build errors?**
- Make sure you've saved all files
- Check that `ISoftDeletable` interface uses `DeletedByUserId`

**Migration errors?**
- Ensure you're connected to the correct database
- Check that tables don't already exist
- Verify foreign key references (Cases, Diseases, AspNetUsers tables must exist)

**Page not showing?**
- Clear browser cache
- Rebuild solution
- Check authorization - user must have ManagePermissions permission

## ?? Support

For detailed implementation information, see:
- `SYMPTOM_TRACKING_IMPLEMENTATION.md`

For database schema details, see:
- `Migrations/Add_Symptom_Tracking.sql`
