using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class DiseaseSymptom
{
    public int Id { get; set; }

    public Guid DiseaseId { get; set; }

    public int SymptomId { get; set; }

    public bool IsCommon { get; set; }

    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public virtual Disease Disease { get; set; } = null!;

    public virtual Symptom Symptom { get; set; } = null!;
}
