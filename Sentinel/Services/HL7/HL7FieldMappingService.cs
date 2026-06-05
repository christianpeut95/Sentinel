using Microsoft.EntityFrameworkCore;
using NHapi.Base.Model;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using System.Text.RegularExpressions;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for applying configuration-driven HL7 field mappings
    /// Supports disease-specific and default mappings with fallback logic
    /// </summary>
    public class HL7FieldMappingService : IHL7FieldMappingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HL7FieldMappingService> _logger;

        public HL7FieldMappingService(
            ApplicationDbContext context,
            ILogger<HL7FieldMappingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string?> GetMappedValueAsync(
            IMessage message,
            Guid configurationId,
            Guid? diseaseId,
            string targetEntity,
            string targetProperty)
        {
            // Get the effective mapping (disease-specific or default)
            var mapping = await _context.HL7FieldMappings
                .Where(m => m.ConfigurationId == configurationId &&
                           m.TargetEntity == targetEntity &&
                           m.TargetProperty == targetProperty &&
                           m.IsActive)
                .OrderByDescending(m => m.DiseaseId == diseaseId ? 1 : 0) // Disease-specific first
                .ThenByDescending(m => m.Priority)
                .FirstOrDefaultAsync();

            if (mapping == null)
            {
                _logger.LogDebug(
                    "No mapping found for {Entity}.{Property} in configuration {ConfigId}, disease {DiseaseId}",
                    targetEntity, targetProperty, configurationId, diseaseId);
                return null;
            }

            return await ExtractValueAsync(message, mapping);
        }

        public async Task<List<HL7FieldMapping>> GetEffectiveMappingsAsync(
            Guid configurationId,
            Guid? diseaseId)
        {
            var mappings = await _context.HL7FieldMappings
                .Where(m => m.ConfigurationId == configurationId &&
                           m.IsActive &&
                           (m.DiseaseId == null || m.DiseaseId == diseaseId))
                .OrderByDescending(m => m.Priority)
                .ToListAsync();

            // Group by target and take disease-specific over defaults
            var effectiveMappings = mappings
                .GroupBy(m => new { m.TargetEntity, m.TargetProperty })
                .Select(g => g.FirstOrDefault(m => m.DiseaseId == diseaseId) ?? g.First())
                .ToList();

            return effectiveMappings;
        }

        public async Task<string?> ExtractValueAsync(IMessage message, HL7FieldMapping mapping)
        {
            try
            {
                // Parse the field path (e.g., "OBR-15.1", "PID-5.1")
                var parts = mapping.FieldPath.Split('-');
                if (parts.Length != 2)
                {
                    _logger.LogWarning("Invalid field path format: {Path}", mapping.FieldPath);
                    return mapping.DefaultValue;
                }

                var segmentName = parts[0];
                var fieldParts = parts[1].Split('.');

                if (!int.TryParse(fieldParts[0], out var fieldIndex))
                {
                    _logger.LogWarning("Invalid field index in path: {Path}", mapping.FieldPath);
                    return mapping.DefaultValue;
                }

                // Get the segment
                var segment = GetSegment(message, segmentName, 0); // Default to first occurrence
                if (segment == null)
                {
                    _logger.LogDebug("Segment {Segment} not found in message", segmentName);
                    return mapping.DefaultValue;
                }

                // Get the field value
                var field = segment.GetField(fieldIndex, 0);
                if (field == null)
                {
                    _logger.LogDebug("Field {Field} not found in segment {Segment}", fieldIndex, segmentName);
                    return mapping.DefaultValue;
                }

                string? rawValue = null;

                // Handle component/subcomponent if specified
                if (fieldParts.Length > 1 && int.TryParse(fieldParts[1], out var componentIndex))
                {
                    var composite = field as IComposite;
                    if (composite != null && componentIndex <= composite.Components.Length)
                    {
                        var component = composite.Components[componentIndex - 1];
                        rawValue = component?.ToString();
                    }
                }
                else
                {
                    rawValue = field.ToString();
                }

                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    return mapping.DefaultValue;
                }

                // Apply transformation based on mapping type
                var transformedValue = await ApplyTransformationAsync(rawValue, mapping);

                // Validate if required
                if (mapping.IsRequired && string.IsNullOrWhiteSpace(transformedValue))
                {
                    _logger.LogWarning(
                        "Required field {Path} ({Entity}.{Property}) is empty",
                        mapping.FieldPath, mapping.TargetEntity, mapping.TargetProperty);
                }

                // Validate against regex if specified
                if (!string.IsNullOrWhiteSpace(mapping.ValidationRegex) &&
                    !string.IsNullOrWhiteSpace(transformedValue))
                {
                    if (!Regex.IsMatch(transformedValue, mapping.ValidationRegex))
                    {
                        _logger.LogWarning(
                            "Field value '{Value}' does not match validation pattern '{Pattern}' for {Path}",
                            transformedValue, mapping.ValidationRegex, mapping.FieldPath);
                        return mapping.DefaultValue;
                    }
                }

                // Update usage statistics
                await UpdateMappingStatsAsync(mapping.Id, success: true);

                return transformedValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error extracting value for mapping {MappingId} ({Path})",
                    mapping.Id, mapping.FieldPath);

                await UpdateMappingStatsAsync(mapping.Id, success: false);
                return mapping.DefaultValue;
            }
        }

        public async Task<List<Disease>> GetConfigurationDiseasesAsync(Guid configurationId)
        {
            return await _context.HL7ConfigurationDiseases
                .Where(cd => cd.ConfigurationId == configurationId)
                .Include(cd => cd.Disease)
                .OrderByDescending(cd => cd.Priority)
                .Select(cd => cd.Disease!)
                .ToListAsync();
        }

        public async Task<Disease?> GetDefaultDiseaseAsync(Guid configurationId)
        {
            var defaultConfig = await _context.HL7ConfigurationDiseases
                .Where(cd => cd.ConfigurationId == configurationId && cd.IsDefault)
                .Include(cd => cd.Disease)
                .FirstOrDefaultAsync();

            return defaultConfig?.Disease;
        }

        private ISegment? GetSegment(IMessage message, string segmentName, int repetition)
        {
            try
            {
                var segmentNames = message.GetStructureName().Split('_');
                var segments = message.GetAll(segmentName);

                if (segments.Length > repetition)
                {
                    return segments[repetition] as ISegment;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error getting segment {Segment}", segmentName);
                return null;
            }
        }

        private async Task<string?> ApplyTransformationAsync(string rawValue, HL7FieldMapping mapping)
        {
            switch (mapping.MappingType)
            {
                case HL7MappingType.DirectCopy:
                    return rawValue;

                case HL7MappingType.CodeLookup:
                    // TODO: Implement code lookup against lookup tables
                    return rawValue;

                case HL7MappingType.DateFormatConversion:
                    return await ConvertDateFormatAsync(rawValue);

                case HL7MappingType.NumericConversion:
                    return ConvertToNumeric(rawValue);

                case HL7MappingType.BooleanConversion:
                    return ConvertToBoolean(rawValue);

                case HL7MappingType.CustomExpression:
                    // TODO: Implement custom expression evaluation
                    _logger.LogWarning("Custom expressions not yet implemented for mapping {Id}", mapping.Id);
                    return rawValue;

                default:
                    return rawValue;
            }
        }

        private Task<string?> ConvertDateFormatAsync(string rawValue)
        {
            // Try to parse HL7 date format (YYYYMMDD, YYYYMMDDHHmmss, etc.)
            if (DateTime.TryParseExact(rawValue, 
                new[] { "yyyyMMdd", "yyyyMMddHHmmss", "yyyyMMddHHmm", "yyyyMMddHH" },
                null,
                System.Globalization.DateTimeStyles.None,
                out var date))
            {
                return Task.FromResult<string?>(date.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            return Task.FromResult<string?>(rawValue);
        }

        private string? ConvertToNumeric(string rawValue)
        {
            if (decimal.TryParse(rawValue, out var numericValue))
            {
                return numericValue.ToString();
            }

            return rawValue;
        }

        private string? ConvertToBoolean(string rawValue)
        {
            var normalized = rawValue.Trim().ToUpperInvariant();

            return normalized switch
            {
                "Y" or "YES" or "TRUE" or "1" or "T" => "true",
                "N" or "NO" or "FALSE" or "0" or "F" => "false",
                _ => rawValue
            };
        }

        private async Task UpdateMappingStatsAsync(Guid mappingId, bool success)
        {
            try
            {
                var mapping = await _context.HL7FieldMappings.FindAsync(mappingId);
                if (mapping != null)
                {
                    mapping.TimesUsed++;
                    if (!success)
                    {
                        mapping.TimesFailed++;
                    }
                    mapping.LastUsedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update mapping stats for {MappingId}", mappingId);
                // Don't throw - stats update failure shouldn't break the flow
            }
        }
    }
}
