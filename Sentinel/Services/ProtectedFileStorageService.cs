using Microsoft.AspNetCore.Http;

namespace Sentinel.Services;

/// <summary>
/// Stores sensitive files outside the web root. Files returned by this service
/// must only be served through an authorised application endpoint.
/// </summary>
public interface IProtectedFileStorageService
{
    Task<ProtectedStoredFile> SaveAttachmentAsync(IFormFile file, string category, CancellationToken cancellationToken = default);
    Stream? OpenRead(string storageKey);
    Task<string?> MigrateLegacyAttachmentAsync(string? legacyPath, string category, CancellationToken cancellationToken = default);
}

public sealed record ProtectedStoredFile(string StorageKey, string OriginalFileName, long Length);

public sealed class ProtectedFileStorageService : IProtectedFileStorageService
{
    public const string NotesCategory = "notes";
    public const string LabResultsCategory = "lab-results";

    private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        NotesCategory,
        LabResultsCategory
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ProtectedFileStorageService> _logger;
    private readonly string _storageRoot;
    private readonly long _maxUploadBytes;

    public ProtectedFileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<ProtectedFileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;

        var configuredRoot = configuration["FileStorage:RootPath"];
        configuredRoot = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine("App_Data", "SentinelFiles")
            : configuredRoot;

        _storageRoot = Path.GetFullPath(Path.IsPathRooted(configuredRoot)
            ? configuredRoot
            : Path.Combine(_environment.ContentRootPath, configuredRoot));

        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        if (_storageRoot.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
            _storageRoot.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FileStorage:RootPath must be outside wwwroot.");
        }

        _maxUploadBytes = configuration.GetValue<long?>("FileStorage:MaxUploadBytes") ?? 26_214_400;
        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<ProtectedStoredFile> SaveAttachmentAsync(
        IFormFile file,
        string category,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        category = ValidateCategory(category);

        if (file.Length <= 0)
            throw new InvalidOperationException("The attachment is empty.");

        if (file.Length > _maxUploadBytes)
            throw new InvalidOperationException($"The attachment exceeds the {_maxUploadBytes:N0}-byte limit.");

        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = $"{category}/{storedName}";
        var destination = GetPhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(output, cancellationToken);

        return new ProtectedStoredFile(storageKey, Path.GetFileName(file.FileName), file.Length);
    }

    public Stream? OpenRead(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.StartsWith('/'))
            return null;

        try
        {
            var path = GetPhysicalPath(storageKey);
            return File.Exists(path)
                ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                : null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Moves one legacy wwwroot upload into protected storage. Only known
    /// legacy upload routes are accepted; arbitrary database paths are never used.
    /// </summary>
    public Task<string?> MigrateLegacyAttachmentAsync(
        string? legacyPath,
        string category,
        CancellationToken cancellationToken = default)
    {
        category = ValidateCategory(category);

        if (!TryGetLegacySourcePath(legacyPath, category, out var sourcePath))
            return Task.FromResult<string?>(null);

        var extension = Path.GetExtension(sourcePath);
        var storageKey = $"{category}/{Guid.NewGuid():N}{extension}";
        var destination = GetPhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        try
        {
            File.Move(sourcePath, destination);
            _logger.LogInformation("Moved legacy sensitive upload to protected storage: {StorageKey}", storageKey);
            return Task.FromResult<string?>(storageKey);
        }
        catch (IOException ex) when (File.Exists(destination))
        {
            _logger.LogWarning(ex, "Protected destination already exists while moving legacy upload {LegacyPath}", legacyPath);
            return Task.FromResult<string?>(null);
        }
    }

    private bool TryGetLegacySourcePath(string? legacyPath, string category, out string sourcePath)
    {
        sourcePath = string.Empty;
        if (string.IsNullOrWhiteSpace(legacyPath))
            return false;

        var normalisedPath = legacyPath.Replace('\\', '/');
        var acceptedPrefixes = category == NotesCategory
            ? new[] { "/uploads/notes/" }
            : new[] { "/uploads/lab-results/", "/uploads/labresults/" };

        var prefix = acceptedPrefixes.FirstOrDefault(p => normalisedPath.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        if (prefix == null)
            return false;

        var fileName = normalisedPath[prefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName) || fileName != Path.GetFileName(fileName))
            return false;

        var legacyDirectory = prefix.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        sourcePath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, legacyDirectory, fileName));
        var webRoot = Path.GetFullPath(_environment.WebRootPath) + Path.DirectorySeparatorChar;
        return sourcePath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) && File.Exists(sourcePath);
    }

    private string GetPhysicalPath(string storageKey)
    {
        var normalisedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var category = normalisedKey.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        ValidateCategory(category ?? string.Empty);

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, normalisedKey));
        var rootWithSeparator = _storageRoot.EndsWith(Path.DirectorySeparatorChar)
            ? _storageRoot
            : _storageRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The requested file is outside protected storage.");

        return fullPath;
    }

    private static string ValidateCategory(string category)
    {
        if (!AllowedCategories.Contains(category))
            throw new InvalidOperationException("Invalid protected file category.");

        return category.ToLowerInvariant();
    }
}
