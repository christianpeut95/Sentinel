using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class CaseStatus
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Case Status")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Display Order")]
        public int? DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Applicable To")]
        public CaseTypeApplicability ApplicableTo { get; set; } = CaseTypeApplicability.Both;
    }

    public enum CaseTypeApplicability
    {
        [Display(Name = "Cases Only")]
        Case = 1,

        [Display(Name = "Contacts Only")]
        Contact = 2,

        [Display(Name = "Both Cases and Contacts")]
        Both = 3
    }
}

