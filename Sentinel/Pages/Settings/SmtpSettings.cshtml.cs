using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Sentinel.Services;
using System.ComponentModel.DataAnnotations;
using MailKit.Net.Smtp;
using MimeKit;

namespace Sentinel.Pages.Settings
{
    [Authorize]
    public class SmtpSettingsModel : PageModel
    {
        private readonly ISystemSettingsService _settingsService;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<SmtpSettingsModel> _logger;

        public SmtpSettingsModel(
            ISystemSettingsService settingsService,
            IEncryptionService encryptionService,
            ILogger<SmtpSettingsModel> logger)
        {
            _settingsService = settingsService;
            _encryptionService = encryptionService;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "SMTP Host is required")]
        public string SmtpHost { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        public int SmtpPort { get; set; } = 587;

        [BindProperty]
        public bool EnableSsl { get; set; } = true;

        [BindProperty]
        public string? SmtpUsername { get; set; }

        [BindProperty]
        public string? SmtpPassword { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "From Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string FromEmail { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "From Display Name is required")]
        public string FromDisplayName { get; set; } = string.Empty;

        [BindProperty]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string? TestEmailAddress { get; set; }

        public SmtpTestResult? SmtpTestResultValue { get; set; }

        public async Task OnGetAsync()
        {
            var settings = await _settingsService.GetSettingsAsync();

            if (settings != null)
            {
                SmtpHost = settings.SmtpHost ?? "smtp.gmail.com";
                SmtpPort = settings.SmtpPort ?? 587;
                EnableSsl = settings.SmtpEnableSsl;
                SmtpUsername = settings.SmtpUsername;
                FromEmail = settings.SmtpFromEmail ?? "noreply@sentinel.local";
                FromDisplayName = settings.SmtpFromDisplayName ?? "Sentinel Surveillance System";
            }
        }

        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                await _settingsService.SaveSmtpSettingsAsync(
                    SmtpHost,
                    SmtpPort,
                    EnableSsl,
                    SmtpUsername,
                    SmtpPassword,
                    FromEmail,
                    FromDisplayName
                );

                _logger.LogInformation("SMTP settings updated by {User}", User.Identity?.Name);

                TempData["SuccessMessage"] = "SMTP settings saved successfully";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving SMTP settings");
                ModelState.AddModelError(string.Empty, "Error saving settings: " + ex.Message);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostTestAsync()
        {
            try
            {
                string? passwordToUse = SmtpPassword;
                if (string.IsNullOrWhiteSpace(passwordToUse))
                {
                    var settings = await _settingsService.GetSettingsAsync();
                    if (!string.IsNullOrWhiteSpace(settings?.SmtpPasswordEncrypted))
                    {
                        passwordToUse = _encryptionService.Decrypt(settings.SmtpPasswordEncrypted);
                    }
                }

                var testEmail = TestEmailAddress ?? User.Identity?.Name ?? "admin@localhost";

                using var client = new SmtpClient();

                await client.ConnectAsync(SmtpHost, SmtpPort, EnableSsl ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.None);

                if (!string.IsNullOrWhiteSpace(SmtpUsername) && !string.IsNullOrWhiteSpace(passwordToUse))
                {
                    await client.AuthenticateAsync(SmtpUsername, passwordToUse);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(FromDisplayName, FromEmail));
                message.To.Add(new MailboxAddress("Test Recipient", testEmail));
                message.Subject = "Sentinel SMTP Test - " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

                message.Body = new TextPart("html")
                {
                    Text = $@"
                        <html>
                        <body style='font-family: Arial, sans-serif;'>
                            <h2 style='color: #159C6E;'>✓ SMTP Configuration Test</h2>
                            <p>This is a test email from Sentinel Surveillance System.</p>
                            <p><strong>Configuration Details:</strong></p>
                            <ul>
                                <li>SMTP Host: {SmtpHost}</li>
                                <li>Port: {SmtpPort}</li>
                                <li>SSL/TLS: {(EnableSsl ? "Enabled" : "Disabled")}</li>
                                <li>Authentication: {(!string.IsNullOrWhiteSpace(SmtpUsername) ? "Enabled" : "Not configured")}</li>
                                <li>Test Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</li>
                            </ul>
                            <p>If you received this email, your SMTP configuration is working correctly.</p>
                            <hr />
                            <p style='color: #666; font-size: 12px;'>
                                This is an automated test message from Sentinel Surveillance System.<br />
                                Configured by: {User.Identity?.Name}
                            </p>
                        </body>
                        </html>"
                };

                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                SmtpTestResultValue = new SmtpTestResult
                {
                    Success = true,
                    Message = $"Test email sent successfully to {testEmail}. Please check your inbox."
                };

                _logger.LogInformation("SMTP test successful. Test email sent to {Email}", testEmail);
            }
            catch (Exception ex)
            {
                SmtpTestResultValue = new SmtpTestResult
                {
                    Success = false,
                    Message = $"Connection test failed: {ex.Message}"
                };

                _logger.LogError(ex, "SMTP connection test failed");
            }

            return Page();
        }

        public class SmtpTestResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
        }
    }
}
