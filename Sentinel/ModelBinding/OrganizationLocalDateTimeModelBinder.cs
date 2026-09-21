using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Sentinel.Services;

namespace Sentinel.ModelBinding;

/// <summary>
/// Converts explicit datetime-local form fields from the configured
/// organisation time zone to UTC. It rejects invalid or ambiguous daylight-saving times
/// instead of silently moving an entered event or exposure.
/// </summary>
public sealed class OrganizationLocalDateTimeModelBinder : IModelBinder
{
    private static readonly string[] SupportedFormats =
    [
        "yyyy-MM-ddTHH:mm",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.FFFFFFF"
    ];

    private readonly IApplicationTimeZoneService _regionalSettings;

    public OrganizationLocalDateTimeModelBinder(IApplicationTimeZoneService regionalSettings) =>
        _regionalSettings = regionalSettings;

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var rawValue = valueResult.FirstValue;
        var isNullable = Nullable.GetUnderlyingType(bindingContext.ModelType) == typeof(DateTime);

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (isNullable)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "A date and time is required.");
            }

            return Task.CompletedTask;
        }

        if (!DateTime.TryParseExact(
                rawValue,
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var organisationLocalTime))
        {
            bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, "Enter a valid date and time.");
            return Task.CompletedTask;
        }

        if (!_regionalSettings.TryAppTimeToUtc(organisationLocalTime, out var utcTime))
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "This local time is invalid or ambiguous because of the daylight-saving time change. Select another time.");
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(utcTime);
        return Task.CompletedTask;
    }
}
