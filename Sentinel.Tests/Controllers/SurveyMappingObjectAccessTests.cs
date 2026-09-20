using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sentinel.Controllers.Api;
using Sentinel.Data;
using Sentinel.Services;

namespace Sentinel.Tests.Controllers;

/// <summary>
/// A mapping preview is not merely metadata: when given a case ID it can read
/// current case and patient values. The case must be authorised before any
/// mapping lookup or preview processing begins.
/// </summary>
public sealed class SurveyMappingObjectAccessTests : IDisposable
{
    private readonly ApplicationDbContext _context = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
    private readonly Mock<ISurveyMappingService> _mappings = new();
    private readonly Mock<ICaseAccessService> _caseAccess = new();

    [Fact]
    public async Task PreviewMappings_InaccessibleCaseId_ReturnsNotFoundBeforeReadingAnyData()
    {
        var inaccessibleCaseId = Guid.NewGuid();
        _caseAccess
            .Setup(service => service.CanAccessCaseAsync(inaccessibleCaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = new SurveyMappingApiController(_context, _mappings.Object, _caseAccess.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.PreviewMappings(new PreviewMappingsRequest
        {
            CaseId = inaccessibleCaseId,
            SurveyResponses = new Dictionary<string, object> { ["onset"] = "2026-09-20" }
        });

        Assert.IsType<NotFoundResult>(result);
        _mappings.Verify(service => service.GetActiveMappingsAsync(
            It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
        _mappings.Verify(service => service.PreviewMappingsAsync(
            It.IsAny<Guid?>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<List<Sentinel.Models.SurveyFieldMapping>>()), Times.Never);
    }

    public void Dispose() => _context.Dispose();
}
