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
    public class AddLabResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddLabResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LabResult LabResult { get; set; } = new LabResult();

        [BindProperty]
        public IFormFile? LabResultAttachment { get; set; }

        public SelectList SpecimenTypesList { get; set; }
        public SelectList TestTypesList { get; set; }
        public SelectList ResultUnitsList { get; set; }
        public string CaseId { get; set; }
        public Case Case { get; set; }

        public async Task<IActionResult> OnGetAsync(string caseId)
        {
            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == Guid.Parse(caseId));

            if (Case == null)
            {
                return NotFound();
            }

            // Pre-populate with case disease
            LabResult.TestedDiseaseId = Case.DiseaseId;
            LabResult.CaseId = Case.Id;

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string caseId)
        {
            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == Guid.Parse(caseId));

            if (Case == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return Page();
            }

            // Set case ID and create lab result
            LabResult.CaseId = Case.Id;
            LabResult.CreatedAt = DateTime.UtcNow;
            LabResult.FriendlyId = await GenerateLabResultIdAsync();

            // DIAGNOSTIC LOGGING
            Console.WriteLine("=== ADD LAB RESULT DIAGNOSTIC ===");
            Console.WriteLine($"Case ID: {LabResult.CaseId}");
            Console.WriteLine($"Lab Result ID: {LabResult.Id}");
            Console.WriteLine($"Friendly ID: {LabResult.FriendlyId}");
            Console.WriteLine($"Test Type ID: {LabResult.TestTypeId}");
            Console.WriteLine($"Specimen Type ID: {LabResult.SpecimenTypeId}");
            Console.WriteLine($"Collection Date: {LabResult.SpecimenCollectionDate}");
            Console.WriteLine($"Database: {_context.Database.GetConnectionString()}");
            Console.WriteLine($"DbContext Hash: {_context.GetHashCode()}");

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

                LabResult.AttachmentPath = $"/uploads/lab-results/{uniqueFileName}";
                LabResult.AttachmentFileName = LabResultAttachment.FileName;
            }

            try
            {
                Console.WriteLine("About to add LabResult to context...");
                _context.LabResults.Add(LabResult);
                
                Console.WriteLine("About to call SaveChangesAsync...");
                var changeCount = await _context.SaveChangesAsync();
                Console.WriteLine($"SaveChanges completed. Changes saved: {changeCount}");
                Console.WriteLine($"Lab Result ID after save: {LabResult.Id}");
                
                // Verify it was actually saved
                var verifyResult = await _context.LabResults
                    .Where(lr => lr.Id == LabResult.Id)
                    .FirstOrDefaultAsync();
                Console.WriteLine($"Verification query result: {(verifyResult != null ? "FOUND" : "NOT FOUND")}");
                
                if (verifyResult == null)
                {
                    Console.WriteLine("ERROR: Lab result not found in database after save!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EXCEPTION during save: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
            Console.WriteLine("=== END DIAGNOSTIC ===");

            // Check if in iframe (check query parameter first, then referer)
            var isInIframe = Request.Query.ContainsKey("iframe");
            
            if (!isInIframe)
            {
                var referer = Request.Headers["Referer"].ToString();
                isInIframe = referer.Contains("/Cases/CreateNew");
            }

            if (isInIframe)
            {
                // Post message to parent window with success notification
                return Content(
                    @"<html>
                    <head>
                        <style>
                            body {
                                display: flex;
                                align-items: center;
                                justify-content: center;
                                height: 100vh;
                                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                                background: #f0fdf4;
                                margin: 0;
                            }
                            .success-message {
                                text-align: center;
                                padding: 2rem;
                            }
                            .success-icon {
                                font-size: 4rem;
                                color: #10b981;
                                margin-bottom: 1rem;
                            }
                            .success-text {
                                font-size: 1.25rem;
                                color: #166534;
                                font-weight: 600;
                            }
                        </style>
                    </head>
                    <body>
                        <div class='success-message'>
                            <div class='success-icon'>?</div>
                            <div class='success-text'>Lab Result Saved Successfully!</div>
                            <p style='color: #16a34a; margin-top: 0.5rem;'>This window will close automatically...</p>
                        </div>
                        <script>
                            console.log('Lab result saved, posting message to parent');
                            if (window.parent && window.parent !== window) {
                                console.log('Posting message: labResultSaved');
                                window.parent.postMessage('labResultSaved', '*');
                            }
                            setTimeout(function() {
                                console.log('Attempting to trigger modal close');
                                if (window.parent && window.parent !== window) {
                                    window.parent.postMessage('labResultSaved', '*');
                                }
                            }, 500);
                        </script>
                    </body>
                    </html>",
                    "text/html"
                );
            }

            // Close popup and refresh parent (traditional popup window)
            return Content(
                "<script>if(window.opener) { window.opener.location.reload(); } window.close();</script>",
                "text/html"
            );
        }

        private async Task<string> GenerateLabResultIdAsync()
        {
            var year = DateTime.Now.Year;
            var prefix = $"LAB{year}";

            var lastId = await _context.LabResults
                .Where(lr => lr.FriendlyId.StartsWith(prefix))
                .OrderByDescending(lr => lr.FriendlyId)
                .Select(lr => lr.FriendlyId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastId != null)
            {
                var numberPart = lastId.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D5}";
        }

        private async Task LoadSelectLists()
        {
            SpecimenTypesList = new SelectList(
                await _context.SpecimenTypes.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            TestTypesList = new SelectList(
                await _context.TestTypes.OrderBy(t => t.Name).ToListAsync(),
                "Id", "Name");

            ResultUnitsList = new SelectList(
                await _context.ResultUnits.OrderBy(r => r.Name).ToListAsync(),
                "Id", "Name");
        }
    }
}
