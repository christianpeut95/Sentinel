# Sentinel Design System Migration Inventory

## Summary
Total pages to update: **~120 pages**

## Status Legend
- ✅ Complete - Page updated with Sentinel design
- 🔄 In Progress - Currently being updated
- ⏳ Pending - Not yet started
- ❌ Skip - Uses component/Blazor (not Razor Page)

---

## Already Completed
- ✅ Pages/Index.cshtml - Dashboard
- ✅ Areas/Identity/Pages/Account/Login.cshtml - Login page
- ✅ Pages/Shared/_LayoutSentinel.cshtml - Main layout
- ✅ wwwroot/css/sentinel-design-system.css - Design system

---

## High Priority (Core User Flows)

### Patients (7 pages - MergePreview doesn't exist)
- ✅ Pages/Patients/Index.cshtml - Patient list (COMPLETE)
- ✅ Pages/Patients/Delete.cshtml - Delete confirmation (COMPLETE)
- ✅ Pages/Patients/Search.cshtml - Search patients (COMPLETE)
- ✅ Pages/Patients/Details.cshtml - Patient details (COMPLETE)
- ✅ Pages/Patients/Merge.cshtml - Merge patients (COMPLETE)
- ✅ Pages/Patients/Create.cshtml - New patient (COMPLETE - 1049 lines)
- ✅ Pages/Patients/Edit.cshtml - Edit patient (COMPLETE - 857 lines, all JS preserved)

### Cases (11 pages)
- ⏳ Pages/Cases/Index.cshtml - Case list
- ✅ Pages/Cases/Details.cshtml - Case details (COMPLETE - user confirmed)
- ⏳ Pages/Cases/Create.cshtml - Create case (from patient)
- ✅ Pages/Cases/CreateNew.cshtml - Create new case (COMPLETE - 1550 lines, 5-step wizard, patient search, modals, all JS preserved)
- ✅ Pages/Cases/Edit.cshtml - Edit case (COMPLETE - 1590 lines, complex form with symptoms, Select2, jurisdictions)
- ✅ Pages/Cases/Search.cshtml - Search cases (COMPLETE - user confirmed)
- ⏳ Pages/Cases/EditTask.cshtml - Edit task
- ⏳ Pages/Cases/AddTask.cshtml - Add task
- ⏳ Pages/Cases/EditExposure.cshtml - Edit exposure
- ⏳ Pages/Cases/EditSymptom.cshtml - Edit symptom
- ⏳ Pages/Cases/Contacts/BulkCreate.cshtml - Bulk create contacts

### Contacts (2 pages)
- ⏳ Pages/Contacts/Details.cshtml - Contact details
- ⏳ Pages/Contacts/Edit.cshtml - Edit contact

### Outbreaks (4 pages)
- ⏳ Pages/Outbreaks/Index.cshtml - Outbreak list
- ⏳ Pages/Outbreaks/Details.cshtml - Outbreak details
- ⏳ Pages/Outbreaks/Tasks.cshtml - Outbreak tasks
- ⏳ Pages/Outbreaks/BulkActions.cshtml - Bulk actions

---

## Medium Priority

### Reports (2 pages)
- ⏳ Pages/Reports/Builder.cshtml - Report builder
- ⏳ Pages/Reports/View.cshtml - View report

### Tasks (2 pages)
- ⏳ Pages/Tasks/CompleteSurvey.cshtml - Complete survey
- ⏳ Pages/Tasks/ViewSurveyResult.cshtml - View survey result

### Data Inbox (2 pages)
- ⏳ Pages/DataInbox/Index.cshtml - Inbox list
- ⏳ Pages/DataInbox/Review.cshtml - Review data

---

## Lower Priority (Settings & Admin)

### Settings - Main (2 pages)
- ⏳ Pages/Settings/Index.cshtml - Settings home
- ⏳ Pages/Settings/About.cshtml - About page
- ⏳ Pages/Settings/Organization.cshtml - Organization settings

### Settings - Diseases (7 pages)
- ⏳ Pages/Settings/Diseases/Index.cshtml
- ⏳ Pages/Settings/Diseases/Create.cshtml
- ⏳ Pages/Settings/Diseases/Edit.cshtml
- ⏳ Pages/Settings/Diseases/Delete.cshtml
- ⏳ Pages/Settings/Diseases/Details.cshtml
- ⏳ Pages/Settings/DiseaseAccess/Index.cshtml
- ⏳ Pages/Settings/DiseaseAccess/ManageRoles.cshtml
- ⏳ Pages/Settings/DiseaseAccess/ManageUsers.cshtml

### Settings - Pathogens (3 pages)
- ⏳ Pages/Settings/Pathogens/Index.cshtml
- ⏳ Pages/Settings/Pathogens/Create.cshtml
- ⏳ Pages/Settings/Pathogens/Edit.cshtml

