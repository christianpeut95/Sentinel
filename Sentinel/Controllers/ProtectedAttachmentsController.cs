using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;
using System.Security.Claims;

namespace Sentinel.Controllers;

/// <summary>
/// Authorised download endpoint for case and patient attachments. Sensitive
/// files must never be linked directly from wwwroot.
/// </summary>
[Authorize]
[Route("attachments")]
public sealed class ProtectedAttachmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IProtectedFileStorageService _fileStorage;
    private readonly ICaseAccessService _caseAccessService;
    private readonly IPermissionService _permissionService;

    public ProtectedAttachmentsController(
        ApplicationDbContext context,
        IProtectedFileStorageService fileStorage,
        ICaseAccessService caseAccessService,
        IPermissionService permissionService)
    {
        _context = context;
        _fileStorage = fileStorage;
        _caseAccessService = caseAccessService;
        _permissionService = permissionService;
    }

    [HttpGet("notes/{id:guid}")]
    public async Task<IActionResult> Note(Guid id, CancellationToken cancellationToken)
    {
        var note = await _context.Notes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        if (note == null || string.IsNullOrWhiteSpace(note.AttachmentPath) ||
            !await CanAccessNoteAsync(note, cancellationToken))
        {
            return NotFound();
        }

        return Download(note.AttachmentPath, note.AttachmentFileName, ProtectedFileStorageService.NotesCategory);
    }

    [HttpGet("lab-results/{id:guid}")]
    public async Task<IActionResult> LabResult(Guid id, CancellationToken cancellationToken)
    {
        var labResult = await _context.LabResults
            .AsNoTracking()
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

        if (labResult?.CaseId is not Guid caseId ||
            string.IsNullOrWhiteSpace(labResult.AttachmentPath) ||
            !await HasPermissionAsync(PermissionModule.Laboratory, PermissionAction.View) ||
            !await _caseAccessService.CanAccessCaseAsync(caseId, cancellationToken))
        {
            return NotFound();
        }

        return Download(labResult.AttachmentPath, labResult.AttachmentFileName, ProtectedFileStorageService.LabResultsCategory);
    }

    private async Task<bool> CanAccessNoteAsync(Note note, CancellationToken cancellationToken)
    {
        if (note.CaseId is Guid caseId)
        {
            return await HasPermissionAsync(PermissionModule.Case, PermissionAction.View) &&
                   await _caseAccessService.CanAccessCaseAsync(caseId, cancellationToken);
        }

        if (note.PatientId is not Guid patientId ||
            !await HasPermissionAsync(PermissionModule.Patient, PermissionAction.View))
        {
            return false;
        }

        // A patient-only note remains available to a Patient.View user when the
        // patient has no cases. If cases exist, at least one must be visible in
        // the current disease-access scope before disclosing the attachment.
        var hasAnyCases = await _context.Cases
            .IgnoreQueryFilters()
            .AnyAsync(c => c.PatientId == patientId, cancellationToken);

        return !hasAnyCases || await _context.Cases
            .AnyAsync(c => c.PatientId == patientId, cancellationToken);
    }

    private async Task<bool> HasPermissionAsync(PermissionModule module, PermissionAction action)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userId) &&
               await _permissionService.HasPermissionAsync(userId, module, action);
    }

    private IActionResult Download(string storageKey, string? originalFileName, string expectedCategory)
    {
        if (!storageKey.StartsWith(expectedCategory + "/", StringComparison.OrdinalIgnoreCase))
            return NotFound();

        var stream = _fileStorage.OpenRead(storageKey);
        if (stream == null)
            return NotFound();

        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Serve uploads as downloads to avoid executing active content such as
        // HTML or SVG within the Sentinel origin.
        return File(stream, "application/octet-stream", Path.GetFileName(originalFileName ?? "attachment"));
    }
}
