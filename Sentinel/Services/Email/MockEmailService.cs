namespace Sentinel.Services.Email
{
    /// <summary>
    /// Mock email service for development and testing
    /// Logs email attempts to console instead of actually sending emails
    /// </summary>
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            _logger.LogInformation("MOCK EMAIL: To={To}, Subject={Subject}, IsHtml={IsHtml}", to, subject, isHtml);
            _logger.LogDebug("MOCK EMAIL BODY:\n{Body}", body);
            return Task.FromResult(true);
        }

        public Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string userName)
        {
            _logger.LogInformation("MOCK PASSWORD RESET EMAIL: To={To}, UserName={UserName}, ResetLink={ResetLink}", 
                to, userName, resetLink);
            return Task.FromResult(true);
        }

        public Task<bool> SendWelcomeEmailAsync(string to, string userName, string? tempPassword = null)
        {
            _logger.LogInformation("MOCK WELCOME EMAIL: To={To}, UserName={UserName}, TempPassword={HasPassword}", 
                to, userName, !string.IsNullOrEmpty(tempPassword));
            return Task.FromResult(true);
        }

        public Task<bool> SendTestEmailAsync(string to)
        {
            _logger.LogInformation("MOCK TEST EMAIL: To={To}", to);
            return Task.FromResult(true);
        }

        public Task<bool> TestConnectionAsync()
        {
            _logger.LogInformation("MOCK SMTP CONNECTION TEST: Success (mock always succeeds)");
            return Task.FromResult(true);
        }

        public Task<bool> SendLockoutNotificationAsync(string to, string userName, DateTimeOffset lockoutEnd, string unlockUrl)
        {
            _logger.LogInformation("MOCK LOCKOUT NOTIFICATION: To={To}, UserName={UserName}, LockoutEnd={LockoutEnd}", 
                to, userName, lockoutEnd);
            return Task.FromResult(true);
        }

        public Task<bool> SendSecurityAlertAsync(string to, string userName, string alertMessage)
        {
            _logger.LogInformation("MOCK SECURITY ALERT: To={To}, UserName={UserName}, Alert={Alert}", 
                to, userName, alertMessage);
            return Task.FromResult(true);
        }
    }
}
