using System.Text.Json;
using Sentinel.Models.Dashboard;
using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class DashboardConfigPolicyTests
{
    [Fact]
    public void TryNormalize_AcceptsAndCanonicalizesBoundedDashboardPreferences()
    {
        var diseaseId = Guid.NewGuid();
        var input = new DashboardConfig
        {
            ConfigVersion = 1,
            Layout = "grid",
            TimeDefaults = new TimeDefaults { DefaultTimeWindow = "24h" },
            PinnedDiseases = [diseaseId.ToString("D"), diseaseId.ToString("D").ToUpperInvariant()],
            Widgets =
            [
                new WidgetConfig
                {
                    WidgetId = "cases-by-disease",
                    Position = 0,
                    Size = "wide",
                    Settings = new Dictionary<string, object>
                    {
                        ["timeWindow"] = JsonDocument.Parse("\"30d\"").RootElement.Clone()
                    }
                }
            ]
        };

        var valid = DashboardConfigPolicy.TryNormalize(input, out var normalized, out var error);

        Assert.True(valid, error);
        Assert.Equal("grid", normalized.Layout);
        Assert.Single(normalized.Widgets);
        Assert.Equal("30d", normalized.Widgets.Single().Settings["timeWindow"]);
        Assert.Single(normalized.PinnedDiseases);
        Assert.Equal(diseaseId.ToString(), normalized.PinnedDiseases.Single());
    }

    [Theory]
    [InlineData("unknown-widget", "medium")]
    [InlineData("cases-by-disease", "extra-wide")]
    public void TryNormalize_RejectsUnknownWidgetsAndUnsupportedSizes(string widgetId, string size)
    {
        var input = ValidConfig();
        input.Widgets[0].WidgetId = widgetId;
        input.Widgets[0].Size = size;

        var valid = DashboardConfigPolicy.TryNormalize(input, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryNormalize_RejectsUnexpectedSettingKeysValuesAndMalformedPinnedDiseaseIds()
    {
        var input = ValidConfig();
        input.Widgets[0].Settings["arbitraryServerSetting"] = "value";
        input.PinnedDiseases = ["not-a-guid"];

        var valid = DashboardConfigPolicy.TryNormalize(input, out _, out _);

        Assert.False(valid);
    }

    [Fact]
    public void TryNormalizeSettings_RejectsNonStringOrUnsupportedTimeWindows()
    {
        var numericSetting = new Dictionary<string, object> { ["timeWindow"] = 30 };
        var unsupportedWindow = new Dictionary<string, object> { ["timeWindow"] = "forever" };

        Assert.False(DashboardConfigPolicy.TryNormalizeSettings(numericSetting, out _));
        Assert.False(DashboardConfigPolicy.TryNormalizeSettings(unsupportedWindow, out _));
    }

    private static DashboardConfig ValidConfig() => new()
    {
        ConfigVersion = 1,
        Layout = "grid",
        TimeDefaults = new TimeDefaults { DefaultTimeWindow = "24h" },
        Widgets =
        [
            new WidgetConfig
            {
                WidgetId = "cases-by-disease",
                Position = 0,
                Size = "medium",
                Settings = new Dictionary<string, object> { ["timeWindow"] = "24h" }
            }
        ]
    };
}
