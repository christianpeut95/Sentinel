namespace Sentinel.Models
{
    public class PatientCustomFieldBoolean
    {
        public int Id { get; set; }
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        
        public int FieldDefinitionId { get; set; }
        public CustomFieldDefinition FieldDefinition { get; set; } = null!;
        
        public bool Value { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
