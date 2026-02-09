# ?? Survey Version Management - Complete Implementation Guide

## ?? Overview

Survey versioning allows users to:
- Create draft versions of surveys
- Mark one version as "Active" (used in tasks)
- Maintain version history for audit/reference
- Collaborate on future versions without affecting active surveys
- Save version notes describing changes

---

## ??? Database Schema

### New Fields in `SurveyTemplates` Table

```sql
-- Version Management
ParentSurveyTemplateId UNIQUEIDENTIFIER NULL  -- Links to original survey
VersionNumber NVARCHAR(20) NOT NULL DEFAULT '1.0'  -- User-friendly version
VersionStatus INT NOT NULL DEFAULT 0  -- 0=Draft, 1=Active, 2=Archived
VersionNotes NVARCHAR(2000) NULL  -- Change description
PublishedAt DATETIME2 NULL  -- When made active
PublishedBy NVARCHAR(256) NULL  -- Who published it

-- Self-referencing foreign key
CONSTRAINT FK_SurveyTemplates_ParentSurveyTemplate 
    FOREIGN KEY (ParentSurveyTemplateId) 
    REFERENCES SurveyTemplates(Id)
```

### Version Status Enum

```csharp
public enum SurveyVersionStatus
{
    Draft = 0,      // Work in progress, not used in tasks
    Active = 1,     // Currently in use (only one active per family)
    Archived = 2    // Previously active, now superseded
}
```

---

## ?? Version Lifecycle

```
???????????
?  Draft  ?  ? Created from "Save As" or new survey
???????????
     ?
     ? "Publish" action
     ?
???????????
? Active  ?  ? Only ONE active version per survey family
???????????    Tasks reference this version
     ?
     ? New version published
     ?
????????????
? Archived ?  ? Historical reference only
????????????
```

---

## ??? Data Structure

### Survey Family Tree Example

```
Food History Survey (Original)
?? v1.0 [Archived] - Created: 2026-01-01
?? v2.0 [Active] - Published: 2026-02-01
?   ?? Notes: "Added food source questions"
?? v2.1 [Draft] - Created: 2026-02-15
?   ?? Notes: "Testing new symptom checklist"
?? v3.0 [Draft] - Created: 2026-02-20
    ?? Notes: "Complete redesign for mobile"
```

### Database Representation

```
Id: abc-123
ParentSurveyTemplateId: NULL
Name: "Food History Survey"
VersionNumber: "1.0"
VersionStatus: Archived
PublishedAt: 2026-01-01

Id: def-456
ParentSurveyTemplateId: abc-123
Name: "Food History Survey"
VersionNumber: "2.0"
VersionStatus: Active
VersionNotes: "Added food source questions"
PublishedAt: 2026-02-01

Id: ghi-789
ParentSurveyTemplateId: abc-123
Name: "Food History Survey"
VersionNumber: "2.1"
VersionStatus: Draft
VersionNotes: "Testing new symptom checklist"

Id: jkl-012
ParentSurveyTemplateId: abc-123
Name: "Food History Survey"
VersionNumber: "3.0"
VersionStatus: Draft
VersionNotes: "Complete redesign for mobile"
```

---

## ?? Implementation Components

### 1. Survey Details Page - Version History

**UI Location:** `EditSurveyTemplate.cshtml` or new `SurveyVersions.cshtml`

