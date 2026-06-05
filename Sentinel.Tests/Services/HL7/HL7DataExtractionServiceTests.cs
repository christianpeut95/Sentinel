using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services.HL7;
using Xunit;

namespace Sentinel.Tests.Services.HL7
{
    public class HL7DataExtractionServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IDuplicateDetectionService> _mockDuplicateService;
        private readonly Mock<ICaseMatchingService> _mockCaseMatchingService;
        private readonly Mock<ILogger<HL7DataExtractionService>> _mockLogger;
        private readonly Mock<ILogger<HL7ParserService>> _mockParserLogger;
        private readonly HL7ParserService _parserService;
        private readonly HL7DataExtractionService _service;

        public HL7DataExtractionServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _mockDuplicateService = new Mock<IDuplicateDetectionService>();
            _mockCaseMatchingService = new Mock<ICaseMatchingService>();
            _mockLogger = new Mock<ILogger<HL7DataExtractionService>>();
            _mockParserLogger = new Mock<ILogger<HL7ParserService>>();

            // Mock the new marker resolution service
            var mockMarkerResolutionService = new Mock<IHL7MarkerResolutionService>();
            mockMarkerResolutionService
                .Setup(m => m.ResolveMarkerFieldsAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<decimal?>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MarkerResolutionResult());

            var mockFieldMappingService = new Mock<IHL7FieldMappingService>();

            // Use real HL7ParserService for authentic test messages
            _parserService = new HL7ParserService(_context, _mockParserLogger.Object);

            _service = new HL7DataExtractionService(
                _context,
                _mockLogger.Object,
                _mockDuplicateService.Object,
                _mockCaseMatchingService.Object,
                mockMarkerResolutionService.Object,
                mockFieldMappingService.Object);

