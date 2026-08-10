using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for managing system-wide settings and setup state
    /// </summary>
    public interface ISystemSettingsService
    {
        Task<SystemSettings?> GetSettingsAsync();
        Task<bool> IsSetupCompletedAsync();
        Task<bool> ValidateTokenAsync(string token);
        Task<SystemSettings> CompleteSetupAsync(string userId);
        Task<SystemSettings> UpdateSettingsAsync(SystemSettings settings);
        Task SaveSmtpSettingsAsync(string host, int port, bool enableSsl, string? username, string? password, string fromEmail, string fromDisplayName);
    }

    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SystemSettingsService> _logger;

        public SystemSettingsService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            ILogger<SystemSettingsService> logger)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task<SystemSettings?> GetSettingsAsync()
        {
            return await _context.SystemSettings.FirstOrDefaultAsync();
        }

        public async Task<bool> IsSetupCompletedAsync()
        {
            var settings = await GetSettingsAsync();
            return settings?.IsSetupCompleted ?? false;
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                var settings = await GetSettingsAsync();

                if (settings == null || string.IsNullOrEmpty(settings.SetupToken))
                {
                    _logger.LogWarning("Token validation failed: No setup token found in database");
                    return false;
                }

                // Check if token has expired
                if (settings.SetupTokenExpiresAt.HasValue && settings.SetupTokenExpiresAt.Value < DateTime.UtcNow)
                {
                    _logger.LogWarning("Token validation failed: Token expired at {ExpiryDate}", settings.SetupTokenExpiresAt);
                    return false;
                }

                // Verify token hash
                var isValid = _encryptionService.VerifyHash(token.Trim(), settings.SetupToken);

                if (isValid)
                {
                    _logger.LogInformation("Setup token validated successfully");
                }
                else
                {
                    _logger.LogWarning("Token validation failed: Hash mismatch");
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating setup token");
                return false;
            }
        }

        public async Task<SystemSettings> CompleteSetupAsync(string userId)
        {
            var settings = await GetSettingsAsync();

            if (settings == null)
            {
                throw new InvalidOperationException("System settings not initialized");
            }

            settings.IsSetupCompleted = true;
            settings.SetupCompletedAt = DateTime.UtcNow;
            settings.SetupCompletedByUserId = userId;
            settings.SetupToken = null; // Invalidate token
            settings.ModifiedAt = DateTime.UtcNow;
            settings.ModifiedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Setup completed by user {UserId} at {CompletedAt}", userId, settings.SetupCompletedAt);

            return settings;
        }

        public async Task<SystemSettings> UpdateSettingsAsync(SystemSettings settings)
        {
            settings.ModifiedAt = DateTime.UtcNow;

            _context.SystemSettings.Update(settings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("System settings updated");

            return settings;
        }

        public async Task SaveSmtpSettingsAsync(
            string host, 
            int port, 
            bool enableSsl, 
            string? username, 
            string? password, 
            string fromEmail, 
            string fromDisplayName)
        {
            var settings = await GetSettingsAsync();

            if (settings == null)
            {
                throw new InvalidOperationException("System settings not initialized");
            }

            settings.SmtpHost = host;
            settings.SmtpPort = port;
            settings.SmtpEnableSsl = enableSsl;
            settings.SmtpUsername = username;
            settings.SmtpFromEmail = fromEmail;
            settings.SmtpFromDisplayName = fromDisplayName;

            // Encrypt password if provided
            if (!string.IsNullOrEmpty(password))
            {
                settings.SmtpPasswordEncrypted = _encryptionService.Encrypt(password);
            }

            settings.SmtpConfigured = true;
            settings.ModifiedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("SMTP settings saved for host {Host}", host);
        }
    }
}
