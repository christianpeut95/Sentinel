namespace Sentinel.Models.Timeline
{
    /// <summary>
    /// Represents a named group of entities for reuse in timeline entries
    /// Example: #Family = (John, Mary, Sue)
    /// </summary>
    public class EntityGroup
    {
        /// <summary>
        /// Unique identifier for this group
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Case this group belongs to
        /// </summary>
        public Guid CaseId { get; set; }

        /// <summary>
        /// Group name (e.g., "Family", "Siblings", "Colleagues")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Entity IDs that belong to this group
        /// These reference entities from the browser session (entity.id or entity.sourceEntityId)
        /// </summary>
        public List<string> EntityIds { get; set; } = new();

        /// <summary>
        /// When this group was created
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional description of the group
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Request model for creating a new entity group
    /// </summary>
    public class CreateEntityGroupRequest
    {
        public Guid CaseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<string> EntityIds { get; set; } = new();
        public string? Description { get; set; }
    }

    /// <summary>
    /// Response model for entity group operations
    /// </summary>
    public class EntityGroupResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<string> EntityIds { get; set; } = new();
        public int EntityCount { get; set; }
    }
}
