namespace Sentinel.Models
{
    public class PatientCustomFieldLookup
    {
        public int Id { get; set; }
        public Guid PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
        
        public int FieldDefinitionId { get; set; }
        public CustomFieldDefinition FieldDefinition { get; set; } = null!;
        
        public int? LookupValueId { get; set; }
        public LookupValue? LookupValue { get; set; }
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
