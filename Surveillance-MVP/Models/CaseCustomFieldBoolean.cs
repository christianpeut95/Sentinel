namespace Surveillance_MVP.Models
{
    public class CaseCustomFieldBoolean
    {
        public int Id { get; set; }
        public Guid CaseId { get; set; }
        public Case Case { get; set; } = null!;
        
        public int FieldDefinitionId { get; set; }
        public CustomFieldDefinition FieldDefinition { get; set; } = null!;
        
        public bool? Value { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
