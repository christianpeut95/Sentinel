using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Cases
{
    [Authorize]
    public class EditLabResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditLabResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LabResult LabResult { get; set; }

        [BindProperty]
        public IFormFile? LabResultAttachment { get; set; }

        public SelectList SpecimenTypesList { get; set; }
        public SelectList TestTypesList { get; set; }
        public SelectList ResultUnitsList { get; set; }
        public string CaseId { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid labResultId, string caseId)
        {
            CaseId = caseId;

            LabResult = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.TestType)
                .Include(lr => lr.TestResult)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId);

            if (LabResult == null)
            {
                return NotFound();
            }

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid labResultId, string caseId)
        {
            CaseId = caseId;

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            var labResultToUpdate = await _context.LabResults.FindAsync(labResultId);
            if (labResultToUpdate == null)
            {
                return NotFound();
            }

            // Update properties
            labResultToUpdate.LaboratoryId = LabResult.LaboratoryId;
            labResultToUpdate.OrderingProviderId = LabResult.OrderingProviderId;
            labResultToUpdate.AccessionNumber = LabResult.AccessionNumber;
            labResultToUpdate.TestedDiseaseId = LabResult.TestedDiseaseId;
            labResultToUpdate.SpecimenCollectionDate = LabResult.SpecimenCollectionDate;
            labResultToUpdate.SpecimenTypeId = LabResult.SpecimenTypeId;
            labResultToUpdate.TestTypeId = LabResult.TestTypeId;
            labResultToUpdate.TestResultId = LabResult.TestResultId;
            labResultToUpdate.ResultDate = LabResult.ResultDate;
            labResultToUpdate.QuantitativeResult = LabResult.QuantitativeResult;
            labResultToUpdate.ResultUnitsId = LabResult.ResultUnitsId;
            labResultToUpdate.IsAmended = LabResult.IsAmended;
            labResultToUpdate.Notes = LabResult.Notes;
            labResultToUpdate.LabInterpretation = LabResult.LabInterpretation;
            labResultToUpdate.ModifiedAt = DateTime.UtcNow;

            // Handle attachment upload
            if (LabResultAttachment != null && LabResultAttachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-results");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{LabResultAttachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await LabResultAttachment.CopyToAsync(fileStream);
                }

                labResultToUpdate.AttachmentPath = $"/uploads/lab-results/{uniqueFileName}";
                labResultToUpdate.AttachmentFileName = LabResultAttachment.FileName;
            }

            await _context.SaveChangesAsync();

            // Redirect back to view page in popup
            return RedirectToPage("/Cases/ViewLabResult", new { labResultId = labResultId, caseId = caseId, saved = true });
        }

        private async Task LoadSelectLists()
        {
            SpecimenTypesList = new SelectList(
                await _context.SpecimenTypes.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name", LabResult?.SpecimenTypeId);

            TestTypesList = new SelectList(
                await _context.TestTypes.OrderBy(t => t.Name).ToListAsync(),
                "Id", "Name", LabResult?.TestTypeId);

            ResultUnitsList = new SelectList(
                await _context.ResultUnits.OrderBy(r => r.Name).ToListAsync(),
                "Id", "Name", LabResult?.ResultUnitsId);
        }
    }
}
