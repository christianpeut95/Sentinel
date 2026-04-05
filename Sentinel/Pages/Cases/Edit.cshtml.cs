using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Cases
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
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IGeocodingService _geocodingService;
        private readonly IServiceProvider _serviceProvider;

        public EditModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            CustomFieldService customFieldService, 
            IDiseaseAccessService diseaseAccessService,
            IExposureRequirementService exposureRequirementService,
            ITaskService taskService,
            IJurisdictionService jurisdictionService,
            IGeocodingService geocodingService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _diseaseAccessService = diseaseAccessService;
            _exposureRequirementService = exposureRequirementService;
            _taskService = taskService;
            _jurisdictionService = jurisdictionService;
            _geocodingService = geocodingService;
            _serviceProvider = serviceProvider;
        }

        [BindProperty]
        public Case Case { get; set; } = default!;

        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<int, object> CustomFieldValues { get; set; } = new();
        
        public List<Symptom> CommonSymptoms { get; set; } = new();
        public List<Symptom> AllAvailableSymptoms { get; set; } = new();
        
        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();
        public Dictionary<int, List<Jurisdiction>> JurisdictionsByType { get; set; } = new();

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
                .Include(c => c.CaseState)
                .Include(c => c.Jurisdiction1).ThenInclude(j => j!.JurisdictionType)
                .Include(c => c.Jurisdiction2).ThenInclude(j => j!.JurisdictionType)
                .Include(c => c.Jurisdiction3).ThenInclude(j => j!.JurisdictionType)
                .Include(c => c.Jurisdiction4).ThenInclude(j => j!.JurisdictionType)
                .Include(c => c.Jurisdiction5).ThenInclude(j => j!.JurisdictionType)
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
                    .Where(cs => cs.IsActive && 
                                (cs.ApplicableTo == (Case.Type == CaseType.Case ? CaseTypeApplicability.Case : CaseTypeApplicability.Contact) || 
                                 cs.ApplicableTo == CaseTypeApplicability.Both))
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

            ViewData["HospitalId"] = new SelectList(
                await _context.Organizations
                    .Where(o => o.IsActive && o.OrganizationType != null && o.OrganizationType.Name == "Hospital")
                    .OrderBy(o => o.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Load States dropdown for case address
            ViewData["CaseStateId"] = new SelectList(
                await _context.States
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync(),
                "Id", "Name");

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

            // Load jurisdiction types only (not all jurisdictions - using autocomplete)
            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

            // Load exposure requirements
            if (Case.DiseaseId.HasValue)
            {
                DiseaseRequirements = await _exposureRequirementService.GetRequirementsForDiseaseAsync(Case.DiseaseId.Value);
                ShouldPromptForExposure = await _exposureRequirementService.ShouldPromptForExposureAsync(Case.DiseaseId.Value);
                
                ExposureCount = await _context.ExposureEvents.CountAsync(e => e.ExposedCaseId == Case.Id);
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
                        .Where(cs => cs.IsActive && 
                                    (cs.ApplicableTo == (Case.Type == CaseType.Case ? CaseTypeApplicability.Case : CaseTypeApplicability.Contact) || 
                                     cs.ApplicableTo == CaseTypeApplicability.Both))
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

                // Reload jurisdiction types only (not all jurisdictions - using autocomplete)
                ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            // CRITICAL FIX: Detach any cached Case entity from OnGet to force fresh load
            var existingEntry = _context.ChangeTracker.Entries<Case>()
                .FirstOrDefault(e => e.Entity.Id == Case.Id);
            
            if (existingEntry != null)
            {
                System.Diagnostics.Debug.WriteLine($"[EDIT] Found cached Case entity (State: {existingEntry.State}) - DETACHING to force fresh load");
                _context.Entry(existingEntry.Entity).State = EntityState.Detached;
            }

            // Load the tracked entity from database (now guaranteed fresh, not cached)
            var caseToUpdate = await _context.Cases.FindAsync(Case.Id);
            if (caseToUpdate == null)
            {
                return NotFound();
            }

            // Capture original values IMMEDIATELY after loading, before any modifications
            _originalCase = new Case
            {
                Id = caseToUpdate.Id,
                FriendlyId = caseToUpdate.FriendlyId,
                PatientId = caseToUpdate.PatientId,
                DiseaseId = caseToUpdate.DiseaseId,
                ConfirmationStatusId = caseToUpdate.ConfirmationStatusId,
                DateOfOnset = caseToUpdate.DateOfOnset,
                DateOfNotification = caseToUpdate.DateOfNotification,
                Hospitalised = caseToUpdate.Hospitalised,
                HospitalId = caseToUpdate.HospitalId,
                DateOfAdmission = caseToUpdate.DateOfAdmission,
                DateOfDischarge = caseToUpdate.DateOfDischarge,
                DiedDueToDisease = caseToUpdate.DiedDueToDisease,
                ClinicalNotificationDate = caseToUpdate.ClinicalNotificationDate,
                ClinicalNotifierOrganisation = caseToUpdate.ClinicalNotifierOrganisation,
                ClinicalNotificationNotes = caseToUpdate.ClinicalNotificationNotes,
                Jurisdiction1Id = caseToUpdate.Jurisdiction1Id,
                Jurisdiction2Id = caseToUpdate.Jurisdiction2Id,
                Jurisdiction3Id = caseToUpdate.Jurisdiction3Id,
                Jurisdiction4Id = caseToUpdate.Jurisdiction4Id,
                Jurisdiction5Id = caseToUpdate.Jurisdiction5Id,
                CaseAddressLine = caseToUpdate.CaseAddressLine,
                CaseCity = caseToUpdate.CaseCity,
                CaseStateId = caseToUpdate.CaseStateId,
                CasePostalCode = caseToUpdate.CasePostalCode
            };

            // Debug: Verify entity state
            var entry = _context.Entry(caseToUpdate);
            System.Diagnostics.Debug.WriteLine($"[EDIT] After FindAsync - Entity State: {entry.State}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] Case ID: {caseToUpdate.FriendlyId}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] Database ConfirmationStatusId: {caseToUpdate.ConfirmationStatusId}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] Form-bound ConfirmationStatusId: {Case.ConfirmationStatusId}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] Values are equal: {caseToUpdate.ConfirmationStatusId == Case.ConfirmationStatusId}");

            // Update properties from the bound model
            // This way EF Core can properly track what changed
            caseToUpdate.PatientId = Case.PatientId;
            caseToUpdate.DiseaseId = Case.DiseaseId;
            caseToUpdate.ConfirmationStatusId = Case.ConfirmationStatusId;
            caseToUpdate.DateOfOnset = Case.DateOfOnset;
            caseToUpdate.DateOfNotification = Case.DateOfNotification;
            caseToUpdate.Hospitalised = Case.Hospitalised;
            caseToUpdate.HospitalId = Case.HospitalId;
            caseToUpdate.DateOfAdmission = Case.DateOfAdmission;
            caseToUpdate.DateOfDischarge = Case.DateOfDischarge;
            caseToUpdate.DiedDueToDisease = Case.DiedDueToDisease;
            caseToUpdate.ClinicalNotificationDate = Case.ClinicalNotificationDate;
            caseToUpdate.ClinicalNotifierOrganisation = Case.ClinicalNotifierOrganisation;
            caseToUpdate.ClinicalNotificationNotes = Case.ClinicalNotificationNotes;
            caseToUpdate.Jurisdiction1Id = Case.Jurisdiction1Id;
            caseToUpdate.Jurisdiction2Id = Case.Jurisdiction2Id;
            caseToUpdate.Jurisdiction3Id = Case.Jurisdiction3Id;
            caseToUpdate.Jurisdiction4Id = Case.Jurisdiction4Id;
            caseToUpdate.Jurisdiction5Id = Case.Jurisdiction5Id;
            // Don't update Type - cases can't change type after creation

            // ===== CASE ADDRESS HANDLING =====
            // Track if case address changed to trigger geocoding
            string oldAddress = $"{caseToUpdate.CaseAddressLine}|{caseToUpdate.CaseCity}|{caseToUpdate.CaseStateId}|{caseToUpdate.CasePostalCode}";
            string newAddress = $"{Case.CaseAddressLine}|{Case.CaseCity}|{Case.CaseStateId}|{Case.CasePostalCode}";
            bool caseAddressChanged = oldAddress != newAddress;

            // Update case address fields
            caseToUpdate.CaseAddressLine = Case.CaseAddressLine;
            caseToUpdate.CaseCity = Case.CaseCity;
            caseToUpdate.CaseStateId = Case.CaseStateId;
            caseToUpdate.CasePostalCode = Case.CasePostalCode;

            // If address changed and not empty, mark as manual override and geocode
            if (caseAddressChanged && !string.IsNullOrWhiteSpace(Case.CaseAddressLine))
            {
                caseToUpdate.CaseAddressManualOverride = true;
                caseToUpdate.CaseAddressCapturedAt = DateTime.UtcNow;
            }

            // Debug: Check if properties are now modified
            System.Diagnostics.Debug.WriteLine($"[EDIT] After property updates - Entity State: {entry.State}");
            var statusProp = entry.Property(nameof(Case.ConfirmationStatusId));
            System.Diagnostics.Debug.WriteLine($"[EDIT] ConfirmationStatusId.IsModified: {statusProp.IsModified}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] ConfirmationStatusId OriginalValue: {statusProp.OriginalValue}");
            System.Diagnostics.Debug.WriteLine($"[EDIT] ConfirmationStatusId CurrentValue: {statusProp.CurrentValue}");

            try
            {
                // ===== SINGLE TRANSACTION PATTERN =====
                // All changes happen in memory first, then ONE save at the end
                // This ensures atomicity and proper review queue detection
                
                // Check if disease changed (for task auto-creation later)
                bool diseaseChanged = _originalCase.DiseaseId != caseToUpdate.DiseaseId;

                // Stage 1: Prepare symptom changes (don't save yet)
                var earliestOnset = await PrepareSymptomChangesAsync();

                // Stage 2: Auto-update onset date if symptoms indicate earlier date
                if (earliestOnset.HasValue && 
                    (caseToUpdate.DateOfOnset == null || earliestOnset.Value < caseToUpdate.DateOfOnset.Value))
                {
                    caseToUpdate.DateOfOnset = earliestOnset.Value;
                }

                // Stage 3: Prepare custom field changes (if disease is selected)
                if (Case.DiseaseId.HasValue)
                {
                    var customFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
                    if (customFields.Any())
                    {
                        // This method now stages changes without saving
                        await _customFieldService.SaveCaseCustomFieldValuesAsync(Case.Id, Request.Form, customFields);
                    }
                }

                // Stage 4: ONE ATOMIC SAVE - All changes committed together
                System.Diagnostics.Debug.WriteLine($"[EDIT] About to perform SINGLE atomic save");
                System.Diagnostics.Debug.WriteLine($"[EDIT] Entity state before save: {_context.Entry(caseToUpdate).State}");
                
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"[EDIT] Atomic save completed successfully");

                // ===== POST-SAVE: GEOCODE CASE ADDRESS IF CHANGED =====
                if (caseAddressChanged && !string.IsNullOrWhiteSpace(Case.CaseAddressLine))
                {
                    try
                    {
                        // Build full address for geocoding
                        var addressParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(Case.CaseAddressLine)) addressParts.Add(Case.CaseAddressLine);
                        if (!string.IsNullOrWhiteSpace(Case.CaseCity)) addressParts.Add(Case.CaseCity);

                        // Get state name if StateId is provided
                        if (Case.CaseStateId.HasValue)
                        {
                            var state = await _context.States.FindAsync(Case.CaseStateId.Value);
                            if (state != null)
                            {
                                addressParts.Add(state.Code ?? state.Name);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(Case.CasePostalCode)) addressParts.Add(Case.CasePostalCode);

                        string fullAddress = string.Join(", ", addressParts);

                        // Geocode the address
                        var (latitude, longitude) = await _geocodingService.GeocodeAsync(fullAddress);

                        if (latitude.HasValue && longitude.HasValue)
                        {
                            // Update case with geocoded coordinates
                            caseToUpdate.CaseLatitude = latitude.Value;
                            caseToUpdate.CaseLongitude = longitude.Value;
                            await _context.SaveChangesAsync();

                            System.Diagnostics.Debug.WriteLine($"[EDIT] Case address geocoded: {latitude}, {longitude}");

                            // Trigger background jurisdiction mapping (1-2 minutes)
                            _ = AutoDetectCaseJurisdictionsInBackgroundAsync(Case.Id, latitude.Value, longitude.Value);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[EDIT] Geocoding failed for address: {fullAddress}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[EDIT] Error geocoding case address: {ex.Message}");
                        // Don't fail the update if geocoding fails
                    }
                }

                // Post-save actions (these don't modify the database)
                
                // Auto-create tasks if disease changed
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

                // Log audit entry
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

        /// <summary>
        /// Prepares symptom changes in memory without saving to database.
        /// Returns the earliest onset date found, or null if none.
        /// </summary>
        private async Task<DateTime?> PrepareSymptomChangesAsync()
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

            // All symptom changes are now staged in memory, ready to be saved with the main transaction
            // The audit logging will be handled after the successful save in OnPostAsync
            
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

        /// <summary>
        /// Background task to auto-detect case jurisdictions based on geocoded coordinates.
        /// Runs asynchronously (1-2 minutes) using point-in-polygon detection.
        /// </summary>
        private async Task AutoDetectCaseJurisdictionsInBackgroundAsync(Guid caseId, double latitude, double longitude)
        {
            try
            {
                // Create a new scope for background work - this ensures proper DI and DbContext lifecycle
                using var scope = _serviceProvider.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedJurisdictionService = scope.ServiceProvider.GetRequiredService<IJurisdictionService>();

                // Reload the case in this scope
                var caseEntity = await scopedContext.Cases.FindAsync(caseId);
                if (caseEntity == null) return;

                // Detect jurisdictions containing the case address point
                var detectedJurisdictions = await scopedJurisdictionService.FindJurisdictionsContainingPointAsync(
                    latitude,
                    longitude
                );

                // Auto-assign to appropriate jurisdiction fields based on JurisdictionType.FieldNumber
                // Group by field number to avoid overwriting - take first match for each type
                var jurisdictionsByField = detectedJurisdictions
                    .Where(j => j.JurisdictionType?.FieldNumber != null)
                    .GroupBy(j => j.JurisdictionType!.FieldNumber)
                    .ToDictionary(g => g.Key, g => g.First());

                bool anyAssigned = false;

                foreach (var kvp in jurisdictionsByField)
                {
                    var fieldNumber = kvp.Key;
                    var jurisdiction = kvp.Value;

                    switch (fieldNumber)
                    {
                        case 1:
                            caseEntity.Jurisdiction1Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"? Assigned Case Jurisdiction1: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 2:
                            caseEntity.Jurisdiction2Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"? Assigned Case Jurisdiction2: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 3:
                            caseEntity.Jurisdiction3Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"? Assigned Case Jurisdiction3: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 4:
                            caseEntity.Jurisdiction4Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"? Assigned Case Jurisdiction4: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                        case 5:
                            caseEntity.Jurisdiction5Id = jurisdiction.Id;
                            anyAssigned = true;
                            Console.WriteLine($"? Assigned Case Jurisdiction5: {jurisdiction.Name} (Type: {jurisdiction.JurisdictionType?.Name})");
                            break;
                    }
                }

                if (anyAssigned)
                {
                    // Save the updated jurisdictions
                    await scopedContext.SaveChangesAsync();
                    Console.WriteLine($"? Background task: Auto-detected and saved {detectedJurisdictions.Count} jurisdictions for case {caseId}");
                }
            }
            catch (Exception ex)
            {
                // Don't fail - just log the error
                Console.WriteLine($"? Background task error: Failed to auto-detect case jurisdictions: {ex.Message}");
            }
        }
    }
}
