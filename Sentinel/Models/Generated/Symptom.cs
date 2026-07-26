using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Symptom
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? ExportCode { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public virtual ICollection<CaseSymptom> CaseSymptoms { get; set; } = new List<CaseSymptom>();

    public virtual ICollection<DiseaseSymptom> DiseaseSymptoms { get; set; } = new List<DiseaseSymptom>();

    public virtual ICollection<SurveyFieldMapping> SurveyFieldMappings { get; set; } = new List<SurveyFieldMapping>();
}
