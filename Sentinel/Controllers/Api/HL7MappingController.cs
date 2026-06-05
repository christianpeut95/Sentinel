using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Controllers.API
{
    [ApiController]
    [Route("api/hl7/mapping")]
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class HL7MappingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7FieldMappingService _mappingService;
        private readonly IHL7ParserService _parserService;

        public HL7MappingController(
            ApplicationDbContext context,
            IHL7FieldMappingService mappingService,
            IHL7ParserService parserService)
        {
            _context = context;
            _mappingService = mappingService;
            _parserService = parserService;
        }

        [HttpPost("confirm-field")]
        public async Task<IActionResult> ConfirmField([FromBody] ConfirmFieldRequest request)
        {
            // Find or create the field mapping
            var mapping = await _context.HL7FieldMappings
                .FirstOrDefaultAsync(m => 
                    m.ConfigurationId == request.ConfigurationId && 
                    m.TargetEntity == GetTargetEntity(request.FieldKey) &&
                    m.TargetProperty == GetTargetProperty(request.FieldKey));

            if (mapping == null)
            {
                mapping = new HL7FieldMapping
                {
                    ConfigurationId = request.ConfigurationId,
                    TargetEntity = GetTargetEntity(request.FieldKey),
                    TargetProperty = GetTargetProperty(request.FieldKey),
                    FieldName = GetFriendlyName(request.FieldKey),
                    Priority = 100,
                    CreatedAt = DateTime.UtcNow
                };
                _context.HL7FieldMappings.Add(mapping);
            }

            mapping.FieldPath = request.FieldPath;
            mapping.MappingType = HL7MappingType.DirectCopy;
            mapping.IsActive = true;
            mapping.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("unresolve-field")]
        public async Task<IActionResult> UnresolveField([FromBody] UnresolveFieldRequest request)
        {
            var mapping = await _context.HL7FieldMappings
                .FirstOrDefaultAsync(m => 
                    m.ConfigurationId == request.ConfigurationId && 
                    m.TargetEntity == GetTargetEntity(request.FieldKey) &&
                    m.TargetProperty == GetTargetProperty(request.FieldKey));

            if (mapping != null)
            {
                mapping.IsActive = false;
                mapping.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok(new { success = true });
        }

        [HttpPost("skip-field")]
        public async Task<IActionResult> SkipField([FromBody] SkipFieldRequest request)
        {
            var mapping = await _context.HL7FieldMappings
                .FirstOrDefaultAsync(m => 
                    m.ConfigurationId == request.ConfigurationId && 
                    m.TargetEntity == GetTargetEntity(request.FieldKey) &&
                    m.TargetProperty == GetTargetProperty(request.FieldKey));

            if (mapping == null)
            {
                mapping = new HL7FieldMapping
                {
                    ConfigurationId = request.ConfigurationId,
                    TargetEntity = GetTargetEntity(request.FieldKey),
                    TargetProperty = GetTargetProperty(request.FieldKey),
                    FieldName = GetFriendlyName(request.FieldKey),
                    Priority = 100,
                    CreatedAt = DateTime.UtcNow
                };
                _context.HL7FieldMappings.Add(mapping);
            }

            mapping.IsActive = false;
            mapping.FieldPath = "SKIPPED";
            mapping.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        [HttpPost("test-parse")]
        public async Task<IActionResult> TestParse([FromBody] TestParseRequest request)
        {
            try
            {
                // Parse the HL7 message using the preview method (doesn't save to DB)
                var parseResult = await _parserService.ParseMessagePreviewAsync(request.MessageContent);

                if (!parseResult.IsValid)
                {
                    return BadRequest(new { error = "Invalid HL7 message", errors = parseResult.Errors });
                }

                // Get all mappings for this configuration
                var mappings = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == request.ConfigurationId && m.IsActive)
                    .ToListAsync();

                var results = new List<TestFieldResult>();

                // For each known field, try to extract from the parsed data
                results.Add(new TestFieldResult
                {
                    FieldName = "Patient first name",
                    ExtractedValue = parseResult.PatientData.GetValueOrDefault("FirstName")
                });

                results.Add(new TestFieldResult
                {
                    FieldName = "Patient last name",
                    ExtractedValue = parseResult.PatientData.GetValueOrDefault("LastName")
                });

                results.Add(new TestFieldResult
                {
                    FieldName = "Patient date of birth",
                    ExtractedValue = parseResult.PatientData.GetValueOrDefault("DateOfBirth")
                });

                results.Add(new TestFieldResult
                {
                    FieldName = "Patient street address",
                    ExtractedValue = parseResult.PatientData.GetValueOrDefault("Address")
                });

                // Check result data
                var firstResult = parseResult.ResultData.FirstOrDefault();
                if (firstResult != null)
                {
                    results.Add(new TestFieldResult
                    {
                        FieldName = "Test result",
                        ExtractedValue = firstResult.GetValueOrDefault("Result")
                    });

                    results.Add(new TestFieldResult
                    {
                        FieldName = "Test date",
                        ExtractedValue = firstResult.GetValueOrDefault("TestDate")
                    });

                    results.Add(new TestFieldResult
                    {
                        FieldName = "Specimen type",
                        ExtractedValue = firstResult.GetValueOrDefault("SpecimenType")
                    });
                }

                results.Add(new TestFieldResult
                {
                    FieldName = "Ordering provider",
                    ExtractedValue = parseResult.OrderData.GetValueOrDefault("OrderingProvider")
                });

                return Ok(new { fields = results });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Could not parse message: {ex.Message}" });
            }
        }

        [HttpPost("save-configuration")]
        public async Task<IActionResult> SaveConfiguration([FromBody] SaveConfigurationRequest request)
        {
            var config = await _context.HL7Configurations.FindAsync(request.ConfigurationId);
            if (config == null)
            {
                return NotFound();
            }

            // Validate that all required fields are mapped
            var requiredFields = GetRequiredFieldKeys();
            var mappings = await _context.HL7FieldMappings
                .Where(m => m.ConfigurationId == request.ConfigurationId && m.IsActive)
                .ToListAsync();

            var unmappedRequired = requiredFields.Where(fieldKey =>
            {
                var targetEntity = GetTargetEntity(fieldKey);
                var targetProperty = GetTargetProperty(fieldKey);
                return !mappings.Any(m => 
                    m.TargetEntity == targetEntity && 
                    m.TargetProperty == targetProperty &&
                    !string.IsNullOrEmpty(m.FieldPath) &&
                    m.FieldPath != "SKIPPED");
            }).ToList();

            if (unmappedRequired.Any())
            {
                return BadRequest(new { 
                    error = $"Required fields not mapped: {string.Join(", ", unmappedRequired.Select(GetFriendlyName))}" 
                });
            }

            config.IsActive = true;
            config.ModifiedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // Helper methods to map field keys to database entities/properties
        private string GetTargetEntity(string fieldKey)
        {
            return fieldKey.Split('_')[0] switch
            {
                "patient" => "Patient",
                "test" => "LabResult",
                "specimen" => "LabResult",
                "ordering" => "LabResult",
                _ => "Unknown"
            };
        }

        private string GetTargetProperty(string fieldKey)
        {
            return fieldKey.Split('_').Last() switch
            {
                "firstname" => "FirstName",
                "lastname" => "LastName",
                "dob" => "DateOfBirth",
                "address" => "Address",
                "result" => "Result",
                "date" => "TestDate",
                "type" => "SpecimenType",
                "provider" => "OrderingProvider",
                _ => fieldKey
            };
        }

        private string GetFriendlyName(string fieldKey)
        {
            return fieldKey switch
            {
                "patient_firstname" => "Patient first name",
                "patient_lastname" => "Patient last name",
                "patient_dob" => "Patient date of birth",
                "patient_address" => "Patient street address",
                "test_result" => "Test result",
                "test_date" => "Test date",
                "specimen_type" => "Specimen type",
                "ordering_provider" => "Ordering provider",
                _ => fieldKey
            };
        }

        private List<string> GetAllFieldKeys()
        {
            return new List<string>
            {
                "patient_firstname",
                "patient_lastname",
                "patient_dob",
                "patient_address",
                "test_result",
                "test_date",
                "specimen_type",
                "ordering_provider"
            };
        }

        private List<string> GetRequiredFieldKeys()
        {
            return new List<string>
            {
                "patient_firstname",
                "patient_lastname",
                "patient_dob",
                "test_result",
                "test_date"
            };
        }
    }

    public class ConfirmFieldRequest
    {
        public Guid ConfigurationId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string FieldPath { get; set; } = string.Empty;
    }

    public class UnresolveFieldRequest
    {
        public Guid ConfigurationId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
    }

    public class SkipFieldRequest
    {
        public Guid ConfigurationId { get; set; }
        public string FieldKey { get; set; } = string.Empty;
    }

    public class TestParseRequest
    {
        public Guid ConfigurationId { get; set; }
        public string MessageContent { get; set; } = string.Empty;
    }

    public class SaveConfigurationRequest
    {
        public Guid ConfigurationId { get; set; }
    }

    public class TestFieldResult
    {
        public string FieldName { get; set; } = string.Empty;
        public string? ExtractedValue { get; set; }
    }
}
