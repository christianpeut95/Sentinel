using System.Collections.Generic;

namespace Sentinel.Models.Dashboard
{
    public class DashboardConfig
    {
        public int ConfigVersion { get; set; } = 1;
        public string Layout { get; set; } = "grid"; // "grid" or "list"
        public List<WidgetConfig> Widgets { get; set; } = new();
        public TimeDefaults TimeDefaults { get; set; } = new();
        public List<string> PinnedDiseases { get; set; } = new(); // Store Guid as string
    }

    public class WidgetConfig
    {
        public string WidgetId { get; set; } = string.Empty;
        public int Position { get; set; }
        public string Size { get; set; } = "medium"; // "compact", "medium", "wide"
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public class TimeDefaults
    {
        public string DefaultTimeWindow { get; set; } = "24h"; // "24h", "48h", "7d", "30d", "thisWeek", "thisMonth"
    }
}