### Settings - Surveys (7 pages)
- ⏳ Pages/Settings/Surveys/SurveyTemplates.cshtml
- ⏳ Pages/Settings/Surveys/SurveyTemplateDetails.cshtml
- ⏳ Pages/Settings/Surveys/CreateSurveyTemplate.cshtml
- ⏳ Pages/Settings/Surveys/EditSurveyTemplate.cshtml
- ⏳ Pages/Settings/Surveys/DesignSurvey.cshtml
- ⏳ Pages/Settings/Surveys/SubmissionLog.cshtml
- ⏳ Pages/Settings/Surveys/CreateSurveyTemplate_NEW.cshtml

### Settings - Mappings (3 pages)
- ⏳ Pages/Settings/Mappings/EditMapping.cshtml
- ⏳ Pages/Settings/Mappings/DeleteMapping.cshtml
- ⏳ Pages/Settings/Mappings/SuggestMappings.cshtml

### Settings - Users & Roles (9 pages)
- ⏳ Pages/Settings/Users/Index.cshtml
- ⏳ Pages/Settings/Users/Create.cshtml
- ⏳ Pages/Settings/Users/Edit.cshtml
- ⏳ Pages/Settings/Users/Details.cshtml
- ⏳ Pages/Settings/Users/Delete.cshtml
- ⏳ Pages/Settings/Users/Permissions.cshtml
- ⏳ Pages/Settings/Roles/Index.cshtml
- ⏳ Pages/Settings/Roles/Permissions.cshtml
- ⏳ Pages/Settings/Roles/GrantPermissionsDirect.cshtml

### Settings - Lookups (30 pages)
- ⏳ Pages/Settings/Lookups/EventTypes.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/LocationTypes.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/OrganizationTypes.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/ResultUnits.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/SpecimenTypes.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/Symptoms.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/TaskTemplates.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/TaskTypes.cshtml
- ⏳ Pages/Settings/Lookups/TestResults.cshtml + Create/Edit
- ⏳ Pages/Settings/Lookups/TestTypes.cshtml + Create/Edit

### Settings - Other Lookups (35 pages)
- ⏳ Pages/Settings/Countries/* (5 pages)
- ⏳ Pages/Settings/States/* (4 pages)
- ⏳ Pages/Settings/Jurisdictions/* (6 pages)
- ⏳ Pages/Settings/JurisdictionTypes/* (2 pages)
- ⏳ Pages/Settings/Languages/* (5 pages)
- ⏳ Pages/Settings/Occupations/* (6 pages)
- ⏳ Pages/Settings/Genders/* (4 pages)
- ⏳ Pages/Settings/SexAtBirths/* (4 pages)
- ⏳ Pages/Settings/Ethnicities/* (5 pages)
- ⏳ Pages/Settings/LookupTables/* (5 pages)

### Tools (3 pages)
- ⏳ Pages/Tools/GenerateFieldInventory.cshtml
- ⏳ Pages/Tools/TestDataGenerator.cshtml
- ⏳ Pages/Tools/TestFieldDiscovery.cshtml

---

## Migration Strategy

### Phase 1: Core User Flows (Priority 1)
1. Patients pages (8 pages)
2. Cases pages (11 pages)
3. Contacts pages (2 pages)
4. Outbreaks pages (4 pages)

**Total: 25 pages**

### Phase 2: Operational Features (Priority 2)
1. Reports (2 pages)
2. Tasks (2 pages)
3. Data Inbox (2 pages)

**Total: 6 pages**

### Phase 3: Settings & Administration (Priority 3)
1. Main settings (3 pages)
2. Diseases & Pathogens (13 pages)
3. Surveys & Mappings (10 pages)
4. Users & Roles (9 pages)
5. All lookups (65 pages)
6. Tools (3 pages)

**Total: ~90 pages**

---

## Common Patterns to Update

### 1. Page Headers
- Replace with `sentinel-card-header` and `sentinel-card-title`
- Use consistent subtitle styling

### 2. Forms
- Update labels to `sentinel-label`
- Update inputs to `sentinel-input`
- Update buttons to `btn-sentinel-primary` / `btn-sentinel-secondary`
- Update form groups to `sentinel-form-group`

### 3. Tables
- Add Sentinel table styling
- Update pagination controls
- Update action buttons

### 4. Cards
- Wrap content in `sentinel-card`
- Add proper card headers
- Update card styling

### 5. Status Chips
- Replace with `chip-outbreak`, `chip-watch`, `chip-info`, `chip-clear`
- Add dot indicators

### 6. Navigation
- Update breadcrumbs
- Update tab navigation
- Update action menus

---

## Testing Checklist
- [ ] Navigation between pages works
- [ ] Forms submit correctly
- [ ] Responsive design on mobile
- [ ] Accessibility maintained
- [ ] No console errors
- [ ] Authentication/authorization works
- [ ] Data displays correctly

---

**Last Updated:** April 18, 2026
**Design System Version:** 1.0
**Status:** In Progress
