using Sentinel.Services;

namespace Sentinel.Tests.Services;

public sealed class SurveyDefinitionSafetyValidatorTests
{
    [Fact]
    public void TryValidate_AllowsStandardInterviewAndMatrixQuestions()
    {
        const string definition = """
            {
              "title": "Case interview",
              "pages": [
                {
                  "name": "interview",
                  "elements": [
                    { "type": "text", "name": "onset", "title": "Date of onset" },
                    { "type": "matrixdynamic", "name": "contacts", "title": "Household contacts" }
                  ]
                }
              ]
            }
            """;

        var valid = SurveyDefinitionSafetyValidator.TryValidate(definition, out var error);

        Assert.True(valid, error);
    }

    [Fact]
    public void TryValidate_RejectsHtmlSurveyElements()
    {
        const string definition = """
            {
              "title": "Unsafe survey",
              "elements": [
                { "type": "html", "name": "instructions", "html": "<img src=x onerror=alert(1)>" }
              ]
            }
            """;

        var valid = SurveyDefinitionSafetyValidator.TryValidate(definition, out var error);

        Assert.False(valid);
        Assert.Contains("HTML survey elements", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryValidate_RejectsRawHtmlPropertiesOnOtherwiseValidQuestions()
    {
        const string definition = """
            {
              "title": "Unsafe survey",
              "elements": [
                { "type": "text", "name": "name", "htmlContent": "<script>alert(1)</script>" }
              ]
            }
            """;

        var valid = SurveyDefinitionSafetyValidator.TryValidate(definition, out var error);

        Assert.False(valid);
        Assert.Contains("Raw HTML", error, StringComparison.Ordinal);
    }
}