```razor
<div class="card">
    <div class="card-header">
        <i class="bi bi-clock-history me-2"></i>Version History
    </div>
    <div class="card-body">
        <table class="table">
            <thead>
                <tr>
                    <th>Version</th>
                    <th>Status</th>
                    <th>Notes</th>
                    <th>Created</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var version in Model.AllVersions.OrderByDescending(v => v.CreatedAt))
                {
                    <tr class="@(version.VersionStatus == SurveyVersionStatus.Active ? "table-success" : "")">
                        <td>
                            <strong>v@version.VersionNumber</strong>
                            @if (version.VersionStatus == SurveyVersionStatus.Active)
                            {
                                <span class="badge bg-success">ACTIVE</span>
                            }
                        </td>
                        <td>
                            <span class="badge bg-@GetStatusBadgeColor(version.VersionStatus)">
                                @version.VersionStatus
                            </span>
                        </td>
                        <td>@version.VersionNotes</td>
                        <td>
                            @version.CreatedAt.ToString("MMM d, yyyy")
                            <br>
                            <small class="text-muted">by @version.CreatedBy</small>
                        </td>
                        <td>
                            @if (version.VersionStatus == SurveyVersionStatus.Draft)
                            {
                                <button class="btn btn-sm btn-success" onclick="publishVersion('@version.Id')">
                                    <i class="bi bi-upload me-1"></i>Publish
                                </button>
                            }
                            <button class="btn btn-sm btn-primary" onclick="editVersion('@version.Id')">
                                <i class="bi bi-pencil me-1"></i>Edit
                            </button>
                            <button class="btn btn-sm btn-info" onclick="compareVersions('@version.Id')">
                                <i class="bi bi-columns-gap me-1"></i>Compare
                            </button>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

### 2. Survey Designer - "Save As New Version"

**Location:** `DesignSurvey.cshtml` header buttons

```html
<div class="btn-group">
    <button type="button" class="btn btn-outline-secondary" id="cancelBtn">
        <i class="bi bi-x-circle me-1"></i>Cancel
    </button>
    <button type="button" class="btn btn-outline-primary" id="saveAsVersionBtn">
        <i class="bi bi-plus-circle me-1"></i>Save As New Version
    </button>
    <button type="button" class="btn btn-outline-primary" id="previewBtn">
        <i class="bi bi-eye me-1"></i>Preview
    </button>
    <button type="button" class="btn btn-primary" id="saveBtn">
        <i class="bi bi-save me-1"></i>Save & Close
    </button>
</div>
```

**JavaScript:**

```javascript
$('#saveAsVersionBtn').on('click', function() {
    // Show modal to get version details
    showSaveAsVersionModal();
});

