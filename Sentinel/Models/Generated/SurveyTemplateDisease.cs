using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class SurveyTemplateDisease
{
    public Guid Id { get; set; }

    public Guid SurveyTemplateId { get; set; }

    public Guid DiseaseId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual SurveyTemplate SurveyTemplate { get; set; } = null!;
}
