# ConfigureLab UI/UX Improvements Plan

## Current Issues Identified

### 1. **No Manual Field Selection When Auto-Detection Fails**
**Problem**: When the system can't detect a field, it only offers:
- Upload different sample
- Set fixed default
- Skip field

**Missing**: Ability to **browse all HL7 fields** and manually select the correct one.

---

### 2. **Limited Reset/Change Options**
**Problem**: Once a field is confirmed, the only option is "Change" which shows the same candidate list.

**Missing**: 
- Clear/reset individual field mappings
- Reset entire configuration
- Visual history of what changed
- Bulk reset options

---

### 3. **Unclear Configuration Scope**
**Problem**: The code shows `ConfigurationId` is used, but the UI doesn't clearly indicate:
- Which HL7 Configuration this mapping applies to
- That mappings are configuration-specific (not global)
- How to switch between different lab configurations

**Current Behavior** (from code):
```csharp
// Line 234: Loads mappings for specific configuration
var mappings = await _context.HL7FieldMappings
    .Where(m => m.ConfigurationId == Configuration.Id && m.IsActive)
    .ToListAsync();
```
✅ **Mappings ARE configuration-specific** (good!)
❌ **UI doesn't make this clear** (bad!)

---

## Proposed Improvements

### **Improvement 1: Add "Browse All Fields" Modal**

#### UI Design
When user clicks **"Help me find this"** or **"Browse all fields"**:

```
┌─────────────────────────────────────────────────────────────┐
│ Find Field: Patient Last Name                              │
├─────────────────────────────────────────────────────────────┤
│ Sample Message Preview:                                     │
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ MSH|^~\&|LAB||SENTINEL||20240315||ORU^R01|MSG001|P|2.5 │ │
│ │ PID|1||MRN123||SMITH^JANE||19850422|F                   │ │
│ │ OBR|1|||87798-0^CT NAAT||||||||||||||||||Quest Lab|     │ │
│ │ OBX|1|ST|87798-0^CT NAAT||POSITIVE||NEGATIVE|A||F       │ │
│ └─────────────────────────────────────────────────────────┘ │
│                                                               │
│ All Available Fields:                                        │
│ ┌───────────────────────────────────────────────────────┐   │
│ │ ▸ MSH (Message Header)                                │   │
│ │   ○ MSH-3: LAB                                        │   │
│ │   ○ MSH-4: (empty)                                    │   │
│ │   ○ MSH-10: MSG001                                    │   │
│ │                                                        │   │
│ │ ▾ PID (Patient Identification)                        │   │
│ │   ○ PID-3.1: MRN123                                   │   │
│ │   ● PID-5.1: SMITH          ← Selected                │   │
│ │   ○ PID-5.2: JANE                                     │   │
│ │   ○ PID-7: 19850422                                   │   │
│ │   ○ PID-8: F                                          │   │
│ │                                                        │   │
│ │ ▸ OBR (Observation Request)                           │   │
│ │ ▸ OBX (Observation Result)                            │   │
│ └───────────────────────────────────────────────────────┘   │
│                                                               │
│ Selected: PID-5.1 = "SMITH"                                  │
│                                                               │
│ [Confirm Selection]  [Cancel]                                │
└─────────────────────────────────────────────────────────────┘
```

#### Implementation

**Backend Method** (`ConfigureLab.cshtml.cs`):
```csharp
public async Task<IActionResult> OnPostGetAllFieldsAsync(Guid configId)
{
    var configuration = await _context.HL7Configurations.FindAsync(configId);
    var mapping = await _context.HL7FieldMappings
        .FirstOrDefaultAsync(m => m.ConfigurationId == configId);

    if (string.IsNullOrEmpty(mapping?.SampleMessage))
    {
        return new JsonResult(new { error = "No sample message available" });
    }

    var parseResult = await _parserService.ParseMessagePreviewAsync(mapping.SampleMessage);

    // Extract ALL fields from ALL segments
    var allFields = new List<HL7FieldOption>();

    foreach (var segment in parseResult.RawSegments)
    {
        var segmentType = segment.Split('|')[0];
        var fields = segment.Split('|');

        for (int i = 1; i < fields.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(fields[i]))
            {
                allFields.Add(new HL7FieldOption
                {
                    SegmentType = segmentType,
                    FieldPath = $"{segmentType}-{i}",
                    FieldValue = fields[i],
                    FieldDescription = GetFieldDescription(segmentType, i)
                });
            }
        }
    }

    return new JsonResult(new { fields = allFields, rawMessage = mapping.SampleMessage });
}

private string GetFieldDescription(string segmentType, int fieldIndex)
{
    // Map to HL7 standard field descriptions
    return (segmentType, fieldIndex) switch
    {
        ("PID", 3) => "Patient Identifier",
        ("PID", 5) => "Patient Name",
        ("PID", 7) => "Date of Birth",
        ("PID", 8) => "Gender",
        ("OBR", 3) => "Filler Order Number (Accession)",
        ("OBR", 4) => "Universal Service ID (Test Name)",
        ("OBR", 7) => "Observation Date/Time",
        ("OBX", 3) => "Observation Identifier (Test Code)",
        ("OBX", 5) => "Observation Value (Result)",
        _ => $"Field {fieldIndex}"
    };
}
```

