namespace Sentinel.Configuration
{
    /// <summary>
    /// Configuration settings for case definition evaluation
    /// </summary>
    public class CaseDefinitionSettings
    {
        /// <summary>
        /// Which date field to use when determining which case definition version applies
        /// </summary>
        public CaseEvaluationDateField EvaluationDateField { get; set; } = CaseEvaluationDateField.SymptomOnset;

        /// <summary>
        /// Automatically trigger evaluation when a lab result is added to a case
        /// </summary>
        public bool AutoEvaluateOnLabResultAdded { get; set; } = true;

        /// <summary>
        /// Automatically trigger evaluation when a case is saved/modified
        /// </summary>
        public bool AutoEvaluateOnCaseSave { get; set; } = false;
    }

    /// <summary>
    /// Determines which date field from a case should be used for evaluation date comparison
    /// </summary>
    public enum CaseEvaluationDateField
    {
        /// <summary>
        /// Use the date the case was first entered into the system
        /// </summary>
        DateEntered = 1,

        /// <summary>
        /// Use the date the case was first notified/reported
        /// </summary>
        DateNotified = 2,

        /// <summary>
        /// Use the earliest laboratory specimen collection date
        /// </summary>
        EarliestLabCollection = 3,

        /// <summary>
        /// Use the date of symptom onset (default, most clinically relevant)
        /// </summary>
        SymptomOnset = 4
    }
}
