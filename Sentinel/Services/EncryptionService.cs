using Microsoft.AspNetCore.DataProtection;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive configuration data
    /// Uses ASP.NET Core Data Protection API for secure, keys-based encryption
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypt a plaintext string
        /// </summary>
        /// <param name="plainText">The text to encrypt</param>
        /// <returns>Base64-encoded encrypted text</returns>
        string Encrypt(string plainText);

        /// <summary>
        /// Decrypt an encrypted string
        /// </summary>
        /// <param name="cipherText">Base64-encoded encrypted text</param>
        /// <returns>Decrypted plaintext</returns>
        string Decrypt(string cipherText);

        /// <summary>
        /// Securely hash a value (one-way, for tokens/passwords)
        /// </summary>
        /// <param name="value">Value to hash</param>
        /// <returns>Hashed value (SHA256)</returns>
        string Hash(string value);

        /// <summary>
        /// Verify a value matches a hash
        /// </summary>
        /// <param name="value">Plain value to check</param>
        /// <param name="hash">Hash to compare against</param>
        /// <returns>True if value matches hash</returns>
        bool VerifyHash(string value, string hash);
    }

    /// <summary>
    /// Implementation of encryption service using Data Protection API
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EncryptionService> logger)
        {
            // Create a protector with a specific purpose string
            // This ensures keys are isolated for Sentinel's use
            _protector = dataProtectionProvider.CreateProtector("Sentinel.SensitiveData.v1");
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                throw new ArgumentException("Cannot encrypt null or empty string", nameof(plainText));
            }

            try
            {
                var encrypted = _protector.Protect(plainText);
                return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(encrypted));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt data");
                throw new InvalidOperationException("Encryption failed", ex);
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                throw new ArgumentException("Cannot decrypt null or empty string", nameof(cipherText));
            }

            try
            {
                var encrypted = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cipherText));
                return _protector.Unprotect(encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt data - data may be corrupted or keys changed");
                throw new InvalidOperationException("Decryption failed", ex);
            }
        }

        public string Hash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Cannot hash null or empty string", nameof(value));
            }

            try
            {
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var bytes = System.Text.Encoding.UTF8.GetBytes(value);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to hash data");
                throw new InvalidOperationException("Hashing failed", ex);
            }
        }

        public bool VerifyHash(string value, string hash)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(hash))
            {
                return false;
            }

            try
            {
                var computedHash = Hash(value);
                return computedHash.Equals(hash, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify hash");
                return false;
            }
        }
    }
}