**Frontend Modal** (`ConfigureLab.cshtml`):
```html
<!-- Browse All Fields Modal -->
<div class="modal fade" id="browseFieldsModal" tabindex="-1">
    <div class="modal-dialog modal-xl">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title">Find Field: <span id="browseFieldName"></span></h5>
                <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
            </div>
            <div class="modal-body">
                <div class="row">
                    <div class="col-md-6">
                        <h6>Sample Message</h6>
                        <pre id="browseRawMessage" style="font-size:11px;background:var(--chalk);padding:12px;border-radius:4px;max-height:400px;overflow-y:auto"></pre>
                    </div>
                    <div class="col-md-6">
                        <h6>All Fields in Message</h6>
                        <div id="browseFieldsList" style="max-height:400px;overflow-y:auto"></div>
                    </div>
                </div>
                <div id="selectedFieldInfo" class="alert alert-info mt-3" style="display:none">
                    Selected: <strong id="selectedFieldPath"></strong> = "<span id="selectedFieldValue"></span>"
                </div>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                <button type="button" class="btn btn-primary" id="confirmFieldSelection" disabled>Confirm Selection</button>
            </div>
        </div>
    </div>
</div>

<script>
let currentBrowseFieldKey = null;

async function browseAllFields(fieldKey, friendlyName) {
    currentBrowseFieldKey = fieldKey;
    document.getElementById('browseFieldName').textContent = friendlyName;

    const response = await fetch('/Settings/HL7/FieldMappings/ConfigureLab?handler=GetAllFields&configId=@Model.Configuration.Id', {
        method: 'POST',
        headers: { 'RequestVerificationToken': document.querySelector('[name="__RequestVerificationToken"]').value }
    });

    const data = await response.json();

    // Show raw message with highlighting
    document.getElementById('browseRawMessage').textContent = data.rawMessage;

    // Build field tree
    const fieldsList = document.getElementById('browseFieldsList');
    const groupedFields = groupFieldsBySegment(data.fields);

    fieldsList.innerHTML = '';
    for (const [segmentType, fields] of Object.entries(groupedFields)) {
        const segmentDiv = document.createElement('div');
        segmentDiv.className = 'mb-3';
        segmentDiv.innerHTML = `
            <div class="d-flex align-items-center mb-2" style="cursor:pointer" onclick="toggleSegment(this)">
                <i class="bi bi-chevron-right me-2"></i>
                <strong>${segmentType}</strong> <span class="text-muted ms-2">(${fields.length} fields)</span>
            </div>
            <div class="fields-container" style="display:none;padding-left:20px">
                ${fields.map(f => `
                    <div class="form-check mb-2">
                        <input class="form-check-input" type="radio" name="fieldSelection" 
                               id="field_${f.fieldPath.replace(/[^a-zA-Z0-9]/g, '_')}"
                               value="${f.fieldPath}" 
                               data-value="${f.fieldValue}"
                               onchange="selectField('${f.fieldPath}', '${f.fieldValue}')">
                        <label class="form-check-label" for="field_${f.fieldPath.replace(/[^a-zA-Z0-9]/g, '_')}">
                            <code>${f.fieldPath}</code>: ${f.fieldValue}
                            <small class="text-muted d-block">${f.fieldDescription}</small>
                        </label>
                    </div>
                `).join('')}
            </div>
        `;
        fieldsList.appendChild(segmentDiv);
    }

    // Open the first segment by default
    fieldsList.querySelector('.fields-container').style.display = 'block';

    const modal = new bootstrap.Modal(document.getElementById('browseFieldsModal'));
    modal.show();
}

function toggleSegment(element) {
    const icon = element.querySelector('i');
    const container = element.nextElementSibling;

    if (container.style.display === 'none') {
        container.style.display = 'block';
        icon.className = 'bi bi-chevron-down me-2';
    } else {
        container.style.display = 'none';
        icon.className = 'bi bi-chevron-right me-2';
    }
}

function selectField(fieldPath, fieldValue) {
    document.getElementById('selectedFieldPath').textContent = fieldPath;
    document.getElementById('selectedFieldValue').textContent = fieldValue;
    document.getElementById('selectedFieldInfo').style.display = 'block';
    document.getElementById('confirmFieldSelection').disabled = false;
}

document.getElementById('confirmFieldSelection').addEventListener('click', async function() {
    const selectedRadio = document.querySelector('input[name="fieldSelection"]:checked');
    if (!selectedRadio) return;

    const fieldPath = selectedRadio.value;
    const fieldValue = selectedRadio.dataset.value;

    await confirmFieldMapping(currentBrowseFieldKey, fieldValue, fieldPath);

    bootstrap.Modal.getInstance(document.getElementById('browseFieldsModal')).hide();
});

function groupFieldsBySegment(fields) {
    return fields.reduce((acc, field) => {
        if (!acc[field.segmentType]) acc[field.segmentType] = [];
        acc[field.segmentType].push(field);
        return acc;
    }, {});
}
</script>
```

