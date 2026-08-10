using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Sentinel.Services;
using System.Text.Json;

namespace Sentinel.Pages.Settings;

[Authorize(Policy = "Permission.Settings.ManageOrganization")]
public class BackupsModel : PageModel
{
    private readonly IBackupService _backupService;
    private readonly ILogger<BackupsModel> _logger;
    private readonly IConfiguration _configuration;

    public BackupsModel(
        IBackupService backupService,
        ILogger<BackupsModel> logger,
        IConfiguration configuration)
    {
        _backupService = backupService;
        _logger = logger;
        _configuration = configuration;
    }

    public List<BackupInfo> Backups { get; set; } = new();
    public string CurrentBackupPath { get; set; } = string.Empty;
    public string? StatusMessage { get; set; }
    public bool StatusIsError { get; set; }

    public async Task OnGetAsync()
    {
        LoadCurrentConfiguration();
        await LoadBackups();
    }

    public async Task<IActionResult> OnPostCreateBackupAsync()
    {
        try
        {
            _logger.LogInformation("Creating new database backup");
            var result = await _backupService.CreateBackupAsync(BackupType.Full);

            if (result.Success)
            {
                StatusMessage = $"Backup created successfully: {result.BackupFileName} ({result.SizeInMB:F2} MB)";
                StatusIsError = false;
                _logger.LogInformation("Backup created: {FileName}", result.BackupFileName);
            }
            else
            {
                StatusMessage = $"Backup failed: {result.ErrorMessage}";
                StatusIsError = true;
                _logger.LogError("Backup creation failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating backup: {ex.Message}";
            StatusIsError = true;
            _logger.LogError(ex, "Exception during backup creation");
        }

        LoadCurrentConfiguration();
        await LoadBackups();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteBackupAsync(string fileName)
    {
        try
        {
            _logger.LogInformation("Deleting backup: {FileName}", fileName);
            var success = await _backupService.DeleteBackupAsync(fileName);

            if (success)
            {
                StatusMessage = $"Backup deleted successfully: {fileName}";
                StatusIsError = false;
            }
            else
            {
                StatusMessage = $"Failed to delete backup: {fileName}";
                StatusIsError = true;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting backup: {ex.Message}";
            StatusIsError = true;
            _logger.LogError(ex, "Exception during backup deletion");
        }

        LoadCurrentConfiguration();
        await LoadBackups();
        return Page();
    }

    private void LoadCurrentConfiguration()
    {
        var configPath = _configuration["Backup:Path"] ?? "";
        CurrentBackupPath = string.IsNullOrWhiteSpace(configPath)
            ? @"C:\DatabaseBackups\SurveillanceMVP (default)"
            : configPath;
    }

    private async Task LoadBackups()
    {
        try
        {
            Backups = await _backupService.GetBackupHistoryAsync();
            _logger.LogInformation("Loaded {Count} backups from history", Backups.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load backup history");
            StatusMessage = $"Failed to load backup history: {ex.Message}";
            StatusIsError = true;
        }
    }

    public string FormatDuration(TimeSpan? duration)
    {
        if (!duration.HasValue || duration.Value == TimeSpan.Zero)
            return "N/A";

        if (duration.Value.TotalMinutes < 1)
            return $"{duration.Value.Seconds}s";

        if (duration.Value.TotalHours < 1)
            return $"{duration.Value.Minutes}m {duration.Value.Seconds}s";

        return $"{(int)duration.Value.TotalHours}h {duration.Value.Minutes}m";
    }
}
