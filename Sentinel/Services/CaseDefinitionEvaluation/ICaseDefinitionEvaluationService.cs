using Sentinel.Models;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Public service interface for case definition evaluation
    /// </summary>
    public interface ICaseDefinitionEvaluationService
    {
        /// <summary>
        /// Evaluates a case against a specific case definition
        /// </summary>
        /// <param name="caseId">The case ID to evaluate</param>
        /// <param name="definitionId">The case definition ID to evaluate against</param>
        /// <returns>Evaluation result with recommendation</returns>
        Task<EvaluationResult> EvaluateCaseAsync(Guid caseId, int definitionId);

        /// <summary>
        /// Evaluates a case against all active definitions for its disease
        /// </summary>
        /// <param name="caseId">The case ID to evaluate</param>
        /// <returns>List of evaluation results</returns>
        Task<List<EvaluationResult>> EvaluateAllDefinitionsForCaseAsync(Guid caseId);

        /// <summary>
        /// Applies the evaluation result to a case (updates classification status)
        /// </summary>
        /// <param name="caseId">The case ID</param>
        /// <param name="evaluationResult">The evaluation result to apply</param>
        /// <param name="userId">The user applying the classification</param>
        /// <returns>True if applied successfully</returns>
        Task<bool> ApplyClassificationAsync(Guid caseId, EvaluationResult evaluationResult, string userId);

        /// <summary>
        /// Records an evaluation in the classification history without applying it
        /// </summary>
        /// <param name="caseId">The case ID</param>
        /// <param name="evaluationResult">The evaluation result to record</param>
        /// <returns>The created history record ID</returns>
        Task<int> RecordEvaluationAsync(Guid caseId, EvaluationResult evaluationResult);
    }
}
