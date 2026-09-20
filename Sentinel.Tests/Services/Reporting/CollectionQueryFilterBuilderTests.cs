using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq.Dynamic.Core;
using Sentinel.Data;
using Sentinel.DTOs;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services.Reporting;

public sealed class CollectionQueryFilterBuilderTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IReportFieldMetadataService> _fieldMetadata = new();
    private readonly CollectionQueryFilterBuilder _builder;

    public CollectionQueryFilterBuilderTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _fieldMetadata
            .Setup(service => service.GetFieldsForEntityAsync("Case", false, FieldUsageContext.General))
            .ReturnsAsync([LabResultsCollection]);

        _builder = new CollectionQueryFilterBuilder(
            _context,
            new DynamicDateResolver(),
            _fieldMetadata.Object);
    }

    [Fact]
    public async Task BuildCollectionFilterClauseAsync_ValidKnownSubField_BuildsClause()
    {
        var query = new CollectionQueryDto
        {
            CollectionName = "LabResults",
            Operation = "HasAny",
            SubFilters =
            [
                new CollectionSubFilter
                {
                    Field = "QualitativeResultText",
                    Operator = "Contains",
                    Value = "Salmonella"
                }
            ]
        };

        var clause = await _builder.BuildCollectionFilterClauseAsync(query, "Case");

        Assert.Equal("LabResults.Any(x => x.CaseId == Id && (x.QualitativeResultText != null && x.QualitativeResultText.Contains(\"Salmonella\")))", clause);
        Assert.Equal("String", query.SubFilters[0].DataType);
    }

    [Fact]
    public async Task BuildCollectionFilterClauseAsync_UnknownSubField_RejectsDefinition()
    {
        var query = new CollectionQueryDto
        {
            CollectionName = "LabResults",
            Operation = "HasAny",
            SubFilters =
            [
                new CollectionSubFilter { Field = "Id.ToString()", Operator = "Equals", Value = "x" }
            ]
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _builder.BuildCollectionFilterClauseAsync(query, "Case"));
    }

    [Fact]
    public async Task BuildCollectionFilterClauseAsync_NonNumericValueForNumericSubField_RejectsDefinition()
    {
        var query = new CollectionQueryDto
        {
            CollectionName = "LabResults",
            Operation = "HasAny",
            SubFilters =
            [
                new CollectionSubFilter
                {
                    Field = "QuantitativeValue",
                    Operator = "GreaterThan",
                    Value = "1 || true"
                }
            ]
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _builder.BuildCollectionFilterClauseAsync(query, "Case"));
    }

    [Fact]
    public async Task BuildCollectionFilterClauseAsync_QuotedBooleanPayload_RemainsALiteralAndDoesNotBroadenResults()
    {
        var query = new CollectionQueryDto
        {
            CollectionName = "LabResults",
            Operation = "HasAny",
            SubFilters =
            [
                new CollectionSubFilter
                {
                    Field = "FriendlyId",
                    Operator = "Contains",
                    Value = "\" ) || true || (x.FriendlyId.Contains(\""
                }
            ]
        };

        var clause = await _builder.BuildCollectionFilterClauseAsync(query, "Case");

        var caseId = Guid.NewGuid();
        var nonMatchingCase = new Sentinel.Models.Case
        {
            Id = caseId,
            LabResults =
            [
                new Sentinel.Models.LabResult
                {
                    CaseId = caseId,
                    FriendlyId = "LAB-EXPECTED-NONMATCH"
                }
            ]
        };

        var config = new ParsingConfig
        {
            DisallowNewKeyword = true,
            AllowNewToEvaluateAnyType = false,
            ResolveTypesBySimpleName = false,
            AllowEqualsAndToStringMethodsOnObject = false,
            RestrictOrderByToPropertyOrField = true
        };

        var matching = new[] { nonMatchingCase }
            .AsQueryable()
            .Where(config, clause!)
            .ToList();

        Assert.Empty(matching);
        Assert.Contains("\\\"", clause, StringComparison.Ordinal);
    }

    public void Dispose() => _context.Dispose();

    private static ReportFieldMetadata LabResultsCollection => new()
    {
        EntityType = "Case",
        FieldPath = "LabResults",
        IsCollection = true,
        IsFilterable = true,
        CollectionSubFieldsMetadata =
        [
            new CollectionSubFieldMetadata { FieldPath = "QualitativeResultText", DataType = "String" },
            new CollectionSubFieldMetadata { FieldPath = "FriendlyId", DataType = "String" },
            new CollectionSubFieldMetadata { FieldPath = "QuantitativeValue", DataType = "Decimal" }
        ]
    };
}
