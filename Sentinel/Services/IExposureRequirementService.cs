using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public interface IExposureRequirementService
    {
        Task<Disease?> GetRequirementsForDiseaseAsync(Guid diseaseId);
        Task<bool> ShouldPromptForExposureAsync(Guid diseaseId);
        Task<Location?> GetDefaultExposureLocationAsync(Patient patient);
        Task<bool> ValidateExposureCompletenessAsync(Case caseEntity);
        Task<List<Case>> GetCasesAffectedByAddressChangeAsync(Guid patientId, int recentDays = 30);
        Task<List<Case>> GetCasesWithMissingExposureDataAsync(Guid? diseaseId = null, int? maxAgeDays = null);
    }
}
