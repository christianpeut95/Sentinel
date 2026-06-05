using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sentinel.Services.HL7;

namespace Sentinel.Controllers.Api
{
    [Authorize]
    [ApiController]
    [Route("api/hl7/diagnostics")]
    public class HL7DiagnosticsApiController : ControllerBase
    {
        private readonly HL7DiagnosticService _diagnosticService;

        public HL7DiagnosticsApiController(HL7DiagnosticService diagnosticService)
        {
            _diagnosticService = diagnosticService;
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
    }
}
