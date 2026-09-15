using Microsoft.AspNetCore.Identity;
using Sentinel.Models;

namespace Sentinel.Services;

public interface ICommonPasswordDenyList
{
    int Count { get; }
    bool Contains(string password);
}

/// <summary>
/// Loads Sentinel's offline common-password deny-list. The list is bundled
/// with the application so password submission never requires a third-party
/// network request.
/// </summary>
public sealed class CommonPasswordDenyList : ICommonPasswordDenyList
{
    public const int MinimumRequiredEntries = 3_000;

    private readonly HashSet<string> _passwords;

    public CommonPasswordDenyList(IWebHostEnvironment environment, ILogger<CommonPasswordDenyList> logger)
    {
        var path = Path.Combine(environment.ContentRootPath, "Security", "common-passwords.txt");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException("The required common-password deny-list is missing.");
        }

        _passwords = new HashSet<string>(
            File.ReadLines(path).Where(password => !string.IsNullOrEmpty(password)),
            StringComparer.OrdinalIgnoreCase);

        if (_passwords.Count < MinimumRequiredEntries)
        {
            throw new InvalidOperationException(
                $"The common-password deny-list must contain at least {MinimumRequiredEntries:N0} entries.");
        }

        logger.LogInformation("Loaded common-password deny-list with {PasswordCount} entries.", _passwords.Count);
    }

    public int Count => _passwords.Count;

    public bool Contains(string password) => _passwords.Contains(password);
}

/// <summary>
/// Identity password validator which blocks known common passwords without
/// logging or transmitting the submitted value.
/// </summary>
public sealed class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private readonly ICommonPasswordDenyList _denyList;

    public CommonPasswordValidator(ICommonPasswordDenyList denyList)
    {
        _denyList = denyList;
    }

    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        if (!string.IsNullOrEmpty(password) && _denyList.Contains(password))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooCommon",
                Description = "This password is too common. Choose a different password."
            }));
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
