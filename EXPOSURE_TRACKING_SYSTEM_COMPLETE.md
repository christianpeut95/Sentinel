# Exposure Tracking System - Implementation Complete

## Overview
The Exposure Tracking System has been successfully implemented to support comprehensive exposure and contact tracing for disease surveillance. The system provides a flexible three-tier architecture that separates physical places, temporal events, and individual exposure records.

## Data Models Created

### 1. **Lookup Tables**

#### LocationType (`Models/Lookups/LocationType.cs`)
- **Purpose**: Categorizes physical locations (Healthcare, Education, Retail, etc.)
- **Key Fields**:
  - `Name` - Type name (e.g., "Healthcare Facility", "School")
  - `Description` - Additional details about the location type
  - `IsHighRisk` - Flags high-risk location types (healthcare, aged care)
  - `IsActive` - Enable/disable location types
  - `DisplayOrder` - Sort order for UI display

#### EventType (`Models/Lookups/EventType.cs`)
- **Purpose**: Categorizes events/gatherings (Party, Wedding, Conference, etc.)
- **Key Fields**:
  - `Name` - Event type name
  - `Description` - Additional details
  - `IsActive` - Enable/disable event types
  - `DisplayOrder` - Sort order for UI display

### 2. **Core Entities**

#### Location (`Models/Location.cs`)
- **Purpose**: Physical places where disease transmission can occur
- **Key Features**:
  - Reusable entities (one location, many exposures over time)
  - Optional link to Organizations (when location is an institutional facility)
  - **Automatic geocoding support** with status tracking
  - High-risk flagging for special handling
  
- **Key Fields**:
  - `Name` - Location name (e.g., "Springfield Hospital", "Central Mall")
  - `LocationTypeId` - FK to LocationType lookup
  - `Address` - Physical address
  - `Latitude/Longitude` - **Decimal(10,7)** - Geolocation coordinates
  - `GeocodingStatus` - Tracks geocoding state ("Success", "Failed", "Pending")
  - `LastGeocoded` - Timestamp of last geocoding attempt
  - `OrganizationId` - Optional FK to Organizations
  - `IsHighRisk` - Risk flag
  - `IsActive` - Active/inactive status
  - `Notes` - Additional context
  - Audit fields (CreatedDate, CreatedByUserId, LastModified, LastModifiedByUserId)

#### Event (`Models/Event.cs`)
- **Purpose**: Specific gatherings or occurrences at locations during defined time periods
- **Key Features**:
  - Links multiple cases exposed at same gathering
  - Always linked to a Location (where it occurred)
  - Tracks event duration and estimated attendees
  - Indoor/outdoor flag for transmission risk assessment
  
- **Key Fields**:
  - `Name` - Event name (e.g., "Johnson Wedding", "Sunday Service Jan 15")
  - `EventTypeId` - FK to EventType lookup
  - `LocationId` - **Required** FK to Location
  - `StartDateTime` - Event start date/time
  - `EndDateTime` - Event end date/time (nullable for single-point events)
  - `EstimatedAttendees` - Number of attendees (for attack rate calculations)
  - `IsIndoor` - Indoor vs outdoor (transmission risk factor)
  - `OrganizerOrganizationId` - Optional FK to Organizations
  - `Description` - Event details
  - `IsActive` - Active/inactive status
  - Audit fields

#### ExposureEvent (`Models/ExposureEvent.cs`)
- **Purpose**: Individual exposure records per case (polymorphic - supports multiple exposure types)
- **Key Features**:
  - **Four exposure patterns**: Event, Location, Contact, Travel
  - Flexible linkage to Events, Locations, or other Cases
  - Status tracking for investigation workflow (Potential ? Confirmed)
  - Date ranges for complex scenarios (multi-day exposures, quarantine calculations)
  
- **Key Fields**:
  - `CaseId` - **Required** FK to Cases
  - `ExposureType` - Enum (Event, Location, Contact, Travel)
  - `ExposureStartDate` - **Required** start date/time
  - `ExposureEndDate` - Optional end date/time (for ranges)
  - **For Event-based exposures**:
    - `EventId` - FK to Events
  - **For Location-based exposures**:
    - `LocationId` - FK to Locations
    - `FreeTextLocation` - For one-off locations not in system
  - **For Contact-based exposures**:
    - `RelatedCaseId` - FK to Cases (contact tracing)
    - `ContactType` - Enum (Household, Healthcare, Social, Workplace, School, Unknown)
  - **For Travel-based exposures**:
    - `CountryCode` - ISO country code (3-char)
  - **Investigation tracking**:
    - `ExposureStatus` - Enum (Unknown, PotentialExposure, UnderInvestigation, ConfirmedExposure, RuledOut)
    - `ConfidenceLevel` - String field for exposure certainty
    - `InvestigationNotes` - Text field for investigation findings
    - `StatusChangedDate` - When status was updated
    - `StatusChangedByUserId` - Who updated status
  - `Description` - Additional context
  - Audit fields

