using System;
using System.IO.Compression;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Occupations
{
    [Authorize(Policy = "Permission.Occupation.Import")]
    public class UploadModel : PageModel
    {
        private const long MaximumSpreadsheetUncompressedBytes = 100 * 1024 * 1024;
        private const int MaximumSpreadsheetEntries = 100;
        private const double MaximumSpreadsheetCompressionRatio = 100;
        private readonly IOccupationImportService _importService;
        private readonly ILogger<UploadModel> _logger;

        public UploadModel(IOccupationImportService importService, ILogger<UploadModel> logger)
        {
            _importService = importService;
            _logger = logger;
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
                if (!ValidateXlsxArchive(stream))
                {
                    ModelState.AddModelError("UploadFile", "The upload is not a valid Excel workbook.");
                    return Page();
                }

                stream.Position = 0;
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
                _logger.LogWarning(ex, "Occupation import failed for uploaded workbook {FileName}", UploadFile.FileName);
                ModelState.AddModelError(string.Empty, "The workbook could not be processed. Check that it is a valid ANZSCO Excel file and try again.");
            }

            return Page();
        }

        private static bool ValidateXlsxArchive(Stream stream)
        {
            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
                if (archive.Entries.Count == 0 || archive.Entries.Count > MaximumSpreadsheetEntries)
                    return false;

                long totalUncompressedBytes = 0;
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(entry.FullName))
                        return false;

                    totalUncompressedBytes = checked(totalUncompressedBytes + entry.Length);
                    if (totalUncompressedBytes > MaximumSpreadsheetUncompressedBytes ||
                        (entry.Length > 0 && entry.CompressedLength > 0 &&
                         (double)entry.Length / entry.CompressedLength > MaximumSpreadsheetCompressionRatio))
                    {
                        return false;
                    }
                }

                return archive.GetEntry("[Content_Types].xml") != null &&
                       archive.GetEntry("xl/workbook.xml") != null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
        }
    }
}
