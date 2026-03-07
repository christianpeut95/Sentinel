using System;
using System.Collections.Generic;
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
    [Authorize(Policy = "Permission.Case.View")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly CustomFieldService _customFieldService;
        private readonly IPermissionService _permissionService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly ITaskService _taskService;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            CustomFieldService customFieldService, 
            IPermissionService permissionService, 
            IDiseaseAccessService diseaseAccessService, 
            ITaskService taskService, 
            IJurisdictionService jurisdictionService,
            ILogger<DetailsModel> logger)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _permissionService = permissionService;
            _diseaseAccessService = diseaseAccessService;
            _taskService = taskService;
            _jurisdictionService = jurisdictionService;
            _logger = logger;
        }

        public Case Case { get; set; } = default!;
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

        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();

        // Note properties - NOT bound automatically to avoid validation conflicts
        public Note NewNote { get; set; } = default!;
        public IFormFile? Attachment { get; set; }

        // LabResult properties - NOT bound automatically to avoid validation conflicts  
        public LabResult NewLabResult { get; set; } = default!;
        public IFormFile? LabResultAttachment { get; set; }



        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var caseEntity = await _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
                .Include(c => c.Patient)
                    .ThenInclude(p => p.Gender)
                .Include(c => c.Patient)
                    .ThenInclude(p => p.CountryOfBirth)
                .Include(c => c.ConfirmationStatus)
                .Include(c => c.Disease)
                .Include(c => c.Hospital)
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

            // Redirect to appropriate page based on case type
            if (caseEntity.Type == CaseType.Contact)
            {
                return RedirectToPage("/Contacts/Details", new { id = caseEntity.Id });
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

            // Load notes for this case
            Notes = await _context.Notes
                .Where(n => n.CaseId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Load lab results for this case
            LabResults = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.TestType)
                .Include(lr => lr.TestResult)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .Where(lr => lr.CaseId == id)
                .OrderByDescending(lr => lr.ResultDate)
                .ThenByDescending(lr => lr.SpecimenCollectionDate)
                .ToListAsync();

            // Load custom fields if disease is selected
            if (Case.DiseaseId.HasValue)
            {
                CustomFields = await _customFieldService.GetEffectiveFieldsForDiseaseAsync(Case.DiseaseId.Value);
                CustomFieldValues = await _customFieldService.GetCaseCustomFieldValuesAsync(Case.Id);
            }

            // Load symptoms
            CaseSymptoms = await _context.CaseSymptoms
                .Include(cs => cs.Symptom)
                .Where(cs => cs.CaseId == id)
                .OrderBy(cs => cs.OnsetDate ?? DateTime.MaxValue)
                .ThenBy(cs => cs.Symptom!.Name)
                .ToListAsync();

            // Load ACQUISITIONS - Where THIS case was exposed (upstream)
            Acquisitions = await _context.ExposureEvents
                .Include(e => e.Event).ThenInclude(e => e!.Location)
                .Include(e => e.Location)
                .Include(e => e.SourceCase).ThenInclude(c => c!.Patient)
                .Include(e => e.ContactClassification)
                .Where(e => e.ExposedCaseId == id)
                .Where(e => e.SourceCaseId == null || 
                           (e.SourceCase != null && e.SourceCase.Patient != null))
                .OrderByDescending(e => e.ExposureStartDate)
                .ToListAsync();

            // Load TRANSMISSIONS - Who THIS case exposed (downstream)
            Transmissions = await _context.ExposureEvents
                .Include(e => e.Event).ThenInclude(e => e!.Location)
                .Include(e => e.Location)
                .Include(e => e.ExposedCase).ThenInclude(c => c!.Patient)
                .Include(e => e.ContactClassification)
                .Where(e => e.SourceCaseId == id)
                .OrderByDescending(e => e.ExposureStartDate)
                .ToListAsync();


            // Load tasks
            Tasks = await _taskService.GetTasksForCase(id.Value);

            // Load active jurisdiction types for display
            ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

            // Load dropdown lists for lab results
            await LoadLabResultDropdowns();

            // Load task templates for the case's disease
            await LoadTaskTemplates();

            await _auditService.LogViewAsync(
                entityType: "Case",
                entityId: Case.Id.ToString(),
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers["User-Agent"].ToString()
            );

            return Page();
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

            TestTypesList = new SelectList(
                await _context.TestTypes
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.DisplayOrder)
                    .ThenBy(t => t.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            TestResultsList = new SelectList(
                await _context.TestResults
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.DisplayOrder)
                    .ThenBy(r => r.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

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

        public async Task<IActionResult> OnPostAddNoteAsync(Guid id)
        {
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
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "notes");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{Attachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Attachment.CopyToAsync(fileStream);
                }

                NewNote.AttachmentPath = $"/uploads/notes/{uniqueFileName}";
                NewNote.AttachmentFileName = Attachment.FileName;
                NewNote.AttachmentSize = Attachment.Length;
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

        public async Task<IActionResult> OnPostAddLabResultAsync(Guid id)
        {
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
            NewLabResult.CaseId = id;
            
            // Generate FriendlyId based on existing lab results count
            var existingLabResultsCount = await _context.LabResults.CountAsync(lr => lr.CaseId == id);
            var caseEntity = await _context.Cases.FindAsync(id);
            NewLabResult.FriendlyId = $"{caseEntity?.FriendlyId}-LAB{existingLabResultsCount + 1:D3}";
            
            NewLabResult.CreatedAt = DateTime.UtcNow;
            
            // Clear navigation properties to avoid tracking issues
            NewLabResult.Case = null;
            NewLabResult.Laboratory = null;
            NewLabResult.OrderingProvider = null;
            NewLabResult.SpecimenType = null;
            NewLabResult.TestType = null;
            NewLabResult.TestResult = null;
            NewLabResult.ResultUnits = null;
            NewLabResult.TestedDisease = null;

            // Handle file attachment
            if (LabResultAttachment != null && LabResultAttachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labresults");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{LabResultAttachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await LabResultAttachment.CopyToAsync(fileStream);
                }

                NewLabResult.AttachmentPath = $"/uploads/labresults/{uniqueFileName}";
                NewLabResult.AttachmentFileName = LabResultAttachment.FileName;
                NewLabResult.AttachmentSize = LabResultAttachment.Length;
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
                TempData["ErrorMessage"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to save lab result: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostUpdateLabResultAsync(Guid id, Guid labResultId)
        {
            // Manually bind the LabResult from form data
            NewLabResult = new LabResult();
            await TryUpdateModelAsync(NewLabResult, "NewLabResult");
            
            // Manually bind the file attachment
            LabResultAttachment = Request.Form.Files.GetFile("LabResultAttachment");
            
            // Remove properties from ModelState validation
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

            var existingLabResult = await _context.LabResults.FindAsync(labResultId);
            if (existingLabResult == null)
            {
                TempData["ErrorMessage"] = "Lab result not found.";
                return RedirectToPage(new { id });
            }

            // Update properties
            existingLabResult.LaboratoryId = NewLabResult.LaboratoryId;
            existingLabResult.OrderingProviderId = NewLabResult.OrderingProviderId;
            existingLabResult.AccessionNumber = NewLabResult.AccessionNumber;
            existingLabResult.SpecimenCollectionDate = NewLabResult.SpecimenCollectionDate;
            existingLabResult.SpecimenTypeId = NewLabResult.SpecimenTypeId;
            existingLabResult.TestTypeId = NewLabResult.TestTypeId;
            existingLabResult.TestedDiseaseId = NewLabResult.TestedDiseaseId;
            existingLabResult.TestResultId = NewLabResult.TestResultId;
            existingLabResult.ResultDate = NewLabResult.ResultDate;
            existingLabResult.QuantitativeResult = NewLabResult.QuantitativeResult;
            existingLabResult.ResultUnitsId = NewLabResult.ResultUnitsId;
            existingLabResult.IsAmended = NewLabResult.IsAmended;
            existingLabResult.Notes = NewLabResult.Notes;
            existingLabResult.LabInterpretation = NewLabResult.LabInterpretation;
            existingLabResult.ModifiedAt = DateTime.UtcNow;

            // Handle file attachment
            if (LabResultAttachment != null && LabResultAttachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "labresults");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{LabResultAttachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await LabResultAttachment.CopyToAsync(fileStream);
                }

                existingLabResult.AttachmentPath = $"/uploads/labresults/{uniqueFileName}";
                existingLabResult.AttachmentFileName = LabResultAttachment.FileName;
                existingLabResult.AttachmentSize = LabResultAttachment.Length;
            }

            try
            {
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: id.ToString(),
                    fieldName: "Lab Result Updated",
                    oldValue: null,
                    newValue: existingLabResult.AccessionNumber ?? "Lab Result",
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Lab result updated successfully.";
            }
            catch (DbUpdateException dbEx)
            {
                TempData["ErrorMessage"] = $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Failed to update lab result: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        public async Task<JsonResult> OnGetLabResultDetailsAsync(Guid labResultId)
        {
            var labResult = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.TestType)
                .Include(lr => lr.TestResult)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId);

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
                    testTypeId = labResult.TestTypeId,
                    testTypeName = labResult.TestType?.Name,
                    testedDiseaseId = labResult.TestedDiseaseId,
                    testedDiseaseName = labResult.TestedDisease?.Name,
                    testResultId = labResult.TestResultId,
                    testResultName = labResult.TestResult?.Name,
                    resultDate = labResult.ResultDate?.ToString("yyyy-MM-dd"),
                    quantitativeResult = labResult.QuantitativeResult,
                    resultUnitsId = labResult.ResultUnitsId,
                    resultUnitsName = labResult.ResultUnits?.Name,
                    isAmended = labResult.IsAmended,
                    notes = labResult.Notes,
                    labInterpretation = labResult.LabInterpretation,
                    attachmentPath = labResult.AttachmentPath,
                    attachmentFileName = labResult.AttachmentFileName,
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
            var query = _context.TestResults.Where(tr => tr.IsActive);

            // Filter by test type if provided
            if (testTypeId.HasValue)
            {
                query = query.Where(tr => tr.TestTypeId == testTypeId.Value);
            }

            // Filter by search term if provided
            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(tr => tr.Name.Contains(term));
            }

            var testResults = await query
                .OrderBy(tr => tr.DisplayOrder)
                .ThenBy(tr => tr.Name)
                .Take(20)
                .Select(tr => new { id = tr.Id, text = tr.Name })
                .ToListAsync();

            return new JsonResult(testResults);
        }

        public async Task<IActionResult> OnPostDeleteNoteAsync(Guid id, Guid noteId)
        {
            // Check if user has Case.Delete permission
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!await _permissionService.HasPermissionAsync(userId, PermissionModule.Case, PermissionAction.Delete))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete notes.";
                return RedirectToPage(new { id });
            }

            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
            {
                TempData["ErrorMessage"] = "Note not found.";
                return RedirectToPage(new { id });
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

        public async Task<IActionResult> OnPostDeleteLabResultAsync(Guid id, Guid labResultId)
        {
            var labResult = await _context.LabResults.FindAsync(labResultId);
            if (labResult == null)
            {
                TempData["ErrorMessage"] = "Lab result not found.";
                return RedirectToPage(new { id });
            }

            await _context.SoftDeleteAsync(labResult);

            await _auditService.LogChangeAsync(
                entityType: "Case",
                entityId: id.ToString(),
                fieldName: "Lab Result Deleted",
                oldValue: labResult.AccessionNumber ?? labResult.FriendlyId,
                newValue: null,
                userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Lab result deleted successfully.";
            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostDeleteExposureAsync(Guid id, Guid exposureId)
        {
            try
            {
                var exposure = await _context.ExposureEvents.FindAsync(exposureId);
                
                if (exposure == null || exposure.ExposedCaseId != id)
                {
                    TempData["ErrorMessage"] = "Exposure not found.";
                    return RedirectToPage(new { id });
                }

                await _context.SoftDeleteAsync(exposure);

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: id.ToString(),
                    fieldName: "Exposure Deleted",
                    oldValue: exposure.ExposureType.ToString(),
                    newValue: null,
                    userId: User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Exposure deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting exposure: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostAddExposureAsync(
            Guid CaseId,
            string ExposureDirection,
            ExposureType ExposureType,
            ExposureStatus ExposureStatus,
            DateTime ExposureStartDate,
            DateTime? ExposureEndDate,
            Guid? EventId,
            Guid? LocationId,
            Guid? SourceCaseId,
            string? CountryCode,
            string? Description,
            bool IsReportingExposure)
        {
            try
            {
                var exposure = new ExposureEvent
                {
                    Id = Guid.NewGuid(),
                    ExposureType = ExposureType,
                    ExposureStatus = ExposureStatus,
                    ExposureStartDate = ExposureStartDate,
                    ExposureEndDate = ExposureEndDate,
                    EventId = EventId,
                    LocationId = LocationId,
                    CountryCode = CountryCode,
                    Description = Description,
                    IsReportingExposure = IsReportingExposure,
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };

                // Handle Exposure Direction
                if (ExposureDirection == "Transmission")
                {
                    // Current case is the SOURCE (exposed others)
                    exposure.SourceCaseId = CaseId;
                    
                    // For Contact type, SourceCaseId from form is actually the ExposedCaseId
                    if (ExposureType == ExposureType.Contact && SourceCaseId.HasValue)
                    {
                        exposure.ExposedCaseId = SourceCaseId.Value;
                        exposure.SourceCaseId = CaseId; // Override with current case
                    }
                    // For Location/Event transmissions, ExposedCaseId will be set when contacts are created
                    else if (ExposureType == ExposureType.Location || ExposureType == ExposureType.Event)
                    {
                        // Temporary placeholder - contacts will be created later
                        exposure.ExposedCaseId = CaseId; // Self-reference until contacts added
                    }
                    else
                    {
                        exposure.ExposedCaseId = CaseId; // Default fallback
                    }
                }
                else
                {
                    // Default: Acquisition - current case was exposed
                    exposure.ExposedCaseId = CaseId;
                    // SourceCaseId stays as provided from form
                    exposure.SourceCaseId = SourceCaseId;
                }

                _context.ExposureEvents.Add(exposure);
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "Case",
                    entityId: CaseId.ToString(),
                    fieldName: "Exposure Added",
                    oldValue: null,
                    newValue: $"{ExposureDirection} - {ExposureType}",
                    userId: User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"{ExposureDirection} exposure added successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding exposure to case {CaseId}", CaseId);
                TempData["ErrorMessage"] = $"Error adding exposure: {ex.Message}";
            }

            return RedirectToPage(new { id = CaseId });
        }

        // ========================================================================
        // TASK MANAGEMENT HANDLERS
        // ========================================================================


        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostCompleteTaskAsync(Guid id, Guid taskId, string? completionNotes)
        {
            try
            {
                var task = await _context.CaseTasks.FindAsync(taskId);
                
                if (task == null || task.CaseId != id)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToPage(new { id });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                task.Status = CaseTaskStatus.Completed;
                task.CompletedAt = DateTime.UtcNow;
                task.CompletedByUserId = userId;
                task.CompletionNotes = completionNotes;
                task.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: taskId.ToString(),
                    fieldName: "Status",
                    oldValue: "Pending/InProgress",
                    newValue: "Completed",
                    userId: userId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Task marked as completed.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error completing task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostUpdateTaskAsync(Guid id, Guid taskId, CaseTaskStatus status, TaskPriority priority, DateTime? dueDate, string? assignedToUserId)
        {
            try
            {
                var task = await _context.CaseTasks.FindAsync(taskId);
                
                if (task == null || task.CaseId != id)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToPage(new { id });
                }

                var changes = new List<string>();

                if (task.Status != status)
                {
                    changes.Add($"Status: {task.Status} ? {status}");
                    task.Status = status;
                }

                if (task.Priority != priority)
                {
                    changes.Add($"Priority: {task.Priority} ? {priority}");
                    task.Priority = priority;
                }

                if (task.DueDate != dueDate)
                {
                    changes.Add($"Due Date: {task.DueDate?.ToString("dd MMM yyyy") ?? "None"} ? {dueDate?.ToString("dd MMM yyyy") ?? "None"}");
                    task.DueDate = dueDate;
                }

                if (task.AssignedToUserId != assignedToUserId)
                {
                    changes.Add($"Assigned To: {task.AssignedToUserId ?? "None"} ? {assignedToUserId ?? "None"}");
                    task.AssignedToUserId = assignedToUserId;
                }

                task.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                if (changes.Any())
                {
                    await _auditService.LogChangeAsync(
                        entityType: "CaseTask",
                        entityId: taskId.ToString(),
                        fieldName: "Task Updated",
                        oldValue: null,
                        newValue: string.Join(", ", changes),
                        userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                    );
                }

                TempData["SuccessMessage"] = "Task updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostCancelTaskAsync(Guid id, Guid taskId, string? cancellationReason)
        {
            try
            {
                var task = await _context.CaseTasks.FindAsync(taskId);
                
                if (task == null || task.CaseId != id)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToPage(new { id });
                }

                task.Status = CaseTaskStatus.Cancelled;
                task.CompletionNotes = $"Cancelled: {cancellationReason}";
                task.ModifiedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: taskId.ToString(),
                    fieldName: "Status",
                    oldValue: task.Status.ToString(),
                    newValue: "Cancelled",
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Task cancelled.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error cancelling task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostDeleteTaskAsync(Guid id, Guid taskId)
        {
            try
            {
                var task = await _context.CaseTasks
                    .Include(t => t.TaskTemplate)
                    .FirstOrDefaultAsync(t => t.Id == taskId);
                
                if (task == null || task.CaseId != id)
                {
                    TempData["ErrorMessage"] = "Task not found.";
                    return RedirectToPage(new { id });
                }

                var taskName = task.TaskTemplate?.Name ?? "Unknown Task";

                _context.CaseTasks.Remove(task);
                await _context.SaveChangesAsync();

                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: taskId.ToString(),
                    fieldName: "Task Deleted",
                    oldValue: taskName,
                    newValue: null,
                    userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = "Task deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        // ========================================================================
        // TASK MANAGEMENT - MANUAL ADD
        // ========================================================================

        private async Task LoadTaskTemplates()
        {
            // Load task types for dropdown
            TaskTypesList = new SelectList(
                await _context.TaskTypes
                    .Where(tt => tt.IsActive)
                    .OrderBy(tt => tt.Name)
                    .ToListAsync(),
                "Id",
                "Name"
            );

            // Load survey templates - show only ONE active version per survey family
            // Group by ParentSurveyTemplateId OR by Id if no parent (base version)
            var allActiveSurveys = await _context.SurveyTemplates
                .Where(st => st.VersionStatus == SurveyVersionStatus.Active && st.IsActive)
                .OrderBy(st => st.PublishedAt ?? st.CreatedAt) // Most recently published first
                .ToListAsync();

            // Group by survey family and get the LATEST version of each
            var uniqueSurveys = allActiveSurveys
                .GroupBy(st => st.ParentSurveyTemplateId ?? st.Id)
                .Select(g => g.OrderByDescending(s => s.VersionNumber).First()) // Get LATEST version
                .OrderBy(st => st.Category)
                .ThenBy(st => st.Name)
                .Select(st => new
                {
                    st.Id,
                    DisplayName = st.Category != null 
                        ? $"{st.Name} ({st.Category}) v{st.VersionNumber}" 
                        : $"{st.Name} v{st.VersionNumber}"
                })
                .ToList();

            SurveyTemplatesList = new SelectList(uniqueSurveys, "Id", "DisplayName");

            // Load task templates for this disease, filtered to Case-applicable templates only
            if (Case.DiseaseId.HasValue)
            {
                var templateSources = await _taskService.GetApplicableTaskTemplates(Case.DiseaseId.Value);
                AvailableTaskTemplates = templateSources
                    .Select(ts => ts.Template)
                    .Where(t => t.IsActive && (t.ApplicableToType == null || t.ApplicableToType == CaseType.Case))
                    .OrderBy(t => t.Name)
                    .ToList();
            }
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostAddTaskFromTemplateAsync(Guid id, Guid taskTemplateId)
        {
            try
            {
                // ?? STEP 1: Load Case
                var caseEntity = await _context.Cases.FindAsync(id);
                if (caseEntity == null)
                {
                    TempData["ErrorMessage"] = "Case not found.";
                    return RedirectToPage(new { id });
                }

                // ?? STEP 2: Load Template WITH TaskType (crucial for IsInterviewTask inheritance)
                var template = await _context.TaskTemplates
                    .AsNoTracking()
                    .Include(t => t.TaskType)
                    .FirstOrDefaultAsync(t => t.Id == taskTemplateId);

                if (template == null)
                {
                    TempData["ErrorMessage"] = "Task template not found.";
                    return RedirectToPage(new { id });
                }

                // ?? STEP 3: Get current user
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // ?? STEP 4: REBUILD IsInterviewTask Logic
                // Priority: TaskType.IsInterviewTask > Template.IsInterviewTask
                // Use TaskType as source of truth since it defines the task category
                bool isInterviewTask;
                
                if (template.TaskType != null)
                {
                    // TaskType is loaded - use it as primary source
                    isInterviewTask = template.TaskType.IsInterviewTask;
                    _logger?.LogInformation(
                        $"[TASK CREATION] Template: {template.Name}, " +
                        $"TaskType: {template.TaskType.Name}, " +
                        $"TaskType.IsInterviewTask: {template.TaskType.IsInterviewTask}, " +
                        $"Template.IsInterviewTask: {template.IsInterviewTask}, " +
                        $"FINAL IsInterviewTask: {isInterviewTask}");
                }
                else
                {
                    // Fallback to template flag (shouldn't happen with Include)
                    isInterviewTask = template.IsInterviewTask;
                    _logger?.LogWarning(
                        $"[TASK CREATION] TaskType not loaded for template {template.Name}. " +
                        $"Using template flag: {isInterviewTask}");
                }

                // ?? STEP 5: Calculate Due Date
                DateTime? dueDate = CalculateDueDate(template, caseEntity);

                // ?? STEP 6: Create NEW Task Instance
                var newTask = new CaseTask
                {
                    Id = Guid.NewGuid(),
                    CaseId = id,
                    TaskTemplateId = taskTemplateId,
                    Title = template.Name,
                    Description = template.Description,
                    TaskTypeId = template.TaskTypeId,
                    Priority = template.DefaultPriority,
                    AssignmentType = template.AssignmentType,
                    AssignedToUserId = userId,
                    Status = CaseTaskStatus.Pending,
                    IsInterviewTask = isInterviewTask, // ? Set from TaskType
                    CreatedAt = DateTime.UtcNow,
                    DueDate = dueDate
                };

                // ?? STEP 7: Log Before Save
                _logger?.LogInformation(
                    $"[TASK CREATION] About to save task: " +
                    $"Id={newTask.Id}, Title={newTask.Title}, " +
                    $"IsInterviewTask={newTask.IsInterviewTask}, " +
                    $"TaskTypeId={newTask.TaskTypeId}, " +
                    $"AssignedToUserId={newTask.AssignedToUserId}");

                // ?? STEP 8: Save to Database
                _context.CaseTasks.Add(newTask);
                await _context.SaveChangesAsync();

                // ?? STEP 9: Verify Save
                var savedTask = await _context.CaseTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == newTask.Id);
                
                if (savedTask != null)
                {
                    _logger?.LogInformation(
                        $"[TASK CREATION] ? Task saved successfully: " +
                        $"Id={savedTask.Id}, IsInterviewTask={savedTask.IsInterviewTask}");
                }

                // ?? STEP 10: Audit Log
                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: newTask.Id.ToString(),
                    fieldName: "Task Created from Template",
                    oldValue: null,
                    newValue: $"{template.Name} (IsInterviewTask={isInterviewTask})",
                    userId: userId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = $"Task '{template.Name}' added successfully. IsInterviewTask={isInterviewTask}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[TASK CREATION] ? Error creating task");
                TempData["ErrorMessage"] = $"Error adding task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostAddTaskManualAsync(
            Guid id, 
            string title, 
            string? description, 
            Guid taskTypeId,
            int priority,
            DateTime? dueDate,
            Guid? surveyTemplateId,
            string? customSurveyJson)
        {
            try
            {
                var caseEntity = await _context.Cases.FindAsync(id);
                if (caseEntity == null)
                {
                    TempData["ErrorMessage"] = "Case not found.";
                    return RedirectToPage(new { id });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    TempData["ErrorMessage"] = "Task title is required.";
                    return RedirectToPage(new { id });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Create a temporary task template if survey is specified
                Guid? tempTaskTemplateId = null;
                if (surveyTemplateId.HasValue || !string.IsNullOrWhiteSpace(customSurveyJson))
                {
                    var tempTemplate = new TaskTemplate
                    {
                        Id = Guid.NewGuid(),
                        Name = $"[Manual] {title}",
                        Description = description,
                        TaskTypeId = taskTypeId,
                        DefaultPriority = (TaskPriority)priority,
                        AssignmentType = TaskAssignmentType.Anyone,
                        IsActive = false, // Mark as inactive (temporary)
                        CreatedAt = DateTime.UtcNow
                    };

                    // Link to survey template if selected
                    if (surveyTemplateId.HasValue)
                    {
                        tempTemplate.SurveyTemplateId = surveyTemplateId.Value;
                    }
                    // Or use custom survey JSON
                    else if (!string.IsNullOrWhiteSpace(customSurveyJson))
                    {
                        tempTemplate.SurveyDefinitionJson = customSurveyJson;
                    }

                    _context.TaskTemplates.Add(tempTemplate);
                    tempTaskTemplateId = tempTemplate.Id;
                }

                // Get TaskType to check if it's an interview task
                var taskType = await _context.TaskTypes.FindAsync(taskTypeId);
                
                // Create manual task
                var task = new CaseTask
                {
                    Id = Guid.NewGuid(),
                    CaseId = id,
                    TaskTemplateId = tempTaskTemplateId, // Link to temp template if survey specified
                    Title = title,
                    Description = description,
                    TaskTypeId = taskTypeId,
                    Priority = (TaskPriority)priority,
                    AssignmentType = TaskAssignmentType.Anyone,
                    AssignedToUserId = userId,
                    Status = CaseTaskStatus.Pending,
                    IsInterviewTask = taskType?.IsInterviewTask ?? false,
                    CreatedAt = DateTime.UtcNow,
                    DueDate = dueDate
                };

                _context.CaseTasks.Add(task);
                await _context.SaveChangesAsync();

                var successMessage = $"Task '{title}' added successfully.";
                if (surveyTemplateId.HasValue || !string.IsNullOrWhiteSpace(customSurveyJson))
                {
                    successMessage += " Survey configured.";
                }

                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: task.Id.ToString(),
                    fieldName: "Manual Task Created",
                    oldValue: null,
                    newValue: title,
                    userId: userId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                TempData["SuccessMessage"] = successMessage;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error adding task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        /// <summary>
        /// ? CLEAN, SIMPLE TASK CREATION HANDLER - REBUILT FROM SCRATCH
        /// This WILL save IsInterviewTask correctly!
        /// </summary>
        [Authorize(Policy = "Permission.Case.Edit")]
        public async Task<IActionResult> OnPostCreateTaskAsync(
            Guid id,
            string Title,
            Guid TaskTypeId,
            int Priority,
            DateTime? DueDate,
            string? Description,
            bool IncludeSurvey,
            Guid? SurveyTemplateId)
        {
            _logger?.LogInformation("? CreateTask handler called - Title: {Title}, TaskTypeId: {TaskTypeId}", 
                Title, TaskTypeId);

            try
            {
                // 1. Get the case
                var caseEntity = await _context.Cases.FindAsync(id);
                if (caseEntity == null)
                {
                    TempData["ErrorMessage"] = "Case not found.";
                    return RedirectToPage(new { id });
                }

                // 2. Get the TaskType (WITH IsInterviewTask!)
                var taskType = await _context.TaskTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(tt => tt.Id == TaskTypeId);

                if (taskType == null)
                {
                    TempData["ErrorMessage"] = "Task type not found.";
                    return RedirectToPage(new { id });
                }

                _logger?.LogInformation("? TaskType loaded: {Name}, IsInterviewTask: {IsInterviewTask}", 
                    taskType.Name, taskType.IsInterviewTask);

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 3. Create TaskTemplate if survey is included
                Guid? templateId = null;
                if (IncludeSurvey && SurveyTemplateId.HasValue)
                {
                    var tempTemplate = new TaskTemplate
                    {
                        Id = Guid.NewGuid(),
                        Name = $"[Manual] {Title}",
                        Description = Description,
                        TaskTypeId = TaskTypeId,
                        DefaultPriority = (TaskPriority)Priority,
                        AssignmentType = TaskAssignmentType.Anyone,
                        IsActive = false, // Temporary template
                        SurveyTemplateId = SurveyTemplateId.Value,
                        IsInterviewTask = taskType.IsInterviewTask, // Inherit from TaskType!
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.TaskTemplates.Add(tempTemplate);
                    await _context.SaveChangesAsync();
                    templateId = tempTemplate.Id;

                    _logger?.LogInformation("? Temp template created: {Id}, IsInterviewTask: {IsInterviewTask}", 
                        templateId, tempTemplate.IsInterviewTask);
                }

                // 4. Create the actual CaseTask
                var newTask = new CaseTask
                {
                    Id = Guid.NewGuid(),
                    CaseId = id,
                    Title = Title,
                    Description = Description,
                    TaskTypeId = TaskTypeId,
                    Priority = (TaskPriority)Priority,
                    DueDate = DueDate,
                    Status = CaseTaskStatus.Pending,
                    AssignedToUserId = userId,
                    TaskTemplateId = templateId,
                    // ?? THE CRITICAL LINE - Set IsInterviewTask from TaskType!
                    IsInterviewTask = taskType.IsInterviewTask,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CaseTasks.Add(newTask);
                await _context.SaveChangesAsync();

                _logger?.LogInformation("??? TASK SAVED! Id: {Id}, IsInterviewTask: {IsInterviewTask}, TaskType: {TaskType}", 
                    newTask.Id, newTask.IsInterviewTask, taskType.Name);

                // 5. Verify it saved correctly
                var verifyTask = await _context.CaseTasks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == newTask.Id);

                if (verifyTask != null)
                {
                    _logger?.LogInformation("?? VERIFICATION: Task {Id} has IsInterviewTask = {IsInterviewTask}", 
                        verifyTask.Id, verifyTask.IsInterviewTask);
                }

                // 6. Audit log
                await _auditService.LogChangeAsync(
                    entityType: "CaseTask",
                    entityId: newTask.Id.ToString(),
                    fieldName: "Task Created (Manual)",
                    oldValue: null,
                    newValue: $"{Title} (IsInterviewTask={newTask.IsInterviewTask}, TaskType={taskType.Name})",
                    userId: userId,
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
                );

                var queueMessage = newTask.IsInterviewTask ? " ?? This task will appear in the Interview Queue." : "";
                TempData["SuccessMessage"] = $"Task '{Title}' created successfully!{queueMessage}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "? Error creating task");
                TempData["ErrorMessage"] = $"Error creating task: {ex.Message}";
            }

            return RedirectToPage(new { id });
        }

        private DateTime? CalculateDueDate(TaskTemplate template, Case caseEntity)
        {
            if (!template.DueDaysFromOnset.HasValue && !template.DueDaysFromNotification.HasValue)
                return null;

            DateTime? baseDate = null;

            if (template.DueCalculationMethod == TaskDueCalculationMethod.FromSymptomOnset && caseEntity.DateOfOnset.HasValue)
            {
                baseDate = caseEntity.DateOfOnset.Value;
                if (template.DueDaysFromOnset.HasValue)
                    return baseDate.Value.AddDays(template.DueDaysFromOnset.Value);
            }
            else if (template.DueCalculationMethod == TaskDueCalculationMethod.FromNotificationDate && caseEntity.DateOfNotification.HasValue)
            {
                baseDate = caseEntity.DateOfNotification.Value;
                if (template.DueDaysFromNotification.HasValue)
                    return baseDate.Value.AddDays(template.DueDaysFromNotification.Value);
            }
            else if (template.DueCalculationMethod == TaskDueCalculationMethod.FromTaskCreation)
            {
                baseDate = DateTime.UtcNow;
                if (template.DueDaysFromOnset.HasValue)
                    return baseDate.Value.AddDays(template.DueDaysFromOnset.Value);
            }

            return null;
        }
    }
}



