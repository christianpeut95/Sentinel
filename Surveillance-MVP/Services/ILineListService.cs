using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services;

public interface ILineListService
{
    /// <summary>
    /// Get all available fields for line list configuration
    /// </summary>
    Task<List<LineListField>> GetAvailableFieldsAsync(int outbreakId);
    
    /// <summary>
    /// Get line list data with selected fields
    /// </summary>
    Task<List<LineListDataRow>> GetLineListDataAsync(int outbreakId, List<string> fieldPaths, string? sortConfig = null, string? filterConfig = null);
    
    /// <summary>
    /// Save line list configuration
    /// </summary>
    Task<OutbreakLineListConfiguration> SaveConfigurationAsync(OutbreakLineListConfiguration config);
    
    /// <summary>
    /// Get user's configurations for an outbreak
    /// </summary>
    Task<List<OutbreakLineListConfiguration>> GetUserConfigurationsAsync(int outbreakId, string userId);
    
    /// <summary>
    /// Get shared configurations for an outbreak
    /// </summary>
    Task<List<OutbreakLineListConfiguration>> GetSharedConfigurationsAsync(int outbreakId);
    
    /// <summary>
    /// Delete configuration
    /// </summary>
    Task<bool> DeleteConfigurationAsync(int configId, string userId);
    
    /// <summary>
    /// Set configuration as default for user
    /// </summary>
    Task<bool> SetDefaultConfigurationAsync(int configId, string userId);
    
    /// <summary>
    /// Export line list data to CSV
    /// </summary>
    Task<byte[]> ExportToCsvAsync(int outbreakId, List<string> fieldPaths, string? sortConfig = null);
}
