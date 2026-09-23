using Microsoft.AspNetCore.Components;
using Sentinel.Services;
using System.Text.Json;

namespace Sentinel.Components.Pages.Settings.Backups;

public partial class Index : ComponentBase
{
    [Inject]
    private IBackupService BackupService { get; set; } = default!;

    [Inject]
    private ILogger<Index> Logger { get; set; } = default!;

    [Inject]
    private IConfiguration Configuration { get; set; } = default!;

    [Inject]
    private IWebHostEnvironment Environment { get; set; } = default!;

    private List<BackupInfo> backups = new();
    private bool isLoading = true;
    private bool isCreatingBackup = false;
    private bool isProcessing = false;

    // Status messages
    private string? statusMessage;
    private bool statusIsError = false;

    // Modal state
    private bool showRestoreModal = false;
    private bool showDeleteModal = false;
    private BackupInfo? backupToRestore;
    private BackupInfo? backupToDelete;

    // Configuration state
    private bool showConfigSection = false;
    private bool isSavingConfig = false;
    private string configuredBackupPath = "";
    private string currentBackupPath = "";

    protected override async Task OnInitializedAsync()
    {
        LoadCurrentConfiguration();
        await LoadBackups();
    }

    private void LoadCurrentConfiguration()
    {
        var configPath = Configuration["Backup:Path"] ?? "";
        currentBackupPath = string.IsNullOrEmpty(configPath) 
            ? @"C:\DatabaseBackups\SurveillanceMVP (default)" 
            : configPath;
        configuredBackupPath = configPath;
    }

