using System.Globalization;

namespace Sentinel.Services;

/// <summary>
/// Validates and normalises organisation-wide regional settings. Configuration
/// values are treated as untrusted by setup and settings pages.
/// </summary>
public static class OrganizationRegionalSettings
{
    // Setup defaults match Sentinel's Australian starter configuration. Existing
    // installations retain their configured value; invalid/missing values still
    // fail safely to UTC in ApplicationTimeZoneService.
    public const string DefaultTimeZoneId = "Australia/Adelaide";
    public const string DefaultLocale = "en-AU";

    public sealed record TimeZoneOption(string Id, string DisplayName, TimeSpan BaseUtcOffset);

    public sealed record LocaleOption(string Name, string DisplayName);

    public sealed record RegionOption(string Code, string DisplayName);

    public static bool TryResolveTimeZone(string? timeZoneId, out TimeZoneInfo timeZone)
    {
        timeZone = TimeZoneInfo.Utc;
        if (string.IsNullOrWhiteSpace(timeZoneId) || timeZoneId.Length > 128)
        {
            return false;
        }

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows and Linux expose different identifier families. Prefer
            // portable IANA identifiers in configuration, but resolve either.
            if (TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId, out var windowsId))
            {
                try
                {
                    timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsId);
                    return true;
                }
                catch (TimeZoneNotFoundException)
                {
                    return false;
                }
                catch (InvalidTimeZoneException)
                {
                    return false;
                }
            }

            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static string GetCanonicalTimeZoneId(TimeZoneInfo timeZone)
    {
        if (timeZone.Id.Contains('/', StringComparison.Ordinal))
        {
            return timeZone.Id;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZone.Id, out var ianaId)
            ? ianaId
            : timeZone.Id;
    }

    public static IReadOnlyList<TimeZoneOption> GetTimeZoneOptions() =>
        TimeZoneInfo.GetSystemTimeZones()
            .Select(timeZone => new TimeZoneOption(
                GetCanonicalTimeZoneId(timeZone),
                $"{timeZone.DisplayName} ({GetCanonicalTimeZoneId(timeZone)})",
                timeZone.BaseUtcOffset))
            .GroupBy(option => option.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(option => option.BaseUtcOffset)
            .ThenBy(option => option.DisplayName, StringComparer.CurrentCulture)
            .ToArray();

    public static bool TryGetCulture(string? locale, out CultureInfo culture)
    {
        culture = CultureInfo.GetCultureInfo(DefaultLocale);
        if (string.IsNullOrWhiteSpace(locale) || locale.Length > 85)
        {
            return false;
        }

        try
        {
            var candidate = CultureInfo.GetCultureInfo(locale);
            if (candidate.IsNeutralCulture)
            {
                return false;
            }

            culture = candidate;
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    public static IReadOnlyList<LocaleOption> GetLocaleOptions() =>
        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new LocaleOption(culture.Name, $"{culture.EnglishName} ({culture.Name})"))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCulture)
            .ToArray();

    public static bool TryGetRegion(string? countryCode, out RegionInfo? region)
    {
        region = null;
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
        {
            return false;
        }

        var normalizedCode = countryCode.Trim().ToUpperInvariant();
        var option = GetRegionOptions().FirstOrDefault(option =>
            string.Equals(option.Code, normalizedCode, StringComparison.Ordinal));
        if (option is null)
        {
            return false;
        }

        region = new RegionInfo(option.Code);
        return true;
    }

    public static IReadOnlyList<RegionOption> GetRegionOptions() =>
        CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture => new RegionInfo(culture.Name))
            .GroupBy(region => region.TwoLetterISORegionName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .Where(region => region.TwoLetterISORegionName.Length == 2
                             && !string.Equals(region.TwoLetterISORegionName, "IV", StringComparison.OrdinalIgnoreCase))
            .Select(region => new RegionOption(region.TwoLetterISORegionName, region.EnglishName))
            .OrderBy(option => option.DisplayName, StringComparer.CurrentCulture)
            .ToArray();
}
