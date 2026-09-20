namespace Sentinel.Tests.Security;

/// <summary>
/// Guards configured workflow screens that catch exceptions locally. They must
/// retain the exception in structured server logs, but must not turn its raw
/// message into a user-facing alert or validation error.
/// </summary>
public sealed class UserFacingExceptionDisclosureTests
{
    [Theory]
    [InlineData("Components/CollectionMappingEditor.razor")]
    [InlineData("Components/SurveyMappingEditor.razor")]
    [InlineData("Components/Settings/HL7/UploadSampleStage.razor")]
    [InlineData("Components/Settings/HL7/TestAndActivateStage.razor")]
    [InlineData("Components/Setup/Stages/DatabaseConnectionStage.razor")]
    [InlineData("Components/Setup/Stages/AdminAccountStage.razor")]
    [InlineData("Components/Pages/ConfigureCollectionBlazor.razor")]
    [InlineData("Pages/Patients/Create.cshtml.cs")]
    [InlineData("Services/PatientAddressService.cs")]
    [InlineData("Services/GeocodingBackgroundService.cs")]
    [InlineData("Services/TestDataGeneratorService.cs")]
    public void UserFacingExceptionHandlers_DoNotExposeRawExceptionMessages(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Sentinel", relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.DoesNotContain("ex.Message", source, StringComparison.Ordinal);
        Assert.DoesNotContain("cfEx.Message", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Components/CollectionMappingEditor.razor")]
    [InlineData("Components/SurveyMappingEditor.razor")]
    [InlineData("Components/Settings/HL7/UploadSampleStage.razor")]
    [InlineData("Components/Settings/HL7/TestAndActivateStage.razor")]
    [InlineData("Components/Setup/Stages/DatabaseConnectionStage.razor")]
    [InlineData("Components/Setup/Stages/AdminAccountStage.razor")]
    [InlineData("Components/Pages/ConfigureCollectionBlazor.razor")]
    [InlineData("Pages/Patients/Create.cshtml.cs")]
    [InlineData("Services/PatientAddressService.cs")]
    [InlineData("Services/GeocodingBackgroundService.cs")]
    [InlineData("Services/TestDataGeneratorService.cs")]
    public void UserFacingExceptionHandlers_LogFailuresForInvestigation(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Sentinel", relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.True(
            source.Contains("LogWarning(ex", StringComparison.Ordinal) || source.Contains("LogError(ex", StringComparison.Ordinal) || source.Contains("LogWarning(cfEx", StringComparison.Ordinal),
            $"{relativePath} must retain an exception in server logs before returning a generic user-facing error.");
    }

    private static string GetRepositoryRoot()
    {
        var candidates = new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        };

        foreach (var candidate in candidates)
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root for exception disclosure checks.");
    }
}
