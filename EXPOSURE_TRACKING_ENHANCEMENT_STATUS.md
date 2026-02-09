# Exposure Tracking Enhancement - Implementation Status

## ? COMPLETED

### 1. Model Changes
- ? Disease Model: Added all exposure tracking properties
  - ExposureTrackingMode enum
  - DefaultToResidentialAddress
  - AlwaysPromptForLocation
  - SyncWithPatientAddressUpdates
  - ExposureGuidanceText
  - RequireGeographicCoordinates
  - AllowDomesticAcquisition
  - ExposureDataGracePeriodDays
  - RequiredLocationTypeIds

- ? ExposureEvent Model: Added new properties
  - IsDefaultedFromResidentialAddress
  - IsInterstateTravel
  - InterstateOriginState

- ? ExposureEnums: Added ExposureTrackingMode enum
  - Optional
  - LocallyAcquired
  - LocalSpecificRegion
  - OverseasAcquired

### 2. Database Migration
- ? Migration created: `20260206014645_AddExposureTrackingEnhancements`
- ? Migration applied to database successfully
- ? Seed data script created: `SeedExposureTrackingConfiguration.sql`
  - Configured 15+ common diseases with appropriate tracking modes
  - Includes Measles, Salmonella, Malaria, TB, COVID-19, etc.

### 3. Service Layer
- ? IExposureRequirementService interface created
- ? ExposureRequirementService implementation complete
  - GetRequirementsForDiseaseAsync()
  - ShouldPromptForExposureAsync()
  - GetDefaultExposureLocationAsync()
  - ValidateExposureCompletenessAsync()
  - GetCasesAffectedByAddressChangeAsync()
  - GetCasesWithMissingExposureDataAsync()
- ? Service registered in Program.cs

### 4. Disease Management UI
- ? Edit.cshtml: Added "Exposure Tracking" tab
  - Exposure tracking mode selector
  - Behavior options (checkboxes for all settings)
  - Data completeness settings
  - Guidance text entry
- ? Edit.cshtml.cs: Added SaveExposureTracking handler
- ? CreateWizard.cshtml: Added Step 5 "Exposure Tracking"
  - Updated wizard to 6 steps (was 5)
  - Added exposure configuration UI
- ? CreateWizard.cshtml.cs: Already handles Disease binding

## ?? TODO - Remaining Implementation

### 5. Case Creation/Edit UI Changes ?? HIGH PRIORITY
**Files to modify:**
- [ ] `Cases/Create.cshtml` and `Cases/Create.cshtml.cs`
  - Add exposure section that shows/hides based on disease selection
  - Display guidance text from disease config
  - Pre-populate with residential address if configured
  - Add validation prompts (soft warnings vs hard validation)
  - Show country picker for OverseasAcquired mode
  - Add interstate travel checkbox for LocallyAcquired mode
  
- [ ] `Cases/Edit.cshtml` and `Cases/Edit.cshtml.cs`
  - Same functionality as Create
  - Plus: Show exposure completeness status
  - Quick "Add Exposure" button if missing

**Implementation approach:**
1. Inject `IExposureRequirementService` into PageModel
2. Load disease requirements in OnGet/OnPost
3. Add conditional section in Razor view
4. Add JavaScript to show/hide based on disease dropdown selection
5. Validate exposure completeness before save

### 6. Patient Address Update Flow ?? MEDIUM PRIORITY
**Files to create/modify:**
- [ ] `Patients/Edit.cshtml.cs`
  - Detect address changes in OnPost
  - Query `GetCasesAffectedByAddressChangeAsync()`
  - Store affected case IDs in TempData or session
  - Redirect to confirmation page/modal

- [ ] Create `Patients/_AddressUpdatePrompt.cshtml` (Partial View)
  - Modal showing list of affected cases
  - Checkboxes for bulk selection
  - "Update Selected" button
  
- [ ] Create `Patients/UpdateExposuresFromAddress` (Handler or separate page)
  - Take selected case IDs
  - Update ExposureEvents with new address
  - Log audit trail

### 7. Dashboard/Incomplete Cases Widget ?? MEDIUM PRIORITY
**Files to create:**
- [ ] Create `ViewModels/IncompleteCaseViewModel.cs`
  - CaseId, CaseNumber, PatientName, DiseaseName
  - NotificationDate, DaysOpen
  - MissingDataType
  - Priority level

- [ ] Modify `Pages/Index.cshtml` (Dashboard) or `Cases/Index.cshtml`
  - Add "Cases Missing Exposure Data" widget
  - Query using `GetCasesWithMissingExposureDataAsync()`
  - Calculate age and color-code by urgency
  - Filter by disease
  - Click to navigate to case edit

### 8. Cases Details Page Enhancement ?? LOW PRIORITY
**Files to modify:**
- [ ] `Cases/Details.cshtml` and `Cases/Details.cshtml.cs`
  - Show exposure requirement status
  - Display grace period countdown if applicable
  - Highlight missing exposure data prominently
  - "Add Exposure" quick button if missing

