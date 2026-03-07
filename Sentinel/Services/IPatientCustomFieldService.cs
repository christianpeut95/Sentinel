using Sentinel.Models;

namespace Sentinel.Services
{
    public interface IPatientCustomFieldService
    {
        Task<List<CustomFieldDefinition>> GetCreateEditFieldsAsync();
        Task<List<CustomFieldDefinition>> GetDetailsFieldsAsync();
        Task<Dictionary<int, string?>> GetPatientFieldValuesAsync(Guid patientId);
        Task<Dictionary<int, string?>> GetPatientFieldDisplayValuesAsync(Guid patientId);
        Task SavePatientFieldValuesAsync(Guid patientId, Dictionary<string, string?> fieldValues, string? userId, string? ipAddress);
    }
}

