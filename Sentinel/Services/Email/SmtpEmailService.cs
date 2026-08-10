using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Sentinel.Data;

namespace Sentinel.Services.Email
{
    /// <summary>
    /// SMTP-based email service implementation using MailKit
    /// Sends emails via configured SMTP server for password resets, notifications, and alerts
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SmtpEmailService> _logger;
        private readonly IConfiguration _configuration;

        public SmtpEmailService(
            ApplicationDbContext context,
            IEncryptionService encryptionService,
            ILogger<SmtpEmailService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _encryptionService = encryptionService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            try
            {
                var settings = await GetSmtpSettingsAsync();
                if (settings == null)
                {
                    _logger.LogWarning("SMTP not configured - cannot send email");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.FromDisplayName ?? "Sentinel", settings.FromEmail));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                {
                    bodyBuilder.HtmlBody = body;
                }
                else
                {
                    bodyBuilder.TextBody = body;
                }
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                // Connect to SMTP server
                await client.ConnectAsync(settings.Host, settings.Port, settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                // Authenticate if credentials provided
                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    await client.AuthenticateAsync(settings.Username, settings.Password);
                }

                // Send email
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To} with subject: {Subject}", to, subject);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string userName)
        {
            var appName = _configuration["Organization:Name"] ?? "Sentinel";
            var subject = $"Password Reset Request - {appName}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; line-height: 1.6; color: #0C2A20; background: #F5F3EC; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FBFAF5; border: 1px solid #D8D4C4; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #1E5D44 0%, #0C2A20 100%); color: #F5F3EC; padding: 32px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; letter-spacing: -0.02em; }}
        .content {{ padding: 32px; }}
        .content p {{ margin: 0 0 16px; color: #4A5A55; }}
        .button {{ display: inline-block; background: #3DD598; color: #0C2A20; padding: 12px 32px; text-decoration: none; border-radius: 6px; font-weight: 500; margin: 16px 0; }}
        .button:hover {{ background: #159C6E; }}
        .footer {{ padding: 24px 32px; background: #ECEAE1; border-top: 1px solid #D8D4C4; text-align: center; font-size: 13px; color: #6B7A78; }}
        .alert {{ background: #FFF4E6; border-left: 4px solid #E0A43A; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🛡️ Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>We received a request to reset your password for your {appName} account.</p>
            <p>Click the button below to reset your password:</p>
            <p style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </p>
            <div class='alert'>
                <strong>⚠️ Security Notice:</strong> This link will expire in 24 hours. If you didn't request this reset, please ignore this email or contact your administrator.
            </div>
            <p>If the button doesn't work, copy and paste this link into your browser:</p>
            <p style='font-family: monospace; font-size: 12px; background: #ECEAE1; padding: 8px; border-radius: 4px; word-break: break-all;'>{resetLink}</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from {appName}. Please do not reply to this email.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(to, subject, body, isHtml: true);
        }

        public async Task<bool> SendWelcomeEmailAsync(string to, string userName, string? tempPassword = null)
        {
            var appName = _configuration["Organization:Name"] ?? "Sentinel";
            var appUrl = _configuration["SystemSettings:ApplicationUrl"] ?? "http://localhost";
            var subject = $"Welcome to {appName}";

            var passwordSection = string.IsNullOrEmpty(tempPassword) 
                ? $"<p>Please use the password provided by your administrator to log in.</p>"
                : $@"<div class='alert'>
                        <strong>🔑 Your temporary password:</strong><br/>
                        <code style='font-size: 16px; font-weight: 600; color: #0C2A20;'>{tempPassword}</code><br/>
                        <small>Please change this password after your first login.</small>
                     </div>";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; line-height: 1.6; color: #0C2A20; background: #F5F3EC; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FBFAF5; border: 1px solid #D8D4C4; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #3DD598 0%, #1E5D44 100%); color: #0C2A20; padding: 32px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 28px; font-weight: 600; letter-spacing: -0.03em; }}
        .content {{ padding: 32px; }}
        .content p {{ margin: 0 0 16px; color: #4A5A55; }}
        .button {{ display: inline-block; background: #3DD598; color: #0C2A20; padding: 12px 32px; text-decoration: none; border-radius: 6px; font-weight: 500; margin: 16px 0; }}
        .alert {{ background: #D9F2E5; border-left: 4px solid #3DD598; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
        .footer {{ padding: 24px 32px; background: #ECEAE1; border-top: 1px solid #D8D4C4; text-align: center; font-size: 13px; color: #6B7A78; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>👋 Welcome to {appName}</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <p>Your account has been created and you now have access to the {appName} surveillance system.</p>
            {passwordSection}
            <p style='text-align: center;'>
                <a href='{appUrl}/Identity/Account/Login' class='button'>Go to Login</a>
            </p>
            <p>If you have any questions or need assistance, please contact your system administrator.</p>
        </div>
        <div class='footer'>
            <p>This is an automated message from {appName}.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(to, subject, body, isHtml: true);
        }

        public async Task<bool> SendTestEmailAsync(string to)
        {
            var appName = _configuration["Organization:Name"] ?? "Sentinel";
            var subject = $"Test Email from {appName}";
            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; line-height: 1.6; color: #0C2A20; background: #F5F3EC; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FBFAF5; border: 1px solid #D8D4C4; border-radius: 8px; padding: 32px; text-align: center; }}
        .success {{ color: #159C6E; font-size: 48px; margin-bottom: 16px; }}
        h1 {{ font-size: 24px; font-weight: 600; margin: 0 0 8px; }}
        p {{ color: #4A5A55; margin: 0 0 8px; }}
        .timestamp {{ font-family: 'Geist Mono', monospace; font-size: 12px; color: #6B7A78; background: #ECEAE1; padding: 8px; border-radius: 4px; margin-top: 16px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='success'>✓</div>
        <h1>SMTP Configuration Test</h1>
        <p>Congratulations! Your {appName} email configuration is working correctly.</p>
        <p class='timestamp'>Sent: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
    </div>
</body>
</html>";

            return await SendEmailAsync(to, subject, body, isHtml: true);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var settings = await GetSmtpSettingsAsync();
                if (settings == null)
                {
                    _logger.LogWarning("SMTP not configured - cannot test connection");
                    return false;
                }

                using var client = new SmtpClient();
                await client.ConnectAsync(settings.Host, settings.Port, settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

                if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
                {
                    await client.AuthenticateAsync(settings.Username, settings.Password);
                }

                await client.DisconnectAsync(true);

                _logger.LogInformation("SMTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP connection test failed");
                return false;
            }
        }

        public async Task<bool> SendLockoutNotificationAsync(string to, string userName, DateTimeOffset lockoutEnd, string unlockUrl)
        {
            var appName = _configuration["Organization:Name"] ?? "Sentinel";
            var subject = $"Account Locked - {appName}";
            var lockoutMinutes = (lockoutEnd - DateTimeOffset.UtcNow).TotalMinutes;

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; line-height: 1.6; color: #0C2A20; background: #F5F3EC; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FBFAF5; border: 1px solid #D8D4C4; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #E04D2B 0%, #C23A1A 100%); color: #FBFAF5; padding: 32px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .content {{ padding: 32px; }}
        .alert {{ background: #FFE5E0; border-left: 4px solid #E04D2B; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
        .button {{ display: inline-block; background: #3DD598; color: #0C2A20; padding: 12px 32px; text-decoration: none; border-radius: 6px; font-weight: 500; margin: 16px 0; }}
        .footer {{ padding: 24px 32px; background: #ECEAE1; border-top: 1px solid #D8D4C4; text-align: center; font-size: 13px; color: #6B7A78; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔒 Account Locked</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <div class='alert'>
                <strong>Your account has been temporarily locked due to multiple failed login attempts.</strong>
            </div>
            <p>Your account will be automatically unlocked in approximately <strong>{lockoutMinutes:F0} minutes</strong> ({lockoutEnd:yyyy-MM-dd HH:mm} UTC).</p>
            <p>If you believe this was an error or need immediate access, you can request an early unlock:</p>
            <p style='text-align: center;'>
                <a href='{unlockUrl}' class='button'>Request Unlock</a>
            </p>
            <p><strong>Security Tip:</strong> If you did not attempt to log in, please contact your administrator immediately as this may indicate unauthorized access attempts.</p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from {appName}.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(to, subject, body, isHtml: true);
        }

        public async Task<bool> SendSecurityAlertAsync(string to, string userName, string alertMessage)
        {
            var appName = _configuration["Organization:Name"] ?? "Sentinel";
            var subject = $"Security Alert - {appName}";

            var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <style>
        body {{ font-family: 'Geist', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; line-height: 1.6; color: #0C2A20; background: #F5F3EC; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #FBFAF5; border: 1px solid #D8D4C4; border-radius: 8px; overflow: hidden; }}
        .header {{ background: linear-gradient(135deg, #E0A43A 0%, #C88A20 100%); color: #0C2A20; padding: 32px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .content {{ padding: 32px; }}
        .alert {{ background: #FFF4E6; border-left: 4px solid #E0A43A; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
        .footer {{ padding: 24px 32px; background: #ECEAE1; border-top: 1px solid #D8D4C4; text-align: center; font-size: 13px; color: #6B7A78; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>⚠️ Security Alert</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>{userName}</strong>,</p>
            <div class='alert'>
                <strong>Security Alert:</strong> {alertMessage}
            </div>
            <p><strong>Timestamp:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</p>
            <p>If this activity was not performed by you, please contact your administrator immediately and consider changing your password.</p>
        </div>
        <div class='footer'>
            <p>This is an automated security notification from {appName}.</p>
        </div>
    </div>
</body>
</html>";

            return await SendEmailAsync(to, subject, body, isHtml: true);
        }

        // ── Private Helper Methods ──────────────────────────────────

        private async Task<SmtpSettings?> GetSmtpSettingsAsync()
        {
            var systemSettings = await _context.SystemSettings.FirstOrDefaultAsync();

            if (systemSettings == null || !systemSettings.SmtpConfigured)
            {
                return null;
            }

            // Decrypt password if needed
            string? decryptedPassword = null;
            if (!string.IsNullOrEmpty(systemSettings.SmtpPasswordEncrypted))
            {
                decryptedPassword = _encryptionService.Decrypt(systemSettings.SmtpPasswordEncrypted);
            }

            return new SmtpSettings
            {
                Host = systemSettings.SmtpHost ?? "",
                Port = systemSettings.SmtpPort ?? 587,
                EnableSsl = systemSettings.SmtpEnableSsl,
                Username = systemSettings.SmtpUsername,
                Password = decryptedPassword,
                FromEmail = systemSettings.SmtpFromEmail ?? "noreply@sentinel.local",
                FromDisplayName = systemSettings.SmtpFromDisplayName ?? "Sentinel"
            };
        }

        private class SmtpSettings
        {
            public string Host { get; set; } = "";
            public int Port { get; set; }
            public bool EnableSsl { get; set; }
            public string? Username { get; set; }
            public string? Password { get; set; }
            public string FromEmail { get; set; } = "";
            public string FromDisplayName { get; set; } = "";
        }
    }
}
