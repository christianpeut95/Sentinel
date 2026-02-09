using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models.Lookups
{
    public class Occupation
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "ANZSCO Code")]
        [StringLength(6)]
        public string Code { get; set; }

        [Required]
        [Display(Name = "Occupation Name")]
        public string Name { get; set; }

        [Display(Name = "Major Group Code")]
        [StringLength(1)]
        public string? MajorGroupCode { get; set; }

        [Display(Name = "Major Group Name")]
        public string? MajorGroupName { get; set; }

        [Display(Name = "Sub-Major Group Code")]
        [StringLength(2)]
        public string? SubMajorGroupCode { get; set; }

        [Display(Name = "Sub-Major Group Name")]
        public string? SubMajorGroupName { get; set; }

        [Display(Name = "Minor Group Code")]
        [StringLength(3)]
        public string? MinorGroupCode { get; set; }

        [Display(Name = "Minor Group Name")]
        public string? MinorGroupName { get; set; }

        [Display(Name = "Unit Group Code")]
        [StringLength(4)]
        public string? UnitGroupCode { get; set; }

        [Display(Name = "Unit Group Name")]
        public string? UnitGroupName { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}
