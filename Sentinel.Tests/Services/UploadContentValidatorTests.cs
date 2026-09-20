using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Services;
using System.IO.Compression;
using System.Text;

namespace Sentinel.Tests.Services;

public sealed class UploadContentValidatorTests : IDisposable
{
    private readonly string _temporaryRoot = Path.Combine(Path.GetTempPath(), $"sentinel-upload-test-{Guid.NewGuid():N}");

    [Fact]
    public async Task ReadStrictUtf8TextAsync_RejectsInvalidUtf8()
    {
        await using var stream = new MemoryStream([0xC3, 0x28]);

        await Assert.ThrowsAsync<DecoderFallbackException>(() => UploadContentValidator.ReadStrictUtf8TextAsync(stream));
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsGenericZipRenamedAsDocx()
    {
        var service = CreateStorageService();
        var file = CreateFile("not-a-document.docx", CreateZip((archive) =>
        {
            WriteEntry(archive, "notes.txt", "not an Office document");
        }));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsInvalidUtf8TextAttachment()
    {
        var service = CreateStorageService();
        var file = CreateFile("invalid.txt", [0xC3, 0x28]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_AcceptsValidOpenXmlDocxPackage()
    {
        var service = CreateStorageService();
        var file = CreateFile("document.docx", CreateZip((archive) =>
        {
            WriteEntry(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\" />");
            WriteEntry(archive, "word/document.xml", "<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        }));

        var stored = await service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory);

        Assert.EndsWith(".docx", stored.StorageKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsFilesOverTheConfiguredSizeLimit()
    {
        var service = CreateStorageService(maxUploadBytes: 5);
        var file = CreateFile("oversized.pdf", "%PDF-1"u8.ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory));

        Assert.Contains("exceeds", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_AcceptsAFileExactlyAtTheConfiguredSizeLimit()
    {
        var service = CreateStorageService(maxUploadBytes: 5);
        var file = CreateFile("at-limit.pdf", "%PDF-"u8.ToArray());

        var stored = await service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory);

        Assert.Equal(5, stored.Length);
        Assert.EndsWith(".pdf", stored.StorageKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsDisallowedExtensionBeforeStorage()
    {
        var service = CreateStorageService();
        var file = CreateFile("unsafe.html", "<script>alert(1)</script>"u8.ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory));

        Assert.Contains("not allowed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAttachmentAsync_RejectsRenamedPdfWithInvalidSignature()
    {
        var service = CreateStorageService();
        var file = CreateFile("not-a-pdf.pdf", "plain text"u8.ToArray());

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAttachmentAsync(file, ProtectedFileStorageService.NotesCategory));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenRead_RejectsAPathTraversalStorageKey()
    {
        var service = CreateStorageService();

        var stream = service.OpenRead("notes/../../outside.txt");

        Assert.Null(stream);
    }

    private ProtectedFileStorageService CreateStorageService(long? maxUploadBytes = null)
    {
        Directory.CreateDirectory(_temporaryRoot);
        var environment = new TestHostEnvironment
        {
            ContentRootPath = _temporaryRoot,
            WebRootPath = Path.Combine(_temporaryRoot, "wwwroot")
        };
        Directory.CreateDirectory(environment.WebRootPath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileStorage:RootPath"] = Path.Combine(_temporaryRoot, "files"),
                ["FileStorage:MaxUploadBytes"] = maxUploadBytes?.ToString()
            })
            .Build();

        return new ProtectedFileStorageService(environment, configuration, NullLogger<ProtectedFileStorageService>.Instance);
    }

    private static IFormFile CreateFile(string fileName, byte[] bytes)
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName);
    }

    private static byte[] CreateZip(Action<ZipArchive> addEntries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            addEntries(archive);
        }

        return stream.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_temporaryRoot))
        {
            Directory.Delete(_temporaryRoot, recursive: true);
        }
    }

    private sealed class TestHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Sentinel.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
