using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;
using System.Text.RegularExpressions;

namespace Sentinel.Pages.Settings.HL7.FieldMappings
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class ConfigureLabModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IHL7ParserService _parserService;
        private readonly IHL7FieldMappingService _mappingService;
        private readonly ILogger<ConfigureLabModel> _logger;

        public ConfigureLabModel(
            ApplicationDbContext context,
            IHL7ParserService parserService,
            IHL7FieldMappingService mappingService,
            ILogger<ConfigureLabModel> logger)
        {
            _context = context;
            _parserService = parserService;
            _mappingService = mappingService;
            _logger = logger;
        }

        public HL7Configuration? Configuration { get; set; }
        public List<MappingFieldCard> Fields { get; set; } = new();
        public int UnresolvedFieldCount => Fields.Count(f => f.Status != "Confirmed" && (f.IsRequired || f.Status == "NeedsAttention"));
        public string? SampleMessage { get; set; }
        public bool HasSampleMessage => !string.IsNullOrEmpty(SampleMessage);

        [BindProperty]
        public string? UploadedMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? configId, string? sampleMessage)
        {
            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("ConfigureLab OnGetAsync START");
            _logger.LogInformation("ConfigId parameter: {ConfigId}", configId);
            _logger.LogInformation("SampleMessage parameter: {HasSample} (length: {Length})", 
                !string.IsNullOrEmpty(sampleMessage), 
                sampleMessage?.Length ?? 0);

            // Configuration ID is required
            if (!configId.HasValue || configId.Value == Guid.Empty)
            {
                _logger.LogWarning("No valid configId provided - redirecting to SelectLab");
                TempData["ErrorMessage"] = "Please create a lab configuration first before configuring field mappings.";
                return RedirectToPage("/Settings/HL7/FieldMappings/SelectLab");
            }

            Configuration = await _context.HL7Configurations
                .FirstOrDefaultAsync(c => c.Id == configId.Value);

            if (Configuration == null)
            {
                _logger.LogWarning("Configuration not found for ID: {ConfigId}", configId.Value);
                TempData["ErrorMessage"] = "Lab configuration not found.";
                return RedirectToPage("/Settings/HL7/FieldMappings/SelectLab");
            }

            _logger.LogInformation("Configuration loaded: {ConfigName} (ID: {ConfigId})", 
                Configuration.ConfigurationName, 
                Configuration.Id);

            // Check if there's a stored sample message for this configuration
            if (!string.IsNullOrEmpty(sampleMessage))
            {
                _logger.LogInformation("Sample message provided via query parameter (length: {Length})", sampleMessage.Length);
                SampleMessage = sampleMessage;
            }
            else if (Configuration != null)
            {
                _logger.LogInformation("No sample in query parameter, checking database...");
                // Try to load a previously saved sample
                var existingMapping = await _context.HL7FieldMappings
                    .FirstOrDefaultAsync(m => m.ConfigurationId == Configuration.Id);

                if (existingMapping != null && !string.IsNullOrEmpty(existingMapping.SampleMessage))
                {
                    _logger.LogInformation("Found existing sample in database (length: {Length})", existingMapping.SampleMessage.Length);
                    SampleMessage = existingMapping.SampleMessage;
                }
                else
                {
                    _logger.LogInformation("No existing sample found in database");
                }
            }

            _logger.LogInformation("Final SampleMessage state: {HasSample} (length: {Length})", 
                !string.IsNullOrEmpty(SampleMessage), 
                SampleMessage?.Length ?? 0);

            await BuildFieldCardsAsync();

            _logger.LogInformation("ConfigureLab OnGetAsync END - Fields count: {Count}", Fields?.Count ?? 0);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");

            return Page();
        }

        public async Task<IActionResult> OnPostUploadSampleAsync(Guid configId)
        {
            _logger.LogInformation("═══════════════════════════════════════════════════════════");
            _logger.LogInformation("OnPostUploadSampleAsync START");
            _logger.LogInformation("ConfigId: {ConfigId}", configId);
            _logger.LogInformation("UploadedMessage: {HasMessage} (length: {Length})", 
                !string.IsNullOrEmpty(UploadedMessage), 
                UploadedMessage?.Length ?? 0);

            if (string.IsNullOrWhiteSpace(UploadedMessage))
            {
                _logger.LogWarning("No message content provided");
                TempData["ErrorMessage"] = "Please paste an HL7 message to analyze";
                return RedirectToPage(new { configId });
            }

            Configuration = await _context.HL7Configurations.FindAsync(configId);
            if (Configuration == null)
            {
                _logger.LogWarning("Configuration not found for ID: {ConfigId}", configId);
                return NotFound();
            }

            _logger.LogInformation("Configuration found: {ConfigName}", Configuration.ConfigurationName);
            _logger.LogInformation("Validating message format...");

            // Validate the message can be parsed
            var validation = await _parserService.ValidateMessageAsync(UploadedMessage);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", validation.Errors));
                TempData["ErrorMessage"] = $"Could not parse message: {string.Join(", ", validation.Errors)}";
                return RedirectToPage(new { configId });
            }

            _logger.LogInformation("Message validation passed ✓");
            _logger.LogInformation("Looking for existing mapping record...");

            // Store the sample message for this configuration
            var mapping = await _context.HL7FieldMappings
                .FirstOrDefaultAsync(m => m.ConfigurationId == configId) 
                ?? new HL7FieldMapping
                {
                    ConfigurationId = configId,
                    TargetEntity = "Configuration",
                    TargetProperty = "SampleMessage",
                    FieldName = "Sample HL7 Message",
                    CreatedAt = DateTime.UtcNow
                };

            if (mapping.Id == Guid.Empty)
            {
                _logger.LogInformation("Creating new mapping record");
            }
            else
            {
                _logger.LogInformation("Updating existing mapping record (ID: {MappingId})", mapping.Id);
            }

            mapping.SampleMessage = UploadedMessage;
            mapping.ModifiedAt = DateTime.UtcNow;

            if (mapping.Id == Guid.Empty)
            {
                _context.HL7FieldMappings.Add(mapping);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sample message saved to database ✓");

            TempData["SuccessMessage"] = "Sample message uploaded. Sentinel is analyzing the fields...";

            _logger.LogInformation("Redirecting to OnGetAsync with configId={ConfigId} and sampleMessage (length: {Length})", 
                configId, 
                UploadedMessage.Length);
            _logger.LogInformation("═══════════════════════════════════════════════════════════");

            return RedirectToPage(new { configId, sampleMessage = UploadedMessage });
        }

        public async Task<IActionResult> OnPostClearSampleAsync(Guid configId)
        {
            Configuration = await _context.HL7Configurations.FindAsync(configId);
            if (Configuration == null)
            {
                return NotFound();
            }

            // Clear the stored sample message
            var mapping = await _context.HL7FieldMappings
                .FirstOrDefaultAsync(m => m.ConfigurationId == configId && m.TargetEntity == "Configuration");

            if (mapping != null)
            {
                mapping.SampleMessage = null;
                mapping.ModifiedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Sample message cleared. Upload a different message to re-analyze.";
            return RedirectToPage(new { configId });
        }

        private async Task BuildFieldCardsAsync()
        {
            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║ BuildFieldCardsAsync START                                ║");
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");
            _logger.LogInformation("ConfigId: {ConfigId}", Configuration?.Id);
            _logger.LogInformation("HasSampleMessage: {HasSample}", HasSampleMessage);
            _logger.LogInformation("SampleMessage length: {Length}", SampleMessage?.Length ?? 0);

            if (HasSampleMessage)
            {
                var firstLine = SampleMessage!.Split('\n', '\r')[0];
                _logger.LogInformation("Sample first line: {FirstLine}", firstLine.Substring(0, Math.Min(80, firstLine.Length)));
            }

            // Define the surveillance fields we need to map
            var fieldDefinitions = GetFieldDefinitions();
            _logger.LogInformation("✓ Field definitions loaded: {Count} fields", fieldDefinitions.Count);

            // Load existing mappings if configuration exists
            Dictionary<string, HL7FieldMapping> existingMappings = new();
            if (Configuration != null)
            {
                var mappings = await _context.HL7FieldMappings
                    .Where(m => m.ConfigurationId == Configuration.Id && m.IsActive)
                    .ToListAsync();

                _logger.LogInformation("✓ Existing mappings loaded: {Count} mappings", mappings.Count);

                foreach (var mapping in mappings)
                {
                    var key = $"{mapping.TargetEntity}_{mapping.TargetProperty}";
                    existingMappings[key] = mapping;
                    _logger.LogDebug("  - Existing mapping: {Key} → {FieldPath}", key, mapping.FieldPath);
                }
            }

            // Parse sample message if available
            HL7ParseResult? parseResult = null;
            if (HasSampleMessage)
            {
                try
                {
                    _logger.LogInformation("▶ Attempting to parse sample message...");
                    parseResult = await _parserService.ParseMessagePreviewAsync(SampleMessage!);

                    _logger.LogInformation("✓ ✓ ✓ PARSE SUCCESSFUL ✓ ✓ ✓");
                    _logger.LogInformation("PatientData keys ({Count}): {Keys}", 
                        parseResult.PatientData.Count,
                        string.Join(", ", parseResult.PatientData.Keys));

                    foreach (var kvp in parseResult.PatientData)
                    {
                        _logger.LogDebug("  PatientData[{Key}] = {Value}", kvp.Key, kvp.Value);
                    }

                    _logger.LogInformation("OrderData keys ({Count}): {Keys}", 
                        parseResult.OrderData.Count,
                        string.Join(", ", parseResult.OrderData.Keys));

                    foreach (var kvp in parseResult.OrderData)
                    {
                        _logger.LogDebug("  OrderData[{Key}] = {Value}", kvp.Key, kvp.Value);
                    }

                    _logger.LogInformation("SpecimenData keys ({Count}): {Keys}", 
                        parseResult.SpecimenData.Count,
                        string.Join(", ", parseResult.SpecimenData.Keys));

                    foreach (var kvp in parseResult.SpecimenData)
                    {
                        _logger.LogDebug("  SpecimenData[{Key}] = {Value}", kvp.Key, kvp.Value);
                    }

                    _logger.LogInformation("ResultData count: {Count}", parseResult.ResultData.Count);
                    for (int i = 0; i < parseResult.ResultData.Count; i++)
                    {
                        var result = parseResult.ResultData[i];
                        _logger.LogDebug("  ResultData[{Index}] keys: {Keys}", i, string.Join(", ", result.Keys));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ ✗ ✗ PARSE FAILED ✗ ✗ ✗");
                    _logger.LogError("Exception type: {Type}", ex.GetType().Name);
                    _logger.LogError("Exception message: {Message}", ex.Message);
                    _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                    TempData["ErrorMessage"] = $"Could not parse message: {ex.Message}. Please check the message format and try again.";
                    // Continue to show fields without candidates
                }
            }
            else
            {
                _logger.LogInformation("⊘ No sample message available for parsing");
            }

            // Build field cards
            _logger.LogInformation("▶ Building field cards...");
            Fields = new List<MappingFieldCard>();

            foreach (var fieldDef in fieldDefinitions)
            {
                var fieldKey = fieldDef.Key;
                var existingKey = $"{fieldDef.TargetEntity}_{fieldDef.TargetProperty}";
                var hasExistingMapping = existingMappings.ContainsKey(existingKey);

                _logger.LogDebug("Processing field: {FieldKey} (Entity: {Entity}, Property: {Property})", 
                    fieldKey, fieldDef.TargetEntity, fieldDef.TargetProperty);

                var card = new MappingFieldCard
                {
                    FieldKey = fieldKey,
                    FriendlyName = fieldDef.FriendlyName,
                    HelpText = fieldDef.HelpText,
                    IsRequired = fieldDef.IsRequired,
                    Priority = fieldDef.Priority
                };

                // Determine status and populate candidates
                if (hasExistingMapping)
                {
                    var mapping = existingMappings[existingKey];
                    if (!string.IsNullOrEmpty(mapping.FieldPath) && mapping.FieldPath != "SKIPPED")
                    {
                        card.Status = "Confirmed";
                        card.ConfirmedValue = await GetMappedDisplayValue(parseResult, fieldDef, mapping.FieldPath);
                        card.ExtractedHint = $"From {GetFieldPathHint(mapping.FieldPath)}";
                        _logger.LogDebug("  → Status: Confirmed (value: {Value})", card.ConfirmedValue);
                    }
                    else if (mapping.FieldPath == "SKIPPED")
                    {
                        card.Status = "Confirmed"; // Skipped is a valid resolution
                        card.ConfirmedValue = "(skipped - not needed)";
                        _logger.LogDebug("  → Status: Confirmed (skipped)");
                    }
                    else
                    {
                        card.Status = "NotFound";
                        _logger.LogDebug("  → Status: NotFound (has mapping but no path)");
                    }
                }
                else if (parseResult != null)
                {
                    // Try to find candidates from parsed message
                    _logger.LogDebug("  Detecting candidates for {FieldKey}...", fieldKey);
                    var candidates = await DetectCandidatesAsync(parseResult, fieldDef);

                    _logger.LogInformation("  Field {FieldKey}: {CandidateCount} candidates detected", fieldKey, candidates.Count);

                    foreach (var candidate in candidates)
                    {
                        _logger.LogDebug("    Candidate: {Value} (Confidence: {Confidence}%, Path: {Path})", 
                            candidate.Value, candidate.Confidence, candidate.FieldPath);
                    }

                    if (candidates.Count == 1 && candidates[0].Confidence >= 80)
                    {
                        // High confidence single match - pre-confirm it but ask for verification
                        card.Status = "NeedsAttention";
                        card.CandidateValues = candidates;
                        _logger.LogDebug("  → Status: NeedsAttention (high confidence single match)");
                    }
                    else if (candidates.Count > 0)
                    {
                        card.Status = "NeedsAttention";
                        card.CandidateValues = candidates;
                        _logger.LogDebug("  → Status: NeedsAttention ({Count} candidates)", candidates.Count);
                    }
                    else
                    {
                        card.Status = "NotFound";
                        _logger.LogDebug("  → Status: NotFound (no candidates detected)");
                    }
                }
                else
                {
                    card.Status = "NotFound";
                    _logger.LogDebug("  → Status: NotFound (no parse result)");
                }

                Fields.Add(card);
            }

            _logger.LogInformation("╔═══════════════════════════════════════════════════════════╗");
            _logger.LogInformation("║ FIELD CARDS BUILT - SUMMARY                               ║");
            _logger.LogInformation("╠═══════════════════════════════════════════════════════════╣");
            _logger.LogInformation("║ Total fields:        {Count,3}                                ║", Fields.Count);
            _logger.LogInformation("║ Confirmed:           {Count,3}                                ║", Fields.Count(f => f.Status == "Confirmed"));
            _logger.LogInformation("║ Needs attention:     {Count,3}                                ║", Fields.Count(f => f.Status == "NeedsAttention"));
            _logger.LogInformation("║ Not found:           {Count,3}                                ║", Fields.Count(f => f.Status == "NotFound"));
            _logger.LogInformation("╚═══════════════════════════════════════════════════════════╝");

            // Sort by priority
            Fields = Fields.OrderBy(f => f.Priority).ToList();

            _logger.LogInformation("Fields sorted by priority");
        }

        private List<FieldDefinition> GetFieldDefinitions()
        {
            return new List<FieldDefinition>
            {
                new FieldDefinition
                {
                    Key = "patient_firstname",
                    FriendlyName = "Patient first name",
                    HelpText = "The patient's given name (e.g., Jane)",
                    IsRequired = true,
                    Priority = 10,
                    TargetEntity = "Patient",
                    TargetProperty = "FirstName",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "patient_lastname",
                    FriendlyName = "Patient last name",
                    HelpText = "The patient's family name or surname (e.g., Smith)",
                    IsRequired = true,
                    Priority = 11,
                    TargetEntity = "Patient",
                    TargetProperty = "LastName",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "patient_dob",
                    FriendlyName = "Patient date of birth",
                    HelpText = "When the patient was born — helps us match patients correctly",
                    IsRequired = true,
                    Priority = 12,
                    TargetEntity = "Patient",
                    TargetProperty = "DateOfBirth",
                    DataType = "date"
                },
                new FieldDefinition
                {
                    Key = "patient_mrn",
                    FriendlyName = "Patient medical record number",
                    HelpText = "The hospital's unique ID for this patient (if available)",
                    IsRequired = false,
                    Priority = 13,
                    TargetEntity = "Patient",
                    TargetProperty = "MRN",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "patient_address",
                    FriendlyName = "Patient street address",
                    HelpText = "The patient's home address (used for geographic analysis)",
                    IsRequired = false,
                    Priority = 20,
                    TargetEntity = "Patient",
                    TargetProperty = "Address",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "patient_postcode",
                    FriendlyName = "Patient postcode",
                    HelpText = "The patient's postal code for region tracking",
                    IsRequired = false,
                    Priority = 21,
                    TargetEntity = "Patient",
                    TargetProperty = "Postcode",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "test_result",
                    FriendlyName = "Test result",
                    HelpText = "The outcome of the lab test (e.g., Positive, Negative, Detected)",
                    IsRequired = true,
                    Priority = 30,
                    TargetEntity = "LabResult",
                    TargetProperty = "Result",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "test_date",
                    FriendlyName = "Test date",
                    HelpText = "When the sample was collected or the test was performed",
                    IsRequired = true,
                    Priority = 31,
                    TargetEntity = "LabResult",
                    TargetProperty = "TestDate",
                    DataType = "date"
                },
                new FieldDefinition
                {
                    Key = "specimen_type",
                    FriendlyName = "Specimen type",
                    HelpText = "What was tested (e.g., nasal swab, blood, urine)",
                    IsRequired = false,
                    Priority = 40,
                    TargetEntity = "LabResult",
                    TargetProperty = "SpecimenType",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "test_name",
                    FriendlyName = "Test name (panel/order)",
                    HelpText = "The name of the overall test order or panel (e.g., 'Salmonella Culture and PCR'). Individual test results are captured separately.",
                    IsRequired = false,
                    Priority = 41,
                    TargetEntity = "LabResult",
                    TargetProperty = "TestName",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "accession_number",
                    FriendlyName = "Accession number",
                    HelpText = "The lab's unique identifier for this test",
                    IsRequired = false,
                    Priority = 42,
                    TargetEntity = "LabResult",
                    TargetProperty = "AccessionNumber",
                    DataType = "text"
                },
                new FieldDefinition
                {
                    Key = "ordering_provider",
                    FriendlyName = "Ordering provider",
                    HelpText = "The doctor or clinic that ordered the test (helps with contact tracing)",
                    IsRequired = false,
                    Priority = 50,
                    TargetEntity = "LabResult",
                    TargetProperty = "OrderingProvider",
                    DataType = "text"
                }
            };
        }

        private async Task<List<CandidateValue>> DetectCandidatesAsync(HL7ParseResult parseResult, FieldDefinition fieldDef)
        {
            var candidates = new List<CandidateValue>();

            // Extract potential values from the parsed result based on field type
            switch (fieldDef.Key)
            {
                case "patient_firstname":
                    if (parseResult.PatientData.TryGetValue("FirstName", out var firstName) && !string.IsNullOrEmpty(firstName))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = firstName,
                            DisplayValue = firstName,
                            Hint = "Found in patient name section",
                            FieldPath = "PID-5.2",
                            Confidence = 90
                        });
                    }
                    break;

                case "patient_lastname":
                    if (parseResult.PatientData.TryGetValue("LastName", out var lastName) && !string.IsNullOrEmpty(lastName))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = lastName,
                            DisplayValue = lastName,
                            Hint = "Found in patient name section",
                            FieldPath = "PID-5.1",
                            Confidence = 90
                        });
                    }
                    break;

                case "patient_dob":
                    // Check both DOB (V2.5) and DateOfBirth (V2.5.1) keys
                    string dobValue = null;
                    if (parseResult.PatientData.TryGetValue("DOB", out dobValue) || 
                        parseResult.PatientData.TryGetValue("DateOfBirth", out dobValue))
                    {
                        if (!string.IsNullOrEmpty(dobValue))
                        {
                            candidates.Add(new CandidateValue
                            {
                                Value = dobValue,
                                DisplayValue = FormatDateForDisplay(dobValue),
                                Hint = "Found in patient details — looks like a date of birth",
                                FieldPath = "PID-7",
                                Confidence = 95
                            });
                        }
                    }

                    // Also check if there's a test date that might be confused with DOB
                    if (parseResult.OrderData.TryGetValue("OrderDateTime", out var orderDate) && !string.IsNullOrEmpty(orderDate))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = orderDate,
                            DisplayValue = FormatDateForDisplay(orderDate),
                            Hint = "Found near test info — more likely the test date, not DOB",
                            FieldPath = "OBR-7",
                            Confidence = 20
                        });
                    }
                    break;

                case "patient_mrn":
                    if (parseResult.PatientData.TryGetValue("PatientId", out var mrn) && !string.IsNullOrEmpty(mrn))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = mrn,
                            DisplayValue = mrn,
                            Hint = "Patient identifier from hospital system",
                            FieldPath = "PID-3.1",
                            Confidence = 85
                        });
                    }
                    break;

                case "patient_address":
                    if (parseResult.PatientData.TryGetValue("Address", out var address) && !string.IsNullOrEmpty(address))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = address,
                            DisplayValue = address,
                            Hint = "Street address from patient details",
                            FieldPath = "PID-11",
                            Confidence = 80
                        });
                    }
                    break;

                case "patient_postcode":
                    if (parseResult.PatientData.TryGetValue("Zip", out var postcode) && !string.IsNullOrEmpty(postcode))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = postcode,
                            DisplayValue = postcode,
                            Hint = "Postal code from patient address",
                            FieldPath = "PID-11.5",
                            Confidence = 85
                        });
                    }
                    break;

                case "test_result":
                    // Check all OBX segments for results - there may be multiple (e.g., Culture and PCR)
                    for (int i = 0; i < parseResult.ResultData.Count; i++)
                    {
                        var resultData = parseResult.ResultData[i];
                        if (resultData.TryGetValue("Result", out var result) && !string.IsNullOrEmpty(result))
                        {
                            var resultTestName = resultData.GetValueOrDefault("TestName", "");
                            var resultTestCode = resultData.GetValueOrDefault("TestCode", "");

                            var hint = "Test outcome from lab result section";
                            if (!string.IsNullOrEmpty(resultTestName))
                            {
                                hint = $"Result for: {resultTestName}";
                            }
                            else if (!string.IsNullOrEmpty(resultTestCode))
                            {
                                hint = $"Result for test code: {resultTestCode}";
                            }

                            // If multiple results, show which OBX segment
                            var fieldPath = parseResult.ResultData.Count > 1 ? $"OBX[{i}]-5" : "OBX-5";

                            candidates.Add(new CandidateValue
                            {
                                Value = result,
                                DisplayValue = result,
                                Hint = hint,
                                FieldPath = fieldPath,
                                Confidence = 90
                            });
                        }
                    }
                    break;

                case "test_date":
                    if (parseResult.OrderData.TryGetValue("OrderDateTime", out var collDate) && !string.IsNullOrEmpty(collDate))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = collDate,
                            DisplayValue = FormatDateForDisplay(collDate),
                            Hint = "Sample collection date — when the test was taken",
                            FieldPath = "OBR-7",
                            Confidence = 90
                        });
                    }

                    var firstRes = parseResult.ResultData.FirstOrDefault();
                    if (firstRes != null && firstRes.TryGetValue("ObservationDateTime", out var obsDate) && !string.IsNullOrEmpty(obsDate))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = obsDate,
                            DisplayValue = FormatDateForDisplay(obsDate),
                            Hint = "Observation/result date — might be later than collection",
                            FieldPath = "OBX-14",
                            Confidence = 70
                        });
                    }
                    break;

                case "specimen_type":
                    // Check SPM segment first (dedicated specimen segment)
                    if (parseResult.SpecimenData.TryGetValue("SpecimenType", out var specimen) && !string.IsNullOrEmpty(specimen))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = specimen,
                            DisplayValue = FormatSpecimenType(specimen),
                            Hint = "Type of sample collected (from SPM segment)",
                            FieldPath = "SPM-4",
                            Confidence = 95
                        });
                    }

                    // Also check OBR-15 as fallback (many labs don't use SPM segment)
                    if (parseResult.OrderData.TryGetValue("SpecimenType", out var obrSpecimen) && !string.IsNullOrEmpty(obrSpecimen))
                    {
                        // Only add if different from SPM or if SPM wasn't found
                        if (candidates.All(c => c.Value != obrSpecimen))
                        {
                            candidates.Add(new CandidateValue
                            {
                                Value = obrSpecimen,
                                DisplayValue = FormatSpecimenType(obrSpecimen),
                                Hint = "Specimen source from order section (OBR-15)",
                                FieldPath = "OBR-15",
                                Confidence = 85
                            });
                        }
                    }

                    // If still no candidates, try to infer from test names (low confidence)
                    if (candidates.Count == 0)
                    {
                        var inferredSpecimens = InferSpecimenFromTestNames(parseResult);
                        foreach (var inferred in inferredSpecimens)
                        {
                            candidates.Add(inferred);
                        }
                    }
                    break;

                case "test_name":
                    // Check OBR first (order-level test name)
                    string testName;
                    if (parseResult.OrderData.TryGetValue("TestName", out testName) && !string.IsNullOrEmpty(testName))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = testName,
                            DisplayValue = testName,
                            Hint = "Test description from order",
                            FieldPath = "OBR-4.2",
                            Confidence = 90
                        });
                    }

                    // Also check OBX segments (result-level test names)
                    var firstTestResult = parseResult.ResultData.FirstOrDefault();
                    string testNameFromOBX;
                    if (firstTestResult != null && firstTestResult.TryGetValue("TestName", out testNameFromOBX) && !string.IsNullOrEmpty(testNameFromOBX))
                    {
                        // Only add if different from OBR test name
                        if (candidates.All(c => c.Value != testNameFromOBX))
                        {
                            candidates.Add(new CandidateValue
                            {
                                Value = testNameFromOBX,
                                DisplayValue = testNameFromOBX,
                                Hint = "Test name from result section",
                                FieldPath = "OBX-3.2",
                                Confidence = 85
                            });
                        }
                    }
                    break;

                case "accession_number":
                    if (parseResult.OrderData.TryGetValue("AccessionNumber", out var accession) && !string.IsNullOrEmpty(accession))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = accession,
                            DisplayValue = accession,
                            Hint = "Lab tracking number",
                            FieldPath = "OBR-3.1",
                            Confidence = 95
                        });
                    }
                    break;

                case "ordering_provider":
                    if (parseResult.OrderData.TryGetValue("OrderingProvider", out var provider) && !string.IsNullOrEmpty(provider))
                    {
                        candidates.Add(new CandidateValue
                        {
                            Value = provider,
                            DisplayValue = provider,
                            Hint = "Doctor who requested the test",
                            FieldPath = "OBR-16",
                            Confidence = 85
                        });
                    }
                    break;
            }

            return candidates.OrderByDescending(c => c.Confidence).ToList();
        }

        private async Task<string?> GetMappedDisplayValue(HL7ParseResult? parseResult, FieldDefinition fieldDef, string fieldPath)
        {
            if (parseResult == null) return null;

            // Try to extract the value to show what it would map to
            var dataDict = fieldDef.TargetEntity switch
            {
                "Patient" => parseResult.PatientData,
                "LabResult" => parseResult.ResultData.FirstOrDefault() ?? new Dictionary<string, string>(),
                _ => parseResult.OrderData
            };

            if (dataDict.TryGetValue(fieldDef.TargetProperty, out var value))
            {
                return fieldDef.DataType == "date" ? FormatDateForDisplay(value) : value;
            }

            return null;
        }

        private List<CandidateValue> InferSpecimenFromTestNames(HL7ParseResult parseResult)
        {
            var inferred = new List<CandidateValue>();
            var testNames = new List<string>();

            // Collect all test names
            if (parseResult.OrderData.TryGetValue("TestName", out var obrTestName) && !string.IsNullOrEmpty(obrTestName))
            {
                testNames.Add(obrTestName);
            }

            foreach (var result in parseResult.ResultData)
            {
                if (result.TryGetValue("TestName", out var obxTestName) && !string.IsNullOrEmpty(obxTestName))
                {
                    testNames.Add(obxTestName);
                }
            }

            // Try to infer specimen from test name keywords
            foreach (var testName in testNames.Distinct())
            {
                var lowerName = testName.ToLower();
                string? inferredType = null;
                string hint = "";

                if (lowerName.Contains("blood") || lowerName.Contains("serum") || lowerName.Contains("plasma"))
                {
                    inferredType = "Blood";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("stool") || lowerName.Contains("fecal") || lowerName.Contains("fec"))
                {
                    inferredType = "Stool";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("urine"))
                {
                    inferredType = "Urine";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("nasal") || lowerName.Contains("nasopharyngeal"))
                {
                    inferredType = "Nasal swab";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("throat"))
                {
                    inferredType = "Throat swab";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("sputum"))
                {
                    inferredType = "Sputum";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }
                else if (lowerName.Contains("csf") || lowerName.Contains("cerebrospinal"))
                {
                    inferredType = "Cerebrospinal fluid (CSF)";
                    hint = $"Inferred from test name '{testName}' — please verify";
                }

                if (inferredType != null && !inferred.Any(c => c.Value == inferredType))
                {
                    inferred.Add(new CandidateValue
                    {
                        Value = inferredType,
                        DisplayValue = inferredType,
                        Hint = hint,
                        FieldPath = "inferred",
                        Confidence = 40  // Low confidence - needs user verification
                    });
                }
            }

            return inferred;
        }

        private string FormatDateForDisplay(string dateValue)
        {
            if (DateTime.TryParse(dateValue, out var date))
            {
                return date.ToString("d MMMM yyyy");
            }

            // Try HL7 format YYYYMMDD
            if (dateValue.Length == 8 && dateValue.All(char.IsDigit))
            {
                if (DateTime.TryParseExact(dateValue, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return date.ToString("d MMMM yyyy");
                }
            }

            return dateValue;
        }

        private string FormatSpecimenType(string specimen)
        {
            return specimen.ToLower() switch
            {
                var s when s.Contains("nasal") || s.Contains("nasopharyngeal") => "Nasal swab",
                var s when s.Contains("throat") => "Throat swab",
                var s when s.Contains("blood") => "Blood",
                var s when s.Contains("urine") => "Urine",
                var s when s.Contains("stool") || s.Contains("fec") => "Stool",
                _ => specimen
            };
        }

        private string GetFieldPathHint(string fieldPath)
        {
            // Convert technical path to friendly hint
            if (fieldPath.StartsWith("PID")) return "patient information";
            if (fieldPath.StartsWith("OBR")) return "test order details";
            if (fieldPath.StartsWith("OBX")) return "test result";
            if (fieldPath.StartsWith("SPM")) return "specimen details";
            return "message field";
        }
    }

    public class FieldDefinition
    {
        public string Key { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int Priority { get; set; }
        public string TargetEntity { get; set; } = string.Empty;
        public string TargetProperty { get; set; } = string.Empty;
        public string DataType { get; set; } = "text"; // text, date, numeric
    }

    public class MappingFieldCard
    {
        public string FieldKey { get; set; } = string.Empty;
        public string FriendlyName { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; } = "NotFound"; // NotFound, NeedsAttention, Confirmed
        public string? ConfirmedValue { get; set; }
        public string? ExtractedHint { get; set; }
        public List<CandidateValue>? CandidateValues { get; set; }
    }

    public class CandidateValue
    {
        public string Value { get; set; } = string.Empty;
        public string DisplayValue { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string FieldPath { get; set; } = string.Empty;
        public int? Confidence { get; set; }
    }
}
