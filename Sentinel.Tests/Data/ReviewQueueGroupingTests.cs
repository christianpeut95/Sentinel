using Sentinel.Data;

namespace Sentinel.Tests.Services;

public sealed class ReviewQueueGroupingTests
{
    [Fact]
    public void GenerateReviewGroupKey_UsesTheConfiguredTimeWindowRatherThanAFixedHourlyBucket()
    {
        var diseaseId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var caseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var key = ApplicationDbContext.GenerateReviewGroupKey(
            "StatusChanged",
            "ConfirmationStatus",
            "{\"newValue\":\"Confirmed\"}",
            diseaseId,
            caseId);

        Assert.Equal(
            "StatusChanged|ConfirmationStatus|22222222-2222-2222-2222-222222222222|Confirmed|11111111-1111-1111-1111-111111111111",
            key);
    }
}
