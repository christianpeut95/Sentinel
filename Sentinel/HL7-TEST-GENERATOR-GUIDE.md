# HL7 Test File Generator - User Guide

## Overview
The HL7 Test File Generator is a built-in tool for creating realistic test HL7 messages to validate system configuration and workflow. It's accessible to Administrators at **Settings → HL7 Integration → Generate Test Files**.

---

## Architecture

### Hybrid Approach
- **Separate Library**: `Sentinel.HL7Generator` (class library)
  - Reusable message generation logic
  - Lab template definitions
  - Fake data generators
  - Independent of Sentinel domain models

- **In-App Integration**: `Sentinel` (main app)
  - Blazor UI page (`/settings/hl7/generate-test-files`)
  - Service bridge (`HL7TestMessageService`)
  - Database persistence for history & templates
  - Processing result tracking

---

## Key Features

### 1. **Lab Template Selection**
Pre-configured formats for common labs:
- Quest Diagnostics
- LabCorp
- Hospital Lab
- Reference Lab
- Generic HL7 2.5.1

Each template includes default sending application, facility, and format characteristics.

### 2. **Patient & Provider Modes**

**Patient Modes:**
- **Random**: Generate new fake patient each time
- **Existing**: Select from database
- **Custom**: Enter specific patient details

**Provider Modes:**
- **Random**: Generate fake ordering provider
- **Existing**: Select from database
- **Custom**: Enter specific NPI/name

### 3. **Biomarker Configuration**
- Add multiple biomarkers per message
- Configure test codes, LOINC codes
- Set results (POSITIVE, NEGATIVE, DETECTED, NOT DETECTED)
- Add quantitative values & units
- Set abnormal flags

### 4. **Accession Number Options**
- Auto-generate unique number
- Use custom number
- Optionally add lab comments

### 5. **Output Destination**
- Select existing HL7 configuration (auto-processing)
- Or specify custom file path for manual placement

### 6. **Batch Generation**
Generate 1-50 files with automatic variations:
- New patients
- New providers
- New accession numbers
- Timestamp variations

### 7. **Template Save/Replay**
- Save configurations as reusable templates
- Load saved templates to recreate test scenarios
- Great for regression testing

### 8. **Processing Results Panel**
Right-side panel shows:
- Recently generated messages
- Live processing status from Sentinel
- Case creation results
- Entity extraction success
- Parsing/processing errors
- Quick actions: regenerate with new patient, clone exact message

---

## Workflow Examples

### Scenario 1: Test New Lab Configuration
1. Navigate to Generate Test Files
2. Select lab template matching the new lab
3. Choose "Random" patient/provider
4. Add biomarkers for target disease (e.g., Chlamydia)
5. Select the new HL7 configuration as output
6. Add test comment: "Testing new LabCorp config - Chlamydia positive"
7. Generate
8. Watch processing panel for case creation confirmation

### Scenario 2: Batch Test Multiple Formats
1. Configure a standard message template
2. Set batch count to 10
3. Generate
4. Review processing results to ensure all 10 parsed correctly

### Scenario 3: Reusable Test Suite
1. Create a test message for Disease A
2. Check "Save as template" → name it "Disease A - Positive Result"
3. Generate
4. Click "Load Template" → select saved template
5. Modify as needed and generate again
6. Repeat for Disease B, C, etc.

### Scenario 4: Debug Parsing Issue
1. Load recent problematic message from history
2. Click "Clone" to regenerate identical message
3. Modify biomarker or field causing issue
4. Regenerate and compare processing results

---

## Database Schema

### `HL7TestMessageTemplates`
Stores reusable configurations:
- Template name
- Lab template type
- Full configuration JSON
- Test comment
- Audit fields (created by, created at)

### `HL7TestMessageHistory`
Tracks all generated messages:
- Raw HL7 content
- Output file path
- Accession number
- Patient MRN
- Configuration snapshot (JSON)
- Linked `HL7MessageId` (after processing)
- Processing status & result
- Auto-process flag
- Test comment
- Audit fields

