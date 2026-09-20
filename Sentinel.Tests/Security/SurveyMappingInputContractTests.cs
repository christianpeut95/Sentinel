using System.Reflection;
using Sentinel.Controllers.Api;
using Sentinel.Models;

namespace Sentinel.Tests.Security;

public sealed class SurveyMappingInputContractTests
{
    [Fact]
    public void ValidateEndpoint_UsesTheAllowListedInputContract()
    {
        var endpoint = typeof(SurveyMappingApiController).GetMethod(
            "ValidateMapping",
            BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(endpoint);
        var parameter = Assert.Single(endpoint!.GetParameters());
        Assert.Equal(typeof(SurveyFieldMappingInput), parameter.ParameterType);
        Assert.NotEqual(typeof(SurveyFieldMapping), parameter.ParameterType);
    }

    [Fact]
    public void InputContract_DoesNotExposePersistentIdentityAuditOrNavigationMembers()
    {
        var properties = typeof(SurveyFieldMappingInput)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Id", properties);
        Assert.DoesNotContain("Priority", properties);
        Assert.DoesNotContain("CreatedByUserId", properties);
        Assert.DoesNotContain("CreatedDate", properties);
        Assert.DoesNotContain("LastModifiedByUserId", properties);
        Assert.DoesNotContain("LastModified", properties);
        Assert.DoesNotContain("CreatedBy", properties);
        Assert.DoesNotContain("LastModifiedBy", properties);
        Assert.DoesNotContain("TargetSymptom", properties);
    }
}