function showSaveAsVersionModal() {
    const modalHtml = `
        <div class="modal fade" id="saveAsVersionModal">
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Save As New Version</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="mb-3">
                            <label class="form-label">Version Number</label>
                            <input type="text" class="form-control" id="versionNumber" 
                                   placeholder="e.g., 2.1, 3.0-beta" />
                            <small class="text-muted">Recommended: Increment from current</small>
                        </div>
                        <div class="mb-3">
                            <label class="form-label">What Changed?</label>
                            <textarea class="form-control" id="versionNotes" rows="4"
                                      placeholder="Describe changes made in this version..."></textarea>
                        </div>
                        <div class="form-check">
                            <input type="checkbox" class="form-check-input" id="publishNow" />
                            <label class="form-check-label">
                                Publish immediately as Active version
                            </label>
                            <small class="form-text text-muted d-block">
                                If unchecked, version will be saved as Draft
                            </small>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                        <button type="button" class="btn btn-primary" onclick="saveNewVersion()">
                            <i class="bi bi-save me-1"></i>Create Version
                        </button>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    $('body').append(modalHtml);
    new bootstrap.Modal(document.getElementById('saveAsVersionModal')).show();
}

function saveNewVersion() {
    const versionNumber = $('#versionNumber').val();
    const versionNotes = $('#versionNotes').val();
    const publishNow = $('#publishNow').is(':checked');
    
    if (!versionNumber) {
        alert('Version number is required');
        return;
    }
    
    const surveyJson = surveyCreator.JSON;
    
    $.ajax({
        url: '/api/SurveyTemplates/SaveAsNewVersion',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            parentSurveyId: surveyId,
            versionNumber: versionNumber,
            versionNotes: versionNotes,
            publishImmediately: publishNow,
            surveyDefinitionJson: JSON.stringify(surveyJson),
            outputMappingJson: JSON.stringify(currentMappings.output),
            inputMappingJson: JSON.stringify(currentMappings.input)
        }),
        success: function(response) {
            alert('New version created successfully!');
            window.location.href = `/Settings/Surveys/EditSurveyTemplate?id=${response.newVersionId}`;
        },
        error: function(xhr) {
            alert('Failed to create version: ' + xhr.responseText);
        }
    });
}
```

### 3. Backend API - Version Management

**New Controller:** `SurveyVersionController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
public class SurveyVersionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SurveyVersionController> _logger;
    
    public SurveyVersionController(ApplicationDbContext context, ILogger<SurveyVersionController> logger)
    {
        _context = context;
        _logger = logger;
    }
    
    [HttpPost("SaveAsNewVersion")]
    public async Task<IActionResult> SaveAsNewVersion([FromBody] SaveAsVersionRequest request)
    {
        // Get parent survey
        var parentSurvey = await _context.SurveyTemplates
            .FirstOrDefaultAsync(st => st.Id == request.ParentSurveyId);
            
        if (parentSurvey == null)
            return NotFound("Parent survey not found");
        
        // Find root parent (handle nested versions)
        var rootParentId = parentSurvey.ParentSurveyTemplateId ?? parentSurvey.Id;
        
        // Check if version number already exists
        var existingVersion = await _context.SurveyTemplates
            .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
            .AnyAsync(st => st.VersionNumber == request.VersionNumber);
            
        if (existingVersion)
            return BadRequest($"Version {request.VersionNumber} already exists");
        
        // Create new version
        var newVersion = new SurveyTemplate
        {
            Id = Guid.NewGuid(),
            ParentSurveyTemplateId = rootParentId,
            Name = parentSurvey.Name, // Inherit name
            Description = parentSurvey.Description,
            Category = parentSurvey.Category,
            Tags = parentSurvey.Tags,
            VersionNumber = request.VersionNumber,
            VersionStatus = request.PublishImmediately ? 
                SurveyVersionStatus.Active : 
                SurveyVersionStatus.Draft,
            VersionNotes = request.VersionNotes,
            SurveyDefinitionJson = request.SurveyDefinitionJson,
            DefaultInputMappingJson = request.InputMappingJson,
            DefaultOutputMappingJson = request.OutputMappingJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = User.Identity?.Name,
            IsActive = true
        };
        
        // If publishing immediately, archive current active version
        if (request.PublishImmediately)
        {
            var currentActive = await _context.SurveyTemplates
                .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                .FirstOrDefaultAsync(st => st.VersionStatus == SurveyVersionStatus.Active);
                
            if (currentActive != null)
            {
                currentActive.VersionStatus = SurveyVersionStatus.Archived;
            }
            
            newVersion.PublishedAt = DateTime.UtcNow;
            newVersion.PublishedBy = User.Identity?.Name;
        }
        
        _context.SurveyTemplates.Add(newVersion);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Created survey version {VersionNumber} for survey {ParentId}, Status: {Status}",
            request.VersionNumber, rootParentId, newVersion.VersionStatus);
        
        return Ok(new { newVersionId = newVersion.Id });
    }
    
    [HttpPost("PublishVersion/{id}")]
    public async Task<IActionResult> PublishVersion(Guid id)
    {
        var version = await _context.SurveyTemplates
            .Include(st => st.ParentSurveyTemplate)
            .FirstOrDefaultAsync(st => st.Id == id);
            
        if (version == null)
            return NotFound();
            
        if (version.VersionStatus == SurveyVersionStatus.Active)
            return BadRequest("Version is already active");
        
        // Find root parent
        var rootParentId = version.ParentSurveyTemplateId ?? version.Id;
        
        // Archive current active version
        var currentActive = await _context.SurveyTemplates
            .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
            .FirstOrDefaultAsync(st => st.VersionStatus == SurveyVersionStatus.Active);
            
        if (currentActive != null)
        {
            currentActive.VersionStatus = SurveyVersionStatus.Archived;
        }
        
        // Activate this version
        version.VersionStatus = SurveyVersionStatus.Active;
        version.PublishedAt = DateTime.UtcNow;
        version.PublishedBy = User.Identity?.Name;
        
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Published survey version {VersionNumber} (Id: {Id})",
            version.VersionNumber, id);
        
        return Ok();
    }
    
    [HttpGet("GetVersions/{surveyId}")]
    public async Task<IActionResult> GetVersions(Guid surveyId)
    {
        // Get all versions for this survey family
        var survey = await _context.SurveyTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(st => st.Id == surveyId);
            
        if (survey == null)
            return NotFound();
        
        var rootParentId = survey.ParentSurveyTemplateId ?? survey.Id;
        
        var versions = await _context.SurveyTemplates
            .AsNoTracking()
            .Where(st => st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId)
            .OrderByDescending(st => st.CreatedAt)
            .Select(st => new
            {
                st.Id,
                st.VersionNumber,
                st.VersionStatus,
                st.VersionNotes,
                st.CreatedAt,
                st.CreatedBy,
                st.PublishedAt,
                st.PublishedBy
            })
            .ToListAsync();
        
        return Ok(versions);
    }
}

public class SaveAsVersionRequest
{
    public Guid ParentSurveyId { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public string? VersionNotes { get; set; }
    public bool PublishImmediately { get; set; }
    public string SurveyDefinitionJson { get; set; } = string.Empty;
    public string? InputMappingJson { get; set; }
    public string? OutputMappingJson { get; set; }
}
```

---

## ?? User Workflows

### Workflow 1: Create Draft Version

```
1. User opens Active survey in designer
2. Makes changes to questions/logic
3. Clicks "Save As New Version"
4. Enters version number: "2.1"
5. Enters notes: "Added symptom severity question"
6. Leaves "Publish immediately" unchecked
7. Clicks "Create Version"
   ? New draft version created
   ? Active version unchanged
   ? Tasks still use active version
```

### Workflow 2: Collaborate on Draft

```
1. User A creates draft v3.0 with notes
2. User B opens v3.0 (still draft)
3. User B makes additional changes
4. Saves changes (updates draft)
5. User A reviews v3.0
6. User A clicks "Publish"
   ? v3.0 becomes Active
   ? Previous active becomes Archived
   ? Tasks now use v3.0
```

### Workflow 3: Version Comparison

```
1. User viewing survey details
2. Sees version history table
3. Selects v2.0 (Archived) and v3.0 (Active)
4. Clicks "Compare"
5. Side-by-side diff view:
   - Removed questions highlighted in red
   - Added questions highlighted in green
   - Modified logic shown with changes
```

---

## ?? Data Migration

### Update Existing Surveys

```sql
-- Set version numbers for existing surveys
UPDATE SurveyTemplates
SET 
    VersionNumber = CAST(Version AS NVARCHAR(20)) + '.0',
    VersionStatus = CASE WHEN IsActive = 1 THEN 1 ELSE 2 END,
    PublishedAt = CreatedAt,
    PublishedBy = CreatedBy
WHERE ParentSurveyTemplateId IS NULL;
```

---

## ? Validation Rules

### Business Rules

1. **One Active Version Per Family**
   - Only one version can have Status = Active per survey family
   - Publishing a version automatically archives the current active

2. **Version Number Uniqueness**
   - Within a survey family, version numbers must be unique
   - Recommend semantic versioning: major.minor[-tag]

3. **Edit Restrictions**
   - Draft versions: Fully editable
   - Active versions: Read-only (create new version to modify)
   - Archived versions: Read-only (historical reference)

4. **Deletion Rules**
   - Cannot delete Active version
   - Cannot delete if parent has children (delete children first)
   - Deleting parent archives all child versions

5. **Task References**
   - Tasks always reference the Active version at time of creation
   - Changing active version doesn't affect existing tasks
   - Historical data integrity maintained

---

## ?? UI Components Summary

### Survey Details Page
- Version history table
- Status badges (Draft/Active/Archived)
- Publish button for drafts
- Compare versions button

### Survey Designer
- "Save As New Version" button
- Version info display in header
- Status indicator

### Survey List Page
- Show active version number
- Badge if drafts exist
- Quick actions menu

---

## ?? Testing Checklist

- [ ] Create new survey (v1.0 Draft)
- [ ] Publish survey (v1.0 Active)
- [ ] Create draft v2.0 from active
- [ ] Edit draft v2.0
- [ ] Publish v2.0 (v1.0 becomes Archived)
- [ ] Verify tasks still use correct version
- [ ] Create multiple drafts (v2.1, v3.0)
- [ ] Compare versions side-by-side
- [ ] Test validation rules
- [ ] Test permissions

---

## ?? Future Enhancements

### Phase 2 Features

1. **Version Diff Viewer**
   - Visual comparison of survey JSON
   - Highlight added/removed/modified questions

2. **Version Branching**
   - Create multiple draft branches from same base
   - Merge changes between branches

3. **Auto-versioning**
   - Automatically create draft when editing active
   - Suggest version number based on changes

4. **Version Approval Workflow**
   - Require approval before publishing
   - Review/comment system
   - Approval history

5. **Version Analytics**
   - Response rates per version
   - Completion time comparison
   - Question performance metrics

---

## ?? Summary

**Status:** ? Schema implemented, ready for UI/API implementation

**Files Modified:**
- `Models/SurveyTemplate.cs` - Added version fields
- Migration created: `AddSurveyVersioning`

**Next Steps:**
1. Apply migration to database
2. Create `SurveyVersionController.cs`
3. Update `DesignSurvey.cshtml` with "Save As" button
4. Create version history UI component
5. Add comparison view (optional)

**Key Benefits:**
- ? Safe survey updates without affecting active tasks
- ? Collaborative draft editing
- ? Complete audit trail
- ? Historical reference preserved
- ? Flexible versioning strategy

---

**Last Updated:** February 7, 2026  
**Status:** Schema ready, UI implementation needed
