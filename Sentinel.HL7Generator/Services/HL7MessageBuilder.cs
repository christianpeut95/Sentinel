using System.Text;
using Sentinel.HL7Generator.Models;

namespace Sentinel.HL7Generator.Services;

/// <summary>
/// Builds HL7 messages from request data
/// </summary>
public class HL7MessageBuilder
{
    private readonly HL7MessageRequest _request;
    private readonly FakeDataGenerator _fakeData;
    private readonly StringBuilder _message;
    private const string FieldSeparator = "|";
    private const string ComponentSeparator = "^";
    private const string RepetitionSeparator = "~";
    private const string EscapeCharacter = "\\";
    private const string SubcomponentSeparator = "&";

    public HL7MessageBuilder(HL7MessageRequest request)
    {
        _request = request;
        _fakeData = new FakeDataGenerator();
        _message = new StringBuilder();

        // Auto-fill missing required fields
        if (string.IsNullOrEmpty(_request.MessageControlId))
        {
            _request.MessageControlId = _fakeData.GenerateMessageControlId();
        }

        if (string.IsNullOrEmpty(_request.AccessionNumber))
        {
            _request.AccessionNumber = _fakeData.GenerateAccessionNumber();
        }
    }

    public string Build()
    {
        _message.Clear();

        BuildMSH();
        BuildPID();
        BuildOBR();
        BuildOBXSegments();

        return _message.ToString();
    }

    private void BuildMSH()
    {
        var timestamp = _request.MessageDateTime.ToString("yyyyMMddHHmmss");

        _message.Append("MSH");
        _message.Append(FieldSeparator);
        _message.Append($"{ComponentSeparator}{RepetitionSeparator}{EscapeCharacter}{SubcomponentSeparator}");
        _message.Append(FieldSeparator);
        _message.Append(_request.SendingApplication);
        _message.Append(FieldSeparator);
        _message.Append(_request.SendingFacility);
        _message.Append(FieldSeparator);
        _message.Append(_request.ReceivingApplication);
        _message.Append(FieldSeparator);
        _message.Append(_request.ReceivingFacility);
        _message.Append(FieldSeparator);
        _message.Append(timestamp);
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Security (MSH-8)
        _message.Append(FieldSeparator);
        _message.Append("ORU^R01"); // Message Type (MSH-9)
        _message.Append(FieldSeparator);
        _message.Append(_request.MessageControlId); // Message Control ID (MSH-10)
        _message.Append(FieldSeparator);
        _message.Append("P"); // Processing ID (MSH-11) - P = Production, T = Test
        _message.Append(FieldSeparator);
        _message.Append(_request.HL7Version); // Version ID (MSH-12)
        _message.AppendLine();
    }

    private void BuildPID()
    {
        var patient = _request.Patient;

        _message.Append("PID");
        _message.Append(FieldSeparator);
        _message.Append("1"); // Set ID (PID-1)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Patient ID (External) (PID-2) - deprecated
        _message.Append(FieldSeparator);
        _message.Append($"{patient.MRN}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}MRN"); // Patient Identifier List (PID-3)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Alternate Patient ID (PID-4)
        _message.Append(FieldSeparator);
        _message.Append($"{patient.FamilyName}{ComponentSeparator}{patient.GivenName}{ComponentSeparator}{patient.MiddleName ?? string.Empty}"); // Patient Name (PID-5)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Mother's Maiden Name (PID-6)
        _message.Append(FieldSeparator);
        _message.Append(patient.DateOfBirth.ToString("yyyyMMdd")); // Date of Birth (PID-7)
        _message.Append(FieldSeparator);
        _message.Append(patient.Gender); // Sex (PID-8)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Patient Alias (PID-9)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Race (PID-10)
        _message.Append(FieldSeparator);
        // Patient Address (PID-11)
        if (!string.IsNullOrEmpty(patient.AddressLine1))
        {
            _message.Append($"{patient.AddressLine1}{ComponentSeparator}{ComponentSeparator}{patient.City}{ComponentSeparator}{patient.State}{ComponentSeparator}{patient.ZipCode}");
        }
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // County Code (PID-12)
        _message.Append(FieldSeparator);
        _message.Append(patient.PhoneNumber ?? string.Empty); // Phone Number - Home (PID-13)
        _message.AppendLine();
    }