            SeedLookupData();
        }

        private void SeedLookupData()
        {
            // Seed organization types
            _context.OrganizationTypes.AddRange(
                new OrganizationType { Id = 1, Name = "Laboratory", IsActive = true },
                new OrganizationType { Id = 2, Name = "Healthcare Provider", IsActive = true }
            );

            // Seed sex at birth
            _context.SexAtBirths.AddRange(
                new SexAtBirth { Id = 1, Name = "Male", IsActive = true },
                new SexAtBirth { Id = 2, Name = "Female", IsActive = true },
                new SexAtBirth { Id = 3, Name = "Other", IsActive = true }
            );

            // Seed states
            _context.States.AddRange(
                new State { Id = 1, Code = "NSW", Name = "New South Wales", IsActive = true },
                new State { Id = 2, Code = "VIC", Name = "Victoria", IsActive = true },
                new State { Id = 3, Code = "QLD", Name = "Queensland", IsActive = true }
            );

            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Patient Matching Tests

        [Fact]
        public async Task FindOrCreatePatientAsync_WithMRN_FindsExistingPatient()
        {
            // Arrange
            var existingPatient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0001",
                GivenName = "John",
                FamilyName = "Smith",
                DateOfBirth = new DateTime(1980, 5, 15)
            };
            _context.Patients.Add(existingPatient);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7Message("P-2026-0001", "John", "Smith", new DateTime(1980, 5, 15));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.IdentifierOnly,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingPatient.Id, result.Patient?.Id);
            Assert.False(result.IsNewPatient);
            Assert.False(result.RequiresManualReview);
            Assert.Equal("IdentifierOnly", result.MatchMethod);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_StrictMatch_FindsExactDemographics()
        {
            // Arrange
            var existingPatient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0002",
                GivenName = "Jane",
                FamilyName = "Doe",
                DateOfBirth = new DateTime(1990, 3, 20)
            };
            _context.Patients.Add(existingPatient);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7Message(null, "Jane", "Doe", new DateTime(1990, 3, 20));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.StrictMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingPatient.Id, result.Patient?.Id);
            Assert.False(result.IsNewPatient);
            Assert.Equal("StrictMatch", result.MatchMethod);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_FuzzyMatch_FindsCaseInsensitiveName()
        {
            // Arrange
            var existingPatient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0003",
                GivenName = "Robert",
                FamilyName = "Johnson",
                DateOfBirth = new DateTime(1975, 8, 10)
            };
            _context.Patients.Add(existingPatient);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7Message(null, "robert", "JOHNSON", new DateTime(1975, 8, 10));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.FuzzyMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingPatient.Id, result.Patient?.Id);
            Assert.False(result.IsNewPatient);
            Assert.Equal("FuzzyMatch", result.MatchMethod);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_NoMatch_CreatesNewPatient()
        {
            // Arrange
            var message = await CreateTestHL7Message(null, "NewPatient", "Test", new DateTime(2000, 1, 1));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.FuzzyMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Patient);
            Assert.True(result.IsNewPatient);
            Assert.Equal("NewPatient", result.Patient.GivenName);
            Assert.Equal("Test", result.Patient.FamilyName);
            Assert.Equal(new DateTime(2000, 1, 1), result.Patient.DateOfBirth);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_ConflictingMRNAndDemographics_FlagsForManualReview()
        {
            // Arrange
            var patientByMRN = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0100",
                GivenName = "Alice",
                FamilyName = "Brown",
                DateOfBirth = new DateTime(1985, 6, 15)
            };

            var patientByDemographics = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0101",
                GivenName = "Bob",
                FamilyName = "White",
                DateOfBirth = new DateTime(1985, 7, 20)
            };

            _context.Patients.AddRange(patientByMRN, patientByDemographics);
            await _context.SaveChangesAsync();

            // Message has MRN matching first patient but demographics matching second
            var message = await CreateTestHL7Message("P-2026-0100", "Bob", "White", new DateTime(1985, 7, 20));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.FuzzyMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.RequiresManualReview);
            Assert.Contains("belongs to different patient than demographics", result.ConflictReason);
            Assert.Equal(2, result.ConflictingPatients.Count);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_ManualReviewStrategy_AlwaysReturnsNull()
        {
            // Arrange
            var message = await CreateTestHL7Message("P-2026-0200", "Manual", "Review", new DateTime(1995, 1, 1));

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.ManualReviewRequired,
                autoCreate: false);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Patient);
            Assert.True(result.RequiresManualReview);
            Assert.Equal("ManualReviewRequired", result.MatchMethod);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_MapsAddressAndPhone()
        {
            // Arrange
            var message = await CreateTestHL7MessageWithFullAddress(
                null, "Address", "Test",
                new DateTime(1990, 1, 1),
                "123 Main St", "Sydney", "NSW", "2000", "0412345678");

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.FuzzyMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Patient);
            Assert.Equal("123 Main St", result.Patient.AddressLine);
            Assert.Equal("Sydney", result.Patient.City);
            Assert.Equal(1, result.Patient.StateId); // NSW
            Assert.Equal("2000", result.Patient.PostalCode);
            Assert.Equal("0412345678", result.Patient.HomePhone);
        }

        [Fact]
        public async Task FindOrCreatePatientAsync_MapsSexAtBirth()
        {
            // Arrange
            var message = await CreateTestHL7MessageWithSex(null, "Sex", "Test", new DateTime(1990, 1, 1), "Male");

            // Act
            var result = await _service.FindOrCreatePatientAsync(
                message,
                PatientMatchingStrategy.FuzzyMatch,
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Patient);
            Assert.Equal(1, result.Patient.SexAtBirthId); // Male
        }

        #endregion

        #region Organization Matching Tests

        [Fact]
        public async Task FindOrCreateOrganizationAsync_ExactMatch_FindsExisting()
        {
            // Arrange
            var existingLab = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "PathLab Services",
                OrganizationTypeId = 1
            };
            _context.Organizations.Add(existingLab);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.FindOrCreateOrganizationAsync(
                "PathLab Services",
                "Laboratory",
                autoCreate: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingLab.Id, result.Id);
        }

        [Fact]
        public async Task FindOrCreateOrganizationAsync_FuzzyMatch_FindsSimilarName()
        {
            // Arrange
            var existingLab = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Australian Clinical Labs",
                OrganizationTypeId = 1
            };
            _context.Organizations.Add(existingLab);
            await _context.SaveChangesAsync();

            // Act - Try to find with slight variation
            var result = await _service.FindOrCreateOrganizationAsync(
                "australian clinical labs",
                "Laboratory",
                autoCreate: false);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingLab.Id, result.Id);
        }

        [Fact]
        public async Task FindOrCreateOrganizationAsync_NoMatch_CreatesWhenAutoCreateTrue()
        {
            // Act
            var result = await _service.FindOrCreateOrganizationAsync(
                "New Lab Services",
                "Laboratory",
                autoCreate: true);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("New Lab Services", result.Name);
            Assert.Equal(1, result.OrganizationTypeId);

            // Verify saved to database
            var saved = await _context.Organizations.FindAsync(result.Id);
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task FindOrCreateOrganizationAsync_NoMatch_ReturnsNullWhenAutoCreateFalse()
        {
            // Act
            var result = await _service.FindOrCreateOrganizationAsync(
                "Nonexistent Lab",
                "Laboratory",
                autoCreate: false);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task FindOrCreateOrganizationAsync_InvalidTypeName_ReturnsNull()
        {
            // Act
            var result = await _service.FindOrCreateOrganizationAsync(
                "Some Org",
                "InvalidType",
                autoCreate: true);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region LabResult Creation Tests

        [Fact]
        public async Task CreateOrUpdateLabResultAsync_NewAccession_CreatesLabResult()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0500",
                GivenName = "Test",
                FamilyName = "Patient",
                DateOfBirth = new DateTime(1990, 1, 1)
            };

            var laboratory = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Lab",
                OrganizationTypeId = 1
            };

            _context.Patients.Add(patient);
            _context.Organizations.Add(laboratory);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7MessageWithResults(
                "ACC-001",
                new[] {
                    new { TestCode = "HIV-1", Result = "Detected", Unit = "", Status = "F" }
                });

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult { IsDuplicate = false });

            // Act
            var result = await _service.CreateOrUpdateLabResultAsync(
                message,
                patient,
                laboratory,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("ACC-001", result.AccessionNumber);
            Assert.Equal(patient.Id, result.PatientId);
            Assert.Equal(laboratory.Id, result.LaboratoryId);
            Assert.Single(result.Markers);
            Assert.Equal("HIV-1", result.Markers.First().TestCode);
            Assert.Equal("Detected", result.Markers.First().QualitativeResultText);
        }

        [Fact]
        public async Task CreateOrUpdateLabResultAsync_ExistingAccession_AppendsNewMarker()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0501",
                GivenName = "Test",
                FamilyName = "Patient"
            };

            var laboratory = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Lab",
                OrganizationTypeId = 1
            };

            var existingLabResult = new LabResult
            {
                Id = Guid.NewGuid(),
                FriendlyId = "LAB-2026-00001",
                AccessionNumber = "ACC-002",
                PatientId = patient.Id,
                LaboratoryId = laboratory.Id,
                Markers = new List<LabResultMarker>
                {
                    new LabResultMarker
                    {
                        Id = Guid.NewGuid(),
                        TestCode = "HIV-1",
                        QualitativeResultText = "Not Detected",
                        ResultStatus = "Preliminary"
                    }
                }
            };

            _context.Patients.Add(patient);
            _context.Organizations.Add(laboratory);
            _context.LabResults.Add(existingLabResult);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7MessageWithResults(
                "ACC-002",
                new[] {
                    new { TestCode = "HIV-2", Result = "Not Detected", Unit = "", Status = "F" }
                });

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult { IsDuplicate = false });

            // Act
            var result = await _service.CreateOrUpdateLabResultAsync(
                message,
                patient,
                laboratory,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(existingLabResult.Id, result.Id);
            Assert.Equal(2, result.Markers.Count);
            Assert.Contains(result.Markers, m => m.TestCode == "HIV-1");
            Assert.Contains(result.Markers, m => m.TestCode == "HIV-2");
        }

        [Fact]
        public async Task CreateOrUpdateLabResultAsync_ExistingMarker_UpdatesWithHistory()
        {
            // Arrange
            var patient = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0502",
                GivenName = "Test",
                FamilyName = "Patient"
            };

            var laboratory = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Test Lab",
                OrganizationTypeId = 1
            };

            var existingLabResult = new LabResult
            {
                Id = Guid.NewGuid(),
                FriendlyId = "LAB-2026-00002",
                AccessionNumber = "ACC-003",
                PatientId = patient.Id,
                LaboratoryId = laboratory.Id
            };

            var existingMarker = new LabResultMarker
            {
                Id = Guid.NewGuid(),
                LabResultId = existingLabResult.Id,
                TestCode = "HIV-1",
                QualitativeResultText = "Not Detected",
                ResultStatus = "P"
            };

            _context.Patients.Add(patient);
            _context.Organizations.Add(laboratory);
            _context.LabResults.Add(existingLabResult);
            _context.LabResultMarkers.Add(existingMarker);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7MessageWithResults(
                "ACC-003",
                new[] {
                    new { TestCode = "HIV-1", Result = "Detected", Unit = "", Status = "F" }
                });

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult { IsDuplicate = false });

            // Act
            var result = await _service.CreateOrUpdateLabResultAsync(
                message,
                patient,
                laboratory,
                null);

            // Save changes to persist history
            await _context.SaveChangesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Markers);
            var updatedMarker = result.Markers.First();
            Assert.Equal("Detected", updatedMarker.QualitativeResultText);
            Assert.Equal("F", updatedMarker.ResultStatus);

            // Verify history was created
            var history = await _context.LabResultMarkerHistories
                .Where(h => h.LabResultMarkerId == existingMarker.Id)
                .ToListAsync();

            Assert.Single(history);
            Assert.Equal("Not Detected", history[0].PreviousQualitativeValue);
            Assert.Equal("Detected", history[0].NewQualitativeValue);
            Assert.Equal(MarkerChangeType.Finalized, history[0].ChangeType); // P -> F is Finalized, not just Updated
        }

        [Fact]
        public async Task CreateOrUpdateLabResultAsync_DuplicateDetected_SkipsProcessing()
        {
            // Arrange
            var patient = new Patient { Id = Guid.NewGuid(), FriendlyId = "P-2026-0503", GivenName = "Test", FamilyName = "Patient" };
            var laboratory = new Organization { Id = Guid.NewGuid(), Name = "Test Lab", OrganizationTypeId = 1 };

            _context.Patients.Add(patient);
            _context.Organizations.Add(laboratory);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7MessageWithResults("ACC-004", new[] {
                new { TestCode = "HIV-1", Result = "Detected", Unit = "", Status = "F" }
            });

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult 
                { 
                    IsDuplicate = true, 
                    OriginalMessageId = Guid.NewGuid(),
                    DetectionMethod = "Test",
                    DifferenceDescription = "Duplicate found" 
                });

            // Act
            var result = await _service.CreateOrUpdateLabResultAsync(
                message,
                patient,
                laboratory,
                null);

            // Assert
            Assert.Null(result); // Should not create/update when duplicate detected
        }

        [Fact]
        public async Task CreateOrUpdateLabResultAsync_QuantitativeResult_ParsesCorrectly()
        {
            // Arrange
            var patient = new Patient { Id = Guid.NewGuid(), FriendlyId = "P-2026-0504", GivenName = "Test", FamilyName = "Patient" };
            var laboratory = new Organization { Id = Guid.NewGuid(), Name = "Test Lab", OrganizationTypeId = 1 };

            _context.Patients.Add(patient);
            _context.Organizations.Add(laboratory);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7MessageWithQuantitativeResults(
                "ACC-005",
                new[] {
                    new { TestCode = "GLUCOSE", Result = "5.5", Unit = "mmol/L", Status = "F" }
                });

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult { IsDuplicate = false });

            // Act
            var result = await _service.CreateOrUpdateLabResultAsync(
                message,
                patient,
                laboratory,
                null);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Markers);
            var marker = result.Markers.First();
            Assert.Equal("GLUCOSE", marker.TestCode);
            Assert.Equal(5.5m, marker.QuantitativeValue);
            Assert.Equal("mmol/L", marker.QuantitativeUnit);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task ExtractAndCreateEntitiesAsync_FullWorkflow_CreatesAllEntities()
        {
            // Arrange
            var message = await CreateCompleteHL7Message(
                mrn: null,
                firstName: "Integration",
                lastName: "Test",
                dob: new DateTime(1985, 6, 15),
                accessionNumber: "ACC-999",
                labName: "Integration Lab",
                providerName: "Dr. Test Provider");

            _mockDuplicateService
                .Setup(x => x.CheckForDuplicateAsync(It.IsAny<HL7Message>(), It.IsAny<HL7Configuration?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DuplicateDetectionResult { IsDuplicate = false });

            var config = new HL7Configuration
            {
                AutoCreatePatients = true,
                AutoCreateOrganizations = true,
                PatientMatchingStrategy = PatientMatchingStrategy.FuzzyMatch
            };

            // Act
            var result = await _service.ExtractAndCreateEntitiesAsync(
                message,
                configuration: config);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Patient);
            Assert.NotNull(result.Laboratory);
            Assert.NotNull(result.LabResult);
            Assert.Equal("Integration", result.Patient.GivenName);
            Assert.Equal("Test", result.Patient.FamilyName);
            Assert.Equal("Integration Lab", result.Laboratory.Name);
            Assert.Equal("ACC-999", result.LabResult.AccessionNumber);
        }

        [Fact]
        public async Task ExtractAndCreateEntitiesAsync_ConflictingPatient_ReturnsWarning()
        {
            // Arrange
            var existingPatient1 = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0600",
                GivenName = "Conflict1",
                FamilyName = "Test"
            };

            var existingPatient2 = new Patient
            {
                Id = Guid.NewGuid(),
                FriendlyId = "P-2026-0601",
                GivenName = "Conflict2",
                FamilyName = "Test"
            };

            _context.Patients.AddRange(existingPatient1, existingPatient2);
            await _context.SaveChangesAsync();

            var message = await CreateTestHL7Message("P-2026-0600", "Conflict2", "Test", null);

            // Act
            var result = await _service.ExtractAndCreateEntitiesAsync(
                message,
                configuration: null);

            // Assert
            Assert.False(result.Success);
            Assert.True(result.RequiresManualReview);
            Assert.Contains(result.Errors, e => e.Contains("belongs to different patient"));
        }

        #endregion

        #region Helper Methods

        private async Task<HL7Message> CreateTestHL7Message(string? mrn, string firstName, string lastName, DateTime? dob)
        {
            var content = $@"MSH|^~\&|LAB|TEST|APP|TEST|20260101120000||ORU^R01|MSG001|P|2.5.1
PID|1||{mrn ?? ""}||{lastName}^{firstName}||{(dob?.ToString("yyyyMMdd") ?? "")}|M
OBR|1||ACC-001|HIV^HIV Test||20260101100000
OBX|1|ST|HIV-1^HIV-1 Test||Detected||||||F";

            // Use real parser to create properly structured HL7Message
            return await _parserService.ParseMessageAsync(content);
        }

        private async Task<HL7Message> CreateTestHL7MessageWithFullAddress(
            string? mrn, string firstName, string lastName, DateTime dob,
            string address, string city, string state, string zip, string phone)
        {
            var content = $@"MSH|^~\&|LAB|TEST|APP|TEST|20260101120000||ORU^R01|MSG002|P|2.5.1
PID|1||{mrn ?? ""}||{lastName}^{firstName}||{dob:yyyyMMdd}|M|||{address}^^{city}^{state}^{zip}||{phone}
OBR|1||ACC-002|TEST^Test||20260101100000
OBX|1|ST|TEST^Test||Normal||||||F";

            return await _parserService.ParseMessageAsync(content);
        }

        private async Task<HL7Message> CreateTestHL7MessageWithSex(string? mrn, string firstName, string lastName, DateTime dob, string sex)
        {
            var content = $@"MSH|^~\&|LAB|TEST|APP|TEST|20260101120000||ORU^R01|MSG003|P|2.5.1
PID|1||{mrn ?? ""}||{lastName}^{firstName}||{dob:yyyyMMdd}|{sex}
OBR|1||ACC-003|TEST^Test||20260101100000
OBX|1|ST|TEST^Test||Normal||||||F";

            return await _parserService.ParseMessageAsync(content);
        }

        private async Task<HL7Message> CreateTestHL7MessageWithResults(string accessionNumber, dynamic[] tests)
        {
            var obxSegments = string.Join("\n", tests.Select((t, i) =>
                $"OBX|{i + 1}|ST|{t.TestCode}^Test||{t.Result}|{t.Unit}|||||{t.Status}"));

            var content = $@"MSH|^~\&|LAB|TEST|APP|TEST|20260101120000||ORU^R01|MSG{accessionNumber}|P|2.5.1
PID|1||||Test^Patient||19900101|M
OBR|1||{accessionNumber}|PANEL^Test Panel||20260101100000
{obxSegments}";

            return await _parserService.ParseMessageAsync(content);
        }

        private async Task<HL7Message> CreateTestHL7MessageWithQuantitativeResults(string accessionNumber, dynamic[] tests)
        {
            var obxSegments = string.Join("\n", tests.Select((t, i) =>
                $"OBX|{i + 1}|NM|{t.TestCode}^Test||{t.Result}|{t.Unit}|||||{t.Status}"));

            var content = $@"MSH|^~\&|LAB|TEST|APP|TEST|20260101120000||ORU^R01|MSG{accessionNumber}|P|2.5.1
PID|1||||Test^Patient||19900101|M
OBR|1||{accessionNumber}|PANEL^Test Panel||20260101100000
{obxSegments}";

            return await _parserService.ParseMessageAsync(content);
        }

        private async Task<HL7Message> CreateCompleteHL7Message(
            string? mrn, string firstName, string lastName, DateTime dob,
            string accessionNumber, string labName, string providerName)
        {
            // MSH-3: Sending Application, MSH-4: Sending Facility (Lab Name)
            var content = $@"MSH|^~\&|LABSYS|{labName}|APP|TEST|20260101120000||ORU^R01|MSGCOMPLETE|P|2.5.1
PID|1||{mrn ?? ""}||{lastName}^{firstName}||{dob:yyyyMMdd}|M
OBR|1||{accessionNumber}|HIV^HIV Test||20260101100000||||||||{providerName}
OBX|1|ST|HIV-1^HIV-1 Test||Detected||||||F";

            return await _parserService.ParseMessageAsync(content);
        }

        #endregion
    }
}
