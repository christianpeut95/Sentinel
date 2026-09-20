using System.Text.Json;
using Sentinel.Models.Dashboard;

namespace Sentinel.Services;

/// <summary>
/// Defines the server-side shape and bounds for a user's persisted dashboard
/// preference. Dashboard configuration is user-owned, but it still influences
/// server-side queries and must never become an unbounded or arbitrary request
/// object simply because it is stored as JSON.
/// </summary>
public static class DashboardConfigPolicy
{
    private static readonly HashSet<string> AllowedWidgetIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "recent-activity",
        "cases-by-disease",
        "quick-stats",
        "hl7-overview",
        "tasks-surveys",
        "outbreak-tracker",
        "data-review-queue"
    };

    private static readonly HashSet<string> AllowedLayouts = new(StringComparer.OrdinalIgnoreCase)
    {
        "grid",
        "list"
    };

    private static readonly HashSet<string> AllowedSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        "compact",
        "medium",
        "wide"
    };

    private static readonly HashSet<string> AllowedTimeWindows = new(StringComparer.OrdinalIgnoreCase)
    {
        "24h",
        "48h",
        "7d",
        "30d",
        "thisWeek",
        "thisMonth"
    };

    public static bool TryNormalize(
        DashboardConfig? input,
        out DashboardConfig normalized,
        out string error)
    {
        normalized = new DashboardConfig();
        error = "The dashboard configuration is invalid.";

        if (input is null)
        {
            return false;
        }

        if (input.ConfigVersion != 1 || !AllowedLayouts.Contains(input.Layout ?? string.Empty))
        {
            return false;
        }

        if (input.Widgets is null || input.Widgets.Count > AllowedWidgetIds.Count ||
            input.PinnedDiseases is null || input.PinnedDiseases.Count > 50 ||
            input.TimeDefaults is null ||
            !AllowedTimeWindows.Contains(input.TimeDefaults.DefaultTimeWindow ?? string.Empty))
        {
            return false;
        }

        var widgetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var widgets = new List<WidgetConfig>();
        foreach (var widget in input.Widgets)
        {
            if (widget is null ||
                !AllowedWidgetIds.Contains(widget.WidgetId ?? string.Empty) ||
                !widgetIds.Add(widget.WidgetId) ||
                widget.Position < 0 || widget.Position >= AllowedWidgetIds.Count ||
                !AllowedSizes.Contains(widget.Size ?? string.Empty) ||
                !TryNormalizeSettings(widget.Settings, out var settings))
            {
                return false;
            }

            widgets.Add(new WidgetConfig
            {
                WidgetId = widget.WidgetId,
                Position = widget.Position,
                Size = widget.Size,
                Settings = settings
            });
        }

        var pinnedDiseases = new List<string>();
        foreach (var diseaseId in input.PinnedDiseases)
        {
            if (!Guid.TryParse(diseaseId, out var parsedId))
            {
                return false;
            }

            var canonicalId = parsedId.ToString();
            if (!pinnedDiseases.Contains(canonicalId, StringComparer.OrdinalIgnoreCase))
            {
                pinnedDiseases.Add(canonicalId);
            }
        }

        normalized = new DashboardConfig
        {
            ConfigVersion = 1,
            Layout = input.Layout,
            Widgets = widgets,
            TimeDefaults = new TimeDefaults { DefaultTimeWindow = input.TimeDefaults.DefaultTimeWindow },
            PinnedDiseases = pinnedDiseases
        };
        error = string.Empty;
        return true;
    }

    public static bool TryNormalizeSettings(
        IReadOnlyDictionary<string, object>? input,
        out Dictionary<string, object> normalized)
    {
        normalized = new Dictionary<string, object>(StringComparer.Ordinal);
        if (input is null)
        {
            return true;
        }

        if (input.Count > 1)
        {
            return false;
        }

        foreach (var (key, value) in input)
        {
            if (!string.Equals(key, "timeWindow", StringComparison.Ordinal) ||
                !TryReadString(value, out var timeWindow) ||
                !AllowedTimeWindows.Contains(timeWindow))
            {
                return false;
            }

            normalized["timeWindow"] = timeWindow;
        }

        return true;
    }

    private static bool TryReadString(object? value, out string result)
    {
        result = string.Empty;
        switch (value)
        {
            case string stringValue:
                result = stringValue;
                return true;
            case JsonElement { ValueKind: JsonValueKind.String } jsonValue:
                result = jsonValue.GetString() ?? string.Empty;
                return true;
            default:
                return false;
        }
    }
}
