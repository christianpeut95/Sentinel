using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReportField
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    public string FieldPath { get; set; } = null!;

    public string DisplayName { get; set; } = null!;

    public string DataType { get; set; } = null!;

    public string? PivotArea { get; set; }

    public string? AggregationType { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCustomField { get; set; }

    public int? CustomFieldDefinitionId { get; set; }

    public virtual ReportDefinition ReportDefinition { get; set; } = null!;
}
