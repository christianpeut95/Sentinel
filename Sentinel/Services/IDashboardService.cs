using Sentinel.Models.Dashboard;

namespace Sentinel.Services
{
    public interface IDashboardService
    {
        Task<DashboardConfig> GetUserDashboardConfigAsync(string userId);
        Task SaveUserDashboardConfigAsync(string userId, DashboardConfig config);
        Task<WidgetData> GetWidgetDataAsync(string widgetId, string userId, Dictionary<string, object>? settings = null);
        Task<bool> CanAccessWidgetAsync(string widgetId, string userId);
        DashboardConfig GetDefaultConfig(string userRole);
    }
}
