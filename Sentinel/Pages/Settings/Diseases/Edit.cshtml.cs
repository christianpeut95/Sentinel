using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sentinel.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Disease Disease { get; set; } = default!;

        public SelectList ParentDiseases { get; set; } = default!;
        public List<CustomFieldDefinition> AvailableCustomFields { get; set; } = new();
        public List<int> LinkedFieldIds { get; set; } = new();
        public List<int> InheritedFieldIds { get; set; } = new();
        
        public List<Symptom> AllSymptoms { get; set; } = new();
        public List<DiseaseSymptom> DiseaseSymptoms { get; set; } = new();
        public List<Disease> ChildDiseases { get; set; } = new();
        public List<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var disease = await _context.Diseases.FirstOrDefaultAsync(m => m.Id == id);
            if (disease == null)
            {
                return NotFound();
            }
            Disease = disease;
            await LoadParentDiseases(id.Value);
            await LoadCategories();
            await LoadCustomFields();
            await LoadSymptoms();
            await LoadChildDiseases(id.Value);
            await LoadDiseaseTaskTemplates(id.Value);
            await LoadAllTaskTemplates();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action)
        {
            if (!ModelState.IsValid)
            {
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            try
            {
                if (Disease.ParentDiseaseId.HasValue && Disease.ParentDiseaseId.Value == Disease.Id)
                {
                    ModelState.AddModelError("Disease.ParentDiseaseId", "A disease cannot be its own parent.");
                    await LoadParentDiseases(Disease.Id);
                    await LoadCategories();
                    return Page();
                }

                if (await _context.Diseases.AsNoTracking().AnyAsync(d => d.Code == Disease.Code && d.Id != Disease.Id))
                {
                    ModelState.AddModelError("Disease.Code", "A disease with this code already exists.");
                    await LoadParentDiseases(Disease.Id);
                    await LoadCategories();
                    return Page();
                }

                _context.Attach(Disease).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await DiseaseExists(Disease.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                var diseaseName = Disease.Name;
                var diseaseId = Disease.Id;
                
                TempData["SuccessMessage"] = $"Disease '{diseaseName}' has been updated successfully.";
                
                // If "Save & Add Child" was clicked, redirect to create page with parent set
                if (action == "saveAndAddChild")
                {
                    return RedirectToPage("./Create", new { parentId = diseaseId });
                }
                
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                await LoadCustomFields();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSaveCustomFieldsAsync()
        {
            var diseaseId = Disease.Id;
            
            // Remove all existing links for this disease
            var existingLinks = await _context.DiseaseCustomFields
                .Where(dcf => dcf.DiseaseId == diseaseId)
                .ToListAsync();
            _context.DiseaseCustomFields.RemoveRange(existingLinks);

            // Add new links based on form data
            var formKeys = Request.Form.Keys.Where(k => k.StartsWith("field_")).ToList();
            foreach (var key in formKeys)
            {
                var fieldIdStr = key.Replace("field_", "");
                if (int.TryParse(fieldIdStr, out int fieldId))
                {
                    var inheritKey = $"inherit_{fieldId}";
                    var inheritToChildren = Request.Form[inheritKey].ToString() == "true";

                    var link = new DiseaseCustomField
                    {
                        DiseaseId = diseaseId,
                        CustomFieldDefinitionId = fieldId,
                        InheritToChildDiseases = inheritToChildren,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.DiseaseCustomFields.Add(link);
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Custom fields updated successfully.";
            
            return RedirectToPage(new { id = diseaseId });
        }

        private async Task LoadCustomFields()
        {
            // Get all available custom fields for cases
            AvailableCustomFields = await _context.CustomFieldDefinitions
                .Where(f => f.ShowOnCaseForm && f.IsActive)
                .Include(f => f.LookupTable)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();

            // Get linked fields for this disease
            var links = await _context.DiseaseCustomFields
                .Where(dcf => dcf.DiseaseId == Disease.Id)
                .ToListAsync();

            LinkedFieldIds = links.Select(l => l.CustomFieldDefinitionId).ToList();
            InheritedFieldIds = links.Where(l => l.InheritToChildDiseases).Select(l => l.CustomFieldDefinitionId).ToList();
        }

        private async Task<bool> DiseaseExists(Guid id)
        {
            return await _context.Diseases.AnyAsync(e => e.Id == id);
        }

        private async Task LoadParentDiseases(Guid currentDiseaseId)
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive && d.Id != currentDiseaseId)
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    DisplayName = new string('—', d.Level) + " " + d.Name
                })
                .ToListAsync();

            ParentDiseases = new SelectList(diseases, "Id", "DisplayName");
        }

        private async Task LoadCategories()
        {
            ViewData["CategoryId"] = new SelectList(
                await _context.DiseaseCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name");
        }

        private async Task LoadSymptoms()
        {
            AllSymptoms = await _context.Symptoms
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            DiseaseSymptoms = await _context.DiseaseSymptoms
                .Include(ds => ds.Symptom)
                .Where(ds => ds.DiseaseId == Disease.Id)
                .OrderBy(ds => ds.SortOrder)
                .ToListAsync();
        }

        private async Task LoadChildDiseases(Guid diseaseId)
        {
            ChildDiseases = await _context.Diseases
                .Where(d => d.ParentDiseaseId == diseaseId && d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ThenBy(d => d.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveSymptomsAsync(Guid? id)
        {
            if (!id.HasValue)
            {
                return BadRequest("Disease ID is required.");
            }

            var diseaseId = id.Value;
            
            // Verify the disease exists
            var diseaseExists = await _context.Diseases.AnyAsync(d => d.Id == diseaseId);
            if (!diseaseExists)
            {
                return NotFound($"Disease with ID {diseaseId} not found.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var applyToChildren = Request.Form["applyToChildren"].ToString() == "true";

            // Get existing disease-symptom associations (including soft-deleted)
            var existingAssociations = await _context.DiseaseSymptoms
                .IgnoreQueryFilters()
                .Where(ds => ds.DiseaseId == diseaseId)
                .ToListAsync();

            // Get symptom IDs from form (only the checkbox keys, not symptom_common_X or symptom_order_X)
            var selectedSymptomIds = Request.Form.Keys
                .Where(k => k.StartsWith("symptom_") && 
                           !k.Contains("_common_") && 
                           !k.Contains("_order_"))
                .Select(k => int.Parse(k.Replace("symptom_", "")))
                .ToList();

            // Remove unselected symptoms
            foreach (var existing in existingAssociations.Where(e => !e.IsDeleted))
            {
                if (!selectedSymptomIds.Contains(existing.SymptomId))
                {
                    existing.IsDeleted = true;
                    existing.DeletedAt = DateTime.UtcNow;
                    existing.DeletedByUserId = userId;
                }
            }

            // Add or update selected symptoms
            foreach (var symptomId in selectedSymptomIds)
            {
                var existing = existingAssociations.FirstOrDefault(ds => ds.SymptomId == symptomId);

                if (existing != null && existing.IsDeleted)
                {
                    // Restore if was deleted
                    existing.IsDeleted = false;
                    existing.DeletedAt = null;
                    existing.DeletedByUserId = null;
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                }
                else if (existing == null)
                {
                    // Create new association
                    var diseaseSymptom = new DiseaseSymptom
                    {
                        DiseaseId = diseaseId,
                        SymptomId = symptomId,
                        IsCommon = true,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId
                    };
                    _context.DiseaseSymptoms.Add(diseaseSymptom);
                    existing = diseaseSymptom;
                }

                // Update IsCommon from form
                var isCommonKey = $"symptom_common_{symptomId}";
                if (Request.Form.ContainsKey(isCommonKey))
                {
                    existing.IsCommon = Request.Form[isCommonKey] == "true";
                }

                // Update SortOrder from form
                var sortOrderKey = $"symptom_order_{symptomId}";
                if (Request.Form.ContainsKey(sortOrderKey) && int.TryParse(Request.Form[sortOrderKey], out var sortOrder))
                {
                    existing.SortOrder = sortOrder;
                }

                if (!existing.IsDeleted)
                {
                    existing.UpdatedAt = DateTime.UtcNow;
                    existing.UpdatedBy = userId;
                }
            }

            await _context.SaveChangesAsync();

            // Apply to child diseases if requested
            if (applyToChildren)
            {
                await ApplySymptomsToChildrenAsync(diseaseId, selectedSymptomIds, userId);
            }

            TempData["SuccessMessage"] = applyToChildren 
                ? "Symptoms updated successfully and applied to child diseases." 
                : "Symptoms updated successfully.";
            
            return RedirectToPage(new { id = diseaseId });
        }

        private async Task ApplySymptomsToChildrenAsync(Guid parentDiseaseId, List<int> symptomIds, string? userId)
        {
            // Get all child diseases recursively
            var childDiseaseIds = await GetAllChildDiseaseIdsAsync(parentDiseaseId);

            // Get parent symptoms with their settings (use IgnoreQueryFilters to see all including deleted)
            var parentSymptoms = await _context.DiseaseSymptoms
                .IgnoreQueryFilters()
                .Where(ds => ds.DiseaseId == parentDiseaseId && !ds.IsDeleted)
                .ToListAsync();

            foreach (var childId in childDiseaseIds)
            {
                // Get existing associations for this child (including soft-deleted)
                var existingAssociations = await _context.DiseaseSymptoms
                    .IgnoreQueryFilters()
                    .Where(ds => ds.DiseaseId == childId)
                    .ToListAsync();

                // Remove symptoms not in the parent's list
                foreach (var existing in existingAssociations.Where(e => !e.IsDeleted))
                {
                    if (!symptomIds.Contains(existing.SymptomId))
                    {
                        existing.IsDeleted = true;
                        existing.DeletedAt = DateTime.UtcNow;
                        existing.DeletedByUserId = userId;
                    }
                }

                // Add new symptoms from parent
                foreach (var symptomId in symptomIds)
                {
                    var existing = existingAssociations.FirstOrDefault(ds => ds.SymptomId == symptomId);

                    if (existing != null && existing.IsDeleted)
                    {
                        // Restore
                        existing.IsDeleted = false;
                        existing.DeletedAt = null;
                        existing.DeletedByUserId = null;
                        existing.UpdatedAt = DateTime.UtcNow;
                        existing.UpdatedBy = userId;
                        
                        // Update properties from parent
                        var parentSymptom = parentSymptoms.FirstOrDefault(ps => ps.SymptomId == symptomId);
                        if (parentSymptom != null)
                        {
                            existing.IsCommon = parentSymptom.IsCommon;
                            existing.SortOrder = parentSymptom.SortOrder;
                        }
                    }
                    else if (existing == null)
                    {
                        // Create new - copy settings from parent
                        var parentSymptom = parentSymptoms.FirstOrDefault(ps => ps.SymptomId == symptomId);
                        
                        _context.DiseaseSymptoms.Add(new DiseaseSymptom
                        {
                            DiseaseId = childId,
                            SymptomId = symptomId,
                            IsCommon = parentSymptom?.IsCommon ?? true,
                            SortOrder = parentSymptom?.SortOrder ?? 0,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId
                        });
                    }
                    else if (!existing.IsDeleted)
                    {
                        // Update existing active record with parent's settings
                        var parentSymptom = parentSymptoms.FirstOrDefault(ps => ps.SymptomId == symptomId);
                        if (parentSymptom != null)
                        {
                            existing.IsCommon = parentSymptom.IsCommon;
                            existing.SortOrder = parentSymptom.SortOrder;
                            existing.UpdatedAt = DateTime.UtcNow;
                            existing.UpdatedBy = userId;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<List<Guid>> GetAllChildDiseaseIdsAsync(Guid parentId)
        {
            var childIds = new List<Guid>();
            var directChildren = await _context.Diseases
                .Where(d => d.ParentDiseaseId == parentId && d.IsActive)
                .Select(d => d.Id)
                .ToListAsync();

            childIds.AddRange(directChildren);

            // Recursively get children of children
            foreach (var childId in directChildren)
            {
                var grandChildren = await GetAllChildDiseaseIdsAsync(childId);
                childIds.AddRange(grandChildren);
            }

            return childIds;
        }

        public async Task<IActionResult> OnPostSaveExposureTrackingAsync()
        {
            // Remove validation for properties we're not editing
            ModelState.Remove(nameof(Disease.Name));
            ModelState.Remove(nameof(Disease.Code));
            ModelState.Remove(nameof(Disease.ExportCode));
            ModelState.Remove(nameof(Disease.PathIds));
            ModelState.Remove(nameof(Disease.DiseaseCategoryId));
            ModelState.Remove(nameof(Disease.ParentDiseaseId));
            
            if (!ModelState.IsValid)
            {
                // Log model state errors for debugging
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                await LoadCustomFields();
                await LoadSymptoms();
                await LoadChildDiseases(Disease.Id);
                return Page();
            }

            try
            {
                var existingDisease = await _context.Diseases.FindAsync(Disease.Id);
                if (existingDisease == null)
                {
                    return NotFound();
                }

                existingDisease.ExposureTrackingMode = Disease.ExposureTrackingMode;
                existingDisease.DefaultToResidentialAddress = Disease.DefaultToResidentialAddress;
                existingDisease.AlwaysPromptForLocation = Disease.AlwaysPromptForLocation;
                existingDisease.SyncWithPatientAddressUpdates = Disease.SyncWithPatientAddressUpdates;
                existingDisease.ExposureGuidanceText = Disease.ExposureGuidanceText;
                existingDisease.RequireGeographicCoordinates = Disease.RequireGeographicCoordinates;
                existingDisease.AllowDomesticAcquisition = Disease.AllowDomesticAcquisition;
                existingDisease.ExposureDataGracePeriodDays = Disease.ExposureDataGracePeriodDays;
                existingDisease.RequiredLocationTypeIds = Disease.RequiredLocationTypeIds;
                existingDisease.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Exposure tracking settings have been updated successfully.";
                return RedirectToPage(new { id = Disease.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                await LoadCustomFields();
                await LoadSymptoms();
                await LoadChildDiseases(Disease.Id);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostSaveReviewSettingsAsync()
        {
            // Remove validation for properties we're not editing
            ModelState.Remove(nameof(Disease.Name));
            ModelState.Remove(nameof(Disease.Code));
            ModelState.Remove(nameof(Disease.ExportCode));
            ModelState.Remove(nameof(Disease.PathIds));
            ModelState.Remove(nameof(Disease.DiseaseCategoryId));
            ModelState.Remove(nameof(Disease.ParentDiseaseId));
            
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                await LoadCustomFields();
                await LoadSymptoms();
                await LoadChildDiseases(Disease.Id);
                return Page();
            }

            try
            {
                var existingDisease = await _context.Diseases.FindAsync(Disease.Id);
                if (existingDisease == null)
                {
                    return NotFound();
                }

                // Update review settings
                existingDisease.ReviewGroupingWindowHours = Disease.ReviewGroupingWindowHours;
                existingDisease.ReviewAutoQueueLabResults = Disease.ReviewAutoQueueLabResults;
                existingDisease.ReviewAutoQueueExposures = Disease.ReviewAutoQueueExposures;
                existingDisease.ReviewAutoQueueContacts = Disease.ReviewAutoQueueContacts;
                existingDisease.ReviewAutoQueueConfirmationChanges = Disease.ReviewAutoQueueConfirmationChanges;
                existingDisease.ReviewAutoQueueDiseaseChanges = Disease.ReviewAutoQueueDiseaseChanges;
                existingDisease.ReviewAutoQueueClinicalNotifications = Disease.ReviewAutoQueueClinicalNotifications;
                existingDisease.ReviewAutoQueueNewCases = Disease.ReviewAutoQueueNewCases;
                existingDisease.ReviewDefaultPriority = Disease.ReviewDefaultPriority;
                existingDisease.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Data review settings have been updated successfully.";
                return RedirectToPage(new { id = Disease.Id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadParentDiseases(Disease.Id);
                await LoadCategories();
                await LoadCustomFields();
                await LoadSymptoms();
                await LoadChildDiseases(Disease.Id);
                return Page();
            }
        }

        private async Task LoadDiseaseTaskTemplates(Guid diseaseId)
        {
            DiseaseTaskTemplates = await _context.DiseaseTaskTemplates
                .Include(dtt => dtt.TaskTemplate)
                    .ThenInclude(tt => tt!.TaskType)
                .Where(dtt => dtt.DiseaseId == diseaseId && dtt.IsActive)
                .OrderBy(dtt => dtt.DisplayOrder)
                .ThenBy(dtt => dtt.TaskTemplate!.Name)
                .ToListAsync();
        }

        private async Task LoadAllTaskTemplates()
        {
            AllTaskTemplates = await _context.TaskTemplates
                .Include(tt => tt.TaskType)
                .Where(tt => tt.IsActive)
                .OrderBy(tt => tt.TaskType!.DisplayOrder)
                .ThenBy(tt => tt.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostSaveTaskMappingAsync(Guid id, Guid dttId)
        {
            var diseaseTaskTemplate = await _context.DiseaseTaskTemplates.FindAsync(dttId);

            if (diseaseTaskTemplate == null || diseaseTaskTemplate.DiseaseId != id)
            {
                return NotFound();
            }

            var inputMappingJson = Request.Form["inputMappingJson"].ToString();
            var outputMappingJson = Request.Form["outputMappingJson"].ToString();

            // Validate JSON if provided
            if (!string.IsNullOrWhiteSpace(inputMappingJson))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(inputMappingJson);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    TempData["ErrorMessage"] = $"Invalid Input Mapping JSON: {ex.Message}";
                    return RedirectToPage(new { id });
                }
            }

            if (!string.IsNullOrWhiteSpace(outputMappingJson))
            {
                try
                {
                    System.Text.Json.JsonDocument.Parse(outputMappingJson);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    TempData["ErrorMessage"] = $"Invalid Output Mapping JSON: {ex.Message}";
                    return RedirectToPage(new { id });
                }
            }

            // Update mappings
            diseaseTaskTemplate.InputMappingJson = string.IsNullOrWhiteSpace(inputMappingJson) ? null : inputMappingJson.Trim();
            diseaseTaskTemplate.OutputMappingJson = string.IsNullOrWhiteSpace(outputMappingJson) ? null : outputMappingJson.Trim();
            diseaseTaskTemplate.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Survey mappings saved successfully for '{diseaseTaskTemplate.TaskTemplate?.Name}'.";
            return RedirectToPage(new { id });
        }

        // NEW: Manage Task Template Assignments
        public List<TaskTemplate> AllTaskTemplates { get; set; } = new();

        public async Task<IActionResult> OnPostUpdateTaskAssignmentAsync(Guid id, Guid taskTemplateId)
        {
            try
            {
                var autoCreateCase = Request.Form[$"autoCreateCase_{taskTemplateId}"] == "true";
                var autoCreateContact = Request.Form[$"autoCreateContact_{taskTemplateId}"] == "true";
                var autoCreateLab = Request.Form[$"autoCreateLab_{taskTemplateId}"] == "true";

                var assignment = await _context.DiseaseTaskTemplates
                    .FirstOrDefaultAsync(d => d.DiseaseId == id && d.TaskTemplateId == taskTemplateId);

                if (assignment != null)
                {
                    assignment.AutoCreateOnCaseCreation = autoCreateCase;
                    assignment.AutoCreateOnContactCreation = autoCreateContact;
                    assignment.AutoCreateOnLabConfirmation = autoCreateLab;
                    assignment.ModifiedAt = DateTime.UtcNow;
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Task assignment updated successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Task assignment not found.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating task assignment: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostAddTaskTemplateAsync(Guid id, Guid newTaskTemplateId)
        {
            try
            {
                var autoCreateCase = Request.Form["newAutoCreateCase"] == "true";
                var autoCreateContact = Request.Form["newAutoCreateContact"] == "true";
                var autoCreateLab = Request.Form["newAutoCreateLab"] == "true";
                var applyToChildren = Request.Form["newApplyToChildren"] == "true";

                // Check if already assigned
                var exists = await _context.DiseaseTaskTemplates
                    .AnyAsync(d => d.DiseaseId == id && d.TaskTemplateId == newTaskTemplateId);

                if (exists)
                {
                    TempData["ErrorMessage"] = "This task template is already assigned to this disease.";
                    return RedirectToPage(new { id });
                }

                var newAssignment = new DiseaseTaskTemplate
                {
                    Id = Guid.NewGuid(),
                    DiseaseId = id,
                    TaskTemplateId = newTaskTemplateId,
                    AutoCreateOnCaseCreation = autoCreateCase,
                    AutoCreateOnContactCreation = autoCreateContact,
                    AutoCreateOnLabConfirmation = autoCreateLab,
                    ApplyToChildren = applyToChildren,
                    AllowChildOverride = true,
                    IsInherited = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DiseaseTaskTemplates.Add(newAssignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Task template assigned successfully.";
                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error assigning task template: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }

        public async Task<IActionResult> OnPostRemoveTaskTemplateAsync(Guid id, Guid taskTemplateId)
        {
            try
            {
                var assignment = await _context.DiseaseTaskTemplates
                    .FirstOrDefaultAsync(d => d.DiseaseId == id && d.TaskTemplateId == taskTemplateId);

                if (assignment != null)
                {
                    _context.DiseaseTaskTemplates.Remove(assignment);
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Task template removed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Task assignment not found.";
                }

                return RedirectToPage(new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error removing task template: {ex.Message}";
                return RedirectToPage(new { id });
            }
        }
    }
}