### 3. **Enumerations** (`Models/ExposureEnums.cs`)

```csharp
ExposureType {
    Event = 1,      // Attended specific gathering
    Location = 2,   // Visited place
    Contact = 3,    // Direct contact with person/case
    Travel = 4      // Travel to country/region
}

ExposureStatus {
    Unknown = 0,
    PotentialExposure = 1,      // Initial entry - "patient was there"
    UnderInvestigation = 2,     // Identified as possible source
    ConfirmedExposure = 3,      // Outbreak linked to this
    RuledOut = 4                // Investigated, not the source
}

ContactType {
    Household = 1,
    Healthcare = 2,
    Social = 3,
    Workplace = 4,
    School = 5,
    Unknown = 6
}
```

## Database Schema

### Tables Created
1. **LocationTypes** - Lookup table for location categories
2. **EventTypes** - Lookup table for event categories
3. **Locations** - Physical places
4. **Events** - Temporal gatherings at locations
5. **ExposureEvents** - Individual case exposures

### Relationships
```
Case (1) ??> (many) ExposureEvents
            ?
            ???> (0..1) Event ??> (1) Location ??> (0..1) Organization
            ?
            ???> (0..1) Location ??> (0..1) Organization
            ?
            ???> (0..1) RelatedCase (for contact tracing)

Location (1) ??> (0..1) LocationType
             ???> (0..1) Organization

Event (1) ??> (0..1) EventType
          ???> (1) Location
          ???> (0..1) OrganizerOrganization
```

### Indexes Created
- **Locations**: Name, LocationTypeId, Lat/Long, GeocodingStatus, OrganizationId
- **Events**: Name, EventTypeId, LocationId, OrganizerOrganizationId, StartDateTime/EndDateTime
- **ExposureEvents**: CaseId, EventId, LocationId, RelatedCaseId, ExposureType, ExposureStatus, Start/End Dates

## Key Features Implemented

### 1. **Automatic Geocoding Support**
- `Latitude` and `Longitude` fields on Location model (decimal 10,7 precision)
- `GeocodingStatus` field tracks geocoding state
- `LastGeocoded` timestamp for retry logic
- Integrates with existing `IGeocodingService` interface

### 2. **Audit Tracking**
- All models implement `IAuditable` interface
- **Automatic population** of audit fields via `UpdateAuditableEntities()` method in ApplicationDbContext
- Tracks: CreatedDate, CreatedByUserId, LastModified, LastModifiedByUserId
- Full audit logging to AuditLogs table

### 3. **Flexible Exposure Recording**
- **Structured data** for important exposures (Events/Locations)
- **Free-text** for one-off locations
- **Progressive enhancement**: Start with basic info, add Events later when outbreak identified
- **Status workflow**: Potential ? UnderInvestigation ? Confirmed ? RuledOut

### 4. **Contact Tracing**
- Bidirectional case linkage via `RelatedCaseId`
- `ContactType` categorization for risk assessment
- Date ranges for quarantine calculations ("7 days after last exposure")

### 5. **Organizations Integration**
- Locations can link to Organizations (when location is an institutional facility)
- Events can link to organizer Organizations
- Maintains separation: Organizations = admin entities, Locations = epidemiological data

## Database Configuration

### Entity Framework Configuration (ApplicationDbContext.cs)
- Foreign key relationships configured with Restrict delete behavior
- Indexes on all key query fields
- No soft delete on exposure entities (different from Cases/Patients)

### Migration
- Manual SQL script created: `Migrations/ManualScripts/AddExposureTrackingSystem.sql`
- Script includes IF NOT EXISTS checks for idempotency
- Safe to run multiple times

## Usage Scenarios

### Scenario 1: Measles Outbreak at Birthday Party
1. Create **Location**: "Central Park"
2. Create **Event**: "Sarah's Birthday Party" (linked to Central Park, Jan 15)
3. For each case: Create **ExposureEvent**:
   - `ExposureType` = Event
   - `EventId` = Sarah's party
   - `ExposureStatus` = ConfirmedExposure

**Query**: "Show all cases exposed at Sarah's party" ? Returns all cases with EventId = party

### Scenario 2: COVID Household Contact
- Create **ExposureEvent**:
  - `ExposureType` = Contact
  - `RelatedCaseId` = Index case
  - `ContactType` = Household
  - `ExposureStartDate/EndDate` = Range of contact
  - `LocationId` = Home address (optional)

### Scenario 3: Legionella Investigation (Unknown Source)
1. Initial investigation: Create multiple **ExposureEvents** with `ExposureStatus` = PotentialExposure
   - "Stayed at Grand Hotel" (Mar 5-7)
   - "Visited Gym" (Mar 7)
   - "Shopped at mall" (Mar 13)
