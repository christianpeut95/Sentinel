using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class SurveyTemplate
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Category { get; set; }

    public string SurveyDefinitionJson { get; set; } = null!;

    public string? DefaultInputMappingJson { get; set; }

    public string? DefaultOutputMappingJson { get; set; }

    public int Version { get; set; }

    public Guid? ParentSurveyTemplateId { get; set; }

    public string VersionNumber { get; set; } = null!;

    public int VersionStatus { get; set; }

    public string? VersionNotes { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? PublishedBy { get; set; }

    public string? Tags { get; set; }

    public bool IsActive { get; set; }

    public bool IsSystemTemplate { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? ModifiedBy { get; set; }

    public int UsageCount { get; set; }

    public DateTime? LastUsedAt { get; set; }

    public virtual ICollection<SurveyTemplate> InverseParentSurveyTemplate { get; set; } = new List<SurveyTemplate>();

    public virtual SurveyTemplate? ParentSurveyTemplate { get; set; }

    public virtual ICollection<SurveyTemplateDisease> SurveyTemplateDiseases { get; set; } = new List<SurveyTemplateDisease>();

    public virtual ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
}
