using Microsoft.Data.SqlClient;
using Dapper;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry
{
    /// <summary>
    /// Builds snapshot reports of current system totals for usage monitoring
    /// </summary>
    public class UsageSnapshotBuilder
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UsageSnapshotBuilder> _logger;

        public UsageSnapshotBuilder(
            IConfiguration configuration,
            ILogger<UsageSnapshotBuilder> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Build a snapshot report of current system totals
        /// </summary>
        public async Task<SnapshotReport> BuildSnapshotAsync()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);

                var snapshot = new SnapshotReport();

                // Query user counts
                var userSnapshot = await connection.QueryFirstOrDefaultAsync<UserSnapshot>(@"
                    SELECT 
                        COUNT(*) as Total,
                        SUM(CASE WHEN LockoutEnd IS NULL OR LockoutEnd < GETUTCDATE() THEN 1 ELSE 0 END) as Enabled
                    FROM AspNetUsers");
                snapshot.Users = userSnapshot ?? new UserSnapshot();

                // Query disease configuration counts
                var diseaseSnapshot = await connection.QueryFirstOrDefaultAsync<DiseaseSnapshot>(@"
                    SELECT 
                        COUNT(*) as Configured
                    FROM Diseases
                    WHERE IsActive = 1");
                snapshot.Diseases = diseaseSnapshot ?? new DiseaseSnapshot();

                // Query survey configuration counts
                var surveySnapshot = await connection.QueryFirstOrDefaultAsync<SurveySnapshot>(@"
                    SELECT 
                        COUNT(DISTINCT Id) as Definitions,
                        (SELECT COUNT(*) FROM SurveySubmissionLogs) as Responses
                    FROM SurveyTemplates");
                snapshot.Surveys = surveySnapshot ?? new SurveySnapshot();

                // Query system totals
                var totalsSnapshot = await connection.QueryFirstOrDefaultAsync<TotalsSnapshot>(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Cases) as Cases,
                        (SELECT COUNT(*) FROM Patients) as Patients,
                        (SELECT COUNT(*) FROM Outbreaks) as Outbreaks,
                        (SELECT COUNT(*) FROM LabResults) as LabResults,
                        (SELECT COUNT(*) FROM ExposureEvents) as Exposures,
                        (SELECT COUNT(*) FROM ReportDefinitions) as Reports,
                        (SELECT COUNT(*) FROM CustomFieldDefinitions) as CustomFields");
                snapshot.Totals = totalsSnapshot ?? new TotalsSnapshot();

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build usage snapshot");

                // Return empty snapshot on error to avoid blocking usage report submission
                return new SnapshotReport
                {
                    Users = new UserSnapshot(),
                    Diseases = new DiseaseSnapshot(),
                    Surveys = new SurveySnapshot(),
                    Totals = new TotalsSnapshot()
                };
            }
        }
    }
}
