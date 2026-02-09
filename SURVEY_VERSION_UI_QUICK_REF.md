# ?? Survey Version History UI - Quick Reference

## ?? Where to Find It

### Details Page
**Path:** Settings ? Surveys ? Survey Templates ? [Click Survey Name]  
**Location:** Left column, after Usage Statistics

### Edit Page
**Path:** Settings ? Surveys ? Survey Templates ? [Edit Button]  
**Location:** Below survey definition section

---

## ?? Version Status Badges

| Badge | Meaning | Count | Actions Available |
|-------|---------|-------|-------------------|
| ![Active](https://img.shields.io/badge/-Active-success) | Currently in use | Only 1 per survey | View only |
| ![Draft](https://img.shields.io/badge/-Draft-warning) | Not yet published | Multiple allowed | View, Publish, Archive |
| ![Archived](https://img.shields.io/badge/-Archived-secondary) | Old version | Multiple allowed | View only |

---

## ? Quick Actions

### Publish a Draft Version
```
1. Find version with [Draft] badge
2. Click [?? Publish] button
3. Confirm action
4. ? Version becomes Active
5. Previous Active ? Archived
```

### Archive a Draft Version
```
1. Find version with [Draft] badge
2. Click [?? Archive] button
3. Confirm action
4. Version status ? Archived
```

### View Specific Version
```
1. Click [??? View] button
2. Navigate to that version's page
3. See definition and settings
```

---

## ?? Version Lifecycle

```
Create ? Draft ? Publish ? Active
              ?           ?
            Archive ? Archive
```

**Rules:**
- ? Only **1 Active** version at a time
- ?? Multiple **Draft** versions allowed
- ?? Multiple **Archived** versions retained
- ?? **System templates** cannot be modified

---

## ?? Common Tasks

### Task: Create New Version
1. Edit survey in designer
2. Click **"Save As Version"**
3. Enter version number and notes
4. Choose: **Save as Draft** or **Publish immediately**

### Task: Replace Active Version
1. Create new version as Draft
2. Test thoroughly
3. Go to survey Details page
4. Find new version in history
5. Click **Publish**
6. Old active becomes Archived

### Task: Clean Up Old Drafts
1. Review version history
2. Identify unused drafts
3. Click **Archive** on each
4. Confirm removal

---

## ?? Version History Display

### On Details Page
**Format:** Card with scrollable list

```
[Version 2.0]  [Active]
Created: Feb 7, 2026 by admin
Published: Feb 7, 2026 by admin
[View]

[Version 1.5]  [Draft]
Testing new format
Created: Feb 6, 2026 by admin
[View] [Publish] [Archive]
```

### On Edit Page
**Format:** Data table

| Version | Status | Notes | Created | Published | Actions |
|---------|--------|-------|---------|-----------|---------|
| v2.0 [Current] | Active | Added questions | Feb 7 | Feb 7 | [View] |
| v1.5 | Draft | Testing | Feb 6 | - | [Publish] [View] |

---

## ?? Important Notes

### Cannot Do:
- ? Edit archived versions
- ? Re-publish archived versions
- ? Have multiple Active versions
- ? Archive Active version directly
- ? Modify system template versions

### Best Practices:
- ? Add clear version notes
- ? Test drafts before publishing
- ? Archive unused drafts
- ? Use semantic versioning (1.0, 2.0, 2.1)
- ? Review version history before changes

---

## ?? API Endpoints

```javascript
// Publish version
POST /api/SurveyVersion/PublishVersion/{id}

// Archive version
POST /api/SurveyVersion/ArchiveVersion/{id}

// Get all versions
GET /api/SurveyVersion/GetVersions/{surveyId}
```

---

## ?? Keyboard Shortcuts

*(Future enhancement)*

---

## ?? Need Help?

**Documentation:**
- Full Guide: `SURVEY_VERSION_UI_COMPLETE.md`
- Versioning System: `SURVEY_VERSIONING_COMPLETE_GUIDE.md`
- Troubleshooting: `SURVEY_VERSION_NOT_SAVED_FIX.md`

**Common Issues:**
- Version not saving? ? Check `SURVEY_VERSION_NOT_SAVED_FIX.md`
- Cannot publish? ? Ensure version is Draft status
- Actions disabled? ? System templates cannot be modified

---

## ? Quick Checklist

Before Publishing a Version:
- [ ] Survey definition is complete
- [ ] Field mappings configured
- [ ] Version notes added
- [ ] Tested in preview mode
- [ ] Reviewed by team

After Publishing:
- [ ] Verify Active badge appears
- [ ] Previous version archived
- [ ] New tasks use new version
- [ ] Old tasks still use old version

---

**Last Updated:** February 7, 2026
