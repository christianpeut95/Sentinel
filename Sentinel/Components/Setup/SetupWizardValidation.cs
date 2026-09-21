using System.ComponentModel.DataAnnotations;
using Sentinel.Services;

namespace Sentinel.Components.Setup;

/// <summary>
/// Performs server-side validation for data collected by the initial setup
/// wizard. Each stage uses the same checks as the final completion step so a
/// client cannot bypass a required field by navigating directly to Review.
/// </summary>
public static class SetupWizardValidation
{
    public static IReadOnlyList<string> ValidateRequiredConfiguration(
        InitialSetupWizard.AdminAccountData adminData,
        InitialSetupWizard.HttpsConfigData httpsData,
        InitialSetupWizard.OrganizationData organizationData,
        InitialSetupWizard.BackupData backupData,
        string? timeZoneId,
        string? locale)
    {
        var errors = new List<string>();
        ValidateObject(adminData, errors);
        ValidateObject(httpsData, errors);
        ValidateObject(organizationData, errors);
        ValidateObject(backupData, errors);

        if (!Uri.TryCreate(httpsData.ApplicationUrl, UriKind.Absolute, out var applicationUri)
            || (applicationUri.Scheme != Uri.UriSchemeHttp && applicationUri.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add("Application URL must be an absolute HTTP or HTTPS URL.");
        }
        else if (httpsData.EnforceHttps && applicationUri.Scheme != Uri.UriSchemeHttps)
        {
            errors.Add("Application URL must use HTTPS while HTTPS enforcement is enabled.");
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            errors.Add("Time zone is required.");
        }
        else if (!OrganizationRegionalSettings.TryResolveTimeZone(timeZoneId, out _))
        {
            errors.Add("Select a valid time zone.");
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            errors.Add("Date, number and language format is required.");
        }
        else if (!OrganizationRegionalSettings.TryGetCulture(locale, out _))
        {
            errors.Add("Select a valid date, number and language format.");
        }

        if (!OrganizationRegionalSettings.TryGetRegion(organizationData.CountryCode, out _))
        {
            errors.Add("Select a valid country or region.");
        }

        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    public static IReadOnlyList<string> ValidateAdministratorDetails(
        InitialSetupWizard.AdminAccountData adminData)
    {
        var errors = new List<string>();
        ValidateObject(adminData, errors);

        if (!string.Equals(adminData.Password, adminData.ConfirmPassword, StringComparison.Ordinal))
        {
            errors.Add("Passwords do not match.");
        }

        if (adminData.CreateBackupAdmin)
        {
            if (string.IsNullOrWhiteSpace(adminData.BackupEmail)
                || string.IsNullOrWhiteSpace(adminData.BackupFullName)
                || string.IsNullOrWhiteSpace(adminData.BackupPassword))
            {
                errors.Add("Complete all backup administrator fields or turn off backup administrator creation.");
            }

            if (!string.IsNullOrWhiteSpace(adminData.BackupEmail))
            {
                if (!new EmailAddressAttribute().IsValid(adminData.BackupEmail))
                {
                    errors.Add("Enter a valid backup administrator email address.");
                }

                if (adminData.BackupEmail.Equals(adminData.Email, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add("Backup administrator email must be different from the primary administrator email.");
                }
            }
        }

        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static void ValidateObject(object instance, ICollection<string> errors)
    {
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);

        foreach (var result in results)
        {
            errors.Add(result.ErrorMessage ?? "A required setup value is invalid.");
        }
    }
}
