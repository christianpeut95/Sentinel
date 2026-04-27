namespace Sentinel.Models.Timeline
{
    /// <summary>
    /// Represents an entity extracted from natural language text
    /// </summary>
    public class ExtractedEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Type of entity (Person, Location, Event, etc.)
        /// </summary>
        public EntityType EntityType { get; set; }

        /// <summary>
        /// The raw text extracted from the narrative
        /// </summary>
        public string RawText { get; set; } = string.Empty;

        /// <summary>
        /// Normalized/cleaned version of the text
        /// </summary>
        public string? NormalizedValue { get; set; }

        /// <summary>
        /// Starting character position in the narrative text
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// Ending character position in the narrative text
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Confidence level of the extraction
        /// </summary>
        public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;

        /// <summary>
        /// Whether the operator has confirmed this entity
        /// </summary>
        public bool IsConfirmed { get; set; } = false;

        /// <summary>
        /// Type of linked record (Location, Event, Contact, etc.)
        /// </summary>
        public string? LinkedRecordType { get; set; }

        /// <summary>
        /// ID of the linked record in the database
        /// </summary>
        public Guid? LinkedRecordId { get; set; }

        /// <summary>
        /// Display name for the linked record
        /// </summary>
        public string? LinkedRecordDisplayName { get; set; }

        /// <summary>
        /// Suggestions for autocomplete (e.g., from Places API)
        /// </summary>
        public List<EntitySuggestion> Suggestions { get; set; } = new();

        /// <summary>
        /// Additional metadata as JSON (flexible for future needs)
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Notes added by the operator
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Suggestion for an extracted entity (autocomplete option)
    /// </summary>
    public class EntitySuggestion
    {
        public string DisplayText { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? RecordId { get; set; }
        public string? RecordType { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Address { get; set; }
        public double Score { get; set; } = 0.0; // Relevance score
    }

    /// <summary>
    /// Relationship between two entities
    /// </summary>
    public class EntityRelationship
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID of the primary entity in this relationship
        /// </summary>
        public string PrimaryEntityId { get; set; } = string.Empty;

        /// <summary>
        /// ID of the related entity
        /// </summary>
        public string RelatedEntityId { get; set; } = string.Empty;

        /// <summary>
        /// Type of relationship
        /// </summary>
        public RelationshipType RelationType { get; set; }

        /// <summary>
        /// Optional time entity ID when this relationship occurred
        /// </summary>
        public string? TimeEntityId { get; set; }

        /// <summary>
        /// Confidence level of this relationship detection
        /// </summary>
        public ConfidenceLevel Confidence { get; set; } = ConfidenceLevel.Medium;

        /// <summary>
        /// Whether operator has confirmed this relationship
        /// </summary>
        public bool IsConfirmed { get; set; } = false;

        /// <summary>
        /// Character position in narrative where relationship was detected
        /// </summary>
        public int SourcePosition { get; set; }

        /// <summary>
        /// Additional metadata about this relationship
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// For sequential relationships, the order in sequence
        /// </summary>
        public int? SequenceOrder { get; set; }

        /// <summary>
        /// Optional contextual notes
        /// </summary>
        public string? Notes { get; set; }
    }
}
