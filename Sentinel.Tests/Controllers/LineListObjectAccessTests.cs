using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Controllers;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Tests.Controllers;

/// <summary>
/// Object-level checks for the outbreak line-list routes. Possession of an
/// outbreak identifier must never disclose metadata, views, records, or an
/// export without proving access to that specific outbreak.
/// </summary>
public sealed class LineListObjectAccessTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    private readonly Mock<ILineListService> _lineLists = new();
    private readonly Mock<IOutbreakAccessService> _outbreakAccess = new();

    [Fact]
    public async Task FieldMetadata_ForAnInaccessibleOutbreak_ReturnsNotFoundBeforeServiceCall()
    {
        var controller = CreateController(accessible: false);

        var result = await controller.GetAvailableFields(44);

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.GetAvailableFieldsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Data_ForAnInaccessibleOutbreak_ReturnsNotFoundBeforeExtraction()
    {
        var controller = CreateController(accessible: false);

        var result = await controller.GetLineListData(new LineListDataRequest
        {
            OutbreakId = 44,
            FieldPaths = ["Patient.GivenName"]
        });

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.GetLineListDataAsync(
            It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task SavedViews_ForAnInaccessibleOutbreak_ReturnNotFoundBeforeReadingConfiguration()
    {
        var controller = CreateController(accessible: false);

        var result = await controller.GetConfigurations(44);

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.GetUserConfigurationsAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        _lineLists.Verify(service => service.GetSharedConfigurationsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Export_ForAnInaccessibleOutbreak_ReturnsNotFoundBeforeGeneratingCsv()
    {
        var controller = CreateController(accessible: false);

        var result = await controller.ExportToCsv(new LineListExportRequest
        {
            OutbreakId = 44,
            FieldPaths = ["Patient.GivenName"]
        });

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.ExportToCsvAsync(
            It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfiguration_ForAnInaccessibleOutbreak_ReturnsNotFoundBeforeDeleting()
    {
        await AddConfigurationAsync(id: 91, outbreakId: 44, ownerId: "line-list-test-user");
        var controller = CreateController(accessible: false);

        var result = await controller.DeleteConfiguration(91);

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.DeleteConfigurationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfiguration_BelongingToAnotherUser_ReturnsForbiddenWithoutDeleting()
    {
        await AddConfigurationAsync(id: 92, outbreakId: 44, ownerId: "other-user");
        var controller = CreateController(accessible: true);

        var result = await controller.DeleteConfiguration(92);

        Assert.IsType<ForbidResult>(result);
        _lineLists.Verify(service => service.DeleteConfigurationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetDefaultConfiguration_BelongingToAnotherUser_ReturnsNotFoundWithoutUpdating()
    {
        await AddConfigurationAsync(id: 93, outbreakId: 44, ownerId: "other-user");
        var controller = CreateController(accessible: true);

        var result = await controller.SetDefaultConfiguration(93);

        Assert.IsType<NotFoundResult>(result);
        _lineLists.Verify(service => service.SetDefaultConfigurationAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfiguration_BelongingToAnotherUser_ReturnsForbiddenWithoutSaving()
    {
        await AddConfigurationAsync(id: 94, outbreakId: 44, ownerId: "other-user");
        var controller = CreateController(accessible: true);

        var result = await controller.SaveConfiguration(new SaveLineListConfigurationRequest
        {
            Id = 94,
            OutbreakId = 44,
            Name = "Tampered configuration",
            SelectedFields = "[]",
            SortConfiguration = "[]"
        });

        Assert.IsType<ForbidResult>(result);
        _lineLists.Verify(service => service.SaveConfigurationAsync(It.IsAny<OutbreakLineListConfiguration>()), Times.Never);
    }

    [Fact]
    public async Task Data_UsesTheExplicitResponseContractAndRejectsAnUnconfiguredField()
    {
        _lineLists
            .Setup(service => service.GetAvailableFieldsAsync(44))
            .ReturnsAsync(
            [
                new LineListField
                {
                    FieldPath = "Patient.GivenName",
                    DisplayName = "Given name",
                    Category = "Patient",
                    DataType = "string"
                }
            ]);
        _lineLists
            .Setup(service => service.GetLineListDataAsync(44, It.IsAny<List<string>>(), null, null))
            .ReturnsAsync(
            [
                new LineListDataRow
                {
                    CaseId = Guid.NewGuid(),
                    OutbreakCaseId = 55,
                    Values = new Dictionary<string, object?> { ["Patient.GivenName"] = "Avery" }
                }
            ]);
        var controller = CreateController(accessible: true);

        var allowedResult = await controller.GetLineListData(new LineListDataRequest
        {
            OutbreakId = 44,
            FieldPaths = ["Patient.GivenName"]
        });
        var allowedResponse = Assert.IsType<OkObjectResult>(allowedResult);
        var row = Assert.Single(Assert.IsAssignableFrom<IEnumerable<LineListDataResponse>>(allowedResponse.Value));
        Assert.Equal("Avery", row.Values["Patient.GivenName"]);

        var rejectedResult = await controller.GetLineListData(new LineListDataRequest
        {
            OutbreakId = 44,
            FieldPaths = ["Patient.PasswordHash"]
        });

        Assert.IsType<BadRequestObjectResult>(rejectedResult);
        _lineLists.Verify(
            service => service.GetLineListDataAsync(44, It.Is<List<string>>(fields => fields.Contains("Patient.PasswordHash")), null, null),
            Times.Never);
    }

    [Fact]
    public async Task Configurations_ReturnOnlyTheDedicatedConfigurationResponseContract()
    {
        _lineLists
            .Setup(service => service.GetUserConfigurationsAsync(44, "line-list-test-user"))
            .ReturnsAsync(
            [
                new OutbreakLineListConfiguration
                {
                    Id = 101,
                    OutbreakId = 44,
                    Name = "My view",
                    SelectedFields = "[\"Patient.GivenName\"]",
                    SortConfiguration = "[]",
                    UserId = "line-list-test-user",
                    CreatedByUserId = "line-list-test-user"
                }
            ]);
        _lineLists
            .Setup(service => service.GetSharedConfigurationsAsync(44))
            .ReturnsAsync([]);
        var controller = CreateController(accessible: true);

        var result = await controller.GetConfigurations(44);

        var response = Assert.IsType<OkObjectResult>(result);
        var contract = Assert.IsType<LineListConfigurationsResponse>(response.Value);
        var configuration = Assert.Single(contract.UserConfigurations);
        Assert.Equal("My view", configuration.Name);
        Assert.Null(configuration.CreatedByUser);
    }

    private async Task AddConfigurationAsync(int id, int outbreakId, string ownerId)
    {
        _context.OutbreakLineListConfigurations.Add(new OutbreakLineListConfiguration
        {
            Id = id,
            OutbreakId = outbreakId,
            Name = "Object access test",
            SelectedFields = "[]",
            SortConfiguration = "[]",
            UserId = ownerId,
            CreatedByUserId = ownerId
        });
        await _context.SaveChangesAsync();
    }

    private LineListController CreateController(bool accessible)
    {
        _outbreakAccess
            .Setup(service => service.CanAccessOutbreakAsync(44, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessible);

        return new LineListController(
            _lineLists.Object,
            _outbreakAccess.Object,
            _context,
            NullLogger<LineListController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "line-list-test-user")], "Test"))
                }
            }
        };
    }

    public void Dispose() => _context.Dispose();
}
