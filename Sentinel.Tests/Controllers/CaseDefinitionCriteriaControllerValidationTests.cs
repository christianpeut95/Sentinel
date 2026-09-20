using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sentinel.Controllers.Api;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Tests.Controllers;

/// <summary>
/// Exercises the server-side boundary used by the criteria builder. These
/// assertions deliberately invoke the controller directly so that a forged
/// request cannot rely on client-side picker limits or JavaScript ordering.
/// </summary>
public sealed class CaseDefinitionCriteriaControllerValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public CaseDefinitionCriteriaControllerValidationTests()
    {
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    [Fact]
    public async Task AddCaseFieldCriterion_RejectsAParentFromAnotherDefinitionWithoutPersisting()
    {
        var (firstDefinition, secondDefinition) = await AddDefinitionsAsync();
        var foreignParent = await AddCriterionAsync(secondDefinition.Id, null);
        var controller = CreateController();

        var result = await controller.AddCaseFieldCriterion(firstDefinition.Id, new CaseFieldCriterionInput
        {
            LogicalOperator = LogicalOperator.AND,
            GroupNumber = 1,
            ParentCriteriaId = foreignParent.Id,
            FieldPath = "OnsetDate",
            Operator = ComparisonOperator.Equals,
            Value = "2026-09-19",
            DisplayText = "Forged cross-definition parent"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Single(await _context.CaseDefinitionCriteria.ToListAsync());
    }

    [Fact]
    public async Task MoveToParent_RejectsMovingARootBelowItsGrandchild()
    {
        var (definition, _) = await AddDefinitionsAsync();
        var root = await AddCriterionAsync(definition.Id, null);
        var child = await AddCriterionAsync(definition.Id, root.Id);
        var grandchild = await AddCriterionAsync(definition.Id, child.Id);
        var controller = CreateController();

        var result = await controller.MoveToParent(
            definition.Id,
            root.Id,
            new MoveToParentInput { ParentCriteriaId = grandchild.Id });

        Assert.IsType<BadRequestObjectResult>(result);
        var persistedRoot = await _context.CaseDefinitionCriteria.SingleAsync(c => c.Id == root.Id);
        Assert.Null(persistedRoot.ParentCriteriaId);
    }

    [Fact]
    public async Task UpdateCaseFieldCriterion_RejectsReflectionPathAndLeavesPersistedCriterionUntouched()
    {
        var (definition, _) = await AddDefinitionsAsync();
        var criterion = await AddCriterionAsync(definition.Id, null, "OnsetDate");
        var controller = CreateController();

        var result = await controller.UpdateCaseFieldCriterion(definition.Id, criterion.Id, new CaseFieldCriterionInput
        {
            LogicalOperator = LogicalOperator.OR,
            GroupNumber = 1,
            FieldPath = "CaseDefinition.Criteria",
            Operator = ComparisonOperator.Equals,
            Value = "forged",
            DisplayText = "Forged reflection path"
        });

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await _context.CaseDefinitionCriteria.SingleAsync(c => c.Id == criterion.Id);
        Assert.Equal("OnsetDate", persisted.FieldPath);
    }

    [Fact]
    public async Task ReorderCriterion_RejectsUnknownDirectionWithoutChangingDisplayOrder()
    {
        var (definition, _) = await AddDefinitionsAsync();
        var criterion = await AddCriterionAsync(definition.Id, null);
        var controller = CreateController();

        var result = await controller.ReorderCriterion(
            definition.Id,
            criterion.Id,
            new ReorderInput { Direction = "sideways" });

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, (await _context.CaseDefinitionCriteria.SingleAsync()).DisplayOrder);
    }

    [Fact]
    public async Task AddCaseFieldCriterion_RejectsMutationOfAnActiveDefinition()
    {
        var (definition, _) = await AddDefinitionsAsync();
        definition.Status = CaseDefinitionStatus.Current;
        await _context.SaveChangesAsync();
        var controller = CreateController();

        var result = await controller.AddCaseFieldCriterion(definition.Id, new CaseFieldCriterionInput
        {
            LogicalOperator = LogicalOperator.AND,
            GroupNumber = 1,
            FieldPath = "OnsetDate",
            Operator = ComparisonOperator.Equals,
            Value = "2026-09-19",
            DisplayText = "Attempted active-definition edit"
        });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Empty(await _context.CaseDefinitionCriteria.ToListAsync());
    }

    [Fact]
    public void CriteriaInputContract_AcceptsTheExistingStringEnumPayloadWithoutRelaxingTheType()
    {
        const string payload = """
            {"logicalOperator":"AND","groupNumber":1,"customFieldId":7,"operator":"Equals","value":"Yes","displayText":"Example"}
            """;

        var input = JsonSerializer.Deserialize<CustomFieldCriterionInput>(payload, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(input);
        Assert.Equal(LogicalOperator.AND, input.LogicalOperator);
        Assert.Equal(ComparisonOperator.Equals, input.Operator);
    }

    [Fact]
    public void CriteriaRoutes_HaveNoLegacyRawJsonEndpointInProgram()
    {
        var root = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(root, "Sentinel", "Program.cs"));
        var controller = File.ReadAllText(Path.Combine(
            root,
            "Sentinel",
            "Controllers",
            "Api",
            "CaseDefinitionCriteriaController.cs"));

        Assert.DoesNotContain("API endpoint to reorder case definition criteria", program, StringComparison.Ordinal);
        Assert.DoesNotContain("JsonElement>(body)", program, StringComparison.Ordinal);
        Assert.Contains("UpdateClinicalCriterion", controller, StringComparison.Ordinal);
        Assert.Contains("UpdateCustomFieldCriterion", controller, StringComparison.Ordinal);
        Assert.Contains("UpdateCaseFieldCriterion", controller, StringComparison.Ordinal);
        Assert.Contains("ValidateCriterionPlacementAsync", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void CriteriaBuilderHasNoSecondLegacyMutationBoundary()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "Sentinel",
            "Pages",
            "Settings",
            "CaseDefinitions",
            "BuildCriteria.cshtml.cs"));

        foreach (var obsoleteHandler in new[]
                 {
                     "OnPostAddLabCriterionAsync",
                     "OnPostAddClinicalCriterionAsync",
                     "OnPostAddCustomFieldCriterionAsync",
                     "OnPostAddCaseFieldCriterionAsync",
                     "OnPostDeleteCriterionAsync"
                 })
        {
            Assert.DoesNotContain(obsoleteHandler, source, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("class LabCriterionInput", source, StringComparison.Ordinal);
        Assert.DoesNotContain("class ClinicalCriterionInput", source, StringComparison.Ordinal);
        Assert.DoesNotContain("class CustomFieldCriterionInput", source, StringComparison.Ordinal);
        Assert.DoesNotContain("class CaseFieldCriterionInput", source, StringComparison.Ordinal);
        Assert.Contains("Definition.Status != CaseDefinitionStatus.Draft", source, StringComparison.Ordinal);
    }

    private async Task<(CaseDefinition first, CaseDefinition second)> AddDefinitionsAsync()
    {
        var first = new CaseDefinition
        {
            Name = "First test definition",
            DiseaseId = Guid.NewGuid(),
            ConfirmationStatusId = 1
        };
        var second = new CaseDefinition
        {
            Name = "Second test definition",
            DiseaseId = Guid.NewGuid(),
            ConfirmationStatusId = 1
        };
        _context.CaseDefinitions.AddRange(first, second);
        await _context.SaveChangesAsync();
        return (first, second);
    }

    private async Task<CaseDefinitionCriteria> AddCriterionAsync(
        int definitionId,
        int? parentId,
        string fieldPath = "OnsetDate")
    {
        var criterion = new CaseDefinitionCriteria
        {
            CaseDefinitionId = definitionId,
            ParentCriteriaId = parentId,
            CriterionType = CriterionType.Demographic,
            LogicalOperator = LogicalOperator.AND,
            GroupNumber = 1,
            FieldPath = fieldPath,
            Operator = ComparisonOperator.Equals,
            ValueJson = "{}",
            DisplayOrder = 0
        };
        _context.CaseDefinitionCriteria.Add(criterion);
        await _context.SaveChangesAsync();
        return criterion;
    }

    private CaseDefinitionCriteriaController CreateController() => new(
        _context,
        NullLogger<CaseDefinitionCriteriaController>.Instance);

    private static string FindRepositoryRoot()
    {
        foreach (var candidate in new[]
                 {
                     new DirectoryInfo(Directory.GetCurrentDirectory()),
                     new DirectoryInfo(AppContext.BaseDirectory)
                 })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Sentinel.Tests")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }

    public void Dispose() => _context.Dispose();
}
