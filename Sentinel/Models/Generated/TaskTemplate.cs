using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TaskTemplate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public Guid TaskTypeId { get; set; }

    public int DefaultPriority { get; set; }

    public int TriggerType { get; set; }

    public int? ApplicableToType { get; set; }

    public int? DueDaysFromOnset { get; set; }

    public int? DueDaysFromNotification { get; set; }

    public int? DueDaysFromContact { get; set; }

    public int DueCalculationMethod { get; set; }

    public bool IsRecurring { get; set; }

    public int? RecurrencePattern { get; set; }

    public int? RecurrenceCount { get; set; }

    public int? RecurrenceDurationDays { get; set; }

    public Guid? SurveyTemplateId { get; set; }

    public string? SurveyDefinitionJson { get; set; }

    public string? DefaultInputMappingJson { get; set; }

    public string? DefaultOutputMappingJson { get; set; }

    public string? Instructions { get; set; }

    public string? CompletionCriteria { get; set; }

    public bool RequiresEvidence { get; set; }

    public int AssignmentType { get; set; }

    public int InheritanceBehavior { get; set; }

    public string? RestrictToSubDiseaseIds { get; set; }

    public bool IsActive { get; set; }

    public bool IsInterviewTask { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual ICollection<CaseTask> CaseTasks { get; set; } = new List<CaseTask>();

    public virtual ICollection<DiseaseTaskTemplate> DiseaseTaskTemplates { get; set; } = new List<DiseaseTaskTemplate>();

    public virtual SurveyTemplate? SurveyTemplate { get; set; }

    public virtual TaskType TaskType { get; set; } = null!;
}
