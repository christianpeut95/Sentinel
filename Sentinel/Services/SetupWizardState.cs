namespace Sentinel.Services;

/// <summary>
/// Holds the validated initial-setup token for the lifetime of one interactive
/// setup wizard circuit. The token is never sent back to the browser after the
/// initial validation stage.
/// </summary>
public interface ISetupWizardState
{
    bool HasValidatedToken { get; }
    string? ValidatedToken { get; }

    void SetValidatedToken(string token);
    void Clear();
}

public sealed class SetupWizardState : ISetupWizardState
{
    public bool HasValidatedToken => !string.IsNullOrEmpty(ValidatedToken);

    public string? ValidatedToken { get; private set; }

    public void SetValidatedToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("A validated setup token is required.", nameof(token));
        }

        ValidatedToken = token.Trim();
    }

    public void Clear() => ValidatedToken = null;
}
