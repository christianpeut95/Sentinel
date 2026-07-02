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
using V23_ORU_R01 = NHapi.Model.V23.Message.ORU_R01;
using V23_PID = NHapi.Model.V23.Segment.PID;
using V23_OBR = NHapi.Model.V23.Segment.OBR;
using V23_OBX = NHapi.Model.V23.Segment.OBX;
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

            // Clean up the message - remove log prefixes and find actual HL7 content
            var normalizedMessage = rawMessage;

            // 1. If message contains logging output, extract just the HL7 part
            if (normalizedMessage.Contains("Sentinel.") || normalizedMessage.Contains(": Information:") || normalizedMessage.Contains(": Debug:") || normalizedMessage.Contains(": Error:"))
            {
                _logger.LogWarning("Message appears to contain logging output - attempting to extract HL7 content");

                // Find the MSH segment which marks the start of the actual HL7 message
                var mshIndex = normalizedMessage.IndexOf("MSH|");
                if (mshIndex > 0)
                {
                    normalizedMessage = normalizedMessage.Substring(mshIndex);
                    _logger.LogInformation("Extracted HL7 content starting from MSH segment at position {Position}", mshIndex);
                }
            }

            // Normalize line endings and fix common formatting issues
            // 2. Replace various line ending combinations with \r
            normalizedMessage = normalizedMessage.Replace("\r\n", "\r").Replace("\n", "\r");

            // 3. If segments are separated by spaces instead of \r, fix that
            // Common issue when copy-pasting from databases or text editors
            // Look for segment identifiers that appear after a space (not at start of message)
            if (!normalizedMessage.Contains("\r"))
            {
                // Check if we have space-separated segments (e.g., "2.5.1 PID|" or "M OBR|")
                var hasSpaceSeparatedSegments = System.Text.RegularExpressions.Regex.IsMatch(
                    normalizedMessage,
                    @"\s(MSH|PID|PV1|ORC|OBR|OBX|SPM|NTE|FT1|DG1|GT1|IN1|NK1|AL1)\|"
                );

                if (hasSpaceSeparatedSegments)
                {
                    _logger.LogWarning("Message appears to use spaces between segments instead of line breaks - attempting to fix");
                    // Replace space before segment identifiers with \r
                    normalizedMessage = System.Text.RegularExpressions.Regex.Replace(
                        normalizedMessage, 
                        @"\s+(MSH|PID|PV1|ORC|OBR|OBX|SPM|NTE|FT1|DG1|GT1|IN1|NK1|AL1)\|", 
                        "\r$1|"
                    );
                }
            }

            // CRITICAL FIX: Store the normalized message so future processing uses the cleaned version
            // This ensures segments are properly separated when the message is later processed by the staging workflow
            hl7Message.RawMessage = normalizedMessage;
            _logger.LogDebug("Message normalized and saved to RawMessage field");

            // Parse the message
            var parsedMessage = _parser.Parse(normalizedMessage);

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

                        // CRITICAL FIX: Update the existing message's RawMessage with the normalized version
                        // This fixes issues where the original message had space-separated segments
                        if (existingMessage.RawMessage != normalizedMessage)
                        {
                            _logger.LogInformation(
                                "Updating existing message {MessageControlId} with normalized RawMessage (was {OldLength} chars, now {NewLength} chars)",
                                existingMessage.MessageControlId,
                                existingMessage.RawMessage?.Length ?? 0,
                                normalizedMessage.Length);

                            existingMessage.RawMessage = normalizedMessage;

                            // Re-parse segments with normalized message
                            existingMessage.Segments.Clear();
                            await ParseSegmentsAsync(existingMessage, parsedMessage, cancellationToken);

                            await _context.SaveChangesAsync(cancellationToken);
                            _logger.LogInformation("Successfully updated existing message {MessageControlId} with normalized data", existingMessage.MessageControlId);
                        }

                        // Return the existing message (now with normalized content)
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

            // Clean up the message - remove log prefixes and find actual HL7 content
            // 1. If message contains logging output, extract just the HL7 part
            if (rawMessage.Contains("Sentinel.") || rawMessage.Contains(": Information:") || rawMessage.Contains(": Debug:") || rawMessage.Contains(": Error:"))
            {
                _logger.LogWarning("Message appears to contain logging output - attempting to extract HL7 content");

                // Find the MSH segment which marks the start of the actual HL7 message
                var mshIndex = rawMessage.IndexOf("MSH|");
                if (mshIndex > 0)
                {
                    rawMessage = rawMessage.Substring(mshIndex);
                    _logger.LogInformation("Extracted HL7 content starting from MSH segment at position {Position}", mshIndex);
                }
            }

            // Normalize line endings and fix common formatting issues
            // 2. Replace various line ending combinations with \r
            rawMessage = rawMessage.Replace("\r\n", "\r").Replace("\n", "\r");

            // 3. If segments are separated by spaces instead of \r, fix that
            // Common issue when copy-pasting from databases or text editors
            // Look for segment identifiers that appear after a space (not at start of message)
            if (!rawMessage.Contains("\r"))
            {
                // Check if we have space-separated segments (e.g., "2.5.1 PID|" or "M OBR|")
                var hasSpaceSeparatedSegments = System.Text.RegularExpressions.Regex.IsMatch(
                    rawMessage,
                    @"\s(MSH|PID|PV1|ORC|OBR|OBX|SPM|NTE|FT1|DG1|GT1|IN1|NK1|AL1)\|"
                );

                if (hasSpaceSeparatedSegments)
                {
                    _logger.LogWarning("Message appears to use spaces between segments instead of line breaks - attempting to fix");
                    // Replace space before segment identifiers with \r
                    rawMessage = System.Text.RegularExpressions.Regex.Replace(
                        rawMessage, 
                        @"\s+(MSH|PID|PV1|ORC|OBR|OBX|SPM|NTE|FT1|DG1|GT1|IN1|NK1|AL1)\|", 
                        "\r$1|"
                    );
                }
            }

            _logger.LogDebug("After normalization first 200 chars: {Preview}", rawMessage.Substring(0, Math.Min(200, rawMessage.Length)));

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
            // Support V2.3, V2.5 and V2.5.1 message types
            bool isV251 = parsedMessage is ORU_R01;
            bool isV25 = parsedMessage is V25_ORU_R01;
            bool isV23 = parsedMessage is V23_ORU_R01;

            _logger.LogInformation("Message version check: V251={IsV251}, V25={IsV25}, V23={IsV23}", isV251, isV25, isV23);

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

                // Extract all OBX segments - iterate through all PATIENT_RESULT and ORDER_OBSERVATION groups
                try
                {
                    int totalObxCount = 0;
                    int patientResultCount = oruMessage.PATIENT_RESULTRepetitionsUsed;
                    _logger.LogInformation("PATIENT_RESULT groups: {Count}", patientResultCount);

                    for (int prIndex = 0; prIndex < patientResultCount; prIndex++)
                    {
                        var patientResult = oruMessage.GetPATIENT_RESULT(prIndex);
                        int orderObsCount = patientResult.ORDER_OBSERVATIONRepetitionsUsed;
                        _logger.LogInformation("PATIENT_RESULT[{PRIndex}] has {OrderCount} ORDER_OBSERVATION groups", prIndex, orderObsCount);

                        for (int orderIndex = 0; orderIndex < orderObsCount; orderIndex++)
                        {
                            var orderObs = patientResult.GetORDER_OBSERVATION(orderIndex);

                            // Check how many OBX at ORDER_OBSERVATION level
                            var obxCount = orderObs.OBSERVATIONRepetitionsUsed;
                            _logger.LogInformation("ORDER_OBSERVATION[{OrderIndex}] has {OBXCount} OBX segments directly", orderIndex, obxCount);

                            for (int obxIndex = 0; obxIndex < obxCount; obxIndex++)
                            {
                                var obx = orderObs.GetOBSERVATION(obxIndex).OBX;
                                result.ResultData.Add(ExtractResultData(obx));
                                totalObxCount++;
                            }

                            // Check SPECIMEN groups - they may contain the container group structure
                            var specimenCount = orderObs.SPECIMENRepetitionsUsed;
                            _logger.LogInformation("ORDER_OBSERVATION[{OrderIndex}] has {SpecimenCount} SPECIMEN groups", orderIndex, specimenCount);

                            // If we have specimens but no OBX yet, the structure might be:
                            // ORDER_OBSERVATION -> SPECIMEN -> (back to ORDER_OBSERVATION level for OBX)
                            // In v2.5.1, sometimes all OBX come after all SPM segments at the ORDER_OBSERVATION level
                            // So we already captured them above. Let's just log for diagnosis.
                            if (specimenCount > 0 && obxCount == 0)
                            {
                                _logger.LogWarning("Found {SpecimenCount} SPM segments but 0 OBX at ORDER_OBSERVATION level. Message structure may place OBX segments differently.", specimenCount);
                            }
                        }
                    }

                    // If we still have 0 OBX, try alternate approach: parse raw segments
                    if (totalObxCount == 0)
                    {
                        _logger.LogWarning("No OBX found via structured parsing. Attempting to parse OBX from raw message segments...");

                        // Get all segments from the message
                        var allSegmentNames = oruMessage.Names;
                        _logger.LogInformation("Message contains these segment types: {Segments}", string.Join(", ", allSegmentNames));

                        // Try to access OBX segments directly from message structure
                        try
                        {
                            // In some HL7 structures, OBX can be accessed differently
                            // Let's try getting the first ORDER_OBSERVATION and check its structure
                            if (patientResultCount > 0)
                            {
                                var firstOrderObs = oruMessage.GetPATIENT_RESULT(0).GetORDER_OBSERVATION(0);
                                var structure = firstOrderObs.GetStructureName();
                                _logger.LogInformation("ORDER_OBSERVATION structure name: {Structure}", structure);

                                // Log all available repetition properties
                                var type = firstOrderObs.GetType();
                                var properties = type.GetProperties()
                                    .Where(p => p.Name.Contains("Repetitions") || p.Name.Contains("OBSERVATION"))
                                    .Select(p => p.Name);
                                _logger.LogInformation("Available repetition properties: {Properties}", string.Join(", ", properties));
                            }
                        }
                        catch (Exception inspectEx)
                        {
                            _logger.LogDebug(inspectEx, "Error inspecting message structure");
                        }

                        // Fallback: Parse OBX segments from raw message text
                        _logger.LogInformation("Attempting to extract OBX segments from raw message text...");
                        var obxFromRaw = ParseOBXFromRawMessage(rawMessage);
                        _logger.LogInformation("Extracted {Count} OBX segments from raw message", obxFromRaw.Count);
                        result.ResultData.AddRange(obxFromRaw);
                        totalObxCount = obxFromRaw.Count;
                    }

                    _logger.LogInformation("Result data extracted: {Count} total observations across all groups", totalObxCount);
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

                // Extract all OBX segments - iterate through all PATIENT_RESULT and ORDER_OBSERVATION groups
                try
                {
                    int totalObxCount = 0;
                    int patientResultCount = oruMessage.PATIENT_RESULTRepetitionsUsed;
                    _logger.LogInformation("V2.5: PATIENT_RESULT groups: {Count}", patientResultCount);

                    for (int prIndex = 0; prIndex < patientResultCount; prIndex++)
                    {
                        var patientResult = oruMessage.GetPATIENT_RESULT(prIndex);
                        int orderObsCount = patientResult.ORDER_OBSERVATIONRepetitionsUsed;
                        _logger.LogInformation("V2.5: PATIENT_RESULT[{PRIndex}] has {OrderCount} ORDER_OBSERVATION groups", prIndex, orderObsCount);

                        for (int orderIndex = 0; orderIndex < orderObsCount; orderIndex++)
                        {
                            var orderObs = patientResult.GetORDER_OBSERVATION(orderIndex);
                            var obxCount = orderObs.OBSERVATIONRepetitionsUsed;
                            _logger.LogInformation("V2.5: ORDER_OBSERVATION[{OrderIndex}] has {OBXCount} OBX segments", orderIndex, obxCount);

                            for (int obxIndex = 0; obxIndex < obxCount; obxIndex++)
                            {
                                var obx = orderObs.GetOBSERVATION(obxIndex).OBX;
                                var obxData = ExtractResultDataV25(obx);

                                // Log each OBX result for debugging
                                _logger.LogDebug("V2.5: OBX[{Index}] extracted: TestCode={TestCode}, TestName={TestName}, Result={Result}", 
                                    totalObxCount, 
                                    obxData.GetValueOrDefault("TestCode", "(none)"),
                                    obxData.GetValueOrDefault("TestName", "(none)"),
                                    obxData.GetValueOrDefault("Result", "(none)"));

                                result.ResultData.Add(obxData);
                                totalObxCount++;
                            }
                        }
                    }
                    _logger.LogInformation("V2.5: Result data extracted: {Count} total observations across all groups", totalObxCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract result data");
                }
            }
            else if (isV23)
            {
                _logger.LogInformation("Message is ORU_R01 V2.3, extracting patient/order/result data...");
                var oruMessage = (V23_ORU_R01)parsedMessage;

                try
                {
                    var pid = oruMessage.GetRESPONSE().PATIENT.PID;
                    result.PatientData = ExtractPatientDataV23(pid);
                    _logger.LogInformation("Patient data extracted: {Count} fields", result.PatientData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract patient data");
                }

                try
                {
                    // Extract order and results
                    var obr = oruMessage.GetRESPONSE().GetORDER_OBSERVATION().OBR;
                    result.OrderData = ExtractOrderDataV23(obr);
                    _logger.LogInformation("Order data extracted: {Count} fields", result.OrderData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to extract order data");
                }

                // Extract all OBX segments - iterate through all RESPONSE and ORDER_OBSERVATION groups
                try
                {
                    int totalObxCount = 0;
                    int responseCount = oruMessage.RESPONSERepetitionsUsed;
                    _logger.LogInformation("V2.3: RESPONSE groups: {Count}", responseCount);

                    for (int respIndex = 0; respIndex < responseCount; respIndex++)
                    {
                        var response = oruMessage.GetRESPONSE(respIndex);
                        int orderObsCount = response.ORDER_OBSERVATIONRepetitionsUsed;
                        _logger.LogInformation("V2.3: RESPONSE[{RespIndex}] has {OrderCount} ORDER_OBSERVATION groups", respIndex, orderObsCount);

                        for (int orderIndex = 0; orderIndex < orderObsCount; orderIndex++)
                        {
                            var orderObs = response.GetORDER_OBSERVATION(orderIndex);
                            var obxCount = orderObs.OBSERVATIONRepetitionsUsed;
                            _logger.LogInformation("V2.3: ORDER_OBSERVATION[{OrderIndex}] has {OBXCount} OBX segments", orderIndex, obxCount);

                            for (int obxIndex = 0; obxIndex < obxCount; obxIndex++)
                            {
                                var obx = orderObs.GetOBSERVATION(obxIndex).OBX;
                                var obxData = ExtractResultDataV23(obx);

                                _logger.LogDebug("V2.3: OBX[{Index}] extracted: TestCode={TestCode}, TestName={TestName}, Result={Result}", 
                                    totalObxCount, 
                                    obxData.GetValueOrDefault("TestCode", "(none)"),
                                    obxData.GetValueOrDefault("TestName", "(none)"),
                                    obxData.GetValueOrDefault("Result", "(none)"));

                                result.ResultData.Add(obxData);
                                totalObxCount++;
                            }
                        }
                    }
                    _logger.LogInformation("V2.3: Result data extracted: {Count} total observations across all groups", totalObxCount);
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

            // Normalize line endings (some messages use \r, \n, or \r\n)
            rawMessage = rawMessage.Replace("\r\n", "\r").Replace("\n", "\r");

            // Basic MSH field validation before parsing
            var mshLine = rawMessage.Split('\r')[0];
            if (mshLine.Length < 8)
            {
                result.Errors.Add("MSH segment is too short. Expected format: MSH|^~\\&|...");
                return result;
            }

            // Validate field separator (should be | at position 3)
            if (mshLine[3] != '|')
            {
                result.Errors.Add($"Invalid field separator at position 3. Expected '|', found '{mshLine[3]}'");
                return result;
            }

            // Validate encoding characters (should be ^~\& at positions 4-8)
            var encodingChars = mshLine.Substring(4, 4);
            if (encodingChars != "^~\\&")
            {
                result.Warnings.Add($"Non-standard encoding characters: '{encodingChars}'. Expected '^~\\&'");
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

            // Provide more helpful error messages for common parsing issues
            if (ex.Message.Contains("version not recognized") || ex.Message.Contains("Can't process message of version"))
            {
                result.Errors.Add("Unable to parse HL7 version. Please check:");
                result.Errors.Add("• Message uses correct field separator (|) and encoding characters (^~\\&)");
                result.Errors.Add("• MSH segment has proper structure: MSH|^~\\&|SendingApp|...");
                result.Errors.Add("• Line breaks use standard format (\\r or \\n between segments)");
                result.Errors.Add($"Technical details: {ex.Message}");
            }
            else
            {
                result.Errors.Add(ex.Message);
            }
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
                    _logger.LogDebug("OBR-16 ordering provider present but all name components are empty, trying raw segment parsing...");

                    // Try raw segment parsing for v2.3 - the structure might be different
                    try
                    {
                        var rawSegment = PipeParser.Encode(obr, new EncodingCharacters('|', "^~\\&"));
                        var fields = rawSegment.Split('|');

                        if (fields.Length >= 17)
                        {
                            var field16 = fields[16].Trim();
                            if (!string.IsNullOrWhiteSpace(field16))
                            {
                                providerName = ParseProviderName(field16, "OBR-16");
                                if (!string.IsNullOrEmpty(providerName))
                                {
                                    data["OrderingProvider"] = providerName;
                                    _logger.LogInformation("V2.3: Ordering provider from OBR-16 (raw): {Provider}", providerName);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not parse ordering provider from raw OBR segment");
                    }
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

            // Observation Method (OBX-17) - Test method/type (e.g., NAAT, Culture, etc.)
            if (obx.GetObservationMethod().Length > 0)
            {
                var method = obx.GetObservationMethod(0);
                data["TestMethod"] = method.Text.Value ?? method.Identifier.Value ?? "";
                data["TestMethodCode"] = method.Identifier.Value ?? "";
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
            // Standard: ID^LastName^FirstName^MiddleName^Suffix^Prefix^Degree^SourceTable^AssigningAuthority
            // Alternate: LastName^FirstName^MiddleName... (when ID is in later component)
            var components = fieldValue.Split('^');

            if (components.Length >= 3)
            {
                // Check if first component is likely an ID (numeric) or a name (alpha)
                var comp0 = components[0].Trim();
                var comp1 = components[1].Trim();
                var comp2 = components[2].Trim();

                string providerName = null;
                string id = null;

                // If component 0 is empty or looks like a name, assume format: LastName^FirstName
                if (string.IsNullOrEmpty(comp0) || !char.IsDigit(comp0[0]))
                {
                    // Format: LastName^FirstName^...^...^...^...^...^...^ProviderId
                    var lastName = comp0;
                    var firstName = comp1;
                    providerName = $"{firstName} {lastName}".Trim();

                    // ID might be in a later component (e.g., component 8)
                    if (components.Length > 8 && !string.IsNullOrWhiteSpace(components[8]))
                    {
                        id = components[8].Trim();
                    }
                }
                else
                {
                    // Standard format: ID^LastName^FirstName
                    id = comp0;
                    var lastName = comp1;
                    var firstName = comp2;
                    providerName = $"{firstName} {lastName}".Trim();
                }

                if (!string.IsNullOrEmpty(providerName))
                {
                    _logger.LogInformation("Ordering provider from {Location}: {Provider} (ID: {Id})", 
                        fieldLocation, providerName, id ?? "N/A");
                    return providerName;
                }
                else if (!string.IsNullOrEmpty(id))
                {
                    // Use ID as fallback
                    _logger.LogInformation("Ordering provider from {Location} (ID only): {Provider}", 
                        fieldLocation, id);
                    return id;
                }
            }
            else if (components.Length >= 2)
            {
                // Just two components - assume LastName^FirstName
                var lastName = components[0].Trim();
                var firstName = components[1].Trim();
                var providerName = $"{firstName} {lastName}".Trim();

                if (!string.IsNullOrEmpty(providerName))
                {
                    _logger.LogInformation("Ordering provider from {Location}: {Provider}", 
                        fieldLocation, providerName);
                    return providerName;
                }
            }
            else if (components.Length > 0 && !string.IsNullOrWhiteSpace(components[0]))
            {
                // Fallback: use first component only
                var value = components[0].Trim();
                _logger.LogInformation("Ordering provider from {Location} (single value): {Provider}", 
                    fieldLocation, value);
                return value;
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

            // Observation Method (OBX-17) - Test method/type (e.g., NAAT, Culture, etc.)
            if (obx.GetObservationMethod().Length > 0)
            {
                var method = obx.GetObservationMethod(0);
                data["TestMethod"] = method.Text.Value ?? method.Identifier.Value ?? "";
                data["TestMethodCode"] = method.Identifier.Value ?? "";
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

    private Dictionary<string, string> ExtractPatientDataV23(V23_PID pid)
    {
        var data = new Dictionary<string, string>();

        try
        {
            // Patient ID - try direct access first
            if (pid.GetPatientIDInternalID().Length > 0)
            {
                data["PatientID"] = pid.GetPatientIDInternalID(0).ID.Value ?? "";
            }

            // Patient Name (PID-5)
            if (pid.GetPatientName().Length > 0)
            {
                var name = pid.GetPatientName(0);
                data["FirstName"] = name.GivenName.Value ?? "";
                data["LastName"] = name.FamilyName.Value ?? "";
                data["FullName"] = $"{name.GivenName.Value ?? ""} {name.FamilyName.Value ?? ""}".Trim();
            }

            // Date of Birth
            data["DateOfBirth"] = pid.DateOfBirth.TimeOfAnEvent.Value ?? "";

            // Sex
            data["Sex"] = pid.Sex.Value ?? "";

            // Address
            if (pid.GetPatientAddress().Length > 0)
            {
                var address = pid.GetPatientAddress(0);
                data["Address"] = address.StreetAddress.Value ?? "";
                data["City"] = address.City.Value ?? "";
                data["State"] = address.StateOrProvince.Value ?? "";
                data["ZipCode"] = address.ZipOrPostalCode.Value ?? "";
            }

            // Phone - try both properties
            if (pid.GetPhoneNumberHome().Length > 0)
            {
                // V2.3 XTN structure is different, try ToString as fallback
                data["Phone"] = pid.GetPhoneNumberHome(0).ToString() ?? "";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting patient data from V2.3 PID segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractOrderDataV23(V23_OBR obr)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["OrderNumber"] = obr.PlacerOrderNumber.EntityIdentifier.Value ?? "";
            data["FillerOrderNumber"] = obr.FillerOrderNumber.EntityIdentifier.Value ?? "";
            data["AccessionNumber"] = obr.FillerOrderNumber.EntityIdentifier.Value ?? ""; // OBR-3.1
            data["OrderDateTime"] = obr.ObservationDateTime.TimeOfAnEvent.Value ?? "";
            data["SpecimenReceivedDateTime"] = obr.SpecimenReceivedDateTime.TimeOfAnEvent.Value ?? "";
            data["ResultStatus"] = obr.ResultStatus.Value ?? "";

            // Test Name from Universal Service Identifier (OBR-4)
            var universalService = obr.UniversalServiceIdentifier;
            if (universalService != null)
            {
                data["TestName"] = universalService.Text.Value ?? universalService.Identifier.Value ?? "";
                data["TestCode"] = universalService.Identifier.Value ?? "";
            }

            // Specimen Source (OBR-15)
            _logger.LogDebug("V23: Checking OBR-15 SpecimenSource...");

            if (obr.SpecimenSource != null)
            {
                var specimenSourceCode = obr.SpecimenSource.SpecimenSourceNameOrCode;
                if (specimenSourceCode != null)
                {
                    var textValue = specimenSourceCode.Text?.Value;
                    var idValue = specimenSourceCode.Identifier?.Value;
                    _logger.LogDebug("V23: OBR-15 Text={Text}, Identifier={Id}", textValue ?? "(null)", idValue ?? "(null)");

                    if (!string.IsNullOrWhiteSpace(textValue))
                    {
                        data["SpecimenSource"] = textValue;
                        _logger.LogInformation("V23: Specimen source from OBR-15 (text): {Source}", textValue);
                    }
                    else if (!string.IsNullOrWhiteSpace(idValue))
                    {
                        data["SpecimenSource"] = idValue;
                        _logger.LogInformation("V23: Specimen source from OBR-15 (id): {Source}", idValue);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting order data from V2.3 OBR segment");
        }

        return data;
    }

    private Dictionary<string, string> ExtractResultDataV23(V23_OBX obx)
    {
        var data = new Dictionary<string, string>();

        try
        {
            data["TestCode"] = obx.ObservationIdentifier.Identifier.Value ?? "";
            data["TestName"] = obx.ObservationIdentifier.Text.Value ?? "";
            data["Units"] = obx.Units.Identifier.Value ?? "";
            data["ReferenceRange"] = obx.ReferencesRange.Value ?? "";
            // V2.3 may not have observation result status in all cases
            data["ObservationDateTime"] = obx.DateTimeOfTheObservation.TimeOfAnEvent.Value ?? "";

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

            // Observation Method (OBX-17) - Test method/type (e.g., NAAT, Culture, etc.)
            // Note: In v2.3, this field may not always be populated
            try
            {
                if (obx.GetObservationMethod().Length > 0)
                {
                    var method = obx.GetObservationMethod(0);
                    data["TestMethod"] = method.Text.Value ?? method.Identifier.Value ?? "";
                    data["TestMethodCode"] = method.Identifier.Value ?? "";
                }
            }
            catch
            {
                // OBX-17 may not be available in all v2.3 implementations
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error extracting result data from V2.3 OBX segment");
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

    /// <summary>
    /// Fallback method to extract OBX segments directly from raw HL7 message text.
    /// Used when NHapi's structured parsing fails due to non-standard segment ordering.
    /// </summary>
    private List<Dictionary<string, string>> ParseOBXFromRawMessage(string rawMessage)
    {
        var results = new List<Dictionary<string, string>>();

        try
        {
            // Split message into segments (HL7 uses \r as segment separator after normalization)
            var segments = rawMessage.Split('\r', StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                // Check if this is an OBX segment
                if (!segment.StartsWith("OBX|", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Split segment into fields (| is the field separator)
                var fields = segment.Split('|');

                if (fields.Length < 6) // OBX must have at least 6 fields
                {
                    _logger.LogWarning("OBX segment has insufficient fields: {Segment}", segment);
                    continue;
                }

                var data = new Dictionary<string, string>();

                try
                {
                    // OBX-1: Set ID
                    if (fields.Length > 1)
                        data["SetID"] = fields[1];

                    // OBX-2: Value Type
                    if (fields.Length > 2)
                        data["ValueType"] = fields[2];

                    // OBX-3: Observation Identifier (test code and name)
                    if (fields.Length > 3 && !string.IsNullOrEmpty(fields[3]))
                    {
                        var components = fields[3].Split('^');
                        data["TestCode"] = components.Length > 0 ? components[0] : "";
                        data["TestName"] = components.Length > 1 ? components[1] : "";
                        data["CodingSystem"] = components.Length > 2 ? components[2] : "";
                    }

                    // OBX-4: Observation Sub-ID
                    if (fields.Length > 4)
                        data["SubID"] = fields[4];

                    // OBX-5: Observation Value (the actual result)
                    if (fields.Length > 5 && !string.IsNullOrEmpty(fields[5]))
                    {
                        // For CWE type, parse components
                        var valueComponents = fields[5].Split('^');
                        data["Result"] = valueComponents.Length > 1 ? valueComponents[1] : valueComponents[0];
                        data["ResultCode"] = valueComponents.Length > 0 ? valueComponents[0] : "";
                    }

                    // OBX-6: Units
                    if (fields.Length > 6)
                        data["Units"] = fields[6];

                    // OBX-7: Reference Range
                    if (fields.Length > 7)
                        data["ReferenceRange"] = fields[7];

                    // OBX-8: Abnormal Flags
                    if (fields.Length > 8)
                        data["AbnormalFlag"] = fields[8];

                    // OBX-11: Observation Result Status
                    if (fields.Length > 11)
                        data["ResultStatus"] = fields[11];

                    // OBX-14: Date/Time of Observation
                    if (fields.Length > 14)
                        data["ObservationDateTime"] = fields[14];

                    // OBX-17: Observation Method (test method like NAAT)
                    if (fields.Length > 17 && !string.IsNullOrEmpty(fields[17]))
                    {
                        var methodComponents = fields[17].Split('^');
                        data["TestMethodCode"] = methodComponents.Length > 0 ? methodComponents[0] : "";
                        data["TestMethod"] = methodComponents.Length > 1 ? methodComponents[1] : methodComponents[0];
                    }

                    results.Add(data);

                    _logger.LogDebug("Parsed OBX from raw text: SetID={SetID}, TestName={TestName}, Result={Result}", 
                        data.GetValueOrDefault("SetID", "?"),
                        data.GetValueOrDefault("TestName", "?"),
                        data.GetValueOrDefault("Result", "?"));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing OBX segment: {Segment}", segment);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OBX segments from raw message");
        }

        return results;
    }

    #endregion
}
