using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services;

/// <summary>
/// One-time-on-start migration for attachments previously stored beneath
/// wwwroot/uploads. The files are moved before requests are served and the
/// corresponding database values are changed from public URLs to storage keys.
/// </summary>
public sealed class ProtectedFileStorageMigrationService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProtectedFileStorageMigrationService> _logger;

    public ProtectedFileStorageMigrationService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProtectedFileStorageMigrationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IProtectedFileStorageService>();

        // TimelineStorageService owns the timeline migration. Resolve it here
        // so the legacy JSON files are moved on startup rather than only after
        // the first user opens a case timeline.
        _ = scope.ServiceProvider.GetRequiredService<ITimelineStorageService>();

        var notes = await context.Notes
            .IgnoreQueryFilters()
            .Where(n => n.AttachmentPath != null && n.AttachmentPath.StartsWith("/uploads/notes/"))
            .ToListAsync(cancellationToken);

        var labResults = await context.LabResults
            .IgnoreQueryFilters()
            .Where(lr => lr.AttachmentPath != null &&
                (lr.AttachmentPath.StartsWith("/uploads/lab-results/") ||
                 lr.AttachmentPath.StartsWith("/uploads/labresults/")))
            .ToListAsync(cancellationToken);

        var migratedCount = 0;

        foreach (var note in notes)
        {
            var storageKey = await fileStorage.MigrateLegacyAttachmentAsync(
                note.AttachmentPath,
                ProtectedFileStorageService.NotesCategory,
                cancellationToken);

            if (storageKey == null)
                continue;

            note.AttachmentPath = storageKey;
            migratedCount++;
        }

        foreach (var labResult in labResults)
        {
            var storageKey = await fileStorage.MigrateLegacyAttachmentAsync(
                labResult.AttachmentPath,
                ProtectedFileStorageService.LabResultsCategory,
                cancellationToken);

            if (storageKey == null)
                continue;

            labResult.AttachmentPath = storageKey;
            migratedCount++;
        }

        if (migratedCount > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Migrated {Count} legacy sensitive uploads to protected storage", migratedCount);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
