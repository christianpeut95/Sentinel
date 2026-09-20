using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Controllers;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Tests.Controllers;

/// <summary>
/// Direct object-access regression coverage for protected attachment downloads.
/// The file key alone must never be sufficient to retrieve a case, patient, or
/// laboratory attachment.
/// </summary>
public sealed class ProtectedAttachmentAccessTests : IDisposable
{
    private readonly DefaultHttpContext _requestContext = new();
    private readonly ApplicationDbContext _context;
    private readonly Mock<IProtectedFileStorageService> _storage = new();
    private readonly Mock<ICaseAccessService> _caseAccess = new();
    private readonly Mock<IPermissionService> _permissions = new();

    public ProtectedAttachmentAccessTests()
    {
        var accessor = new HttpContextAccessor { HttpContext = _requestContext };
        _context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            accessor);

        _permissions
            .Setup(service => service.HasPermissionAsync(
                "attachment-test-user",
                It.IsAny<PermissionModule>(),
                It.IsAny<PermissionAction>()))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task CaseNote_RequiresHierarchyAccessBeforeOpeningTheStoredFile()
    {
        var caseRecord = await AddAccessibleCaseAsync();
        var note = new Note
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            AttachmentPath = "notes/not-accessible.pdf",
            AttachmentFileName = "not-accessible.pdf",
            Content = "Attachment access test"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        _caseAccess
            .Setup(service => service.CanAccessCaseAsync(caseRecord.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.Note(note.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _storage.Verify(service => service.OpenRead(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CaseNote_UsesCasePermissionAndHierarchyAccessThenServesSafeDownload()
    {
        var caseRecord = await AddAccessibleCaseAsync();
        var note = new Note
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            AttachmentPath = "notes/case-note.pdf",
            AttachmentFileName = "case-note.pdf",
            Content = "Attachment access test"
        };
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        _caseAccess
            .Setup(service => service.CanAccessCaseAsync(caseRecord.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storage.Setup(service => service.OpenRead("notes/case-note.pdf"))
            .Returns(new MemoryStream([1, 2, 3]));
        var controller = CreateController();

        var result = await controller.Note(note.Id, CancellationToken.None);

        var download = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("application/octet-stream", download.ContentType);
        Assert.Equal("case-note.pdf", download.FileDownloadName);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", controller.Response.Headers["X-Content-Type-Options"].ToString());
        _permissions.Verify(service => service.HasPermissionAsync(
            "attachment-test-user", PermissionModule.Case, PermissionAction.View), Times.Once);
    }

    [Fact]
    public async Task LaboratoryAttachment_RequiresLaboratoryPermissionEvenWhenCaseIsAccessible()
    {
        var caseRecord = await AddAccessibleCaseAsync();
        var labResult = new LabResult
        {
            Id = Guid.NewGuid(),
            CaseId = caseRecord.Id,
            AttachmentPath = "lab-results/private-result.pdf",
            AttachmentFileName = "private-result.pdf"
        };
        _context.LabResults.Add(labResult);
        await _context.SaveChangesAsync();

        _caseAccess
            .Setup(service => service.CanAccessCaseAsync(caseRecord.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _permissions
            .Setup(service => service.HasPermissionAsync(
                "attachment-test-user", PermissionModule.Laboratory, PermissionAction.View))
            .ReturnsAsync(false);
        var controller = CreateController();

        var result = await controller.LabResult(labResult.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _storage.Verify(service => service.OpenRead(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PatientNote_UsesThePatientVisibilityFilterBeforeOpeningTheStoredFile()
    {
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Restricted",
            FamilyName = "Patient",
            FriendlyId = "PT-ATTACHMENT"
        };
        var note = new Note
        {
            Id = Guid.NewGuid(),
            PatientId = patient.Id,
            AttachmentPath = "notes/patient-note.pdf",
            AttachmentFileName = "patient-note.pdf",
            Content = "Attachment access test"
        };
        _context.AddRange(patient, note);
        await _context.SaveChangesAsync();
        _requestContext.Items["CaseScopedPatientAccess"] = true;
        var controller = CreateController();

        var result = await controller.Note(note.Id, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        _storage.Verify(service => service.OpenRead(It.IsAny<string>()), Times.Never);
    }

    private async Task<Case> AddAccessibleCaseAsync()
    {
        var disease = new Disease
        {
            Id = Guid.NewGuid(),
            Name = "Attachment access test disease",
            Code = "ATTACHMENT-TEST",
            ExportCode = "ATTACHMENT-TEST",
            PathIds = "attachment-test"
        };
        var patient = new Patient
        {
            Id = Guid.NewGuid(),
            GivenName = "Attachment",
            FamilyName = "Test",
            FriendlyId = $"PT-{Guid.NewGuid():N}"[..18]
        };
        var caseRecord = new Case
        {
            Id = Guid.NewGuid(),
            DiseaseId = disease.Id,
            Disease = disease,
            PatientId = patient.Id,
            Patient = patient,
            FriendlyId = $"CASE-{Guid.NewGuid():N}"[..20],
            Type = CaseType.Case
        };

        _requestContext.Items["AccessibleDiseaseIds"] = new List<Guid> { disease.Id };
        _context.Cases.Add(caseRecord);
        await _context.SaveChangesAsync();
        return caseRecord;
    }

    private ProtectedAttachmentsController CreateController()
    {
        return new ProtectedAttachmentsController(
            _context,
            _storage.Object,
            _caseAccess.Object,
            _permissions.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.NameIdentifier, "attachment-test-user")], "Test"))
                }
            }
        };
    }

    public void Dispose() => _context.Dispose();
}
