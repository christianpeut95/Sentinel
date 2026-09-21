using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services;

/// <summary>
/// Represents the two independent conditions required before Sentinel loads
/// WebDataRocks: organisation-level activation and current-user acceptance of
/// the vendor agreement.
/// </summary>
public sealed record WebDataRocksLicenseStatus(
    bool OrganizationEnabled,
    bool OrganizationAccepted,
    bool UserAccepted)
{
    public bool CanUse => OrganizationEnabled && OrganizationAccepted && UserAccepted;
}

public interface IWebDataRocksLicenseService
{
    Task<WebDataRocksLicenseStatus> GetStatusAsync(string? userId, CancellationToken cancellationToken = default);
    Task SetOrganizationAcceptanceAsync(bool enabled, string acceptedByUserId, CancellationToken cancellationToken = default);
    Task AcceptForUserAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class WebDataRocksLicenseService : IWebDataRocksLicenseService
{
    /// <summary>
    /// The revision displayed at https://www.webdatarocks.com/license-agreement/.
    /// Update this value when the vendor publishes a new agreement so both the
    /// organisation and each user must accept the new revision before use.
    /// </summary>
    public const string CurrentLicenseVersion = "2024-04-18";
    public const string LicenseUrl = "https://www.webdatarocks.com/license-agreement/";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebDataRocksLicenseService> _logger;

    public WebDataRocksLicenseService(
        ApplicationDbContext context,
        ILogger<WebDataRocksLicenseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WebDataRocksLicenseStatus> GetStatusAsync(
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SystemSettings
            .AsNoTracking()
            .Select(s => new
            {
                s.EnableWebDataRocks,
                s.WebDataRocksLicenseVersion,
                s.WebDataRocksLicenseAcceptedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var organizationEnabled = settings?.EnableWebDataRocks == true;
        var organizationAccepted = organizationEnabled
            && settings?.WebDataRocksLicenseAcceptedAt != null
            && string.Equals(settings.WebDataRocksLicenseVersion, CurrentLicenseVersion, StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return new WebDataRocksLicenseStatus(organizationEnabled, organizationAccepted, false);
        }

        var userAcceptance = await _context.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.WebDataRocksLicenseVersion,
                user.WebDataRocksLicenseAcceptedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        var userAccepted = userAcceptance?.WebDataRocksLicenseAcceptedAt != null
            && string.Equals(userAcceptance.WebDataRocksLicenseVersion, CurrentLicenseVersion, StringComparison.Ordinal);

        return new WebDataRocksLicenseStatus(organizationEnabled, organizationAccepted, userAccepted);
    }

    public async Task SetOrganizationAcceptanceAsync(
        bool enabled,
        string acceptedByUserId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.SystemSettings.FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("System settings have not been initialised.");

        settings.EnableWebDataRocks = enabled;
        settings.ModifiedAt = DateTime.UtcNow;
        settings.ModifiedByUserId = acceptedByUserId;

        if (enabled)
        {
            settings.WebDataRocksLicenseVersion = CurrentLicenseVersion;
            settings.WebDataRocksLicenseAcceptedAt = DateTime.UtcNow;
            settings.WebDataRocksLicenseAcceptedByUserId = acceptedByUserId;
        }
        else
        {
            // Disabling the feature removes the organisation-level consent. A
            // later re-enable must explicitly confirm the terms again.
            settings.WebDataRocksLicenseVersion = null;
            settings.WebDataRocksLicenseAcceptedAt = null;
            settings.WebDataRocksLicenseAcceptedByUserId = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Interactive pivot reports were {State} by user {UserId}",
            enabled ? "enabled after licence acceptance" : "disabled",
            acceptedByUserId);
    }

    public async Task AcceptForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("The current user could not be found.");

        user.WebDataRocksLicenseVersion = CurrentLicenseVersion;
        user.WebDataRocksLicenseAcceptedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("WebDataRocks terms accepted by user {UserId}", userId);
    }
}
