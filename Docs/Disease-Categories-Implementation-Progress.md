# Disease Categories Implementation - In Progress ?

## Summary

Adding **Disease Categories** with Reporting IDs to organize diseases into logical groups (e.g., "STI/BBV", "Food Borne Diseases", "Vaccine Preventable Diseases").

## Completed ?

### 1. **DiseaseCategory Model** (`Models/Lookups/DiseaseCategory.cs`)
- ? Created with fields:
  - `Name` - Category name (e.g., "STI/BBV")
  - `ReportingId` - Unique reporting identifier
  - `Description` - Optional description
  - `DisplayOrder` - Sort order
  - `IsActive` - Active/inactive flag
  - Navigation to Diseases collection

### 2. **Disease Model Updated** (`Models/Lookups/Disease.cs`)
- ? Added `DiseaseCategoryId` and `DiseaseCategory` navigation property
- ? Diseases can now belong to a category (optional)

### 3. **ApplicationDbContext Updated** (`Data/ApplicationDbContext.cs`)
- ? Added `DbSet<DiseaseCategory>`
- ? Configured indexes:
  - Unique index on `Name`
  - Unique index on `ReportingId`
  - Index on `DisplayOrder`
- ? Configured relationship: Disease ? DiseaseCategory

### 4. **Disease Index Page Redesigned** (`Pages/Settings/Diseases/Index.cshtml[.cs]`)
- ? Now groups diseases by category
- ? Shows category headers with Reporting ID badge
- ? Shows uncategorized diseases in separate section
- ? Added "Manage Categories" button

### 5. **Build Status**
- ? Build successful

## Next Steps (TODO)

### Step 1: Create Migration
```bash
dotnet ef migrations add AddDiseaseCategories
dotnet ef database update
```

### Step 2: Create DiseaseCategory CRUD Pages
Need to create:
- `Pages/Settings/DiseaseCategories/Index.cshtml[.cs]`
- `Pages/Settings/DiseaseCategories/Create.cshtml[.cs]`
- `Pages/Settings/DiseaseCategories/Edit.cshtml[.cs]`
- `Pages/Settings/DiseaseCategories/Details.cshtml[.cs]`
- `Pages/Settings/DiseaseCategories/Delete.cshtml[.cs]`

### Step 3: Update Disease Create/Edit Pages
Add category dropdown to:
- `Pages/Settings/Diseases/Create.cshtml[.cs]`
- `Pages/Settings/Diseases/Edit.cshtml[.cs]`

### Step 4: Seed Sample Categories
Add to seeder or create manually:
- STI/BBV (Sexually Transmitted & Blood Borne Viruses)
- Food Borne Diseases
- Vaccine Preventable Diseases
- Respiratory Diseases
- Vector Borne Diseases
- Zoonotic Diseases

## Visual Preview

### Disease Index Page (After completion):
```
???????????????????????????????????????????????
? Diseases                    [Manage Categories] [Create New Disease] ?
???????????????????????????????????????????????
? STI/BBV ?? RPT-001                          ?
?   Chlamydia                    ? Active    ?
?   Gonorrhoea                   ? Active    ?
?   — Gonorrhoea (pharyngeal)   ? Active    ?
?   HIV/AIDS                     ? Active    ?
?                                            ?
? Food Borne Diseases ?? RPT-002            ?
?   Salmonella                   ? Active    ?
?   — Salmonella Typhimurium    ? Active    ?
?   —— Salmonella Typhimurium 9 ? Active    ?
?   Campylobacter               ? Active    ?
?   — Campylobacter jejuni      ? Active    ?
?                                            ?
? Vaccine Preventable Diseases ?? RPT-003   ?
?   Measles                     ? Active    ?
?   Mumps                       ? Active    ?
?   Rubella                     ? Active    ?
?                                            ?
? Uncategorized                             ?
?   Other disease               ? Active    ?
???????????????????????????????????????????????
```

## Database Schema

### DiseaseCategories Table
```sql
Id (uniqueidentifier, PK)
Name (nvarchar(200), unique)
ReportingId (nvarchar(50), unique)
Description (nvarchar(1000), nullable)
DisplayOrder (int)
IsActive (bit)
CreatedAt (datetime2)
ModifiedAt (datetime2, nullable)
```

### Diseases Table (Updated)
```sql
-- Existing fields...
DiseaseCategoryId (uniqueidentifier, nullable, FK to DiseaseCategories)
-- ...rest of fields
```

## Example Data

### Categories:
| Name | Reporting ID | Description |
|------|--------------|-------------|
| STI/BBV | RPT-001 | Sexually Transmitted & Blood Borne Viruses |
| Food Borne Diseases | RPT-002 | Diseases transmitted through contaminated food |
| Vaccine Preventable Diseases | RPT-003 | Diseases preventable by vaccination |

### Diseases:
| Name | Category | Code | Export Code |
|------|----------|------|-------------|
| Chlamydia | STI/BBV | CHLAM | A56 |
| Salmonella | Food Borne | SAL | A02 |
| Measles | Vaccine Preventable | MEAS | B05 |

## Benefits

? **Organization** - Diseases grouped logically
? **Reporting** - Each category has unique Reporting ID
? **Filtering** - Easy to filter by category in reports
? **Hierarchical** - Maintains disease hierarchy within categories
? **Flexibility** - Categories optional (uncategorized section)

## Files Modified

### Created:
- ? `Models/Lookups/DiseaseCategory.cs`

### Modified:
- ? `Models/Lookups/Disease.cs`
- ? `Data/ApplicationDbContext.cs`
- ? `Pages/Settings/Diseases/Index.cshtml`
- ? `Pages/Settings/Diseases/Index.cshtml.cs`

### To Be Created:
- ? `Pages/Settings/DiseaseCategories/` (5 CRUD pages)
- ? Migration file

### To Be Modified:
- ? `Pages/Settings/Diseases/Create.cshtml[.cs]` - Add category dropdown
- ? `Pages/Settings/Diseases/Edit.cshtml[.cs]` - Add category dropdown

## Quick Start (After Completion)

### 1. Create Categories
```
Settings ? Diseases ? Manage Categories ? Create New
- Name: "STI/BBV"
- Reporting ID: "RPT-001"
- Description: "Sexually Transmitted & Blood Borne Viruses"
```

### 2. Assign Category to Disease
```
Settings ? Diseases ? Edit (existing disease)
- Select Category: "STI/BBV"
- Save
```

### 3. Create New Disease with Category
```
Settings ? Diseases ? Create New Disease
- Name: "Chlamydia"
- Category: "STI/BBV"
- Code: "CHLAM"
- Export Code: "A56"
```

## Status

? **Core models created**  
? **Database relationships configured**  
? **Index page redesigned**  
? **Build successful**  
? **Migration needed**  
? **Category CRUD pages needed**  
? **Disease forms need category dropdown**  

Ready for next phase implementation!
