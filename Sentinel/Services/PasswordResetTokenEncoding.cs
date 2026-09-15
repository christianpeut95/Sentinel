using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Sentinel.Services;

/// <summary>
/// Encodes Identity password-reset tokens for safe transport in URL query strings.
/// </summary>
public static class PasswordResetTokenEncoding
{
    public static string Encode(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    public static bool TryDecode(string? encodedToken, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(encodedToken))
        {
            return false;
        }

        try
        {
            token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
            return !string.IsNullOrWhiteSpace(token);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
