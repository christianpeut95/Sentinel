using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Gender
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
