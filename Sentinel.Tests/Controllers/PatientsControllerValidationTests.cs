using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Controllers;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Tests.Controllers;

public sealed class PatientsControllerValidationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPatientDuplicateCheckService> _duplicates = new();
    private readonly DefaultHttpContext _requestContext = new();

    public PatientsControllerValidationTests()
    {
        _requestContext.Items["CaseScopedPatientAccess"] = false;
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new HttpContextAccessor { HttpContext = _requestContext });
    }

    [Fact]
    public async Task UpdatePatient_RejectsUnknownReferenceWithoutChangingPatient()
    {
        var patient = await AddPatientAsync();

        var result = await CreateController().UpdatePatient(patient.Id, new UpdatePatientRequest
        {
            Id = patient.Id,
            GivenName = "Changed",
            FamilyName = patient.FamilyName,
            GenderId = 999999
        });

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await _context.Patients.SingleAsync();
        Assert.Equal("Original", persisted.GivenName);
        Assert.Null(persisted.GenderId);
    }

    [Fact]
    public async Task UpdatePatient_RejectsFutureDateOfBirthWithoutChangingPatient()
    {
        var patient = await AddPatientAsync();

        var result = await CreateController().UpdatePatient(patient.Id, new UpdatePatientRequest
        {
            Id = patient.Id,
            GivenName = patient.GivenName,
            FamilyName = patient.FamilyName,
            DateOfBirth = DateTime.UtcNow.Date.AddDays(1)
        });

        Assert.IsType<BadRequestObjectResult>(result);
        var persisted = await _context.Patients.SingleAsync();
        Assert.Null(persisted.DateOfBirth);
    }

    [Fact]
    public async Task UpdatePatient_AllowsActiveReferenceAndOnlyUpdatesRequestOwnedFields()
    {
        var patient = await AddPatientAsync();
        _context.Genders.Add(new Gender { Id = 12, Name = "Test gender", IsActive = true });
        await _context.SaveChangesAsync();

        var result = await CreateController().UpdatePatient(patient.Id, new UpdatePatientRequest
        {
            Id = patient.Id,
            GivenName = "Updated",
            FamilyName = "Patient",
            GenderId = 12,
            DateOfBirth = new DateTime(1990, 1, 1)
        });

        Assert.IsType<OkObjectResult>(result);
        var persisted = await _context.Patients.SingleAsync();
        Assert.Equal("Updated", persisted.GivenName);
        Assert.Equal(12, persisted.GenderId);
        Assert.Equal(new DateTime(1990, 1, 1), persisted.DateOfBirth);
        Assert.Equal("PT-ORIGINAL", persisted.FriendlyId);
        Assert.False(persisted.IsDeleted);
    }

    [Fact]
    public async Task CaseScopedPatientApi_HidesAnInaccessiblePatientForReadUpdateAndDuplicateChecks()
    {
        var patient = await AddPatientAsync();
        await AddCaseAsync(patient, "Restricted patient test disease");

        _requestContext.Items["CaseScopedPatientAccess"] = true;
        _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid>();
        var controller = CreateController();

        var getResult = await controller.GetById(patient.Id);
        var updateResult = await controller.UpdatePatient(patient.Id, new UpdatePatientRequest
        {
            Id = patient.Id,
            GivenName = "Tampered",
            FamilyName = patient.FamilyName
        });
        var duplicatesResult = await controller.GetDuplicates(patient.Id);

        Assert.IsType<NotFoundResult>(getResult);
        Assert.IsType<NotFoundResult>(updateResult);
        Assert.IsType<NotFoundResult>(duplicatesResult);
        Assert.Equal("Original", (await _context.Patients.IgnoreQueryFilters().SingleAsync()).GivenName);
        _duplicates.Verify(service => service.FindPotentialDuplicatesAsync(It.IsAny<Patient>()), Times.Never);
    }

    private async Task<Patient> AddPatientAsync()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Original",
            FamilyName = "Patient",
            FriendlyId = "PT-ORIGINAL"
        };
        _context.Patients.Add(patient);
        await _context.SaveChangesAsync();
        return patient;
    }

    private async Task AddCaseAsync(Patient patient, string diseaseName)
    {
        var disease = new Disease
        {
            Id = Guid.NewGuid(),
            Name = diseaseName,
            Code = "RESTRICTED-PATIENT-TEST",
            ExportCode = "RESTRICTED-PATIENT-TEST",
            PathIds = "restricted-patient-test"
        };
        _context.Cases.Add(new Case
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            Patient = patient,
            DiseaseId = disease.Id,
            Disease = disease,
            FriendlyId = "CASE-RESTRICTED-PATIENT-TEST",
            Type = CaseType.Case
        });
        await _context.SaveChangesAsync();
    }

    private PatientsController CreateController() => new(
        _context,
        _duplicates.Object,
        NullLogger<PatientsController>.Instance);

    public void Dispose() => _context.Dispose();
}
