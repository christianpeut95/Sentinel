using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class TestResult
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Test Result")]
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

        // Link to Test Type - a test result is specific to a test type
        [Display(Name = "Test Type")]
        public int? TestTypeId { get; set; }
        public TestType? TestType { get; set; }

        public ICollection<LabResult> LabResults { get; set; } = new List<LabResult>();
    }
}

