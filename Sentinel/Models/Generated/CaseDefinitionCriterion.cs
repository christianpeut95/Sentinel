using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CaseDefinitionCriterion
{
    public int Id { get; set; }

    public int CaseDefinitionId { get; set; }

    public int? ParentCriteriaId { get; set; }

    public int CriterionType { get; set; }

    public int LogicalOperator { get; set; }

    public int GroupNumber { get; set; }

    public string? FieldPath { get; set; }

    public int? Operator { get; set; }

    public string? ValueJson { get; set; }

    public string? DisplayText { get; set; }

    public int DisplayOrder { get; set; }

    public string? AcceptablePathogensJson { get; set; }

    public string? AcceptableResultsJson { get; set; }

    public string? AcceptableSpecimenTypesJson { get; set; }

    public string? AcceptableTestMethodsJson { get; set; }

    public int? BiomarkerStoragePreference { get; set; }

    public Guid? CanonicalPathogenId { get; set; }

    public int? CanonicalSpecimenTypeId { get; set; }

    public int? CanonicalTestMethodId { get; set; }

    public int? CanonicalTestResultId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? Description { get; set; }

    public bool? IsRequired { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool? RequireAllElementsMatch { get; set; }

    public int? ResultStoragePreference { get; set; }

    public int? SpecimenStoragePreference { get; set; }

    public int? TestMethodStoragePreference { get; set; }

    public int? GroupExitOperator { get; set; }

    public virtual Pathogen? CanonicalPathogen { get; set; }

    public virtual SpecimenType? CanonicalSpecimenType { get; set; }

    public virtual TestMethod? CanonicalTestMethod { get; set; }

    public virtual TestResult? CanonicalTestResult { get; set; }

    public virtual CaseDefinition CaseDefinition { get; set; } = null!;

    public virtual ICollection<CaseDefinitionCriterion> InverseParentCriteria { get; set; } = new List<CaseDefinitionCriterion>();

    public virtual CaseDefinitionCriterion? ParentCriteria { get; set; }
}
