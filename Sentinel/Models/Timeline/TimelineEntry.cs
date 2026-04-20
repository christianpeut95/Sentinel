namespace Sentinel.Models.Timeline
{
    /// <summary>
    /// Represents a single day/date entry in the timeline
    /// </summary>
    public class TimelineEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The date this entry refers to
        /// </summary>
        public DateTime EntryDate { get; set; }

        /// <summary>
        /// The raw narrative text typed by the operator
        /// </summary>
        public string NarrativeText { get; set; } = string.Empty;

        /// <summary>
        /// Entities extracted from the narrative
        /// </summary>
        public List<ExtractedEntity> Entities { get; set; } = new();

        /// <summary>
        /// Relationships between entities
        /// </summary>
        public List<EntityRelationship> Relationships { get; set; } = new();

        /// <summary>
        /// When this entry was parsed
        /// </summary>
        public DateTime? ParsedAt { get; set; }

        /// <summary>
        /// Overall confidence score for this entry
        /// </summary>
        public decimal? ConfidenceScore { get; set; }

        /// <summary>
        /// Whether the operator has reviewed and confirmed this entry
        /// </summary>
        public bool IsReviewed { get; set; } = false;

        /// <summary>
        /// Uncertainty markers detected (e.g., "I think", "maybe")
        /// </summary>
        public List<string> UncertaintyMarkers { get; set; } = new();

        /// <summary>
        /// Corrections applied to this entry
        /// </summary>
        public List<TimelineCorrection> Corrections { get; set; } = new();

        /// <summary>
        /// Protective measures mentioned (e.g., "wearing mask")
        /// </summary>
        public List<string> ProtectiveMeasures { get; set; } = new();

        /// <summary>
        /// Whether this is a memory gap ("can't remember")
        /// </summary>
        public bool IsMemoryGap { get; set; } = false;

        /// <summary>
        /// Notes about the memory gap
        /// </summary>
        public string? MemoryGapNotes { get; set; }
    }

    /// <summary>
    /// Represents a correction made to a timeline entry
    /// </summary>
    public class TimelineCorrection
    {
        public DateTime CorrectedAt { get; set; }
        public string OriginalText { get; set; } = string.Empty;
        public string CorrectedText { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
