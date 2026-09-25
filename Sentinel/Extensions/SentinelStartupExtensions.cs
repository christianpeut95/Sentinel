using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services;

namespace Sentinel.Extensions;

/// <summary>
/// Performs the ordered startup work that requires the built application
/// service provider. This is intentionally separate from service registration:
/// migrations and setup state must never run while the host is being composed.
/// </summary>
public static class SentinelStartupExtensions
{
    public static async Task InitializeSentinelAsync(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            await ApplyMigrationsAndSeedAsync(app.Services);
        }

        // ApplicationDbContext's partial SaveChanges implementation needs this
        // process-wide queue after dependency injection has been built.
        using (var scope = app.Services.CreateScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<Sentinel.Services.CaseDefinitionEvaluation.ICaseEvaluationQueue>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.SetEvaluationQueue(queue);
        }

        if (!app.Environment.IsEnvironment("Testing"))
        {
            await InitializeSetupStateAsync(app.Services);
        }
    }

    public static WebApplication MapSentinelHealthCheck(this WebApplication app)
    {
        app.MapGet("/health", async (ApplicationDbContext dbContext, ILoggerFactory loggerFactory) =>
        {
            try
            {
                await dbContext.Database.CanConnectAsync();
                return Results.Ok(new
                {
                    status = "healthy",
                    application = "Sentinel",
                    timestamp = DateTime.UtcNow,
                    database = "connected"
                });
            }
            catch (Exception exception)
            {
                var errorMessage = UserFacingError.Create(
                    loggerFactory.CreateLogger("Sentinel.Health"),
                    exception,
                    "health check");
                return Results.Json(new
                {
                    status = "unhealthy",
                    application = "Sentinel",
                    timestamp = DateTime.UtcNow,
                    database = "disconnected",
                    error = errorMessage
                }, statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).AllowAnonymous();

        return app;
    }

    private static async Task ApplyMigrationsAndSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Sentinel.Startup");
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        const int maxRetries = 10;
        var retryDelay = TimeSpan.FromSeconds(5);

        for (var retry = 0; retry < maxRetries; retry++)
        {
            try
            {
                logger.LogInformation("Applying database migrations... (Attempt {Retry}/{MaxRetries})", retry + 1, maxRetries);
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");

                logger.LogInformation("Verifying reporting views...");
                try
                {
                    await EnsureReportingViewsExistAsync(dbContext, logger);
                    logger.LogInformation("Reporting views verified successfully");
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to create/verify reporting views. Report Builder may not function correctly.");
                }

                break;
            }
            catch (Exception exception) when (retry < maxRetries - 1)
            {
                logger.LogWarning(exception, "Failed to apply migrations (Attempt {Retry}/{MaxRetries}). Retrying in {Delay} seconds...",
                    retry + 1, maxRetries, retryDelay.TotalSeconds);
                await Task.Delay(retryDelay);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to apply database migrations after {MaxRetries} attempts. Application may not function correctly.", maxRetries);
                throw;
            }
        }

        logger.LogInformation("Seeding permissions and lookup data...");
        await PermissionSeedService.SeedAsync(scope.ServiceProvider);
        await LookupDataSeedService.SeedAsync(scope.ServiceProvider);
        logger.LogInformation("Data seeding complete");
        await DemoUserSeedService.SeedAsync(scope.ServiceProvider);
    }

    private static async Task EnsureReportingViewsExistAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        var scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "RecreateReportingViews.sql");
        if (!File.Exists(scriptPath))
        {
            logger.LogWarning("RecreateReportingViews.sql not found at {Path}. Skipping view recreation.", scriptPath);
            return;
        }

        var viewCreationSql = await File.ReadAllTextAsync(scriptPath);
        logger.LogInformation("Loaded view recreation script from {Path}", scriptPath);
        var batches = viewCreationSql.Split(["\r\nGO\r\n", "\nGO\n", "\r\nGO", "\nGO"], StringSplitOptions.RemoveEmptyEntries);
        logger.LogInformation("Split into {Count} SQL batches", batches.Length);

        var executedBatches = 0;
        foreach (var batch in batches)
        {
            var trimmedBatch = batch.Trim();
            if (string.IsNullOrWhiteSpace(trimmedBatch) ||
                trimmedBatch.StartsWith("--") ||
                trimmedBatch.StartsWith("PRINT"))
            {
                continue;
            }

            try
            {
                await dbContext.Database.ExecuteSqlRawAsync(trimmedBatch);
                executedBatches++;
                logger.LogDebug("Executed batch {Number}: {Preview}...", executedBatches, trimmedBatch[..Math.Min(50, trimmedBatch.Length)]);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to execute SQL batch {Number}: {Batch}", executedBatches + 1, trimmedBatch[..Math.Min(200, trimmedBatch.Length)]);
                throw;
            }
        }

        logger.LogInformation("Successfully executed {Count} SQL batches to recreate reporting views", executedBatches);
    }

    private static async Task InitializeSetupStateAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var setupTokenFileService = scope.ServiceProvider.GetRequiredService<ISetupTokenFileService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Sentinel.SetupInitialization");

        try
        {
            var settings = await context.SystemSettings.FirstOrDefaultAsync();
            var legacyTokenFilePath = Path.Combine(AppContext.BaseDirectory, "setup-token.txt");
            if (File.Exists(legacyTokenFilePath))
            {
                try
                {
                    File.Delete(legacyTokenFilePath);
                    logger.LogInformation("Deleted legacy setup token file during startup");
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not remove the legacy setup token file; its token will not be accepted after rotation");
                }
            }

            if (settings == null)
            {
                logger.LogInformation("No system settings found - generating setup token for first-time setup");
                var plainToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                settings = new SystemSettings
                {
                    Id = Guid.NewGuid(),
                    SetupToken = encryptionService.Hash(plainToken),
                    SetupTokenGeneratedAt = DateTime.UtcNow,
                    SetupTokenExpiresAt = DateTime.UtcNow.AddHours(48),
                    IsSetupCompleted = false,
                    EnforceHttps = true,
                    SmtpEnableSsl = true,
                    HL7ProcessingEnabled = false,
                    SurveillanceStartupCompleted = false,
                    SurveillanceStartupProgressPercentage = 0,
                    EnableFeedbackWidget = false,
                    EnableUsageMonitoring = false,
                    TelemetryEnabled = false,
                    CreatedAt = DateTime.UtcNow
                };

                await setupTokenFileService.WriteTokenAsync(plainToken);
                try
                {
                    context.SystemSettings.Add(settings);
                    await context.SaveChangesAsync();
                }
                catch
                {
                    await setupTokenFileService.DeleteTokenAsync();
                    throw;
                }

                logger.LogCritical("Initial Sentinel setup is pending. Retrieve the one-time setup token from the protected server file {TokenFilePath}. The token expires at {Expiry} UTC and is deleted after setup completes.",
                    setupTokenFileService.TokenFilePath, settings.SetupTokenExpiresAt);
            }
            else if (!settings.IsSetupCompleted && settings.SetupToken != null)
            {
                var tokenExpired = !settings.SetupTokenExpiresAt.HasValue || settings.SetupTokenExpiresAt.Value <= DateTime.UtcNow;
                if (tokenExpired || !setupTokenFileService.TokenFileExists)
                {
                    var replacementToken = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(48));
                    settings.SetupToken = encryptionService.Hash(replacementToken);
                    settings.SetupTokenGeneratedAt = DateTime.UtcNow;
                    settings.SetupTokenExpiresAt = DateTime.UtcNow.AddHours(48);
                    await setupTokenFileService.WriteTokenAsync(replacementToken);
                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch
                    {
                        await setupTokenFileService.DeleteTokenAsync();
                        throw;
                    }

                    logger.LogCritical("Initial Sentinel setup remains incomplete. A replacement one-time setup token was created in the protected server file {TokenFilePath}; it expires at {Expiry} UTC.",
                        setupTokenFileService.TokenFilePath, settings.SetupTokenExpiresAt);
                }
                else
                {
                    logger.LogWarning("Setup not completed. The one-time token remains in the protected server file {TokenFilePath}", setupTokenFileService.TokenFilePath);
                    logger.LogWarning("Setup expires: {Expiry}", settings.SetupTokenExpiresAt);
                }
            }
            else if (settings.IsSetupCompleted)
            {
                logger.LogInformation("Sentinel setup completed at {CompletedAt}", settings.SetupCompletedAt);
            }

            var systemSettingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
            await systemSettingsService.GetInstallationIdAsync();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to initialize system settings / setup token");
            // Deliberately allow the app to start; the setup wizard can surface the
            // incomplete state without exposing implementation errors to a user.
        }
    }
}
