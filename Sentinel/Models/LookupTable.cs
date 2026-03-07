namespace Sentinel.Models
{
    public class LookupTable
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "smoking_statuses"
        public string DisplayName { get; set; } = string.Empty; // e.g., "Smoking Statuses"
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public List<LookupValue> Values { get; set; } = new();
    }
}
