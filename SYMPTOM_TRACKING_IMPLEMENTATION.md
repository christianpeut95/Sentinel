# Symptom Tracking System - Implementation Summary

## Overview
A comprehensive symptom tracking system has been implemented for the surveillance application. This system allows tracking of patient symptoms associated with cases, including onset dates, severity, and notes.

## Database Schema

### Tables Created

#### 1. **Symptoms** (Lookup Table)
Stores the master list of symptoms.

**Columns:**
- `Id` (int, PK)
- `Name` (nvarchar(100)) - Symptom name
- `Code` (nvarchar(50)) - Internal code
- `ExportCode` (nvarchar(50)) - External reporting code
- `Description` (nvarchar(500))
- `IsActive` (bit)
- `SortOrder` (int)
- Audit fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Soft delete fields: `IsDeleted`, `DeletedAt`, `DeletedBy`

**Features:**
- Pre-seeded with 26 common symptoms including "Other"
- Fully auditable with user tracking
- Soft delete support
- Sortable for UI display

#### 2. **CaseSymptoms** (Junction Table)
Links symptoms to specific cases with clinical details.

**Columns:**
- `Id` (int, PK)
- `CaseId` (uniqueidentifier, FK) - References Cases
- `SymptomId` (int, FK) - References Symptoms
- `OnsetDate` (datetime2) - When symptom started
- `Severity` (nvarchar(20)) - Mild/Moderate/Severe
- `Notes` (nvarchar(1000)) - Additional details
- `OtherSymptomText` (nvarchar(200)) - Used when "Other" is selected
- Audit fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Soft delete fields: `IsDeleted`, `DeletedAt`, `DeletedBy`

**Features:**
- Multiple symptoms per case
- Individual onset dates for each symptom
- Severity tracking
- "Other" symptom support with free text
- Cascade delete when case is deleted

#### 3. **DiseaseSymptoms** (Association Table)
Associates common symptoms with specific diseases for UI assistance.

**Columns:**
- `Id` (int, PK)
- `DiseaseId` (uniqueidentifier, FK) - References Diseases
- `SymptomId` (int, FK) - References Symptoms
- `IsCommon` (bit) - Whether this is a common symptom for the disease
- `SortOrder` (int) - Display order for this disease
- Audit fields: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- Soft delete fields: `IsDeleted`, `DeletedAt`, `DeletedBy`

**Features:**
- Helps UI show relevant symptoms first
- Unique disease-symptom combinations
- Cascade delete when disease is deleted

## Pre-Seeded Symptoms

The following symptoms are automatically added during migration:

1. Fever (SYM001)
2. Chills (SYM002)
3. Cough (SYM003)
4. Shortness of Breath (SYM004)
5. Fatigue (SYM005)
6. Muscle or Body Aches (SYM006)
7. Headache (SYM007)
8. Loss of Taste or Smell (SYM008)
9. Sore Throat (SYM009)
10. Congestion or Runny Nose (SYM010)
11. Nausea or Vomiting (SYM011)
12. Diarrhea (SYM012)
13. Abdominal Pain (SYM013)
14. Rash (SYM014)
15. Joint Pain (SYM015)
16. Confusion (SYM016)
17. Seizures (SYM017)
18. Jaundice (SYM018)
19. Bleeding (SYM019)
20. Swollen Lymph Nodes (SYM020)
21. Night Sweats (SYM021)
22. Weight Loss (SYM022)
23. Difficulty Swallowing (SYM023)
24. Vision Changes (SYM024)
25. Hearing Loss (SYM025)
26. **Other** (SYM999) - Special entry for custom symptoms

## Entity Framework Models

### Symptom.cs
- Located: `Surveillance-MVP/Models/Lookups/Symptom.cs`
- Implements: `IAuditable`, `ISoftDeletable`
- Navigation properties to `CaseSymptom` and `DiseaseSymptom`

### CaseSymptom.cs
- Located: `Surveillance-MVP/Models/CaseSymptom.cs`
- Implements: `IAuditable`, `ISoftDeletable`
- Navigation properties to `Case` and `Symptom`

### DiseaseSymptom.cs
- Located: `Surveillance-MVP/Models/DiseaseSymptom.cs`
- Implements: `IAuditable`, `ISoftDeletable`
- Navigation properties to `Disease` and `Symptom`

## ApplicationDbContext Updates

**New DbSets added:**
```csharp
public DbSet<Symptom> Symptoms { get; set; }
public DbSet<CaseSymptom> CaseSymptoms { get; set; }
public DbSet<DiseaseSymptom> DiseaseSymptoms { get; set; }
```

**Configuration added:**
- Unique code constraint on Symptoms
- Cascade delete from Case to CaseSymptoms
- Unique disease-symptom combination constraint
- Indexes for performance
- Global query filters for soft delete

**Navigation Properties Added:**
- `Case.CaseSymptoms` collection
- `Disease.DiseaseSymptoms` collection

## Management Pages

### Symptoms List (`/Settings/Lookups/Symptoms`)
- **File:** `Surveillance-MVP/Pages/Settings/Lookups/Symptoms.cshtml`
- **Features:**
  - Lists all active symptoms
  - Shows code, export code, sort order, and status
  - Links to create and edit pages
  - Sorted by SortOrder then Name

### Create Symptom (`/Settings/Lookups/CreateSymptom`)
- **File:** `Surveillance-MVP/Pages/Settings/Lookups/CreateSymptom.cshtml`
- **Features:**
  - Form to add new symptoms
  - Validates required fields
  - Auto-sets audit fields
  - Success message on creation

