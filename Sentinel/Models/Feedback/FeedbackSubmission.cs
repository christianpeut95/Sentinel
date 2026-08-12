using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Sentinel.Models.Feedback
{
    /// <summary>
    /// Feedback submission payload for Sentinel Feedback API
    /// POST https://feedback.sentinelsurveillance.app/api/v1/feedback
    /// Maximum payload size: 512 KiB
    /// </summary>
    public class FeedbackSubmission
    {
        // -- Required Fields --

        /// <summary>
        /// Feedback type: Bug (1), FeatureRequest (2), Confusing (3), or General (4)
        /// </summary>
        [Required]
        [JsonPropertyName("type")]
        public int Type { get; set; }

        /// <summary>
        /// Short summary of the feedback (3-200 characters)
        /// </summary>
        [Required]
        [StringLength(200, MinimumLength = 3)]
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = null!;

        /// <summary>
        /// Detailed description of the feedback (3-5000 characters)
        /// </summary>
        [Required]
        [StringLength(5000, MinimumLength = 3)]
        [JsonPropertyName("description")]
        public string Description { get; set; } = null!;

        /// <summary>
        /// Indicates whether technical diagnostic information is included
        /// </summary>
        [JsonPropertyName("technicalInfoIncluded")]
        public bool TechnicalInfoIncluded { get; set; } = false;

        // -- Optional Fields --

        /// <summary>
        /// Expected behaviour (optional, max 5000 characters)
        /// </summary>
        [StringLength(5000)]
        [JsonPropertyName("expectedBehaviour")]
        public string? ExpectedBehaviour { get; set; }

        /// <summary>
        /// Reporter's email address for follow-up (optional, max 320 characters)
        /// </summary>
        [EmailAddress]
        [StringLength(320)]
        [JsonPropertyName("reporterEmail")]
        public string? ReporterEmail { get; set; }

        /// <summary>
        /// How often the issue occurs (optional, max 40 characters)
        /// Examples: "Every time", "Sometimes", "Rarely"
        /// </summary>
        [StringLength(40)]
        [JsonPropertyName("reproducibility")]
        public string? Reproducibility { get; set; }

        /// <summary>
        /// Unique installation identifier (max 100 characters)
        /// </summary>
        [StringLength(100)]
        [JsonPropertyName("installationId")]
        public string? InstallationId { get; set; }

        /// <summary>
        /// Sentinel application version (max 50 characters)
        /// </summary>
        [StringLength(50)]
        [JsonPropertyName("sentinelVersion")]
        public string? SentinelVersion { get; set; }

        /// <summary>
        /// Git commit hash (max 64 characters)
        /// </summary>
        [StringLength(64)]
        [JsonPropertyName("commitHash")]
        public string? CommitHash { get; set; }

        /// <summary>
        /// Page route template (max 500 characters)
        /// Example: /Cases/{id}/Edit (not /Cases/12345/Edit)
        /// </summary>
        [StringLength(500)]
        [JsonPropertyName("pageRoute")]
        public string? PageRoute { get; set; }

        /// <summary>
        /// Correlation ID for tracing (max 200 characters)
        /// </summary>
        [StringLength(200)]
        [JsonPropertyName("correlationId")]
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Client user agent string (max 500 characters)
        /// </summary>
        [StringLength(500)]
        [JsonPropertyName("clientUserAgent")]
        public string? ClientUserAgent { get; set; }

        /// <summary>
        /// Detailed technical diagnostic data (max 256 KiB when serialized)
        /// Must include redactionApplied: true if provided
        /// </summary>
        [JsonPropertyName("diagnostics")]
        public DiagnosticsPayload? Diagnostics { get; set; }
    }
}
