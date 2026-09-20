using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Sentinel.Services;

/// <summary>
/// Encodes Identity password-reset tokens for safe transport in browser URL
/// fragments and POST form bodies. URL fragments are not included in HTTP
/// requests, server access logs, or referrer headers.
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

    public static string AddToResetLinkFragment(string resetPageUrl, string userId, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resetPageUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var urlWithoutFragment = resetPageUrl.Split('#', 2)[0];
        return $"{urlWithoutFragment}#userId={Uri.EscapeDataString(userId)}&code={Uri.EscapeDataString(Encode(token))}";
    }
}
