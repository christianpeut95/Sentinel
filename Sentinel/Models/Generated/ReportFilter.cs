using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ReportFilter
{
    public int Id { get; set; }

    public int ReportDefinitionId { get; set; }

    public string FieldPath { get; set; } = null!;

    public string Operator { get; set; } = null!;

    public string? Value { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsCustomField { get; set; }

    public int? CustomFieldDefinitionId { get; set; }

    public string DataType { get; set; } = null!;

    public string LogicOperator { get; set; } = null!;

    public int? GroupId { get; set; }

    public string GroupLogicOperator { get; set; } = null!;

    public bool IsCollectionQuery { get; set; }

    public string? CollectionSubFilters { get; set; }

    public string? CollectionOperator { get; set; }

    public int? DynamicDateOffset { get; set; }

    public string? DynamicDateOffsetUnit { get; set; }

    public string? DynamicDateType { get; set; }

    public bool IsDynamicDate { get; set; }

    public virtual ReportDefinition ReportDefinition { get; set; } = null!;
}
