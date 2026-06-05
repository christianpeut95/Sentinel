using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Service for evaluating cases against case definitions
    /// </summary>
    public class CaseDefinitionEvaluationService : ICaseDefinitionEvaluationService
    {
        private readonly ApplicationDbContext _context;
        private readonly DefinitionEvaluator _definitionEvaluator;
        private readonly ILogger<CaseDefinitionEvaluationService> _logger;

        public CaseDefinitionEvaluationService(
            ApplicationDbContext context,
            DefinitionEvaluator definitionEvaluator,
            ILogger<CaseDefinitionEvaluationService> logger)
        {
            _context = context;
            _definitionEvaluator = definitionEvaluator;
            _logger = logger;
        }

        public async Task<EvaluationResult> EvaluateCaseAsync(Guid caseId, int definitionId)
        {
            _logger.LogInformation("Evaluating case {CaseId} against definition {DefinitionId}", caseId, definitionId);

            // Load case with all related data needed for evaluation
            var caseEntity = await LoadCaseWithRelatedDataAsync(caseId);

            if (caseEntity == null)
            {
                _logger.LogWarning("Case {CaseId} not found", caseId);
                return new EvaluationResult
                {
                    CaseId = caseId,
                    EvaluationDate = DateTime.UtcNow,
                    Rationale = "Case not found",
                    RecommendedAction = RecommendedAction.None
                };
            }

            var result = await _definitionEvaluator.EvaluateDefinitionAsync(caseEntity, definitionId);

            _logger.LogInformation(
                "Evaluation complete for case {CaseId}: IsMatch={IsMatch}, Action={Action}",
                caseId, result.IsMatch, result.RecommendedAction);

            return result;
        }

        public async Task<List<EvaluationResult>> EvaluateAllDefinitionsForCaseAsync(Guid caseId)
        {
            _logger.LogInformation("Evaluating case {CaseId} against all active definitions", caseId);

            var caseEntity = await LoadCaseWithRelatedDataAsync(caseId);

            if (caseEntity == null)
            {
                _logger.LogWarning("Case {CaseId} not found", caseId);
                return new List<EvaluationResult>();
            }

            var results = await _definitionEvaluator.EvaluateAllDefinitionsAsync(caseEntity);

            _logger.LogInformation(
                "Evaluated case {CaseId} against {Count} definitions, {Matches} matched",
                caseId, results.Count, results.Count(r => r.IsMatch));

            return results;
        }

        public async Task<bool> ApplyClassificationAsync(Guid caseId, EvaluationResult evaluationResult, string userId)
        {
            _logger.LogInformation(
                "Applying classification to case {CaseId} from definition {DefinitionId}",
                caseId, evaluationResult.CaseDefinitionId);

            try
            {
                var caseEntity = await _context.Cases
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (caseEntity == null)
                {
                    _logger.LogWarning("Case {CaseId} not found", caseId);
                    return false;
                }

                // Load the case definition to get the confirmation status
                CaseDefinition? definition = null;
                if (evaluationResult.CaseDefinitionId.HasValue)
                {
                    definition = await _context.CaseDefinitions
                        .Include(d => d.ConfirmationStatus)
                        .FirstOrDefaultAsync(d => d.Id == evaluationResult.CaseDefinitionId.Value);
                }

                if (definition == null)
                {
                    _logger.LogWarning("Case definition {DefinitionId} not found", evaluationResult.CaseDefinitionId);
                    return false;
                }

                // Store the previous status for history
                var previousStatusId = caseEntity.ConfirmationStatusId;

                // Update case confirmation status from the definition
                caseEntity.ConfirmationStatusId = definition.ConfirmationStatusId;
                caseEntity.ConfirmationStatusClassifiedDate = DateTime.UtcNow;
                caseEntity.ConfirmationStatusClassifiedBy = userId;
                caseEntity.IsAutoClassified = evaluationResult.RecommendedAction == RecommendedAction.AutoClassify;
                caseEntity.LastEvaluatedDate = evaluationResult.EvaluationDate;
                caseEntity.LastEvaluatedDefinitionIds = evaluationResult.CaseDefinitionId?.ToString();

                _logger.LogInformation(
                    "Setting case {CaseId} confirmation status to {StatusId} ({StatusName})",
                    caseId, definition.ConfirmationStatusId, definition.ConfirmationStatus?.Name);

                // Record in classification history with status change
                await RecordEvaluationInternalAsync(caseEntity, evaluationResult, previousStatusId, definition.ConfirmationStatusId, userId, wasApplied: true);

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully applied classification to case {CaseId}",
                    caseId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying classification to case {CaseId}", caseId);
                return false;
            }
        }

        public async Task<int> RecordEvaluationAsync(Guid caseId, EvaluationResult evaluationResult)
        {
            _logger.LogInformation(
                "Recording evaluation for case {CaseId}",
                caseId);

            try
            {
                var caseEntity = await _context.Cases
                    .FirstOrDefaultAsync(c => c.Id == caseId);

                if (caseEntity == null)
                {
                    _logger.LogWarning("Case {CaseId} not found", caseId);
                    return 0;
                }

                // Load the case definition to get the confirmation status
                CaseDefinition? definition = null;
                if (evaluationResult.CaseDefinitionId.HasValue)
                {
                    definition = await _context.CaseDefinitions
                        .FirstOrDefaultAsync(d => d.Id == evaluationResult.CaseDefinitionId.Value);
                }

                if (definition == null)
                {
                    _logger.LogWarning("Case definition {DefinitionId} not found for recording", evaluationResult.CaseDefinitionId);
                    return 0;
                }

                var historyId = await RecordEvaluationInternalAsync(
                    caseEntity, 
                    evaluationResult, 
                    caseEntity.ConfirmationStatusId, 
                    definition.ConfirmationStatusId, 
                    userId: null, 
                    wasApplied: false);

                await _context.SaveChangesAsync();

                return historyId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording evaluation for case {CaseId}", caseId);
                return 0;
            }
        }

        /// <summary>
        /// Loads a case with all related data needed for evaluation
        /// </summary>
        private async Task<Case?> LoadCaseWithRelatedDataAsync(Guid caseId)
        {
            return await _context.Cases
                .Include(c => c.Patient)
                    .ThenInclude(p => p.Gender)
                .Include(c => c.LabResults)
                    .ThenInclude(lr => lr.Markers)
                        .ThenInclude(m => m.Pathogen)
                .Include(c => c.LabResults)
                    .ThenInclude(lr => lr.Markers)
                        .ThenInclude(m => m.TestMethod)
                .Include(c => c.CaseSymptoms)
                    .ThenInclude(cs => cs.Symptom)
                .Include(c => c.CustomFieldStrings)
                .Include(c => c.CustomFieldNumbers)
                .Include(c => c.CustomFieldDates)
                .Include(c => c.CustomFieldBooleans)
                .Include(c => c.CustomFieldLookups)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == caseId);
        }

        /// <summary>
        /// Records evaluation result in classification history
        /// </summary>
        private async Task<int> RecordEvaluationInternalAsync(
            Case caseEntity, 
            EvaluationResult evaluationResult, 
            int? fromStatusId, 
            int toStatusId, 
            string? userId, 
            bool wasApplied)
        {
            var history = new CaseClassificationHistory
            {
                CaseId = caseEntity.Id,
                CaseDefinitionId = evaluationResult.CaseDefinitionId,
                FromConfirmationStatusId = fromStatusId,
                ToConfirmationStatusId = toStatusId,
                ClassifiedByUserId = userId,
                ClassifiedDate = wasApplied ? DateTime.UtcNow : (DateTime?)null,
                IsAutoClassified = evaluationResult.RecommendedAction == RecommendedAction.AutoClassify,
                EvaluationDate = evaluationResult.EvaluationDate,
                IsMatch = evaluationResult.IsMatch,
                RecommendedAction = evaluationResult.RecommendedAction,
                Rationale = evaluationResult.Rationale,
                CriteriaResultJson = System.Text.Json.JsonSerializer.Serialize(evaluationResult.CriteriaResults),
                WasApplied = wasApplied
            };

            _context.CaseClassificationHistory.Add(history);
            await _context.SaveChangesAsync();

            return history.Id;
        }
    }
}
