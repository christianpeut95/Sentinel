using System.Buffers.Binary;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Pages.Cases.Contacts;
using Sentinel.Pages.Settings.Jurisdictions;
using Sentinel.Services;
using OccupationUploadModel = Sentinel.Pages.Settings.Occupations.UploadModel;

namespace Sentinel.Tests.Pages;

public sealed class FileImportValidationTests
{
    [Fact]
    public void UploadEndpoints_DeclareTheirRequestAndMultipartLimits()
    {
        AssertUploadRequestLimit(typeof(OccupationUploadModel), 10 * 1024 * 1024);
        AssertUploadRequestLimit(typeof(BulkPopulationUploadModel), 5 * 1024 * 1024);
        AssertUploadRequestLimit(typeof(BulkCreateModel), 5 * 1024 * 1024);

        // Large public-health boundary files remain supported only on the three
        // jurisdiction endpoints that explicitly opt into the larger budget.
        AssertUploadRequestLimit(typeof(CreateModel), 100_000_000);
        AssertUploadRequestLimit(typeof(EditModel), 100_000_000);
        AssertUploadRequestLimit(typeof(BulkImportModel), 100_000_000);

        var configurationSource = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Sentinel", "Extensions", "SentinelConfigurationExtensions.cs"));
        Assert.Contains("FileStorage:MaxUploadBytes", configurationSource, StringComparison.Ordinal);
        Assert.Contains("options.Limits.MaxRequestBodySize = defaultMultipartRequestLimit;", configurationSource, StringComparison.Ordinal);
        Assert.Contains("options.MultipartBodyLengthLimit = defaultMultipartRequestLimit;", configurationSource, StringComparison.Ordinal);
    }

