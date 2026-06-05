namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Service for monitoring file system locations for incoming HL7 messages
    /// </summary>
    public interface IHL7FileMonitorService
    {
        /// <summary>
        /// Starts monitoring configured file drop locations
        /// </summary>
        Task StartMonitoringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops monitoring all file drop locations
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Processes a single HL7 file manually
        /// </summary>
        Task<FileProcessingResult> ProcessFileAsync(
            string filePath,
            Guid? configurationId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes all files in a directory
        /// </summary>
        Task<BatchProcessingResult> ProcessDirectoryAsync(
            string directoryPath,
            Guid? configurationId = null,
            bool includeSubdirectories = false,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets current monitoring status
        /// </summary>
        MonitoringStatus GetMonitoringStatus();

        /// <summary>
        /// Reprocesses an existing HL7 message by resetting its status and running extraction again
        /// </summary>
        Task<FileProcessingResult> ReprocessMessageAsync(Guid messageId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all test HL7 data (messages, lab results, and cases)
        /// </summary>
        Task<int> ClearTestDataAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a specific HL7 message and all its related data
        /// </summary>
        Task DeleteMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
    }

    public class FileProcessingResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public Guid? HL7MessageId { get; set; }
        public Guid? PatientId { get; set; }
        public Guid? LabResultId { get; set; }
        public List<string> CasesCreated { get; set; } = new();
        public List<string> CasesLinked { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public DateTime ProcessedAt { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
        public string? MovedToPath { get; set; }
    }

    public class BatchProcessingResult
    {
        public int TotalFiles { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public int SkippedCount { get; set; }
        public List<FileProcessingResult> Results { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }

    public class MonitoringStatus
    {
        public bool IsMonitoring { get; set; }
        public int ActiveWatchers { get; set; }
        public List<string> MonitoredPaths { get; set; } = new();
        public DateTime? MonitoringStartedAt { get; set; }
        public int FilesProcessedToday { get; set; }
        public int FilesFailedToday { get; set; }
        public DateTime? LastFileProcessedAt { get; set; }
        public string? LastFileProcessed { get; set; }
    }
}
