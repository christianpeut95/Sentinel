using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Ancestry
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
