# ConfigureLab Testing Guide

## Quick Start Testing

### Prerequisites
1. Have at least one HL7 Configuration created
2. Have a sample HL7 message ready (can use test generator)
3. Navigate to: `https://localhost:7219/Settings/HL7/FieldMappings/SelectLab`

---

## Test Scenario 1: Configuration Scope Indicators ✅

**Objective**: Verify configuration scope is clearly displayed

**Steps**:
1. Click "Configure" on any lab configuration
2. Observe the page header

**Expected Results**:
✅ Blue info box appears below header showing:
   - "Configuration Scope"
   - "These field mappings apply ONLY to messages from:"
   - Sending Facility name (e.g., "Quest Diagnostics")
   - Sending Application name (if set)
   - Link to "View all configurations"

✅ Configuration switcher dropdown shows:
   - Label: "Viewing configuration:"
   - Current config selected (e.g., "Quest Diagnostics (QUEST)")
   - All other configs in dropdown

✅ Options button shows:
   - "⚙️ Options" button in header
   - Dropdown with "Reset all mappings" and "View sample message"

**Pass Criteria**: All three elements visible and displaying correct data

---

## Test Scenario 2: Progress Indicator ✅

**Objective**: Verify mapping progress is displayed accurately

**Steps**:
1. On ConfigureLab page with sample message uploaded
2. Observe progress card above field cards

**Expected Results**:
✅ Progress card shows:
   - Title: "Mapping Progress"
   - Badge: "X / Y Required Fields" (e.g., "3 / 8")
   - Progress bar with green fill (% complete)
   - Breakdown: "X confirmed, Y need attention, Z not found"

✅ Progress updates when you:
   - Confirm a field → confirmed count increases
   - Clear a field → confirmed count decreases
   - Progress bar % recalculates

**Pass Criteria**: Progress accurately reflects field status

---

## Test Scenario 3: Browse All Fields Modal 🎯 PRIMARY TEST

**Objective**: Verify users can manually select fields when auto-detection fails

### 3A: Open Modal from "Not Found" Field

**Steps**:
1. Upload sample message that has some fields not auto-detected
2. Find a field card with status "Not set" and message "Couldn't find this field"
3. Click **"📋 Browse all fields in message"** button

**Expected Results**:
✅ Modal opens full-screen
✅ Modal title shows: "Find Field: [Field Name]"
✅ Left panel shows raw HL7 message (monospace font, scrollable)
✅ Right panel shows field tree grouped by segment

### 3B: Navigate Field Tree

**Steps**:
1. In the modal, observe the field tree on right side
2. Click on segment headers (MSH, PID, OBR, OBX, etc.)

**Expected Results**:
✅ MSH segment is expanded by default
✅ Clicking segment header toggles expand/collapse
✅ Chevron icon changes: ▶ (collapsed) ↔ ▼ (expanded)
✅ Each field shows:
   - Radio button
   - Field path (e.g., "PID-5.1") in green badge
   - Field value (e.g., "SMITH")
   - Field description (e.g., "Patient Last Name")

### 3C: Select and Confirm Field

**Steps**:
1. Expand a segment (e.g., OBR)
2. Click on a field (e.g., OBR-16: "Dr. Smith, John")
3. Observe bottom of modal
4. Click "✓ Confirm Selection" button

**Expected Results**:
✅ Clicking field selects radio button
✅ Selected field info appears at bottom:
   - "Selected: OBR-16 = 'Dr. Smith, John'"
✅ "Confirm Selection" button becomes enabled (was disabled)
✅ Clicking "Confirm Selection":
   - Modal closes
   - Page reloads
   - Field now shows as "Confirmed" with selected value

### 3D: Cancel Without Selection

**Steps**:
1. Open browse fields modal
2. Click "Cancel" button or X in corner

**Expected Results**:
✅ Modal closes without changes
✅ Field remains in "Not set" state

**Pass Criteria**: All steps complete successfully; field mapping saved correctly

---

## Test Scenario 4: Clear Individual Field ✅

**Objective**: Verify users can clear confirmed field mappings

**Steps**:
1. Find a field card with status "✓ Confirmed"
2. Observe the action buttons
3. Click **"❌ Clear"** button
4. Confirm in dialog

**Expected Results**:
✅ Confirmed field shows TWO buttons: "✏️ Change" and "❌ Clear"
✅ Clear button is styled in warning color (orange/red)
✅ Clicking Clear shows confirmation: "Clear the mapping for '[Field Name]'?"
✅ Clicking OK:
   - Page reloads
   - Field status changes to "Not set"
   - Success message: "Cleared mapping for [field]"

**Pass Criteria**: Field mapping removed from database; can be re-mapped

---

## Test Scenario 5: Reset All Mappings ✅