    private async Task LoadBackups()
    {
        try
        {
            isLoading = true;
            backups = await BackupService.GetBackupHistoryAsync();
            Logger.LogInformation("Loaded {Count} backups from history", backups.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load backup history");
            ShowError("Backup history could not be loaded. Check the application logs for details.");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task CreateBackup()
    {
        try
        {
            isCreatingBackup = true;
            ClearStatus();
            StateHasChanged();

            Logger.LogInformation("Creating new database backup");
            var result = await BackupService.CreateBackupAsync(BackupType.Full);

            if (result.Success)
            {
                ShowSuccess($"Backup created successfully: {result.BackupFileName} ({result.SizeInMB:F2} MB)");
                Logger.LogInformation("Backup created: {FileName}", result.BackupFileName);
                await LoadBackups(); // Reload to show new backup
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Backup failed. Check the application logs for details.");
                Logger.LogError("Backup creation failed: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during backup creation");
            ShowError("Backup could not be created. Check the application logs for details.");
        }
        finally
        {
            isCreatingBackup = false;
        }
    }

    private void ShowRestoreConfirmation(BackupInfo backup)
    {
        backupToRestore = backup;
        showRestoreModal = true;
    }

    private void CloseRestoreModal()
    {
        showRestoreModal = false;
        backupToRestore = null;
    }

    private async Task ConfirmRestore()
    {
        if (backupToRestore == null) return;

        try
        {
            isProcessing = true;
            ClearStatus();
            StateHasChanged();

            Logger.LogWarning("Restoring database from backup record {BackupId}", backupToRestore.Id);
            var success = await BackupService.RestoreBackupAsync(backupToRestore.Id);

            if (success)
            {
                ShowSuccess($"Database restored successfully from {backupToRestore.BackupFileName}");
                Logger.LogInformation("Database restored from: {FileName}", backupToRestore.BackupFileName);
            }
            else
            {
                ShowError("Failed to restore database. Check logs for details.");
                Logger.LogError("Database restore failed for: {FileName}", backupToRestore.BackupFileName);
            }

            CloseRestoreModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during database restore");
            ShowError("Database restore failed. Check the application logs for details.");
            CloseRestoreModal();
        }
        finally
        {
            isProcessing = false;
        }
    }

    private void ShowDeleteConfirmation(BackupInfo backup)
    {
        backupToDelete = backup;
        showDeleteModal = true;
    }

    private void CloseDeleteModal()
    {
        showDeleteModal = false;
        backupToDelete = null;
    }

    private async Task ConfirmDelete()
    {
        if (backupToDelete == null) return;

        try
        {
            isProcessing = true;
            ClearStatus();
            StateHasChanged();

            Logger.LogInformation("Deleting backup record {BackupId}", backupToDelete.Id);
            var success = await BackupService.DeleteBackupAsync(backupToDelete.Id);

            if (success)
            {
                ShowSuccess($"Backup deleted: {backupToDelete.BackupFileName}");
                Logger.LogInformation("Backup deleted: {FileName}", backupToDelete.BackupFileName);
                await LoadBackups(); // Reload to remove deleted backup
            }
            else
            {
                ShowError("Failed to delete backup. Check logs for details.");
                Logger.LogError("Backup deletion failed for: {FileName}", backupToDelete.BackupFileName);
            }

            CloseDeleteModal();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Exception during backup deletion");
            ShowError("Backup deletion failed. Check the application logs for details.");
            CloseDeleteModal();
        }
        finally
        {
            isProcessing = false;
        }
    }

    private string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalSeconds < 1)
            return "< 1s";
        if (duration.TotalMinutes < 1)
            return $"{duration.TotalSeconds:F0}s";
        if (duration.TotalHours < 1)
            return $"{duration.TotalMinutes:F1}m";
        return $"{duration.TotalHours:F1}h";
    }

    private void ShowSuccess(string message)
    {
        statusMessage = message;
        statusIsError = false;
    }

    private void ShowError(string message)
    {
        statusMessage = message;
        statusIsError = true;
    }

    private void ClearStatus()
    {
        statusMessage = null;
        statusIsError = false;
    }

    // Configuration methods
    private void ToggleConfigSection()
    {
        showConfigSection = !showConfigSection;
    }

    private void UseDefaultPath()
    {
        configuredBackupPath = "";
    }

    private void CancelConfiguration()
    {
        LoadCurrentConfiguration();
        showConfigSection = false;
    }

    private async Task SaveConfiguration()
    {
        try
        {
            isSavingConfig = true;
            ClearStatus();

            // Validate the path if not empty
            if (!string.IsNullOrWhiteSpace(configuredBackupPath))
            {
                // Ensure it's an absolute path
                if (!Path.IsPathRooted(configuredBackupPath))
                {
                    ShowError("Backup path must be an absolute path (e.g., C:\\backups or /var/backups)");
                    return;
                }

                // Try to create directory if it doesn't exist
                try
                {
                    if (!Directory.Exists(configuredBackupPath))
                    {
                        Directory.CreateDirectory(configuredBackupPath);
                    }

                    // Test write permissions
                    var testFile = Path.Combine(configuredBackupPath, ".write-test");
                    await File.WriteAllTextAsync(testFile, "test");
                    File.Delete(testFile);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Unable to write to configured backup path");
                    ShowError("The backup path could not be written to. Check the path and application logs, then try again.");
                    return;
                }
            }

            // Update appsettings.json
            var appSettingsPath = Path.Combine(Environment.ContentRootPath, "appsettings.json");

            string json;
            using (var stream = File.OpenRead(appSettingsPath))
            using (var reader = new StreamReader(stream))
            {
                json = await reader.ReadToEndAsync();
            }

            var jsonDoc = JsonDocument.Parse(json);
            var options = new JsonWriterOptions { Indented = true };

            using var memoryStream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(memoryStream, options))
            {
                writer.WriteStartObject();

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Name == "Backup")
                    {
                        writer.WriteStartObject("Backup");
                        writer.WriteString("Path", configuredBackupPath);

                        // Preserve RetentionDays
                        if (property.Value.TryGetProperty("RetentionDays", out var retentionDays))
                        {
                            writer.WriteNumber("RetentionDays", retentionDays.GetInt32());
                        }

                        writer.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var updatedJson = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            await File.WriteAllTextAsync(appSettingsPath, updatedJson);

            LoadCurrentConfiguration();
            ShowSuccess("Backup configuration saved successfully. The new path will be used for future backups.");
            Logger.LogInformation("Backup path updated to: {Path}", configuredBackupPath);
            showConfigSection = false;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save backup configuration");
            ShowError("Backup configuration could not be saved. Check the application logs for details.");
        }
        finally
        {
            isSavingConfig = false;
        }
    }
}
