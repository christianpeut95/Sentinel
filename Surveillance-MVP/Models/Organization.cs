using System.ComponentModel.DataAnnotations;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Models
{
    public class Organization : IAuditable
    {
        public Guid Id { get; set; }

        [Display(Name = "Organization ID")]
        [StringLength(20)]
        public string FriendlyId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Organization Name")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Organization Type")]
        public int? OrganizationTypeId { get; set; }
        public OrganizationType? OrganizationType { get; set; }

        [Display(Name = "Address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Phone")]
        [StringLength(50)]
        [DataType(DataType.PhoneNumber)]
        public string? Phone { get; set; }

        [Display(Name = "Email")]
        [StringLength(200)]
        [DataType(DataType.EmailAddress)]
        public string? Email { get; set; }

        [Display(Name = "Contact Person")]
        [StringLength(200)]
        public string? ContactPerson { get; set; }

        [Display(Name = "Export Code")]
        [StringLength(50)]
        public string? ExportCode { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Notes")]
        [StringLength(1000)]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        // Navigation properties
        public ICollection<LabResult> LabResultsAsLaboratory { get; set; } = new List<LabResult>();
        public ICollection<LabResult> LabResultsAsOrderingProvider { get; set; } = new List<LabResult>();

        // Audit fields
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Modified At")]
        public DateTime? ModifiedAt { get; set; }
    }
}
