using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.HL7Generator.Models;
using Sentinel.HL7Generator.Services;
using Sentinel.Models;

namespace Sentinel.Services.HL7;

public class HL7TestMessageService : IHL7TestMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly HL7GeneratorService _generator;
    private readonly ILogger<HL7TestMessageService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HL7TestMessageService(
        ApplicationDbContext context,
        ILogger<HL7TestMessageService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _generator = new HL7GeneratorService();
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<GenerateMessageResult> GenerateAndSaveMessageAsync(
        HL7MessageRequest request,
        string outputPath,
        string? testComment = null,
        bool autoProcess = false,
        Guid? templateId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Generate HL7 message
            var rawHL7 = _generator.GenerateMessage(request);

            // Ensure output directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Generate unique filename if outputPath is a directory
            string filePath;
            if (Directory.Exists(outputPath))
            {
                var fileName = $"TEST_{request.AccessionNumber}_{DateTime.Now:yyyyMMddHHmmss}.hl7";
                filePath = Path.Combine(outputPath, fileName);
            }
            else
            {
                filePath = outputPath;
            }

            // Save to file
            await File.WriteAllTextAsync(filePath, rawHL7, cancellationToken);

            // Save to history
            var history = new HL7TestMessageHistory
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                RawHL7Message = rawHL7,
                FilePath = filePath,
                TestComment = testComment,
                AccessionNumber = request.AccessionNumber,
                PatientMRN = request.Patient.MRN,
                ConfigurationSnapshot = JsonSerializer.Serialize(request),
                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                WasAutoProcessed = autoProcess
            };

            _context.HL7TestMessageHistory.Add(history);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Generated test HL7 message: {AccessionNumber} saved to {FilePath}",
                request.AccessionNumber,
                filePath);

            return new GenerateMessageResult
            {
                Success = true,
                HistoryId = history.Id,
                FilePath = filePath,
                AccessionNumber = request.AccessionNumber,
                PatientMRN = request.Patient.MRN,
                MessageControlId = request.MessageControlId,
                RawHL7 = rawHL7
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating HL7 test message");
            return new GenerateMessageResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<BatchGenerateResult> GenerateMultipleMessagesAsync(
        HL7MessageRequest baseRequest,
        string outputPath,
        int count,
        bool varyPatient,
        bool varyProvider,
        bool varyAccession,
        string? testComment = null,
        bool autoProcess = false,
        CancellationToken cancellationToken = default)
    {
        var result = new BatchGenerateResult();

        try
        {
            var messages = _generator.GenerateMultipleMessages(
                baseRequest,
                count,
                varyPatient,
                varyProvider,
                varyAccession,
                varyTimestamp: true);

            for (int i = 0; i < messages.Count; i++)
            {
                var rawHL7 = messages[i];

                // Parse accession and MRN from generated message
                var lines = rawHL7.Split('\n');
                var obrLine = lines.FirstOrDefault(l => l.StartsWith("OBR"));
                var pidLine = lines.FirstOrDefault(l => l.StartsWith("PID"));

                var accession = obrLine?.Split('|')[3] ?? $"ACC{i:D5}";
                var mrn = pidLine?.Split('|')[3]?.Split('^')[0] ?? $"MRN{i:D8}";

                var fileName = $"TEST_{accession}_{DateTime.Now:yyyyMMddHHmmss}_{i:D3}.hl7";
                var filePath = Path.Combine(outputPath, fileName);

                await File.WriteAllTextAsync(filePath, rawHL7, cancellationToken);

                var history = new HL7TestMessageHistory
                {
                    Id = Guid.NewGuid(),
                    RawHL7Message = rawHL7,
                    FilePath = filePath,
                    TestComment = $"{testComment} (Batch {i + 1}/{count})",
                    AccessionNumber = accession,
                    PatientMRN = mrn,
                    ConfigurationSnapshot = JsonSerializer.Serialize(baseRequest),
                    GeneratedAt = DateTime.UtcNow,
                    GeneratedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                    WasAutoProcessed = autoProcess
                };

                _context.HL7TestMessageHistory.Add(history);

                result.Results.Add(new GenerateMessageResult
                {
                    Success = true,
                    HistoryId = history.Id,
                    FilePath = filePath,
                    AccessionNumber = accession,
                    PatientMRN = mrn,
                    RawHL7 = rawHL7
                });
            }

            await _context.SaveChangesAsync(cancellationToken);

            result.Success = true;
            result.TotalGenerated = messages.Count;

            _logger.LogInformation("Generated {Count} test HL7 messages to {Path}", count, outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating batch of HL7 messages");
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<HL7TestMessageTemplate> SaveTemplateAsync(
        string templateName,
        HL7MessageRequest request,
        string? description = null,
        string? testComment = null,
        CancellationToken cancellationToken = default)
    {
        var template = new HL7TestMessageTemplate
        {
            Id = Guid.NewGuid(),
            TemplateName = templateName,
            Description = description,
            LabTemplateType = request.LabTemplate,
            ConfigurationJson = JsonSerializer.Serialize(request),
            TestComment = testComment,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name
        };

        _context.HL7TestMessageTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Saved HL7 test template: {TemplateName}", templateName);

        return template;
    }

    public async Task<HL7MessageRequest?> LoadTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.HL7TestMessageTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template == null)
            return null;

        return JsonSerializer.Deserialize<HL7MessageRequest>(template.ConfigurationJson);
    }

    public async Task<List<HL7TestMessageTemplate>> GetTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.HL7TestMessageTemplates
            .OrderByDescending(t => t.IsFavorite)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteTemplateAsync(
        Guid templateId,
        CancellationToken cancellationToken = default)
    {
        var template = await _context.HL7TestMessageTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);

        if (template != null)
        {
            _context.HL7TestMessageTemplates.Remove(template);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<HL7TestMessageHistory>> GetRecentHistoryAsync(
        int count = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.HL7TestMessageHistory
            .Include(h => h.Template)
            .Include(h => h.HL7Message)
            .OrderByDescending(h => h.GeneratedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<TestMessageProcessingResult?> GetProcessingResultAsync(
        Guid historyId,
        CancellationToken cancellationToken = default)
    {
        var history = await _context.HL7TestMessageHistory
            .Include(h => h.HL7Message)
                .ThenInclude(m => m!.Patient)
            .Include(h => h.HL7Message)
                .ThenInclude(m => m!.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                        .ThenInclude(m => m.Pathogen)
            .Include(h => h.HL7Message)
                .ThenInclude(m => m!.LabResult)
                    .ThenInclude(lr => lr!.Case)
                        .ThenInclude(c => c!.Disease)
            .FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);

        if (history == null)
            return null;

        var result = new TestMessageProcessingResult
        {
            HistoryId = history.Id,
            HL7MessageId = history.HL7MessageId,
            Status = history.HL7Message?.Status
        };

        if (history.HL7Message != null)
        {
            var msg = history.HL7Message;

            if (msg.Patient != null)
            {
                result.PatientId = msg.PatientId;
                result.PatientName = $"{msg.Patient.GivenName} {msg.Patient.FamilyName}";
            }

            if (msg.LabResult != null)
            {
                result.LabResultId = msg.LabResultId;

                if (msg.LabResult.Markers != null)
                {
                    result.BiomarkersMapped = msg.LabResult.Markers.Select(m => new BiomarkerMapping
                    {
                        TestCode = m.TestCode ?? string.Empty,
                        TestName = m.LOINCCode ?? m.TestCode ?? string.Empty,
                        Result = m.QualitativeResultText ?? m.QuantitativeValue?.ToString() ?? string.Empty,
                        PathogenId = m.PathogenId,
                        PathogenName = m.Pathogen?.Name,
                        WasMapped = m.PathogenId.HasValue
                    }).ToList();
                }

                if (msg.LabResult.Case != null)
                {
                    result.CasesCreated = new List<CaseCreation>
                    {
                        new CaseCreation
                        {
                            CaseId = msg.LabResult.Case.Id,
                            CaseFriendlyId = msg.LabResult.Case.FriendlyId ?? string.Empty,
                            DiseaseName = msg.LabResult.Case.Disease?.Name ?? string.Empty,
                            WasAutoCreated = true // We'd need to track this
                        }
                    };
                }
            }

            result.ProcessedAt = msg.ProcessedAt;

            if (!string.IsNullOrEmpty(msg.ErrorMessage))
            {
                result.Errors.Add(msg.ErrorMessage);
            }

            if (!string.IsNullOrEmpty(msg.ProcessingNotes))
            {
                result.Warnings.Add(msg.ProcessingNotes);
            }
        }

        return result;
    }

    public async Task<GenerateMessageResult> RegenerateWithNewPatientAsync(
        Guid historyId,
        string outputPath,
        bool autoProcess = false,
        CancellationToken cancellationToken = default)
    {
        var history = await _context.HL7TestMessageHistory
            .FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);

        if (history == null || string.IsNullOrEmpty(history.ConfigurationSnapshot))
        {
            return new GenerateMessageResult
            {
                Success = false,
                ErrorMessage = "History not found or configuration missing"
            };
        }

        var request = JsonSerializer.Deserialize<HL7MessageRequest>(history.ConfigurationSnapshot);
        if (request == null)
        {
            return new GenerateMessageResult
            {
                Success = false,
                ErrorMessage = "Failed to deserialize configuration"
            };
        }

        // Generate new patient and accession
        request.Patient = _generator.GenerateRandomPatient();
        request.AccessionNumber = _generator.GenerateAccessionNumber();
        request.MessageControlId = _generator.GenerateMessageControlId();
        request.MessageDateTime = DateTime.Now;
        request.CollectionDateTime = DateTime.Now.AddHours(-Random.Shared.Next(1, 48));

        return await GenerateAndSaveMessageAsync(
            request,
            outputPath,
            history.TestComment,
            autoProcess,
            history.TemplateId,
            cancellationToken);
    }

    public async Task<GenerateMessageResult> CloneMessageAsync(
        Guid historyId,
        string outputPath,
        bool autoProcess = false,
        CancellationToken cancellationToken = default)
    {
        var history = await _context.HL7TestMessageHistory
            .FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);

        if (history == null)
        {
            return new GenerateMessageResult
            {
                Success = false,
                ErrorMessage = "History not found"
            };
        }

        // Just save the same HL7 message to a new file
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string filePath;
        if (Directory.Exists(outputPath))
        {
            var fileName = $"TEST_CLONE_{history.AccessionNumber}_{DateTime.Now:yyyyMMddHHmmss}.hl7";
            filePath = Path.Combine(outputPath, fileName);
        }
        else
        {
            filePath = outputPath;
        }

        await File.WriteAllTextAsync(filePath, history.RawHL7Message, cancellationToken);

        var newHistory = new HL7TestMessageHistory
        {
            Id = Guid.NewGuid(),
            TemplateId = history.TemplateId,
            RawHL7Message = history.RawHL7Message,
            FilePath = filePath,
            TestComment = $"Clone of: {history.TestComment}",
            AccessionNumber = history.AccessionNumber,
            PatientMRN = history.PatientMRN,
            ConfigurationSnapshot = history.ConfigurationSnapshot,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
            WasAutoProcessed = autoProcess
        };

        _context.HL7TestMessageHistory.Add(newHistory);
        await _context.SaveChangesAsync(cancellationToken);

        return new GenerateMessageResult
        {
            Success = true,
            HistoryId = newHistory.Id,
            FilePath = filePath,
            AccessionNumber = history.AccessionNumber,
            PatientMRN = history.PatientMRN,
            RawHL7 = history.RawHL7Message
        };
    }

    public PatientInfo GenerateRandomPatient()
    {
        return _generator.GenerateRandomPatient();
    }

    public ProviderInfo GenerateRandomProvider()
    {
        return _generator.GenerateRandomProvider();
    }

    public string GenerateAccessionNumber()
    {
        return _generator.GenerateAccessionNumber();
    }

    public async Task<List<PatientSelectionItem>> GetPatientsForSelectionAsync(
        string? searchTerm = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(p =>
                p.FriendlyId.Contains(searchTerm) ||
                p.GivenName.Contains(searchTerm) ||
                p.FamilyName.Contains(searchTerm));
        }

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Take(maxResults)
            .Select(p => new PatientSelectionItem
            {
                Id = p.Id,
                MRN = p.FriendlyId,
                FullName = $"{p.GivenName} {p.FamilyName}",
                DateOfBirth = p.DateOfBirth ?? DateTime.MinValue,
                Gender = p.Gender != null ? p.Gender.Name : "Unknown"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ProviderSelectionItem>> GetProvidersForSelectionAsync(
        string? searchTerm = null,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Organizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(o => o.Name.Contains(searchTerm));
        }

        return await query
            .Include(o => o.OrganizationType)
            .OrderBy(o => o.Name)
            .Take(maxResults)
            .Select(o => new ProviderSelectionItem
            {
                Id = o.Id,
                Name = o.Name,
                NPI = null, // Organization doesn't have NPI in your model
                OrganizationType = o.OrganizationType != null ? o.OrganizationType.Name : "Unknown"
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<HL7Configuration>> GetActiveConfigurationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.HL7Configurations
            .Where(c => c.IsActive)
            .OrderBy(c => c.ConfigurationName)
            .ToListAsync(cancellationToken);
    }
}
