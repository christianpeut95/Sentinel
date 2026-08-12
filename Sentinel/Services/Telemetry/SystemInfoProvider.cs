using System.Data;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Telemetry;

namespace Sentinel.Services.Telemetry;

/// <summary>
/// Provides system information for telemetry.
/// Captures only non-sensitive system metadata.
/// </summary>
public class SystemInfoProvider
{
    private const int OperatingSystemMaxLength = 160;
    private const int FrameworkDescriptionMaxLength = 160;
    private const int DeploymentModeMaxLength = 40;
    private const int DatabaseProviderMaxLength = 80;
    private const int DatabaseVersionMaxLength = 80;

    public string GetOperatingSystem()
    {
        return RuntimeInformation.OSDescription;
    }

    public string GetOsArchitecture()
    {
        return RuntimeInformation.OSArchitecture.ToString();
    }

    public string GetDotNetVersion()
    {
        return RuntimeInformation.FrameworkDescription;
    }

    /// <summary>
    /// Identifies the deployment without collecting a machine name, path, or host detail.
    /// </summary>
    public string GetDeploymentMode()
    {
        var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        if (string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runningInContainer, "1", StringComparison.Ordinal)
            || File.Exists("/.dockerenv"))
        {
            return "Docker";
        }

        return "Self-hosted";
    }

    /// <summary>
    /// Builds the optional usage-report runtime block. Failures to determine the database
    /// version do not prevent usage monitoring from submitting the remainder of the report.
    /// </summary>
    public async Task<UsageRuntime> BuildUsageRuntimeAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var runtime = new UsageRuntime
        {
            OperatingSystem = Limit(GetOperatingSystem(), OperatingSystemMaxLength),
            FrameworkDescription = Limit(GetDotNetVersion(), FrameworkDescriptionMaxLength),
            DeploymentMode = Limit(GetDeploymentMode(), DeploymentModeMaxLength),
            DatabaseProvider = Limit(context.Database.ProviderName, DatabaseProviderMaxLength)
        };

        runtime.DatabaseVersion = await GetDatabaseVersionAsync(context, cancellationToken);
        return runtime;
    }

    public string GetProcessorCount()
    {
        return Environment.ProcessorCount.ToString();
    }

    public string GetMachineName()
    {
        // Return anonymized/hashed machine name if needed for privacy
        return Environment.MachineName;
    }

    public Dictionary<string, string> GetSystemInfo()
    {
        return new Dictionary<string, string>
        {
            { "OS", GetOperatingSystem() },
            { "OSArchitecture", GetOsArchitecture() },
            { "DotNetVersion", GetDotNetVersion() },
            { "ProcessorCount", GetProcessorCount() },
            { "MachineName", GetMachineName() },
            { "Is64BitOS", Environment.Is64BitOperatingSystem.ToString() },
            { "Is64BitProcess", Environment.Is64BitProcess.ToString() }
        };
    }

    public long GetWorkingSetMemoryMB()
    {
        return Environment.WorkingSet / (1024 * 1024);
    }

    private static async Task<string?> GetDatabaseVersionAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        try
        {
            if (!wasOpen)
            {
                await connection.OpenAsync(cancellationToken);
            }

            return Limit(connection.ServerVersion, DatabaseVersionMaxLength);
        }
        catch
        {
            // The runtime block is optional, so an unavailable version must not affect
            // submission of the rest of the privacy-safe usage report.
            return null;
        }
        finally
        {
            if (!wasOpen && connection.State != ConnectionState.Closed)
            {
                try
                {
                    await connection.CloseAsync();
                }
                catch
                {
                    // The connection is owned by this short-lived telemetry scope. A close
                    // failure must not block usage-report submission.
                }
            }
        }
    }

    private static string? Limit(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
