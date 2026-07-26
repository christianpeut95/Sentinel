using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TaskType
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Code { get; set; }

    public string? IconClass { get; set; }

    public string? ColorClass { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public bool IsInterviewTask { get; set; }

    public virtual ICollection<CaseTask> CaseTasks { get; set; } = new List<CaseTask>();

    public virtual ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
}
