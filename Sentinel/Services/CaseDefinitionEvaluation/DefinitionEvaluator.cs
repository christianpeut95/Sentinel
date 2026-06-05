using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Orchestrates evaluation of complete case definitions against cases
    /// </summary>
    public class DefinitionEvaluator
    {
        private readonly ApplicationDbContext _context;
        private readonly CriteriaGroupEvaluator _groupEvaluator;
        private readonly ILogger<DefinitionEvaluator> _logger;

        public DefinitionEvaluator(
            ApplicationDbContext context, 
            CriteriaGroupEvaluator groupEvaluator,
            ILogger<DefinitionEvaluator> logger)
        {
            _context = context;
            _groupEvaluator = groupEvaluator;
            _logger = logger;
        }

        /// <summary>
        /// Evaluates a case definition against a case
        /// </summary>
        /// <param name="caseEntity">The case to evaluate</param>
        /// <param name="definitionId">The case definition ID to evaluate against</param>
        /// <returns>Complete evaluation result with recommendation</returns>
        public async Task<EvaluationResult> EvaluateDefinitionAsync(Case caseEntity, int definitionId)
        {
            _logger.LogInformation("\n" + new string('=', 80));
            _logger.LogInformation("Starting evaluation: Case {CaseId} against Definition {DefinitionId}", 
                caseEntity.Id, definitionId);

            var result = new EvaluationResult
            {
                CaseId = caseEntity.Id,
                EvaluationDate = DateTime.UtcNow
            };

            try
            {
                // Load the case definition with all criteria
                var definition = await _context.CaseDefinitions
                    .Include(cd => cd.Criteria.OrderBy(c => c.DisplayOrder))
                    .FirstOrDefaultAsync(cd => cd.Id == definitionId);

                _logger.LogInformation("Loaded definition: {DefinitionName} (ID: {DefinitionId})", 
                    definition?.Name ?? "NOT FOUND", definitionId);

                if (definition == null)
                {
                    _logger.LogWarning("❌ Definition not found");
                    result.Rationale = $"Case definition {definitionId} not found";
                    result.RecommendedAction = RecommendedAction.None;
                    return result;
                }

                result.CaseDefinitionId = definition.Id;
                result.CaseDefinitionName = definition.Name;

                // Check if definition is active
                _logger.LogInformation("Checking definition status: {Status}", definition.Status);
                if (definition.Status != CaseDefinitionStatus.Current)
                {
                    _logger.LogWarning("❌ Definition is not active (Status: {Status})", definition.Status);
                    result.Rationale = $"Case definition '{definition.Name}' is not active (Status: {definition.Status})";
                    result.RecommendedAction = RecommendedAction.None;
                    return result;
                }

                // Check if definition applies to this disease
                _logger.LogInformation("Checking disease match: Case={CaseDisease}, Definition={DefDisease}",
                    caseEntity.DiseaseId, definition.DiseaseId);
                if (caseEntity.DiseaseId.HasValue 
                    && definition.DiseaseId != Guid.Empty
                    && definition.DiseaseId != caseEntity.DiseaseId.Value)
                {
                    _logger.LogWarning("❌ Disease mismatch");
                    result.Rationale = $"Case definition is for a different disease";
                    result.RecommendedAction = RecommendedAction.None;
                    return result;
                }

                // Evaluate all criteria as a group
                var criteriaList = definition.Criteria.ToList();
                _logger.LogInformation("Found {Count} criteria to evaluate", criteriaList.Count);

                if (!criteriaList.Any())
                {
                    _logger.LogWarning("❌ Definition has no criteria");
                    result.Rationale = "Case definition has no criteria";
                    result.RecommendedAction = RecommendedAction.None;
                    return result;
                }

                _logger.LogInformation("Starting criteria evaluation...");

                // Evaluate root-level criteria (parentId = null)
                var groupResult = await _groupEvaluator.EvaluateGroupAsync(caseEntity, criteriaList, parentId: null);

                result.IsMatch = groupResult.IsMatch;
                result.CriteriaResults.Add(groupResult);

                // Determine recommended action based on match result and confidence
                result.RecommendedAction = DetermineRecommendedAction(groupResult, definition);
                result.Rationale = BuildRationale(groupResult, definition);

                // Log final result
                _logger.LogInformation("\\n" + new string('-', 80));
                _logger.LogInformation("{Icon} FINAL RESULT: {Result}", 
                    result.IsMatch ? "✅" : "❌", 
                    result.IsMatch ? "MATCH" : "NO MATCH");
                _logger.LogInformation("Recommended Action: {Action}", result.RecommendedAction);
                _logger.LogInformation("Rationale: {Rationale}", result.Rationale);
                _logger.LogInformation(new string('=', 80) + "\\n");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error evaluating case definition");
                result.Rationale = $"Error evaluating case definition: {ex.Message}";
                result.RecommendedAction = RecommendedAction.FlagForReview;
                return result;
            }
        }

        /// <summary>
        /// Evaluates all active case definitions for a case's disease
        /// </summary>
        /// <param name="caseEntity">The case to evaluate</param>
        /// <returns>List of evaluation results for all applicable definitions</returns>
        public async Task<List<EvaluationResult>> EvaluateAllDefinitionsAsync(Case caseEntity)
        {
            var results = new List<EvaluationResult>();

            // Find all current definitions for this disease
            var query = _context.CaseDefinitions
                .Where(cd => cd.Status == CaseDefinitionStatus.Current);

            // Filter by disease if case has a disease
            if (caseEntity.DiseaseId.HasValue)
            {
                query = query.Where(cd => cd.DiseaseId == Guid.Empty || cd.DiseaseId == caseEntity.DiseaseId.Value);
            }

            var definitions = await query.ToListAsync();

            foreach (var definition in definitions)
            {
                var result = await EvaluateDefinitionAsync(caseEntity, definition.Id);
                results.Add(result);
            }

            return results;
        }

        /// <summary>
        /// Determines what action should be recommended based on evaluation results
        /// </summary>
        private RecommendedAction DetermineRecommendedAction(CriterionResult groupResult, CaseDefinition definition)
        {
            if (!groupResult.IsMatch)
            {
                return RecommendedAction.None;
            }

            // Check confidence level based on criterion types that matched
            var matchedCriteria = GetAllMatchedCriteria(groupResult);

            // High confidence: Has laboratory confirmation
            bool hasLabConfirmation = matchedCriteria.Any(c => 
                c.CriterionType == CriterionType.Laboratory && c.IsMatch);

            // Medium confidence: Clinical + Epidemiological
            bool hasClinical = matchedCriteria.Any(c => 
                c.CriterionType == CriterionType.Clinical && c.IsMatch);

            bool hasEpidemiological = matchedCriteria.Any(c => 
                c.CriterionType == CriterionType.Epidemiological && c.IsMatch);

            // Determine action based on confidence
            if (hasLabConfirmation)
            {
                // High confidence - can auto-classify
                return RecommendedAction.AutoClassify;
            }
            else if (hasClinical && hasEpidemiological)
            {
                // Medium confidence - suggest classification for review
                return RecommendedAction.SuggestClassification;
            }
            else if (hasClinical || hasEpidemiological)
            {
                // Low confidence - flag for manual review
                return RecommendedAction.FlagForReview;
            }

            // Matched but low confidence
            return RecommendedAction.SuggestClassification;
        }

        /// <summary>
        /// Builds a human-readable rationale for the evaluation result
        /// </summary>
        private string BuildRationale(CriterionResult groupResult, CaseDefinition definition)
        {
            if (!groupResult.IsMatch)
            {
                return $"Case does not meet criteria for '{definition.Name}'. {groupResult.ActualValue}";
            }

            var matchedCriteria = GetAllMatchedCriteria(groupResult);
            var criteriaByType = matchedCriteria
                .GroupBy(c => c.CriterionType)
                .Select(g => $"{g.Count()} {g.Key}")
                .ToList();

            var criteriaList = string.Join(", ", criteriaByType);

            return $"Case meets criteria for '{definition.Name}'. Matched criteria: {criteriaList}.";
        }

        /// <summary>
        /// Recursively gets all matched criteria from a group result
        /// </summary>
        private List<CriterionResult> GetAllMatchedCriteria(CriterionResult groupResult)
        {
            var matched = new List<CriterionResult>();

            if (groupResult.ChildResults == null || !groupResult.ChildResults.Any())
            {
                // Leaf criterion
                if (groupResult.IsMatch)
                {
                    matched.Add(groupResult);
                }
            }
            else
            {
                // Group - recurse into children
                foreach (var child in groupResult.ChildResults)
                {
                    matched.AddRange(GetAllMatchedCriteria(child));
                }
            }

            return matched;
        }
    }
}