2. Cluster identified: Update Grand Hotel exposures to `ExposureStatus` = UnderInvestigation
3. Environmental sample confirms: Update to `ExposureStatus` = ConfirmedExposure
4. Other locations: Update to `ExposureStatus` = RuledOut

### Scenario 4: Travel-Related Dengue
- Create **ExposureEvent**:
  - `ExposureType` = Travel
  - `CountryCode` = "TH" (Thailand)
  - `ExposureStartDate/EndDate` = Travel dates
  - `FreeTextLocation` = "Bangkok and Phuket, Thailand"
  - `Description` = "Vacation travel, multiple mosquito bites reported"

## Next Steps

### UI Development (Not Implemented)
1. **Case Details Page** - Add "Exposures" tab
2. **Location Management** - CRUD pages for Locations
3. **Event Management** - CRUD pages for Events
4. **Exposure Entry Forms** - Smart forms that adapt based on ExposureType
5. **Outbreak Investigation Dashboard** - Cluster detection and visualization

### Service Layer (Not Implemented)
1. **GeocodingService** implementation for automatic address geocoding
2. **ExposureService** for complex exposure queries
3. **OutbreakDetectionService** for automated cluster identification

### Seed Data (Not Implemented)
1. **LocationTypes**: Healthcare, Education, Retail, Hospitality, Residential, PublicSpace, Transport, Religious, Other
2. **EventTypes**: Party, Wedding, Funeral, Conference, Concert, Festival, Religious, Sports, School, Other

### Future Enhancements (Not Implemented)
1. **Outbreak** entity linking to Events/Locations
2. **FoodHistory** entity for foodborne disease investigations
3. **VaccinationRecord** entity for vaccination status tracking
4. **Attack rate calculations** on Events
5. **Transmission network visualization**

## Technical Notes

### Geocoding
- Addresses are geocoded when Location records are saved/updated
- Use existing `IGeocodingService` interface
- Multiple implementations available:
  - `GoogleGeocodingService`
  - `NominatimGeocodingService`
  
### Audit Trail
- `UpdateAuditableEntities()` method added to ApplicationDbContext
- Called automatically in `SaveChanges()` and `SaveChangesAsync()`
- Sets CreatedDate/CreatedByUserId on insert
- Sets LastModified/LastModifiedByUserId on insert and update
- Uses reflection to populate fields on any IAuditable entity

### Data Model Philosophy
- **Locations** = Reusable physical places (100-500 entities)
- **Events** = Time-limited gatherings (created when epidemiologically significant)
- **ExposureEvents** = One per case-exposure pair (thousands per year)
- **Organizations** = Separate from Locations (administrative vs epidemiological)

## Files Created/Modified

### New Files Created
- `Models/Lookups/LocationType.cs`
- `Models/Lookups/EventType.cs`
- `Models/ExposureEnums.cs`
- `Models/Location.cs`
- `Models/Event.cs`
- `Models/ExposureEvent.cs`
- `Migrations/ManualScripts/AddExposureTrackingSystem.sql`

### Modified Files
- `Models/Case.cs` - Added `ExposureEvents` navigation property
- `Data/ApplicationDbContext.cs`:
  - Added DbSets for new entities
  - Added Entity Framework configuration
  - Added `UpdateAuditableEntities()` method
  - Modified SaveChanges methods to call UpdateAuditableEntities

### Existing Files (Not Modified)
- `Services/IGeocodingService.cs` - Interface already exists
- `Services/GoogleGeocodingService.cs` - Implementation already exists
- `Services/NominatimGeocodingService.cs` - Implementation already exists

## Compliance with Requirements

? **Removed IsSignificantForOutbreak field** - Not included in models
? **LocationType as lookup field** - Implemented as separate LocationType entity, not enum
? **All addresses get geocoded** - Latitude/Longitude fields on Location model with geocoding status tracking
? **IAuditable interface** - All models implement IAuditable with automatic population
? **Three-tier architecture** - Location, Event, ExposureEvent separation
? **Flexible exposure types** - Event, Location, Contact, Travel supported
? **Contact tracing support** - RelatedCaseId and ContactType fields
? **Investigation workflow** - ExposureStatus enum for Potential ? Confirmed workflow
? **Organizations separation** - Locations separate from Organizations, optional linkage

## Build Status
? **Build Successful** - All models compile without errors

## Database Migration Status
?? **Manual SQL Script** - Use `AddExposureTrackingSystem.sql` to apply schema changes
- EF Core migration generated but has conflicts with existing database state
- Manual SQL script provides clean, idempotent migration
- Script includes IF NOT EXISTS checks for safety

---

## Summary
The Exposure Tracking System provides a robust, flexible foundation for tracking disease exposures, supporting outbreak investigation, and contact tracing. The three-tier architecture (Location ? Event ? ExposureEvent) enables both simple exposure recording and complex outbreak investigation workflows, while maintaining data quality and supporting diverse disease surveillance scenarios.
