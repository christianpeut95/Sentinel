using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Pathogens;

namespace Sentinel.Services.HL7.Models
{
    /// <summary>
    /// Staged processing data for HL7 lab results before database commit
    /// </summary>
    public class HL7ProcessingStage
    {
        public HL7Message HL7Message { get; set; } = null!;

        // Staging structures (may not yet exist in DB)
        public StagedPatient? StagedPatient { get; set; }
        public StagedOrganization? StagedLaboratory { get; set; }
        public StagedOrganization? StagedOrderingProvider { get; set; }
        public StagedLabResult? StagedLabResult { get; set; }

        // Disease matching results
        public List<DiseaseMatch> DiseaseMatches { get; set; } = new();

        // Validation results
        public DuplicateCheckResult? DuplicateCheck { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();

        // Decision tracking
        public bool RequiresManualReview { get; set; }
        public string? ManualReviewReason { get; set; }
        public ProcessingDecision Decision { get; set; } = ProcessingDecision.Pending;
    }

    public enum ProcessingDecision
    {
        Pending,
        Duplicate,          // Exact duplicate, ignore
        CreateNewCase,      // New patient or outside reinfection window
        LinkToExistingCase, // Within reinfection window
        ManualReview,       // Conflicts detected
        NoSurveillance      // Not a reportable disease
    }

    /// <summary>
    /// Temporary patient data structure before DB commit
    /// </summary>
    public class StagedPatient
    {
        public Guid? ExistingPatientId { get; set; } // If matched to existing
        public Patient? ExistingPatient { get; set; }
        public bool IsNew { get; set; }

        // Extracted demographics
        public string? MRN { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? MiddleName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Sex { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }

        // Address
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// Temporary organization data structure before DB commit
    /// </summary>
    public class StagedOrganization
    {
        public Guid? ExistingOrganizationId { get; set; }
        public Organization? ExistingOrganization { get; set; }
        public bool IsNew { get; set; }

        public string Name { get; set; } = string.Empty;
        public string OrganizationTypeName { get; set; } = string.Empty; // "Laboratory" or "Healthcare Provider"
    }

    /// <summary>
    /// Temporary lab result data structure before DB commit
    /// </summary>
    public class StagedLabResult
    {
        public Guid? ExistingLabResultId { get; set; }
        public LabResult? ExistingLabResult { get; set; }
        public bool IsNew { get; set; }
        public bool IsUpdate { get; set; } // Existing result with new/updated markers

        public string? AccessionNumber { get; set; }
        public DateTime? SpecimenCollectionDate { get; set; }
        public DateTime? ResultDate { get; set; }
        public string? Notes { get; set; }

        // Specimen Type fields (from OBR-15)
        public string? SpecimenTypeCode { get; set; }
        public string? SpecimenTypeText { get; set; }
        public string? SpecimenTypeCodingSystem { get; set; }

        // Resolution tracking for case definition matching
        public int? ResolvedSpecimenTypeId { get; set; }
        public MatchMethod SpecimenMatchMethod { get; set; } = MatchMethod.NotMatched;

        public List<StagedMarker> Markers { get; set; } = new();
    }

    public class StagedMarker
    {
        public Guid? ExistingMarkerId { get; set; }
        public LabResultMarker? ExistingMarker { get; set; }
        public bool IsNew { get; set; }
        public bool IsUpdated { get; set; }

        public string? TestCode { get; set; }      // LOINC code
        public string? TestName { get; set; }
        public string? QualitativeResult { get; set; }
        public decimal? QuantitativeValue { get; set; }
        public string? Units { get; set; }
        public string? ReferenceRange { get; set; }
        public string? ResultStatus { get; set; }   // F=Final, P=Preliminary, C=Corrected
        public string? InterpretationFlag { get; set; } // N=Normal, A=Abnormal, H=High, L=Low

        // Test Method fields (from OBX-17)
        public string? TestMethodCode { get; set; }
        public string? TestMethodText { get; set; }
        public string? TestMethodCodingSystem { get; set; }

        // Change tracking
        public string? PreviousQualitativeResult { get; set; }
        public decimal? PreviousQuantitativeValue { get; set; }

        // Resolution tracking for case definition matching
        public Guid? ResolvedPathogenId { get; set; }
        public MatchMethod PathogenMatchMethod { get; set; } = MatchMethod.NotMatched;

        public int? ResolvedTestMethodId { get; set; }
        public MatchMethod TestMethodMatchMethod { get; set; } = MatchMethod.NotMatched;

        public string? NormalizedResultValue { get; set; } // "Positive", "Negative", etc.
    }

    /// <summary>
    /// Disease identified from HL7 markers with hierarchy and case definition matching
    /// </summary>
    public class DiseaseMatch
    {
        public Disease Disease { get; set; } = null!;
        public Disease? OriginalTopLevelDisease { get; set; } // The disease family we started with
        public Pathogen? MatchedPathogen { get; set; }
        public CaseDefinition? MatchedCaseDefinition { get; set; }
        public List<CaseDefinition> MatchedCaseDefinitions { get; set; } = new(); // Support multiple matching case definitions

        public MatchSource Source { get; set; }
        public bool IsPositiveResult { get; set; }
        public List<StagedMarker> MatchedMarkers { get; set; } = new();

        // Existing case checking
        public Case? ExistingCase { get; set; }
        public ReinfectionDecision ReinfectionDecision { get; set; } = ReinfectionDecision.NewCase;
        public string? ReinfectionReason { get; set; }

        // Disease refinement (3c logic)
        public bool ShouldRefineDiseaseOnExistingCase { get; set; }
        public Disease? RefinedDisease { get; set; }

        // Case creation decision properties
        public bool MultipleActiveCasesDetected { get; set; }
        public bool ShouldCreateNewCase { get; set; }
        public Disease? FinalDiseaseForCase { get; set; }  // The disease to use for case creation
    }

    public enum MatchSource
    {
        LOINCPathogenMatch,  // Direct LOINC code → Pathogen → Disease
        CaseDefinitionMatch  // Evaluated against case definition rules
    }

    public enum ReinfectionDecision
    {
        NewCase,             // No existing case, or outside reinfection window
        LinkToExisting,      // Within reinfection window
        ManualReview         // Ambiguous (e.g., multiple active cases)
    }

    /// <summary>
    /// Result of duplicate detection by accession + specimen date + lab
    /// </summary>
    public class DuplicateCheckResult
    {
        public bool IsDuplicate { get; set; }
        public bool IsIdentical { get; set; }        // Same markers + values
        public bool IsPatientMismatch { get; set; }  // Same accession, different patient
        public LabResult? ExistingLabResult { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Tracks how a value was matched during HL7 processing
    /// </summary>
    public enum MatchMethod
    {
        NotMatched,  // No match found
        Exact,       // Matched by code/ID
        Text         // Matched by text search/fuzzy matching
    }
}

