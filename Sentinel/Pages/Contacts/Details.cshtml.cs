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

        public DetailsModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            CustomFieldService customFieldService, 
            IPermissionService permissionService, 
            IDiseaseAccessService diseaseAccessService, 
            ITaskService taskService)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _permissionService = permissionService;
            _diseaseAccessService = diseaseAccessService;
            _taskService = taskService;
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

            // Load notes
            Notes = await _context.Notes
                .Where(n => n.CaseId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Load lab results
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

            // Load dropdown lists for lab results
            await LoadLabResultDropdowns();

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

        // ========================================================================
        // POST HANDLERS
        // ========================================================================

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
            var contactEntity = await _context.Cases.FindAsync(id);
            NewLabResult.FriendlyId = $"{contactEntity?.FriendlyId}-LAB{existingLabResultsCount + 1:D3}";
            
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
