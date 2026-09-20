using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class PasswordResetTokenEncodingTests
{
    [Fact]
    public void Encode_ProducesAUrlSafeRoundTrippableValue()
    {
        const string token = "CfDJ8A+g/9=z?Token with spaces&symbols";

        var encoded = PasswordResetTokenEncoding.Encode(token);

        Assert.DoesNotContain('+', encoded);
        Assert.DoesNotContain('/', encoded);
        Assert.DoesNotContain('=', encoded);
        Assert.True(PasswordResetTokenEncoding.TryDecode(encoded, out var decoded));
        Assert.Equal(token, decoded);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a valid token!")]
    public void TryDecode_RejectsMissingOrInvalidInput(string? encoded)
    {
        Assert.False(PasswordResetTokenEncoding.TryDecode(encoded, out var decoded));
        Assert.Equal(string.Empty, decoded);
    }

    [Fact]
    public void AddToResetLinkFragment_KeepsTheResetSecretOutOfTheRequestUrl()
    {
        const string token = "CfDJ8A+g/9=z?Token with spaces&symbols";
        const string userId = "user id";

        var link = PasswordResetTokenEncoding.AddToResetLinkFragment(
            "https://sentinel.example/Identity/Account/ResetPassword?area=Identity",
            userId,
            token);

        var uri = new Uri(link);
        Assert.Equal("?area=Identity", uri.Query);
        Assert.DoesNotContain("code=", uri.Query, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("userId=", uri.Query, StringComparison.OrdinalIgnoreCase);

        var fragmentValues = uri.Fragment
            .TrimStart('#')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary(
                part => Uri.UnescapeDataString(part[0]),
                part => part.Length == 2 ? Uri.UnescapeDataString(part[1]) : string.Empty,
                StringComparer.Ordinal);
        Assert.Equal(userId, fragmentValues["userId"]);
        Assert.True(PasswordResetTokenEncoding.TryDecode(fragmentValues["code"], out var decoded));
        Assert.Equal(token, decoded);
    }
}
