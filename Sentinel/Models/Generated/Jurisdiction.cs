using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Jurisdiction
{
    public int Id { get; set; }

    public int JurisdictionTypeId { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }

    public string? Description { get; set; }

    public int? ParentJurisdictionId { get; set; }

    public string? BoundaryData { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public long? Population { get; set; }

    public int? PopulationYear { get; set; }

    public string? PopulationSource { get; set; }

    public virtual ICollection<Case> CaseJurisdiction1s { get; set; } = new List<Case>();

    public virtual ICollection<Case> CaseJurisdiction2s { get; set; } = new List<Case>();

    public virtual ICollection<Case> CaseJurisdiction3s { get; set; } = new List<Case>();

    public virtual ICollection<Case> CaseJurisdiction4s { get; set; } = new List<Case>();

    public virtual ICollection<Case> CaseJurisdiction5s { get; set; } = new List<Case>();

    public virtual ICollection<Jurisdiction> InverseParentJurisdiction { get; set; } = new List<Jurisdiction>();

    public virtual JurisdictionType JurisdictionType { get; set; } = null!;

    public virtual Jurisdiction? ParentJurisdiction { get; set; }

    public virtual ICollection<Patient> PatientJurisdiction1s { get; set; } = new List<Patient>();

    public virtual ICollection<Patient> PatientJurisdiction2s { get; set; } = new List<Patient>();

    public virtual ICollection<Patient> PatientJurisdiction3s { get; set; } = new List<Patient>();

    public virtual ICollection<Patient> PatientJurisdiction4s { get; set; } = new List<Patient>();

    public virtual ICollection<Patient> PatientJurisdiction5s { get; set; } = new List<Patient>();
}
