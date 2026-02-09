namespace Surveillance_MVP.Models
{
    public class CustomFieldDefinition
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // e.g., "smoking_status"
        public string Label { get; set; } = string.Empty; // e.g., "Smoking Status"
        public string Category { get; set; } = "General"; // Group fields
        public CustomFieldType FieldType { get; set; }
        
        public bool IsRequired { get; set; }
        public bool IsSearchable { get; set; }
        public bool ShowOnList { get; set; }
        public bool ShowOnCreateEdit { get; set; } = true;
        public bool ShowOnDetails { get; set; } = true;
        
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Validation rules (JSON format)
        public string? ValidationRules { get; set; }
        
        // For dropdown fields
        public int? LookupTableId { get; set; }
        public LookupTable? LookupTable { get; set; }
        
        // Context flags - where this field can be used
        public bool ShowOnPatientForm { get; set; } = true;
        public bool ShowOnCaseForm { get; set; } = false;
        
        // Navigation properties
        public ICollection<DiseaseCustomField> DiseaseCustomFields { get; set; } = new List<DiseaseCustomField>();
    }
}
