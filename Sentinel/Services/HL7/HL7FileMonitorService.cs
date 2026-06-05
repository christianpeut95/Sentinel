using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.HL7;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Monitors file system locations for incoming HL7 messages and processes them automatically
    /// </summary>
    public class HL7FileMonitorService : IHL7FileMonitorService, IDisposable
    {
        private readonly ILogger<HL7FileMonitorService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly SemaphoreSlim _processingSemaphore = new(5); // Max 5 concurrent file processing
        private readonly HashSet<string> _processingFiles = new(); // Track files currently being processed
        private readonly object _processingFilesLock = new(); // Lock for _processingFiles
        private bool _isMonitoring = false;
        private DateTime? _monitoringStartedAt;
        private int _filesProcessedToday = 0;
        private int _filesFailedToday = 0;
        private DateTime? _lastFileProcessedAt;
        private string? _lastFileProcessed;
        private readonly object _statsLock = new();

        public HL7FileMonitorService(
            ILogger<HL7FileMonitorService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("File monitoring is already active");
                return;
            }

            _logger.LogInformation("Starting HL7 file monitoring service");

            // Create a scope to access the database
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Load all active configurations with file drop locations
                var configurations = await context.HL7Configurations
                    .Where(c => c.IsActive && !string.IsNullOrEmpty(c.FileDropPath))
                    .ToListAsync(cancellationToken);

                if (!configurations.Any())
                {
                    _logger.LogWarning("No active HL7 configurations with file drop paths found");
                    return;
                }

                foreach (var config in configurations)
                {
                    try
                    {
                        var path = config.FileDropPath!;

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            _logger.LogInformation("Created file drop directory: {Path}", path);
                        }

                        // Create FileSystemWatcher
                        var watcher = new FileSystemWatcher(path)
                        {
                            Filter = "*.hl7",
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                            EnableRaisingEvents = true
                        };

                        // Also watch for .txt files (some systems use .txt for HL7)
                        watcher.Filters.Add("*.txt");

                        // Wire up event handlers
                        watcher.Created += async (sender, e) => await OnFileCreatedAsync(e.FullPath, config.Id);
                        watcher.Error += (sender, e) => OnWatcherError(e.GetException(), path);

                        _watchers.Add(watcher);
                        _logger.LogInformation("Started monitoring: {Path} (Configuration: {ConfigName})", 
                            path, config.ConfigurationName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start monitoring path: {Path} for configuration: {ConfigName}", 
                            config.FileDropPath, config.ConfigurationName);
                    }
                }
            }

            _isMonitoring = true;
            _monitoringStartedAt = DateTime.UtcNow;

            // Reset daily stats if it's a new day
            ResetDailyStatsIfNeeded();

            _logger.LogInformation("HL7 file monitoring started with {Count} active watchers", _watchers.Count);
        }

        public Task StopMonitoringAsync()
        {
            _logger.LogInformation("Stopping HL7 file monitoring service");

            foreach (var watcher in _watchers)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }

            _watchers.Clear();
            _isMonitoring = false;
            _monitoringStartedAt = null;

            _logger.LogInformation("HL7 file monitoring stopped");
            return Task.CompletedTask;
        }

        public async Task<FileProcessingResult> ProcessFileAsync(
            string filePath,
            Guid? configurationId = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FileProcessingResult
            {
                FilePath = filePath,
                FileName = Path.GetFileName(filePath),
                ProcessedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Processing HL7 file: {FilePath}", filePath);

                // Check if file exists
                if (!File.Exists(filePath))
                {
                    _logger.LogError("File not found at start of processing: {FilePath}", filePath);
                    result.Errors.Add("File not found");
                    return result;
                }

                // Read file content
                string hl7Content;
                try
                {
                    _logger.LogDebug("Reading file content: {FilePath}", filePath);
                    // Wait a moment to ensure file is fully written
                    await Task.Delay(500, cancellationToken);
                    hl7Content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    _logger.LogDebug("Successfully read {Length} characters from: {FilePath}", hl7Content.Length, filePath);
                }
                catch (IOException ex)
                {
                    _logger.LogWarning(ex, "File is locked, will retry: {FilePath}", filePath);
                    await Task.Delay(2000, cancellationToken);
                    hl7Content = await File.ReadAllTextAsync(filePath, cancellationToken);
                    _logger.LogDebug("Successfully read file after retry: {FilePath}", filePath);
                }

                if (string.IsNullOrWhiteSpace(hl7Content))
                {
                    _logger.LogError("File is empty: {FilePath}", filePath);
                    result.Errors.Add("File is empty");
                    result.Success = false;
                    await MoveFileToErrorAsync(filePath, result, "Empty file");
                    return result;
                }

                // Create scope to access scoped services
                _logger.LogDebug("Creating service scope for: {FilePath}", filePath);
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    _logger.LogDebug("Resolving services for: {FilePath}", filePath);
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var parserService = scope.ServiceProvider.GetRequiredService<IHL7ParserService>();
                    var extractionService = scope.ServiceProvider.GetRequiredService<IHL7DataExtractionService>();

                    // Load configuration if specified
                    HL7Configuration? configuration = null;
                    if (configurationId.HasValue)
                    {
                        _logger.LogDebug("Loading HL7 configuration: {ConfigId}", configurationId.Value);
                        configuration = await context.HL7Configurations
                            .FirstOrDefaultAsync(c => c.Id == configurationId.Value, cancellationToken);
                        if (configuration == null)
                        {
                            _logger.LogWarning("HL7 configuration not found: {ConfigId}", configurationId.Value);
                        }
                    }

                    // Parse HL7 message
                    _logger.LogInformation("Parsing HL7 message from: {FilePath}", filePath);
                    var parsedMessage = await parserService.ParseMessageAsync(
                        hl7Content,
                        configurationId,
                        cancellationToken);

                    result.HL7MessageId = parsedMessage?.Id;

                    if (parsedMessage == null || parsedMessage.Status == HL7ProcessingStatus.ParsingFailed)
                    {
                        _logger.LogError("HL7 parsing failed for: {FilePath} → Error: {Error}", 
                            filePath, parsedMessage?.ErrorMessage ?? "Unknown parsing error");
                        result.Errors.Add(parsedMessage?.ErrorMessage ?? "Parsing failed");
                        result.Success = false;
                        await MoveFileToErrorAsync(filePath, result, parsedMessage?.ErrorMessage ?? "Parsing failed");
                        return result;
                    }

                    _logger.LogInformation("Successfully parsed HL7 message: {MessageId} from: {FilePath}", 
                        parsedMessage.Id, filePath);

                    // Extract and create entities using NEW STAGING WORKFLOW
                    _logger.LogInformation("Extracting and creating entities from HL7 message: {MessageId}", parsedMessage.Id);
                    var extractionResult = await extractionService.ExtractAndCreateEntitiesWithStagingAsync(
                        parsedMessage,
                        configuration,
                        cancellationToken);

                    result.PatientId = extractionResult.Patient?.Id;
                    result.LabResultId = extractionResult.LabResult?.Id;
                    result.Warnings.AddRange(extractionResult.Warnings);

                    // Parse case information from warnings
                    ParseCaseInformationFromWarnings(extractionResult.Warnings, result);

                    if (!extractionResult.Success || extractionResult.Errors.Any())
                    {
                        _logger.LogError("Entity extraction failed for: {FilePath} → Errors: {Errors}", 
                            filePath, string.Join("; ", extractionResult.Errors));
                        result.Errors.AddRange(extractionResult.Errors);
                        result.Success = false;

                        if (extractionResult.RequiresManualReview)
                        {
                            _logger.LogWarning("File requires manual review: {FilePath} → Reason: {Reason}", 
                                filePath, extractionResult.ManualReviewReason);
                            await MoveFileToReviewAsync(filePath, result, extractionResult.ManualReviewReason);
                        }
                        else
                        {
                            await MoveFileToErrorAsync(filePath, result, string.Join("; ", extractionResult.Errors));
                        }

                        return result;
                    }

                    // Success! Move to processed folder
                    _logger.LogInformation("Entity extraction successful for: {FilePath} → Patient: {PatientId}, LabResult: {LabResultId}", 
                        filePath, result.PatientId, result.LabResultId);
                    result.Success = true;
                    await MoveFileToProcessedAsync(filePath, result);

                    _logger.LogInformation(
                        "Successfully processed HL7 file: {FileName} → Patient: {PatientId}, LabResult: {LabResultId}, Cases: {Cases}",
                        result.FileName, result.PatientId, result.LabResultId, 
                        string.Join(", ", result.CasesCreated.Concat(result.CasesLinked)));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception processing HL7 file: {FilePath} → Exception Type: {ExceptionType} → Message: {Message} → StackTrace: {StackTrace}", 
                    filePath, ex.GetType().Name, ex.Message, ex.StackTrace);
                result.Errors.Add($"Processing failed: {ex.Message}");
                result.Success = false;

                try
                {
                    // Check if file still exists before attempting move
                    if (File.Exists(filePath))
                    {
                        _logger.LogInformation("File still exists after exception, moving to error folder: {FilePath}", filePath);
                        await MoveFileToErrorAsync(filePath, result, ex.Message);
                    }
                    else
                    {
                        _logger.LogWarning("File no longer exists after exception, cannot move to error folder: {FilePath}", filePath);
                    }
                }
                catch (Exception moveEx)
                {
                    _logger.LogError(moveEx, "Failed to move error file: {FilePath} → Move Exception: {MoveException}", 
                        filePath, moveEx.Message);
                }

                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingDuration = stopwatch.Elapsed;

                UpdateStats(result.Success);
            }
        }

        public async Task<BatchProcessingResult> ProcessDirectoryAsync(
            string directoryPath,
            Guid? configurationId = null,
            bool includeSubdirectories = false,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new BatchProcessingResult
            {
                StartedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting batch processing of directory: {Path}", directoryPath);

                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogError("Directory not found: {Path}", directoryPath);
                    result.CompletedAt = DateTime.UtcNow;
                    return result;
                }

                // Find all HL7 files
                var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                var files = Directory.GetFiles(directoryPath, "*.hl7", searchOption)
                    .Concat(Directory.GetFiles(directoryPath, "*.txt", searchOption))
                    .ToList();

                result.TotalFiles = files.Count;

                _logger.LogInformation("Found {Count} files to process", files.Count);

                // Process files with throttling
                var tasks = files.Select(async filePath =>
                {
                    await _processingSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var fileResult = await ProcessFileAsync(filePath, configurationId, cancellationToken);
                        return fileResult;
                    }
                    finally
                    {
                        _processingSemaphore.Release();
                    }
                });

                var fileResults = await Task.WhenAll(tasks);

                result.Results.AddRange(fileResults);
                result.SuccessCount = fileResults.Count(r => r.Success);
                result.FailureCount = fileResults.Count(r => !r.Success && r.Errors.Any());
                result.SkippedCount = result.TotalFiles - result.SuccessCount - result.FailureCount;

                _logger.LogInformation(
                    "Batch processing complete: {Total} files, {Success} succeeded, {Failed} failed, {Skipped} skipped",
                    result.TotalFiles, result.SuccessCount, result.FailureCount, result.SkippedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during batch processing of directory: {Path}", directoryPath);
                return result;
            }
            finally
            {
                stopwatch.Stop();
                result.TotalDuration = stopwatch.Elapsed;
                result.CompletedAt = DateTime.UtcNow;
            }
        }

        public MonitoringStatus GetMonitoringStatus()
        {
            lock (_statsLock)
            {
                ResetDailyStatsIfNeeded();

                return new MonitoringStatus
                {
                    IsMonitoring = _isMonitoring,
                    ActiveWatchers = _watchers.Count,
                    MonitoredPaths = _watchers.Select(w => w.Path).ToList(),
                    MonitoringStartedAt = _monitoringStartedAt,
                    FilesProcessedToday = _filesProcessedToday,
                    FilesFailedToday = _filesFailedToday,
                    LastFileProcessedAt = _lastFileProcessedAt,
                    LastFileProcessed = _lastFileProcessed
                };
            }
        }

        #region Private Helper Methods

        private async Task OnFileCreatedAsync(string filePath, Guid configurationId)
        {
            try
            {
                _logger.LogInformation("New file detected: {FilePath}", filePath);

                // Check if this file is already being processed (prevents duplicate FileSystemWatcher events)
                lock (_processingFilesLock)
                {
                    if (_processingFiles.Contains(filePath))
                    {
                        _logger.LogWarning("File is already being processed, ignoring duplicate event: {FilePath}", filePath);
                        return;
                    }
                    _processingFiles.Add(filePath);
                }

                try
                {
                    // Small delay to ensure file is fully written
                    await Task.Delay(1000);

                    // Only process if file still exists (might have been moved by another process)
                    if (!File.Exists(filePath))
                    {
                        _logger.LogWarning("File no longer exists, skipping: {FilePath}", filePath);
                        return;
                    }

                    // Process the file directly - ProcessFileAsync already creates its own scope
                    // No need to create an additional scope here as IHL7FileMonitorService is a singleton
                    _logger.LogInformation("Starting ProcessFileAsync for: {FilePath}", filePath);
                    var result = await ProcessFileAsync(filePath, configurationId);

                    if (result.Success)
                    {
                        _logger.LogInformation("Successfully processed file: {FilePath} → Moved to: {MovedTo}", filePath, result.MovedToPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to process file: {FilePath} → Errors: {Errors} → Moved to: {MovedTo}", 
                            filePath, string.Join("; ", result.Errors), result.MovedToPath ?? "NOT MOVED");
                    }
                }
                finally
                {
                    // Always remove from processing set when done
                    lock (_processingFilesLock)
                    {
                        _processingFiles.Remove(filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in file created event handler for: {FilePath}", filePath);

                // Remove from processing set on error
                lock (_processingFilesLock)
                {
                    _processingFiles.Remove(filePath);
                }

                // If the file still exists, try to move it to error folder
                try
                {
                    if (File.Exists(filePath))
                    {
                        _logger.LogInformation("Attempting to move orphaned file to error folder: {FilePath}", filePath);
                        var errorResult = new FileProcessingResult
                        {
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            ProcessedAt = DateTime.UtcNow,
                            Success = false
                        };
                        errorResult.Errors.Add($"Unhandled exception: {ex.Message}");
                        await MoveFileToErrorAsync(filePath, errorResult, $"Event handler exception: {ex.Message}");
                    }
                }
                catch (Exception moveEx)
                {
                    _logger.LogError(moveEx, "Failed to move orphaned file to error folder: {FilePath}", filePath);
                }
            }
        }

        private void OnWatcherError(Exception? exception, string path)
        {
            _logger.LogError(exception, "File watcher error for path: {Path}", path);
        }

        private async Task MoveFileToProcessedAsync(string sourceFile, FileProcessingResult result)
        {
            try
            {
                var sourceDir = Path.GetDirectoryName(sourceFile)!;
                var processedDir = Path.Combine(sourceDir, "Processed", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(processedDir);

                var fileName = Path.GetFileName(sourceFile);
                var destinationFile = Path.Combine(processedDir, $"{DateTime.UtcNow:HHmmss}_{fileName}");

                File.Move(sourceFile, destinationFile, overwrite: true);
                result.MovedToPath = destinationFile;

                _logger.LogDebug("Moved file to processed: {Destination}", destinationFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file to processed folder: {FilePath}", sourceFile);
                result.Warnings.Add($"Could not move file to processed folder: {ex.Message}");
            }
        }

        private async Task MoveFileToErrorAsync(string sourceFile, FileProcessingResult result, string errorReason)
        {
            try
            {
                var sourceDir = Path.GetDirectoryName(sourceFile)!;
                var errorDir = Path.Combine(sourceDir, "Error", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(errorDir);

                var fileName = Path.GetFileName(sourceFile);
                var destinationFile = Path.Combine(errorDir, $"{DateTime.UtcNow:HHmmss}_{fileName}");

                File.Move(sourceFile, destinationFile, overwrite: true);
                result.MovedToPath = destinationFile;

                // Create error log file
                var errorLogPath = Path.ChangeExtension(destinationFile, ".error.txt");
                await File.WriteAllTextAsync(errorLogPath, 
                    $"Error Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Error Reason: {errorReason}\n" +
                    $"Errors:\n{string.Join("\n", result.Errors)}\n");

                _logger.LogDebug("Moved file to error: {Destination}", destinationFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file to error folder: {FilePath}", sourceFile);
                result.Warnings.Add($"Could not move file to error folder: {ex.Message}");
            }
        }

        private async Task MoveFileToReviewAsync(string sourceFile, FileProcessingResult result, string? reviewReason)
        {
            try
            {
                var sourceDir = Path.GetDirectoryName(sourceFile)!;
                var reviewDir = Path.Combine(sourceDir, "Review", DateTime.UtcNow.ToString("yyyy-MM-dd"));
                Directory.CreateDirectory(reviewDir);

                var fileName = Path.GetFileName(sourceFile);
                var destinationFile = Path.Combine(reviewDir, $"{DateTime.UtcNow:HHmmss}_{fileName}");

                File.Move(sourceFile, destinationFile, overwrite: true);
                result.MovedToPath = destinationFile;

                // Create review log file
                var reviewLogPath = Path.ChangeExtension(destinationFile, ".review.txt");
                await File.WriteAllTextAsync(reviewLogPath,
                    $"Review Required Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\n" +
                    $"Review Reason: {reviewReason ?? "Manual review required"}\n" +
                    $"Warnings:\n{string.Join("\n", result.Warnings)}\n");

                _logger.LogDebug("Moved file to review: {Destination}", destinationFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to move file to review folder: {FilePath}", sourceFile);
                result.Warnings.Add($"Could not move file to review folder: {ex.Message}");
            }
        }

        private void ParseCaseInformationFromWarnings(List<string> warnings, FileProcessingResult result)
        {
            foreach (var warning in warnings)
            {
                if (warning.Contains("Created") && warning.Contains("case"))
                {
                    // Extract case IDs from "Created N case(s): C-2026-0001, C-2026-0002"
                    var parts = warning.Split(':');
                    if (parts.Length > 1)
                    {
                        var caseIds = parts[1].Split(',').Select(s => s.Trim()).Where(s => s.StartsWith("C-"));
                        result.CasesCreated.AddRange(caseIds);
                    }
                }
                else if (warning.Contains("Linked to") && warning.Contains("case"))
                {
                    // Extract case IDs from "Linked to N case(s): C-2026-0001"
                    var parts = warning.Split(':');
                    if (parts.Length > 1)
                    {
                        var caseIds = parts[1].Split(',').Select(s => s.Trim()).Where(s => s.StartsWith("C-"));
                        result.CasesLinked.AddRange(caseIds);
                    }
                }
            }
        }

        private void UpdateStats(bool success)
        {
            lock (_statsLock)
            {
                ResetDailyStatsIfNeeded();

                if (success)
                {
                    _filesProcessedToday++;
                }
                else
                {
                    _filesFailedToday++;
                }

                _lastFileProcessedAt = DateTime.UtcNow;
            }
        }

        private void ResetDailyStatsIfNeeded()
        {
            if (_lastFileProcessedAt.HasValue && 
                _lastFileProcessedAt.Value.Date < DateTime.UtcNow.Date)
            {
                _filesProcessedToday = 0;
                _filesFailedToday = 0;
            }
        }

        #endregion

        #region Testing and Management Methods

        public async Task<FileProcessingResult> ReprocessMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reprocessing HL7 message {MessageId}", messageId);

            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var extractionService = scope.ServiceProvider.GetRequiredService<IHL7DataExtractionService>();

            // Load the message with all related data
            var message = await context.HL7Messages
                .Include(m => m.Patient)
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                .Include(m => m.ParsingIssues)
                .Include(m => m.Segments)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
            {
                throw new InvalidOperationException($"HL7 Message {messageId} not found");
            }

            _logger.LogInformation("Found message {MessageControlId}, deleting related data...", message.MessageControlId);

            // Delete associated data to allow reprocessing
            if (message.LabResult != null)
            {
                // Delete associated cases
                var casesToDelete = await context.Cases
                    .Where(c => c.LabResults.Any(lr => lr.Id == message.LabResult.Id))
                    .ToListAsync(cancellationToken);

                if (casesToDelete.Any())
                {
                    _logger.LogInformation("Deleting {Count} cases associated with lab result", casesToDelete.Count);
                    context.Cases.RemoveRange(casesToDelete);
                }

                // Delete lab result and markers
                _logger.LogInformation("Deleting lab result {LabResultId} and its markers", message.LabResult.Id);
                context.LabResults.Remove(message.LabResult);
            }

            // Delete parsing issues and segments to allow clean re-parse
            if (message.ParsingIssues.Any())
            {
                _logger.LogInformation("Deleting {Count} parsing issues", message.ParsingIssues.Count);
                context.RemoveRange(message.ParsingIssues);
            }

            if (message.Segments.Any())
            {
                _logger.LogInformation("Deleting {Count} message segments", message.Segments.Count);
                context.RemoveRange(message.Segments);
            }

            // Reset message status
            message.Status = HL7ProcessingStatus.Received;
            message.ProcessedAt = null;
            message.ErrorMessage = null;
            message.ProcessingNotes = null;
            message.PatientId = null;
            message.LabResultId = null;
            message.LaboratoryOrganizationId = null;
            message.OrderingProviderOrganizationId = null;

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Message reset, reprocessing...");

            // Load configuration if specified
            HL7Configuration? configuration = null;
            if (message.ConfigurationId.HasValue)
            {
                configuration = await context.HL7Configurations
                    .FirstOrDefaultAsync(c => c.Id == message.ConfigurationId, cancellationToken);
            }

            // Reprocess
            var extractionResult = await extractionService.ExtractAndCreateEntitiesAsync(
                message,
                configuration,
                cancellationToken);

            var result = new FileProcessingResult
            {
                Success = extractionResult.Success,
                FileName = $"Reprocessed-{message.MessageControlId}",
                ProcessedAt = DateTime.UtcNow,
                HL7MessageId = message.Id,
                PatientId = extractionResult.Patient?.Id,
                LabResultId = extractionResult.LabResult?.Id,
                Warnings = extractionResult.Warnings,
                Errors = extractionResult.Errors
            };

            // Parse case information from warnings
            ParseCaseInformationFromWarnings(extractionResult.Warnings, result);

            _logger.LogInformation("Reprocessing complete. Success: {Success}, Patient: {PatientId}, LabResult: {LabResultId}",
                result.Success, result.PatientId, result.LabResultId);

            return result;
        }

        public async Task<int> ClearTestDataAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("⚠️ Clearing ALL test HL7 data...");

            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            int deletedCount = 0;

            // Get all HL7 messages with all related data
            var messages = await context.HL7Messages
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                .Include(m => m.Patient)
                .Include(m => m.ParsingIssues)
                .Include(m => m.Segments)
                .ToListAsync(cancellationToken);

            foreach (var message in messages)
            {
                if (message.LabResult != null)
                {
                    // Delete associated cases
                    var cases = await context.Cases
                        .Where(c => c.LabResults.Any(lr => lr.Id == message.LabResult.Id))
                        .ToListAsync(cancellationToken);

                    if (cases.Any())
                    {
                        context.Cases.RemoveRange(cases);
                        _logger.LogDebug("Deleted {Count} cases for message {MessageId}", cases.Count, message.MessageControlId);
                    }

                    // Delete lab result and markers (cascade should handle markers)
                    context.LabResults.Remove(message.LabResult);
                }

                // Delete parsing issues and segments
                if (message.ParsingIssues.Any())
                {
                    context.RemoveRange(message.ParsingIssues);
                    _logger.LogDebug("Deleted {Count} parsing issues for message {MessageId}", message.ParsingIssues.Count, message.MessageControlId);
                }

                if (message.Segments.Any())
                {
                    context.RemoveRange(message.Segments);
                    _logger.LogDebug("Deleted {Count} segments for message {MessageId}", message.Segments.Count, message.MessageControlId);
                }

                // Note: We don't delete patients as they might be referenced by other data
                // Users can manually delete patients if needed

                context.HL7Messages.Remove(message);
                deletedCount++;
            }

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogWarning("✅ Cleared {Count} HL7 messages and associated data", deletedCount);

            // Reset daily stats
            lock (_statsLock)
            {
                _filesProcessedToday = 0;
                _filesFailedToday = 0;
                _lastFileProcessedAt = null;
                _lastFileProcessed = null;
            }

            return deletedCount;
        }

        public async Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deleting HL7 message {MessageId}", messageId);

            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var message = await context.HL7Messages
                .Include(m => m.LabResult)
                    .ThenInclude(lr => lr!.Markers)
                .Include(m => m.ParsingIssues)
                .Include(m => m.Segments)
                .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

            if (message == null)
            {
                throw new InvalidOperationException($"HL7 Message {messageId} not found");
            }

            if (message.LabResult != null)
            {
                // Delete associated cases
                var cases = await context.Cases
                    .Where(c => c.LabResults.Any(lr => lr.Id == message.LabResult.Id))
                    .ToListAsync(cancellationToken);

                if (cases.Any())
                {
                    context.Cases.RemoveRange(cases);
                }

                // Delete lab result (cascade will handle markers)
                context.LabResults.Remove(message.LabResult);
            }

            // Delete parsing issues and segments
            if (message.ParsingIssues.Any())
            {
                context.RemoveRange(message.ParsingIssues);
            }

            if (message.Segments.Any())
            {
                context.RemoveRange(message.Segments);
            }

            context.HL7Messages.Remove(message);
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("✅ Deleted HL7 message {MessageId}", messageId);
        }

        #endregion

        public void Dispose()
        {
            foreach (var watcher in _watchers)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
            _processingSemaphore.Dispose();
        }
    }
}
