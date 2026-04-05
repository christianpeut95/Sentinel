using Sentinel.Models;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing patient address updates and case address synchronization
    /// </summary>
    public interface IPatientAddressService
    {
        /// <summary>
        /// Detects address changes and handles geocoding, jurisdiction updates, and case review
        /// Returns list of cases that need user confirmation for address updates
        /// </summary>
        Task<PatientAddressUpdateResult> ProcessAddressChangeAsync(
            Patient patient,
            string? oldAddressLine,
            string? oldCity,
            int? oldStateId,
            string? oldPostalCode,
            string? currentUserId);

        /// <summary>
        /// Copy patient address to a case (initial case creation or manual override)
        /// </summary>
        Task CopyAddressToCaseAsync(Guid caseId, bool manualOverride = false);

        /// <summary>
        /// Apply patient address to multiple cases (from user prompt or review queue)
        /// </summary>
        Task ApplyAddressToCasesAsync(Guid patientId, List<Guid> caseIds, string? currentUserId);

        /// <summary>
        /// Compare jurisdictions between two sets of IDs and detect if any monitored field changed
        /// </summary>
        bool HasJurisdictionCrossing(
            int?[] oldJurisdictions,
            int?[] newJurisdictions,
            string? fieldsToCheck);

        /// <summary>
        /// Resolve effective address settings for a disease, including inherited settings from parent
        /// </summary>
        Task<DiseaseAddressSettings> GetEffectiveAddressSettingsAsync(Guid diseaseId);
    }

    public class DiseaseAddressSettings
    {
        public bool SyncWithPatientAddressUpdates { get; set; }
        public int? AddressReviewWindowBeforeDays { get; set; }
        public int? AddressReviewWindowAfterDays { get; set; }
        public bool CheckJurisdictionCrossing { get; set; }
        public string? JurisdictionFieldsToCheck { get; set; }
        public bool DefaultToResidentialAddress { get; set; }
        public Guid SourceDiseaseId { get; set; } // Which disease provided these settings (for audit)
        public bool IsInherited { get; set; } // True if settings came from parent
    }

    public class PatientAddressUpdateResult
    {
        public bool Success { get; set; }
        public bool AddressChanged { get; set; }
        public bool GeocodingSucceeded { get; set; }
        public double? NewLatitude { get; set; }
        public double? NewLongitude { get; set; }
        public List<CaseAddressReviewItem> CasesRequiringReview { get; set; } = new();
        public List<Guid> CasesAutoUpdated { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class CaseAddressReviewItem
    {
        public Guid CaseId { get; set; }
        public string CaseFriendlyId { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public DateTime? DateOfOnset { get; set; }
        public bool HasJurisdictionCrossing { get; set; }
        public bool ManualOverrideExists { get; set; }
        public string OldAddress { get; set; } = string.Empty;
        public string NewAddress { get; set; } = string.Empty;
        public string? OldJurisdiction { get; set; }
        public string? NewJurisdiction { get; set; }
    }
}
