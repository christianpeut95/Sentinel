using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Pathogens;
using Sentinel.Models.Lookups;
using System.Text.Json;

namespace Sentinel.Services.HL7;

/// <summary>
/// Centralizes resolution of pathogen/biomarker ID, test type, specimen type, and result
/// from HL7 data BEFORE any case definition matching or evaluation
/// </summary>
public class HL7MarkerResolutionService : IHL7MarkerResolutionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HL7MarkerResolutionService> _logger;

    public HL7MarkerResolutionService(
        ApplicationDbContext context,
        ILogger<HL7MarkerResolutionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Resolves all four key marker fields from HL7 data
    /// </summary>
    public async Task<MarkerResolutionResult> ResolveMarkerFieldsAsync(
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
        CancellationToken cancellationToken = default)
    {
        var result = new MarkerResolutionResult();

        // 1. Resolve Pathogen/Biomarker ID
        result.PathogenId = await ResolvePathogenAsync(
            testCode,
            testName,
            enableTextSearchFallback,
            cancellationToken);

        // 2. Resolve Test Type/Method
        result.TestMethodId = await ResolveTestMethodAsync(
            testMethodCode,
            testMethodText,
            testMethodCodingSystem,
            enableTextSearchFallback,
            cancellationToken);

        // 3. Resolve Specimen Type
        result.SpecimenTypeId = await ResolveSpecimenTypeAsync(
            specimenCode,
            specimenText,
            specimenCodingSystem,
            enableTextSearchFallback,
            cancellationToken);

        // 4. Resolve Result Interpretation
        var resultResolution = await ResolveResultInterpretationAsync(
            qualitativeResult,
            quantitativeValue,
            abnormalFlag,
            enableTextSearchFallback,
            cancellationToken);

        result.TestResultId = resultResolution.TestResultId;
        result.QuantitativeValue = resultResolution.QuantitativeValue;

        return result;
    }

    #region Private Resolution Methods

    private async Task<Guid?> ResolvePathogenAsync(
        string? testCode,
        string? testName,
        bool enableTextFallback,
        CancellationToken cancellationToken)
    {
        // STRATEGY 1: Match by LOINC code
        if (!string.IsNullOrWhiteSpace(testCode))
        {
            var pathogenByCode = await _context.Pathogens
                .IgnoreQueryFilters()
                .Where(p => p.LOINCCode != null && p.LOINCCode == testCode && p.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (pathogenByCode != null)
            {
                _logger.LogDebug("[PATHOGEN] Matched by LOINC code {TestCode} → {PathogenName}", testCode, pathogenByCode.Name);
                return pathogenByCode.Id;
            }
        }

        // STRATEGY 2: Fuzzy text search on pathogen name (if enabled)
        if (enableTextFallback && !string.IsNullOrWhiteSpace(testName))
        {
            var normalizedSearch = NormalizeText(testName);
            var pathogens = await _context.Pathogens
                .IgnoreQueryFilters()
                .Where(p => p.IsActive)
                .ToListAsync(cancellationToken);

            var matchedPathogen = pathogens.FirstOrDefault(p =>
                !string.IsNullOrWhiteSpace(p.Name) &&
                (NormalizeText(p.Name).Contains(normalizedSearch) ||
                 normalizedSearch.Contains(NormalizeText(p.Name))));

            if (matchedPathogen != null)
            {
                _logger.LogDebug("[PATHOGEN] Fuzzy text match '{TestName}' → {PathogenName}", testName, matchedPathogen.Name);
                return matchedPathogen.Id;
            }
        }

        _logger.LogDebug("[PATHOGEN] No match found for TestCode='{TestCode}', TestName='{TestName}'",
            testCode ?? "NULL", testName ?? "NULL");
        return null;
    }

    private async Task<int?> ResolveTestMethodAsync(
        string? methodCode,
        string? methodText,
        string? codingSystem,
        bool enableTextFallback,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(methodCode) && string.IsNullOrWhiteSpace(methodText))
            return null;

        var testMethods = await _context.Set<TestMethod>()
            .IgnoreQueryFilters()
            .Where(tm => tm.IsActive)
            .ToListAsync(cancellationToken);

        if (!testMethods.Any())
            return null;

        // STRATEGY 1: Exact code match
        if (!string.IsNullOrWhiteSpace(methodCode))
        {
            var exactMatch = testMethods.FirstOrDefault(tm =>
                !string.IsNullOrWhiteSpace(tm.SnomedCode) &&
                tm.SnomedCode.Equals(methodCode, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                _logger.LogDebug("[TEST METHOD] Code match {Code} → {Name}", methodCode, exactMatch.Name);
                return exactMatch.Id;
            }
        }

        // STRATEGY 2: Text-based fuzzy matching (if enabled)
        if (enableTextFallback && !string.IsNullOrWhiteSpace(methodText))
        {
            var normalizedText = NormalizeText(methodText);

            var textMatch = testMethods.FirstOrDefault(tm =>
            {
                var normalizedName = NormalizeText(tm.Name);
                return normalizedName.Contains(normalizedText) || normalizedText.Contains(normalizedName);
            });

            if (textMatch != null)
            {
                _logger.LogDebug("[TEST METHOD] Text match '{Text}' → {Name}", methodText, textMatch.Name);
                return textMatch.Id;
            }

            // Try SNOMED display match
            var snomedMatch = testMethods.FirstOrDefault(tm =>
                !string.IsNullOrWhiteSpace(tm.SnomedDisplay) &&
                NormalizeText(tm.SnomedDisplay).Contains(normalizedText));

            if (snomedMatch != null)
            {
                _logger.LogDebug("[TEST METHOD] SNOMED display match '{Text}' → {Name}", methodText, snomedMatch.Name);
                return snomedMatch.Id;
            }
        }

        _logger.LogDebug("[TEST METHOD] No match found for Code='{Code}', Text='{Text}'",
            methodCode ?? "NULL", methodText ?? "NULL");
        return null;
    }

    private async Task<int?> ResolveSpecimenTypeAsync(
        string? specimenCode,
        string? specimenText,
        string? codingSystem,
        bool enableTextFallback,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(specimenCode) && string.IsNullOrWhiteSpace(specimenText))
            return null;

        var specimenTypes = await _context.Set<SpecimenType>()
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        if (!specimenTypes.Any())
            return null;

        // STRATEGY 1: Exact SNOMED CT code match
        if (!string.IsNullOrWhiteSpace(specimenCode) &&
            (codingSystem?.Equals("SCT", StringComparison.OrdinalIgnoreCase) == true ||
             codingSystem?.Equals("SNOMED CT", StringComparison.OrdinalIgnoreCase) == true))
        {
            var snomedMatch = specimenTypes.FirstOrDefault(st =>
                !string.IsNullOrWhiteSpace(st.SnomedCode) &&
                st.SnomedCode == specimenCode);

            if (snomedMatch != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] SNOMED code match {Code} → {Name}", specimenCode, snomedMatch.Name);
                return snomedMatch.Id;
            }
        }

        // STRATEGY 2: HL7 v2 code match
        if (!string.IsNullOrWhiteSpace(specimenCode))
        {
            var hl7Match = specimenTypes.FirstOrDefault(st =>
                !string.IsNullOrWhiteSpace(st.Hl7Code) &&
                st.Hl7Code.Equals(specimenCode, StringComparison.OrdinalIgnoreCase));

            if (hl7Match != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] HL7 code match {Code} → {Name}", specimenCode, hl7Match.Name);
                return hl7Match.Id;
            }
        }

        // STRATEGY 3: LOINC system code match
        if (!string.IsNullOrWhiteSpace(specimenCode))
        {
            var loincMatch = specimenTypes.FirstOrDefault(st =>
                !string.IsNullOrWhiteSpace(st.LoincSystemCode) &&
                st.LoincSystemCode.Equals(specimenCode, StringComparison.OrdinalIgnoreCase));

            if (loincMatch != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] LOINC code match {Code} → {Name}", specimenCode, loincMatch.Name);
                return loincMatch.Id;
            }
        }

        // STRATEGY 4: Text-based fuzzy matching (if enabled)
        if (enableTextFallback && !string.IsNullOrWhiteSpace(specimenText))
        {
            var normalizedText = NormalizeText(specimenText);

            // Try exact normalized match
            var exactTextMatch = specimenTypes.FirstOrDefault(st =>
                NormalizeText(st.Name) == normalizedText);

            if (exactTextMatch != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] Exact text match '{Text}' → {Name}", specimenText, exactTextMatch.Name);
                return exactTextMatch.Id;
            }

            // Try SNOMED Display match
            var snomedDisplayMatch = specimenTypes.FirstOrDefault(st =>
                !string.IsNullOrWhiteSpace(st.SnomedDisplay) &&
                NormalizeText(st.SnomedDisplay) == normalizedText);

            if (snomedDisplayMatch != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] SNOMED display match '{Text}' → {Name}", specimenText, snomedDisplayMatch.Name);
                return snomedDisplayMatch.Id;
            }

            // Try fuzzy match (contains)
            var fuzzyMatch = specimenTypes.FirstOrDefault(st =>
            {
                var normalizedName = NormalizeText(st.Name);
                return normalizedName.Contains(normalizedText) || normalizedText.Contains(normalizedName);
            });

            if (fuzzyMatch != null)
            {
                _logger.LogDebug("[SPECIMEN TYPE] Fuzzy text match '{Text}' → {Name}", specimenText, fuzzyMatch.Name);
                return fuzzyMatch.Id;
            }
        }

        _logger.LogDebug("[SPECIMEN TYPE] No match found for Code='{Code}', Text='{Text}'",
            specimenCode ?? "NULL", specimenText ?? "NULL");
        return null;
    }

    private async Task<ResultInterpretationResolution> ResolveResultInterpretationAsync(
        string? qualitativeResult,
        decimal? quantitativeValue,
        string? abnormalFlag,
        bool enableTextFallback,
        CancellationToken cancellationToken)
    {
        var resolution = new ResultInterpretationResolution
        {
            QuantitativeValue = quantitativeValue
        };

        // If no qualitative result and no abnormal flag, return quantitative value only
        if (string.IsNullOrWhiteSpace(qualitativeResult) && string.IsNullOrWhiteSpace(abnormalFlag))
        {
            return resolution;
        }

        // Try to match qualitative result to TestResult lookup
        var testResults = await _context.Set<TestResult>()
            .IgnoreQueryFilters()
            .Where(tr => tr.IsActive)
            .ToListAsync(cancellationToken);

        if (!testResults.Any())
        {
            _logger.LogDebug("[RESULT] No active TestResult entries found in database");
            return resolution;
        }

        // STRATEGY 1: Match by HL7 code
        if (!string.IsNullOrWhiteSpace(qualitativeResult))
        {
            var hl7Match = testResults.FirstOrDefault(tr =>
                !string.IsNullOrWhiteSpace(tr.Hl7Code) &&
                tr.Hl7Code.Equals(qualitativeResult, StringComparison.OrdinalIgnoreCase));

            if (hl7Match != null)
            {
                _logger.LogDebug("[RESULT] HL7 code match '{Result}' → {Name}", qualitativeResult, hl7Match.Name);
                resolution.TestResultId = hl7Match.Id;
                return resolution;
            }
        }

        // STRATEGY 2: Match by SNOMED code
        if (!string.IsNullOrWhiteSpace(qualitativeResult))
        {
            var snomedMatch = testResults.FirstOrDefault(tr =>
                !string.IsNullOrWhiteSpace(tr.SnomedCode) &&
                tr.SnomedCode.Equals(qualitativeResult, StringComparison.OrdinalIgnoreCase));

            if (snomedMatch != null)
            {
                _logger.LogDebug("[RESULT] SNOMED code match '{Result}' → {Name}", qualitativeResult, snomedMatch.Name);
                resolution.TestResultId = snomedMatch.Id;
                return resolution;
            }
        }

        // STRATEGY 3: Text-based matching (if enabled)
        if (enableTextFallback && !string.IsNullOrWhiteSpace(qualitativeResult))
        {
            var normalizedResult = NormalizeText(qualitativeResult);

            // Try exact name match
            var exactMatch = testResults.FirstOrDefault(tr =>
                NormalizeText(tr.Name) == normalizedResult);

            if (exactMatch != null)
            {
                _logger.LogDebug("[RESULT] Exact text match '{Result}' → {Name}", qualitativeResult, exactMatch.Name);
                resolution.TestResultId = exactMatch.Id;
                return resolution;
            }

            // Try SNOMED display match
            var displayMatch = testResults.FirstOrDefault(tr =>
                !string.IsNullOrWhiteSpace(tr.SnomedDisplay) &&
                NormalizeText(tr.SnomedDisplay) == normalizedResult);

            if (displayMatch != null)
            {
                _logger.LogDebug("[RESULT] SNOMED display match '{Result}' → {Name}", qualitativeResult, displayMatch.Name);
                resolution.TestResultId = displayMatch.Id;
                return resolution;
            }

            // Try fuzzy match
            var fuzzyMatch = testResults.FirstOrDefault(tr =>
            {
                var normalizedName = NormalizeText(tr.Name);
                return normalizedName.Contains(normalizedResult) || normalizedResult.Contains(normalizedName);
            });

            if (fuzzyMatch != null)
            {
                _logger.LogDebug("[RESULT] Fuzzy text match '{Result}' → {Name}", qualitativeResult, fuzzyMatch.Name);
                resolution.TestResultId = fuzzyMatch.Id;
                return resolution;
            }
        }

        // STRATEGY 4: Fallback to abnormal flag interpretation (A, N, H, L, etc.)
        if (!string.IsNullOrWhiteSpace(abnormalFlag))
        {
            var flagMatch = abnormalFlag.ToUpper() switch
            {
                "A" or "H" or "HH" or ">" => testResults.FirstOrDefault(tr =>
                    tr.Name.Equals("Positive", StringComparison.OrdinalIgnoreCase) ||
                    tr.Name.Equals("Abnormal", StringComparison.OrdinalIgnoreCase)),
                "N" or "NORMAL" => testResults.FirstOrDefault(tr =>
                    tr.Name.Equals("Negative", StringComparison.OrdinalIgnoreCase) ||
                    tr.Name.Equals("Normal", StringComparison.OrdinalIgnoreCase)),
                _ => null
            };

            if (flagMatch != null)
            {
                _logger.LogDebug("[RESULT] Abnormal flag match '{Flag}' → {Name}", abnormalFlag, flagMatch.Name);
                resolution.TestResultId = flagMatch.Id;
                return resolution;
            }
        }

        _logger.LogDebug("[RESULT] No match found for QualitativeResult='{Qual}', AbnormalFlag='{Flag}'",
            qualitativeResult ?? "NULL", abnormalFlag ?? "NULL");

        return resolution;
    }

    private string NormalizeText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var normalized = new string(text
            .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
            .ToArray())
            .ToUpperInvariant()
            .Trim();

        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }

    #endregion
}

#region Result Classes

public class MarkerResolutionResult
{
    public Guid? PathogenId { get; set; }
    public int? TestMethodId { get; set; }
    public int? SpecimenTypeId { get; set; }
    public int? TestResultId { get; set; }
    public decimal? QuantitativeValue { get; set; }
}

public class ResultInterpretationResolution
{
    public int? TestResultId { get; set; }
    public decimal? QuantitativeValue { get; set; }
}

#endregion
