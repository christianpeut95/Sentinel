using System.Threading.Tasks;

namespace Sentinel.Services.Email
{
    /// <summary>
    /// Interface for email services in Sentinel
    /// Supports SMTP-based email delivery for password resets, notifications, and alerts
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send an email to a recipient
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body content</param>
        /// <param name="isHtml">Whether the body is HTML formatted (default: true)</param>
        /// <returns>True if email was sent successfully, false otherwise</returns>
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

        /// <summary>
        /// Send a password reset email with a secure reset link
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="resetLink">Password reset URL</param>
        /// <param name="userName">User's display name</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendPasswordResetEmailAsync(string to, string resetLink, string userName);

        /// <summary>
        /// Send a welcome email to a new user
        /// </summary>
        /// <param name="to">User's email address</param>
        /// <param name="userName">User's display name</param>
        /// <param name="tempPassword">Temporary password (if applicable)</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendWelcomeEmailAsync(string to, string userName, string? tempPassword = null);

        /// <summary>
        /// Send a test email to verify SMTP configuration
        /// </summary>
        /// <param name="to">Test recipient email address</param>
        /// <returns>True if test email was sent successfully</returns>
        Task<bool> SendTestEmailAsync(string to);

        /// <summary>
        /// Test SMTP connection without sending an email
        /// </summary>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync();

        /// <summary>
        /// Send an account lockout notification
        /// </summary>
        /// <param name="to">User's email address</param>
        /// <param name="userName">User's display name</param>
        /// <param name="lockoutEnd">When the lockout expires</param>
        /// <param name="unlockUrl">URL to request early unlock</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendLockoutNotificationAsync(string to, string userName, DateTimeOffset lockoutEnd, string unlockUrl);

        /// <summary>
        /// Send a security alert (e.g., suspicious login, password changed)
        /// </summary>
        /// <param name="to">User's email address</param>
        /// <param name="userName">User's display name</param>
        /// <param name="alertMessage">Security alert details</param>
        /// <returns>True if email was sent successfully</returns>
        Task<bool> SendSecurityAlertAsync(string to, string userName, string alertMessage);
    }
}
