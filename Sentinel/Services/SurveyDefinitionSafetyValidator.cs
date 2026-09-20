using System.Text.Json;

namespace Sentinel.Services;

/// <summary>
/// Applies the small, security-relevant subset of SurveyJS definition rules that Sentinel owns.
/// Survey templates are configuration, but they are later rendered in other users' browsers.
/// </summary>
public static class SurveyDefinitionSafetyValidator
{
    public const int MaximumDefinitionLength = 1_000_000;

    public static bool TryValidate(string? surveyDefinitionJson, out string error)
    {
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(surveyDefinitionJson))
        {
            error = "Survey definition is required.";
            return false;
        }

        if (surveyDefinitionJson.Length > MaximumDefinitionLength)
        {
            error = "Survey definition is too large.";
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(surveyDefinitionJson, new JsonDocumentOptions
            {
                MaxDepth = 64
            });

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Survey JSON must contain an object at its root.";
                return false;
            }

            if (!document.RootElement.TryGetProperty("title", out _) &&
                !document.RootElement.TryGetProperty("elements", out _) &&
                !document.RootElement.TryGetProperty("pages", out _))
            {
                error = "Survey JSON must contain a 'title' and either 'elements' or 'pages' property.";
                return false;
            }

            return TryValidateElement(document.RootElement, "$", 0, out error);
        }
        catch (JsonException)
        {
            error = "The survey definition is not valid JSON. Correct it and try again.";
            return false;
        }
    }

    private static bool TryValidateElement(JsonElement element, string path, int depth, out string error)
    {
        error = string.Empty;

        if (depth > 48)
        {
            error = "Survey definition is nested too deeply.";
            return false;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                if (element.TryGetProperty("type", out var type) &&
                    type.ValueKind == JsonValueKind.String &&
                    string.Equals(type.GetString(), "html", StringComparison.OrdinalIgnoreCase))
                {
                    error = "HTML survey elements are not permitted. Use a text or comment question for instructions.";
                    return false;
                }

                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, "html", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(property.Name, "htmlContent", StringComparison.OrdinalIgnoreCase))
                    {
                        error = "Raw HTML is not permitted in survey definitions.";
                        return false;
                    }

                    if (!TryValidateElement(property.Value, $"{path}.{property.Name}", depth + 1, out error))
                    {
                        return false;
                    }
                }

                return true;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (!TryValidateElement(item, $"{path}[{index++}]", depth + 1, out error))
                    {
                        return false;
                    }
                }

                return true;

            default:
                return true;
        }
    }
}
