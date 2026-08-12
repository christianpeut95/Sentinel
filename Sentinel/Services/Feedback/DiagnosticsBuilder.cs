using Microsoft.Data.SqlClient;
using Dapper;
using Sentinel.Models.Feedback;
using Sentinel.Services.Telemetry;
using System.Diagnostics;

namespace Sentinel.Services.Feedback
{
    /// <summary>
    /// Builds privacy-safe diagnostic payloads for feedback submissions.
    /// Enforces strict whitelist to prevent inclusion of patient data or PHI.
    /// </summary>
    public class DiagnosticsBuilder
    {
        private readonly SystemInfoProvider _systemInfoProvider;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApplicationVersionProvider _applicationVersion;
        private readonly ILogger<DiagnosticsBuilder> _logger;

        public DiagnosticsBuilder(
            SystemInfoProvider systemInfoProvider,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IApplicationVersionProvider applicationVersion,
            ILogger<DiagnosticsBuilder> logger)
        {
            _systemInfoProvider = systemInfoProvider;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _applicationVersion = applicationVersion;
            _logger = logger;
        }

        /// <summary>
        /// Builds a privacy-safe diagnostics payload.
        /// Always sets redactionApplied: true and enforces privacy whitelist.
        /// </summary>
        public async Task<DiagnosticsPayload> BuildDiagnosticsAsync(bool minimalMode = false)
        {
            var diagnostics = new DiagnosticsPayload
            {
                SchemaVersion = 1,
                CapturedAtUtc = DateTime.UtcNow,
                RedactionApplied = true,
                RedactionVersion = "sentinel-redactor-v1",
                RedactedFields = new List<string>
                {
                    "patient-identifiers",
                    "clinical-data",
                    "laboratory-results",
                    "survey-answers",
                    "credentials",
                    "connection-strings",
                    "query-strings",
                    "request-bodies",
                    "response-bodies"
                }
            };

            try
            {
                // Start with ONLY the required metadata - test if this works first
                _logger.LogInformation("Building minimal diagnostics payload for testing");

                // Only populate Application info - nothing else
                diagnostics.Application = new ApplicationInfo
                {
                    Version = _applicationVersion.InformationalVersion,
                    CommitHash = _applicationVersion.CommitHash
                };

                return diagnostics;

                /* COMMENTED OUT FOR TESTING - uncomment sections gradually
                // If minimal mode, only include the absolute basics
                if (minimalMode)
                {
                    diagnostics.Application = new ApplicationInfo
                    {
                        Name = "Sentinel",
                        Version = "1.0.0"
                    };

                    _logger.LogInformation("Built minimal diagnostics payload");
                    return diagnostics;
                }

                // Application Information
                var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                diagnostics.Application = new ApplicationInfo
                {
                    Version = null, // Set by controller
                    CommitHash = null, // Set by controller
                    InstallationId = null // Set by controller
                };

                // Runtime Information
                diagnostics.Runtime = new RuntimeInfo
                {
                    DotNetVersion = _systemInfoProvider.GetDotNetVersion(),
                    OperatingSystem = _systemInfoProvider.GetOperatingSystem(),
                    OsArchitecture = _systemInfoProvider.GetOsArchitecture(),
                    RuntimeIdentifier = System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSetMB = _systemInfoProvider.GetWorkingSetMemoryMB()
                };

                // Database Information
                diagnostics.Database = new DatabaseInfo
                {
                    Provider = "SqlServer",
                    ServerVersion = await GetDatabaseVersionAsync()
                };

                // Authentication Information
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    diagnostics.Authentication = new AuthenticationInfo
                    {
                        IsAuthenticated = httpContext.User?.Identity?.IsAuthenticated ?? false
                    };

                    if (diagnostics.Authentication.IsAuthenticated == true)
                    {
                        diagnostics.Authentication.Roles = httpContext.User.Claims
                            .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();
                    }

                    // Request Information
                    diagnostics.Request = new RequestInfo
                    {
                        Method = httpContext.Request.Method,
                        RouteTemplate = null, // Set by controller with sanitization
                        CorrelationId = httpContext.TraceIdentifier,
                        UserAgent = httpContext.Request.Headers["User-Agent"].ToString()
                    };
                }

                // Performance Information
                var errorCounts = await GetRecentErrorCountsAsync();
                if (errorCounts != null)
                {
                    diagnostics.Performance = new PerformanceInfo
                    {
                        RecentErrors = errorCounts
                    };
                }

                // Feature Flags - convert bool to string
                var featureFlags = await GetFeatureFlagsAsync();
                if (featureFlags != null)
                {
                    diagnostics.FeatureFlags = featureFlags.ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                }

                // Configuration - convert to string dictionary
                diagnostics.Configuration = new Dictionary<string, string?>
                {
                    ["Environment"] = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
                };

                // Client info will be populated by controller from browser data - don't initialize empty
                // diagnostics.Client will be set by controller only if client info is provided
                */
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building diagnostics payload");
                // Return partial diagnostics rather than failing
            }

            return diagnostics;
        }