---

### **Improvement 2: Enhanced Reset/Clear Options**

#### Add to Page Header
```html
<div class="d-flex justify-content-between align-items-center mb-4">
    <div>
        <h1>Configure Lab: @Model.Configuration?.ConfigurationName</h1>
        <p class="text-muted">
            Mapping fields for HL7 configuration: <strong>@Model.Configuration?.SendingFacility</strong>
        </p>
    </div>
    <div class="btn-group">
        <button type="button" class="btn btn-outline-secondary btn-sm dropdown-toggle" data-bs-toggle="dropdown">
            <i class="bi bi-gear"></i> Options
        </button>
        <ul class="dropdown-menu dropdown-menu-end">
            <li><button class="dropdown-item" onclick="resetAllMappings()">
                <i class="bi bi-arrow-counterclockwise"></i> Reset all mappings
            </button></li>
            <li><button class="dropdown-item" onclick="exportMappings()">
                <i class="bi bi-download"></i> Export mappings (JSON)
            </button></li>
            <li><button class="dropdown-item" onclick="importMappings()">
                <i class="bi bi-upload"></i> Import mappings
            </button></li>
            <li><hr class="dropdown-divider"></li>
            <li><button class="dropdown-item text-danger" onclick="deleteConfiguration()">
                <i class="bi bi-trash"></i> Delete this configuration
            </button></li>
        </ul>
    </div>
</div>
```

#### Add Individual Field Reset
Update field card "Change" button:
```html
<div class="btn-group btn-group-sm">
    <button type="button" class="btn btn-outline-secondary" onclick="unresolveField('@field.FieldKey')">
        <i class="bi bi-pencil"></i> Change
    </button>
    <button type="button" class="btn btn-outline-danger" onclick="clearFieldMapping('@field.FieldKey')">
        <i class="bi bi-x"></i> Clear
    </button>
</div>
```

#### Backend Methods
```csharp
public async Task<IActionResult> OnPostResetAllMappingsAsync(Guid configId)
{
    var mappings = await _context.HL7FieldMappings
        .Where(m => m.ConfigurationId == configId)
        .ToListAsync();

    _context.HL7FieldMappings.RemoveRange(mappings);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "All field mappings have been reset";
    return RedirectToPage(new { configId });
}

public async Task<IActionResult> OnPostClearFieldMappingAsync(Guid configId, string fieldKey)
{
    var (entity, property) = ParseFieldKey(fieldKey);

    var mapping = await _context.HL7FieldMappings
        .FirstOrDefaultAsync(m => 
            m.ConfigurationId == configId &&
            m.TargetEntity == entity &&
            m.TargetProperty == property);

    if (mapping != null)
    {
        _context.HL7FieldMappings.Remove(mapping);
        await _context.SaveChangesAsync();
    }

    return RedirectToPage(new { configId });
}
```

---

### **Improvement 3: Clear Configuration Scope Indicators**

