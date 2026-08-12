using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;

namespace Sentinel.Controllers.Api
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/admin")]
    public class AdminLogsApiController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminLogsApiController> _logger;

        public AdminLogsApiController(IConfiguration configuration, ILogger<AdminLogsApiController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? level = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { error = "Connection string not configured" });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table exists
                var tableExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SentinelLogs'");

                if (tableExists == 0)
                {
                    return Ok(new { logs = new List<object>(), totalCount = 0 });
                }

                // Build WHERE clause
                var whereClauses = new List<string>();
                var parameters = new DynamicParameters();

                if (!string.IsNullOrEmpty(level))
                {
                    whereClauses.Add("Level = @Level");
                    parameters.Add("Level", level);
                }

                if (from.HasValue)
                {
                    whereClauses.Add("TimeStamp >= @From");
                    parameters.Add("From", from.Value);
                }

                if (to.HasValue)
                {
                    whereClauses.Add("TimeStamp <= @To");
                    parameters.Add("To", to.Value);
                }

                var whereClause = whereClauses.Any() ? "WHERE " + string.Join(" AND ", whereClauses) : "";

                // Get total count
                var countSql = $"SELECT COUNT(*) FROM SentinelLogs {whereClause}";
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql, parameters);

                // Get paginated logs
                var offset = (page - 1) * pageSize;
                parameters.Add("Offset", offset);
                parameters.Add("PageSize", pageSize);

                var logsSql = $@"
                    SELECT 
                        Id,
                        Message,
                        MessageTemplate,
                        Level,
                        TimeStamp,
                        Exception,
                        Properties
                    FROM SentinelLogs
                    {whereClause}
                    ORDER BY TimeStamp DESC
                    OFFSET @Offset ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                var logs = await connection.QueryAsync(logsSql, parameters);

                return Ok(new
                {
                    logs = logs,
                    totalCount = totalCount,
                    page = page,
                    pageSize = pageSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving logs");
                return StatusCode(500, new { error = "Error retrieving logs", details = ex.Message });
            }
        }

        [HttpGet("logs/stats")]
        public async Task<IActionResult> GetLogStats()
        {
            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, new { error = "Connection string not configured" });
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table exists
                var tableExists = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'SentinelLogs'");

                if (tableExists == 0)
                {
                    return Ok(new { byLevel = new Dictionary<string, int>(), byDay = new List<object>() });
                }

                // Get stats by level
                var byLevelSql = @"
                    SELECT Level, COUNT(*) as Count
                    FROM SentinelLogs
                    WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
                    GROUP BY Level";

                var byLevel = await connection.QueryAsync<dynamic>(byLevelSql);

                // Get stats by day (last 7 days)
                var byDaySql = @"
                    SELECT 
                        CAST(TimeStamp AS DATE) as Date,
                        COUNT(*) as Count
                    FROM SentinelLogs
                    WHERE TimeStamp >= DATEADD(day, -7, GETUTCDATE())
                    GROUP BY CAST(TimeStamp AS DATE)
                    ORDER BY Date";

                var byDay = await connection.QueryAsync<dynamic>(byDaySql);

                return Ok(new
                {
                    byLevel = byLevel,
                    byDay = byDay
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving log stats");
                return StatusCode(500, new { error = "Error retrieving log stats", details = ex.Message });
            }
        }
    }
}
