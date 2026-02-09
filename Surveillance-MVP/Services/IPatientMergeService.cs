using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public interface IPatientMergeService
    {
        Task<PatientMergeComparison> GetMergeComparisonAsync(Guid sourcePatientId, Guid targetPatientId);
        Task<bool> ValidateMergeAsync(Guid sourcePatientId, Guid targetPatientId);
        Task<MergeResult> MergePatientsAsync(Guid sourcePatientId, Guid targetPatientId, PatientMergeSelection selection, string? userId, string? ipAddress);
    }

    public class PatientMergeComparison
    {
        public Patient SourcePatient { get; set; } = null!;
        public Patient TargetPatient { get; set; } = null!;
        public Dictionary<string, PatientCustomFieldValue> SourceCustomFields { get; set; } = new();
        public Dictionary<string, PatientCustomFieldValue> TargetCustomFields { get; set; } = new();
        public int SourceAuditLogCount { get; set; }
        public int TargetAuditLogCount { get; set; }
    }

    public class PatientCustomFieldValue
    {
        public int FieldDefinitionId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string? DisplayValue { get; set; }
        public object? RawValue { get; set; }
        public string FieldType { get; set; } = string.Empty;
    }

    public class PatientMergeSelection
    {
        public Dictionary<string, object?> SelectedValues { get; set; } = new();
        public Dictionary<int, object?> SelectedCustomFieldValues { get; set; } = new();
    }

    public class MergeResult
    {
        public bool Success { get; set; }
        public Guid? MergedPatientId { get; set; }
        public Guid? DeletedPatientId { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