#### Update Page Header with Configuration Context
```html
<div class="alert alert-info mb-4">
    <div class="d-flex align-items-start gap-3">
        <i class="bi bi-info-circle" style="font-size:24px"></i>
        <div>
            <strong>Configuration Scope</strong>
            <p class="mb-2">These field mappings apply ONLY to messages from:</p>
            <ul class="mb-0">
                <li><strong>Sending Facility (MSH-4):</strong> @Model.Configuration?.SendingFacility</li>
                <li><strong>Sending Application (MSH-3):</strong> @Model.Configuration?.SendingApplication</li>
            </ul>
            <small class="text-muted">
                Other lab configurations have their own separate field mappings.
                <a asp-page="/Settings/HL7/FieldMappings/SelectLab">View all configurations →</a>
            </small>
        </div>
    </div>
</div>
```

#### Add Configuration Switcher
```html
<div class="mb-4">
    <label class="form-label">Viewing configuration:</label>
    <select class="form-select" style="max-width:400px" onchange="window.location.href='/Settings/HL7/FieldMappings/ConfigureLab?configId=' + this.value">
        @foreach (var config in Model.AllConfigurations)
        {
            <option value="@config.Id" selected="@(config.Id == Model.Configuration?.Id)">
                @config.ConfigurationName (@config.SendingFacility)
            </option>
        }
    </select>
</div>
```

#### Backend Property
```csharp
public List<HL7Configuration> AllConfigurations { get; set; } = new();

public async Task<IActionResult> OnGetAsync(Guid? configId, string? sampleMessage)
{
    // ... existing code ...

    // Load all configurations for switcher
    AllConfigurations = await _context.HL7Configurations
        .OrderBy(c => c.ConfigurationName)
        .ToListAsync();

    // ... rest of existing code ...
}
```

---

### **Improvement 4: Visual Mapping Status Indicators**

#### Add Progress Bar
```html
<div class="card mb-4">
    <div class="card-body">
        <div class="d-flex justify-content-between align-items-center mb-2">
            <h6 class="mb-0">Mapping Progress</h6>
            <span class="badge bg-primary">@Model.ConfirmedFieldCount / @Model.RequiredFieldCount Required Fields</span>
        </div>
        <div class="progress" style="height:8px">
            <div class="progress-bar bg-success" 
                 style="width:@((double)Model.ConfirmedFieldCount / Model.RequiredFieldCount * 100)%">
            </div>
        </div>
        <small class="text-muted">
            @Model.ConfirmedFieldCount confirmed, 
            @Model.NeedsAttentionCount need attention, 
            @Model.NotFoundCount not found
        </small>
    </div>
</div>
```

#### Backend Properties
```csharp
public int RequiredFieldCount => Fields.Count(f => f.IsRequired);
public int ConfirmedFieldCount => Fields.Count(f => f.Status == "Confirmed");
public int NeedsAttentionCount => Fields.Count(f => f.Status == "NeedsAttention");
public int NotFoundCount => Fields.Count(f => f.Status == "NotFound");
```

---

## Implementation Priority

1. **HIGH**: Add "Browse All Fields" modal (fixes biggest pain point)
2. **HIGH**: Add configuration scope indicators (clarifies confusion)
3. **MEDIUM**: Add reset/clear options (improves workflow)
4. **MEDIUM**: Add progress indicators (improves UX)
5. **LOW**: Add import/export (nice to have)

---

## Summary of Changes

### Files to Modify
1. `Pages/Settings/HL7/FieldMappings/ConfigureLab.cshtml.cs`
   - Add `OnPostGetAllFieldsAsync()`
   - Add `OnPostResetAllMappingsAsync()`
   - Add `OnPostClearFieldMappingAsync()`
   - Add `AllConfigurations` property
   - Add progress counter properties

2. `Pages/Settings/HL7/FieldMappings/ConfigureLab.cshtml`
   - Add browse fields modal
   - Add configuration scope alert
   - Add configuration switcher dropdown
   - Add progress bar
   - Add reset/options dropdown
   - Update "Not found" section with "Browse all fields" button
   - Add JavaScript for field browser

### Database Impact
✅ **No schema changes required** - all improvements use existing `HL7FieldMapping` structure

### Benefits
- ✅ Users can manually select fields when auto-detection fails
- ✅ Clear visual feedback on mapping status
- ✅ Easy reset/clear options
- ✅ Configuration scope is explicit and clear
- ✅ Better error recovery workflow
