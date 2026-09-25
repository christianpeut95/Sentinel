using Sentinel.HL7Generator.Models;

namespace Sentinel.HL7Generator.Services;

/// <summary>
/// Main service for generating HL7 test messages
/// </summary>
public class HL7GeneratorService
{
    private readonly FakeDataGenerator _fakeDataGenerator;

    public HL7GeneratorService()
    {
        _fakeDataGenerator = new FakeDataGenerator();
    }

    /// <summary>
    /// Generate an HL7 message from a request
    /// </summary>
    public string GenerateMessage(HL7MessageRequest request)
    {
        var builder = new HL7MessageBuilder(request);
        return builder.Build();
    }

    /// <summary>
    /// Generate an HL7 message using a lab template with random patient data
    /// </summary>
    public string GenerateMessage(LabTemplateType templateType, List<BiomarkerResult> biomarkers)
    {
        var request = LabTemplateFactory.CreateTemplate(templateType);
        request.Patient = _fakeDataGenerator.GeneratePatient();
        request.OrderingProvider = _fakeDataGenerator.GenerateProvider();
        request.BiomarkerResults = biomarkers;

        return GenerateMessage(request);
    }

    /// <summary>
    /// Generate multiple HL7 messages with variations
    /// </summary>
    public List<string> GenerateMultipleMessages(
        HL7MessageRequest baseRequest, 
        int count,
        bool varyPatient = true,
        bool varyProvider = true,
        bool varyAccession = true,
        bool varyTimestamp = true)
    {
        var messages = new List<string>();

        for (int i = 0; i < count; i++)
        {
            var request = CloneRequest(baseRequest);

            if (varyPatient)
            {
                request.Patient = _fakeDataGenerator.GeneratePatient();
            }

            if (varyProvider && request.OrderingProvider != null)
            {
                request.OrderingProvider = _fakeDataGenerator.GenerateProvider();
            }

            if (varyAccession)
            {
                request.AccessionNumber = _fakeDataGenerator.GenerateAccessionNumber();
            }

            if (varyTimestamp)
            {
                request.MessageDateTime = DateTime.Now.AddSeconds(i);
                request.CollectionDateTime = DateTime.Now.AddMinutes(-Random.Shared.Next(1, 120));
            }

            request.MessageControlId = _fakeDataGenerator.GenerateMessageControlId();

            messages.Add(GenerateMessage(request));
        }

        return messages;
    }

    /// <summary>
    /// Get a pre-configured template
    /// </summary>
    public HL7MessageRequest GetTemplate(LabTemplateType templateType)
    {
        return LabTemplateFactory.CreateTemplate(templateType);
    }

    /// <summary>
    /// Generate a random patient
    /// </summary>
    public PatientInfo GenerateRandomPatient()
    {
        return _fakeDataGenerator.GeneratePatient();
    }

    /// <summary>
    /// Generate a random provider
    /// </summary>
    public ProviderInfo GenerateRandomProvider()
    {
        return _fakeDataGenerator.GenerateProvider();
    }

    /// <summary>
    /// Generate a unique accession number
    /// </summary>
    public string GenerateAccessionNumber()
    {
        return _fakeDataGenerator.GenerateAccessionNumber();
    }

    /// <summary>
    /// Generate a unique message control ID
    /// </summary>
    public string GenerateMessageControlId()
    {
        return _fakeDataGenerator.GenerateMessageControlId();
    }

    private HL7MessageRequest CloneRequest(HL7MessageRequest source)
    {
        return new HL7MessageRequest
        {
            LabTemplate = source.LabTemplate,
            SendingApplication = source.SendingApplication,
            SendingFacility = source.SendingFacility,
            ReceivingApplication = source.ReceivingApplication,
            ReceivingFacility = source.ReceivingFacility,
            MessageControlId = source.MessageControlId,
            MessageDateTime = source.MessageDateTime,
            HL7Version = source.HL7Version,
            Patient = new PatientInfo
            {
                MRN = source.Patient.MRN,
                FamilyName = source.Patient.FamilyName,
                GivenName = source.Patient.GivenName,
                MiddleName = source.Patient.MiddleName,
                DateOfBirth = source.Patient.DateOfBirth,
                Gender = source.Patient.Gender,
                AddressLine1 = source.Patient.AddressLine1,
                City = source.Patient.City,
                State = source.Patient.State,
                ZipCode = source.Patient.ZipCode,
                PhoneNumber = source.Patient.PhoneNumber
            },
            OrderingProvider = source.OrderingProvider != null ? new ProviderInfo
            {
                FamilyName = source.OrderingProvider.FamilyName,
                GivenName = source.OrderingProvider.GivenName,
                NPI = source.OrderingProvider.NPI,
                Organization = source.OrderingProvider.Organization
            } : null,
            AccessionNumber = source.AccessionNumber,
            CollectionDateTime = source.CollectionDateTime,
            LabComments = source.LabComments,
            BiomarkerResults = source.BiomarkerResults.Select(b => new BiomarkerResult
            {
                TestCode = b.TestCode,
                TestName = b.TestName,
                LOINCCode = b.LOINCCode,
                Result = b.Result,
                ReferenceRange = b.ReferenceRange,
                AbnormalFlag = b.AbnormalFlag,
                QuantitativeValue = b.QuantitativeValue,
                QuantitativeUnit = b.QuantitativeUnit,
                ObservationDateTime = b.ObservationDateTime
            }).ToList()
        };
    }
}
