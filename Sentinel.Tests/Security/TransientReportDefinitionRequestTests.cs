using System.ComponentModel.DataAnnotations;
using Sentinel.Controllers.Api;

namespace Sentinel.Tests.Security;

public sealed class TransientReportDefinitionRequestTests
{
    [Fact]
    public void ToReportDefinition_MapsOnlyTheTransientQueryShape()
    {
        var request = new TransientReportDefinitionRequest
        {
            EntityType = "Case",
            Fields =
            [
                new TransientReportFieldRequest
                {
                    FieldPath = "FriendlyId",
                    DisplayName = "Case number",
                    DataType = "String"
                }
            ],
            Filters =
            [
                new TransientReportFilterRequest
                {
                    FieldPath = "DateOfNotification",
                    Operator = "GreaterThan",
                    Value = "2026-01-01",
                    DataType = "DateTime",
                    IsDynamicDate = true,
                    DynamicDateType = "Past30Days"
                }
            ]
        };

        var report = request.ToReportDefinition();

        Assert.Equal("Transient report query", report.Name);
        Assert.Equal("Case", report.EntityType);
        Assert.Equal(0, report.Id);
        Assert.Null(report.CreatedByUserId);
        Assert.Null(report.ModifiedByUserId);
        Assert.Null(report.FolderId);
        Assert.False(report.IsPublic);
        Assert.False(report.IsTemplate);
        Assert.Equal(0, report.RunCount);
        Assert.Single(report.Fields);
        Assert.Equal("FriendlyId", report.Fields.Single().FieldPath);
        Assert.Equal(0, report.Fields.Single().DisplayOrder);
        Assert.Single(report.Filters);
        Assert.Equal("DateOfNotification", report.Filters.Single().FieldPath);
        Assert.True(report.Filters.Single().IsDynamicDate);
        Assert.Equal("Past30Days", report.Filters.Single().DynamicDateType);
        Assert.Equal(0, report.Filters.Single().DisplayOrder);
    }

    [Fact]
    public void RequestContract_DoesNotExposePersistedOwnershipAuditOrNavigationProperties()
    {
        var propertyNames = typeof(TransientReportDefinitionRequest)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("Id", propertyNames);
        Assert.DoesNotContain("CreatedByUserId", propertyNames);
        Assert.DoesNotContain("CreatedAt", propertyNames);
        Assert.DoesNotContain("ModifiedByUserId", propertyNames);
        Assert.DoesNotContain("ModifiedAt", propertyNames);
        Assert.DoesNotContain("FolderId", propertyNames);
        Assert.DoesNotContain("Folder", propertyNames);
        Assert.DoesNotContain("IsPublic", propertyNames);
        Assert.DoesNotContain("IsTemplate", propertyNames);
        Assert.DoesNotContain("RunCount", propertyNames);
        Assert.DoesNotContain("LastRunDate", propertyNames);
        Assert.DoesNotContain("CalculatedFields", propertyNames);
    }

    [Fact]
    public void RequestContract_RejectsAnOversizedEntityType()
    {
        var request = new TransientReportDefinitionRequest
        {
            EntityType = new string('a', 51)
        };

        var validationResults = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(validationResults, result =>
            result.MemberNames.Contains(nameof(TransientReportDefinitionRequest.EntityType)));
    }
}
