using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Services.HL7.Models;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Sentinel.Pages.Organizations;

/// <summary>
/// Explicit allow-list for organization form fields. The persistence entity is
/// never request-bound, which prevents overposting of audit and relationship data.
/// </summary>
public sealed class OrganizationInputModel : IValidatableObject
{
    public Guid Id { get; set; }

    [Required]
    [Display(Name = "Organization Name")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Organization Type")]
    public int? OrganizationTypeId { get; set; }

    [Display(Name = "Address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Phone")]
    [StringLength(50)]
    [DataType(DataType.PhoneNumber)]
    public string? Phone { get; set; }

    [Display(Name = "Email")]
    [StringLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    [Display(Name = "Contact Person")]
    [StringLength(200)]
    public string? ContactPerson { get; set; }

    [Display(Name = "Export Code")]
    [StringLength(50)]
    public string? ExportCode { get; set; }

    [Display(Name = "Provider Identifier")]
    [StringLength(100)]
    public string? ProviderIdentifier { get; set; }

    [Display(Name = "Provider Identifier Authority")]
    [StringLength(100)]
    public string? ProviderIdentifierAuthority { get; set; }

    [Display(Name = "Given Name")]
    [StringLength(100)]
    public string? GivenName { get; set; }

    [Display(Name = "Middle Name")]
    [StringLength(100)]
    public string? MiddleName { get; set; }

    [Display(Name = "Family Name")]
    [StringLength(100)]
    public string? FamilyName { get; set; }

    [Display(Name = "Name Prefix")]
    [StringLength(50)]
    public string? NamePrefix { get; set; }

    [Display(Name = "Name Suffix")]
    [StringLength(50)]
    public string? NameSuffix { get; set; }

    [Display(Name = "Credentials")]
    [StringLength(100)]
    public string? Credentials { get; set; }

    [Display(Name = "Is Active")]
    public bool IsActive { get; set; } = true;

    [Display(Name = "Notes")]
    [StringLength(1000)]
    [DataType(DataType.MultilineText)]
    public string? Notes { get; set; }

    public IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        var hasGivenName = !string.IsNullOrWhiteSpace(GivenName);
        var hasFamilyName = !string.IsNullOrWhiteSpace(FamilyName);

        if (hasGivenName != hasFamilyName)
        {
            yield return new DataAnnotationsValidationResult(
                "Given name and family name must be supplied together.",
                new[] { nameof(GivenName), nameof(FamilyName) });
        }

        if (!string.IsNullOrWhiteSpace(ProviderIdentifierAuthority) &&
            string.IsNullOrWhiteSpace(ProviderIdentifier))
        {
            yield return new DataAnnotationsValidationResult(
                "An identifier is required when an identifier authority is supplied.",
                new[] { nameof(ProviderIdentifier), nameof(ProviderIdentifierAuthority) });
        }

        foreach (var (fieldName, value) in SingleLineValues())
        {
            if (value?.Any(char.IsControl) == true)
            {
                yield return new DataAnnotationsValidationResult(
                    "Control characters are not permitted.",
                    new[] { fieldName });
            }
        }

        foreach (var (fieldName, value) in MultilineValues())
        {
            if (value?.Any(character =>
                    char.IsControl(character) && character is not '\r' and not '\n' and not '\t') == true)
            {
                yield return new DataAnnotationsValidationResult(
                    "Unsupported control characters are not permitted.",
                    new[] { fieldName });
            }
        }
    }

    public void ApplyTo(Organization organization)
    {
        organization.Name = RequiredValue(Name);
        organization.OrganizationTypeId = OrganizationTypeId;
        organization.Address = OptionalMultilineValue(Address);
        organization.Phone = OptionalValue(Phone);
        organization.Email = OptionalValue(Email);
        organization.ContactPerson = OptionalValue(ContactPerson);
        organization.ExportCode = OptionalValue(ExportCode);
        organization.ProviderIdentifier = ProviderIdentityUtility.NormalizeIdentifier(ProviderIdentifier);
        organization.ProviderIdentifierAuthority = ProviderIdentityUtility.NormalizeAuthority(ProviderIdentifierAuthority);
        organization.GivenName = OptionalValue(GivenName);
        organization.MiddleName = OptionalValue(MiddleName);
        organization.FamilyName = OptionalValue(FamilyName);
        organization.NamePrefix = OptionalValue(NamePrefix);
        organization.NameSuffix = OptionalValue(NameSuffix);
        organization.Credentials = OptionalValue(Credentials);
        organization.IsActive = IsActive;
        organization.Notes = OptionalMultilineValue(Notes);
    }

    public static OrganizationInputModel FromOrganization(Organization organization) =>
        new()
        {
            Id = organization.Id,
            Name = organization.Name,
            OrganizationTypeId = organization.OrganizationTypeId,
            Address = organization.Address,
            Phone = organization.Phone,
            Email = organization.Email,
            ContactPerson = organization.ContactPerson,
            ExportCode = organization.ExportCode,
            ProviderIdentifier = organization.ProviderIdentifier,
            ProviderIdentifierAuthority = organization.ProviderIdentifierAuthority,
            GivenName = organization.GivenName,
            MiddleName = organization.MiddleName,
            FamilyName = organization.FamilyName,
            NamePrefix = organization.NamePrefix,
            NameSuffix = organization.NameSuffix,
            Credentials = organization.Credentials,
            IsActive = organization.IsActive,
            Notes = organization.Notes
        };

    private IEnumerable<(string FieldName, string? Value)> SingleLineValues()
    {
        yield return (nameof(Name), Name);
        yield return (nameof(Phone), Phone);
        yield return (nameof(Email), Email);
        yield return (nameof(ContactPerson), ContactPerson);
        yield return (nameof(ExportCode), ExportCode);
        yield return (nameof(ProviderIdentifier), ProviderIdentifier);
        yield return (nameof(ProviderIdentifierAuthority), ProviderIdentifierAuthority);
        yield return (nameof(GivenName), GivenName);
        yield return (nameof(MiddleName), MiddleName);
        yield return (nameof(FamilyName), FamilyName);
        yield return (nameof(NamePrefix), NamePrefix);
        yield return (nameof(NameSuffix), NameSuffix);
        yield return (nameof(Credentials), Credentials);
    }

    private IEnumerable<(string FieldName, string? Value)> MultilineValues()
    {
        yield return (nameof(Address), Address);
        yield return (nameof(Notes), Notes);
    }

    private static string RequiredValue(string value) => value.Trim();
    private static string? OptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? OptionalMultilineValue(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n')
                .Trim();
}

internal static class OrganizationFormSupport
{
    public static async Task<SelectList> LoadOrganizationTypesAsync(
        ApplicationDbContext context,
        int? selectedId,
        CancellationToken cancellationToken = default)
    {
        var types = await context.OrganizationTypes
            .Where(type => type.IsActive || type.Id == selectedId)
            .OrderBy(type => type.DisplayOrder)
            .ThenBy(type => type.Name)
            .ToListAsync(cancellationToken);

        return new SelectList(types, "Id", "Name", selectedId);
    }

    public static async Task ValidateReferencesAsync(
        ApplicationDbContext context,
        OrganizationInputModel input,
        ModelStateDictionary modelState,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        int? existingTypeId = null;
        if (organizationId.HasValue)
        {
            existingTypeId = await context.Organizations
                .Where(organization => organization.Id == organizationId.Value)
                .Select(organization => organization.OrganizationTypeId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (input.OrganizationTypeId.HasValue &&
            !await context.OrganizationTypes.AnyAsync(
                type => type.Id == input.OrganizationTypeId.Value &&
                        (type.IsActive || type.Id == existingTypeId),
                cancellationToken))
        {
            modelState.AddModelError(
                $"Input.{nameof(input.OrganizationTypeId)}",
                "Select a valid organization type.");
        }

        var normalizedIdentifier = ProviderIdentityUtility.NormalizeIdentifier(input.ProviderIdentifier);
        if (normalizedIdentifier == null)
            return;

        var candidates = await context.Organizations
            .Where(organization =>
                organization.ProviderIdentifier == normalizedIdentifier &&
                (!organizationId.HasValue || organization.Id != organizationId.Value))
            .Select(organization => organization.ProviderIdentifierAuthority)
            .ToListAsync(cancellationToken);

        var normalizedAuthority = ProviderIdentityUtility.NormalizeAuthority(input.ProviderIdentifierAuthority);
        if (candidates.Any(authority =>
            {
                var candidateAuthority = ProviderIdentityUtility.NormalizeAuthority(authority);
                return normalizedAuthority == null ||
                       candidateAuthority == null ||
                       candidateAuthority == normalizedAuthority;
            }))
        {
            modelState.AddModelError(
                $"Input.{nameof(input.ProviderIdentifier)}",
                "That provider identifier is already assigned to another organization with the same authority.");
        }
    }
}
