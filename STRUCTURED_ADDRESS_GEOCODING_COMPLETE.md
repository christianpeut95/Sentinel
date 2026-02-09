# Structured Address & Geocoding Fields for ExposureEvent - COMPLETE ?

## Overview

Enhanced the `ExposureEvent` model with structured address fields and geocoding data to enable:
- Better reporting by suburb/postcode
- Mapping capabilities with coordinates  
- Clear designation of primary reporting exposure per case
- Structured data for analytics and dashboards

## What Was Added

### 1. Structured Address Fields

| Field | Type | Length | Purpose |
|-------|------|--------|---------|
| `AddressLine` | string | 200 | Street address (e.g., "45 Beach Road") |
| `City` | string | 100 | Suburb/City (e.g., "Cairns") - **Indexed for reporting** |
| `State` | string | 100 | State/Region (e.g., "Queensland") - **Indexed for reporting** |
| `PostalCode` | string | 20 | Postcode (e.g., "4870") - **Indexed for reporting** |
| `Country` | string | 100 | Country name (e.g., "Australia") |

### 2. Geocoding Fields

| Field | Type | Precision | Purpose |
|-------|------|-----------|---------|
| `Latitude` | decimal? | 18,6 | GPS latitude (-90 to 90) |
| `Longitude` | decimal? | 18,6 | GPS longitude (-180 to 180) |
| `GeocodingAccuracy` | string | 50 | Accuracy level (e.g., "ROOFTOP", "APPROXIMATE") |
| `GeocodedDate` | DateTime? | - | When geocoding was performed |

**Note:** Lat/Lon stored as `decimal(18,6)` provides ~11cm precision, sufficient for epidemiological mapping.

### 3. Reporting Exposure Flag

| Field | Type | Purpose |
|-------|------|---------|
| `IsReportingExposure` | bool | **Marks the primary exposure to use for official reporting** |

**Business Rule:** Only ONE exposure per case should have `IsReportingExposure = true`

## Database Migration

**Migration:** `20260206064642_AddStructuredAddressAndGeocodingToExposureEvent`

### Fields Added:
```sql
ALTER TABLE ExposureEvents ADD
    AddressLine nvarchar(200) NULL,
    City nvarchar(100) NULL,
    State nvarchar(100) NULL,
    PostalCode nvarchar(20) NULL,
    Country nvarchar(100) NULL,
    Latitude decimal(18,6) NULL,
    Longitude decimal(18,6) NULL,
    GeocodingAccuracy nvarchar(50) NULL,
    GeocodedDate datetime2 NULL,
    IsReportingExposure bit NOT NULL DEFAULT 0
```

### Indexes Created:
```sql
CREATE INDEX IX_ExposureEvents_City ON ExposureEvents(City)
CREATE INDEX IX_ExposureEvents_State ON ExposureEvents(State)
CREATE INDEX IX_ExposureEvents_PostalCode ON ExposureEvents(PostalCode)
CREATE INDEX IX_ExposureEvents_Latitude_Longitude ON ExposureEvents(Latitude, Longitude)
CREATE INDEX IX_ExposureEvents_IsReportingExposure ON ExposureEvents(IsReportingExposure)
```

## Auto-Create Logic Updated

**File:** `Surveillance-MVP\Pages\Cases\Create.cshtml.cs`

When auto-creating exposure from patient's residential address:

```csharp
var exposureEvent = new ExposureEvent
{
    // ... other fields ...
    
    // Structured address fields
    AddressLine = patient.AddressLine,
    City = patient.City,
    State = patient.State,
    PostalCode = patient.PostalCode,
    Country = "Australia",
    
    // Legacy free-text (for backward compatibility)
    FreeTextLocation = $"{patient.AddressLine}, {patient.City}, {patient.State} {patient.PostalCode}",
    
    // Geocoding from patient if available
    Latitude = patient.Latitude.HasValue ? (decimal?)patient.Latitude.Value : null,
    Longitude = patient.Longitude.HasValue ? (decimal?)patient.Longitude.Value : null,
    
    // Flags
    IsDefaultedFromResidentialAddress = true,
    IsReportingExposure = true, // First exposure = reporting exposure
};
```

