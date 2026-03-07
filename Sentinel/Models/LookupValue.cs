namespace Sentinel.Models
{
    public class LookupValue
    {
        public int Id { get; set; }
        public int LookupTableId { get; set; }
        public LookupTable LookupTable { get; set; } = null!;
        
        public string Value { get; set; } = string.Empty; // Internal value
        public string DisplayText { get; set; } = string.Empty; // What users see
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
