# Address Management with Disease Inheritance

## Overview
This feature allows child diseases to inherit address management settings from their parent diseases, reducing configuration overhead while maintaining flexibility for disease-specific overrides.

## Features Implemented

### 1. **Database Schema**
- Added `InheritAddressSettingsFromParent` (bool) to `Disease` model (default: `true`)
- Added 4 address management fields to `Disease`:
  - `AddressReviewWindowBeforeDays` (int?) - Days before onset to check address changes
  - `AddressReviewWindowAfterDays` (int?) - Days after onset to check address changes
  - `CheckJurisdictionCrossing` (bool) - Flag cases when patient crosses jurisdictions
  - `JurisdictionFieldsToCheck` (string) - Which jurisdiction levels to monitor (e.g., "1,2,3")
- Added 8 address snapshot fields to `Case`:
  - `CaseAddressLine`, `CaseCity`, `CaseState`, `CasePostalCode`
  - `CaseLatitude`, `CaseLongitude`
  - `CaseAddressCapturedAt` (DateTime?) - When address was captured
  - `CaseAddressManualOverride` (bool) - User set a different address intentionally

### 2. **Service Layer**

#### `IPatientAddressService`
- `ProcessAddressChangeAsync()` - Detects address changes, geocodes, flags cases for review
- `CopyAddressToCaseAsync()` - Snapshots patient address to case (on creation)
- `ApplyAddressToCasesAsync()` - Batch applies address to multiple cases
- `GetEffectiveAddressSettingsAsync()` - **NEW** Resolves inherited settings
- `HasJurisdictionCrossing()` - Compares jurisdiction IDs

#### `PatientAddressService` Implementation
- **Inheritance Resolution**: Walks up `PathIds` to find first parent with `InheritAddressSettingsFromParent=false`
- **Time Window Filtering**: Only flags cases within configured time window around onset date
- **Jurisdiction Detection**: Automatically creates high-priority ReviewQueue entries for jurisdiction crossings
- **Manual Override Respect**: Never auto-updates cases with `CaseAddressManualOverride=true`
- **Permission Filtering**: Only shows user cases they have permission to see

### 3. **Result Class**
```csharp
public class DiseaseAddressSettings
{
    public bool SyncWithPatientAddressUpdates { get; set; }
    public int? AddressReviewWindowBeforeDays { get; set; }
    public int? AddressReviewWindowAfterDays { get; set; }
    public bool CheckJurisdictionCrossing { get; set; }
    public string? JurisdictionFieldsToCheck { get; set; }
    public bool DefaultToResidentialAddress { get; set; }
    
    // Audit fields
    public Guid SourceDiseaseId { get; set; }
    public bool IsInherited { get; set; }
}
```

### 4. **User Interface**
- **Disease Edit Page** (`Pages/Settings/Diseases/Edit.cshtml`):
  - New "Address Management Settings" card in Exposure Tracking tab
  - Shows inheritance toggle for child diseases
  - Disables/grays out fields when inheritance is enabled
  - JavaScript automatically manages field state based on toggle
  - Helpful guidance text explaining how the feature works

### 5. **Integration Points**

#### Case Creation (`Pages/Cases/Create.cshtml.cs`)
```csharp
var settings = await _patientAddressService.GetEffectiveAddressSettingsAsync(Case.DiseaseId.Value);
if (settings.DefaultToResidentialAddress)
{
    await _patientAddressService.CopyAddressToCaseAsync(Case.Id, manualOverride: false);
}
```

#### Patient Edit (`Pages/Patients/Edit.cshtml.cs`)
```csharp
var result = await _patientAddressService.ProcessAddressChangeAsync(
    Patient, oldAddressLine, oldCity, oldState, oldPostalCode, currentUserId);

if (result.CasesRequiringReview.Any())
{
    TempData["AddressChangeReview"] = JsonSerializer.Serialize(result.CasesRequiringReview);
}
```

## How Inheritance Works

### Example Hierarchy
```
COVID-19 (Parent)
├─ InheritAddressSettingsFromParent: false
├─ CheckJurisdictionCrossing: true
├─ JurisdictionFieldsToCheck: "1,2,3"
└─ AddressReviewWindowBeforeDays: 14
    └─ AddressReviewWindowAfterDays: 30

COVID-19 Delta (Child)
├─ InheritAddressSettingsFromParent: true
└─ [Inherits all settings from COVID-19]

COVID-19 Omicron (Child)
├─ InheritAddressSettingsFromParent: false
├─ CheckJurisdictionCrossing: true
└─ JurisdictionFieldsToCheck: "1,2"  [Override - only check top 2 levels]
```

### Resolution Algorithm
1. Load disease by ID
2. If `InheritAddressSettingsFromParent=false` → Use disease's own settings
3. If `InheritAddressSettingsFromParent=true`:
   - Split `PathIds` by `/` to get ancestor IDs
   - Reverse order (walk from parent to grandparent)
   - Find first ancestor with `InheritAddressSettingsFromParent=false`
   - Return that ancestor's settings with `IsInherited=true` and `SourceDiseaseId=ancestorId`
4. Fallback to current disease if no suitable parent found

## Usage Scenarios