    private void BuildOBR()
    {
        var collectionTime = _request.CollectionDateTime.ToString("yyyyMMddHHmm");
        var firstResult = _request.BiomarkerResults.FirstOrDefault();

        _message.Append("OBR");
        _message.Append(FieldSeparator);
        _message.Append("1"); // Set ID (OBR-1)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Placer Order Number (OBR-2)
        _message.Append(FieldSeparator);
        _message.Append(_request.AccessionNumber); // Filler Order Number (OBR-3)
        _message.Append(FieldSeparator);
        // Universal Service ID (OBR-4)
        if (firstResult != null)
        {
            _message.Append($"{firstResult.TestCode}{ComponentSeparator}{firstResult.TestName}{ComponentSeparator}LN");
        }
        else
        {
            _message.Append($"LAB{ComponentSeparator}LABORATORY PANEL{ComponentSeparator}LN");
        }
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Priority (OBR-5)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Requested Date/Time (OBR-6)
        _message.Append(FieldSeparator);
        _message.Append(collectionTime); // Observation Date/Time (OBR-7)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Observation End Date/Time (OBR-8)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Collection Volume (OBR-9)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Collector Identifier (OBR-10)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Specimen Action Code (OBR-11)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Danger Code (OBR-12)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Relevant Clinical Info (OBR-13)
        _message.Append(FieldSeparator);
        _message.Append(collectionTime); // Specimen Received Date/Time (OBR-14)
        _message.Append(FieldSeparator);
        // Specimen Source (OBR-15) - includes specimen type
        _message.Append($"{_request.SpecimenType}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{_request.SpecimenType}");
        _message.Append(FieldSeparator);
        // Ordering Provider (OBR-16)
        if (_request.OrderingProvider != null)
        {
            var provider = _request.OrderingProvider;
            _message.Append($"{provider.FamilyName}{ComponentSeparator}{provider.GivenName}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{ComponentSeparator}{provider.NPI}");
        }
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Order Callback Phone Number (OBR-17)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Placer Field 1 (OBR-18)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Placer Field 2 (OBR-19)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Filler Field 1 (OBR-20)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Filler Field 2 (OBR-21)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Results Rpt/Status Chng - Date/Time (OBR-22)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Charge to Practice (OBR-23)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Diagnostic Serv Sect ID (OBR-24)
        _message.Append(FieldSeparator);
        _message.Append("F"); // Result Status (OBR-25) - F = Final
        _message.AppendLine();
    }

    private void BuildOBXSegments()
    {
        for (int i = 0; i < _request.BiomarkerResults.Count; i++)
        {
            BuildOBX(i + 1, _request.BiomarkerResults[i]);
        }
    }

    private void BuildOBX(int setId, BiomarkerResult result)
    {
        var observationTime = (result.ObservationDateTime ?? _request.CollectionDateTime).ToString("yyyyMMddHHmmss");

        _message.Append("OBX");
        _message.Append(FieldSeparator);
        _message.Append(setId.ToString()); // Set ID (OBX-1)
        _message.Append(FieldSeparator);
        _message.Append(result.QuantitativeValue.HasValue ? "NM" : "ST"); // Value Type (OBX-2) - ST = String, NM = Numeric
        _message.Append(FieldSeparator);
        // Observation Identifier (OBX-3)
        _message.Append($"{result.TestCode}{ComponentSeparator}{result.TestName}{ComponentSeparator}LN");
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Observation Sub-ID (OBX-4)
        _message.Append(FieldSeparator);
        // Observation Value (OBX-5)
        if (result.QuantitativeValue.HasValue)
        {
            _message.Append(result.QuantitativeValue.Value.ToString("F2"));
        }
        else
        {
            _message.Append(result.Result);
        }
        _message.Append(FieldSeparator);
        // Units (OBX-6)
        if (result.QuantitativeValue.HasValue && !string.IsNullOrEmpty(result.QuantitativeUnit))
        {
            _message.Append(result.QuantitativeUnit);
        }
        _message.Append(FieldSeparator);
        _message.Append(result.ReferenceRange); // Reference Range (OBX-7)
        _message.Append(FieldSeparator);
        _message.Append(result.AbnormalFlag ?? string.Empty); // Abnormal Flags (OBX-8)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Probability (OBX-9)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Nature of Abnormal Test (OBX-10)
        _message.Append(FieldSeparator);
        _message.Append("F"); // Observation Result Status (OBX-11) - F = Final
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Effective Date of Reference Range (OBX-12)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // User Defined Access Checks (OBX-13)
        _message.Append(FieldSeparator);
        _message.Append(observationTime); // Date/Time of the Observation (OBX-14)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Producer's ID (OBX-15)
        _message.Append(FieldSeparator);
        _message.Append(string.Empty); // Responsible Observer (OBX-16)
        _message.Append(FieldSeparator);
        // Observation Method (OBX-17) - includes test type/method
        _message.Append($"{result.TestType}{ComponentSeparator}{result.TestType}");
        _message.AppendLine();
    }
}