**Objective**: Verify users can reset entire configuration

**Steps**:
1. On ConfigureLab page with some fields confirmed
2. Click **"⚙️ Options"** dropdown
3. Click **"🔄 Reset all mappings"**
4. Confirm in dialog

**Expected Results**:
✅ Options dropdown shows "Reset all mappings" option
✅ Clicking shows confirmation: "Reset all field mappings for this configuration? This cannot be undone."
✅ Clicking OK:
   - Page reloads
   - ALL field mappings cleared (except sample message)
   - All fields back to "Not set"
   - Progress shows "0 / X Required Fields"
   - Success message: "All field mappings have been reset..."

**Pass Criteria**: All mappings cleared; sample message preserved; can re-analyze

---

## Test Scenario 6: Configuration Switcher ✅

**Objective**: Verify users can switch between lab configurations

**Steps**:
1. Ensure you have 2+ HL7 Configurations created
2. On ConfigureLab page, locate "Viewing configuration:" dropdown
3. Select a different configuration
4. Observe page reload

**Expected Results**:
✅ Dropdown lists all lab configurations in format: "Name (Facility)"
✅ Current configuration is selected
✅ Selecting different config:
   - Page navigates to `/ConfigureLab?configId={new-id}`
   - Page reloads with new configuration
   - Configuration scope alert updates with new facility
   - Field mappings are different (specific to new config)

**Pass Criteria**: Switching configs shows correct data per config

---

## Test Scenario 7: View Sample Message ✅

**Objective**: Verify view sample message action

**Steps**:
1. Upload sample message
2. Click "⚙️ Options" → "👁️ View sample message"

**Expected Results**:
✅ Test message modal opens (existing functionality)
✅ Shows preview parsing of sample message

**Pass Criteria**: Modal opens with correct sample data

---

## Test Scenario 8: Integration - End-to-End Configuration

**Objective**: Full workflow from sample upload to save

**Steps**:
1. Navigate to ConfigureLab for a new configuration
2. Upload sample HL7 message
3. Confirm auto-detected fields
4. Use "Browse all fields" for undetected fields
5. Verify progress shows 100%
6. Click "Save configuration"

**Expected Results**:
✅ Each step completes successfully
✅ Progress bar updates after each field confirmation
✅ Final progress shows "8 / 8 Required Fields" (or appropriate count)
✅ Save button becomes enabled when all required fields confirmed
✅ Clicking Save:
   - Success message
   - Redirect to configurations index
   - Configuration marked as "Active"

**Pass Criteria**: Complete configuration saved and active

---

## Test Scenario 9: Edge Cases

### 9A: No Sample Message
**Steps**: Navigate to ConfigureLab without sample  
**Expected**: Upload form displayed; no field cards; no progress indicator

### 9B: Empty Sample Message
**Steps**: Try to upload empty textarea  
**Expected**: Error: "Please paste an HL7 message to analyze"

### 9C: Invalid HL7 Format
**Steps**: Upload text that isn't HL7  
**Expected**: Error: "Could not parse message: [validation errors]"

### 9D: Browse Fields with No Sample
**Steps**: Somehow trigger browse fields without sample (edge case)  
**Expected**: JSON error returned: "No sample message available"

### 9E: Clear Last Required Field
**Steps**: Clear a required field that was confirmed  
**Expected**: Progress decreases; save button becomes disabled

**Pass Criteria**: All edge cases handled gracefully

---

## Test Scenario 10: Performance

**Objective**: Verify acceptable performance

**Steps**:
1. Upload large HL7 message (50+ lines)
2. Click "Browse all fields"
3. Measure modal open time

**Expected Results**:
✅ Modal opens in < 2 seconds
✅ All segments and fields rendered
✅ Scrolling is smooth
✅ No console errors

**Pass Criteria**: Acceptable performance with large messages

---

## Test Scenario 11: Accessibility

### 11A: Keyboard Navigation
**Steps**: Use only keyboard (Tab, Enter, Space, Esc)  
**Expected**: Can navigate all controls, open/close modals, select fields

### 11B: Screen Reader
**Steps**: Test with screen reader (NVDA, JAWS, VoiceOver)  
**Expected**: All elements announced correctly; radio buttons labeled

### 11C: Focus Management
**Steps**: Open modal → Tab through controls → Close modal  
**Expected**: Focus trapped in modal; returns to trigger button on close

**Pass Criteria**: All accessibility requirements met

---

## Regression Testing Checklist

Verify existing functionality still works:

- [ ] Auto-detection still works for common fields
- [ ] Candidate selection still works (radio buttons)
- [ ] "Change" button still works on confirmed fields
- [ ] "Try another sample message" still works
- [ ] "Set a fixed default value" still works
- [ ] "Skip this field" still works
- [ ] Test message modal still works
- [ ] Save configuration still works
- [ ] Redirect to configurations index still works
- [ ] Field mappings used correctly by HL7 processor

