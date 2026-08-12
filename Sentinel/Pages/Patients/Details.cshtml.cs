using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Patients
{
    [Authorize(Policy = "Permission.Patient.View")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IPatientCustomFieldService _customFieldService;
        private readonly IPermissionService _permissionService;
        private readonly IDiseaseAccessService _diseaseAccessService;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IProtectedFileStorageService _fileStorage;
        private readonly ICaseAccessService _caseAccessService;

        public DetailsModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            IPatientCustomFieldService customFieldService, 
            IPermissionService permissionService,
            IDiseaseAccessService diseaseAccessService,
            IJurisdictionService jurisdictionService,
            IProtectedFileStorageService fileStorage,
            ICaseAccessService caseAccessService)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _permissionService = permissionService;
            _diseaseAccessService = diseaseAccessService;
            _jurisdictionService = jurisdictionService;
            _fileStorage = fileStorage;
            _caseAccessService = caseAccessService;
        }

        public Patient Patient { get; set; } = default!;
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();
        public Dictionary<int, string?> CustomFieldValues { get; set; } = new();
        public List<Note> PatientNotes { get; set; } = new List<Note>();
        public List<Note> CaseCommunicationNotes { get; set; } = new List<Note>();
        public List<Case> Cases { get; set; } = new List<Case>();
        public bool CanEditPatient { get; private set; }
        public bool CanDeletePatient { get; private set; }
        public bool CanMergePatient { get; private set; }
        public bool CanViewAuditHistory { get; private set; }
        public bool CanViewCases { get; private set; }
        public bool CanCreateCase { get; private set; }
        public bool CanDeleteCases { get; private set; }
        
        // Jurisdiction properties
        public List<JurisdictionType> ActiveJurisdictionTypes { get; set; } = new();

        [BindProperty]
        public Note NewNote { get; set; } = default!;

        [BindProperty]
        public IFormFile? Attachment { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Eagerly load lookup navigation properties so their .Name is available in the view
            var patient = await _context.Patients
                .Include(p => p.CountryOfBirth)
                .Include(p => p.State)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Ancestry)
                .Include(p => p.AtsiStatus)
                .Include(p => p.SexAtBirth)
                .Include(p => p.Gender)
                .Include(p => p.Occupation)
                .Include(p => p.CreatedByUser)
                .Include(p => p.Jurisdiction1).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction2).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction3).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction4).ThenInclude(j => j!.JurisdictionType)
                .Include(p => p.Jurisdiction5).ThenInclude(j => j!.JurisdictionType)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient is not null)
            {
                Patient = patient;
                await LoadPagePermissionsAsync();

                // Load active jurisdiction types for display
                ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

                CustomFields = await _customFieldService.GetDetailsFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                CustomFieldValues = await _customFieldService.GetPatientFieldDisplayValuesAsync(patient.Id);

                // Load notes directly linked to this patient
                PatientNotes = await _context.Notes
                    .Where(n => n.PatientId == id && n.CaseId == null)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                if (CanViewCases)
                {
                    // Get accessible disease IDs for filtering cases.
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                    var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

                    // Load cases for this patient only when the user can view cases.
                    Cases = await _context.Cases
                        .Include(c => c.Disease)
                        .Include(c => c.ConfirmationStatus)
                        .Where(c => c.PatientId == id &&
                                   (c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value)))
                        .OrderByDescending(c => c.DateOfNotification)
                        .ToListAsync();

                    // Do not expose communications from a case outside the user's
                    // disease-access scope through the patient screen.
                    var communicationTypes = new[] { "Phone Call", "Email", "SMS" };
                    CaseCommunicationNotes = await _context.Notes
                        .Include(n => n.Case)
                        .Where(n => n.Case != null &&
                                    n.Case.PatientId == id &&
                                    communicationTypes.Contains(n.Type) &&
                                    (n.Case.DiseaseId == null ||
                                     accessibleDiseaseIds.Contains(n.Case.DiseaseId.Value)))
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();
                }

                // Log the view action
                var viewUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                await _auditService.LogViewAsync("Patient", patient.Id.ToString(), viewUserId, ipAddress, userAgent);

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAddNoteAsync(Guid id)
        {
            if (!await UserCanEditPatientAsync())
            {
                return Forbid();
            }

            if (!await _context.Patients.AnyAsync(p => p.Id == id))
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToPage(new { id });
            }

            NewNote.Id = Guid.NewGuid();
            NewNote.PatientId = id;
            NewNote.CaseId = null;
            NewNote.OutbreakId = null;
            NewNote.Patient = null;
            NewNote.Case = null;
            NewNote.Outbreak = null;
            NewNote.CreatedBy = User.Identity?.Name ?? "Unknown";
            NewNote.CreatedAt = DateTime.UtcNow;

            // Handle file attachment
            if (Attachment != null && Attachment.Length > 0)
            {
                var storedFile = await _fileStorage.SaveAttachmentAsync(
                    Attachment,
                    ProtectedFileStorageService.NotesCategory,
                    HttpContext.RequestAborted);
                NewNote.AttachmentPath = storedFile.StorageKey;
                NewNote.AttachmentFileName = storedFile.OriginalFileName;
                NewNote.AttachmentSize = storedFile.Length;
            }

            _context.Notes.Add(NewNote);
            await _context.SaveChangesAsync();

            await _auditService.LogChangeAsync(
                entityType: "Patient",
                entityId: id.ToString(),
                fieldName: "Note Added",
                oldValue: null,
                newValue: NewNote.Subject ?? "Note",
                userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Note added successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteNoteAsync(Guid id, Guid noteId)
        {
            if (!await UserCanDeletePatientAsync())
            {
                return Forbid();
            }

            var note = await _context.Notes
                .FirstOrDefaultAsync(n => n.Id == noteId && n.PatientId == id && n.CaseId == null);
            if (note == null)
            {
                return NotFound();
            }

            await _context.SoftDeleteAsync(note);

            await _auditService.LogChangeAsync(
                entityType: "Patient",
                entityId: id.ToString(),
                fieldName: "Note Deleted",
                oldValue: note.Subject ?? "Note",
                newValue: null,
                userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Note deleted successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDeleteCaseNoteAsync(Guid id, Guid noteId)
        {
            if (!await UserCanDeleteCaseAsync())
            {
                return Forbid();
            }

            var note = await _context.Notes
                .Include(n => n.Case)
                .FirstOrDefaultAsync(n => n.Id == noteId && n.CaseId != null && n.Case!.PatientId == id);
            if (note == null)
            {
                return NotFound();
            }

            if (!await _caseAccessService.CanAccessCaseAsync(note.CaseId!.Value))
            {
                return NotFound();
            }

            await _context.SoftDeleteAsync(note);

            await _auditService.LogChangeAsync(
                entityType: "Case",
                entityId: note.CaseId.ToString(),
                fieldName: "Case Communication Note Deleted",
                oldValue: note.Subject ?? "Note",
                newValue: null,
                userId: User.FindFirstValue(ClaimTypes.NameIdentifier),
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );

            TempData["SuccessMessage"] = "Case communication deleted successfully.";
            return RedirectToPage(new { id });
        }

        private async Task LoadPagePermissionsAsync()
        {
            CanEditPatient = await UserCanEditPatientAsync();
            CanDeletePatient = await UserCanDeletePatientAsync();
            CanMergePatient = await UserHasPermissionAsync(PermissionModule.Patient, PermissionAction.Merge);
            CanViewAuditHistory = await UserHasPermissionAsync(PermissionModule.Audit, PermissionAction.View);
            CanViewCases = await UserHasPermissionAsync(PermissionModule.Case, PermissionAction.View);
            CanCreateCase = await UserHasPermissionAsync(PermissionModule.Case, PermissionAction.Create);
            CanDeleteCases = await UserCanDeleteCaseAsync();
        }

        private Task<bool> UserCanEditPatientAsync() =>
            UserHasPermissionAsync(PermissionModule.Patient, PermissionAction.Edit);

        private Task<bool> UserCanDeletePatientAsync() =>
            UserHasPermissionAsync(PermissionModule.Patient, PermissionAction.Delete);

        private async Task<bool> UserCanDeleteCaseAsync() =>
            await UserHasPermissionAsync(PermissionModule.Case, PermissionAction.View) &&
            await UserHasPermissionAsync(PermissionModule.Case, PermissionAction.Delete);

        private async Task<bool> UserHasPermissionAsync(PermissionModule module, PermissionAction action)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrWhiteSpace(userId) &&
                   await _permissionService.HasPermissionAsync(userId, module, action);
        }
    }
}
