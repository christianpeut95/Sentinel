using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
using Sentinel.Services.Telemetry;

namespace Sentinel.Tests.Pages.Patients;

public sealed class PatientCreatePersistenceTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public PatientCreatePersistenceTests()
    {
        var requestContext = new DefaultHttpContext();
        requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = requestContext });
    }

    [Fact]
    public async Task OnPost_PersistsAllowListedContactAddressAndJurisdictionFields()
    {
        var jurisdictionType = new JurisdictionType { Id = 1, FieldNumber = 1, Name = "Health region" };
        _context.States.Add(new State { Id = 1, Code = "SA", Name = "South Australia" });
        _context.JurisdictionTypes.Add(jurisdictionType);
        _context.Jurisdictions.Add(new Jurisdiction
        {
            Id = 1,
            JurisdictionTypeId = 1,
            JurisdictionType = jurisdictionType,
            Name = "Metropolitan region"
        });
        await _context.SaveChangesAsync();

        var model = CreatePage();
        model.Patient = new Patient
        {
            GivenName = "Test",
            FamilyName = "Patient",
            HomePhone = "08 1234 5678",
            MobilePhone = "0400 123 456",
            EmailAddress = "test.patient@example.test",
            AddressLine = "1 Example Street",
            City = "Adelaide",
            StateId = 1,
            PostalCode = "5000",
            Jurisdiction1Id = 1
        };
        model.ConfirmDuplicate = true;

        var result = await model.OnPostAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var patient = await _context.Patients.SingleAsync();
        Assert.Equal("08 1234 5678", patient.HomePhone);
        Assert.Equal("0400 123 456", patient.MobilePhone);
        Assert.Equal("test.patient@example.test", patient.EmailAddress);
        Assert.Equal(1, patient.StateId);
        Assert.Equal(1, patient.Jurisdiction1Id);
    }

    private CreateModel CreatePage()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-user")],
            "test"))
        };
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

        var geocoder = new Mock<IGeocodingService>();
        geocoder
            .Setup(service => service.GeocodeAsync(It.IsAny<string>()))
            .ReturnsAsync((null, null));

        var patientIdGenerator = new Mock<IPatientIdGeneratorService>();
        patientIdGenerator.Setup(service => service.GenerateNextPatientIdAsync()).ReturnsAsync("PT-TEST");

        var audit = new Mock<IAuditService>();
        audit
            .Setup(service => service.LogChangeAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var model = new CreateModel(
            _context,
            geocoder.Object,
            Mock.Of<IPatientDuplicateCheckService>(),
            Mock.Of<IPatientCustomFieldService>(),
            audit.Object,
            patientIdGenerator.Object,
            Mock.Of<ICaseIdGeneratorService>(),
            Mock.Of<IJurisdictionService>(),
            Mock.Of<IServiceProvider>(),
            new ActivityTracker(),
            Mock.Of<IPermissionService>(),
            Mock.Of<IDiseaseAccessService>(),
            NullLogger<CreateModel>.Instance,
            Mock.Of<IApplicationTimeZoneService>())
        {
            PageContext = new PageContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
        };

        return model;
    }

    public void Dispose() => _context.Dispose();
}