### Scenario 1: Patient Moves Within Jurisdiction
- Patient address changes from "123 Main St" to "456 Oak Ave" (same jurisdiction)
- System geocodes new address (server-side, synchronous)
- Related cases checked against time window (e.g., ±14 days from onset)
- Cases within window → Added to `TempData["AddressChangeReview"]`
- User sees prompt: "3 cases may need address update"
- Jurisdiction crossing: **NO** → No automatic ReviewQueue entry

### Scenario 2: Patient Crosses Jurisdiction
- Patient moves from "Regional Health District 1" to "Regional Health District 2"
- System detects change in `Jurisdiction2Id` (configured in `JurisdictionFieldsToCheck`)
- **HIGH PRIORITY**: Automatic ReviewQueue entry created immediately
- Notifies supervisor/data quality team
- All related cases flagged for manual review
- User cannot bypass this review (jurisdiction crossings are critical)

### Scenario 3: Manual Override
- User edits case directly and sets custom address (sets `CaseAddressManualOverride=true`)
- Later, patient moves to new address
- System skips this case during address sync
- Respects user's intentional deviation (e.g., historical address, travel destination)

## Benefits

### Configuration Efficiency
- Parent disease configured once
- All child diseases inherit automatically
- Reduces data entry errors
- Consistent behavior across related diseases

### Flexibility
- Child diseases can override when needed
- Simple toggle to enable/disable inheritance
- No code changes required for new disease variants

### Data Quality
- Historical addresses preserved (snapshot approach)
- Jurisdiction crossings never missed (automatic high-priority review)
- Manual overrides respected
- Time windows prevent false positives

### Audit Trail
- `CaseAddressCapturedAt` tracks when snapshot taken
- `SourceDiseaseId` shows which disease provided settings
- `IsInherited` flag shows inheritance vs override
- ReviewQueue entries preserve change history

## Technical Notes

### Server-Side Geocoding
- All geocoding happens synchronously on server
- User waits for completion before page navigation
- Prevents data corruption from incomplete operations
- Uses either Google Maps API or Nominatim (OSM)

### PathIds Format
- Stored as `/grandparent-guid/parent-guid/`
- Allows efficient ancestor lookup without recursive queries
- Maintained automatically by EF Core

### Permission Filtering
- Users only see cases for diseases they have access to
- TempData results filtered by `DiseaseAccessService`
- Prevents cross-disease data leakage

### Performance Considerations
- `GetEffectiveAddressSettingsAsync` cached per request (scoped service)
- Ancestor lookup uses single query with `AsNoTracking()`
- Review window calculation uses indexed `DateOfOnset` field

## Future Enhancements (Optional)

1. **UI Modal for Address Review**: Display flagged cases in modal instead of TempData
2. **PostGIS Integration**: Automatic jurisdiction detection via geospatial boundaries
3. **Batch Operations**: Apply address changes to multiple patients at once
4. **Notification System**: Email/SMS alerts for jurisdiction crossings
5. **Address History Table**: Track all address changes over time (full temporal model)

## Migrations Applied

1. **20260331121724_AddCaseAddressSnapshotAndDiseaseTimeWindows_Updated**
   - Added 8 fields to `Cases` table
   - Added 4 fields to `Diseases` table

2. **20260331215823_AddInheritAddressSettingsFromParent**
   - Added `InheritAddressSettingsFromParent` to `Diseases` table (default: `true`)

## Files Modified

### Models
- `Models/Case.cs` - Added 8 address snapshot fields
- `Models/Lookups/Disease.cs` - Added 5 address management fields

### Services
- `Services/IPatientAddressService.cs` - Interface with 5 methods
- `Services/PatientAddressService.cs` - Full implementation (~550 lines)

### Pages
- `Pages/Cases/Create.cshtml.cs` - Integrated address snapshot on case creation
- `Pages/Patients/Edit.cshtml.cs` - Integrated address change detection
- `Pages/Settings/Diseases/Edit.cshtml` - Added UI for address settings

### Program.cs
- Registered `IPatientAddressService` in DI container

## Testing Checklist

- [ ] Create parent disease with address settings configured
- [ ] Create child disease with `InheritAddressSettingsFromParent=true`
- [ ] Verify child inherits parent settings
- [ ] Create case for child disease
- [ ] Verify address snapshotted to case
- [ ] Edit patient address (within time window)
- [ ] Verify case flagged for review
- [ ] Edit patient address (outside time window)
- [ ] Verify case NOT flagged
- [ ] Change patient to different jurisdiction
- [ ] Verify automatic ReviewQueue entry created
- [ ] Set `CaseAddressManualOverride=true` on a case
- [ ] Edit patient address
- [ ] Verify manual override case skipped
- [ ] Toggle `InheritAddressSettingsFromParent` on child disease
- [ ] Verify UI fields disabled/enabled appropriately

## Author Notes

This implementation follows the existing architectural patterns in Sentinel:
- Service layer abstraction (interface + implementation)
- DI-based registration
- Migration-driven schema changes
- Razor Pages with server-side logic
- Permission-based filtering
- Review queue integration
- PathIds-based hierarchy (same as SurveyTemplate versioning)

The design prioritizes data integrity over convenience:
- Jurisdiction crossings are non-bypassable
- Manual overrides are sacred
- Historical data is immutable (snapshot approach)
- Server-side operations prevent partial corruption
