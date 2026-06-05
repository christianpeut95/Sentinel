using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7;

namespace Sentinel.Tests.Services.HL7;

public class DuplicateDetectionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<DuplicateDetectionService>> _loggerMock;
    private readonly DuplicateDetectionService _service;

    // Sample HL7 messages with different scenarios
    private const string SampleMessage1 = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429120000||ORU^R01|MSG00001|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M
OBR|1||ACC123456|87798^CHLAMYDIA NAAT^LN|||202404291000
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||POSITIVE||NEGATIVE|A|||F|||20240429103000";

    private const string SampleMessage2_SameContent = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429130000||ORU^R01|MSG00002|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M
OBR|1||ACC123456|87798^CHLAMYDIA NAAT^LN|||202404291000
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||POSITIVE||NEGATIVE|A|||F|||20240429103000";

    private const string SampleMessage3_DifferentTest = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240429140000||ORU^R01|MSG00003|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M
OBR|1||ACC123456|87590^GONORRHEA NAAT^LN|||202404291000
OBX|1|ST|87590^GONORRHEA NAAT^LN||NEGATIVE||NEGATIVE||||F|||20240429103000";

    private const string SampleMessage4_DifferentResult = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240502150000||ORU^R01|MSG00004|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M
OBR|1||ACC123456|87798^CHLAMYDIA NAAT^LN|||202405021000
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||NEGATIVE||NEGATIVE||||F|||20240502103000";

    private const string SampleMessage5_FollowUp = @"MSH|^~\&|LAB|FACILITY|SENTINEL|HOSPITAL|20240506120000||ORU^R01|MSG00005|P|2.5.1