---

## Service Layer

### `IHL7TestMessageService` / `HL7TestMessageService`
Bridge between UI and generator library:

**Generation:**
- `GenerateAndSaveMessageAsync()` - Single message
- `GenerateMultipleMessagesAsync()` - Batch with variations

**Templates:**
- `SaveTemplateAsync()` - Persist configuration
- `LoadTemplateAsync()` - Reload saved config
- `GetTemplatesAsync()` - List all templates

**History & Results:**
- `GetRecentHistoryAsync()` - Recent messages
- `GetProcessingResultAsync()` - Retrieve Sentinel processing outcome
- `RegenerateWithNewPatientAsync()` - Clone config with new patient
- `CloneMessageAsync()` - Exact replica

**Helpers:**
- `GenerateRandomPatient()` - Fake patient data
- `GenerateRandomProvider()` - Fake provider data
- `GenerateAccessionNumber()` - Unique accession
- `GetPatientsForSelectionAsync()` - Database patient lookup
- `GetProvidersForSelectionAsync()` - Database provider lookup
- `GetActiveConfigurationsAsync()` - Available HL7 configs

---

## Technical Details

### Generator Library Components

**`HL7MessageBuilder`**
- Constructs HL7 ORU^R01 messages
- MSH, PID, OBR, OBX segments
- Proper delimiters and encoding

**`FakeDataGenerator`**
- Realistic patient names, DOB, addresses
- Provider names, NPIs
- MRNs, accession numbers
- Message control IDs

**`LabTemplateFactory`**
- Creates preconfigured templates
- Returns template descriptions
- Applies defaults to requests

**`HL7GeneratorService`**
- Main library entry point
- Orchestrates builder, data generation
- Multi-message generation with variations

---

## Development Notes

### Adding New Lab Templates
1. Add enum value to `LabTemplateType`
2. Update `LabTemplateFactory.CreateTemplate()` switch
3. Add description in `GetTemplateDescription()`
4. Update Blazor dropdown in `GenerateTestFiles.razor`

### Extending Biomarker Configuration
Modify `BiomarkerResult` model in `Sentinel.HL7Generator/Models/HL7MessageRequest.cs`

### Customizing Fake Data
Edit `FakeDataGenerator` service for different names, address patterns, etc.

### Processing Result Integration
Service queries Sentinel models:
- `HL7Message` (parsed status)
- `Case` (linked case)
- `Patient` (extracted entities)
- `Provider` (extracted entities)
- `LabResult` (extracted results)

---

## Future Enhancements

**Possible Additions:**
- Export/import template library (JSON)
- Schedule automated test runs
- Compare processing outcomes across versions
- Visual diff for message variations
- Integration with CI/CD for regression testing
- Template library sharing across deployments

---

## Troubleshooting

**Generator not creating files:**
- Check output path permissions
- Verify HL7 configuration is active
- Review application logs

**Messages not processing:**
- Ensure HL7 file watcher service is running
- Check configuration file drop path matches output
- Verify file permissions on drop folder

**Template won't load:**
- Check database connection
- Verify template ID exists in `HL7TestMessageTemplates`
- Review service logs for deserialization errors

**Processing results not appearing:**
- Wait 5-10 seconds for async processing
- Refresh history panel
- Check `HL7Messages` table for parsing status

---

## Access & Permissions

**Required Role:** Administrator

**UI Location:** Settings → HL7 Integration → Generate Test Files

**Route:** `/settings/hl7/generate-test-files`

---

## Best Practices

1. **Use descriptive test comments** - Future you will thank you
2. **Save templates for critical scenarios** - Speeds up regression testing
3. **Test with random data first** - Validates parsing logic
4. **Test with existing patients second** - Validates entity linking
5. **Review processing results** - Don't just generate and walk away
6. **Clean up test data periodically** - Delete old test cases/patients
7. **Document configuration changes** - Link test comments to tickets/changes

---

*Last Updated: 2026-01-24*
