namespace Sentinel.Models
{
    public class DiseaseCustomField
    {
        public int Id { get; set; }
        
        public Guid DiseaseId { get; set; }
        public Lookups.Disease Disease { get; set; } = null!;
        
        public int CustomFieldDefinitionId { get; set; }
        public CustomFieldDefinition CustomFieldDefinition { get; set; } = null!;
        
        public bool InheritToChildDiseases { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
