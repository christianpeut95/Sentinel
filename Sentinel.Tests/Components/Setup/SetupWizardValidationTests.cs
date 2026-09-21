using Sentinel.Components.Setup;

namespace Sentinel.Tests.Components.Setup;

public class SetupWizardValidationTests
{
    [Fact]
    public void ValidateRequiredConfiguration_RejectsBlankAdministratorAndRequiredSetupFields()
    {
        var organization = new InitialSetupWizard.OrganizationData
        {
            CountryCode = ""
        };

        var errors = SetupWizardValidation.ValidateRequiredConfiguration(
            new InitialSetupWizard.AdminAccountData(),
            new InitialSetupWizard.HttpsConfigData(),
            organization,
            new InitialSetupWizard.BackupData(),
            timeZoneId: "",
            locale: "");

        Assert.Contains("Administrator email address is required.", errors);
        Assert.Contains("Administrator password is required.", errors);
        Assert.Contains("Administrator full name is required.", errors);
        Assert.Contains("Application URL is required.", errors);
        Assert.Contains("Organization name is required.", errors);
        Assert.Contains("Country/region is required.", errors);
        Assert.Contains("Backup directory path is required.", errors);
        Assert.Contains("Time zone is required.", errors);
        Assert.Contains("Date, number and language format is required.", errors);
    }

    [Fact]
    public void ValidateRequiredConfiguration_RequiresHttpsWhenHttpsEnforcementIsEnabled()
    {
        var errors = SetupWizardValidation.ValidateRequiredConfiguration(
            ValidAdmin(),
            new InitialSetupWizard.HttpsConfigData
            {
                ApplicationUrl = "http://sentinel.example.test",
                EnforceHttps = true
            },
            ValidOrganization(),
            ValidBackup(),
            "Etc/UTC",
            "en-AU");

        Assert.Contains("Application URL must use HTTPS while HTTPS enforcement is enabled.", errors);
    }

    [Fact]
    public void ValidateAdministratorDetails_RequiresCompleteAndDistinctBackupAdministrator()
    {
        var data = ValidAdmin();
        data.CreateBackupAdmin = true;
        data.BackupEmail = data.Email;
        data.BackupFullName = "";
        data.BackupPassword = "";

        var errors = SetupWizardValidation.ValidateAdministratorDetails(data);

        Assert.Contains("Complete all backup administrator fields or turn off backup administrator creation.", errors);
        Assert.Contains("Backup administrator email must be different from the primary administrator email.", errors);
    }

    [Fact]
    public void ValidateRequiredConfiguration_AcceptsACompleteRequiredConfiguration()
    {
        var errors = SetupWizardValidation.ValidateRequiredConfiguration(
            ValidAdmin(),
            new InitialSetupWizard.HttpsConfigData
            {
                ApplicationUrl = "https://sentinel.example.test",
                EnforceHttps = true
            },
            ValidOrganization(),
            ValidBackup(),
            "Etc/UTC",
            "en-AU");

        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateRequiredConfiguration_RejectsAnUnknownTimeZone()
    {
        var errors = SetupWizardValidation.ValidateRequiredConfiguration(
            ValidAdmin(),
            new InitialSetupWizard.HttpsConfigData
            {
                ApplicationUrl = "https://sentinel.example.test",
                EnforceHttps = true
            },
            ValidOrganization(),
            ValidBackup(),
            "_timeZoneId",
            "en-AU");

        Assert.Contains("Select a valid time zone.", errors);
    }

    [Fact]
    public void ValidateRequiredConfiguration_RejectsUnknownLocaleAndCountryCode()
    {
        var organization = ValidOrganization();
        organization.CountryCode = "ZZ";

        var errors = SetupWizardValidation.ValidateRequiredConfiguration(
            ValidAdmin(),
            new InitialSetupWizard.HttpsConfigData
            {
                ApplicationUrl = "https://sentinel.example.test",
                EnforceHttps = true
            },
            organization,
            ValidBackup(),
            "Etc/UTC",
            "not-a-locale");

        Assert.Contains("Select a valid date, number and language format.", errors);
        Assert.Contains("Select a valid country or region.", errors);
    }

    private static InitialSetupWizard.AdminAccountData ValidAdmin() => new()
    {
        Email = "admin@example.test",
        Password = "correct horse battery staple",
        ConfirmPassword = "correct horse battery staple",
        FullName = "Setup Administrator"
    };

    private static InitialSetupWizard.OrganizationData ValidOrganization() => new()
    {
        Name = "Example Health Department",
        Country = "Australia",
        CountryCode = "AU"
    };

    private static InitialSetupWizard.BackupData ValidBackup() => new()
    {
        BackupPath = "/var/lib/sentinel/backups",
        RetentionDays = 30
    };
}
