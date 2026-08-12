using System.Text.Json.Serialization;

namespace Sentinel.Models.Telemetry
{
    /// <summary>
    /// Main usage report DTO matching the Sentinel Feedback API schema v1
    /// </summary>
    public class UsageReport
    {
        [JsonPropertyName("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [JsonPropertyName("reportId")]
        public string ReportId { get; set; } = string.Empty;

        [JsonPropertyName("installationId")]
        public string InstallationId { get; set; } = string.Empty;

        [JsonPropertyName("sentinelVersion")]
        public string? SentinelVersion { get; set; }

        [JsonPropertyName("generatedAtUtc")]
        public DateTime GeneratedAtUtc { get; set; }

        [JsonPropertyName("period")]
        public ReportPeriod Period { get; set; } = new();

        [JsonPropertyName("activity")]
        public ActivityReport Activity { get; set; } = new();

        [JsonPropertyName("snapshot")]
        public SnapshotReport Snapshot { get; set; } = new();

        [JsonPropertyName("runtime")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public UsageRuntime? Runtime { get; set; }
    }

    /// <summary>
    /// Non-identifying runtime metadata supplied with a usage report when available.
    /// Each value is limited before transmission to comply with the feedback API contract.
    /// </summary>
    public class UsageRuntime
    {
        [JsonPropertyName("operatingSystem")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? OperatingSystem { get; set; }

        [JsonPropertyName("frameworkDescription")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? FrameworkDescription { get; set; }

        [JsonPropertyName("deploymentMode")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DeploymentMode { get; set; }

        [JsonPropertyName("databaseProvider")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DatabaseProvider { get; set; }

        [JsonPropertyName("databaseVersion")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? DatabaseVersion { get; set; }
    }

    public class ReportPeriod
    {
        [JsonPropertyName("startUtc")]
        public DateTime StartUtc { get; set; }

        [JsonPropertyName("endUtc")]
        public DateTime EndUtc { get; set; }
    }

    public class ActivityReport
    {
        [JsonPropertyName("pageViews")]
        public List<PageViewCount> PageViews { get; set; } = new();

        [JsonPropertyName("logins")]
        public LoginActivity Logins { get; set; } = new();

        [JsonPropertyName("casesCreated")]
        public int CasesCreated { get; set; }

        [JsonPropertyName("patientsCreated")]
        public int PatientsCreated { get; set; }

        [JsonPropertyName("outbreaksCreated")]
        public int OutbreaksCreated { get; set; }

        [JsonPropertyName("labResultsCreated")]
        public int LabResultsCreated { get; set; }

        [JsonPropertyName("exposuresCreated")]
        public int ExposuresCreated { get; set; }

        [JsonPropertyName("surveys")]
        public SurveyActivity Surveys { get; set; } = new();

        [JsonPropertyName("hl7")]
        public HL7Activity Hl7 { get; set; } = new();

        [JsonPropertyName("reports")]
        public ReportActivity Reports { get; set; } = new();

        [JsonPropertyName("customFields")]
        public CustomFieldActivity CustomFields { get; set; } = new();
    }

    public class PageViewCount
    {
        [JsonPropertyName("page")]
        public string Page { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class LoginActivity
    {
        [JsonPropertyName("successful")]
        public int Successful { get; set; }

        [JsonPropertyName("failed")]
        public int Failed { get; set; }

        [JsonPropertyName("uniqueActiveUsers")]
        public int UniqueActiveUsers { get; set; }
    }

    public class SurveyActivity
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }

        [JsonPropertyName("completed")]
        public int Completed { get; set; }
    }

    public class HL7Activity
    {
        [JsonPropertyName("messagesProcessed")]
        public int MessagesProcessed { get; set; }

        [JsonPropertyName("messagesSucceeded")]
        public int MessagesSucceeded { get; set; }

        [JsonPropertyName("messagesFailed")]
        public int MessagesFailed { get; set; }
    }

    public class ReportActivity
    {
        [JsonPropertyName("generated")]
        public int Generated { get; set; }
    }

    public class CustomFieldActivity
    {
        [JsonPropertyName("created")]
        public int Created { get; set; }
    }

    public class SnapshotReport
    {
        [JsonPropertyName("users")]
        public UserSnapshot Users { get; set; } = new();

        [JsonPropertyName("diseases")]
        public DiseaseSnapshot Diseases { get; set; } = new();

        [JsonPropertyName("surveys")]
        public SurveySnapshot Surveys { get; set; } = new();

        [JsonPropertyName("totals")]
        public TotalsSnapshot Totals { get; set; } = new();
    }

    public class UserSnapshot
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("enabled")]
        public int Enabled { get; set; }
    }

    public class DiseaseSnapshot
    {
        [JsonPropertyName("configured")]
        public int Configured { get; set; }
    }

    public class SurveySnapshot
    {
        [JsonPropertyName("definitions")]
        public int Definitions { get; set; }

        [JsonPropertyName("responses")]
        public int Responses { get; set; }
    }

    public class TotalsSnapshot
    {
        [JsonPropertyName("cases")]
        public int Cases { get; set; }

        [JsonPropertyName("patients")]
        public int Patients { get; set; }

        [JsonPropertyName("outbreaks")]
        public int Outbreaks { get; set; }

        [JsonPropertyName("labResults")]
        public int LabResults { get; set; }

        [JsonPropertyName("exposures")]
        public int Exposures { get; set; }

        [JsonPropertyName("reports")]
        public int Reports { get; set; }

        [JsonPropertyName("customFields")]
        public int CustomFields { get; set; }
    }
}
