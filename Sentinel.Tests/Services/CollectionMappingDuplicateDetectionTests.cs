using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json.Linq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Reporting;
using Sentinel.Services;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services;

public sealed class CollectionMappingDuplicateDetectionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly CollectionMappingService _service;

    public CollectionMappingDuplicateDetectionTests()
    {
        var requestContext = new DefaultHttpContext();
        requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = requestContext });
        _service = new CollectionMappingService(
            _context,
            Mock.Of<IReportFieldMetadataService>(),
            NullLogger<CollectionMappingService>.Instance,
            Mock.Of<IPatientIdGeneratorService>(),
            Mock.Of<ICaseIdGeneratorService>());
    }

    [Fact]
    public async Task FindDuplicatesAsync_UsesDirectMappedFieldsAndReturnsTheExactMatch()
    {
        var expected = CreatePatient("Alice", "Example", new DateTime(1988, 5, 2));
        _context.Patients.AddRange(expected, CreatePatient("Unrelated", "Person", new DateTime(1970, 1, 1)));
        await _context.SaveChangesAsync();

        var matches = await _service.FindDuplicatesAsync(
            "Patient",
            new Dictionary<string, object>
            {
                ["GivenName"] = "Alice",
                ["FamilyName"] = "Example",
                ["DateOfBirth"] = new DateTime(1988, 5, 2)
            },
            ExactPatientMatchingConfig());

        var match = Assert.Single(matches);
        Assert.Equal(expected.Id, match.ExistingEntityId);
        Assert.Equal(1d, match.ConfidenceScore);
        Assert.Equal("Alice", match.MatchedFields["Patient.GivenName"]);
    }

    [Fact]
    public async Task FindDuplicatesAsync_MatchesSurveyDateStringsAgainstDateTimeProperties()
    {
        var expected = CreatePatient("Cookie", "QA 20260827", new DateTime(1990, 1, 1));
        _context.Patients.Add(expected);
        await _context.SaveChangesAsync();

        var matches = await _service.FindDuplicatesAsync(
            "Patient",
            new Dictionary<string, object>
            {
                ["GivenName"] = "  cookie ",
                ["FamilyName"] = "QA   20260827",
                ["DateOfBirth"] = "1990-01-01"
            },
            ExactPatientMatchingConfig());

        var match = Assert.Single(matches);
        Assert.Equal(expected.Id, match.ExistingEntityId);
        Assert.Equal(1d, match.ConfidenceScore);
    }

    [Fact]
    public async Task FindDuplicatesAsync_RejectsNavigationAndUnknownPropertiesInsteadOfBuildingDynamicQueries()
    {
        _context.Patients.Add(CreatePatient("Alice", "Example", new DateTime(1988, 5, 2)));
        await _context.SaveChangesAsync();

        var matches = await _service.FindDuplicatesAsync(
            "Patient",
            new Dictionary<string, object>
            {
                ["Cases.Disease"] = "not a scalar property",
                ["__invalid"] = "attempted query path"
            },
            new MatchingConfig
            {
                EntityType = "Patient",
                MatchOnFields = new List<string> { "Patient.Cases.Disease", "Patient.__invalid" },
                ConfidenceThreshold = 1,
                Strategy = MatchingStrategy.Exact
            });

        Assert.Empty(matches);
    }

    [Fact]
    public async Task FindDuplicatesAsync_BoundsMatchesForAHighVolumeCandidateSet()
    {
        for (var i = 0; i < 260; i++)
        {
            _context.Patients.Add(CreatePatient("Alice", "Example", new DateTime(1988, 5, 2)));
        }
        await _context.SaveChangesAsync();

        var matches = await _service.FindDuplicatesAsync(
            "Patient",
            new Dictionary<string, object>
            {
                ["GivenName"] = "Alice",
                ["FamilyName"] = "Example",
                ["DateOfBirth"] = new DateTime(1988, 5, 2)
            },
            ExactPatientMatchingConfig());

        Assert.Equal(25, matches.Count);
        Assert.All(matches, match => Assert.Equal(1d, match.ConfidenceScore));
    }

    [Fact]
    public async Task ValidateMappingConfigAsync_ValidatesLogicalContactFieldsAgainstCasePersistenceFields()
    {
        var metadata = new Mock<IReportFieldMetadataService>();

        var service = new CollectionMappingService(
            _context,
            metadata.Object,
            NullLogger<CollectionMappingService>.Instance,
            Mock.Of<IPatientIdGeneratorService>(),
            Mock.Of<ICaseIdGeneratorService>());

        var result = await service.ValidateMappingConfigAsync(new CollectionMappingConfig
        {
            SourceQuestionName = "household_contact_matrix",
            TargetEntityType = nameof(Patient),
            RelatedEntities =
            [
                new RelatedEntityConfig
                {
                    EntityType = "Contact",
                    Mappings =
                    [
                        new RelatedEntityMapping
                        {
                            SourceType = "Context",
                            Source = "{{Context.DiseaseId}}",
                            TargetFieldPath = "Contact.DiseaseId"
                        }
                    ]
                },
                new RelatedEntityConfig
                {
                    EntityType = nameof(ExposureEvent),
                    Mappings =
                    [
                        new RelatedEntityMapping
                        {
                            SourceType = "Context",
                            Source = "{{Context.CaseId}}",
                            TargetFieldPath = "ExposureEvent.SourceCaseId"
                        },
                        new RelatedEntityMapping
                        {
                            SourceType = "RelatedEntity",
                            Source = "{RelatedEntity.Contact.Id}",
                            TargetFieldPath = "ExposureEvent.ExposedCaseId"
                        }
                    ]
                }
            ]
        });

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        metadata.Verify(service => service.GetFieldsForEntityAsync(
            nameof(Case),
            It.IsAny<bool>(),
            It.IsAny<FieldUsageContext>()), Times.Never);
        metadata.Verify(service => service.GetFieldsForEntityAsync(
            "Contact",
            It.IsAny<bool>(),
            It.IsAny<FieldUsageContext>()), Times.Never);
    }

    [Fact]
    public async Task GetRelatedEntityTargetFieldsAsync_ExposesOnlyWritableRelationshipFields()
    {
        var contactFields = await _service.GetRelatedEntityTargetFieldsAsync("Contact");
        var exposureFields = await _service.GetRelatedEntityTargetFieldsAsync(nameof(ExposureEvent));

        Assert.Contains(contactFields, field => field.FieldPath == nameof(Case.DiseaseId));
        Assert.Contains(exposureFields, field => field.FieldPath == nameof(ExposureEvent.SourceCaseId));
        Assert.Contains(exposureFields, field => field.FieldPath == nameof(ExposureEvent.ExposedCaseId));

        Assert.DoesNotContain(contactFields, field => field.FieldPath == nameof(Case.Id));
        Assert.DoesNotContain(exposureFields, field => field.FieldPath == nameof(ExposureEvent.CreatedByUserId));
    }

    [Fact]
    public async Task ValidateMappingConfigAsync_RejectsServerManagedRelatedEntityFields()
    {
        var result = await _service.ValidateMappingConfigAsync(new CollectionMappingConfig
        {
            SourceQuestionName = "household_contact_matrix",
            TargetEntityType = nameof(Patient),
            RelatedEntities =
            [
                new RelatedEntityConfig
                {
                    EntityType = nameof(ExposureEvent),
                    Mappings =
                    [
                        new RelatedEntityMapping
                        {
                            SourceType = "Constant",
                            Source = "arbitrary-user-id",
                            TargetFieldPath = "ExposureEvent.CreatedByUserId"
                        }
                    ]
                }
            ]
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("CreatedByUserId", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ProcessCollectionWithContextAsync_CreatesContactAndExposureUsingDirectRelationshipKeys()
    {
        var metadata = new Mock<IReportFieldMetadataService>();
        metadata
            .Setup(service => service.GetFieldsForEntityAsync(
                nameof(Patient),
                It.IsAny<bool>(),
                It.IsAny<FieldUsageContext>()))
            .ReturnsAsync(new List<ReportFieldMetadata>
            {
                new() { FieldPath = nameof(Patient.GivenName), DataType = "String" },
                new() { FieldPath = nameof(Patient.FamilyName), DataType = "String" },
                new() { FieldPath = nameof(Patient.DateOfBirth), DataType = "DateTime" }
            });

        var patientIds = new Mock<IPatientIdGeneratorService>();
        patientIds.Setup(service => service.GenerateNextPatientIdAsync()).ReturnsAsync("PT-TEST-001");
        var caseIds = new Mock<ICaseIdGeneratorService>();
        caseIds.Setup(service => service.GenerateNextCaseIdAsync()).ReturnsAsync("C-TEST-001");

        var service = new CollectionMappingService(
            _context,
            metadata.Object,
            NullLogger<CollectionMappingService>.Instance,
            patientIds.Object,
            caseIds.Object);

        var sourceCaseId = Guid.NewGuid();
        var diseaseId = Guid.NewGuid();
        var result = await service.ProcessCollectionWithContextAsync(
            Guid.NewGuid(),
            "household_contact_matrix",
            new JArray(new JObject
            {
                ["firstName"] = "Cookie",
                ["lastName"] = "QA",
                ["dateOfBirth"] = "1990-01-01"
            }),
            new CollectionMappingConfig
            {
                SourceQuestionName = "household_contact_matrix",
                TargetEntityType = nameof(Patient),
                RowMappings =
                [
                    new() { SourceColumn = "firstName", TargetFieldPath = "Patient.GivenName", Required = true },
                    new() { SourceColumn = "lastName", TargetFieldPath = "Patient.FamilyName", Required = true },
                    new() { SourceColumn = "dateOfBirth", TargetFieldPath = "Patient.DateOfBirth" }
                ],
                RelatedEntities =
                [
                    new RelatedEntityConfig
                    {
                        EntityType = "Contact",
                        CreationOrder = 1,
                        Mappings =
                        [
                            new()
                            {
                                SourceType = "Context",
                                Source = "{{Context.DiseaseId}}",
                                TargetFieldPath = "Contact.DiseaseId",
                                Required = true
                            }
                        ]
                    },
                    new RelatedEntityConfig
                    {
                        EntityType = nameof(ExposureEvent),
                        CreationOrder = 2,
                        Mappings =
                        [
                            new()
                            {
                                SourceType = "Context",
                                Source = "{{Context.CaseId}}",
                                TargetFieldPath = "ExposureEvent.SourceCaseId",
                                Required = true
                            },
                            new()
                            {
                                SourceType = "RelatedEntity",
                                Source = "{RelatedEntity.Contact.Id}",
                                TargetFieldPath = "ExposureEvent.ExposedCaseId",
                                Required = true
                            },
                            new()
                            {
                                SourceType = "Constant",
                                Source = "Contact",
                                TargetFieldPath = "ExposureEvent.ExposureType",
                                Required = true
                            },
                            new()
                            {
                                SourceType = "Constant",
                                Source = "2026-09-24",
                                TargetFieldPath = "ExposureEvent.ExposureStartDate",
                                Required = true
                            }
                        ]
                    }
                ]
            },
            new SurveySubmissionContext
            {
                CaseId = sourceCaseId,
                DiseaseId = diseaseId,
                MappingAction = MappingAction.AutoSave,
                SubmittedDate = DateTime.UtcNow
            });

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);

        var patient = Assert.Single(_context.Patients.IgnoreQueryFilters());
        var contact = Assert.Single(_context.Cases.IgnoreQueryFilters());
        var exposure = Assert.Single(_context.ExposureEvents.IgnoreQueryFilters());

        Assert.Equal(CaseType.Contact, contact.Type);
        Assert.Equal(patient.Id, contact.PatientId);
        Assert.Equal(diseaseId, contact.DiseaseId);
        Assert.Equal(sourceCaseId, exposure.SourceCaseId);
        Assert.Equal(contact.Id, exposure.ExposedCaseId);
        Assert.Equal(ExposureType.Contact, exposure.ExposureType);
    }

    [Fact]
    public async Task ProcessCollectionWithContextAsync_ReprocessingResolvedDuplicateLinksExistingPatientAndCreatesRelatedEntities()
    {
        var metadata = PatientFieldMetadataService();
        var patientIds = new Mock<IPatientIdGeneratorService>();
        var caseIds = new Mock<ICaseIdGeneratorService>();
        caseIds.Setup(service => service.GenerateNextCaseIdAsync()).ReturnsAsync("C-TEST-002");

        var service = new CollectionMappingService(
            _context,
            metadata.Object,
            NullLogger<CollectionMappingService>.Instance,
            patientIds.Object,
            caseIds.Object);

        var existingPatient = CreatePatient("Cookie", "QA", new DateTime(1990, 1, 1));
        _context.Patients.Add(existingPatient);
        await _context.SaveChangesAsync();

        var sourceCaseId = Guid.NewGuid();
        var diseaseId = Guid.NewGuid();
        var result = await service.ProcessCollectionWithContextAsync(
            Guid.NewGuid(),
            "household_contact_matrix",
            new JArray(new JObject
            {
                ["firstName"] = "Cookie",
                ["lastName"] = "QA",
                ["dateOfBirth"] = "1990-01-01"
            }),
            HouseholdContactConfig(),
            new SurveySubmissionContext
            {
                CaseId = sourceCaseId,
                DiseaseId = diseaseId,
                PatientId = existingPatient.Id,
                MappingAction = MappingAction.QueueForReview,
                SubmittedDate = DateTime.UtcNow,
                AdditionalData = new Dictionary<string, object>
                {
                    ["ResolvedFromDuplicate"] = true,
                    ["PatientAlreadyExists"] = true
                }
            });

        Assert.True(result.Success, string.Join("; ", result.Errors));
        Assert.Empty(result.Errors);
        Assert.Single(_context.Patients.IgnoreQueryFilters());

        var contact = Assert.Single(_context.Cases.IgnoreQueryFilters());
        var exposure = Assert.Single(_context.ExposureEvents.IgnoreQueryFilters());

        Assert.Equal(existingPatient.Id, contact.PatientId);
        Assert.Equal(diseaseId, contact.DiseaseId);
        Assert.Equal(sourceCaseId, exposure.SourceCaseId);
        Assert.Equal(contact.Id, exposure.ExposedCaseId);
    }

    public void Dispose() => _context.Dispose();

    private static MatchingConfig ExactPatientMatchingConfig() => new()
    {
        EntityType = "Patient",
        MatchOnFields = new List<string>
        {
            "Patient.GivenName",
            "Patient.FamilyName",
            "Patient.DateOfBirth"
        },
        ConfidenceThreshold = 1,
        Strategy = MatchingStrategy.Exact
    };

    private static Mock<IReportFieldMetadataService> PatientFieldMetadataService()
    {
        var metadata = new Mock<IReportFieldMetadataService>();
        metadata
            .Setup(service => service.GetFieldsForEntityAsync(
                nameof(Patient),
                It.IsAny<bool>(),
                It.IsAny<FieldUsageContext>()))
            .ReturnsAsync(new List<ReportFieldMetadata>
            {
                new() { FieldPath = nameof(Patient.GivenName), DataType = "String" },
                new() { FieldPath = nameof(Patient.FamilyName), DataType = "String" },
                new() { FieldPath = nameof(Patient.DateOfBirth), DataType = "DateTime" }
            });
        return metadata;
    }

    private static CollectionMappingConfig HouseholdContactConfig() => new()
    {
        SourceQuestionName = "household_contact_matrix",
        TargetEntityType = nameof(Patient),
        RowMappings =
        [
            new() { SourceColumn = "firstName", TargetFieldPath = "Patient.GivenName", Required = true },
            new() { SourceColumn = "lastName", TargetFieldPath = "Patient.FamilyName", Required = true },
            new() { SourceColumn = "dateOfBirth", TargetFieldPath = "Patient.DateOfBirth" }
        ],
        RelatedEntities =
        [
            new RelatedEntityConfig
            {
                EntityType = "Contact",
                CreationOrder = 1,
                Mappings =
                [
                    new()
                    {
                        SourceType = "Context",
                        Source = "{{Context.DiseaseId}}",
                        TargetFieldPath = "Contact.DiseaseId",
                        Required = true
                    }
                ]
            },
            new RelatedEntityConfig
            {
                EntityType = nameof(ExposureEvent),
                CreationOrder = 2,
                Mappings =
                [
                    new()
                    {
                        SourceType = "Context",
                        Source = "{{Context.CaseId}}",
                        TargetFieldPath = "ExposureEvent.SourceCaseId",
                        Required = true
                    },
                    new()
                    {
                        SourceType = "RelatedEntity",
                        Source = "{RelatedEntity.Contact.Id}",
                        TargetFieldPath = "ExposureEvent.ExposedCaseId",
                        Required = true
                    },
                    new()
                    {
                        SourceType = "Constant",
                        Source = "Contact",
                        TargetFieldPath = "ExposureEvent.ExposureType",
                        Required = true
                    },
                    new()
                    {
                        SourceType = "Constant",
                        Source = "2026-09-24",
                        TargetFieldPath = "ExposureEvent.ExposureStartDate",
                        Required = true
                    }
                ]
            }
        ]
    };

    private static ReportFieldMetadata Field(string fieldPath) => new()
    {
        FieldPath = fieldPath,
        DataType = "Guid"
    };

    private static Patient CreatePatient(string givenName, string familyName, DateTime dateOfBirth) => new()
    {
        Id = Guid.NewGuid(),
        FriendlyId = $"PT-{Guid.NewGuid():N}"[..20],
        GivenName = givenName,
        FamilyName = familyName,
        DateOfBirth = dateOfBirth
    };
}
