using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class SurveyFieldMapping
{
    public Guid Id { get; set; }

    public int ConfigurationType { get; set; }

    public Guid ConfigurationId { get; set; }

    public int Priority { get; set; }

    public string SurveyQuestionName { get; set; } = null!;

    public string TargetFieldPath { get; set; } = null!;

    public int TargetFieldType { get; set; }

    public int FieldCategory { get; set; }

    public int MappingAction { get; set; }

    public int BusinessRule { get; set; }

    public bool TriggerReviewQueue { get; set; }

    public int ReviewPriority { get; set; }

    public int GroupingWindowHours { get; set; }

    public string? ValidationRules { get; set; }

    public string? TransformationScript { get; set; }

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public int? TargetSymptomId { get; set; }

    public int Complexity { get; set; }

    public string? CollectionConfigJson { get; set; }

    public string? MatchingRulesJson { get; set; }

    public int? OnDuplicateFound { get; set; }

    public int ExecutionOrder { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? LastModifiedByUserId { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual AspNetUser? LastModifiedByUser { get; set; }

    public virtual Symptom? TargetSymptom { get; set; }
}
