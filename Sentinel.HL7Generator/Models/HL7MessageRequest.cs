namespace Sentinel.HL7Generator.Models;

/// <summary>
/// Request model for generating an HL7 message
/// </summary>
public class HL7MessageRequest
{
    public LabTemplateType LabTemplate { get; set; } = LabTemplateType.Generic;

    // Header Information
    public string SendingApplication { get; set; } = "LAB";
    public string SendingFacility { get; set; } = "TESTFACILITY";
    public string ReceivingApplication { get; set; } = "SENTINEL";
    public string ReceivingFacility { get; set; } = "HOSPITAL";
    public string MessageControlId { get; set; } = string.Empty;
    public DateTime MessageDateTime { get; set; } = DateTime.Now;
    public string HL7Version { get; set; } = "2.5.1";

    // Patient Information
    public PatientInfo Patient { get; set; } = new();

    // Provider Information
    public ProviderInfo? OrderingProvider { get; set; }

    // Order/Specimen Information
    public string AccessionNumber { get; set; } = string.Empty;
    public DateTime CollectionDateTime { get; set; } = DateTime.Now;
    public string SpecimenType { get; set; } = "URINE"; // URINE, BLOOD, SWAB, etc.
    public string? LabComments { get; set; }

    // Results
    public List<BiomarkerResult> BiomarkerResults { get; set; } = new();
}

public class PatientInfo
{
    public string MRN { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = "U"; // M, F, U, O
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? PhoneNumber { get; set; }
}

public class ProviderInfo
{
    public string FamilyName { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string? NPI { get; set; }
    public string? Organization { get; set; }
}

public class BiomarkerResult
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string? LOINCCode { get; set; }
    public string TestType { get; set; } = "NAAT"; // NAAT, PCR, Culture, Antibody, Antigen, etc.
    public string Result { get; set; } = string.Empty; // POSITIVE, NEGATIVE, DETECTED, etc.
    public string ReferenceRange { get; set; } = "NEGATIVE";
    public string? AbnormalFlag { get; set; } // A = abnormal, N = normal
    public decimal? QuantitativeValue { get; set; }
    public string? QuantitativeUnit { get; set; }
    public DateTime? ObservationDateTime { get; set; }
}
