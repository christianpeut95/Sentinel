using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Settings
{
    [Authorize(Policy = "Permission.Settings.ManageCustomFields")] // Using existing admin permission
    public class DeletedRecordsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DeletedRecordsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Patient> DeletedPatients { get; set; } = new();
        public List<Case> DeletedCases { get; set; } = new();
        public List<LabResult> DeletedLabResults { get; set; } = new();
        public List<Note> DeletedNotes { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrEmpty(Filter) || Filter == "patients")
            {
                DeletedPatients = await _context.OnlyDeleted<Patient>()
                    .OrderByDescending(p => p.DeletedAt)
                    .Take(100)
                    .ToListAsync();
            }

            if (string.IsNullOrEmpty(Filter) || Filter == "cases")
            {
                DeletedCases = await _context.OnlyDeleted<Case>()
                    .Include(c => c.Patient)
                    .Include(c => c.Disease)
                    .OrderByDescending(c => c.DeletedAt)
                    .Take(100)
                    .ToListAsync();
            }

            if (string.IsNullOrEmpty(Filter) || Filter == "labresults")
            {
                DeletedLabResults = await _context.OnlyDeleted<LabResult>()
                    .Include(lr => lr.Case)
                        .ThenInclude(c => c.Patient)
                    .OrderByDescending(lr => lr.DeletedAt)
                    .Take(100)
                    .ToListAsync();
            }

            if (string.IsNullOrEmpty(Filter) || Filter == "notes")
            {
                DeletedNotes = await _context.OnlyDeleted<Note>()
                    .Include(n => n.Patient)
                    .Include(n => n.Case)
                    .OrderByDescending(n => n.DeletedAt)
                    .Take(100)
                    .ToListAsync();
            }
        }

        public async Task<IActionResult> OnPostRestorePatientAsync(Guid id)
        {
            var patient = await _context.OnlyDeleted<Patient>().FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToPage();
            }

            await _context.RestoreAsync(patient);
            TempData["SuccessMessage"] = $"Patient '{patient.GivenName} {patient.FamilyName}' has been restored.";
            return RedirectToPage(new { Filter = "patients" });
        }

        public async Task<IActionResult> OnPostRestoreCaseAsync(Guid id)
        {
            var caseEntity = await _context.OnlyDeleted<Case>()
                .Include(c => c.Patient)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (caseEntity == null)
            {
                TempData["ErrorMessage"] = "Case not found.";
                return RedirectToPage();
            }

            await _context.RestoreAsync(caseEntity);
            TempData["SuccessMessage"] = $"Case '{caseEntity.FriendlyId}' has been restored.";
            return RedirectToPage(new { Filter = "cases" });
        }

        public async Task<IActionResult> OnPostRestoreLabResultAsync(Guid id)
        {
            var labResult = await _context.OnlyDeleted<LabResult>().FirstOrDefaultAsync(lr => lr.Id == id);
            if (labResult == null)
            {
                TempData["ErrorMessage"] = "Lab result not found.";
                return RedirectToPage();
            }

            await _context.RestoreAsync(labResult);
            TempData["SuccessMessage"] = $"Lab result '{labResult.FriendlyId}' has been restored.";
            return RedirectToPage(new { Filter = "labresults" });
        }

        public async Task<IActionResult> OnPostRestoreNoteAsync(Guid id)
        {
            var note = await _context.OnlyDeleted<Note>().FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                TempData["ErrorMessage"] = "Note not found.";
                return RedirectToPage();
            }

            await _context.RestoreAsync(note);
            TempData["SuccessMessage"] = "Note has been restored.";
            return RedirectToPage(new { Filter = "notes" });
        }

        public async Task<IActionResult> OnPostPermanentDeletePatientAsync(Guid id)
        {
            var patient = await _context.OnlyDeleted<Patient>().FirstOrDefaultAsync(p => p.Id == id);
            if (patient == null)
            {
                TempData["ErrorMessage"] = "Patient not found.";
                return RedirectToPage();
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Patient '{patient.GivenName} {patient.FamilyName}' has been permanently deleted.";
            return RedirectToPage(new { Filter = "patients" });
        }

        public async Task<IActionResult> OnPostPermanentDeleteCaseAsync(Guid id)
        {
            var caseEntity = await _context.OnlyDeleted<Case>().FirstOrDefaultAsync(c => c.Id == id);
            if (caseEntity == null)
            {
                TempData["ErrorMessage"] = "Case not found.";
                return RedirectToPage();
            }

            _context.Cases.Remove(caseEntity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Case '{caseEntity.FriendlyId}' has been permanently deleted.";
            return RedirectToPage(new { Filter = "cases" });
        }

        public async Task<IActionResult> OnPostPermanentDeleteLabResultAsync(Guid id)
        {
            var labResult = await _context.OnlyDeleted<LabResult>().FirstOrDefaultAsync(lr => lr.Id == id);
            if (labResult == null)
            {
                TempData["ErrorMessage"] = "Lab result not found.";
                return RedirectToPage();
            }

            _context.LabResults.Remove(labResult);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Lab result '{labResult.FriendlyId}' has been permanently deleted.";
            return RedirectToPage(new { Filter = "labresults" });
        }

        public async Task<IActionResult> OnPostPermanentDeleteNoteAsync(Guid id)
        {
            var note = await _context.OnlyDeleted<Note>().FirstOrDefaultAsync(n => n.Id == id);
            if (note == null)
            {
                TempData["ErrorMessage"] = "Note not found.";
                return RedirectToPage();
            }

            _context.Notes.Remove(note);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Note has been permanently deleted.";
            return RedirectToPage(new { Filter = "notes" });
        }
    }
}
