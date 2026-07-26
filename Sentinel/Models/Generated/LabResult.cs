using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class LabResult
{
    public Guid Id { get; set; }

    public string FriendlyId { get; set; } = null!;

    public Guid? CaseId { get; set; }

    public Guid? LaboratoryId { get; set; }

    public string? AccessionNumber { get; set; }

    public DateTime? SpecimenCollectionDate { get; set; }

    public int? SpecimenTypeId { get; set; }

    public Guid? TestedDiseaseId { get; set; }

    public Guid? OrderingProviderId { get; set; }

    public DateTime? ResultDate { get; set; }

    public int? ResultUnitsId { get; set; }

    public bool IsAmended { get; set; }

    public string? Notes { get; set; }

    public string? LabInterpretation { get; set; }

    public string? AttachmentPath { get; set; }

    public string? AttachmentFileName { get; set; }

    public long? AttachmentSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public Guid? PatientId { get; set; }

    public int? TestResultId { get; set; }

    public int? TestTypeId { get; set; }

    public bool IsMultiplexClone { get; set; }

    public Guid? ParentLabResultId { get; set; }

    public virtual Case? Case { get; set; }

    public virtual ICollection<Hl7message> Hl7messages { get; set; } = new List<Hl7message>();

    public virtual ICollection<LabResult> InverseParentLabResult { get; set; } = new List<LabResult>();

    public virtual ICollection<LabResultMarker> LabResultMarkers { get; set; } = new List<LabResultMarker>();

    public virtual Organization? Laboratory { get; set; }

    public virtual Organization? OrderingProvider { get; set; }

    public virtual LabResult? ParentLabResult { get; set; }

    public virtual Patient? Patient { get; set; }

    public virtual ResultUnit? ResultUnits { get; set; }

    public virtual SpecimenType? SpecimenType { get; set; }

    public virtual TestResult? TestResult { get; set; }

    public virtual TestType? TestType { get; set; }

    public virtual Disease? TestedDisease { get; set; }
}
