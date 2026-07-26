using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Language
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
