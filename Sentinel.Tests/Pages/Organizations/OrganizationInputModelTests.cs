using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Pages.Organizations;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Sentinel.Tests.Pages.Organizations;

public sealed class OrganizationInputModelTests
{
    [Fact]
    public void Validate_RequiresGivenAndFamilyNamesTogether()
    {
        var input = ValidInput();
        input.GivenName = "Anne";

        var errors = Validate(input);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(OrganizationInputModel.GivenName)) &&
            error.MemberNames.Contains(nameof(OrganizationInputModel.FamilyName)));
    }

    [Fact]
    public void Validate_RequiresIdentifierWhenAuthorityIsProvided()
    {
        var input = ValidInput();
        input.ProviderIdentifierAuthority = "AHPRA";

        var errors = Validate(input);

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(OrganizationInputModel.ProviderIdentifier)));
    }

    [Theory]
    [InlineData("Unsafe\rName")]
    [InlineData("Unsafe\0Name")]
    public void Validate_RejectsControlCharactersInSingleLineFields(string name)
    {
        var input = ValidInput();
        input.Name = name;

        Assert.Contains(Validate(input), error =>
            error.MemberNames.Contains(nameof(OrganizationInputModel.Name)));
    }

    [Fact]
    public void Validate_AllowsLineBreaksButRejectsUnsupportedMultilineControls()
    {
        var input = ValidInput();
        input.Address = "Level 1\nMain Street";
        Assert.Empty(Validate(input));

        input.Address = "Level 1\u0001Main Street";
        Assert.Contains(Validate(input), error =>
            error.MemberNames.Contains(nameof(OrganizationInputModel.Address)));
    }

    [Fact]
    public void ApplyTo_NormalizesProviderIdentifierAndPreservesServerOwnedFields()
    {
        var createdAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var modifiedAt = createdAt.AddDays(1);
        var id = Guid.NewGuid();
        var organization = new Organization
        {
            Id = id,
            FriendlyId = "ORG-001",
            CreatedAt = createdAt,
            ModifiedAt = modifiedAt
        };
        var input = ValidInput();
        input.ProviderIdentifier = " ab-123 ";
        input.ProviderIdentifierAuthority = " ahpra ";
        input.GivenName = " Anne ";
        input.FamilyName = " O'Neil ";

        input.ApplyTo(organization);

        Assert.Equal("AB123", organization.ProviderIdentifier);
        Assert.Equal("AHPRA", organization.ProviderIdentifierAuthority);
        Assert.Equal("Anne", organization.GivenName);
        Assert.Equal("O'Neil", organization.FamilyName);
        Assert.Equal(id, organization.Id);
        Assert.Equal("ORG-001", organization.FriendlyId);
        Assert.Equal(createdAt, organization.CreatedAt);
        Assert.Equal(modifiedAt, organization.ModifiedAt);
    }

    [Fact]
    public async Task ValidateReferences_RejectsInactiveTypeOnCreate()
    {
        await using var context = CreateContext();
        context.OrganizationTypes.Add(new OrganizationType
        {
            Id = 17,
            Name = "Inactive Provider",
            IsActive = false
        });
        await context.SaveChangesAsync();
        var input = ValidInput();
        input.OrganizationTypeId = 17;
        var modelState = new ModelStateDictionary();

        await OrganizationFormSupport.ValidateReferencesAsync(context, input, modelState);

        Assert.False(modelState.IsValid);
        Assert.True(modelState.ContainsKey("Input.OrganizationTypeId"));
    }

    [Fact]
    public async Task ValidateReferences_DetectsNormalizedIdentifierCollisionWithinAuthority()
    {
        await using var context = CreateContext();
        context.Organizations.Add(new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Existing Provider",
            ProviderIdentifier = "AB123",
            ProviderIdentifierAuthority = "AHPRA"
        });
        await context.SaveChangesAsync();
        var input = ValidInput();
        input.ProviderIdentifier = "ab-123";
        input.ProviderIdentifierAuthority = "ahpra";
        var modelState = new ModelStateDictionary();

        await OrganizationFormSupport.ValidateReferencesAsync(context, input, modelState);

        Assert.False(modelState.IsValid);
        Assert.True(modelState.ContainsKey("Input.ProviderIdentifier"));
    }

    private static OrganizationInputModel ValidInput() =>
        new()
        {
            Name = "Example Clinic",
            IsActive = true
        };

    private static List<DataAnnotationsValidationResult> Validate(OrganizationInputModel input)
    {
        var results = new List<DataAnnotationsValidationResult>();
        Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true);
        return results;
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"OrganizationInput_{Guid.NewGuid()}")
            .Options;

        return new ApplicationDbContext(
            options,
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() });
    }
}
