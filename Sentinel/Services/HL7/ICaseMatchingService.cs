using Sentinel.Models;
using Sentinel.Models.HL7;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for matching lab results to cases and managing disease identification
    /// </summary>
    public interface ICaseMatchingService
    {
        /// <summary>
        /// Process a lab result and create or link to appropriate cases based on markers
        /// </summary>
        Task<CaseMatchingResult> ProcessLabResultAsync(
            LabResult labResult,
            Patient patient,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Find or create a case for a specific disease and lab result
        /// </summary>
        Task<Case> FindOrCreateCaseAsync(
            Patient patient,
            Disease disease,
            LabResult labResult,
            int? confirmationStatusId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Identify diseases from lab result markers
        /// </summary>
        Task<List<DiseaseIdentification>> IdentifyDiseasesFromMarkersAsync(
            LabResult labResult,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a case should be transferred to a more specific disease
        /// </summary>
        Task<DiseaseRefinementResult> EvaluateDiseaseRefinementAsync(
            Case existingCase,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of processing a lab result through case matching
    /// </summary>
    public class CaseMatchingResult
    {
        public bool Success { get; set; }
        public List<Case> CasesCreated { get; set; } = new();
        public List<Case> CasesLinked { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public bool RequiresManualReview { get; set; }
        public string? ManualReviewReason { get; set; }
    }

    /// <summary>
    /// Disease identified from lab result markers
    /// </summary>
    public class DiseaseIdentification
    {
        public Disease Disease { get; set; } = null!;
        public List<LabResultMarker> MatchingMarkers { get; set; } = new();
        public IdentificationSource Source { get; set; }
        public int SpecificityScore { get; set; }
        public bool IsPositiveResult { get; set; }
        public CaseStatus? ConfirmationStatus { get; set; }
        public int? ConfirmationStatusId { get; set; }
    }

    /// <summary>
    /// Source of disease identification
    /// </summary>
    public enum IdentificationSource
    {
        Biomarker = 1,
        CaseDefinition = 2,
        ParentDisease = 3
    }

    /// <summary>
    /// Result of evaluating disease refinement
    /// </summary>
    public class DiseaseRefinementResult
    {
        public bool ShouldRefine { get; set; }
        public Disease? NewDisease { get; set; }
        public CaseDefinition? MatchingDefinition { get; set; }
        public bool RequiresReview { get; set; }
        public string? Reason { get; set; }
    }
}
