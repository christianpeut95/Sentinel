using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public class ExposureRequirementService : IExposureRequirementService
    {
        private readonly ApplicationDbContext _context;

        public ExposureRequirementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Disease?> GetRequirementsForDiseaseAsync(Guid diseaseId)
        {
            return await _context.Diseases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == diseaseId);
        }

        public async Task<bool> ShouldPromptForExposureAsync(Guid diseaseId)
        {
            var disease = await GetRequirementsForDiseaseAsync(diseaseId);
            if (disease == null)
                return false;

            return disease.ExposureTrackingMode != ExposureTrackingMode.Optional ||
                   disease.AlwaysPromptForLocation;
        }

        public async Task<Location?> GetDefaultExposureLocationAsync(Patient patient)
        {
            if (patient == null)
                return null;

            if (string.IsNullOrWhiteSpace(patient.AddressLine))
                return null;

            var fullAddress = patient.AddressLine;
            if (!string.IsNullOrWhiteSpace(patient.City))
                fullAddress += $", {patient.City}";
            if (!string.IsNullOrWhiteSpace(patient.State))
                fullAddress += $", {patient.State}";
            if (!string.IsNullOrWhiteSpace(patient.PostalCode))
                fullAddress += $" {patient.PostalCode}";

            var location = new Location
            {
                Name = "Patient Residential Address",
                Address = fullAddress,
                Latitude = patient.Latitude.HasValue ? (decimal?)patient.Latitude.Value : null,
                Longitude = patient.Longitude.HasValue ? (decimal?)patient.Longitude.Value : null,
                IsActive = true
            };

            return await Task.FromResult(location);
        }

        public async Task<bool> ValidateExposureCompletenessAsync(Case caseEntity)
        {
            if (caseEntity.DiseaseId == null)
                return true;

            var disease = await GetRequirementsForDiseaseAsync(caseEntity.DiseaseId.Value);
            if (disease == null)
                return true;

            if (disease.ExposureTrackingMode == ExposureTrackingMode.Optional)
                return true;

            var hasExposure = await _context.ExposureEvents
                .AnyAsync(e => e.ExposedCaseId == caseEntity.Id);

            if (disease.ExposureTrackingMode == ExposureTrackingMode.LocalSpecificRegion ||
                disease.ExposureTrackingMode == ExposureTrackingMode.OverseasAcquired)
            {
                return hasExposure;
            }

            return true;
        }

        public async Task<List<Case>> GetCasesAffectedByAddressChangeAsync(Guid patientId, int recentDays = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-recentDays);

            var cases = await _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.ExposureEvents)
                .Where(c => c.PatientId == patientId)
                .Where(c => c.DateOfNotification.HasValue && c.DateOfNotification.Value >= cutoffDate)
                .Where(c => c.Disease != null && c.Disease.SyncWithPatientAddressUpdates)
                .Where(c => c.ExposureEvents.Any(e => e.IsDefaultedFromResidentialAddress))
                .ToListAsync();

            return cases;
        }

        public async Task<List<Case>> GetCasesWithMissingExposureDataAsync(Guid? diseaseId = null, int? maxAgeDays = null)
        {
            var query = _context.Cases
                .Include(c => c.Disease)
                .Include(c => c.Patient)
                .Include(c => c.ExposureEvents)
                .Where(c => c.Disease != null)
                .Where(c => c.Disease!.ExposureTrackingMode != ExposureTrackingMode.Optional)
                .Where(c => !c.ExposureEvents.Any());

            if (diseaseId.HasValue)
            {
                query = query.Where(c => c.DiseaseId == diseaseId.Value);
            }

            if (maxAgeDays.HasValue)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays.Value);
                query = query.Where(c => c.DateOfNotification.HasValue && c.DateOfNotification.Value >= cutoffDate);
            }

            var cases = await query
                .OrderByDescending(c => c.DateOfNotification ?? c.DateOfOnset)
                .ToListAsync();

            if (!maxAgeDays.HasValue)
            {
                cases = cases.Where(c =>
                {
                    if (c.Disease?.ExposureDataGracePeriodDays == null)
                        return true;

                    var referenceDate = c.DateOfNotification ?? c.DateOfOnset ?? DateTime.UtcNow;
                    var caseAge = (DateTime.UtcNow - referenceDate).Days;
                    return caseAge <= c.Disease.ExposureDataGracePeriodDays.Value;
                }).ToList();
            }

            return cases;
        }
    }
}
