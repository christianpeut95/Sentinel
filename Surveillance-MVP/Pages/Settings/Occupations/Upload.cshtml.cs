using System;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Surveillance_MVP.Services;

namespace Surveillance_MVP.Pages.Settings.Occupations
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class UploadModel : PageModel
    {
        private readonly IOccupationImportService _importService;

        public UploadModel(IOccupationImportService importService)
        {
            _importService = importService;
        }

        [BindProperty]
        public IFormFile? UploadFile { get; set; }

        public ImportResult? ImportResult { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadFile == null || UploadFile.Length == 0)
            {
                ModelState.AddModelError("UploadFile", "Please select a file to upload.");
                return Page();
            }

            // Validate file extension
            var extension = Path.GetExtension(UploadFile.FileName).ToLowerInvariant();
            if (extension != ".xlsx")
            {
                ModelState.AddModelError("UploadFile", "Please upload an Excel file (.xlsx).");
                return Page();
            }

            // Validate file size (max 10MB)
            if (UploadFile.Length > 10 * 1024 * 1024)
            {
                ModelState.AddModelError("UploadFile", "File size must be less than 10MB.");
                return Page();
            }

            try
            {
                using var stream = UploadFile.OpenReadStream();
                ImportResult = await _importService.ImportFromExcelAsync(stream);

                if (ImportResult.Success)
                {
                    TempData["SuccessMessage"] = $"Successfully imported {ImportResult.RecordsImported} occupation(s). {ImportResult.RecordsSkipped} record(s) skipped.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Import failed. See details below.";
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error processing file: {ex.Message}");
            }

            return Page();
        }
    }
}
