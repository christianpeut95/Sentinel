using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Occupation
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? MajorGroupCode { get; set; }

    public string? MajorGroupName { get; set; }

    public string? SubMajorGroupCode { get; set; }

    public string? SubMajorGroupName { get; set; }

    public string? MinorGroupCode { get; set; }

    public string? MinorGroupName { get; set; }

    public string? UnitGroupCode { get; set; }

    public string? UnitGroupName { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