**Pass Criteria**: No regressions; all existing features work

---

## Browser Compatibility Testing

Test in each browser:
- [ ] Chrome 120+
- [ ] Edge 120+
- [ ] Firefox 121+
- [ ] Safari 17+ (macOS)

Test responsive layouts:
- [ ] Desktop (≥1200px)
- [ ] Tablet (768px - 1199px)
- [ ] Mobile (<768px)

**Pass Criteria**: Works in all supported browsers and screen sizes

---

## Database Verification

After testing, verify database state:

**Check `HL7FieldMappings` table**:
```sql
-- All mappings should have correct ConfigurationId
SELECT ConfigurationId, TargetEntity, TargetProperty, FieldPath, IsActive
FROM HL7FieldMappings
WHERE ConfigurationId = '{your-test-config-id}'
ORDER BY Priority;
```

**Expected**:
✅ Manually selected fields have correct `FieldPath` (e.g., "OBR-16")
✅ All mappings have `IsActive = true`
✅ Sample message stored in mapping with `TargetEntity = 'Configuration'`
✅ No duplicate mappings for same field

**Pass Criteria**: Database state is correct and consistent

---

## Success Criteria Summary

### Critical (Must Pass)
✅ Browse all fields modal opens and displays all fields correctly  
✅ Manual field selection saves mapping to database  
✅ Configuration scope indicator shows correct facility  
✅ Progress indicator updates accurately  
✅ Reset all clears all mappings  
✅ Clear individual field works  

### Important (Should Pass)
✅ Configuration switcher works  
✅ No regressions in existing functionality  
✅ All edge cases handled gracefully  

### Nice to Have (Should Pass)
✅ Performance acceptable with large messages  
✅ Accessibility requirements met  
✅ Works in all browsers  

---

## Bug Reporting Template

If you find a bug, report with:
```
**Bug**: [Brief description]
**Steps to Reproduce**:
1. [Step 1]
2. [Step 2]
**Expected**: [What should happen]
**Actual**: [What actually happened]
**Browser**: [Chrome/Edge/Firefox/Safari + version]
**Screenshot**: [If applicable]
**Console Errors**: [If any]
```

---

## Test Data Setup

### Sample HL7 Message (Complete)
```
MSH|^~\&|QUEST||SENTINEL||20240315103052||ORU^R01|MSG001|P|2.5
PID|1||MRN123456||SMITH^JANE^M||19850422|F|||123 Main St^^Wellington^^6011||^PRN^PH^^^64^21123456||||||||||||||||||||||
OBR|1|||87798-0^Chlamydia trachomatis NAAT^LN||20240314||||||||||^JONES^ROBERT^J|||||||20240315|||F
OBX|1|ST|87798-0^Chlamydia trachomatis NAAT^LN||POSITIVE||NEGATIVE|A|||F|||20240315
```

### Sample HL7 Message (Minimal - Missing Fields)
```
MSH|^~\&|LAB||HOSP||20240315||ORU^R01|MSG002|P|2.5
PID|1||999||DOE^JOHN||||M
OBR|1|||TEST|||||||||||||||||||20240315|||F
OBX|1|ST|TEST||RESULT||NORMAL|A|||F
```
Use this to test "Not found" → Browse all fields workflow

---

## Post-Testing

After all tests pass:
1. Document any issues found
2. Verify fixes
3. Re-test affected scenarios
4. Update this guide if needed
5. Mark as ready for production

---

## Quick Smoke Test (5 minutes)

If time is limited, run this quick smoke test:

1. ✅ Navigate to ConfigureLab
2. ✅ Verify configuration scope alert visible
3. ✅ Upload sample message
4. ✅ Verify progress indicator visible
5. ✅ Find "Not set" field → Click "Browse all fields"
6. ✅ Select a field → Confirm
7. ✅ Verify field now confirmed
8. ✅ Click "Clear" on confirmed field
9. ✅ Verify field cleared
10. ✅ Click "Options" → "Reset all"
11. ✅ Verify all fields reset

**Pass Criteria**: All 11 steps complete successfully

---

## Conclusion

These tests verify all four major improvements:
1. ✅ Browse all fields functionality
2. ✅ Configuration scope indicators
3. ✅ Reset/clear options
4. ✅ Progress indicators

Complete all critical tests before deploying to production.

**Estimated Testing Time**:
- Quick smoke test: 5 minutes
- Full test scenarios: 30-45 minutes
- Regression + edge cases: 1 hour
- Full suite (all browsers, accessibility): 2 hours