## Use Cases Enabled

### 1. Reporting by Geographic Area

**Query cases by suburb:**
```sql
SELECT 
    d.Name AS Disease,
    ee.City AS Suburb,
    COUNT(*) AS CaseCount
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE ee.State = 'Queensland'
  AND ee.City IS NOT NULL
  AND c.DateOfOnset >= '2025-01-01'
GROUP BY d.Name, ee.City
ORDER BY CaseCount DESC
```

**Query cases by postcode:**
```sql
SELECT 
    ee.PostalCode,
    ee.City,
    d.Name AS Disease,
    COUNT(*) AS CaseCount
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE ee.State = 'Queensland'
  AND ee.PostalCode IS NOT NULL
GROUP BY ee.PostalCode, ee.City, d.Name
ORDER BY CaseCount DESC
```

### 2. Mapping Cases by Coordinates

**Get all cases with geocoded exposures:**
```sql
SELECT 
    c.FriendlyId AS CaseID,
    d.Name AS Disease,
    ee.AddressLine + ', ' + ee.City AS Location,
    ee.Latitude,
    ee.Longitude,
    ee.GeocodingAccuracy,
    c.DateOfOnset
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE ee.Latitude IS NOT NULL 
  AND ee.Longitude IS NOT NULL
  AND c.DateOfOnset >= '2025-01-01'
ORDER BY c.DateOfOnset DESC
```

### 3. Find Reporting Exposure for Each Case

**Get primary exposure location per case:**
```sql
SELECT 
    c.FriendlyId AS CaseID,
    p.GivenName + ' ' + p.FamilyName AS PatientName,
    d.Name AS Disease,
    ee.AddressLine,
    ee.City,
    ee.State,
    ee.PostalCode,
    ee.Latitude,
    ee.Longitude
FROM Cases c
JOIN Patients p ON c.PatientId = p.Id
JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
WHERE c.DateOfOnset >= '2025-01-01'
ORDER BY c.DateOfOnset DESC
```

### 4. Detect Cases Without Reporting Exposure

**Find cases missing primary exposure:**
```sql
SELECT 
    c.FriendlyId AS CaseID,
    d.Name AS Disease,
    c.DateOfOnset,
    COUNT(ee.Id) AS ExposureCount,
    SUM(CASE WHEN ee.IsReportingExposure = 1 THEN 1 ELSE 0 END) AS ReportingExposureCount
FROM Cases c
JOIN Diseases d ON c.DiseaseId = d.Id
LEFT JOIN ExposureEvents ee ON ee.CaseId = c.Id
WHERE d.ExposureTrackingMode IN (2, 3) -- LocalSpecificRegion or OverseasAcquired
GROUP BY c.Id, c.FriendlyId, d.Name, c.DateOfOnset
HAVING SUM(CASE WHEN ee.IsReportingExposure = 1 THEN 1 ELSE 0 END) = 0
ORDER BY c.DateOfOnset DESC
```

## Reporting Examples

### Example 1: Disease Distribution by Suburb

```sql
-- Ross River Virus cases by suburb in Queensland
SELECT 
    ee.City AS Suburb,
    ee.PostalCode,
    COUNT(*) AS CaseCount,
    MIN(c.DateOfOnset) AS FirstCase,
    MAX(c.DateOfOnset) AS LastCase
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE d.Name = 'Ross River Virus'
  AND ee.State = 'Queensland'
  AND c.DateOfOnset >= DATEADD(month, -6, GETDATE())
GROUP BY ee.City, ee.PostalCode
ORDER BY CaseCount DESC
```

### Example 2: Cluster Detection by Proximity

