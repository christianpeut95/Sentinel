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
    [Authorize(Policy = "Permission.HL7.View")]
    public class TestingModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FileMonitorService _fileMonitor;
        private readonly ILogger<TestingModel> _logger;
        private readonly IAuthorizationService _authorizationService;

        public TestingModel(
            ApplicationDbContext context,
            IHL7FileMonitorService fileMonitor,
            ILogger<TestingModel> logger,
            IAuthorizationService authorizationService)
        {
            _context = context;
            _fileMonitor = fileMonitor;
            _logger = logger;
            _authorizationService = authorizationService;
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
                    HasProcessingError = !string.IsNullOrEmpty(m.ErrorMessage)
                })
                .ToListAsync();

            MonitoringStatus = _fileMonitor.GetMonitoringStatus();
        }

        public async Task<IActionResult> OnPostReprocessAsync(Guid messageId)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "Permission.HL7.Process")).Succeeded)
            {
                return Forbid();
            }

            try
            {
                _logger.LogInformation("User requested reprocessing of message {MessageId}", messageId);
                var result = await _fileMonitor.ReprocessMessageAsync(messageId);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Message reprocessed successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Message reprocessing completed with errors. Review the application logs using the message control ID for technical details.";
                    if (result.Warnings.Any())
                    {
                        TempData["WarningMessage"] = "Message reprocessing completed with warnings. Review the application logs using the message control ID for technical details.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reprocessing message {MessageId}", messageId);
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearAllAsync()
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "Permission.HL7.Process")).Succeeded)
            {
                return Forbid();
            }

            try
            {
                _logger.LogWarning("User requested clearing all test data");
                var deletedCount = await _fileMonitor.ClearTestDataAsync();
                TempData["SuccessMessage"] = $"✅ Cleared {deletedCount} HL7 messages and associated data";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing test data");
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteMessageAsync(Guid messageId)
        {
            if (!(await _authorizationService.AuthorizeAsync(User, "Permission.HL7.Process")).Succeeded)
            {
                return Forbid();
            }

            try
            {
                _logger.LogInformation("User requested deletion of message {MessageId}", messageId);
                await _fileMonitor.DeleteMessageAsync(messageId);
                TempData["SuccessMessage"] = "✅ Message and associated data deleted";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                TempData["ErrorMessage"] = Sentinel.Services.UserFacingError.Create(HttpContext, ex);
            }

            return RedirectToPage();
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
            public bool HasProcessingError { get; set; }
        }
    }
}
