using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.HL7;
using Sentinel.Services.HL7;

namespace Sentinel.Pages.Settings.HL7
{
    [Authorize]
    public class TestingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FileMonitorService _fileMonitor;
        private readonly ILogger<TestingModel> _logger;

        public TestingModel(
            ApplicationDbContext context,
            IHL7FileMonitorService fileMonitor,
            ILogger<TestingModel> logger)
        {
            _context = context;
            _fileMonitor = fileMonitor;
            _logger = logger;
        }

        public List<HL7MessageViewModel> Messages { get; set; } = new();
        public MonitoringStatus? MonitoringStatus { get; set; }

        public async Task OnGetAsync()
        {
            Messages = await _context.HL7Messages
                .OrderByDescending(m => m.ReceivedAt)
                .Take(100)
                .Select(m => new HL7MessageViewModel
                {
                    Id = m.Id,
                    MessageControlId = m.MessageControlId,
                    MessageType = m.MessageType,
                    Status = m.Status,
                    ReceivedAt = m.ReceivedAt,
                    ProcessedAt = m.ProcessedAt,
                    PatientId = m.PatientId,
                    PatientName = m.Patient != null ? $"{m.Patient.GivenName} {m.Patient.FamilyName}" : null,
                    HasLabResult = m.LabResultId != null,
                    LabResultId = m.LabResultId,
                    HasCase = m.LabResult != null && m.LabResult.CaseId != null,
                    CaseId = m.LabResult != null ? m.LabResult.CaseId : null,
                    ErrorMessage = m.ErrorMessage,
                    ProcessingNotes = m.ProcessingNotes
                })
                .ToListAsync();

            MonitoringStatus = _fileMonitor.GetMonitoringStatus();
        }

        public async Task<IActionResult> OnPostReprocessAsync(Guid messageId)
        {
            try
            {
                _logger.LogInformation("User requested reprocessing of message {MessageId}", messageId);
                var result = await _fileMonitor.ReprocessMessageAsync(messageId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = $"✅ Message reprocessed successfully!\n" +
                        $"Patient: {result.PatientId}\n" +
                        $"Lab Result: {result.LabResultId}\n" +
                        $"Cases Created: {string.Join(", ", result.CasesCreated)}\n" +
                        $"Cases Linked: {string.Join(", ", result.CasesLinked)}";
                }
                else
                {
                    TempData["ErrorMessage"] = $"❌ Reprocessing completed with errors:\n{string.Join("\n", result.Errors)}";
                    if (result.Warnings.Any())
                    {
                        TempData["WarningMessage"] = $"⚠️ Warnings:\n{string.Join("\n", result.Warnings)}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reprocessing message {MessageId}", messageId);
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAllAsync()
        {
            try
            {
                _logger.LogWarning("User requested clearing all test data");
                var deletedCount = await _fileMonitor.ClearTestDataAsync();
                TempData["SuccessMessage"] = $"✅ Cleared {deletedCount} HL7 messages and associated data";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing test data");
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteMessageAsync(Guid messageId)
        {
            try
            {
                _logger.LogInformation("User requested deletion of message {MessageId}", messageId);
                await _fileMonitor.DeleteMessageAsync(messageId);
                TempData["SuccessMessage"] = "✅ Message and associated data deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                TempData["ErrorMessage"] = $"❌ Error: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetViewDetailsAsync(Guid messageId)
        {
            var message = await _context.HL7Messages
                .Include(m => m.Patient)
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
            {
                return NotFound();
            }

            return new JsonResult(new
            {
                message.MessageControlId,
                message.MessageType,
                message.Status,
                RawContent = message.RawMessage,
                message.ErrorMessage,
                message.ProcessingNotes,
                Patient = message.Patient != null ? new
                {
                    message.Patient.FriendlyId,
                    FirstName = message.Patient.GivenName,
                    LastName = message.Patient.FamilyName,
                    message.Patient.DateOfBirth
                } : null,
                LabResult = message.LabResult != null ? new
                {
                    message.LabResult.FriendlyId,
                    message.LabResult.SpecimenCollectionDate,
                    MarkerCount = message.LabResult.Markers?.Count ?? 0,
                    Markers = message.LabResult.Markers?.Select(m => new
                    {
                        m.TestCode,
                        TestName = m.Pathogen?.Name ?? m.LOINCCode ?? m.TestCode,
                        m.QualitativeResultText,
                        m.QuantitativeValue,
                        Units = m.QuantitativeUnit,
                        m.InterpretationFlag
                    })
                } : null
            });
        }

        public class HL7MessageViewModel
        {
            public Guid Id { get; set; }
            public string MessageControlId { get; set; } = string.Empty;
            public string? MessageType { get; set; }
            public HL7ProcessingStatus Status { get; set; }
            public DateTime ReceivedAt { get; set; }
            public DateTime? ProcessedAt { get; set; }
            public Guid? PatientId { get; set; }
            public string? PatientName { get; set; }
            public bool HasLabResult { get; set; }
            public Guid? LabResultId { get; set; }
            public bool HasCase { get; set; }
            public Guid? CaseId { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ProcessingNotes { get; set; }
        }
    }
}
