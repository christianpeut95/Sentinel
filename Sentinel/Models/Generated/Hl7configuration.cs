using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Hl7configuration
{
    public Guid Id { get; set; }

    public string ConfigurationName { get; set; } = null!;

    public string? SendingFacility { get; set; }

    public string? SendingApplication { get; set; }

    public string? FileDropPath { get; set; }

    public string FilePattern { get; set; } = null!;

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public string CharacterEncoding { get; set; } = null!;

    public Guid? DefaultLaboratoryId { get; set; }

    public bool AutoCreateOrganizations { get; set; }

    public int PatientMatchingStrategy { get; set; }

    public bool AutoCreatePatients { get; set; }

    public bool AutoCreateCases { get; set; }

    public int DuplicateDetectionWindowHours { get; set; }

    public int DuplicateDetectionStrategy { get; set; }

    public string? FieldMappingConfig { get; set; }

    public bool ProcessOnReceipt { get; set; }

    public bool ArchiveProcessedFiles { get; set; }

    public string? ArchivePath { get; set; }

    public bool DeleteAfterArchive { get; set; }

    public bool SendNotificationsOnError { get; set; }

    public string? NotificationEmailAddresses { get; set; }

    public string? DefaultDateFormat { get; set; }

    public string? TimezoneOffset { get; set; }

    public bool RequirePatientIdentifier { get; set; }

    public bool RequireSpecimenCollectionDate { get; set; }

    public bool RequireResultDate { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public bool IsTestMode { get; set; }

    public string? TestModeDescription { get; set; }

    public virtual Organization? DefaultLaboratory { get; set; }

    public virtual ICollection<Hl7configurationDisease> Hl7configurationDiseases { get; set; } = new List<Hl7configurationDisease>();

    public virtual ICollection<Hl7fieldMapping> Hl7fieldMappings { get; set; } = new List<Hl7fieldMapping>();

    public virtual ICollection<Hl7message> Hl7messages { get; set; } = new List<Hl7message>();
}
