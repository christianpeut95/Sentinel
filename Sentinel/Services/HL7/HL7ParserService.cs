using Microsoft.EntityFrameworkCore;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V251.Message;
using NHapi.Model.V251.Segment;
using V25_ORU_R01 = NHapi.Model.V25.Message.ORU_R01;
using V25_PID = NHapi.Model.V25.Segment.PID;
using V25_OBR = NHapi.Model.V25.Segment.OBR;
using V25_OBX = NHapi.Model.V25.Segment.OBX;
using V25_SPM = NHapi.Model.V25.Segment.SPM;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services.HL7;

public class HL7ParserService : IHL7ParserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HL7ParserService> _logger;
    private readonly PipeParser _parser;

    public HL7ParserService(
        ApplicationDbContext context,
        ILogger<HL7ParserService> logger)
    {
        _context = context;
        _logger = logger;
        _parser = new PipeParser();
    }

    public async Task<HL7Message> ParseMessageAsync(string rawMessage, Guid? configurationId = null, CancellationToken cancellationToken = default)
    {
        var hl7Message = new HL7Message
        {
            RawMessage = rawMessage,
            ReceivedAt = DateTime.UtcNow,
            Status = HL7ProcessingStatus.Received,
            ConfigurationId = configurationId
        };

        try
        {
            hl7Message.Status = HL7ProcessingStatus.Parsing;

            // Parse the message
            var parsedMessage = _parser.Parse(rawMessage);

            // Extract MSH (Message Header) segment
            var msh = GetMSHSegment(parsedMessage);
            if (msh != null)
            {
                hl7Message.MessageControlId = msh.MessageControlID.Value;
                hl7Message.SendingFacility = msh.SendingFacility.NamespaceID.Value;
                hl7Message.SendingApplication = msh.SendingApplication.NamespaceID.Value;
                hl7Message.MessageType = $"{msh.MessageType.MessageCode.Value}^{msh.MessageType.TriggerEvent.Value}";
                var parsedDateTime = ParseHL7DateTime(msh.DateTimeOfMessage.Time.Value);
                hl7Message.MessageDateTime = parsedDateTime ?? DateTime.UtcNow;
                hl7Message.HL7Version = msh.VersionID.VersionID.Value;

                // CHECK FOR DUPLICATE: If MessageControlId is not empty, check if we've already processed this message
                if (!string.IsNullOrWhiteSpace(hl7Message.MessageControlId))
                {
                    var existingMessage = await _context.HL7Messages
                        .Include(m => m.Segments)  // ← CRITICAL: Load segments for extraction
                        .Where(m => m.MessageControlId == hl7Message.MessageControlId &&
                                    m.SendingFacility == hl7Message.SendingFacility)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingMessage != null)
                    {
                        _logger.LogWarning(
                            "Duplicate HL7 message detected: MessageControlId={MessageControlId}, SendingFacility={SendingFacility}, " +
                            "Original received at {ReceivedAt}, Status={Status}",
                            hl7Message.MessageControlId,
                            hl7Message.SendingFacility,
                            existingMessage.ReceivedAt,
                            existingMessage.Status);

                        // Return the existing message instead of creating a duplicate
                        return existingMessage;
                    }
                }
                else
                {
                    _logger.LogWarning("HL7 message has empty MessageControlId - duplicate detection cannot be performed");
                }
            }

            // Parse segments
            await ParseSegmentsAsync(hl7Message, parsedMessage, cancellationToken);

            hl7Message.Status = HL7ProcessingStatus.ParsedSuccessfully;
            hl7Message.ParsedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse HL7 message: {MessageControlId}", hl7Message.MessageControlId);
            hl7Message.Status = HL7ProcessingStatus.ParsingFailed;
            hl7Message.ErrorMessage = ex.Message;

            // Create parsing issue
            hl7Message.ParsingIssues.Add(new HL7ParsingIssue
            {
                IssueType = HL7IssueType.InvalidFormat,
                Severity = HL7IssueSeverity.Critical,
                Description = ex.Message,
                SegmentType = "MSH",
                RawValue = rawMessage.Length > 500 ? rawMessage.Substring(0, 500) : rawMessage
            });
        }

        _context.HL7Messages.Add(hl7Message);
        await _context.SaveChangesAsync(cancellationToken);

        // CRITICAL: Reload message with Segments to ensure the collection is populated for extraction
        // After SaveChanges, the Segments collection might be cleared/detached from tracking
        var reloadedMessage = await _context.HL7Messages
            .Include(m => m.Segments)
            .FirstOrDefaultAsync(m => m.Id == hl7Message.Id, cancellationToken);

        return reloadedMessage ?? hl7Message;
    }

    public async Task<HL7ParseResult> ParseMessagePreviewAsync(string rawMessage, CancellationToken cancellationToken = default)
    {
        var result = new HL7ParseResult { IsValid = false };

        try
        {
            _logger.LogInformation("ParseMessagePreviewAsync: Parsing raw message (length: {Length})", rawMessage.Length);
            _logger.LogDebug("Raw message first 200 chars: {Preview}", rawMessage.Substring(0, Math.Min(200, rawMessage.Length)));

            var parsedMessage = _parser.Parse(rawMessage);
            result.IsValid = true;

            _logger.LogInformation("Message parsed successfully. Type: {Type}", parsedMessage.GetType().Name);
            _logger.LogInformation("Message full type: {FullType}", parsedMessage.GetType().FullName);
            _logger.LogInformation("Message assembly: {Assembly}", parsedMessage.GetType().Assembly.FullName);
            _logger.LogInformation("Checking if message is ORU_R01...");
            _logger.LogInformation("ORU_R01 type: {ORUType}", typeof(ORU_R01).FullName);
            _logger.LogInformation("ORU_R01 assembly: {ORUAssembly}", typeof(ORU_R01).Assembly.FullName);
            _logger.LogInformation("Type check result: {IsORU}", parsedMessage is ORU_R01);
            _logger.LogInformation("Type name equals: {NameEquals}", parsedMessage.GetType().Name == "ORU_R01");
            _logger.LogInformation("Type equals: {TypeEquals}", parsedMessage.GetType() == typeof(ORU_R01));

            // Extract MSH
            var msh = GetMSHSegment(parsedMessage);
            if (msh != null)
            {
                result.MessageControlId = msh.MessageControlID.Value;
                result.SendingFacility = msh.SendingFacility.NamespaceID.Value;
                result.SendingApplication = msh.SendingApplication.NamespaceID.Value;
                result.MessageType = $"{msh.MessageType.MessageCode.Value}^{msh.MessageType.TriggerEvent.Value}";
                result.MessageDateTime = ParseHL7DateTime(msh.DateTimeOfMessage.Time.Value);
                result.HL7Version = msh.VersionID.VersionID.Value;

                _logger.LogInformation("MSH extracted: MessageType={MessageType}, Version={Version}", result.MessageType, result.HL7Version);
            }
            else
            {
                _logger.LogWarning("MSH segment not found or could not be extracted");
            }

            // Extract patient data (PID segment)
            // Support both V2.5 and V2.5.1 message types
            bool isV251 = parsedMessage is ORU_R01;
            bool isV25 = parsedMessage is V25_ORU_R01;

            _logger.LogInformation("Message version check: V251={IsV251}, V25={IsV25}", isV251, isV25);

            if (isV251)
            {
                _logger.LogInformation("Message is ORU_R01 V2.5.1, extracting patient/order/specimen/result data...");
                var oruMessage = (ORU_R01)parsedMessage;

                try
                {
                    var pid = oruMessage.GetPATIENT_RESULT().PATIENT.PID;
                    result.PatientData = ExtractPatientData(pid);
                    _logger.LogInformation("Patient data extracted: {Count} fields", result.PatientData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract patient data");
                }

                try
                {
                    // Extract order and results
                    var obr = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR;
                    result.OrderData = ExtractOrderData(obr);
                    _logger.LogInformation("Order data extracted: {Count} fields", result.OrderData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract order data");
                }

                // Extract specimen data (SPM segment if available)
                try
                {
                    var spmCount = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().SPECIMENRepetitionsUsed;
                    _logger.LogInformation("SPM repetitions count: {Count}", spmCount);

                    if (spmCount > 0)
                    {
                        var spm = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().GetSPECIMEN(0).SPM;
                        result.SpecimenData = ExtractSpecimenData(spm);
                        _logger.LogInformation("Specimen data extracted: {Count} fields", result.SpecimenData.Count);
                    }
                    else
                    {
                        _logger.LogInformation("No SPM segments present in message");
                    }
                }
                catch (Exception ex)
                {
                    // SPM segment might not be present in all messages
                    _logger.LogWarning(ex, "Could not extract specimen data (SPM may not be present)");
                }

                // Extract all OBX segments
                try
                {
                    var obxCount = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBSERVATIONRepetitionsUsed;
                    _logger.LogInformation("OBX repetitions count: {Count}", obxCount);

                    for (int i = 0; i < obxCount; i++)
                    {
                        var obx = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().GetOBSERVATION(i).OBX;
                        result.ResultData.Add(ExtractResultData(obx));
                    }
                    _logger.LogInformation("Result data extracted: {Count} observations", result.ResultData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract result data");
                }
            }
            else if (isV25)
            {
                _logger.LogInformation("Message is ORU_R01 V2.5, extracting patient/order/specimen/result data...");
                var oruMessage = (V25_ORU_R01)parsedMessage;

                try
                {
                    var pid = oruMessage.GetPATIENT_RESULT().PATIENT.PID;
                    result.PatientData = ExtractPatientDataV25(pid);
                    _logger.LogInformation("Patient data extracted: {Count} fields", result.PatientData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract patient data");
                }

                try
                {
                    // Extract order and results
                    var obr = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBR;
                    result.OrderData = ExtractOrderDataV25(obr);
                    _logger.LogInformation("Order data extracted: {Count} fields", result.OrderData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract order data");
                }

                // Extract specimen data (SPM segment if available)
                try
                {
                    var spmCount = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().SPECIMENRepetitionsUsed;
                    _logger.LogInformation("SPM repetitions count: {Count}", spmCount);

                    if (spmCount > 0)
                    {
                        var spm = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().GetSPECIMEN(0).SPM;
                        result.SpecimenData = ExtractSpecimenDataV25(spm);
                        _logger.LogInformation("Specimen data extracted: {Count} fields", result.SpecimenData.Count);
                    }
                    else
                    {
                        _logger.LogInformation("No SPM segments present in message");
                    }
                }
                catch (Exception ex)
                {
                    // SPM segment might not be present in all messages
                    _logger.LogWarning(ex, "Could not extract specimen data (SPM may not be present)");
                }

                // Extract all OBX segments
                try
                {
                    var obxCount = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().OBSERVATIONRepetitionsUsed;
                    _logger.LogInformation("OBX repetitions count: {Count}", obxCount);

                    for (int i = 0; i < obxCount; i++)
                    {
                        var obx = oruMessage.GetPATIENT_RESULT().GetORDER_OBSERVATION().GetOBSERVATION(i).OBX;
                        var obxData = ExtractResultDataV25(obx);

                        // Log each OBX result for debugging
                        _logger.LogDebug("OBX[{Index}] extracted: TestCode={TestCode}, TestName={TestName}, Result={Result}", 
                            i, 
                            obxData.GetValueOrDefault("TestCode", "(none)"),
                            obxData.GetValueOrDefault("TestName", "(none)"),
                            obxData.GetValueOrDefault("Result", "(none)"));

                        result.ResultData.Add(obxData);
                    }
                    _logger.LogInformation("Result data extracted: {Count} observations", result.ResultData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract result data");
                }
            }
            else
            {
                _logger.LogWarning("⚠️ ⚠️ ⚠️ Message is NOT ORU_R01 type! Type is: {Type}", parsedMessage.GetType().Name);
                _logger.LogWarning("Cannot extract patient/order/result data from non-ORU messages");
                result.Warnings.Add($"Message type {parsedMessage.GetType().Name} is not supported for field extraction. Only ORU^R01 messages are currently supported.");
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Preview parsing failed for HL7 message");
        }

        return result;
    }

    public async Task<HL7ValidationResult> ValidateMessageAsync(string rawMessage, CancellationToken cancellationToken = default)
    {
        var result = new HL7ValidationResult { IsValid = false };

        try
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                result.Errors.Add("Message is empty");
                return result;
            }

            if (!rawMessage.StartsWith("MSH"))
            {
                result.Errors.Add("Message must start with MSH segment");
                return result;
            }

            var parsedMessage = _parser.Parse(rawMessage);
            result.IsValid = true;

            var msh = GetMSHSegment(parsedMessage);
            if (msh != null)
            {
                result.MessageType = $"{msh.MessageType.MessageCode.Value}^{msh.MessageType.TriggerEvent.Value}";
                result.HL7Version = msh.VersionID.VersionID.Value;

                // Check for ORU^R01 (lab results) message type
                if (msh.MessageType.MessageCode.Value != "ORU" || msh.MessageType.TriggerEvent.Value != "R01")
                {
                    result.Warnings.Add($"Message type {result.MessageType} is not ORU^R01 (lab results). Processing may be limited.");
                }

                // Validate required fields
                if (string.IsNullOrEmpty(msh.MessageControlID.Value))
                {
                    result.Warnings.Add("Message Control ID is missing");
                }

                if (string.IsNullOrEmpty(msh.SendingFacility.NamespaceID.Value))
                {
                    result.Warnings.Add("Sending Facility is missing");
                }
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public string? GetSegmentValue(HL7Message message, string segmentType)
    {
        var segment = message.Segments.FirstOrDefault(s => s.SegmentType == segmentType);
        return segment?.RawSegment;
    }

    #region Private Helper Methods

    private async Task ParseSegmentsAsync(HL7Message hl7Message, IMessage parsedMessage, CancellationToken cancellationToken)
    {
        int sequenceNumber = 0;

        try
        {
            // Split the raw message into lines to get raw segments
            var lines = hl7Message.RawMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Extract segment type (first 3 characters)
                var segmentType = line.Length >= 3 ? line.Substring(0, 3) : line;

                hl7Message.Segments.Add(new HL7MessageSegment
                {
                    SegmentType = segmentType,
                    SequenceNumber = ++sequenceNumber,
                    RawSegment = line,
                    ParsedData = line,
                    IsParsed = true,
                    ParsedAt = DateTime.UtcNow
                });
            }

            // Set SetId for OBX segments
            var obxSegments = hl7Message.Segments.Where(s => s.SegmentType == "OBX").ToList();
            for (int i = 0; i < obxSegments.Count; i++)
            {
                obxSegments[i].SetId = i;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing segments for message {MessageControlId}", hl7Message.MessageControlId);
            throw;
        }

        await Task.CompletedTask;
    }

    private MSH? GetMSHSegment(IMessage message)
    {
        try
        {
            if (message is ORU_R01 oruMessage)
                return oruMessage.MSH;

            // Try to get MSH using reflection for other message types
            var mshProperty = message.GetType().GetProperty("MSH");
            return mshProperty?.GetValue(message) as MSH;
        }
        catch
        {
            return null;
        }
    }

    private Dictionary<string, string> ExtractPatientData(PID pid)
    {
        var data = new Dictionary<string, string>();

        try
        {
            // Patient ID
            if (pid.GetPatientIdentifierList().Length > 0)
            {
                data["PatientId"] = pid.GetPatientIdentifierList(0).IDNumber.Value ?? "";
            }

            // Patient Name
            if (pid.GetPatientName().Length > 0)
            {
                var name = pid.GetPatientName(0);
                data["LastName"] = name.FamilyName.Surname.Value ?? "";
                data["FirstName"] = name.GivenName.Value ?? "";
                data["MiddleName"] = name.SecondAndFurtherGivenNamesOrInitialsThereof.Value ?? "";
            }

            // Demographics
            data["DOB"] = pid.DateTimeOfBirth.Time.Value ?? "";
            data["DateOfBirth"] = pid.DateTimeOfBirth.Time.Value ?? ""; // Alias for compatibility
            data["Sex"] = pid.AdministrativeSex.Value ?? "";

            // Address
            if (pid.GetPatientAddress().Length > 0)
            {
                var address = pid.GetPatientAddress(0);
                data["Address"] = address.StreetAddress.StreetOrMailingAddress.Value ?? "";
                data["City"] = address.City.Value ?? "";
                data["State"] = address.StateOrProvince.Value ?? "";
                data["Zip"] = address.ZipOrPostalCode.Value ?? "";
            }

            // Phone
            if (pid.GetPhoneNumberHome().Length > 0)
            {
                data["Phone"] = pid.GetPhoneNumberHome(0).TelephoneNumber.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting patient data from PID segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractOrderData(OBR obr)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["AccessionNumber"] = obr.FillerOrderNumber.EntityIdentifier.Value ?? "";
            data["OrderDateTime"] = obr.ObservationDateTime.Time.Value ?? "";
            data["SpecimenReceivedDateTime"] = obr.SpecimenReceivedDateTime.Time.Value ?? "";
            data["ResultStatus"] = obr.ResultStatus.Value ?? "";

            // Test Name from Universal Service Identifier (OBR-4)
            var universalService = obr.UniversalServiceIdentifier;
            if (universalService != null)
            {
                data["TestName"] = universalService.Text.Value ?? universalService.Identifier.Value ?? "";
                data["TestCode"] = universalService.Identifier.Value ?? "";
            }

            // Specimen Source (OBR-15) - fallback when SPM segment is not present
            if (obr.SpecimenSource != null)
            {
                var specimenType = obr.SpecimenSource.SpecimenSourceNameOrCode?.Text.Value 
                    ?? obr.SpecimenSource.SpecimenSourceNameOrCode?.Identifier.Value ?? "";
                if (!string.IsNullOrEmpty(specimenType))
                {
                    data["SpecimenType"] = specimenType;
                    _logger.LogDebug("Specimen type from OBR-15: {SpecimenType}", specimenType);
                }
            }

            // Ordering Provider (OBR-16)
            if (obr.GetOrderingProvider().Length > 0)
            {
                var provider = obr.GetOrderingProvider(0);
                var givenName = provider.GivenName?.Value ?? "";
                var familyName = provider.FamilyName?.Surname?.Value ?? "";
                var providerId = provider.IDNumber?.Value ?? "";

                var providerName = $"{givenName} {familyName}".Trim();

                // If no name, try ID
                if (string.IsNullOrEmpty(providerName) && !string.IsNullOrEmpty(providerId))
                {
                    providerName = providerId;
                }

                if (!string.IsNullOrEmpty(providerName))
                {
                    data["OrderingProvider"] = providerName;
                    _logger.LogDebug("Ordering provider from OBR-16: {Provider}", providerName);
                }
                else
                {
                    _logger.LogDebug("OBR-16 ordering provider present but all name components are empty");
                }
            }
            else
            {
                _logger.LogDebug("No ordering provider (OBR-16) in message");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting order data from OBR segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractResultData(OBX obx)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["TestCode"] = obx.ObservationIdentifier.Identifier.Value ?? "";
            data["TestName"] = obx.ObservationIdentifier.Text.Value ?? "";
            data["Units"] = obx.Units.Identifier.Value ?? "";
            data["ReferenceRange"] = obx.ReferencesRange.Value ?? "";
            data["ResultStatus"] = obx.ObservationResultStatus.Value ?? "";
            data["ObservationDateTime"] = obx.DateTimeOfTheObservation.Time.Value ?? "";

            // Observation Value - varies by data type
            var obsValues = obx.GetObservationValue();
            if (obsValues.Length > 0)
            {
                data["Result"] = obsValues[0].Data?.ToString() ?? "";
            }

            // Abnormal Flags
            if (obx.GetAbnormalFlags().Length > 0)
            {
                data["AbnormalFlag"] = obx.GetAbnormalFlags(0).Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting result data from OBX segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractSpecimenData(SPM spm)
    {
        var data = new Dictionary<string, string>();

        try
        {
            // Specimen Type (SPM-4)
            // SPM.SpecimenType is a CWE (Coded with Exceptions) type
            var specimenType = spm.SpecimenType;
            if (specimenType != null)
            {
                data["SpecimenType"] = specimenType.Text.Value ?? specimenType.Identifier.Value ?? "";
                data["SpecimenTypeCode"] = specimenType.Identifier.Value ?? "";
            }

            // Collection DateTime (SPM-17)
            try
            {
                data["CollectionDateTime"] = spm.SpecimenCollectionDateTime?.RangeStartDateTime?.Time?.Value ?? "";
            }
            catch
            {
                // Collection date might not be present
            }

            // Received DateTime (SPM-18)
            try
            {
                data["ReceivedDateTime"] = spm.SpecimenReceivedDateTime?.Time?.Value ?? "";
            }
            catch
            {
                // Received date might not be present
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting specimen data from SPM segment");
        }

        return data;
    }

    // V2.5-specific extraction methods
    private Dictionary<string, string> ExtractPatientDataV25(V25_PID pid)
    {
        var data = new Dictionary<string, string>();

        try
        {
            // Patient ID
            if (pid.GetPatientIdentifierList().Length > 0)
            {
                data["PatientId"] = pid.GetPatientIdentifierList(0).IDNumber.Value ?? "";
            }

            // Patient Name
            if (pid.GetPatientName().Length > 0)
            {
                var name = pid.GetPatientName(0);
                data["LastName"] = name.FamilyName.Surname.Value ?? "";
                data["FirstName"] = name.GivenName.Value ?? "";
                data["MiddleName"] = name.SecondAndFurtherGivenNamesOrInitialsThereof.Value ?? "";
            }

            // Demographics
            data["DOB"] = pid.DateTimeOfBirth.Time.Value ?? "";
            data["DateOfBirth"] = pid.DateTimeOfBirth.Time.Value ?? ""; // Alias for compatibility
            data["Sex"] = pid.AdministrativeSex.Value ?? "";

            // Address
            if (pid.GetPatientAddress().Length > 0)
            {
                var address = pid.GetPatientAddress(0);
                data["Address"] = address.StreetAddress.StreetOrMailingAddress.Value ?? "";
                data["City"] = address.City.Value ?? "";
                data["State"] = address.StateOrProvince.Value ?? "";
                data["Zip"] = address.ZipOrPostalCode.Value ?? "";
                data["Country"] = address.Country.Value ?? "";
            }

            // Phone
            if (pid.GetPhoneNumberHome().Length > 0)
            {
                data["Phone"] = pid.GetPhoneNumberHome(0).TelephoneNumber.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting patient data from V2.5 PID segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractOrderDataV25(V25_OBR obr)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["OrderNumber"] = obr.PlacerOrderNumber.EntityIdentifier.Value ?? "";
            data["FillerOrderNumber"] = obr.FillerOrderNumber.EntityIdentifier.Value ?? "";
            data["AccessionNumber"] = obr.FillerOrderNumber.EntityIdentifier.Value ?? ""; // OBR-3.1
            data["OrderDateTime"] = obr.ObservationDateTime.Time.Value ?? "";
            data["SpecimenReceivedDateTime"] = obr.SpecimenReceivedDateTime.Time.Value ?? "";
            data["ResultStatus"] = obr.ResultStatus.Value ?? "";

            // Test Name from Universal Service Identifier (OBR-4)
            var universalService = obr.UniversalServiceIdentifier;
            if (universalService != null)
            {
                data["TestName"] = universalService.Text.Value ?? universalService.Identifier.Value ?? "";
                data["TestCode"] = universalService.Identifier.Value ?? "";
            }

            // Specimen Source (OBR-15) - fallback when SPM segment is not present
            _logger.LogDebug("V25: Checking OBR-15 SpecimenSource...");

            // Try structured access first
            if (obr.SpecimenSource != null)
            {
                _logger.LogDebug("V25: OBR-15 SpecimenSource is not null");
                var specimenSourceCode = obr.SpecimenSource.SpecimenSourceNameOrCode;
                if (specimenSourceCode != null)
                {
                    _logger.LogDebug("V25: SpecimenSourceNameOrCode exists");
                    var textValue = specimenSourceCode.Text?.Value;
                    var idValue = specimenSourceCode.Identifier?.Value;
                    _logger.LogDebug("V25: OBR-15 Text={Text}, Identifier={Id}", textValue ?? "(null)", idValue ?? "(null)");

                    var specimenType = textValue ?? idValue ?? "";
                    if (!string.IsNullOrEmpty(specimenType))
                    {
                        data["SpecimenType"] = specimenType;
                        _logger.LogInformation("V25: Specimen type from OBR-15 (structured): {SpecimenType}", specimenType);
                    }
                    else
                    {
                        _logger.LogDebug("V25: OBR-15 specimen type text and identifier are both empty");
                    }
                }
                else
                {
                    _logger.LogDebug("V25: SpecimenSourceNameOrCode is null");
                }
            }

            // If structured access failed, try raw segment string parsing
            if (!data.ContainsKey("SpecimenType") || string.IsNullOrEmpty(data["SpecimenType"]))
            {
                try
                {
                    // Get the raw encoded segment string
                    var rawSegment = PipeParser.Encode(obr, new EncodingCharacters('|', "^~\\&"));
                    _logger.LogDebug("V25: Raw OBR segment (FULL): {Segment}", rawSegment);

                    // Split by pipe to get fields (field 0 is segment name "OBR")
                    var fields = rawSegment.Split('|');
                    _logger.LogDebug("V25: OBR split into {Count} fields", fields.Length);

                    // Check standard position OBR-15 (index 15)
                    // But some labs send specimen type in OBR-12 (index 12) instead!
                    string specimenType = null;

                    if (fields.Length >= 16)
                    {
                        var field15 = fields[15].Trim();
                        if (!string.IsNullOrWhiteSpace(field15))
                        {
                            specimenType = field15;
                            _logger.LogInformation("V25: Specimen type from OBR-15 (standard position): {SpecimenType}", specimenType);
                        }
                    }

                    // Fallback: Check OBR-12 (index 12) - some labs use this non-standard position
                    if (string.IsNullOrEmpty(specimenType) && fields.Length >= 13)
                    {
                        var field12 = fields[12].Trim();
                        if (!string.IsNullOrWhiteSpace(field12) && !field12.Contains("^"))
                        {
                            // If it's a simple text value (no ^ component separators), it's likely specimen type
                            specimenType = field12;
                            _logger.LogInformation("V25: Specimen type from OBR-12 (non-standard position): {SpecimenType}", specimenType);
                        }
                    }

                    if (!string.IsNullOrEmpty(specimenType))
                    {
                        data["SpecimenType"] = specimenType;
                    }
                    else
                    {
                        _logger.LogDebug("V25: No specimen type found in OBR-15 or OBR-12");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not parse specimen type from raw segment");
                }
            }
            else
            {
                _logger.LogDebug("V25: OBR-15 SpecimenSource is null");
            }

            // Ordering Provider (OBR-16)
            _logger.LogDebug("V25: Checking OBR-16 OrderingProvider...");
            var providerCount = obr.GetOrderingProvider().Length;
            _logger.LogDebug("V25: OrderingProvider repetitions: {Count}", providerCount);

            if (providerCount > 0)
            {
                var provider = obr.GetOrderingProvider(0);
                _logger.LogDebug("V25: Provider object retrieved");

                var givenName = provider.GivenName?.Value ?? "";
                var familyName = provider.FamilyName?.Surname?.Value ?? "";
                var providerId = provider.IDNumber?.Value ?? "";

                _logger.LogDebug("V25: Provider components: GivenName={Given}, FamilyName={Family}, ID={Id}", 
                    givenName, familyName, providerId);

                var providerName = $"{givenName} {familyName}".Trim();

                // If no name, try ID
                if (string.IsNullOrEmpty(providerName) && !string.IsNullOrEmpty(providerId))
                {
                    providerName = providerId;
                }

                if (!string.IsNullOrEmpty(providerName))
                {
                    data["OrderingProvider"] = providerName;
                    _logger.LogInformation("V25: Ordering provider from OBR-16 (structured): {Provider}", providerName);
                }
                else
                {
                    _logger.LogDebug("V25: OBR-16 ordering provider present but all name components are empty");
                }
            }
            else
            {
                _logger.LogDebug("V25: No ordering provider (OBR-16) repetitions, trying raw segment parsing...");

                // Try raw segment string parsing
                try
                {
                    // Get the raw encoded segment string
                    var rawSegment = PipeParser.Encode(obr, new EncodingCharacters('|', "^~\\&"));

                    // Split by pipe to get fields (field 0 is segment name "OBR")
                    var fields = rawSegment.Split('|');
                    _logger.LogDebug("V25: OBR-16 split into {Count} fields", fields.Length);

                    string providerName = null;

                    // Check standard position OBR-16 (index 16)
                    if (fields.Length >= 17)
                    {
                        var field16 = fields[16].Trim();
                        if (!string.IsNullOrWhiteSpace(field16))
                        {
                            providerName = ParseProviderName(field16, "OBR-16 (standard)");
                        }
                    }

                    // Fallback: Check OBR-14 (index 14) - some labs use this non-standard position
                    if (string.IsNullOrEmpty(providerName) && fields.Length >= 15)
                    {
                        var field14 = fields[14].Trim();
                        if (!string.IsNullOrWhiteSpace(field14) && field14.Contains("^"))
                        {
                            // If it has component separators, it's likely a provider XCN field
                            providerName = ParseProviderName(field14, "OBR-14 (non-standard)");
                        }
                    }

                    if (!string.IsNullOrEmpty(providerName))
                    {
                        data["OrderingProvider"] = providerName;
                    }
                    else
                    {
                        _logger.LogDebug("V25: No ordering provider found in OBR-16 or OBR-14");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not parse ordering provider from raw segment");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting order data from V2.5 OBR segment");
        }

        return data;
    }

    private string ParseProviderName(string fieldValue, string fieldLocation)
    {
        try
        {
            // Parse XCN (Extended Composite ID Number and Name) format:
            // ID^LastName^FirstName^MiddleName^Suffix^Prefix^Degree^SourceTable^AssigningAuthority
            var components = fieldValue.Split('^');

            if (components.Length >= 3)
            {
                var id = components[0];
                var lastName = components[1];
                var firstName = components[2];
                var providerName = $"{firstName} {lastName}".Trim();

                if (!string.IsNullOrEmpty(providerName))
                {
                    _logger.LogInformation("V25: Ordering provider from {Location}: {Provider} (ID: {Id})", 
                        fieldLocation, providerName, id);
                    return providerName;
                }
            }
            else if (components.Length > 0 && !string.IsNullOrWhiteSpace(components[0]))
            {
                // Fallback: use ID only
                var providerId = components[0].Trim();
                _logger.LogInformation("V25: Ordering provider from {Location} (ID only): {Provider}", 
                    fieldLocation, providerId);
                return providerId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse provider name from {Location}: {Value}", fieldLocation, fieldValue);
        }

        return null;
    }

    private Dictionary<string, string> ExtractResultDataV25(V25_OBX obx)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["TestCode"] = obx.ObservationIdentifier.Identifier.Value ?? "";
            data["TestName"] = obx.ObservationIdentifier.Text.Value ?? "";
            data["Units"] = obx.Units.Identifier.Value ?? "";
            data["ReferenceRange"] = obx.ReferencesRange.Value ?? "";
            data["ResultStatus"] = obx.ObservationResultStatus.Value ?? "";
            data["ObservationDateTime"] = obx.DateTimeOfTheObservation.Time.Value ?? "";

            // Observation Value - varies by data type
            var obsValues = obx.GetObservationValue();
            if (obsValues.Length > 0)
            {
                data["Result"] = obsValues[0].Data?.ToString() ?? "";
            }

            // Abnormal Flags
            if (obx.GetAbnormalFlags().Length > 0)
            {
                data["AbnormalFlag"] = obx.GetAbnormalFlags(0).Value ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting result data from V2.5 OBX segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractSpecimenDataV25(V25_SPM spm)
    {
        var data = new Dictionary<string, string>();

        try
        {
            // Specimen Type (SPM-4)
            var specimenType = spm.SpecimenType;
            if (specimenType != null)
            {
                data["SpecimenType"] = specimenType.Text.Value ?? specimenType.Identifier.Value ?? "";
                data["SpecimenTypeCode"] = specimenType.Identifier.Value ?? "";
            }

            // Collection DateTime (SPM-17)
            try
            {
                data["CollectionDateTime"] = spm.SpecimenCollectionDateTime?.RangeStartDateTime?.Time?.Value ?? "";
            }
            catch
            {
                // Collection date might not be present
            }

            // Received DateTime (SPM-18)
            try
            {
                data["ReceivedDateTime"] = spm.SpecimenReceivedDateTime?.Time?.Value ?? "";
            }
            catch
            {
                // Received date might not be present
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting specimen data from V2.5 SPM segment");
        }

        return data;
    }

    private DateTime? ParseHL7DateTime(string? hl7DateTime)
    {
        if (string.IsNullOrWhiteSpace(hl7DateTime))
            return null;

        try
        {
            // HL7 datetime format: YYYYMMDDHHMMSS
            if (hl7DateTime.Length >= 8)
            {
                var year = int.Parse(hl7DateTime.Substring(0, 4));
                var month = int.Parse(hl7DateTime.Substring(4, 2));
                var day = int.Parse(hl7DateTime.Substring(6, 2));

                var hour = hl7DateTime.Length >= 10 ? int.Parse(hl7DateTime.Substring(8, 2)) : 0;
                var minute = hl7DateTime.Length >= 12 ? int.Parse(hl7DateTime.Substring(10, 2)) : 0;
                var second = hl7DateTime.Length >= 14 ? int.Parse(hl7DateTime.Substring(12, 2)) : 0;

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse HL7 datetime: {DateTime}", hl7DateTime);
        }

        return null;
    }

    #endregion
}
