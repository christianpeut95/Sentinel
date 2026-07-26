using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7configurationDisease
{
    public Guid Id { get; set; }

    public Guid ConfigurationId { get; set; }

    public Guid DiseaseId { get; set; }

    public bool IsDefault { get; set; }

    public int Priority { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public virtual Hl7configuration Configuration { get; set; } = null!;

    public virtual Disease Disease { get; set; } = null!;
}
