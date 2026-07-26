using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LookupValue
{
    public int Id { get; set; }

    public int LookupTableId { get; set; }

    public string Value { get; set; } = null!;

    public string DisplayText { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<CaseCustomFieldLookup> CaseCustomFieldLookups { get; set; } = new List<CaseCustomFieldLookup>();

    public virtual LookupTable LookupTable { get; set; } = null!;

    public virtual ICollection<PatientCustomFieldLookup> PatientCustomFieldLookups { get; set; } = new List<PatientCustomFieldLookup>();
}
