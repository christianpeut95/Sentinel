using Sentinel.Models;

namespace Sentinel.Services
{
    public class SurveyDataContext
    {
        public CaseTask Task { get; set; } = null!;
        public Case Case { get; set; } = null!;
        public Patient? Patient { get; set; }
        public List<ExposureEvent>? Exposures { get; set; }
        public List<LabResult>? LabResults { get; set; }
        public Dictionary<string, object> CustomFields { get; set; } = new();
    }

    public class SurveyDefinitionWithData
    {
        public string? SurveyDefinitionJson { get; set; }
        public Dictionary<string, object> PrePopulatedData { get; set; } = new();
        public bool HasSurvey { get; set; }
    }

    public interface ISurveyService
    {
        /// <summary>
        /// Gets survey definition and pre-populates data based on input mappings
        /// </summary>
        Task<SurveyDefinitionWithData> GetSurveyForTaskAsync(Guid taskId);

        /// <summary>
        /// Saves survey responses and maps them to case/patient fields based on output mappings
        /// </summary>
        Task SaveSurveyResponseAsync(Guid taskId, Dictionary<string, object> responses);

        /// <summary>
        /// Validates that a survey definition is valid JSON and SurveyJS format
        /// </summary>
        bool ValidateSurveyDefinition(string surveyJson);

        /// <summary>
        /// Gets the survey response data for a completed task (read-only)
        /// </summary>
        Task<Dictionary<string, object>?> GetSurveyResponseAsync(Guid taskId);

        /// <summary>
        /// Resolves a field path like "Patient.Age" or "Case.DiseaseId" to its value
        /// </summary>
        object? ResolveFieldPath(string fieldPath, SurveyDataContext context);

        /// <summary>
        /// Sets a value on a field path like "Case.CustomFields.RiskScore"
        /// </summary>
        Task SetFieldValueAsync(string fieldPath, object? value, SurveyDataContext context);
    }
}
