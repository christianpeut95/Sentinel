using Sentinel.Services.Telemetry;

namespace Sentinel.Tests.Services.Telemetry;

public class SemanticPageIdentifierTests
{
    [Theory]
    [InlineData("/identity/account/login", "Identity.Account.Login")]
    [InlineData("/not-found", "NotFound")]
    [InlineData("/cases/addlabresult/{id}", "Cases.AddLabResult")]
    [InlineData("/dashboard", "Dashboard")]
    [InlineData("/dashboard/mytasks", "Dashboard.MyTasks")]
    [InlineData("/", "Home")]
    [InlineData("/settings/index", "Settings.Index")]
    [InlineData("/cases/details", "Cases.Details")]
    [InlineData("/admin/logs", "Admin.Logs")]
    [InlineData("/cases/details/123?tab=lab", "Cases.Details")]
    public void FromPath_ConvertsKnownRouteTemplatesToSemanticIdentifiers(string path, string expected)
    {
        var pageIdentifier = SemanticPageIdentifier.FromPath(path);

        Assert.Equal(expected, pageIdentifier);
    }
}
