using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.HL7.Configure")]
    public class SelectLabModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SelectLabModel> _logger;

        public SelectLabModel(ApplicationDbContext context, ILogger<SelectLabModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<LabConfigurationCard> Configurations { get; set; } = new();

        public async Task OnGetAsync()
        {
            var configs = await _context.HL7Configurations
                .Include(c => c.ConfigurationDiseases)
                .OrderBy(c => c.Priority)
                .ThenBy(c => c.ConfigurationName)
                .ToListAsync();

            foreach (var config in configs)
            {
                // First, get ALL mappings (not just active ones) to diagnose the issue
                var allMappings = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == config.Id)
                    .ToListAsync();

                _logger.LogInformation("Config {ConfigId} ({ConfigName}): Found {TotalMappings} total mappings in database",
                    config.Id, config.ConfigurationName, allMappings.Count);

                foreach (var mapping in allMappings)
                {
                    _logger.LogInformation("  - ALL Mapping: {Entity}.{Property} -> {FieldPath} (IsActive={IsActive}, FieldName={FieldName})",
                        mapping.TargetEntity, mapping.TargetProperty, mapping.FieldPath, mapping.IsActive, mapping.FieldName);
                }

                var mappings = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == config.Id && m.IsActive)
                    .ToListAsync();

                _logger.LogInformation("Config {ConfigId} ({ConfigName}): Found {MappingCount} active mappings, IsActive={IsActive}",
                    config.Id, config.ConfigurationName, mappings.Count, config.IsActive);

                foreach (var mapping in mappings)
                {
                    _logger.LogInformation("  - ACTIVE Mapping: {Entity}.{Property} -> {FieldPath} (IsActive={IsActive})",
                        mapping.TargetEntity, mapping.TargetProperty, mapping.FieldPath, mapping.IsActive);
                }

                var requiredFieldCount = GetRequiredFieldKeys().Count;
                var mappedRequiredCount = mappings.Count(m => 
                    !string.IsNullOrEmpty(m.FieldPath) && 
                    m.FieldPath != "SKIPPED" &&
                    IsRequiredField(m.TargetEntity, m.TargetProperty));

                _logger.LogInformation("Config {ConfigId}: {MappedCount} of {RequiredCount} required fields mapped",
                    config.Id, mappedRequiredCount, requiredFieldCount);

                string status;
                string statusMessage;

                if (mappedRequiredCount == requiredFieldCount && config.IsActive)
                {
                    status = "Active";
                    statusMessage = $"All required fields mapped. Processing messages.";
                }
                else if (mappedRequiredCount > 0)
                {
                    status = "NeedsAttention";
                    statusMessage = $"{mappedRequiredCount} of {requiredFieldCount} required fields mapped. Finish setup to start processing.";
                }
                else
                {
                    status = "NotConfigured";
                    statusMessage = "Not yet configured. Start by uploading a sample message.";
                }

                _logger.LogInformation("Config {ConfigId} final status: {Status} - {Message}",
                    config.Id, status, statusMessage);

                Configurations.Add(new LabConfigurationCard
                {
                    Id = config.Id,
                    ConfigurationName = config.ConfigurationName,
                    Description = config.SendingFacility ?? "No facility specified",
                    Status = status,
                    StatusMessage = statusMessage,
                    MappedFieldCount = mappings.Count(m => !string.IsNullOrEmpty(m.FieldPath) && m.FieldPath != "SKIPPED")
                });
            }
        }

        private List<string> GetRequiredFieldKeys()
        {
            return new List<string>
            {
                "patient_firstname",
                "patient_lastname",
                "patient_dob",
                "test_type",
                "test_result",
                "test_date"
            };
        }

        private bool IsRequiredField(string targetEntity, string targetProperty)
        {
            var requiredFields = new Dictionary<string, List<string>>
            {
                { "Patient", new List<string> { "FirstName", "LastName", "DateOfBirth" } },
                { "LabResult", new List<string> { "TestName", "Result", "TestDate" } }
            };

            return requiredFields.ContainsKey(targetEntity) && 
                   requiredFields[targetEntity].Contains(targetProperty);
        }
    }

    public class LabConfigurationCard
    {
        public Guid Id { get; set; }
        public string ConfigurationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "NotConfigured"; // Active, NeedsAttention, NotConfigured
        public string StatusMessage { get; set; } = string.Empty;
        public int MappedFieldCount { get; set; }
    }
}
