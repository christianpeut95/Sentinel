using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class TestType
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? ExportCode { get; set; }

    public int? DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}