        /// <summary>
        /// Gets database version (safe - no connection string or data)
        /// </summary>
        private async Task<string?> GetDatabaseVersionAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    return null;

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                var version = await connection.QuerySingleOrDefaultAsync<string>(
                    "SELECT CAST(SERVERPROPERTY('ProductVersion') AS NVARCHAR(128))"
                );
                return version;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve database version for diagnostics");
                return null;
            }
        }

        /// <summary>
        /// Gets counts of recent errors from log table (safe - counts only, no messages)
        /// </summary>
        private async Task<ErrorCounts?> GetRecentErrorCountsAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    return null;

                using var connection = new SqlConnection(connectionString);

                // Check if log table exists
                var tableExists = await connection.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                      WHERE TABLE_NAME = 'SentinelLogs'"
                );

                if (tableExists != 1)
                    return null;

                var now = DateTime.UtcNow;
                var oneHourAgo = now.AddHours(-1);
                var oneDayAgo = now.AddDays(-1);

                var counts = await connection.QuerySingleOrDefaultAsync<(int OneHour, int OneDay)>(
                    @"SELECT 
                        (SELECT COUNT(*) FROM SentinelLogs 
                         WHERE Level IN ('Error', 'Fatal') AND TimeStamp >= @OneHourAgo) AS OneHour,
                        (SELECT COUNT(*) FROM SentinelLogs 
                         WHERE Level IN ('Error', 'Fatal') AND TimeStamp >= @OneDayAgo) AS OneDay",
                    new { OneHourAgo = oneHourAgo, OneDayAgo = oneDayAgo }
                );

                return new ErrorCounts
                {
                    Last1Hour = counts.OneHour,
                    Last24Hours = counts.OneDay
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve error counts for diagnostics");
                return null;
            }
        }

        /// <summary>
        /// Gets feature flags (safe - boolean state only)
        /// </summary>
        private async Task<Dictionary<string, bool>> GetFeatureFlagsAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                    return new Dictionary<string, bool>();

                using var connection = new SqlConnection(connectionString);

                // Check if SystemSettings table exists
                var tableExists = await connection.QuerySingleOrDefaultAsync<int?>(
                    @"SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
                      WHERE TABLE_NAME = 'SystemSettings'"
                );

                if (tableExists != 1)
                    return new Dictionary<string, bool>();

                var settings = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    @"SELECT TOP 1 
                        HL7ProcessingEnabled,
                        SmtpConfigured,
                        TelemetryEnabled,
                        LocalLoggingEnabled
                      FROM SystemSettings"
                );

                if (settings == null)
                    return new Dictionary<string, bool>();

                return new Dictionary<string, bool>
                {
                    { "hl7Enabled", settings.HL7ProcessingEnabled ?? false },
                    { "emailConfigured", settings.SmtpConfigured ?? false },
                    { "telemetryEnabled", settings.TelemetryEnabled ?? false },
                    { "loggingEnabled", settings.LocalLoggingEnabled ?? false }
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve feature flags for diagnostics");
                return new Dictionary<string, bool>();
            }
        }
    }
}
