using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class CustomFieldDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Label { get; set; } = null!;

    public string Category { get; set; } = null!;

    public int FieldType { get; set; }

    public bool IsRequired { get; set; }

    public bool IsSearchable { get; set; }

    public bool ShowOnList { get; set; }

    public bool ShowOnCreateEdit { get; set; }

    public bool ShowOnDetails { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? ValidationRules { get; set; }

    public int? LookupTableId { get; set; }

    public bool ShowOnPatientForm { get; set; }

    public bool ShowOnCaseForm { get; set; }

    public virtual ICollection<CaseCustomFieldBoolean> CaseCustomFieldBooleans { get; set; } = new List<CaseCustomFieldBoolean>();

    public virtual ICollection<CaseCustomFieldDate> CaseCustomFieldDates { get; set; } = new List<CaseCustomFieldDate>();

    public virtual ICollection<CaseCustomFieldLookup> CaseCustomFieldLookups { get; set; } = new List<CaseCustomFieldLookup>();

    public virtual ICollection<CaseCustomFieldNumber> CaseCustomFieldNumbers { get; set; } = new List<CaseCustomFieldNumber>();

    public virtual ICollection<CaseCustomFieldString> CaseCustomFieldStrings { get; set; } = new List<CaseCustomFieldString>();

    public virtual ICollection<DiseaseCustomField> DiseaseCustomFields { get; set; } = new List<DiseaseCustomField>();

    public virtual ICollection<Hl7customFieldMapping> Hl7customFieldMappings { get; set; } = new List<Hl7customFieldMapping>();

    public virtual LookupTable? LookupTable { get; set; }

    public virtual ICollection<PatientCustomFieldBoolean> PatientCustomFieldBooleans { get; set; } = new List<PatientCustomFieldBoolean>();

    public virtual ICollection<PatientCustomFieldDate> PatientCustomFieldDates { get; set; } = new List<PatientCustomFieldDate>();

    public virtual ICollection<PatientCustomFieldLookup> PatientCustomFieldLookups { get; set; } = new List<PatientCustomFieldLookup>();

    public virtual ICollection<PatientCustomFieldNumber> PatientCustomFieldNumbers { get; set; } = new List<PatientCustomFieldNumber>();

    public virtual ICollection<PatientCustomFieldString> PatientCustomFieldStrings { get; set; } = new List<PatientCustomFieldString>();
}
