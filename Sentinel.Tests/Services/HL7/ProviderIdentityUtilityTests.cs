using Sentinel.Services.HL7.Models;

namespace Sentinel.Tests.Services.HL7;

public sealed class ProviderIdentityUtilityTests
{
    [Fact]
    public void ParseXcn_StandardValue_ExtractsIdentifierAuthorityAndStructuredName()
    {
        var identity = ProviderIdentityUtility.ParseXcn(
            "A-123^O'Neil&ignored^Anne^Marie^Jr^Dr^MBBS^^AHPRA&1.2.36&ISO");

        Assert.NotNull(identity);
        Assert.Equal("A123", identity.Identifier);
        Assert.Equal("AHPRA", identity.IdentifierAuthority);
        Assert.Equal("Anne", identity.GivenName);
        Assert.Equal("Marie", identity.MiddleName);
        Assert.Equal("O'Neil", identity.FamilyName);
        Assert.Equal("Dr", identity.NamePrefix);
        Assert.Equal("Jr", identity.NameSuffix);
        Assert.Equal("MBBS", identity.Credentials);
        Assert.Equal("Dr Anne Marie O'Neil Jr", identity.DisplayName);
    }

    [Fact]
    public void ParseXcn_LegacyGeneratorValue_RemainsSupported()
    {
        var identity = ProviderIdentityUtility.ParseXcn(
            "Robinson^Richard^^^^^^^1660236056");

        Assert.NotNull(identity);
        Assert.Equal("1660236056", identity.Identifier);
        Assert.Equal("Richard", identity.GivenName);
        Assert.Equal("Robinson", identity.FamilyName);
    }

    [Fact]
    public void ParseXcn_AlphanumericStandardIdentifier_IsNotTreatedAsAFamilyName()
    {
        var identity = ProviderIdentityUtility.ParseXcn(
            "AB-123^Smith^^^^^^^AHPRA&1.2.36&ISO");

        Assert.NotNull(identity);
        Assert.Equal("AB123", identity.Identifier);
        Assert.Equal("Smith", identity.FamilyName);
        Assert.Null(identity.GivenName);
        Assert.Equal("AHPRA", identity.IdentifierAuthority);
    }

    [Fact]
    public void ParseXcn_RepeatingValue_UsesFirstUsableIdentity()
    {
        var identity = ProviderIdentityUtility.ParseXcn("^^^^~ZX-9^Nguyen^Mai");

        Assert.NotNull(identity);
        Assert.Equal("ZX9", identity.Identifier);
        Assert.Equal("Mai", identity.GivenName);
        Assert.Equal("Nguyen", identity.FamilyName);
    }

    [Theory]
    [InlineData(" 12-34 56 ", "123456")]
    [InlineData("ab.c/9", "ABC9")]
    [InlineData("---", null)]
    public void NormalizeIdentifier_ProducesStableComparisonValue(string value, string? expected)
    {
        Assert.Equal(expected, ProviderIdentityUtility.NormalizeIdentifier(value));
    }

    [Theory]
    [InlineData(" ahpra ", "AHPRA")]
    [InlineData("local issuer", "LOCAL ISSUER")]
    [InlineData("\t\r\n", null)]
    public void NormalizeAuthority_ProducesStableComparisonValue(string value, string? expected)
    {
        Assert.Equal(expected, ProviderIdentityUtility.NormalizeAuthority(value));
    }

    [Fact]
    public void NormalizeName_IsCasePunctuationAndDiacriticInsensitive()
    {
        Assert.Equal("ANNE MARIE O NEIL", ProviderIdentityUtility.NormalizeName("  Änne-Marie O'Neil "));
    }

    [Theory]
    [InlineData(null, "Marie", true)]
    [InlineData("M", "Marie", true)]
    [InlineData("Marie", "Michael", false)]
    [InlineData("Marie", "Louise", false)]
    public void MiddleNamesAreCompatible_UsesMissingOrInitialCompatibleMatching(
        string? left,
        string? right,
        bool expected)
    {
        Assert.Equal(expected, ProviderIdentityUtility.MiddleNamesAreCompatible(left, right));
    }
}
