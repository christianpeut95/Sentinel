using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseHl7matchingConfig
{
    public Guid DiseaseId { get; set; }

    public bool OverrideParentRules { get; set; }

    public bool TestMethodUseTextFallback { get; set; }

    public bool TestMethodNormalizeWhitespace { get; set; }

    public bool TestMethodIgnorePunctuation { get; set; }

    public bool TestMethodCaseInsensitive { get; set; }

    public bool SpecimenTypeUseTextFallback { get; set; }

    public bool SpecimenTypeNormalizeWhitespace { get; set; }

    public bool SpecimenTypeIgnorePunctuation { get; set; }

    public bool SpecimenTypeCaseInsensitive { get; set; }

    public bool PathogenUseTextFallback { get; set; }

    public bool PathogenNormalizeWhitespace { get; set; }

    public bool PathogenIgnorePunctuation { get; set; }

    public bool PathogenCaseInsensitive { get; set; }

    public bool TestResultUseTextFallback { get; set; }

    public bool TestResultNormalizeWhitespace { get; set; }

    public bool TestResultIgnorePunctuation { get; set; }

    public bool TestResultCaseInsensitive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool AllowMissingPathogen { get; set; }

    public bool AllowMissingResult { get; set; }

    public bool AllowMissingSpecimenType { get; set; }

    public bool AllowMissingTestMethod { get; set; }

    public int MaxMissingFieldsAllowed { get; set; }

    public int? PartialMatchConfirmationStatusId { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual CaseStatus? PartialMatchConfirmationStatus { get; set; }
}
