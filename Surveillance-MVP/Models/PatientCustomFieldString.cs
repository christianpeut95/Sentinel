namespace Surveillance_MVP.Models
{
    public class PatientCustomFieldString
    {
        public int Id { get; set; }
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        
        public int FieldDefinitionId { get; set; }
        public CustomFieldDefinition FieldDefinition { get; set; } = null!;
        
        public string? Value { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
