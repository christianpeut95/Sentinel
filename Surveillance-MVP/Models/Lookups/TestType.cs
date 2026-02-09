using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models.Lookups
{
    public class TestType
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Test Type")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Export Code")]
        [StringLength(50)]
        public string? ExportCode { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
        public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
    }
}

