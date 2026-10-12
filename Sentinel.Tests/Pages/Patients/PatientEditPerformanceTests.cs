using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Pages.Patients;
using Sentinel.Services;

namespace Sentinel.Tests.Pages.Patients;

public sealed class PatientEditPerformanceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IJurisdictionService> _jurisdictions = new();
    private readonly Mock<IPatientCustomFieldService> _customFields = new();

    public PatientEditPerformanceTests()
    {
        var requestContext = new DefaultHttpContext();
        requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = requestContext });

        _jurisdictions
            .Setup(service => service.GetActiveJurisdictionTypesAsync())
            .ReturnsAsync(new List<JurisdictionType>());
        _customFields
            .Setup(service => service.GetCreateEditFieldsAsync())
            .ReturnsAsync(new List<CustomFieldDefinition>());
        _customFields
            .Setup(service => service.GetPatientFieldValuesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new Dictionary<int, string?>());
    }

    [Fact]
    public async Task OnGet_LoadsJurisdictionMetadataWithoutLoadingAllJurisdictions()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Test",
            FamilyName = "Patient",
            FriendlyId = "PT-TEST"
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();

        var result = await CreatePage().OnGetAsync(patient.Id);

        Assert.IsType<PageResult>(result);
        _jurisdictions.Verify(service => service.GetActiveJurisdictionTypesAsync(), Times.Once);
        _jurisdictions.Verify(service => service.GetGroupedJurisdictionsAsync(), Times.Never);
    }

    private EditModel CreatePage()
    {
        var httpContext = new DefaultHttpContext();
        var pageContext = new PageContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
        };

        return new EditModel(
            _context,
            Mock.Of<IGeocodingService>(),
            _customFields.Object,
            Mock.Of<IAuditService>(),
            Mock.Of<IPatientIdGeneratorService>(),
            _jurisdictions.Object,
            Mock.Of<IServiceProvider>(),
            Mock.Of<IPatientAddressService>(),
            NullLogger<EditModel>.Instance)
        {
            PageContext = pageContext
        };
    }

    public void Dispose() => _context.Dispose();
}
