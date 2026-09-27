using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.HL7;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Polls configured file-drop locations for incoming HL7 messages and processes them automatically.
    /// </summary>
    public class HL7FileMonitorService : IHL7FileMonitorService, IDisposable
    {
        private readonly ILogger<HL7FileMonitorService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".hl7", ".txt"
        };
        private const int MaxFilesPerScan = 100;

        private readonly SemaphoreSlim _processingSemaphore = new(5);
        private readonly HashSet<string> _processingFiles;
        private readonly object _processingFilesLock = new();
        private readonly object _monitoringLock = new();
        private readonly TimeSpan _fileSettleDelay;
        private readonly long _maxInboundFileBytes;
        private readonly string? _allowedRootPath;
        private readonly bool _restrictToAllowedRoot;
        private Dictionary<Guid, MonitoredDropLocation> _monitoredLocations = new();
        private bool _isMonitoring;
        private DateTime? _monitoringStartedAt;
        private int _filesProcessedToday = 0;
        private int _filesFailedToday = 0;
        private DateTime? _lastFileProcessedAt;
        private string? _lastFileProcessed;
        private readonly object _statsLock = new();

        public HL7FileMonitorService(
            ILogger<HL7FileMonitorService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IOptions<HL7FileMonitorOptions> options)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;

            var configuredOptions = options.Value;
            _fileSettleDelay = TimeSpan.FromSeconds(Math.Clamp(configuredOptions.FileSettleDelaySeconds, 0, 60));
            _maxInboundFileBytes = Math.Clamp(configuredOptions.MaxInboundFileBytes, 1024, 100 * 1024 * 1024);
            _processingFiles = new HashSet<string>(OperatingSystem.IsWindows()
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal);

            if (!string.IsNullOrWhiteSpace(configuredOptions.AllowedRootPath))
            {
                _restrictToAllowedRoot = true;
                try
                {
                    _allowedRootPath = Path.GetFullPath(configuredOptions.AllowedRootPath);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    _logger.LogError(ex, "The configured HL7 file-drop root is invalid; no configured paths will be accepted");
                    _allowedRootPath = string.Empty;
                }
            }
        }

        public async Task StartMonitoringAsync(CancellationToken cancellationToken = default)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("File monitoring is already active");
                return;
            }

            _logger.LogInformation("Starting HL7 file-drop polling service");

            var locations = new Dictionary<Guid, MonitoredDropLocation>();

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
                        if (!TryNormalizeFileDropPath(config.FileDropPath, out var path, out var validationError))
                        {
                            _logger.LogError(
                                "HL7 configuration {ConfigurationName} was not activated because its file-drop path is invalid: {ValidationError}",
                                config.ConfigurationName,
                                validationError);
                            continue;
                        }

                        if (!IsSupportedFilePattern(config.FilePattern))
                        {
                            _logger.LogError(
                                "HL7 configuration {ConfigurationName} was not activated because its file pattern is not supported: {FilePattern}",
                                config.ConfigurationName,
                                config.FilePattern);
                            continue;
                        }

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            _logger.LogInformation("Created file drop directory: {Path}", path);
                        }

                        CreateOperationalSubdirectories(path);
                        locations[config.Id] = new MonitoredDropLocation(
                            config.Id,
                            path,
                            config.ConfigurationName,
                            config.FilePattern);
                        _logger.LogInformation("Configured HL7 polling location: {Path} (Configuration: {ConfigName})",
                            path, config.ConfigurationName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to start monitoring path: {Path} for configuration: {ConfigName}", 
                            config.FileDropPath, config.ConfigurationName);
                    }
                }
            }

            lock (_monitoringLock)
            {
                _monitoredLocations = locations;
                _isMonitoring = locations.Count > 0;
                _monitoringStartedAt = _isMonitoring ? DateTime.UtcNow : null;
            }

            // Reset daily stats if it's a new day
            ResetDailyStatsIfNeeded();

            _logger.LogInformation("HL7 file-drop polling started with {Count} active locations", locations.Count);
        }

        public Task StopMonitoringAsync()
        {
            _logger.LogInformation("Stopping HL7 file-drop polling service");

            lock (_monitoringLock)
            {
                _monitoredLocations = new Dictionary<Guid, MonitoredDropLocation>();
                _isMonitoring = false;
                _monitoringStartedAt = null;
            }

            _logger.LogInformation("HL7 file-drop polling stopped");
            return Task.CompletedTask;
        }

        public async Task ReloadConfigurationsAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Reloading HL7 configurations and restarting monitoring");

            // Stop current monitoring
            await StopMonitoringAsync();

            // Start monitoring again (will reload configurations from database)
            await StartMonitoringAsync(cancellationToken);

            _logger.LogInformation("HL7 configurations reloaded and monitoring restarted");
        }

        public bool TryNormalizeFileDropPath(string? fileDropPath, out string normalizedPath, out string validationError)
        {
            normalizedPath = string.Empty;
            validationError = string.Empty;

            if (string.IsNullOrWhiteSpace(fileDropPath))
            {
                validationError = "A file-drop path is required.";
                return false;
            }

            try
            {
                if (!Path.IsPathFullyQualified(fileDropPath))
                {
                    validationError = "The file-drop path must be an absolute path.";
                    return false;
                }

                normalizedPath = Path.GetFullPath(fileDropPath);
                if (_restrictToAllowedRoot &&
                    (string.IsNullOrWhiteSpace(_allowedRootPath) || !IsPathWithinRoot(normalizedPath, _allowedRootPath)))
                {
                    validationError = "The file-drop path is outside the configured HL7 file-drop root.";
                    normalizedPath = string.Empty;
                    return false;
                }

                if (ContainsReparsePoint(normalizedPath))
                {
                    validationError = "The file-drop path must not include a symbolic link or reparse point.";
                    normalizedPath = string.Empty;
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or IOException or UnauthorizedAccessException)
            {
                validationError = "The file-drop path is not valid.";
                return false;
            }
        }

        public async Task ScanConfiguredDirectoriesAsync(CancellationToken cancellationToken = default)
        {
            MonitoredDropLocation[] locations;
            lock (_monitoringLock)
            {
                if (!_isMonitoring)
                {
                    return;
                }

                locations = _monitoredLocations.Values.ToArray();
            }

            foreach (var location in locations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ScanDirectoryAsync(location, cancellationToken);
            }
        }

        private async Task ScanDirectoryAsync(MonitoredDropLocation location, CancellationToken cancellationToken)
        {
            try
            {
                if (!Directory.Exists(location.Path))
                {
                    _logger.LogWarning("Configured HL7 file-drop directory is unavailable: {Path}", location.Path);
                    return;
                }

                var files = Directory.EnumerateFiles(location.Path, "*", SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedInboundFile)
                    .Where(filePath => MatchesConfiguredFilePattern(filePath, location.FilePattern))
                    .Where(filePath => IsReadyForProcessing(filePath, out _))
                    .Take(MaxFilesPerScan)
                    .ToArray();

                if (files.Length == 0)
                {
                    return;
                }

                _logger.LogInformation(
                    "HL7 polling found {FileCount} ready file(s) in configuration {ConfigurationName}",
                    files.Length,
                    location.ConfigurationName);

                await Task.WhenAll(files.Select(filePath => ProcessPolledFileAsync(filePath, location, cancellationToken)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Access was denied while polling HL7 file-drop directory: {Path}", location.Path);
            }
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "Unable to poll HL7 file-drop directory: {Path}", location.Path);
            }
        }

        private async Task ProcessPolledFileAsync(
            string filePath,
            MonitoredDropLocation location,
            CancellationToken cancellationToken)
        {
            if (!TryClaimFile(filePath))
            {
                return;
            }

            try
            {
                if (!IsReadyForProcessing(filePath, out var reason))
                {
                    _logger.LogDebug("Skipping HL7 file during polling: {FilePath}. Reason: {Reason}", filePath, reason);
                    return;
                }

                await _processingSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // Recheck after waiting for capacity: another process may have moved or changed the file.
                    if (!IsReadyForProcessing(filePath, out reason))
                    {
                        _logger.LogDebug("Skipping HL7 file before processing: {FilePath}. Reason: {Reason}", filePath, reason);
                        return;
                    }

                    var result = await ProcessFileAsync(filePath, location.ConfigurationId, cancellationToken);
                    if (result.Success)
                    {
                        _logger.LogInformation("Processed polled HL7 file {FileName}", result.FileName);
                    }
                    else
                    {
                        _logger.LogWarning("HL7 file processing did not succeed for {FileName}. File was moved to {MovedTo}",
                            result.FileName,
                            result.MovedToPath ?? "its source directory");
                    }
                }
                finally
                {
                    _processingSemaphore.Release();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while polling HL7 file {FilePath}", filePath);
            }
            finally
            {
                ReleaseFileClaim(filePath);
            }
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

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length > _maxInboundFileBytes)
                {
                    _logger.LogWarning("HL7 file exceeds the configured maximum size and will not be read: {FilePath}", filePath);
                    result.Errors.Add("File exceeds the configured maximum size.");
                    await MoveFileToErrorAsync(filePath, result, "File exceeds the configured maximum size.");
                    return result;
                }

                // Read file content
                string hl7Content;
                try
                {
                    _logger.LogDebug("Reading file content: {FilePath}", filePath);
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
                            .Include(c => c.FieldMappings)
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
                _logger.LogError(ex, "Unhandled exception processing HL7 file: {FilePath}", filePath);
                result.Errors.Add("Processing failed. Check the application logs using the message control ID or file name.");
                result.Success = false;

                try
                {
                    // Check if file still exists before attempting move
                    if (File.Exists(filePath))
                    {
                        _logger.LogInformation("File still exists after exception, moving to error folder: {FilePath}", filePath);
                        await MoveFileToErrorAsync(filePath, result, "Unexpected processing failure. See application logs for details.");
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
                    if (!TryClaimFile(filePath))
                    {
                        return (FileProcessingResult?)null;
                    }

                    var semaphoreAcquired = false;
                    try
                    {
                        await _processingSemaphore.WaitAsync(cancellationToken);
                        semaphoreAcquired = true;
                        return await ProcessFileAsync(filePath, configurationId, cancellationToken);
                    }
                    finally
                    {
                        if (semaphoreAcquired)
                        {
                            _processingSemaphore.Release();
                        }
                        ReleaseFileClaim(filePath);
                    }
                });

                var fileResults = await Task.WhenAll(tasks);

                var completedResults = fileResults.Where(r => r is not null).Select(r => r!).ToList();
                result.Results.AddRange(completedResults);
                result.SuccessCount = completedResults.Count(r => r.Success);
                result.FailureCount = completedResults.Count(r => !r.Success && r.Errors.Any());
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
            MonitoredDropLocation[] locations;
            bool isMonitoring;
            DateTime? startedAt;
            lock (_monitoringLock)
            {
                locations = _monitoredLocations.Values.ToArray();
                isMonitoring = _isMonitoring;
                startedAt = _monitoringStartedAt;
            }

            lock (_statsLock)
            {
                ResetDailyStatsIfNeeded();

                return new MonitoringStatus
                {
                    IsMonitoring = isMonitoring,
                    ActivePollingLocations = locations.Length,
                    MonitoredPaths = locations.Select(location => location.Path).ToList(),
                    MonitoringStartedAt = startedAt,
                    FilesProcessedToday = _filesProcessedToday,
                    FilesFailedToday = _filesFailedToday,
                    LastFileProcessedAt = _lastFileProcessedAt,
                    LastFileProcessed = _lastFileProcessed
                };
            }
        }

        #region Private Helper Methods

        private bool IsReadyForProcessing(string filePath, out string reason)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                {
                    reason = "The file no longer exists.";
                    return false;
                }

                if (!IsSupportedInboundFile(filePath))
                {
                    reason = "Only .hl7 and .txt files are accepted.";
                    return false;
                }

                if ((fileInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    reason = "Symbolic links and reparse points are not accepted.";
                    return false;
                }

                if (fileInfo.Length == 0)
                {
                    reason = "The file is empty.";
                    return false;
                }

                if (fileInfo.Length > _maxInboundFileBytes)
                {
                    reason = "The file exceeds the configured maximum size.";
                    return false;
                }

                if (DateTime.UtcNow - fileInfo.LastWriteTimeUtc < _fileSettleDelay)
                {
                    reason = "The file is still settling.";
                    return false;
                }

                reason = string.Empty;
                return true;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or ArgumentException or NotSupportedException)
            {
                reason = "The file could not be inspected safely.";
                _logger.LogWarning(ex, "Unable to inspect candidate HL7 file: {FilePath}", filePath);
                return false;
            }
        }

        private static bool IsSupportedInboundFile(string filePath)
        {
            return SupportedExtensions.Contains(Path.GetExtension(filePath));
        }

        private static bool MatchesConfiguredFilePattern(string filePath, string? filePattern)
        {
            return filePattern switch
            {
                "*.hl7" => Path.GetExtension(filePath).Equals(".hl7", StringComparison.OrdinalIgnoreCase),
                "*.txt" => Path.GetExtension(filePath).Equals(".txt", StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        private static bool IsSupportedFilePattern(string? filePattern)
        {
            return filePattern is "*.hl7" or "*.txt";
        }

        private bool TryClaimFile(string filePath)
        {
            lock (_processingFilesLock)
            {
                return _processingFiles.Add(filePath);
            }
        }

        private void ReleaseFileClaim(string filePath)
        {
            lock (_processingFilesLock)
            {
                _processingFiles.Remove(filePath);
            }
        }

        private static void CreateOperationalSubdirectories(string path)
        {
            Directory.CreateDirectory(Path.Combine(path, "Processed"));
            Directory.CreateDirectory(Path.Combine(path, "Error"));
            Directory.CreateDirectory(Path.Combine(path, "Review"));
        }

        private static bool IsPathWithinRoot(string path, string rootPath)
        {
            var relativePath = Path.GetRelativePath(rootPath, path);
            return relativePath == "." ||
                   (!relativePath.Equals("..", StringComparison.Ordinal) &&
                    !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) &&
                    !Path.IsPathRooted(relativePath));
        }

        private static bool ContainsReparsePoint(string path)
        {
            var root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                return true;
            }

            var currentPath = root;
            var relativePath = path[root.Length..];
            foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            {
                if (string.IsNullOrWhiteSpace(segment))
                {
                    continue;
                }

                currentPath = Path.Combine(currentPath, segment);
                if ((Directory.Exists(currentPath) || File.Exists(currentPath)) &&
                    (File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed record MonitoredDropLocation(
            Guid ConfigurationId,
            string Path,
            string ConfigurationName,
            string FilePattern);

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
                result.Warnings.Add("Could not move the file to the processed folder. Check the application logs for details.");
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
                result.Warnings.Add("Could not move the file to the error folder. Check the application logs for details.");
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
                result.Warnings.Add("Could not move the file to the review folder. Check the application logs for details.");
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
            _processingSemaphore.Dispose();
        }
    }
}