```sql
-- Find cases within 1km of each other (using Haversine approximation)
SELECT 
    c1.FriendlyId AS Case1,
    c2.FriendlyId AS Case2,
    d.Name AS Disease,
    ee1.City AS Location,
    SQRT(
        POWER(111.32 * (ee2.Latitude - ee1.Latitude), 2) +
        POWER(111.32 * (ee2.Longitude - ee1.Longitude) * COS(ee1.Latitude / 57.2958), 2)
    ) AS DistanceKm,
    ABS(DATEDIFF(day, c1.DateOfOnset, c2.DateOfOnset)) AS DaysBetween
FROM Cases c1
JOIN ExposureEvents ee1 ON ee1.CaseId = c1.Id AND ee1.IsReportingExposure = 1
JOIN Cases c2 ON c2.DiseaseId = c1.DiseaseId AND c2.Id > c1.Id
JOIN ExposureEvents ee2 ON ee2.CaseId = c2.Id AND ee2.IsReportingExposure = 1
JOIN Diseases d ON c1.DiseaseId = d.Id
WHERE ee1.Latitude IS NOT NULL 
  AND ee2.Latitude IS NOT NULL
  AND c1.DateOfOnset >= DATEADD(month, -3, GETDATE())
  -- Distance filter (approximate 1km)
  AND ABS(ee2.Latitude - ee1.Latitude) < 0.01
  AND ABS(ee2.Longitude - ee1.Longitude) < 0.01
ORDER BY DistanceKm
```

### Example 3: State-Level Summary Report

```sql
-- Case counts by state for diseases requiring exposure tracking
SELECT 
    ee.State,
    d.Name AS Disease,
    COUNT(*) AS CaseCount,
    COUNT(CASE WHEN ee.Latitude IS NOT NULL THEN 1 END) AS GeocodedCount,
    CAST(COUNT(CASE WHEN ee.Latitude IS NOT NULL THEN 1 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS GeocodingRate
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE c.DateOfOnset >= DATEADD(year, -1, GETDATE())
  AND ee.State IS NOT NULL
GROUP BY ee.State, d.Name
ORDER BY ee.State, CaseCount DESC
```

## Integration with Existing Features

### Backward Compatibility
? **FreeTextLocation** still populated for legacy support
? Existing exposure records without structured fields still work
? Queries can use either structured or free-text fields

### Geocoding Service Integration
When geocoding is performed (future enhancement):
```csharp
exposure.Latitude = geocodedResult.Latitude;
exposure.Longitude = geocodedResult.Longitude;
exposure.GeocodingAccuracy = geocodedResult.Accuracy; // "ROOFTOP" | "APPROXIMATE"
exposure.GeocodedDate = DateTime.UtcNow;

// Also populate structured address if not already set
if (string.IsNullOrEmpty(exposure.City))
    exposure.City = geocodedResult.City;
if (string.IsNullOrEmpty(exposure.PostalCode))
    exposure.PostalCode = geocodedResult.PostalCode;
```

### Exposure Form Updates (Future)
The exposure create/edit forms should be updated to:
- Display structured address fields
- Allow user to select which exposure is the "reporting exposure"
- Validate that only one exposure per case has IsReportingExposure = true
- Show geocoding status and allow manual geocoding trigger

## Business Rules

### IsReportingExposure Management

**Rule 1:** Only one exposure per case can have `IsReportingExposure = true`

**Rule 2:** When creating first exposure ? auto-set `IsReportingExposure = true`

**Rule 3:** When user sets a different exposure as reporting exposure:
```csharp
// Unset all other exposures for this case
var otherExposures = await _context.ExposureEvents
    .Where(e => e.CaseId == caseId && e.Id != selectedExposureId && e.IsReportingExposure)
    .ToListAsync();
    
foreach (var exposure in otherExposures)
{
    exposure.IsReportingExposure = false;
}

// Set the selected one
selectedExposure.IsReportingExposure = true;
await _context.SaveChangesAsync();
```

**Rule 4:** If reporting exposure is deleted ? set next exposure as reporting (if any exist)

## Testing

### Test Case 1: Auto-Create with Structured Address

**Steps:**
1. Create patient with full address:
   - Address: "45 Beach Road"
   - City: "Cairns"
   - State: "Queensland"
   - PostalCode: "4870"
2. Create case for Ross River Virus
3. System auto-creates exposure

