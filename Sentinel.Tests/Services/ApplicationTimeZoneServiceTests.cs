using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sentinel.Services;
using Sentinel.Services.Reporting;

namespace Sentinel.Tests.Services;

public sealed class ApplicationTimeZoneServiceTests
{
    [Fact]
    public void RegionalSettings_AcceptKnownValuesAndRejectUntrustedValues()
    {
        Assert.True(OrganizationRegionalSettings.TryResolveTimeZone("Australia/Adelaide", out var timeZone));
        Assert.Equal("Australia/Adelaide", OrganizationRegionalSettings.GetCanonicalTimeZoneId(timeZone));

        Assert.True(OrganizationRegionalSettings.TryGetCulture("en-AU", out var culture));
        Assert.Equal("en-AU", culture.Name);

        Assert.True(OrganizationRegionalSettings.TryGetRegion("AU", out var region));
        Assert.Equal("AU", region!.TwoLetterISORegionName);

        Assert.False(OrganizationRegionalSettings.TryResolveTimeZone("../../../untrusted", out _));
        Assert.False(OrganizationRegionalSettings.TryGetCulture("not-a-culture", out _));
        Assert.False(OrganizationRegionalSettings.TryGetRegion("ZZ", out _));
    }

    [Fact]
    public void Service_ConvertsUtcAndRejectsNonexistentAdelaideDstTime()
    {
        using var service = CreateService();

        var winterUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var local = service.UtcToAppTime(winterUtc);

        Assert.Equal(new DateTime(2026, 6, 1, 9, 30, 0), local);
        Assert.Equal("en-AU", service.AppCulture.Name);
        Assert.Equal("1 June 2026", service.Format(winterUtc, "d MMMM yyyy"));

        // Adelaide moves from 02:00 to 03:00 on 4 October 2026, so 02:30
        // cannot be silently persisted as a different instant.
        Assert.False(service.TryAppTimeToUtc(new DateTime(2026, 10, 4, 2, 30, 0), out _));

        // The repeated hour at the end of daylight saving likewise has no
        // unambiguous offset in an HTML datetime-local submission.
        Assert.False(service.TryAppTimeToUtc(new DateTime(2026, 4, 5, 2, 30, 0), out _));

        Assert.True(service.TryAppTimeToUtc(new DateTime(2026, 6, 1, 9, 30, 0), out var converted));
        Assert.Equal(winterUtc, converted);
    }

    [Fact]
    public void DynamicDateResolver_UsesOrganisationDayWhenNoReferenceIsProvided()
    {
        var regionalSettings = Mock.Of<IApplicationTimeZoneService>(service =>
            service.Now == new DateTime(2026, 1, 2, 0, 30, 0));
        var resolver = new DynamicDateResolver(regionalSettings);

        Assert.Equal(new DateTime(2026, 1, 2), resolver.ResolveDate("Today"));
        Assert.Equal(new DateTime(2025, 12, 26), resolver.ResolveDate("Past7Days"));
    }

    [Fact]
    public void Service_UsesMatchingUtcIdentifierWhenTimezoneConfigurationIsMissing()
    {
        using var service = new ApplicationTimeZoneService(
            new ConfigurationBuilder().AddInMemoryCollection().Build(),
            NullLogger<ApplicationTimeZoneService>.Instance);

        Assert.Equal(TimeZoneInfo.Utc, service.AppTimeZone);
        Assert.Equal(OrganizationRegionalSettings.GetCanonicalTimeZoneId(TimeZoneInfo.Utc), service.TimeZoneId);
    }

    private static ApplicationTimeZoneService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Organization:TimeZoneId"] = "Australia/Adelaide",
                ["Organization:Locale"] = "en-AU"
            })
            .Build();

        return new ApplicationTimeZoneService(
            configuration,
            NullLogger<ApplicationTimeZoneService>.Instance);
    }
}
