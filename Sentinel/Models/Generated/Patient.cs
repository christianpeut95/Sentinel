using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Patient
{
    public Guid Id { get; set; }

    public string FriendlyId { get; set; } = null!;

    public string GivenName { get; set; } = null!;

    public string FamilyName { get; set; } = null!;

    public DateTime? DateOfBirth { get; set; }

    public int? SexAtBirthId { get; set; }

    public int? GenderId { get; set; }

    public string? HomePhone { get; set; }

    public string? MobilePhone { get; set; }

    public string? EmailAddress { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? PostalCode { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public int? CountryOfBirthId { get; set; }

    public int? LanguageSpokenAtHomeId { get; set; }

    public int? AncestryId { get; set; }

    public int? AtsiStatusId { get; set; }

    public int? OccupationId { get; set; }

    public bool IsDeceased { get; set; }

    public DateTime? DateOfDeath { get; set; }

    public int? Jurisdiction1Id { get; set; }

    public int? Jurisdiction2Id { get; set; }

    public int? Jurisdiction3Id { get; set; }

    public int? Jurisdiction4Id { get; set; }

    public int? Jurisdiction5Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public int? StateId { get; set; }

    public virtual Ancestry? Ancestry { get; set; }

    public virtual AtsiStatus? AtsiStatus { get; set; }

    public virtual ICollection<Case> Cases { get; set; } = new List<Case>();

    public virtual Country? CountryOfBirth { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual Gender? Gender { get; set; }

    public virtual ICollection<GeocodingQueueItem> GeocodingQueueItems { get; set; } = new List<GeocodingQueueItem>();

    public virtual ICollection<Hl7message> Hl7messages { get; set; } = new List<Hl7message>();

    public virtual Jurisdiction? Jurisdiction1 { get; set; }

    public virtual Jurisdiction? Jurisdiction2 { get; set; }

    public virtual Jurisdiction? Jurisdiction3 { get; set; }

    public virtual Jurisdiction? Jurisdiction4 { get; set; }

    public virtual Jurisdiction? Jurisdiction5 { get; set; }

    public virtual ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();

    public virtual Language? LanguageSpokenAtHome { get; set; }

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual Occupation? Occupation { get; set; }

    public virtual ICollection<PatientCustomFieldBoolean> PatientCustomFieldBooleans { get; set; } = new List<PatientCustomFieldBoolean>();

    public virtual ICollection<PatientCustomFieldDate> PatientCustomFieldDates { get; set; } = new List<PatientCustomFieldDate>();

    public virtual ICollection<PatientCustomFieldLookup> PatientCustomFieldLookups { get; set; } = new List<PatientCustomFieldLookup>();

    public virtual ICollection<PatientCustomFieldNumber> PatientCustomFieldNumbers { get; set; } = new List<PatientCustomFieldNumber>();

    public virtual ICollection<PatientCustomFieldString> PatientCustomFieldStrings { get; set; } = new List<PatientCustomFieldString>();

    public virtual ICollection<ReviewQueue> ReviewQueues { get; set; } = new List<ReviewQueue>();

    public virtual SexAtBirth? SexAtBirth { get; set; }

    public virtual State? State { get; set; }
}
