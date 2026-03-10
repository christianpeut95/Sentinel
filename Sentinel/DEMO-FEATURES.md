# Demo Branch Features

This document outlines the demo-specific features that are **only available in the demo branch** and should not be merged into master.

## ?? Delete All Test Data Function

### Overview
A safe, demo-only function to delete all patients, cases, and related test data from the database. This is useful for resetting the demo environment between demonstrations.

### Location
- **Service**: `Services/TestDataGeneratorService.cs`
- **Method**: `DeleteAllTestDataAsync(string confirmationCode, Action<string>? progressCallback)`

### Safety Features

The function has **TWO layers of protection** to prevent accidental data loss:

#### 1. Demo Mode Check
```csharp
var isDemoMode = _configuration.GetValue<bool>("Demo:EnableDemoMode");
```
- Must be set to `true` in `appsettings.Demo.json`
- Blocks execution if not in demo mode

#### 2. Confirmation Code Required
```csharp
const string EXPECTED_CODE = "DELETE-ALL-DEMO-DATA";
```
- Requires exact match of confirmation string
- Prevents accidental execution

### Configuration

Add to `appsettings.Demo.json`:
```json
{
  "Demo": {
    "EnableDemoUsers": true,
    "ShowDemoBanner": true,
    "EnableDemoMode": true
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=aspnet-Sentinel;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**Note**: `appsettings.Demo.json` is gitignored by default.

### Usage Example

```csharp
var result = await testDataGenerator.DeleteAllTestDataAsync(
    "DELETE-ALL-DEMO-DATA",
    (message) => Console.WriteLine(message)
);

if (result.HasErrors)
{
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
else
{
    Console.WriteLine($"Successfully deleted {result.TotalDeleted} records");
    Console.WriteLine($"  - Patients: {result.PatientsDeleted}");
    Console.WriteLine($"  - Cases: {result.CasesDeleted}");
    Console.WriteLine($"  - Lab Results: {result.LabResultsDeleted}");
}
```

### What Gets Deleted

The function deletes (in correct dependency order):

1. ? Review queue entries
2. ? Case tasks
3. ? Exposure events
4. ? Case symptoms
5. ? Notes
6. ? Case custom fields (all types)
7. ? Lab results
8. ? Outbreak case associations
9. ? Cases
10. ? Patient custom fields (all types)
11. ? Patients
12. ? Related audit logs

### What is PRESERVED

The following are **NOT** deleted (these are configuration, not test data):

- ? Diseases
- ? Symptoms
- ? Lookups (Countries, Languages, etc.)
- ? Jurisdictions
- ? Organizations
- ? Custom field definitions
- ? Task templates
- ? Survey templates
- ? Users and roles
- ? Permissions

### Error Handling

If any of the safety checks fail, you'll see messages like:

```
? BLOCKED: This function is only available in Demo mode.
Set 'Demo:EnableDemoMode' to true in appsettings.json
```

```
? BLOCKED: Invalid confirmation code.
Expected: DELETE-ALL-DEMO-DATA
```

### Performance

The function:
- Uses `IgnoreQueryFilters()` to delete soft-deleted items
- Deletes in batches per entity type
- Clears context memory after completion
- Provides progress callbacks for monitoring

Typical deletion time for 10,000 records: **< 30 seconds**

---

## ?? Performance-Optimized Test Data Generation

The demo branch also includes the enhanced `TestDataGeneratorService` with:

### Memory Optimizations
- **Streaming patient loading** instead of loading all into memory
- **Lookup cache** - all lookups loaded once, not repeatedly
- **Batch processing** - 20 cases at a time with memory clearing
- **~95% memory reduction** compared to original implementation

### Features
- Disease-specific symptoms
- Lab results with proper lookups
- Realistic accession numbers
- Case notes
- Custom field support (ready for enhancement)

### Usage
```csharp
var result = await testDataGenerator.GenerateCasesAsync(
    startYear: 2024,
    endYear: 2024,
    casesPerYear: 1000,
    diseaseIds: new List<Guid> { diseaseId },
    options: new CaseGenerationOptions
    {
        IncludeLabResults = true,
        LabResultsPerCaseMin = 1,
        LabResultsPerCaseMax = 3,
        LabResultProbabilityPercent = 80,
        IncludeSymptoms = true,
        SymptomProbabilityPercent = 70,
        IncludeNotes = true,
        CaseNoteProbabilityPercent = 60,
        UseSeasonalPatterns = true
    }
);
```

---

## ?? Important Notes

1. **Never merge demo branch into master** - These features are intentionally demo-only
2. **Always verify database** - Check connection string before running delete
3. **Test on demo database first** - Never run against production
4. **Document usage** - Keep this file updated with any demo-specific features

---

## ?? Security Considerations

- All three safety checks must pass
- Connection string check prevents production accidents
- Confirmation code prevents UI button misclicks
- Demo mode flag provides application-level control
- Audit logs cleaned to prevent orphaned references

---

## ?? Result Classes

### `TestDataDeletionResult`
```csharp
public class TestDataDeletionResult
{
    public int PatientsDeleted { get; set; }
    public int CasesDeleted { get; set; }
    public int LabResultsDeleted { get; set; }
    public int SymptomsDeleted { get; set; }
    public int NotesDeleted { get; set; }
    public int ExposuresDeleted { get; set; }
    public int TasksDeleted { get; set; }
    public int PatientCustomFieldsDeleted { get; set; }
    public int CaseCustomFieldsDeleted { get; set; }
    public int ReviewQueueEntriesDeleted { get; set; }
    public int AuditLogsDeleted { get; set; }
    public List<string> Errors { get; set; }
    public TimeSpan Duration { get; }
    public int TotalDeleted { get; }
}
```

---

**Last Updated**: January 2025  
**Branch**: `demo`  
**Status**: ? Active, DO NOT MERGE TO MASTER