### 9. Reporting/Analytics ?? FUTURE ENHANCEMENT
**Files to create:**
- [ ] `Pages/Reports/ExposureAnalysis.cshtml`
  - Cases by acquisition type (local vs overseas)
  - Interstate travel patterns
  - Cases by exposure location type
  - Diseases with incomplete exposure data
  - Time series of completeness

### 10. Configuration/Settings Page ?? FUTURE ENHANCEMENT
**Files to modify:**
- [ ] `Settings/Index.cshtml`
  - Add link to system-wide exposure tracking defaults
  - Default grace period
  - Auto-prompt settings

## ?? Priority Implementation Order

### Phase 1: Core Functionality (Do This Next)
1. **Case Create/Edit exposure prompting** - Critical for data entry workflows
   - Start with Cases/Create.cshtml and Cases/Create.cshtml.cs
   - Add disease-based conditional exposure section
   - Implement validation based on tracking mode
   
2. **Run seed data script** - Configure existing diseases
   - Execute `SeedExposureTrackingConfiguration.sql`
   - Test with real diseases in the system

### Phase 2: Data Completeness (Week 2)
3. **Dashboard widget for incomplete cases** - Helps users see what needs attention
4. **Patient address update flow** - Maintains data accuracy

### Phase 3: Enhancements (Week 3-4)
5. **Cases Details page enhancement** - Better visibility
6. **Reporting** - Analytics and insights
7. **System settings** - Fine-tuning

## ?? Testing Checklist

After implementing each phase:

### Phase 1 Testing:
- [ ] Create a new case for Measles - should show exposure section with guidance
- [ ] Create a new case for Malaria - should require country selection
- [ ] Create a new case for Ross River - should pre-fill residential address
- [ ] Create a case for Influenza - should not show exposure section (optional)
- [ ] Try to save incomplete exposure data - check validation works

### Phase 2 Testing:
- [ ] Dashboard shows incomplete cases
- [ ] Update patient address - check for prompt to update cases
- [ ] Update multiple case exposures from address change

### Phase 3 Testing:
- [ ] View case details - see exposure status
- [ ] Run exposure reports
- [ ] Configure system defaults

## ?? Key Files Modified

### Models
- `Models/Lookups/Disease.cs` ?
- `Models/ExposureEvent.cs` ?
- `Models/ExposureEnums.cs` ?

### Services
- `Services/IExposureRequirementService.cs` ?
- `Services/ExposureRequirementService.cs` ?

### Migrations
- `Migrations/20260206014645_AddExposureTrackingEnhancements.cs` ?
- `Migrations/ManualScripts/SeedExposureTrackingConfiguration.sql` ?

### UI - Disease Management
- `Pages/Settings/Diseases/Edit.cshtml` ?
- `Pages/Settings/Diseases/Edit.cshtml.cs` ?
- `Pages/Settings/Diseases/CreateWizard.cshtml` ?
- `Pages/Settings/Diseases/CreateWizard.cshtml.cs` ? (no changes needed)

### UI - Case Management (TODO)
- `Pages/Cases/Create.cshtml` ??
- `Pages/Cases/Create.cshtml.cs` ??
- `Pages/Cases/Edit.cshtml` ??
- `Pages/Cases/Edit.cshtml.cs` ??
- `Pages/Cases/Details.cshtml` ??
- `Pages/Cases/Details.cshtml.cs` ??

### Configuration
- `Program.cs` ? (Service registered)

## ?? Next Steps

**Immediate (Do Now):**
1. Run the seed data script to configure diseases:
   ```sql
   -- Execute in SQL Server Management Studio or Azure Data Studio
   -- Use the SeedExposureTrackingConfiguration.sql script
   ```

2. Test the Disease Edit page exposure tracking tab
   - Navigate to Settings > Diseases > Edit any disease
   - Check that the "Exposure Tracking" tab appears
   - Save exposure settings

3. Test the Disease Create Wizard
   - Settings > Diseases > Create Disease (Wizard)
   - Verify Step 5 "Exposure Tracking" appears

**Next Session:**
Start implementing Case Create/Edit exposure prompting (Phase 1, Item 1)

## ?? Implementation Notes

- Follow the Copilot instructions: Use eager-loading with EF Core Includes
- Use null-safe access in Razor views (`?.Name`)
- Keep UI consistent with existing case forms
- Reuse exposure event creation patterns from existing exposure pages
- Consider mobile responsiveness for the exposure section
- Add proper validation messages
- Log all data changes for audit trail

## ?? Achievements So Far

- ? Complete data model with 9 new disease properties
- ? Complete data model with 3 new exposure event properties  
- ? Full service layer with 6 methods
- ? Admin UI for disease configuration (Edit + Wizard)
- ? Database migration and seed data
- ? Build successful with no errors

**Estimated completion: 60% of total feature**
**Core backend and admin UI: 100% complete**
**Data entry UI: 0% complete (next priority)**
