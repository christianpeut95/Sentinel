using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    /// <summary>
    /// System-wide settings and configuration state
    /// Single-row table for application setup and runtime settings
    /// </summary>
    public class SystemSettings
    {
        public Guid Id { get; set; }

        // ── Setup State ────────────────────────────────────────────
        [Display(Name = "Setup Completed")]
        public bool IsSetupCompleted { get; set; } = false;

        [Display(Name = "Setup Completed At")]
        public DateTime? SetupCompletedAt { get; set; }

        [Display(Name = "Setup Completed By User")]
        [StringLength(450)]
        public string? SetupCompletedByUserId { get; set; }

        // ── Token Security ─────────────────────────────────────────
        [Display(Name = "Setup Token (Hashed)")]
        [StringLength(128)]
        public string? SetupToken { get; set; }

        [Display(Name = "Setup Token Generated At")]
        public DateTime? SetupTokenGeneratedAt { get; set; }

        [Display(Name = "Setup Token Expires At")]
        public DateTime? SetupTokenExpiresAt { get; set; }

        // ── Registration Control ───────────────────────────────────
        // ── Application Identity ───────────────────────────────────
        [Display(Name = "Application Name")]
        [StringLength(200)]
        public string? ApplicationName { get; set; }

        [Display(Name = "Application URL")]
        [StringLength(500)]
        public string? ApplicationUrl { get; set; }

        [Display(Name = "Enforce HTTPS")]
        public bool EnforceHttps { get; set; } = true;

        [Display(Name = "Domain Name")]
        [StringLength(200)]
        public string? DomainName { get; set; }

        [Display(Name = "SSL Certificate Path")]
        [StringLength(1000)]
        public string? SslCertificatePath { get; set; }

        // ── SMTP / Email Settings ──────────────────────────────────
        [Display(Name = "SMTP Host")]
        [StringLength(200)]
        public string? SmtpHost { get; set; }

        [Display(Name = "SMTP Port")]
        public int? SmtpPort { get; set; }

        [Display(Name = "SMTP Username")]
        [StringLength(200)]
        public string? SmtpUsername { get; set; }

        [Display(Name = "SMTP Password (Encrypted)")]
        [StringLength(500)]
        public string? SmtpPasswordEncrypted { get; set; }

        [Display(Name = "SMTP From Email")]
        [StringLength(200)]
        public string? SmtpFromEmail { get; set; }

        [Display(Name = "SMTP From Display Name")]
        [StringLength(200)]
        public string? SmtpFromDisplayName { get; set; }

        [Display(Name = "SMTP Enable SSL")]
        public bool SmtpEnableSsl { get; set; } = true;

        [Display(Name = "SMTP Configured")]
        public bool SmtpConfigured { get; set; } = false;

        // ── HL7 Default Settings ───────────────────────────────────
        [Display(Name = "HL7 Processing Enabled")]
        public bool HL7ProcessingEnabled { get; set; } = false;

        [Display(Name = "HL7 Default Drop Path")]
        [StringLength(1000)]
        public string? HL7DefaultDropPath { get; set; }

        [Display(Name = "HL7 Default Archive Path")]
        [StringLength(1000)]
        public string? HL7DefaultArchivePath { get; set; }

        // ── Surveillance Startup Checklist ────────────────────────
        [Display(Name = "Surveillance Startup Completed")]
        public bool SurveillanceStartupCompleted { get; set; } = false;

        [Display(Name = "Surveillance Startup Checklist (JSON)")]
        public string? SurveillanceStartupChecklistJson { get; set; }

        [Display(Name = "Surveillance Startup Progress Percentage")]
        public int SurveillanceStartupProgressPercentage { get; set; } = 0;

        // ── User Feedback ────────────────────────────────────────────────────────
        /// <summary>
        /// Unique identifier for this Sentinel installation, used to link feedback submissions
        /// </summary>
        [Display(Name = "Installation ID")]
        [StringLength(36)]
        public string? InstallationId { get; set; }

        /// <summary>
        /// Enable or disable the feedback widget (opt-out approach, enabled by default)
        /// </summary>
        [Display(Name = "Enable Feedback Widget")]
        public bool EnableFeedbackWidget { get; set; } = true;

        /// <summary>
        /// Enable or disable anonymous usage statistics collection and hourly reporting
        /// </summary>
        [Display(Name = "Enable Anonymous Usage Statistics")]
        public bool EnableUsageMonitoring { get; set; } = true;

        // -- Access Control ----------------------------------------------------------
        /// <summary>
        /// When enabled, users only see patients that have at least one case they can access.
        /// Case visibility is determined by the shared disease-access query filter.
        /// </summary>
        [Display(Name = "Case-Scoped Patient Access")]
        public bool CaseScopedPatientAccess { get; set; } = false;

        // -- Telemetry & Logging ----------------------------------------------------
        [Display(Name = "Enable Telemetry")]
        public bool TelemetryEnabled { get; set; } = true;

        [Display(Name = "Enable Local Logging")]
        public bool LocalLoggingEnabled { get; set; } = true;

        [Display(Name = "Log Retention Days")]
        public int? LogRetentionDays { get; set; } = 30;

        [Display(Name = "Minimum Log Level")]
        [StringLength(50)]
        public string? MinimumLogLevel { get; set; } = "Information";

        [Display(Name = "Include User Information")]
        public bool IncludeUserInformation { get; set; } = true;

        [Display(Name = "Include System Information")]
        public bool IncludeSystemInformation { get; set; } = true;

        // ── Audit Fields ───────────────────────────────────────────
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }

        [Display(Name = "Modified By User")]
        [StringLength(450)]
        public string? ModifiedByUserId { get; set; }

        // ── Navigation ─────────────────────────────────────────────
        public ApplicationUser? SetupCompletedByUser { get; set; }
        public ApplicationUser? ModifiedByUser { get; set; }
    }
}