PID|1||12345678^^^MRN||DOE^JOHN^M||19800115|M
OBR|1||ACC123456|87070^CULTURE IDENTIFICATION^LN|||202404291000
OBX|1|ST|87070^CULTURE IDENTIFICATION^LN||STAPHYLOCOCCUS AUREUS||||||F|||20240506103000";

    public DuplicateDetectionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new ApplicationDbContext(options);
        _loggerMock = new Mock<ILogger<DuplicateDetectionService>>();
        _service = new DuplicateDetectionService(_context, _loggerMock.Object);
    }

    [Fact]
    public async Task CheckForDuplicate_MessageControlId_ShouldFindDuplicate()
    {
        // Arrange
        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var duplicate = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        duplicate.Id = Guid.NewGuid();

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.MessageControlId
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(duplicate, config);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal(original.Id, result.OriginalMessageId);
        Assert.Equal("MessageControlId", result.DetectionMethod);
        Assert.Contains(result.MatchCriteria, c => c.Contains("MSG00001"));
    }

    [Fact]
    public async Task CheckForDuplicate_MessageControlId_DifferentFacility_ShouldNotBeDuplicate()
    {
        // Arrange
        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY_A");
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var newMessage = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY_B");

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.MessageControlId
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(newMessage, config);

        // Assert
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task CheckForDuplicate_AccessionAndContent_SameContent_ShouldFindDuplicate()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        original.LaboratoryOrganizationId = labOrg.Id;
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var duplicate = CreateHL7Message(SampleMessage2_SameContent, "MSG00002", "FACILITY");
        duplicate.LaboratoryOrganizationId = labOrg.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.AccessionAndLab
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(duplicate, config);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Equal("ACC123456", result.AccessionNumber);
        Assert.Contains("Identical test results", result.MatchCriteria);
    }

    [Fact]
    public async Task CheckForDuplicate_AccessionAndContent_DifferentTest_ShouldBeRelatedNotDuplicate()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        original.LaboratoryOrganizationId = labOrg.Id;
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var followUp = CreateHL7Message(SampleMessage3_DifferentTest, "MSG00003", "FACILITY");
        followUp.LaboratoryOrganizationId = labOrg.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.AccessionAndLab
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(followUp, config);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.True(result.IsRelatedResult);
        Assert.Contains(original.Id, result.RelatedMessageIds);
        Assert.Contains("New tests", result.DifferenceDescription);
    }

    [Fact]
    public async Task CheckForDuplicate_AccessionAndContent_DifferentResult_ShouldBeRelatedNotDuplicate()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var preliminary = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        preliminary.LaboratoryOrganizationId = labOrg.Id;
        _context.HL7Messages.Add(preliminary);
        await _context.SaveChangesAsync();

        var final = CreateHL7Message(SampleMessage4_DifferentResult, "MSG00004", "FACILITY");
        final.LaboratoryOrganizationId = labOrg.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.AccessionAndLab
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(final, config);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.True(result.IsRelatedResult);
        Assert.Contains("POSITIVE → NEGATIVE", result.DifferenceDescription);
    }

    [Fact]
    public async Task CheckForDuplicate_CultureFollowUp_ShouldBeRelatedNotDuplicate()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var initialCulture = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        initialCulture.LaboratoryOrganizationId = labOrg.Id;
        _context.HL7Messages.Add(initialCulture);
        await _context.SaveChangesAsync();

        var identification = CreateHL7Message(SampleMessage5_FollowUp, "MSG00005", "FACILITY");
        identification.LaboratoryOrganizationId = labOrg.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.AccessionAndLab
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(identification, config);

        // Assert
        Assert.False(result.IsDuplicate);
        Assert.True(result.IsRelatedResult);
        Assert.Equal("ACC123456", result.AccessionNumber);
        Assert.Contains("New tests", result.DifferenceDescription);
    }

    [Fact]
    public async Task CheckForDuplicate_PatientSpecimenTest_SameContent_ShouldFindDuplicate()
    {
        // Arrange
        var patient = new Patient { Id = Guid.NewGuid(), GivenName = "John", FamilyName = "Doe" };
        _context.Patients.Add(patient);

        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        original.PatientId = patient.Id;
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var duplicate = CreateHL7Message(SampleMessage2_SameContent, "MSG00002", "FACILITY");
        duplicate.PatientId = patient.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.PatientSpecimenTest
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(duplicate, config);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.Contains(result.MatchCriteria, c => c.Contains(patient.Id.ToString()));
        Assert.Contains(result.MatchCriteria, c => c.Contains("87798"));
    }

    [Fact]
    public async Task CheckForDuplicate_Disabled_ShouldAlwaysReturnNotDuplicate()
    {
        // Arrange
        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var duplicate = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.Disabled
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(duplicate, config);

        // Assert
        Assert.False(result.IsDuplicate);
    }

    [Fact]
    public async Task CheckForDuplicate_Combined_ShouldTryMultipleStrategies()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var original = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        original.LaboratoryOrganizationId = labOrg.Id;
        _context.HL7Messages.Add(original);
        await _context.SaveChangesAsync();

        var duplicate = CreateHL7Message(SampleMessage2_SameContent, "MSG00002", "FACILITY");
        duplicate.LaboratoryOrganizationId = labOrg.Id;

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.Combined
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(duplicate, config);

        // Assert
        Assert.True(result.IsDuplicate);
        Assert.NotNull(result.DetectionMethod);
    }

    [Fact]
    public async Task CheckForDuplicate_TimeWindow_ShouldOnlyCheckRecentMessages()
    {
        // Arrange
        var oldMessage = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        oldMessage.ReceivedAt = DateTime.UtcNow.AddDays(-100); // Outside default 90-day window
        _context.HL7Messages.Add(oldMessage);
        await _context.SaveChangesAsync();

        var newMessage = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.MessageControlId,
            DuplicateDetectionWindowHours = 2160 // 90 days
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(newMessage, config);

        // Assert
        Assert.False(result.IsDuplicate); // Old message outside window
    }

    [Fact]
    public async Task HasSameContentAsync_IdenticalContent_ShouldReturnTrue()
    {
        // Arrange
        var message1 = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        var message2 = CreateHL7Message(SampleMessage2_SameContent, "MSG00002", "FACILITY");

        // Act
        var result = await _service.HasSameContentAsync(message1, message2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasSameContentAsync_DifferentContent_ShouldReturnFalse()
    {
        // Arrange
        var message1 = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        var message2 = CreateHL7Message(SampleMessage3_DifferentTest, "MSG00003", "FACILITY");

        // Act
        var result = await _service.HasSameContentAsync(message1, message2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task FindRelatedMessagesAsync_ShouldReturnSameAccessionMessages()
    {
        // Arrange
        var labOrg = new Organization { Id = Guid.NewGuid(), Name = "Test Lab" };
        _context.Organizations.Add(labOrg);

        var message1 = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        message1.LaboratoryOrganizationId = labOrg.Id;

        var message2 = CreateHL7Message(SampleMessage3_DifferentTest, "MSG00003", "FACILITY");
        message2.LaboratoryOrganizationId = labOrg.Id;

        var message3 = CreateHL7Message(SampleMessage5_FollowUp, "MSG00005", "FACILITY");
        message3.LaboratoryOrganizationId = labOrg.Id;

        _context.HL7Messages.AddRange(message1, message2, message3);
        await _context.SaveChangesAsync();

        var newMessage = CreateHL7Message(SampleMessage4_DifferentResult, "MSG00004", "FACILITY");
        newMessage.LaboratoryOrganizationId = labOrg.Id;

        // Act
        var related = await _service.FindRelatedMessagesAsync(newMessage);

        // Assert
        Assert.Equal(3, related.Count); // All have same accession number ACC123456
        Assert.Contains(related, m => m.Id == message1.Id);
        Assert.Contains(related, m => m.Id == message2.Id);
        Assert.Contains(related, m => m.Id == message3.Id);
    }

    [Fact]
    public void ExtractTestResults_ShouldParseOBXSegments()
    {
        // Arrange
        var message = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");

        // Act
        var results = _service.ExtractTestResults(message);

        // Assert
        Assert.Single(results);
        Assert.Equal("87798", results[0].TestCode);
        Assert.Equal("CHLAMYDIA NAAT", results[0].TestName);
        Assert.Equal("POSITIVE", results[0].ResultValue);
    }

    [Fact]
    public async Task CheckForDuplicate_SameMessageId_ShouldNotMarkAsDuplicateOfItself()
    {
        // Arrange
        var message = CreateHL7Message(SampleMessage1, "MSG00001", "FACILITY");
        _context.HL7Messages.Add(message);
        await _context.SaveChangesAsync();

        var config = new HL7Configuration
        {
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.MessageControlId
        };

        // Act
        var result = await _service.CheckForDuplicateAsync(message, config);

        // Assert
        Assert.False(result.IsDuplicate);
    }

    private HL7Message CreateHL7Message(string rawMessage, string messageControlId, string sendingFacility)
    {
        var message = new HL7Message
        {
            Id = Guid.NewGuid(),
            RawMessage = rawMessage,
            MessageControlId = messageControlId,
            SendingFacility = sendingFacility,
            MessageType = "ORU^R01",
            Status = HL7ProcessingStatus.ParsedSuccessfully,
            ReceivedAt = DateTime.UtcNow,
            MessageDateTime = DateTime.UtcNow
        };

        // Parse segments
        var lines = rawMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        int sequenceNumber = 0;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var segmentType = line.Length >= 3 ? line.Substring(0, 3) : line;
            message.Segments.Add(new HL7MessageSegment
            {
                Id = Guid.NewGuid(),
                SegmentType = segmentType,
                SequenceNumber = ++sequenceNumber,
                RawSegment = line,
                IsParsed = true
            });
        }

        return message;
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
