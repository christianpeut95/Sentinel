using Sentinel.HL7Generator.Models;

namespace Sentinel.HL7Generator.Services;

/// <summary>
/// Factory for creating lab-specific HL7 message templates
/// </summary>
public class LabTemplateFactory
{
    public static HL7MessageRequest CreateTemplate(LabTemplateType templateType)
    {
        return templateType switch
        {
            LabTemplateType.QuestDiagnostics => CreateQuestTemplate(),
            LabTemplateType.LabCorp => CreateLabCorpTemplate(),
            LabTemplateType.HospitalLab => CreateHospitalLabTemplate(),
            LabTemplateType.ReferenceLab => CreateReferenceLabTemplate(),
            LabTemplateType.Generic => CreateGenericTemplate(),
            _ => CreateGenericTemplate()
        };
    }

    private static HL7MessageRequest CreateQuestTemplate()
    {
        return new HL7MessageRequest
        {
            LabTemplate = LabTemplateType.QuestDiagnostics,
            SendingApplication = "QUEST",
            SendingFacility = "QUEST DIAGNOSTICS",
            ReceivingApplication = "SENTINEL",
            ReceivingFacility = "HOSPITAL",
            HL7Version = "2.5.1",
            MessageDateTime = DateTime.Now
        };
    }

    private static HL7MessageRequest CreateLabCorpTemplate()
    {
        return new HL7MessageRequest
        {
            LabTemplate = LabTemplateType.LabCorp,
            SendingApplication = "LABCORP",
            SendingFacility = "LABCORP USA",
            ReceivingApplication = "SENTINEL",
            ReceivingFacility = "HOSPITAL",
            HL7Version = "2.5",
            MessageDateTime = DateTime.Now
        };
    }

    private static HL7MessageRequest CreateHospitalLabTemplate()
    {
        return new HL7MessageRequest
        {
            LabTemplate = LabTemplateType.HospitalLab,
            SendingApplication = "LAB",
            SendingFacility = "MAIN HOSPITAL",
            ReceivingApplication = "SENTINEL",
            ReceivingFacility = "HOSPITAL",
            HL7Version = "2.3",
            MessageDateTime = DateTime.Now
        };
    }

    private static HL7MessageRequest CreateReferenceLabTemplate()
    {
        return new HL7MessageRequest
        {
            LabTemplate = LabTemplateType.ReferenceLab,
            SendingApplication = "REFLAB",
            SendingFacility = "REFERENCE LABORATORY",
            ReceivingApplication = "SENTINEL",
            ReceivingFacility = "HOSPITAL",
            HL7Version = "2.5.1",
            MessageDateTime = DateTime.Now
        };
    }

    private static HL7MessageRequest CreateGenericTemplate()
    {
        return new HL7MessageRequest
        {
            LabTemplate = LabTemplateType.Generic,
            SendingApplication = "LAB",
            SendingFacility = "TESTFACILITY",
            ReceivingApplication = "SENTINEL",
            ReceivingFacility = "HOSPITAL",
            HL7Version = "2.5.1",
            MessageDateTime = DateTime.Now
        };
    }

    public static string GetTemplateDescription(LabTemplateType templateType)
    {
        return templateType switch
        {
            LabTemplateType.QuestDiagnostics => "Quest Diagnostics standard format with LOINC codes (HL7 2.5.1)",
            LabTemplateType.LabCorp => "LabCorp format with mix of LOINC and local codes (HL7 2.5)",
            LabTemplateType.HospitalLab => "Basic hospital lab format with minimal fields (HL7 2.3)",
            LabTemplateType.ReferenceLab => "Comprehensive reference lab format with all optional fields (HL7 2.5.1)",
            LabTemplateType.Generic => "Generic HL7 2.5.1 format (customizable)",
            _ => "Unknown template"
        };
    }
}
