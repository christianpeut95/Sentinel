using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sentinel.Controllers.Api;
using Sentinel.Data;
using Sentinel.Models.Reporting;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Controllers;

/// <summary>
/// Object-level regression coverage for saved reports. A report identifier must
/// not let a report editor copy, move, or remove another user's private report.
/// </summary>
public sealed class ReportOwnershipObjectAccessTests : IDisposable
{
    private const string CurrentUserId = "report-object-access-user";
    private readonly ApplicationDbContext _context = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    private readonly Mock<IReportFolderService> _folders = new();

    [Fact]
    public async Task DuplicateReport_PrivateReportOwnedByAnotherUser_ReturnsNotFoundWithoutCopying()
    {
        await AddPrivateReportAsync(id: 41, ownerId: "other-user");
        var controller = CreateController();

        var result = await controller.DuplicateReport(41);

        Assert.IsType<NotFoundResult>(result);
        Assert.Single(await _context.ReportDefinitions.ToListAsync());
    }

    [Fact]
    public async Task MoveToFolder_PrivateReportOwnedByAnotherUser_ReturnsNotFoundBeforeCheckingTargetFolder()
    {
        await AddPrivateReportAsync(id: 42, ownerId: "other-user");
        var controller = CreateController();

        var result = await controller.MoveToFolder(42, new MoveToFolderRequest { FolderId = 9 });

        Assert.IsType<NotFoundResult>(result);
        _folders.Verify(service => service.CanEditFolderAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        Assert.Equal(2, (await _context.ReportDefinitions.SingleAsync()).FolderId);
    }

    [Fact]
    public async Task RemoveFromFolder_PrivateReportOwnedByAnotherUser_ReturnsNotFoundWithoutChangingFolder()
    {
        await AddPrivateReportAsync(id: 43, ownerId: "other-user");
        var controller = CreateController();

        var result = await controller.RemoveFromFolder(43);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(2, (await _context.ReportDefinitions.SingleAsync()).FolderId);
    }

    [Fact]
    public async Task DuplicateReport_PublicReport_CreatesAPrivateCopyForTheCaller()
    {
        _context.ReportDefinitions.Add(new ReportDefinition
        {
            Id = 44,
            Name = "Public report",
            EntityType = "Case",
            CreatedByUserId = "other-user",
            IsPublic = true,
            Fields = [new ReportField { FieldPath = "FriendlyId", DisplayName = "Case ID", DataType = "String" }]
        });
        await _context.SaveChangesAsync();
        var controller = CreateController();

        var result = await controller.DuplicateReport(44);

        var response = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(response.Value);
        var copy = (await _context.ReportDefinitions
            .Include(report => report.Fields)
            .SingleAsync(report => report.Id != 44));
        Assert.Equal(CurrentUserId, copy.CreatedByUserId);
        Assert.False(copy.IsPublic);
        Assert.Null(copy.FolderId);
        Assert.Single(copy.Fields);
    }

    private async Task AddPrivateReportAsync(int id, string ownerId)
    {
        _context.ReportDefinitions.Add(new ReportDefinition
        {
            Id = id,
            Name = "Private report",
            EntityType = "Case",
            CreatedByUserId = ownerId,
            IsPublic = false,
            FolderId = 2
        });
        await _context.SaveChangesAsync();
    }

    private ReportsApiController CreateController()
    {
        return new ReportsApiController(_context, _folders.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, CurrentUserId),
                        new Claim(ClaimTypes.Name, CurrentUserId)
                    ], "Test"))
                }
            }
        };
    }

    public void Dispose() => _context.Dispose();
}
