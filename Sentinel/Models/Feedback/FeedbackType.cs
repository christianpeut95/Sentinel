namespace Sentinel.Models.Feedback
{
    /// <summary>
    /// Feedback type categories for Sentinel Feedback API
    /// </summary>
    public enum FeedbackType
    {
        /// <summary>
        /// Something is broken or not working as expected
        /// </summary>
        Bug,

        /// <summary>
        /// Request for new functionality
        /// </summary>
        FeatureRequest,

        /// <summary>
        /// UI/UX is unclear or difficult to understand
        /// </summary>
        Confusing,

        /// <summary>
        /// General feedback or comments
        /// </summary>
        General
    }
}
