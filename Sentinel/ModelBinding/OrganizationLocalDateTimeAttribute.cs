using Microsoft.AspNetCore.Mvc;

namespace Sentinel.ModelBinding;

/// <summary>
/// Marks an instant that is edited through an HTML datetime-local control.
/// The browser submits it as organisation-local civil time; the binder stores
/// the corresponding UTC value.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OrganizationLocalDateTimeAttribute : ModelBinderAttribute
{
    public OrganizationLocalDateTimeAttribute()
        : base(typeof(OrganizationLocalDateTimeModelBinder))
    {
    }
}
