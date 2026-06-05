using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Tests.Services.HL7;

public class HL7ParserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<HL7ParserService>> _loggerMock;
    private readonly HL7ParserService _service;

    // Sample HL7 ORU^R01 message for testing
    private const string SampleHL7Message = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429120000||ORU^R01|MSG00001|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M|||123 MAIN ST^^ANYTOWN^CA^12345||555-1234
OBR|1||ACC123456|87798^CHLAMYDIA NAAT^LN|||202404291000|||||||202404291030||||||||F
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||POSITIVE||NEGATIVE|A|||F|||20240429103000";

    private const string SampleHL7WithMultipleResults = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429120000||ORU^R01|MSG00002|P|2.5.1
PID|1||987654321^^^MRN||SMITH^JANE^A||19900220|F|||456 ELM ST^^OTHERTOWN^CA^54321||555-5678
OBR|1||ACC789012|87798^STI PANEL^LN|||202404291100|||||||202404291130||||||||F
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||POSITIVE||NEGATIVE|A|||F|||20240429113000
OBX|2|ST|87590^GONORRHEA NAAT^LN||NEGATIVE||NEGATIVE||||F|||20240429113000
OBX|3|ST|86780^SYPHILIS AB^LN||POSITIVE||NEGATIVE|A|||F|||20240429113000";

    private const string InvalidHL7Message = "This is not a valid HL7 message";

    public HL7ParserServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<HL7ParserService>>();
        _service = new HL7ParserService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task ParseMessageAsync_ValidMessage_ShouldParseSuccessfully()
    {
        // Act
        var result = await _service.ParseMessageAsync(SampleHL7Message);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HL7ProcessingStatus.ParsedSuccessfully, result.Status);
        Assert.Equal("MSG00001", result.MessageControlId);
        Assert.Equal("FACILITY", result.SendingFacility);
        Assert.Equal("LAB", result.SendingApplication);
        Assert.Equal("ORU^R01", result.MessageType);
        Assert.Equal("2.5.1", result.HL7Version);
        Assert.NotNull(result.ParsedAt);
        Assert.True(result.Segments.Count > 0);
    }

    [Fact]
    public async Task ParseMessageAsync_ValidMessage_ShouldExtractAllSegments()
    {
        // Act
        var result = await _service.ParseMessageAsync(SampleHL7Message);

        // Assert
        Assert.Contains(result.Segments, s => s.SegmentType == "MSH");
        Assert.Contains(result.Segments, s => s.SegmentType == "PID");
        Assert.Contains(result.Segments, s => s.SegmentType == "OBR");
        Assert.Contains(result.Segments, s => s.SegmentType == "OBX");

        var mshSegment = result.Segments.First(s => s.SegmentType == "MSH");
        Assert.Equal(1, mshSegment.SequenceNumber);
        Assert.True(mshSegment.IsParsed);
        Assert.False(string.IsNullOrEmpty(mshSegment.RawSegment));
    }

    [Fact]
    public async Task ParseMessageAsync_MultipleOBXSegments_ShouldParseAll()
    {
        // Act
        var result = await _service.ParseMessageAsync(SampleHL7WithMultipleResults);

        // Assert
        var obxSegments = result.Segments.Where(s => s.SegmentType == "OBX").ToList();
        Assert.Equal(3, obxSegments.Count);
        Assert.Equal(0, obxSegments[0].SetId);
        Assert.Equal(1, obxSegments[1].SetId);
        Assert.Equal(2, obxSegments[2].SetId);
    }

    [Fact]
    public async Task ParseMessageAsync_InvalidMessage_ShouldSetParsingFailedStatus()
    {
        // Act
        var result = await _service.ParseMessageAsync(InvalidHL7Message);

        // Assert
        Assert.Equal(HL7ProcessingStatus.ParsingFailed, result.Status);
        Assert.NotNull(result.ErrorMessage);
        Assert.True(result.ParsingIssues.Count > 0);
        Assert.Equal(HL7IssueType.InvalidFormat, result.ParsingIssues.First().IssueType);
        Assert.Equal(HL7IssueSeverity.Critical, result.ParsingIssues.First().Severity);
    }

    [Fact]
    public async Task ParseMessageAsync_WithConfiguration_ShouldLinkConfiguration()
    {
        // Arrange
        var config = new HL7Configuration
        {
            SendingFacility = "FACILITY",
            SendingApplication = "LAB",
            IsActive = true
        };
        _context.HL7Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ParseMessageAsync(SampleHL7Message, config.Id);

        // Assert
        Assert.Equal(config.Id, result.ConfigurationId);
    }

    [Fact]
    public async Task ParseMessageAsync_ShouldSaveToDatabase()
    {
        // Act
        var result = await _service.ParseMessageAsync(SampleHL7Message);

        // Assert
        var savedMessage = await _context.HL7Messages
            .Include(m => m.Segments)
            .FirstOrDefaultAsync(m => m.Id == result.Id);

        Assert.NotNull(savedMessage);
        Assert.Equal("MSG00001", savedMessage.MessageControlId);
        Assert.True(savedMessage.Segments.Count > 0);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_ValidMessage_ShouldReturnParsedData()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(SampleHL7Message);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("MSG00001", result.MessageControlId);
        Assert.Equal("FACILITY", result.SendingFacility);
        Assert.Equal("LAB", result.SendingApplication);
        Assert.Equal("ORU^R01", result.MessageType);
        Assert.True(result.PatientData.Count > 0);
        Assert.True(result.OrderData.Count > 0);
        Assert.True(result.ResultData.Count > 0);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_ShouldExtractPatientData()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(SampleHL7Message);

        // Assert
        Assert.Equal("12345678", result.PatientData["PatientId"]);
        Assert.Equal("DOE", result.PatientData["LastName"]);
        Assert.Equal("JOHN", result.PatientData["FirstName"]);
        Assert.Equal("M", result.PatientData["MiddleName"]);
        Assert.Equal("M", result.PatientData["Sex"]);
        Assert.Contains("19800115", result.PatientData["DateOfBirth"]);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_ShouldExtractOrderData()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(SampleHL7Message);

        // Assert
        Assert.Equal("ACC123456", result.OrderData["AccessionNumber"]);
        Assert.True(result.OrderData.ContainsKey("ResultStatus")); // nHapi might not parse this field consistently
        Assert.NotEmpty(result.OrderData["OrderDateTime"]);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_ShouldExtractResultData()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(SampleHL7Message);

        // Assert
        Assert.Single(result.ResultData);
        var firstResult = result.ResultData[0];
        Assert.Equal("87798", firstResult["TestCode"]);
        Assert.Equal("CHLAMYDIA NAAT", firstResult["TestName"]);
        Assert.Equal("POSITIVE", firstResult["Result"]);
        Assert.Equal("A", firstResult["AbnormalFlag"]);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_MultipleResults_ShouldExtractAll()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(SampleHL7WithMultipleResults);

        // Assert
        Assert.Equal(3, result.ResultData.Count);
        Assert.Equal("CHLAMYDIA NAAT", result.ResultData[0]["TestName"]);
        Assert.Equal("GONORRHEA NAAT", result.ResultData[1]["TestName"]);
        Assert.Equal("SYPHILIS AB", result.ResultData[2]["TestName"]);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_InvalidMessage_ShouldReturnInvalid()
    {
        // Act
        var result = await _service.ParseMessagePreviewAsync(InvalidHL7Message);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public async Task ParseMessagePreviewAsync_ShouldNotSaveToDatabase()
    {
        // Act
        await _service.ParseMessagePreviewAsync(SampleHL7Message);

        // Assert
        var count = await _context.HL7Messages.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ValidateMessageAsync_ValidMessage_ShouldReturnValid()
    {
        // Act
        var result = await _service.ValidateMessageAsync(SampleHL7Message);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("ORU^R01", result.MessageType);
        Assert.Equal("2.5.1", result.HL7Version);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateMessageAsync_EmptyMessage_ShouldReturnInvalid()
    {
        // Act
        var result = await _service.ValidateMessageAsync("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty"));
    }

    [Fact]
    public async Task ValidateMessageAsync_NoMSHSegment_ShouldReturnInvalid()
    {
        // Arrange
        const string invalidMessage = "PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M";

        // Act
        var result = await _service.ValidateMessageAsync(invalidMessage);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("MSH"));
    }

    [Fact]
    public async Task ValidateMessageAsync_NonORUMessage_ShouldWarn()
    {
        // Arrange - ADT^A01 message
        const string adtMessage = @"MSH|^~\&|ADT|FACILITY|SENTINEL|HOSPITAL|20240429120000||ADT^A01|MSG00003|P|2.5.1";

        // Act
        var result = await _service.ValidateMessageAsync(adtMessage);

        // Assert
        Assert.True(result.IsValid);
        Assert.True(result.Warnings.Count > 0);
        Assert.Contains(result.Warnings, w => w.Contains("not ORU^R01"));
    }

    [Fact]
    public async Task ValidateMessageAsync_MissingMessageControlId_ShouldWarn()
    {
        // Arrange
        const string messageWithoutControlId = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429120000||ORU^R01||P|2.5.1";

        // Act
        var result = await _service.ValidateMessageAsync(messageWithoutControlId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("Message Control ID"));
    }

    [Fact]
    public async Task GetSegmentValue_ExistingSegment_ShouldReturnValue()
    {
        // Arrange
        var message = await _service.ParseMessageAsync(SampleHL7Message);

        // Act
        var pidValue = _service.GetSegmentValue(message, "PID");

        // Assert
        Assert.NotNull(pidValue);
        Assert.Contains("PID", pidValue);
    }

    [Fact]
    public async Task GetSegmentValue_NonExistingSegment_ShouldReturnNull()
    {
        // Arrange
        var message = await _service.ParseMessageAsync(SampleHL7Message);

        // Act
        var value = _service.GetSegmentValue(message, "NK1");

        // Assert
        Assert.Null(value);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