### Edit Symptom (`/Settings/Lookups/EditSymptom`)
- **File:** `Surveillance-MVP/Pages/Settings/Lookups/EditSymptom.cshtml`
- **Features:**
  - Edit existing symptoms
  - Soft delete functionality
  - Shows audit information
  - Updates audit fields on save
  - Warning for deleted symptoms

## Authorization
All symptom management pages require the `RequireManagePermissionsPermission` policy.

## Migration File
**Location:** `Migrations/Add_Symptom_Tracking.sql`

**Run the migration:**
```sql
-- Execute against your SurveillanceMVP database
USE [SurveillanceMVP]
GO
-- Then run the entire Add_Symptom_Tracking.sql script
```

**What it does:**
1. Creates Symptoms, CaseSymptoms, and DiseaseSymptoms tables
2. Sets up foreign key relationships
3. Creates performance indexes
4. Seeds 26 common symptoms
5. Verifies successful migration

## Next Steps

### 1. Update Case Details/Edit Pages
Add symptom tracking UI to case management pages:
- Symptom checklist with onset date picker
- Severity dropdown (Mild/Moderate/Severe)
- Notes field per symptom
- Special handling for "Other" symptom

### 2. Disease-Symptom Association (Optional)
For improved UX, associate common symptoms with diseases:
```sql
-- Example: Associate fever and cough with COVID-19
INSERT INTO DiseaseSymptoms (DiseaseId, SymptomId, IsCommon, SortOrder)
SELECT 
    (SELECT Id FROM Diseases WHERE Code = 'COVID19'),
    Id,
    1,
    SortOrder
FROM Symptoms
WHERE Code IN ('FEVER', 'COUGH', 'SOB', 'FATIGUE');
```

### 3. Reporting
Create queries/reports that utilize symptom data:
- Common symptoms by disease
- Symptom onset to diagnosis time
- Severity distribution
- Export for public health reporting

### 4. Case Creation/Edit Enhancement
When creating/editing a case:
- Show common symptoms for selected disease first
- Allow multiple symptom selection
- Capture onset date for each symptom
- Support "Other" with free text

## Usage Examples

### Query Cases by Symptom
```csharp
var casesWithFever = await _context.Cases
    .Include(c => c.CaseSymptoms)
        .ThenInclude(cs => cs.Symptom)
    .Where(c => c.CaseSymptoms.Any(cs => cs.Symptom.Code == "FEVER"))
    .ToListAsync();
```

### Get Symptoms for a Case
```csharp
var caseSymptoms = await _context.CaseSymptoms
    .Include(cs => cs.Symptom)
    .Where(cs => cs.CaseId == caseId)
    .OrderBy(cs => cs.OnsetDate)
    .ToListAsync();
```

### Get Common Symptoms for a Disease
```csharp
var commonSymptoms = await _context.DiseaseSymptoms
    .Include(ds => ds.Symptom)
    .Where(ds => ds.DiseaseId == diseaseId && ds.IsCommon)
    .OrderBy(ds => ds.SortOrder)
    .Select(ds => ds.Symptom)
    .ToListAsync();
```

## Best Practices

1. **Always use "Other" for unlisted symptoms** - Don't create new symptom records on the fly
2. **Capture onset dates** - Critical for epidemiological analysis
3. **Use severity consistently** - Stick to Mild/Moderate/Severe
4. **Associate common symptoms with diseases** - Improves data entry experience
5. **Export codes are important** - Map to external reporting systems (SNOMED, ICD, etc.)

## Benefits

? **Structured Data**: Symptoms are standardized and queryable  
? **Flexibility**: "Other" option for uncommon symptoms  
? **Auditable**: Full audit trail for compliance  
? **Optional**: Symptom data can be omitted for certain case types  
? **Disease-Specific**: UI can show relevant symptoms based on disease  
? **Time-Aware**: Individual onset dates for each symptom  
? **Exportable**: Export codes for external reporting  
? **Soft Delete**: No data loss, can be restored  

## Files Modified/Created

### Created:
- `Migrations/Add_Symptom_Tracking.sql`
- `Surveillance-MVP/Models/Lookups/Symptom.cs`
- `Surveillance-MVP/Models/CaseSymptom.cs`
- `Surveillance-MVP/Models/DiseaseSymptom.cs`
- `Surveillance-MVP/Pages/Settings/Lookups/Symptoms.cshtml`
- `Surveillance-MVP/Pages/Settings/Lookups/Symptoms.cshtml.cs`
- `Surveillance-MVP/Pages/Settings/Lookups/CreateSymptom.cshtml`
- `Surveillance-MVP/Pages/Settings/Lookups/CreateSymptom.cshtml.cs`
- `Surveillance-MVP/Pages/Settings/Lookups/EditSymptom.cshtml`
- `Surveillance-MVP/Pages/Settings/Lookups/EditSymptom.cshtml.cs`

### Modified:
- `Surveillance-MVP/Models/Case.cs` - Added `CaseSymptoms` navigation property
- `Surveillance-MVP/Models/Lookups/Disease.cs` - Added `DiseaseSymptoms` navigation property
- `Surveillance-MVP/Data/ApplicationDbContext.cs` - Added DbSets and configuration
- `Surveillance-MVP/Pages/Settings/Index.cshtml` - Added Symptoms link
