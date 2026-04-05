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

        public DetailsModel(
            ApplicationDbContext context, 
            IAuditService auditService, 
            IPatientCustomFieldService customFieldService, 
            IPermissionService permissionService,
            IDiseaseAccessService diseaseAccessService,
            IJurisdictionService jurisdictionService)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
            _permissionService = permissionService;
            _diseaseAccessService = diseaseAccessService;
            _jurisdictionService = jurisdictionService;
        }

        public Patient Patient { get; set; } = default!;
        public List<CustomFieldDefinition> CustomFields { get; set; } = new();
        public Dictionary<string, List<CustomFieldDefinition>> FieldsByCategory { get; set; } = new();
        public Dictionary<int, string?> CustomFieldValues { get; set; } = new();
        public List<Note> PatientNotes { get; set; } = new List<Note>();
        public List<Note> CaseCommunicationNotes { get; set; } = new List<Note>();
        public List<Case> Cases { get; set; } = new List<Case>();
        
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

                // Load active jurisdiction types for display
                ActiveJurisdictionTypes = await _jurisdictionService.GetActiveJurisdictionTypesAsync();

                CustomFields = await _customFieldService.GetDetailsFieldsAsync();
                FieldsByCategory = CustomFields.GroupBy(f => f.Category).ToDictionary(g => g.Key, g => g.ToList());
                CustomFieldValues = await _customFieldService.GetPatientFieldDisplayValuesAsync(patient.Id);

                // Load notes directly linked to this patient
                PatientNotes = await _context.Notes
                    .Where(n => n.PatientId == id)
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

                // Get accessible disease IDs for filtering cases
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
                var accessibleDiseaseIds = await _diseaseAccessService.GetAccessibleDiseaseIdsAsync(userId);

                // Load cases for this patient (filtered by disease access)
                Cases = await _context.Cases
                    .Include(c => c.Disease)
                    .Include(c => c.ConfirmationStatus)
                    .Where(c => c.PatientId == id && 
                               (c.DiseaseId == null || accessibleDiseaseIds.Contains(c.DiseaseId.Value)))
                    .OrderByDescending(c => c.DateOfNotification)
                    .ToListAsync();

                // Load communication notes (Phone Call, Email, SMS) from related cases
                var communicationTypes = new[] { "Phone Call", "Email", "SMS" };
                CaseCommunicationNotes = await _context.Notes
                    .Include(n => n.Case)
                    .Where(n => n.Case!.PatientId == id && communicationTypes.Contains(n.Type))
                    .OrderByDescending(n => n.CreatedAt)
                    .ToListAsync();

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
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please fill in all required fields.";
                return RedirectToPage(new { id });
            }

            NewNote.Id = Guid.NewGuid();
            NewNote.PatientId = id;
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
            // Check if user has Patient.Delete permission
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _permissionService.HasPermissionAsync(userId, PermissionModule.Patient, PermissionAction.Delete))
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
            // Check if user has Case.Delete permission (since these are case communications)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!await _permissionService.HasPermissionAsync(userId, PermissionModule.Case, PermissionAction.Delete))
            {
                TempData["ErrorMessage"] = "You do not have permission to delete case communications.";
                return RedirectToPage(new { id });
            }

            var note = await _context.Notes.FindAsync(noteId);
            if (note == null)
            {
                TempData["ErrorMessage"] = "Note not found.";
                return RedirectToPage(new { id });
            }

            // Verify this note belongs to a case related to this patient
            if (note.CaseId == null)
            {
                TempData["ErrorMessage"] = "This note is not a case communication.";
                return RedirectToPage(new { id });
            }

            var caseEntity = await _context.Cases.FindAsync(note.CaseId.Value);
            if (caseEntity == null || caseEntity.PatientId != id)
            {
                TempData["ErrorMessage"] = "This note does not belong to a case for this patient.";
                return RedirectToPage(new { id });
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
    }
}
