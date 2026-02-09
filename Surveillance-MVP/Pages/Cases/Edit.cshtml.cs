using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Cases
{
    [Authorize(Policy = "Permission.Case.Edit")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly CustomFieldService _customFieldService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly IExposureRequirementService _exposureRequirementService;
        private readonly ITaskService _taskService;

        public EditModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            CustomFieldService customFieldService, 
            IDiseaseAccessService diseaseAccessService,
            IExposureRequirementService exposureRequirementService,
            ITaskService taskService)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _diseaseAccessService = diseaseAccessService;
            _exposureRequirementService = exposureRequirementService;
            _taskService = taskService;
        }

        [BindProperty]
        public Case Case { get; set; } = default!;

        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<int, object> CustomFieldValues { get; set; } = new();
        
        public List<Symptom> CommonSymptoms { get; set; } = new();
        public List<Symptom> AllAvailableSymptoms { get; set; } = new();
        public List<CaseSymptom> CaseSymptoms { get; set; } = new();
        
        public Disease? DiseaseRequirements { get; set; }
        public bool ShouldPromptForExposure { get; set; }
        public int ExposureCount { get; set; }
        public bool HasIncompleteExposure { get; set; }

        private Case _originalCase = default!;

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (caseEntity == null)
            {
                return NotFound();
            }

            // Check disease access
            if (caseEntity.DiseaseId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, caseEntity.DiseaseId.Value);
                
                if (!canAccess)
                {
                    return Forbid();
                }
            }

            Case = caseEntity;

            ViewData["PatientId"] = new SelectList(
                await _context.Patients
                    .OrderBy(p => p.FamilyName)
                    .ThenBy(p => p.GivenName)
                    .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                    .ToListAsync(),
                "Id", "FullName");

            ViewData["ConfirmationStatusId"] = new SelectList(
                await _context.CaseStatuses
                    .Where(cs => cs.IsActive)
                    .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                    .ThenBy(cs => cs.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Filter diseases based on access
            var accessUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(accessUserId);

            ViewData["DiseaseId"] = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive && accessibleDiseaseIds.Contains(d.Id))
                    .OrderBy(d => d.Level)
                    .ThenBy(d => d.DisplayOrder)
                    .ThenBy(d => d.Name)
                    .Select(d => new { 
                        d.Id, 
                        DisplayName = new string('—', d.Level) + " " + d.Name 
                    })
                    .ToListAsync(),
                "Id", "DisplayName");

            // Load custom fields if disease is selected
            if (Case.DiseaseId.HasValue)
            {
                CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
                CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Case.Id);

                // Load common symptoms for this disease
                CommonSymptoms = await _context.DiseaseSymptoms
                    .Where(ds => ds.DiseaseId == Case.DiseaseId.Value && ds.IsCommon)
                    .Include(ds => ds.Symptom)
                    .OrderBy(ds => ds.SortOrder)
                    .Select(ds => ds.Symptom!)
                    .ToListAsync();
            }

            // Load all active symptoms for autocomplete
            AllAvailableSymptoms = await _context.Symptoms
                .Where(s => s.IsActive && s.Code != "OTHER")
                .OrderBy(s => s.Name)
                .ToListAsync();

            CaseSymptoms = await _context.CaseSymptoms
                .Include(cs => cs.Symptom)
                .Where(cs => cs.CaseId == Case.Id)
                .ToListAsync();

            // Load exposure requirements
            if (Case.DiseaseId.HasValue)
            {
                DiseaseRequirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                ShouldPromptForExposure = await _exposureRequirementService.ShouldPromptForExposureAsync(Case.DiseaseId.Value);
                
                ExposureCount = await _context.ExposureEvents.CountAsync(e => e.CaseId == Case.Id);
                HasIncompleteExposure = !await _exposureRequirementService.ValidateExposureCompletenessAsync(Case);
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["PatientId"] = new SelectList(
                    await _context.Patients
                        .OrderBy(p => p.FamilyName)
                        .ThenBy(p => p.GivenName)
                        .Select(p => new { p.Id, FullName = p.GivenName + " " + p.FamilyName + " (" + p.FriendlyId + ")" })
                        .ToListAsync(),
                    "Id", "FullName");

                ViewData["ConfirmationStatusId"] = new SelectList(
                    await _context.CaseStatuses
                        .Where(cs => cs.IsActive)
                        .OrderBy(cs => cs.DisplayOrder ?? int.MaxValue)
                        .ThenBy(cs => cs.Name)
                        .ToListAsync(),
                    "Id", "Name");

                // Filter diseases based on access
                var postUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var postAccessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(postUserId);

                ViewData["DiseaseId"] = new SelectList(
                    await _context.Diseases
                        .Where(d => d.IsActive && postAccessibleDiseaseIds.Contains(d.Id))
                        .OrderBy(d => d.Level)
                        .ThenBy(d => d.DisplayOrder)
                        .ThenBy(d => d.Name)
                        .Select(d => new { 
                            d.Id, 
                            DisplayName = new string('—', d.Level) + " " + d.Name 
                        })
                        .ToListAsync(),
                    "Id", "DisplayName");


                // Reload custom fields
                if (Case.DiseaseId.HasValue)
                {
                    CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
                    CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Case.Id);

                    // Reload common symptoms
                    CommonSymptoms = await _context.DiseaseSymptoms
                        .Where(ds => ds.DiseaseId == Case.DiseaseId.Value && ds.IsCommon)
                        .Include(ds => ds.Symptom)
                        .OrderBy(ds => ds.SortOrder)
                        .Select(ds => ds.Symptom!)
                        .ToListAsync();
                }

                // Reload all symptoms for autocomplete
                AllAvailableSymptoms = await _context.Symptoms
                    .Where(s => s.IsActive && s.Code != "OTHER")
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            _originalCase = await _context.Cases.AsNoTracking().FirstOrDefaultAsync(c => c.Id == Case.Id);

            _context.Attach(Case).State = EntityState.Modified;

            try
            {
                // Check if disease changed
                bool diseaseChanged = _originalCase.DiseaseId != Case.DiseaseId;

                await _context.SaveChangesAsync();

                // Auto-create tasks if disease changed and is now set
                if (diseaseChanged && Case.DiseaseId.HasValue)
                {
                    try
                    {
                        await _taskService.CreateTasksForCase(Case.Id, TaskTrigger.OnCaseCreation);
                        System.Diagnostics.Debug.WriteLine($"Tasks auto-created for updated case {Case.FriendlyId}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error creating tasks: {ex.Message}");
                        // Don't fail case update if task creation fails
                    }
                }

                // Save custom field values if disease is selected
                if (Case.DiseaseId.HasValue)
                {
                    var customFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
                    if (customFields.Any())
                    {
                        await _customFieldService.SaveCaseCustomFieldValuesAsync(Case.Id, Request.Form, customFields);
                        await _context.SaveChangesAsync();
                    }
                }

                // Save symptoms
                var earliestOnset = await SaveCaseSymptomsAsync();

                var changes = GetChangeSummary();
                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Updated",
                    oldValue: null,
                    newValue: changes,
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Case {Case.FriendlyId} updated successfully.";
                
                // Add special message if date of onset was auto-updated
                if (earliestOnset.HasValue && 
                    (_originalCase.DateOfOnset == null || earliestOnset.Value < _originalCase.DateOfOnset.Value))
                {
                    TempData["OnsetDateUpdated"] = true;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CaseExists(Case.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Details", new { id = Case.Id });
        }

        private async Task<DateTime?> SaveCaseSymptomsAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var symptomChanges = new List<string>();

            // Get existing symptoms for this case
            var existingSymptoms = await _context.CaseSymptoms
                .Include(cs => cs.Symptom)
                .Where(cs => cs.CaseId == Case.Id)
                .ToListAsync();

            // Get symptom IDs from common symptom checkboxes
            var commonSymptomIds = Request.Form.Keys
                .Where(k => k.StartsWith("common_symptom_"))
                .Select(k => int.Parse(k.Replace("common_symptom_", "")))
                .ToList();

            // Get additional symptom IDs from autocomplete (stored as comma-separated hidden field)
            var additionalSymptomIds = new List<int>();
            if (Request.Form.ContainsKey("additional_symptom_ids") && 
                !string.IsNullOrWhiteSpace(Request.Form["additional_symptom_ids"]))
            {
                additionalSymptomIds = Request.Form["additional_symptom_ids"]
                    .ToString()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.Parse(id.Trim()))
                    .ToList();
            }

            // Combine all selected symptom IDs
            var allSelectedSymptomIds = commonSymptomIds.Union(additionalSymptomIds).ToList();

            // Handle "Other" symptom
            var hasOtherText = Request.Form.ContainsKey("other_symptom_text") && 
                              !string.IsNullOrWhiteSpace(Request.Form["other_symptom_text"]);
            
            if (hasOtherText)
            {
                var otherSymptom = await _context.Symptoms.FirstOrDefaultAsync(s => s.Code == "OTHER");
                if (otherSymptom != null)
                {
                    allSelectedSymptomIds.Add(otherSymptom.Id);
                }
            }

            // Remove unselected symptoms
            foreach (var existing in existingSymptoms)
            {
                if (!allSelectedSymptomIds.Contains(existing.SymptomId))
                {
                    existing.IsDeleted = true;
                    existing.DeletedAt = DateTime.UtcNow;
                    existing.DeletedByUserId = userId;
                    
                    // Log removal
                    symptomChanges.Add($"Removed symptom: {existing.Symptom?.Name}");
                }
            }

            // Track earliest onset date
            DateTime? earliestOnset = null;

            // Add or update selected symptoms
            foreach (var symptomId in allSelectedSymptomIds)
            {
                var existing = existingSymptoms.FirstOrDefault(cs => cs.SymptomId == symptomId);
                var symptomName = existing?.Symptom?.Name ?? (await _context.Symptoms.FindAsync(symptomId))?.Name;
                bool isNewSymptom = false;
                
                if (existing != null && existing.IsDeleted)
                {
                    // Restore if was deleted
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedByUserId = null;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                    symptomChanges.Add($"Restored symptom: {symptomName}");
                }
                else if (existing == null)
                {
                    // Create new
                    var caseSymptom = new CaseSymptom
                    {
                        CaseId = Case.Id,
                        SymptomId = symptomId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.CaseSymptoms.Add(caseSymptom);
                    existing = caseSymptom;
                    isNewSymptom = true;
                }
                
                // Update onset date - try both common and additional prefixes
                var onsetDateKey = $"onset_date_{symptomId}";
                var oldOnsetDate = existing.OnsetDate;
                
                if (Request.Form.ContainsKey(onsetDateKey) && 
                    DateTime.TryParse(Request.Form[onsetDateKey], out var onsetDate))
                {
                    existing.OnsetDate = onsetDate;
                    
                    // Track earliest onset
                    if (!earliestOnset.HasValue || onsetDate < earliestOnset.Value)
                    {
                        earliestOnset = onsetDate;
                    }
                    
                    // Log onset date changes
                    if (oldOnsetDate != onsetDate)
                    {
                        if (oldOnsetDate.HasValue)
                        {
                            symptomChanges.Add($"{symptomName} onset changed from {oldOnsetDate.Value:dd MMM yyyy} to {onsetDate:dd MMM yyyy}");
                        }
                        else
                        {
                            symptomChanges.Add($"{symptomName} onset set to {onsetDate:dd MMM yyyy}");
                        }
                    }
                }
                else if (existing.OnsetDate.HasValue)
                {
                    // Track earliest even if not changed
                    if (!earliestOnset.HasValue || existing.OnsetDate.Value < earliestOnset.Value)
                    {
                        earliestOnset = existing.OnsetDate.Value;
                    }
                }

                // Update "Other" symptom text
                var symptom = await _context.Symptoms.FindAsync(symptomId);
                if (symptom?.Code == "OTHER" && Request.Form.ContainsKey("other_symptom_text"))
                {
                    var oldOtherText = existing.OtherSymptomText;
                    existing.OtherSymptomText = Request.Form["other_symptom_text"];
                    
                    if (oldOtherText != existing.OtherSymptomText)
                    {
                        symptomChanges.Add($"Other symptom description: {existing.OtherSymptomText}");
                    }
                }

                if (isNewSymptom)
                {
                    symptomChanges.Add($"Added symptom: {symptomName}");
                }

                if (!existing.IsDeleted)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                }
            }

            await _context.SaveChangesAsync();

            // Update case date of onset if earliest symptom onset is earlier
            if (earliestOnset.HasValue)
            {
                var caseToUpdate = await _context.Cases.FindAsync(Case.Id);
                if (caseToUpdate != null)
                {
                    if (!caseToUpdate.DateOfOnset.HasValue || earliestOnset.Value < caseToUpdate.DateOfOnset.Value)
                    {
                        var oldDate = caseToUpdate.DateOfOnset;
                        caseToUpdate.DateOfOnset = earliestOnset.Value;
                        
                        await _context.SaveChangesAsync();
                        
                        var dateChangeMsg = oldDate.HasValue
                            ? $"Case date of onset auto-updated from {oldDate.Value:dd MMM yyyy} to {earliestOnset.Value:dd MMM yyyy} (earliest symptom onset)"
                            : $"Case date of onset auto-set to {earliestOnset.Value:dd MMM yyyy} (earliest symptom onset)";
                        
                        symptomChanges.Add(dateChangeMsg);
                        
                        // Update the Case property so it reflects on the page
                        Case.DateOfOnset = earliestOnset.Value;
                    }
                }
            }

            // Log all symptom changes to audit log
            if (symptomChanges.Any())
            {
                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: Case.Id.ToString(),
                    fieldName: "Symptoms",
                    oldValue: null,
                    newValue: string.Join("; ", symptomChanges),
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );
            }
            
            return earliestOnset;
        }

        public async Task<JsonResult> OnGetSearchSymptomsAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new JsonResult(new List<object>());
            }

            var symptoms = await _context.Symptoms
                .Where(s => s.IsActive && 
                           s.Code != "OTHER" && 
                           s.Name.Contains(term))
                .OrderBy(s => s.Name)
                .Take(20)
                .Select(s => new { id = s.Id, text = s.Name })
                .ToListAsync();

            return new JsonResult(symptoms);
        }

        private bool CaseExists(Guid id)
        {
            return _context.Cases.Any(e => e.Id == id);
        }

        private string GetChangeSummary()
        {
            var changes = new System.Collections.Generic.List<string>();

            if (_originalCase.PatientId != Case.PatientId)
                changes.Add($"Patient changed");

            if (_originalCase.Type != Case.Type)
                changes.Add($"Type changed from {_originalCase.Type} to {Case.Type}");

            if (_originalCase.DateOfOnset != Case.DateOfOnset)
                changes.Add($"Date of Onset changed from {_originalCase.DateOfOnset?.ToString("dd MMM yyyy") ?? "not set"} to {Case.DateOfOnset?.ToString("dd MMM yyyy") ?? "not set"}");

            if (_originalCase.DateOfNotification != Case.DateOfNotification)
                changes.Add($"Date of Notification changed from {_originalCase.DateOfNotification?.ToString("dd MMM yyyy") ?? "not set"} to {Case.DateOfNotification?.ToString("dd MMM yyyy") ?? "not set"}");

            if (_originalCase.ConfirmationStatusId != Case.ConfirmationStatusId)
                changes.Add($"Confirmation Status changed");

            return changes.Any() ? string.Join("; ", changes) : "Case updated";
        }
    }
}
