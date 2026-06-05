using Sentinel.Models.CaseDefinitions;

namespace Sentinel.Services.CaseDefinitionEvaluation
{
    /// <summary>
    /// Result of evaluating a case against case definition criteria
    /// </summary>
    public class EvaluationResult
    {
        /// <summary>
        /// The case that was evaluated
        /// </summary>
        public Guid CaseId { get; set; }

        /// <summary>
        /// The case definition ID that was evaluated against
        /// </summary>
        public int? CaseDefinitionId { get; set; }

        /// <summary>
        /// Name of the definition for display purposes
        /// </summary>
        public string CaseDefinitionName { get; set; } = string.Empty;

        /// <summary>
        /// True if the case meets all criteria for this definition
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// When this evaluation was performed
        /// </summary>
        public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Individual criterion results showing what matched and what didn't
        /// </summary>
        public List<CriterionResult> CriteriaResults { get; set; } = new();

        /// <summary>
        /// Recommended action based on evaluation and definition settings
        /// </summary>
        public RecommendedAction RecommendedAction { get; set; }

        /// <summary>
        /// Human-readable explanation of the evaluation result
        /// </summary>
        public string Rationale { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of evaluating a single criterion
    /// </summary>
    public class CriterionResult
    {
        /// <summary>
        /// ID of the criterion that was evaluated
        /// </summary>
        public int CriterionId { get; set; }

        /// <summary>
        /// Type of criterion (Laboratory, Clinical, etc.)
        /// </summary>
        public CriterionType CriterionType { get; set; }

        /// <summary>
        /// Human-readable display text for this criterion
        /// </summary>
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// True if this criterion was satisfied
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// For grouped criteria, the results of child criteria
        /// </summary>
        public List<CriterionResult> ChildResults { get; set; } = new();

        /// <summary>
        /// Logical operator used if this is a group (AND/OR)
        /// </summary>
        public LogicalOperator? LogicalOperator { get; set; }

        /// <summary>
        /// The actual value(s) found in the case data
        /// </summary>
        public string? ActualValue { get; set; }

        /// <summary>
        /// The expected value(s) from the criterion definition
        /// </summary>
        public string? ExpectedValue { get; set; }

        /// <summary>
        /// Field path that was evaluated (e.g., "Patient.Age")
        /// </summary>
        public string? FieldPath { get; set; }

        /// <summary>
        /// Error message if evaluation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
