using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7customFieldMapping
{
    public int Id { get; set; }

    public Guid DiseaseId { get; set; }

    public string Hl7testCode { get; set; } = null!;

    public string? TestCodeDescription { get; set; }

    public int CustomFieldDefinitionId { get; set; }

    public bool ExtractQualitativeResult { get; set; }

    public bool ExtractQuantitativeResult { get; set; }

    public string? ValueTransformation { get; set; }

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;

    public virtual Disease Disease { get; set; } = null!;
}