    [Fact]
    public void AllPageLevelFileUploadSurfaces_UseProtectedStorageOrDedicatedValidation()
    {
        var pagesRoot = Path.Combine(GetRepositoryRoot(), "Sentinel", "Pages");
        var expectedValidationByRelativePath = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine("Cases", "AddLabResult.cshtml.cs")] = ["SaveAttachmentAsync"],
            [Path.Combine("Cases", "Details.cshtml.cs")] = ["SaveAttachmentAsync"],
            [Path.Combine("Cases", "EditLabResult.cshtml.cs")] = ["SaveAttachmentAsync"],
            [Path.Combine("Cases", "Contacts", "BulkCreate.cshtml.cs")] = ["MaximumCsvBytes"],
            [Path.Combine("Contacts", "Details.cshtml.cs")] = ["SaveAttachmentAsync"],
            [Path.Combine("Patients", "Details.cshtml.cs")] = ["SaveAttachmentAsync"],
            [Path.Combine("Settings", "Jurisdictions", "BulkImport.cshtml.cs")] =
                ["GetShapefileAttributeNamesAsync", "ExtractAllFeaturesFromShapefileAsync"],
            [Path.Combine("Settings", "Jurisdictions", "BulkPopulationUpload.cshtml.cs")] = ["MaximumCsvBytes"],
            [Path.Combine("Settings", "Jurisdictions", "Create.cshtml.cs")] = ["ValidateShapefileAsync"],
            [Path.Combine("Settings", "Jurisdictions", "Edit.cshtml.cs")] = ["ValidateShapefileAsync"],
            [Path.Combine("Settings", "Occupations", "Upload.cshtml.cs")] = ["ValidateXlsxArchive"]
        };

        var actualUploadSurfaces = Directory.EnumerateFiles(pagesRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("IFormFile", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(pagesRoot, path))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Equal(
            expectedValidationByRelativePath.Keys.OrderBy(path => path, StringComparer.OrdinalIgnoreCase),
            actualUploadSurfaces);

        foreach (var (relativePath, expectedMarkers) in expectedValidationByRelativePath)
        {
            var source = File.ReadAllText(Path.Combine(pagesRoot, relativePath));
            foreach (var expectedMarker in expectedMarkers)
            {
                Assert.Contains(expectedMarker, source, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public async Task OccupationUpload_RejectsRenamedZipThatIsNotAnXlsx()
    {
        var importer = new Mock<IOccupationImportService>();
        var model = new OccupationUploadModel(importer.Object, NullLogger<OccupationUploadModel>.Instance)
        {
            UploadFile = CreateFile("not-a-workbook.xlsx", CreateZip(archive =>
                WriteEntry(archive, "notes.txt", "not an Office workbook")))
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["UploadFile"]!.Errors,
            error => error.ErrorMessage.Contains("not a valid Excel workbook", StringComparison.OrdinalIgnoreCase));
        importer.Verify(service => service.ImportFromExcelAsync(It.IsAny<Stream>()), Times.Never);
    }

    private static void AssertUploadRequestLimit(Type pageModelType, long expectedBytes)
    {
        var requestLimit = pageModelType
            .GetCustomAttributes(inherit: true)
            .OfType<IRequestSizeLimitMetadata>()
            .SingleOrDefault();
        var formLimit = pageModelType.GetCustomAttribute<RequestFormLimitsAttribute>(inherit: true);

        Assert.NotNull(requestLimit);
        Assert.Equal(expectedBytes, requestLimit!.MaxRequestBodySize);
        Assert.NotNull(formLimit);
        Assert.Equal(expectedBytes, formLimit!.MultipartBodyLengthLimit);
    }

    private static string GetRepositoryRoot()
    {
        foreach (var candidate in new[] { new DirectoryInfo(Directory.GetCurrentDirectory()), new DirectoryInfo(AppContext.BaseDirectory) })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root for upload-limit checks.");
    }

    [Fact]
    public async Task OccupationUpload_AcceptsStructurallyValidXlsxBeforeImporting()
    {
        var importer = new Mock<IOccupationImportService>();
        importer.Setup(service => service.ImportFromExcelAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new ImportResult { Success = true, RecordsImported = 1 });
        var model = new OccupationUploadModel(importer.Object, NullLogger<OccupationUploadModel>.Instance)
        {
            UploadFile = CreateFile("occupations.xlsx", CreateValidXlsx())
        };
        ConfigurePageModel(model);

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(model.ModelState["UploadFile"]?.Errors ?? []);
        Assert.True(model.ImportResult!.Success);
        importer.Verify(service => service.ImportFromExcelAsync(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task OccupationUpload_RejectsFilesAboveItsTenMegabyteRequestLimit()
    {
        var importer = new Mock<IOccupationImportService>();
        var model = new OccupationUploadModel(importer.Object, NullLogger<OccupationUploadModel>.Instance)
        {
            UploadFile = CreateFile("too-large.xlsx", [], 10 * 1024 * 1024 + 1)
        };

        var result = await model.OnPostAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["UploadFile"]!.Errors,
            error => error.ErrorMessage.Contains("less than 10MB", StringComparison.OrdinalIgnoreCase));
        importer.Verify(service => service.ImportFromExcelAsync(It.IsAny<Stream>()), Times.Never);
    }

    [Fact]
    public async Task PopulationPreview_RejectsInvalidUtf8Csv()
    {
        await using var context = CreateContext();
        var model = new BulkPopulationUploadModel(context, NullLogger<BulkPopulationUploadModel>.Instance)
        {
            CsvFile = CreateFile("population.csv", [0xC3, 0x28])
        };

        var result = await model.OnPostPreviewAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState[string.Empty]!.Errors,
            error => error.ErrorMessage.Contains("could not be processed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PopulationPreview_RejectsCsvWithoutRequiredColumns()
    {
        await using var context = CreateContext();
        var model = new BulkPopulationUploadModel(context, NullLogger<BulkPopulationUploadModel>.Instance)
        {
            CsvFile = CreateFile("population.csv", Encoding.UTF8.GetBytes("Name\nExample\n"))
        };

        var result = await model.OnPostPreviewAsync();

        Assert.IsType<PageResult>(result);
        Assert.Contains(model.ModelState["CsvFile"]!.Errors,
            error => error.ErrorMessage.Contains("Missing required columns", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PopulationPreview_AcceptsStrictUtf8CsvWithExpectedColumns()
    {
        await using var context = CreateContext();
        context.Jurisdictions.Add(new Jurisdiction
        {
            Id = 1,
            JurisdictionTypeId = 1,
            Name = "Test jurisdiction",
            Code = "TEST"
        });
        await context.SaveChangesAsync();

        var model = new BulkPopulationUploadModel(context, NullLogger<BulkPopulationUploadModel>.Instance)
        {
            CsvFile = CreateFile("population.csv", Encoding.UTF8.GetBytes(
                "JurisdictionCode,JurisdictionName,Population,PopulationYear,PopulationSource\nTEST,Test jurisdiction,1234,2026,Test source\n"))
        };
        ConfigurePageModel(model);

        var result = await model.OnPostPreviewAsync();

        Assert.IsType<PageResult>(result);
        var preview = Assert.Single(model.PreviewData);
        Assert.Equal("TEST", preview.JurisdictionCode);
        Assert.Equal(1234, preview.Population);
        Assert.Empty(model.ValidationErrors);
    }

    [Fact]
    public async Task ContactCsvUpload_RejectsNonCsvExtensionBeforeProcessing()
    {
        await using var context = CreateContext();
        var model = CreateContactImportModel(context);
        model.CaseId = Guid.NewGuid();
        model.TempData = CreateTempData(model.PageContext.HttpContext);

        var result = await model.OnPostUploadCsvAsync(CreateFile("contacts.txt", "FirstName,LastName\nAda,Lovelace\n"u8.ToArray()));

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Upload a CSV file (.csv).", model.ErrorMessage);
    }

    [Fact]
    public async Task ContactCsvUpload_RejectsFilesAboveItsFiveMegabyteLimit()
    {
        await using var context = CreateContext();
        var model = CreateContactImportModel(context);
        model.CaseId = Guid.NewGuid();
        model.TempData = CreateTempData(model.PageContext.HttpContext);

        var result = await model.OnPostUploadCsvAsync(CreateFile("contacts.csv", [], 5 * 1024 * 1024 + 1));

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("The contact import file exceeds the 5 MB limit.", model.ErrorMessage);
    }

    [Fact]
    public async Task ShapefileService_RejectsUnsafeAndExpansionProneArchivesAcrossReadPaths()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new JurisdictionService(context, cache, NullLogger<JurisdictionService>.Instance);
        var archiveBytes = CreateZip(archive =>
        {
            WriteEntry(archive, "../outside.shp", "unsafe path");
            WriteEntry(archive, "highly-compressible.dbf", new string('A', 128 * 1024), CompressionLevel.SmallestSize);
        });

        await using var validationStream = new MemoryStream(archiveBytes);
        var validation = await service.ValidateShapefileAsync(validationStream);

        await using var attributesStream = new MemoryStream(archiveBytes);
        var attributes = await service.GetShapefileAttributeNamesAsync(attributesStream);

        await using var extractionStream = new MemoryStream(archiveBytes);
        var features = await service.ExtractAllFeaturesFromShapefileAsync(extractionStream);

        Assert.False(validation.success);
        Assert.Empty(attributes);
        Assert.Empty(features);
    }

    [Fact]
    public async Task ShapefileService_RejectsArchivesWithMoreThanOneHundredEntries()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new JurisdictionService(context, cache, NullLogger<JurisdictionService>.Instance);
        var archiveBytes = CreateZip(archive =>
        {
            for (var index = 0; index < 101; index++)
            {
                WriteEntry(archive, $"entry-{index:D3}.txt", "x");
            }
        });

        await using var stream = new MemoryStream(archiveBytes);
        var validation = await service.ValidateShapefileAsync(stream);

        Assert.False(validation.success);
    }

    [Fact]
    public async Task ShapefileService_AcceptsAValidPolygonArchive()
    {
        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new JurisdictionService(context, cache, NullLogger<JurisdictionService>.Instance);
        var archiveBytes = CreateZip(archive =>
        {
            WriteBinaryEntry(archive, "boundary.shp", CreateSinglePolygonShapefile());
            WriteBinaryEntry(archive, "boundary.shx", CreateSinglePolygonShapeIndex());
            WriteBinaryEntry(archive, "boundary.dbf", CreateSingleRecordDbf());
        });

        await using var validationStream = new MemoryStream(archiveBytes);
        var validation = await service.ValidateShapefileAsync(validationStream);

        await using var conversionStream = new MemoryStream(archiveBytes);
        var geoJson = await service.ConvertShapefileToGeoJsonAsync(conversionStream);

        Assert.True(validation.success, validation.error);
        Assert.NotNull(geoJson);
        Assert.Contains("Polygon", geoJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShapefileService_AcceptsALargeValidPolygonArchiveWithinTheJurisdictionBudget()
    {
        const int largeDbfBytes = 16 * 1024 * 1024;

        await using var context = CreateContext();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var service = new JurisdictionService(context, cache, NullLogger<JurisdictionService>.Instance);
        var archiveBytes = CreateZip(archive =>
        {
            WriteBinaryEntry(archive, "boundary.shp", CreateSinglePolygonShapefile());
            WriteBinaryEntry(archive, "boundary.shx", CreateSinglePolygonShapeIndex());
            WriteBinaryEntry(archive, "boundary.dbf", CreateLargeSingleRecordDbf(largeDbfBytes));
        });

        Assert.InRange(archiveBytes.Length, 8 * 1024 * 1024, 100_000_000);

        await using var validationStream = new MemoryStream(archiveBytes);
        var validation = await service.ValidateShapefileAsync(validationStream);

        await using var attributesStream = new MemoryStream(archiveBytes);
        var attributes = await service.GetShapefileAttributeNamesAsync(attributesStream);

        await using var extractionStream = new MemoryStream(archiveBytes);
        var features = await service.ExtractAllFeaturesFromShapefileAsync(extractionStream);

        Assert.True(validation.success, validation.error);
        Assert.Contains("NAME", attributes);
        Assert.Single(features);
    }

    private static BulkCreateModel CreateContactImportModel(ApplicationDbContext context)
    {
        var model = new BulkCreateModel(
            context,
            Mock.Of<IDuplicateDetectionService>(),
            Mock.Of<IPatientIdGeneratorService>(),
            Mock.Of<ICaseIdGeneratorService>(),
            Mock.Of<IOutbreakService>(),
            Mock.Of<IOutbreakAccessService>(),
            Mock.Of<IAuthorizationService>(),
            Mock.Of<IApplicationTimeZoneService>(service => service.Now == new DateTime(2026, 9, 21)));
        ConfigurePageModel(model);
        return model;
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static void ConfigurePageModel(PageModel model)
    {
        var httpContext = new DefaultHttpContext();
        model.PageContext = new PageContext { HttpContext = httpContext };
        model.TempData = CreateTempData(httpContext);
    }

    private static ITempDataDictionary CreateTempData(HttpContext httpContext) =>
        new TempDataDictionary(httpContext, new InMemoryTempDataProvider());

    private static IFormFile CreateFile(string fileName, byte[] content, long? declaredLength = null)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, declaredLength ?? content.Length, "upload", fileName);
    }

    private static byte[] CreateValidXlsx() => CreateZip(archive =>
    {
        WriteEntry(archive, "[Content_Types].xml", "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\" />");
        WriteEntry(archive, "xl/workbook.xml", "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");
    });

    private static byte[] CreateZip(Action<ZipArchive> addEntries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            addEntries(archive);
        }

        return stream.ToArray();
    }

    private static void WriteEntry(
        ZipArchive archive,
        string name,
        string content,
        CompressionLevel compressionLevel = CompressionLevel.Optimal)
    {
        var entry = archive.CreateEntry(name, compressionLevel);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(content);
    }

    private static void WriteBinaryEntry(ZipArchive archive, string name, byte[] content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(content, 0, content.Length);
    }

    private static byte[] CreateSinglePolygonShapefile()
    {
        const int fileLengthBytes = 236;
        const int recordContentLengthWords = 64;
        var bytes = new byte[fileLengthBytes];
        WriteShapefileHeader(bytes, fileLengthBytes / 2);

        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(100, 4), 1);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(104, 4), recordContentLengthWords);
        var content = bytes.AsSpan(108);
        BinaryPrimitives.WriteInt32LittleEndian(content[..4], 5);
        WriteBoundingBox(content.Slice(4, 32));
        BinaryPrimitives.WriteInt32LittleEndian(content.Slice(36, 4), 1);
        BinaryPrimitives.WriteInt32LittleEndian(content.Slice(40, 4), 5);
        BinaryPrimitives.WriteInt32LittleEndian(content.Slice(44, 4), 0);

        var points = new[] { (0d, 0d), (0d, 1d), (1d, 1d), (1d, 0d), (0d, 0d) };
        for (var index = 0; index < points.Length; index++)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(content.Slice(48 + index * 16, 8), points[index].Item1);
            BinaryPrimitives.WriteDoubleLittleEndian(content.Slice(56 + index * 16, 8), points[index].Item2);
        }

        return bytes;
    }

    private static byte[] CreateSinglePolygonShapeIndex()
    {
        const int fileLengthBytes = 108;
        var bytes = new byte[fileLengthBytes];
        WriteShapefileHeader(bytes, fileLengthBytes / 2);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(100, 4), 50);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(104, 4), 64);
        return bytes;
    }

    private static byte[] CreateSingleRecordDbf()
    {
        const int headerLength = 65;
        const int recordLength = 21;
        var bytes = new byte[headerLength + recordLength + 1];
        bytes[0] = 0x03;
        bytes[1] = 126;
        bytes[2] = 9;
        bytes[3] = 19;
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), 1);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(8, 2), headerLength);
        BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(10, 2), recordLength);

        Encoding.ASCII.GetBytes("NAME").CopyTo(bytes, 32);
        bytes[43] = (byte)'C';
        bytes[48] = 20;
        bytes[64] = 0x0D;

        bytes[65] = 0x20;
        Encoding.ASCII.GetBytes("Test boundary").CopyTo(bytes, 66);
        for (var index = 79; index < 86; index++)
        {
            bytes[index] = (byte)' ';
        }

        bytes[^1] = 0x1A;
        return bytes;
    }

    private static byte[] CreateLargeSingleRecordDbf(int targetLength)
    {
        var dbf = CreateSingleRecordDbf();
        Array.Resize(ref dbf, targetLength);

        // Use deterministic high-entropy padding after the valid DBF record.
        // This keeps the ZIP realistically large while leaving the parsed record intact.
        uint state = 0x9E3779B9;
        for (var index = 86; index < dbf.Length; index++)
        {
            state = state * 1_664_525 + 1_013_904_223;
            dbf[index] = (byte)(state >> 24);
        }

        return dbf;
    }

    private static void WriteShapefileHeader(byte[] bytes, int fileLengthWords)
    {
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(0, 4), 9994);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(24, 4), fileLengthWords);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28, 4), 1000);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32, 4), 5);
        WriteBoundingBox(bytes.AsSpan(36, 32));
    }

    private static void WriteBoundingBox(Span<byte> bytes)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(bytes[..8], 0d);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.Slice(8, 8), 0d);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.Slice(16, 8), 1d);
        BinaryPrimitives.WriteDoubleLittleEndian(bytes.Slice(24, 8), 1d);
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
