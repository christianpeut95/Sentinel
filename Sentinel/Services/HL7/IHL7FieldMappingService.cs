using NHapi.Base.Model;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for applying configuration-driven HL7 field mappings
    /// Provides disease-specific and default mapping resolution
    /// </summary>
    public interface IHL7FieldMappingService
    {
        /// <summary>
        /// Get the mapped value for a specific target from the HL7 message
        /// Uses disease-specific mapping if available, otherwise falls back to default
        /// </summary>
        Task<string?> GetMappedValueAsync(
            IMessage message,
            Guid configurationId,
            Guid? diseaseId,
            string targetEntity,
            string targetProperty);

        /// <summary>
        /// Get all field mappings for a configuration and optional disease
        /// Returns disease-specific mappings merged with defaults
        /// </summary>
        Task<List<HL7FieldMapping>> GetEffectiveMappingsAsync(
            Guid configurationId,
            Guid? diseaseId);

        /// <summary>
        /// Extract a value from an HL7 message using a specific field mapping
        /// Handles transformation rules, code lookups, and default values
        /// </summary>
        Task<string?> ExtractValueAsync(
            IMessage message,
            HL7FieldMapping mapping);

        /// <summary>
        /// Get all diseases associated with a configuration
        /// </summary>
        Task<List<Disease>> GetConfigurationDiseasesAsync(Guid configurationId);

        /// <summary>
        /// Get the default disease for a configuration (if any)
        /// </summary>
        Task<Disease?> GetDefaultDiseaseAsync(Guid configurationId);

        /// <summary>
        /// Extract values from all repetitions of a repeating segment (e.g., OBX)
        /// Returns a list where each entry corresponds to one segment occurrence
        /// </summary>
        Task<List<string?>> ExtractValuesFromRepeatingSegmentAsync(
            IMessage message,
            HL7FieldMapping mapping);

        /// <summary>
        /// Extract a value from a specific occurrence of a repeating segment
        /// </summary>
        Task<string?> ExtractValueFromSegmentRepetitionAsync(
            IMessage message,
            HL7FieldMapping mapping,
            int repetition);

        /// <summary>
        /// Get the mapped value for a target property from raw HL7 segments when NHapi parsing is not available
        /// Useful for dynamic fallback scenarios with vendor-specific message structures
        /// </summary>
        Task<string?> GetMappedValueFromRawSegmentAsync(
            string rawSegment,
            Guid configurationId,
            Guid? diseaseId,
            string targetEntity,
            string targetProperty);
    }
}
