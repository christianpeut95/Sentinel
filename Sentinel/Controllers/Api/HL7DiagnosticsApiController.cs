using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services.HL7;

namespace Sentinel.Controllers.Api
{
    [Authorize(Policy = "Permission.HL7.View")]
    [ApiController]
    [Route("api/hl7/diagnostics")]
    [EnableRateLimiting("workflow-api")] // 100 per minute - diagnostic operations
    public class HL7DiagnosticsApiController : ControllerBase
    {
        private readonly HL7DiagnosticService _diagnosticService;
        private readonly ApplicationDbContext _context;

        public HL7DiagnosticsApiController(
            HL7DiagnosticService diagnosticService,
            ApplicationDbContext context)
        {
            _diagnosticService = diagnosticService;
            _context = context;
        }

        /// <summary>
        /// Diagnose why a case was not created for a lab result
        /// GET: api/hl7/diagnostics/lab-result/{labResultId}
        /// </summary>
        [HttpGet("lab-result/{labResultId}")]
        public async Task<IActionResult> DiagnoseLabResult(Guid labResultId)
        {
            var report = await _diagnosticService.DiagnoseLabResultAsync(labResultId);
            return Ok(report);
        }

        /// <summary>
        /// Get all active pathogen to disease mappings
        /// GET: api/hl7/diagnostics/pathogen-mappings
        /// </summary>
        [HttpGet("pathogen-mappings")]
        public async Task<IActionResult> GetPathogenMappings()
        {
            var mappings = await _diagnosticService.GetAllPathogenMappingsAsync();
            return Ok(mappings);
        }

        /// <summary>
        /// Returns the detail required by the HL7 Testing dialog.
        /// Keeping this as an API endpoint prevents a failed Razor Page named
        /// handler from returning a full HTML page to the JavaScript client.
        /// GET: api/hl7/diagnostics/messages/{messageId}
        /// </summary>
        [HttpGet("messages/{messageId:guid}")]
        public async Task<IActionResult> GetMessageDetails(Guid messageId, CancellationToken cancellationToken)
        {
            var message = await _context.HL7Messages
                .AsNoTracking()
                .Include(m => m.Patient)
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                        .ThenInclude(marker => marker.Pathogen)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message is null)
            {
                return NotFound();
            }

            return Ok(new
            {
                message.MessageControlId,
                message.MessageType,
                message.Status,
                RawContent = message.RawMessage,
                HasProcessingError = !string.IsNullOrEmpty(message.ErrorMessage),
                Patient = message.Patient is null ? null : new
                {
                    message.Patient.FriendlyId,
                    FirstName = message.Patient.GivenName,
                    LastName = message.Patient.FamilyName,
                    message.Patient.DateOfBirth
                },
                LabResult = message.LabResult is null ? null : new
                {
                    message.LabResult.FriendlyId,
                    message.LabResult.SpecimenCollectionDate,
                    MarkerCount = message.LabResult.Markers.Count,
                    Markers = message.LabResult.Markers.Select(marker => new
                    {
                        marker.TestCode,
                        TestName = marker.Pathogen?.Name ?? marker.LOINCCode ?? marker.TestCode,
                        QualitativeResult = marker.QualitativeResultText,
                        marker.QuantitativeValue,
                        Units = marker.QuantitativeUnit,
                        marker.InterpretationFlag
                    }).ToList()
                }
            });
        }
    }
}
