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

    // Attachments are always served as downloads, but restricting them to
    // recognised document/image types reduces accidental storage of scripts,
    // executables and active web content. SVG and HTML are intentionally absent.
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv"
    };

    private const int MaximumOriginalFileNameLength = 180;

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

        var originalFileName = NormaliseOriginalFileName(file.FileName);
        var extension = Path.GetExtension(originalFileName);
        ValidateExtension(extension);
        await ValidateFileSignatureAsync(file, extension, cancellationToken);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var storageKey = $"{category}/{storedName}";
        var destination = GetPhysicalPath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(output, cancellationToken);

        return new ProtectedStoredFile(storageKey, originalFileName, file.Length);
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

    private static string NormaliseOriginalFileName(string? fileName)
    {
        var normalised = Path.GetFileName(fileName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalised) ||
            normalised.Length > MaximumOriginalFileNameLength ||
            normalised.Any(char.IsControl))
        {
            throw new InvalidOperationException("The attachment file name is invalid.");
        }

        return normalised;
    }

    private static void ValidateExtension(string extension)
    {
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("This attachment type is not allowed. Upload a PDF, image, Office document, text file or CSV file.");
        }
    }

    private static async Task ValidateFileSignatureAsync(IFormFile file, string extension, CancellationToken cancellationToken)
    {
        // Text/CSV are always served as downloads. Binary formats must also
        // present their expected container signature so a renamed executable is
        // not accepted as a document or image.
        if (extension is ".txt" or ".csv")
        {
            return;
        }

        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        var valid = extension switch
        {
            ".pdf" => StartsWith(header, bytesRead, 0x25, 0x50, 0x44, 0x46, 0x2D),
            ".jpg" or ".jpeg" => StartsWith(header, bytesRead, 0xFF, 0xD8, 0xFF),
            ".png" => StartsWith(header, bytesRead, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            ".doc" or ".xls" => StartsWith(header, bytesRead, 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1),
            ".docx" or ".xlsx" => StartsWith(header, bytesRead, 0x50, 0x4B, 0x03, 0x04),
            _ => false
        };

        if (!valid)
        {
            throw new InvalidOperationException("The attachment content does not match its file type.");
        }
    }

    private static bool StartsWith(byte[] value, int valueLength, params byte[] expected)
    {
        return valueLength >= expected.Length && expected.SequenceEqual(value.Take(expected.Length));
    }
}
