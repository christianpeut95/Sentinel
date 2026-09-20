using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Contacts
{
    [Authorize(Policy = "Permission.Case.View")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly CustomFieldService _customFieldService;
        private readonly IPermissionService _permissionService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly ITaskService _taskService;
        private readonly IProtectedFileStorageService _fileStorage;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            CustomFieldService customFieldService, 
            IPermissionService permissionService, 
            IDiseaseAccessService diseaseAccessService, 
            ITaskService taskService,
            IProtectedFileStorageService fileStorage,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _permissionService = permissionService;
            _diseaseAccessService = diseaseAccessService;
            _taskService = taskService;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public Case Contact { get; set; } = default!;
        public List<Note> Notes { get; set; } = new List<Note>();
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<int, object> CustomFieldValues { get; set; } = new();
        public List<LabResult> LabResults { get; set; } = new List<LabResult>();
        public List<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();
        public List<ExposureEvent> Acquisitions { get; set; } = new List<ExposureEvent>();
        public List<ExposureEvent> Transmissions { get; set; } = new List<ExposureEvent>();
        public List<CaseTask> Tasks { get; set; } = new List<CaseTask>();
        public List<TaskTemplate> AvailableTaskTemplates { get; set; } = new List<TaskTemplate>();
        public SelectList TaskTypesList { get; set; } = default!;
        public SelectList SurveyTemplatesList { get; set; } = default!;
        public SelectList LaboratoriesList { get; set; } = default!;
        public SelectList OrganizationsList { get; set; } = default!;
        public SelectList DiseasesList { get; set; } = default!;
        public SelectList SpecimenTypesList { get; set; } = default!;
        public SelectList TestTypesList { get; set; } = default!;
        public SelectList TestResultsList { get; set; } = default!;
        public SelectList ResultUnitsList { get; set; } = default!;

        // Note properties
        public Note NewNote { get; set; } = default!;
        public IFormFile? Attachment { get; set; }

        // LabResult properties
        public LabResult NewLabResult { get; set; } = default!;
        public IFormFile? LabResultAttachment { get; set; }

        public bool CanEditCase { get; private set; }
        public bool CanDeleteCase { get; private set; }
        public bool CanViewLabResults { get; private set; }
        public bool CanCreateLabResults { get; private set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contactEntity = await _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
                .Include(c => c.Patient)
                    .ThenInclude(p => p.Gender)
                .Include(c => c.Patient)
                    .ThenInclude(p => p.CountryOfBirth)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (contactEntity == null)
            {
                return NotFound();
            }

            // Redirect to appropriate page based on case type
            if (contactEntity.Type == CaseType.Case)
            {
                return RedirectToPage("/Cases/Details", new { id = contactEntity.Id });
            }

            // Check disease access
            if (contactEntity.DiseaseId.HasValue)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var canAccess = await _diseaseAccessService.CanAccessDiseaseAsync(userId, contactEntity.DiseaseId.Value);
                
                if (!canAccess)
                {
                    return Forbid();
                }
            }

            Contact = contactEntity;
            CanEditCase = await UserCanEditCaseAsync(contactEntity.Id);
            CanDeleteCase = await UserCanDeleteCaseAsync(contactEntity.Id);
            CanViewLabResults = await UserCanViewLabResultsAsync(contactEntity.Id);
            CanCreateLabResults = await UserCanCreateLabResultsAsync(contactEntity.Id);

            // Load notes
            Notes = await _context.Notes
                .Where(n => n.CaseId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            if (CanViewLabResults)
            {
                LabResults = await _context.LabResults
                    .Include(lr => lr.Laboratory)
                    .Include(lr => lr.OrderingProvider)
                    .Include(lr => lr.SpecimenType)
                    .Include(lr => lr.ResultUnits)
                    .Include(lr => lr.TestedDisease)
                    .Include(lr => lr.Markers).ThenInclude(m => m.Pathogen)
                    .Include(lr => lr.Markers).ThenInclude(m => m.TestMethod)
                    .Where(lr => lr.CaseId == id)
                    .OrderByDescending(lr => lr.ResultDate)
                    .ThenByDescending(lr => lr.SpecimenCollectionDate)
                    .ToListAsync();
            }

            // Load custom fields if disease is selected
            if (Contact.DiseaseId.HasValue)
            {
                CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Contact.DiseaseId.Value);
                CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Contact.Id);
            }

            // Load symptoms
            CaseSymptoms = await _context.CaseSymptoms
                .Include(cs => cs.Symptom)
                .Where(cs => cs.CaseId == id)
                .OrderBy(cs => cs.OnsetDate ?? DateTime.MaxValue)
                .ThenBy(cs => cs.Symptom!.Name)
                .ToListAsync();

            // Load ACQUISITIONS - Where THIS contact was exposed (upstream)
            Acquisitions = await _context.ExposureEvents
                .Include(e => e.Event).ThenInclude(e => e!.Location)
                .Include(e => e.Location)
                .Include(e => e.SourceCase).ThenInclude(c => c!.Patient)
                .Include(e => e.ContactClassification)
                .Where(e => e.ExposedCaseId == id)
                .OrderByDescending(e => e.ExposureStartDate)
                .ToListAsync();

            // Load TRANSMISSIONS - Who THIS contact exposed (downstream)
            Transmissions = await _context.ExposureEvents
                .Include(e => e.ExposedCase).ThenInclude(c => c!.Patient)
                .Include(e => e.ExposedCase).ThenInclude(c => c!.Disease)
                .Include(e => e.ExposedCase).ThenInclude(c => c!.ConfirmationStatus)
                .Include(e => e.ContactClassification)
                .Where(e => e.SourceCaseId == id)
                .OrderByDescending(e => e.ExposureStartDate)
                .ToListAsync();

            // Load tasks
            Tasks = await _taskService.GetTasksForCase(id.Value);

            if (CanCreateLabResults)
            {
                await LoadLabResultDropdowns();
            }

            // Load task templates for the contact's disease
            await LoadTaskTemplates();

            await _auditService.LogViewAsync(
                entityType: "Case",
                entityId: Contact.Id.ToString(),
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
            );

            return Page();
        }

        private async Task<bool> UserCanEditCaseAsync(Guid? caseId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) ||
                !await _permissionService.HasPermissionAsync(userId, PermissionModule.Case, PermissionAction.Edit))
            {
                return false;
            }

            // Mutations must use the normal case query so disease hierarchy and
            // restricted-disease filters are applied to the supplied contact ID.
            return !caseId.HasValue || await _context.Cases
                .AnyAsync(c => c.Id == caseId.Value && !c.IsDeleted);
        }

        private async Task<bool> UserCanDeleteCaseAsync(Guid caseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userId)
                && await _permissionService.HasPermissionAsync(userId, PermissionModule.Case, PermissionAction.Delete)
                && await _context.Cases.AnyAsync(c => c.Id == caseId && !c.IsDeleted);
        }

        private async Task<bool> UserCanViewLabResultsAsync(Guid caseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userId)
                && await _permissionService.HasPermissionAsync(userId, PermissionModule.Laboratory, PermissionAction.View)
                && await _context.Cases.AnyAsync(c => c.Id == caseId && c.Type == CaseType.Contact);
        }

        private async Task<bool> UserCanCreateLabResultsAsync(Guid caseId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userId)
                && await _permissionService.HasPermissionAsync(userId, PermissionModule.Laboratory, PermissionAction.Create)
                && await UserCanEditCaseAsync(caseId);
        }

        // ========================================================================
        // POST HANDLERS
        // ========================================================================

        public async Task<IActionResult> OnPostAddNoteAsync(Guid id)
        {
            if (!await UserCanEditCaseAsync(id))
            {
                return Forbid();
            }

            // Manually bind the Note from form data
            NewNote = new Note();
            await TryUpdateModelAsync(NewNote, "NewNote");
            
            // Manually bind the file attachment
            Attachment = Request.Form.Files.GetFile("Attachment");
            
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToPage(new { id });
            }

            NewNote.Id = Guid.NewGuid();
            NewNote.CaseId = id;
            NewNote.CreatedBy = User.Identity?.Name ?? "Unknown";
            NewNote.CreatedAt = DateTime.UtcNow;

            // Handle file attachment
            if (Attachment != null && Attachment.Length > 0)
            {
                try
                {
                    var storedFile = await _fileStorage.SaveAttachmentAsync(
                        Attachment,
                        ProtectedFileStorageService.NotesCategory,
                        HttpContext.RequestAborted);
                    NewNote.AttachmentPath = storedFile.StorageKey;
                    NewNote.AttachmentFileName = storedFile.OriginalFileName;
                    NewNote.AttachmentSize = storedFile.Length;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(
                        HttpContext,
                        ex,
                        "The attachment could not be accepted. Check its type, content and size before trying again.");
                    return RedirectToPage(new { id });
                }
            }

            _context.Notes.Add(NewNote);
            await _context.SaveChangesAsync();

            await _auditService.LogChangeAsync(
                entityType: "Case",
                entityId: id.ToString(),
                fieldName: "Note Added",
                oldValue: null,
                newValue: NewNote.Subject ?? "Note",
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Note added successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteNoteAsync(Guid id, Guid noteId)
        {
            if (!await UserCanDeleteCaseAsync(id))
            {
                return Forbid();
            }

            var note = await _context.Notes.FindAsync(noteId);
            if (note == null || note.CaseId != id)
            {
                return NotFound();
            }

            await _context.SoftDeleteAsync(note);

            await _auditService.LogChangeAsync(
                entityType: "Case",
                entityId: id.ToString(),
                fieldName: "Note Deleted",
                oldValue: note.Subject ?? "Note",
                newValue: null,
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Note deleted successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddLabResultAsync(Guid id)
        {
            if (!await UserCanCreateLabResultsAsync(id))
            {
                return Forbid();
            }

            var contactEntity = await _context.Cases
                .FirstOrDefaultAsync(c => c.Id == id && c.Type == CaseType.Contact);

            if (contactEntity == null)
            {
                return NotFound();
            }

            // Manually bind the LabResult from form data
            NewLabResult = new LabResult();
            await TryUpdateModelAsync(NewLabResult, "NewLabResult");
            
            // Manually bind the file attachment
            LabResultAttachment = Request.Form.Files.GetFile("LabResultAttachment");
            
            // Remove properties from ModelState validation that we set manually or are navigation properties
            ModelState.Remove("NewLabResult.CaseId");
            ModelState.Remove("NewLabResult.FriendlyId");
            ModelState.Remove("NewLabResult.Case");
            ModelState.Remove("NewLabResult.Laboratory");
            ModelState.Remove("NewLabResult.OrderingProvider");
            ModelState.Remove("NewLabResult.SpecimenType");
            ModelState.Remove("NewLabResult.TestType");
            ModelState.Remove("NewLabResult.TestResult");
            ModelState.Remove("NewLabResult.ResultUnits");
            ModelState.Remove("NewLabResult.TestedDisease");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                return RedirectToPage(new { id });
            }

            // Set the case ID and other required fields
            NewLabResult.Id = Guid.NewGuid();
            NewLabResult.CaseId = contactEntity.Id;
            NewLabResult.TestedDiseaseId = contactEntity.DiseaseId;
            
            // Generate FriendlyId based on existing lab results count
            var existingLabResultsCount = await _context.LabResults.CountAsync(lr => lr.CaseId == id);
            NewLabResult.FriendlyId = $"{contactEntity?.FriendlyId}-LAB{existingLabResultsCount + 1:D3}";
            
            NewLabResult.CreatedAt = DateTime.UtcNow;
            
            // Clear navigation properties to avoid tracking issues
            NewLabResult.Case = null;
            NewLabResult.Laboratory = null;
            NewLabResult.OrderingProvider = null;
            NewLabResult.SpecimenType = null;
            NewLabResult.ResultUnits = null;
            NewLabResult.TestedDisease = null;

            // Handle file attachment
            if (LabResultAttachment != null && LabResultAttachment.Length > 0)
            {
                try
                {
                    var storedFile = await _fileStorage.SaveAttachmentAsync(
                        LabResultAttachment,
                        ProtectedFileStorageService.LabResultsCategory,
                        HttpContext.RequestAborted);
                    NewLabResult.AttachmentPath = storedFile.StorageKey;
                    NewLabResult.AttachmentFileName = storedFile.OriginalFileName;
                    NewLabResult.AttachmentSize = storedFile.Length;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(
                        HttpContext,
                        ex,
                        "The attachment could not be accepted. Check its type, content and size before trying again.");
                    return RedirectToPage(new { id });
                }
            }

            try
            {
                _context.LabResults.Add(NewLabResult);
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: id.ToString(),
                    fieldName: "Lab Result Added",
                    oldValue: null,
                    newValue: NewLabResult.AccessionNumber ?? "Lab Result",
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Lab result added successfully.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Unable to add laboratory result to contact {ContactId}", id);
                TempData["ErrorMessage"] = "The laboratory result could not be saved. Check the entered information and try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to add laboratory result to contact {ContactId}", id);
                TempData["ErrorMessage"] = "The laboratory result could not be saved. Please try again.";
            }

            return RedirectToPage(new { id });
        }

        public async Task<JsonResult> OnGetLabResultDetailsAsync(Guid id, Guid labResultId)
        {
            if (!await UserCanViewLabResultsAsync(id))
            {
                return new JsonResult(new { success = false, message = "Access denied" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var labResult = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .Include(lr => lr.Markers).ThenInclude(m => m.Pathogen)
                .Include(lr => lr.Markers).ThenInclude(m => m.TestMethod)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId && lr.CaseId == id);

            if (labResult == null)
            {
                return new JsonResult(new { success = false, message = "Lab result not found" });
            }

            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    id = labResult.Id,
                    friendlyId = labResult.FriendlyId,
                    laboratoryId = labResult.LaboratoryId,
                    laboratoryName = labResult.Laboratory?.Name,
                    orderingProviderId = labResult.OrderingProviderId,
                    orderingProviderName = labResult.OrderingProvider?.Name,
                    accessionNumber = labResult.AccessionNumber,
                    specimenCollectionDate = labResult.SpecimenCollectionDate?.ToString("yyyy-MM-dd"),
                    specimenTypeId = labResult.SpecimenTypeId,
                    specimenTypeName = labResult.SpecimenType?.Name,
                    testedDiseaseId = labResult.TestedDiseaseId,
                    testedDiseaseName = labResult.TestedDisease?.Name,
                    resultDate = labResult.ResultDate?.ToString("yyyy-MM-dd"),
                    resultUnitsId = labResult.ResultUnitsId,
                    resultUnitsName = labResult.ResultUnits?.Name,
                    isAmended = labResult.IsAmended,
                    notes = labResult.Notes,
                    labInterpretation = labResult.LabInterpretation,
                    attachmentPath = labResult.AttachmentPath == null ? null : $"/attachments/lab-results/{labResult.Id}",
                    attachmentFileName = labResult.AttachmentFileName,
                    markers = labResult.Markers.Select(m => new
                    {
                        id = m.Id,
                        pathogenId = m.PathogenId,
                        pathogenName = m.Pathogen?.Name,
                        testMethodId = m.TestMethodId,
                        testMethodName = m.TestMethod?.Name,
                        qualitativeResult = m.QualitativeResultText,
                        quantitativeValue = m.QuantitativeValue,
                        quantitativeUnit = m.QuantitativeUnit,
                        referenceRangeLow = m.ReferenceRangeLow,
                        referenceRangeHigh = m.ReferenceRangeHigh,
                        interpretationFlag = m.InterpretationFlag,
                        loincCode = m.LOINCCode,
                        notes = m.Notes
                    }).ToList(),
                    createdAt = labResult.CreatedAt.ToString("dd MMM yyyy HH:mm"),
                    modifiedAt = labResult.ModifiedAt?.ToString("dd MMM yyyy HH:mm")
                }
            });
        }

        public async Task<JsonResult> OnGetSearchOrganizationsAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new JsonResult(new List<object>());
            }

            var organizations = await _context.Organizations
                .Where(o => o.IsActive && o.Name.Contains(term))
                .OrderBy(o => o.Name)
                .Take(20)
                .Select(o => new { id = o.Id, text = o.Name })
                .ToListAsync();

            return new JsonResult(organizations);
        }

        public async Task<JsonResult> OnGetSearchDiseasesAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                return new JsonResult(new List<object>());
            }

            var diseases = await _context.Diseases
                .Where(d => d.IsActive && d.Name.Contains(term))
                .OrderBy(d => d.Name)
                .Take(20)
                .Select(d => new { id = d.Id, text = d.Name })
                .ToListAsync();

            return new JsonResult(diseases);
        }

        public async Task<JsonResult> OnGetSearchTestResultsAsync(int? testTypeId, string term)
        {
            // LEGACY: TestResults removed - return empty list
            return new JsonResult(new List<object>());
        }

        private async Task LoadLabResultDropdowns()
        {
            LaboratoriesList = new SelectList(
                await _context.Organizations
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            OrganizationsList = new SelectList(
                await _context.Organizations
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            DiseasesList = new SelectList(
                await _context.Diseases
                    .Where(d => d.IsActive)
                    .OrderBy(d => d.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            SpecimenTypesList = new SelectList(
                await _context.SpecimenTypes
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            // LEGACY: TestTypes removed - use Pathogen/Markers system instead  
            TestTypesList = new SelectList(new List<object>()); // Empty list

            // LEGACY: TestResults removed - use Pathogen/Markers system instead
            TestResultsList = new SelectList(new List<object>()); // Empty list

            ResultUnitsList = new SelectList(
                await _context.ResultUnits
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.DisplayOrder)
                    .ThenBy(u => u.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );
        }

        private async Task LoadTaskTemplates()
        {
            if (Contact.DiseaseId.HasValue)
            {
                var templateSources = await _taskService.GetApplicableTaskTemplates(Contact.DiseaseId.Value);
                AvailableTaskTemplates = templateSources
                    .Select(ts => ts.Template)
                    .Where(t => t.IsActive && (t.ApplicableToType == null || t.ApplicableToType == CaseType.Contact))
                    .OrderBy(t => t.Name)
                    .ToList();
            }
        }
    }
}
