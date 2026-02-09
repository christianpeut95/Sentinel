# Disease Hierarchy Model - Implementation Guide

## Overview
The Disease model implements a hierarchical structure using a **path-based approach** for efficient querying without recursion.

## Model Structure

### Key Fields

| Field | Type | Purpose |
|-------|------|---------|
| `Name` | string | Display name (e.g., "Salmonella Typhimurium 9") |
| `Code` | string | Short code for UI display (e.g., "ST9") |
| `ExportCode` | string | Official reporting code (e.g., "A02.1") |
| `ParentDiseaseId` | Guid? | Reference to parent disease |
| `PathIds` | string | Path of GUIDs for querying (e.g., "/guid1/guid2/guid3/") |
| `Level` | int | Hierarchy depth (0=root, 1=child, 2=grandchild) |
| `IsNotifiable` | bool | Requires official notification? |
| `DisplayOrder` | int | Sort order within siblings |

## Example Data Structure

```
Disease: Salmonella
?? Id: a1b2c3d4...
?? Name: "Salmonella"
?? Code: "SAL"
?? ExportCode: "A02"
?? PathIds: "/a1b2c3d4.../"
?? Level: 0
?
???? Disease: Salmonella Typhimurium
?    ?? Id: e5f6g7h8...
?    ?? Name: "Salmonella Typhimurium"
?    ?? Code: "SAL-TYP"
?    ?? ExportCode: "A02.0"
?    ?? PathIds: "/a1b2c3d4.../e5f6g7h8.../"
?    ?? Level: 1
?    ?
?    ???? Disease: Salmonella Typhimurium 9
?         ?? Id: i9j0k1l2...
?         ?? Name: "Salmonella Typhimurium 9"
?         ?? Code: "SAL-TYP-9"
?         ?? ExportCode: "A02.0.9"
?         ?? PathIds: "/a1b2c3d4.../e5f6g7h8.../i9j0k1l2.../"
?         ?? Level: 2
```

## Querying Examples

### Get all Salmonella cases (any level)
```csharp
var salmonellaId = "a1b2c3d4...";
var cases = await _context.Cases
    .Include(c => c.Disease)
    .Include(c => c.Patient)
    .Where(c => c.Disease.PathIds.Contains($"/{salmonellaId}/"))
    .ToListAsync();
```

### Get case count breakdown by disease family
```csharp
var salmonellaId = "a1b2c3d4...";
var breakdown = await _context.Cases
    .Where(c => c.Disease.PathIds.Contains($"/{salmonellaId}/"))
    .GroupBy(c => new { c.Disease.Name, c.Disease.Code })
    .Select(g => new {
        Disease = g.Key.Name,
        Code = g.Key.Code,
        Count = g.Count()
    })
    .OrderByDescending(x => x.Count)
    .ToListAsync();
```

### Get top-level diseases with case counts
```csharp
var diseases = await _context.Diseases
    .Where(d => d.Level == 0 && d.IsActive)
    .Select(d => new {
        Disease = d.Name,
        Code = d.Code,
        TotalCases = _context.Cases.Count(c => c.Disease.PathIds.Contains($"/{d.Id}/"))
    })
    .OrderBy(d => d.Disease)
    .ToListAsync();
```

### Get all children of a disease
```csharp
var parentId = "a1b2c3d4...";
var children = await _context.Diseases
    .Where(d => d.ParentDiseaseId == parentId && d.IsActive)
    .OrderBy(d => d.DisplayOrder)
    .ThenBy(d => d.Name)
    .ToListAsync();
```

### Get all descendants of a disease (any depth)
```csharp
var diseaseId = "a1b2c3d4...";
var descendants = await _context.Diseases
    .Where(d => d.PathIds.Contains($"/{diseaseId}/") && d.Id != diseaseId)
    .OrderBy(d => d.Level)
    .ThenBy(d => d.DisplayOrder)
    .ToListAsync();
```

## Automatic Path Maintenance

The `PathIds` and `Level` fields are **automatically maintained** by `ApplicationDbContext`:

```csharp
// When saving a new disease or updating its parent
private async Task UpdateDiseasePaths()
{
    var changedDiseases = ChangeTracker.Entries<Disease>()
        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
        .Select(e => e.Entity)
        .ToList();

    foreach (var disease in changedDiseases)
    {
        if (disease.ParentDiseaseId.HasValue)
        {
            var parent = await Diseases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == disease.ParentDiseaseId.Value);
            
            if (parent != null)
            {
                disease.PathIds = $"{parent.PathIds}{disease.Id}/";
                disease.Level = parent.Level + 1;
            }
        }
        else
        {
            disease.PathIds = $"/{disease.Id}/";
            disease.Level = 0;
        }
    }
}
```

## Database Indexes

Optimized for performance:

```csharp
// Code must be unique
builder.Entity<Disease>()
    .HasIndex(d => d.Code)
    .IsUnique();

// Fast export code lookups
builder.Entity<Disease>()
    .HasIndex(d => d.ExportCode);

// Fast path-based queries
builder.Entity<Disease>()
    .HasIndex(d => d.PathIds);

// Fast hierarchy browsing
builder.Entity<Disease>()
    .HasIndex(d => new { d.Level, d.DisplayOrder });
```

## Integration with Cases

```csharp
public class Case : IAuditable
{
    // ... existing properties

    [Required]
    [Display(Name = "Disease")]
    public Guid DiseaseId { get; set; }
    public Disease? Disease { get; set; }
}
```

## Next Steps

1. **Migration**: Create a new migration to add the Diseases table
   ```bash
   dotnet ef migrations add AddDiseaseHierarchy
   dotnet ef database update
   ```

2. **Seed Data**: Create common diseases in your seeder

3. **UI Pages**: Create CRUD pages in `/Pages/Settings/Diseases/`
   - Index (show hierarchy tree)
   - Create (with parent selection)
   - Edit
   - Delete (check for cases first)

4. **Update Case Pages**: Add disease dropdown to case create/edit forms

5. **Reporting**: Create reports that leverage path-based queries

## Performance Benefits

- ? **No recursion** - Simple `LIKE` queries
- ? **Single query** - Get all descendants in one go
- ? **Indexed** - Fast path matching
- ? **Automatic** - Paths maintained on save
- ? **Scalable** - Works with thousands of diseases

## Notes

- Path maintenance happens automatically in `SaveChangesAsync`
- Moving a disease to a new parent will update its path
- Children paths are NOT automatically updated when parent moves (consider adding this if needed)
- `OnDelete: Restrict` prevents accidental deletion of diseases with children or cases
