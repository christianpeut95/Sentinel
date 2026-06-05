namespace Sentinel.Services.HL7;

public interface IHL7MarkerResolutionService
{
    Task<MarkerResolutionResult> ResolveMarkerFieldsAsync(
        string? testCode,
        string? testName,
        string? qualitativeResult,
        decimal? quantitativeValue,
        string? abnormalFlag,
        string? specimenCode,
        string? specimenText,
        string? specimenCodingSystem,
        string? testMethodCode,
        string? testMethodText,
        string? testMethodCodingSystem,
        bool enableTextSearchFallback = true,
        CancellationToken cancellationToken = default);
}
