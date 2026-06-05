using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class SelectLabModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SelectLabModel(ApplicationDbContext context)
        {
            _context = context;
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
                var mappings = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == config.Id && m.IsActive)
                    .ToListAsync();

                var requiredFieldCount = GetRequiredFieldKeys().Count;
                var mappedRequiredCount = mappings.Count(m => 
                    !string.IsNullOrEmpty(m.FieldPath) && 
                    m.FieldPath != "SKIPPED" &&
                    IsRequiredField(m.TargetEntity, m.TargetProperty));

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

                // Get message count (if tracking is implemented)
                var messageCount = 0; // TODO: Link to actual message processing stats

                Configurations.Add(new LabConfigurationCard
                {
                    Id = config.Id,
                    ConfigurationName = config.ConfigurationName,
                    Description = config.SendingFacility ?? "No facility specified",
                    Status = status,
                    StatusMessage = statusMessage,
                    MappedFieldCount = mappings.Count(m => !string.IsNullOrEmpty(m.FieldPath) && m.FieldPath != "SKIPPED"),
                    MessageCount = messageCount
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
                "test_result",
                "test_date"
            };
        }

        private bool IsRequiredField(string targetEntity, string targetProperty)
        {
            var requiredFields = new Dictionary<string, List<string>>
            {
                { "Patient", new List<string> { "FirstName", "LastName", "DateOfBirth" } },
                { "LabResult", new List<string> { "Result", "TestDate" } }
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
        public int MessageCount { get; set; }
    }
}
