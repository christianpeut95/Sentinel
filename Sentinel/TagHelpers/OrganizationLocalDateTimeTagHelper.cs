using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Sentinel.ModelBinding;
using Sentinel.Services;

namespace Sentinel.TagHelpers;

/// <summary>
/// Renders explicitly marked UTC timestamps as organisation-local values in an
/// HTML datetime-local input. The paired model binder converts them back to UTC.
/// </summary>
[HtmlTargetElement("input", Attributes = "asp-for")]
public sealed class OrganizationLocalDateTimeTagHelper : TagHelper
{
    private readonly IApplicationTimeZoneService _regionalSettings;

    public OrganizationLocalDateTimeTagHelper(IApplicationTimeZoneService regionalSettings) =>
        _regionalSettings = regionalSettings;

    [HtmlAttributeName("asp-for")]
    public ModelExpression For { get; set; } = default!;

    // Run after the built-in InputTagHelper has populated type and value.
    public override int Order => 1000;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var property = For.Metadata.ContainerType?.GetProperty(For.Metadata.PropertyName ?? string.Empty);
        if (property?.GetCustomAttributes(typeof(OrganizationLocalDateTimeAttribute), inherit: true).Length != 1
            || !string.Equals(output.Attributes["type"]?.Value?.ToString(), "datetime-local", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Nullable<DateTime> boxes as DateTime when it has a value.
        if (For.Model is DateTime value)
        {
            output.Attributes.SetAttribute("value", _regionalSettings.ToDateTimeLocalValue(value));
        }
    }
}