**Expected Result:**
```sql
SELECT 
    AddressLine,
    City,
    State,
    PostalCode,
    Country,
    IsDefaultedFromResidentialAddress,
    IsReportingExposure
FROM ExposureEvents
WHERE CaseId = '<case-id>'
```

Should return:
| Field | Value |
|-------|-------|
| AddressLine | 45 Beach Road |
| City | Cairns |
| State | Queensland |
| PostalCode | 4870 |
| Country | Australia |
| IsDefaultedFromResidentialAddress | 1 |
| IsReportingExposure | 1 |

### Test Case 2: Query by Suburb

```sql
-- Should return cases in Cairns
SELECT c.FriendlyId, d.Name, ee.City
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE ee.City = 'Cairns'
```

### Test Case 3: Query by Postcode

```sql
-- Should return cases in 4870
SELECT c.FriendlyId, d.Name, ee.PostalCode, ee.City
FROM Cases c
JOIN ExposureEvents ee ON ee.CaseId = c.Id AND ee.IsReportingExposure = 1
JOIN Diseases d ON c.DiseaseId = d.Id
WHERE ee.PostalCode = '4870'
```

## Future Enhancements

### Phase 1: Exposure Form Updates
- [ ] Update Create/Edit forms to show structured address fields
- [ ] Add radio button to select reporting exposure
- [ ] Add validation to ensure only one reporting exposure
- [ ] Show geocoding status with "Geocode Now" button

### Phase 2: Automatic Geocoding
- [ ] Auto-geocode new exposures using Google Maps API
- [ ] Batch geocoding tool for existing exposures
- [ ] Show geocoding accuracy indicator
- [ ] Allow manual coordinate override

### Phase 3: Mapping & Visualization
- [ ] Case map showing all geocoded exposures
- [ ] Cluster detection algorithm
- [ ] Heatmap by suburb/postcode
- [ ] Distance calculator between cases

### Phase 4: Reporting Dashboard
- [ ] Cases by suburb report
- [ ] Cases by postcode report
- [ ] State-level summary
- [ ] Geocoding completeness metrics
- [ ] Export to shapefile/GeoJSON

## Files Modified

1. **Surveillance-MVP\Models\ExposureEvent.cs**
   - Added 10 new properties (address fields + geocoding + IsReportingExposure)

2. **Surveillance-MVP\Data\ApplicationDbContext.cs**
   - Configured decimal precision for Lat/Lon
   - Added indexes for reporting queries

3. **Surveillance-MVP\Pages\Cases\Create.cshtml.cs**
   - Updated auto-create logic to populate structured fields
   - Set IsReportingExposure = true for first exposure

4. **Surveillance-MVP\Migrations\20260206064642_AddStructuredAddressAndGeocodingToExposureEvent.cs**
   - Database migration with all new fields and indexes

## Build Status

? **Build Successful**
- All compilation errors resolved
- Type conversions handled (double ? decimal)
- Migration generated correctly

## Deployment Steps

1. **Apply Migration:**
   ```bash
   dotnet ef database update
   ```

2. **Verify Migration:**
   ```sql
   SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE
   FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_NAME = 'ExposureEvents'
     AND COLUMN_NAME IN ('AddressLine', 'City', 'State', 'PostalCode', 'Latitude', 'Longitude', 'IsReportingExposure')
   ```

3. **Test Auto-Create:**
   - Create new case with patient that has address
   - Verify structured fields populated

4. **Test Reporting Queries:**
   - Run suburb query
   - Run postcode query
   - Verify performance with indexes

## Summary

This enhancement provides:

? **Structured address data** for better reporting
? **Geocoding fields** for mapping capabilities
? **IsReportingExposure flag** for clear primary exposure designation
? **Indexed fields** for fast reporting queries
? **Backward compatibility** with existing free-text field
? **Auto-population** from patient address
? **Ready for geocoding integration**

The system now supports comprehensive epidemiological reporting by geographic area (suburb, postcode, state) and enables future mapping and cluster detection features.

---

**Implementation Date:** February 6, 2025  
**Migration:** 20260206064642  
**Build Status:** ? Successful  
**Ready for:** Database Update & Testing
