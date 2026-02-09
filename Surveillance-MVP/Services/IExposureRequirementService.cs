using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Services
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
