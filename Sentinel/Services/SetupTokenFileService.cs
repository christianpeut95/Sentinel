using System.Text;

namespace Sentinel.Services;

/// <summary>
/// Holds the one-time initial setup token outside the web root. The plaintext
/// token exists only until setup completes; the database stores a hash.
/// </summary>
public interface ISetupTokenFileService
{
    string TokenFilePath { get; }
    bool TokenFileExists { get; }
    Task WriteTokenAsync(string token, CancellationToken cancellationToken = default);
    Task DeleteTokenAsync(CancellationToken cancellationToken = default);
}

public sealed class SetupTokenFileService : ISetupTokenFileService
{
    private readonly ILogger<SetupTokenFileService> _logger;

    public SetupTokenFileService(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger<SetupTokenFileService> logger)
    {
        _logger = logger;

        var configuredPath = configuration["Setup:TokenFilePath"];
        configuredPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine("App_Data", "SentinelSetup", "setup-token.txt")
            : configuredPath.Trim();

        TokenFilePath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));

        var webRoot = Path.GetFullPath(environment.WebRootPath);
        if (TokenFilePath.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
            TokenFilePath.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Setup:TokenFilePath must be outside wwwroot.");
        }
    }

    public string TokenFilePath { get; }

    public bool TokenFileExists => File.Exists(TokenFilePath);

    public async Task WriteTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("A setup token is required.", nameof(token));
        }

        var directory = Path.GetDirectoryName(TokenFilePath)
            ?? throw new InvalidOperationException("The setup token file path has no parent directory.");
        Directory.CreateDirectory(directory);
        RestrictUnixDirectoryPermissions(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(TokenFilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporaryPath, token, new UTF8Encoding(false), cancellationToken);
            RestrictUnixFilePermissions(temporaryPath);

            File.Move(temporaryPath, TokenFilePath, overwrite: true);
            RestrictUnixFilePermissions(TokenFilePath);

            _logger.LogInformation("Created protected initial-setup token file at {TokenFilePath}", TokenFilePath);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public Task DeleteTokenAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(TokenFilePath))
        {
            File.Delete(TokenFilePath);
            _logger.LogInformation("Deleted the consumed initial-setup token file at {TokenFilePath}", TokenFilePath);
        }

        return Task.CompletedTask;
    }

    private static void RestrictUnixFilePermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    private static void RestrictUnixDirectoryPermissions(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute);
        }
    }
}
