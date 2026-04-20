namespace Sentinel.Models.Timeline
{
    /// <summary>
    /// Complete timeline data for a case (stored as JSON file)
    /// </summary>
    public class CaseTimelineData
    {
        public Guid CaseId { get; set; }

        /// <summary>
        /// All timeline entries for this case
        /// </summary>
        public List<TimelineEntry> Entries { get; set; } = new();

        /// <summary>
        /// Convention locations defined by the user (shortcuts)
        /// </summary>
        public Dictionary<string, ConventionLocation> Conventions { get; set; } = new();

        /// <summary>
        /// When this timeline was created
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// User ID who created this timeline
        /// </summary>
        public string CreatedByUserId { get; set; } = string.Empty;

        /// <summary>
        /// When this timeline was last modified
        /// </summary>
        public DateTime? LastModified { get; set; }

        /// <summary>
        /// User ID who last modified this timeline
        /// </summary>
        public string? LastModifiedByUserId { get; set; }

        /// <summary>
        /// Whether the entire timeline has been reviewed
        /// </summary>
        public bool IsReviewed { get; set; } = false;

        /// <summary>
        /// When the review was completed
        /// </summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>
        /// User ID who reviewed this timeline
        /// </summary>
        public string? ReviewedByUserId { get; set; }

        /// <summary>
        /// Version number for tracking changes
        /// </summary>
        public int Version { get; set; } = 1;
    }

    /// <summary>
    /// Represents a user-defined location shortcut
    /// </summary>
    public class ConventionLocation
    {
        public string ConventionName { get; set; } = string.Empty;
        public Guid? LocationId { get; set; }
        public string? LocationName { get; set; }
        public string? FreeTextAddress { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
