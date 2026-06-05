using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Settings.HL7.Messages
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FileMonitorService _fileMonitor;

        public DetailsModel(ApplicationDbContext context, IHL7FileMonitorService fileMonitor)
        {
            _context = context;
            _fileMonitor = fileMonitor;
        }

        public HL7Message Message { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var message = await _context.HL7Messages
                .Include(m => m.Configuration)
                .Include(m => m.Patient)
                .Include(m => m.Case)
                    .ThenInclude(c => c.Disease)
                .Include(m => m.Segments.OrderBy(s => s.SequenceNumber))
                .Include(m => m.DuplicateOfMessage)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (message == null)
            {
                return NotFound();
            }

            Message = message;
            return Page();
        }

        public async Task<IActionResult> OnPostReprocessAsync(Guid id)
        {
            var message = await _context.HL7Messages.FindAsync(id);
            if (message == null)
            {
                return NotFound();
            }

            // Check if file still exists
            if (!string.IsNullOrWhiteSpace(message.FilePath) && System.IO.File.Exists(message.FilePath))
            {
                // Reprocess the file
                var result = await _fileMonitor.ProcessFileAsync(
                    message.FilePath, 
                    message.ConfigurationId
                );

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Message reprocessed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"Error reprocessing message: {string.Join(", ", result.Errors)}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Original file not found. Cannot reprocess.";
            }

            return RedirectToPage(new { id });
        }
    }
}
