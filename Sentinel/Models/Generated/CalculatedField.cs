using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CalculatedField
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    public string Name { get; set; } = null!;

    public string Expression { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public virtual ReportDefinition ReportDefinition { get; set; } = null!;
}
