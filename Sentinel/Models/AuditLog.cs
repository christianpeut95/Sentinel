using System;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Entity Type")]
        [StringLength(100)]
        public string EntityType { get; set; }

        [Required]
        [Display(Name = "Entity ID")]
        [StringLength(50)]
        public string EntityId { get; set; }

        [Required]
        [Display(Name = "Action")]
        [StringLength(50)]
        public string Action { get; set; }

        [Required]
        [Display(Name = "Field Name")]
        [StringLength(100)]
        public string FieldName { get; set; }

        [Display(Name = "Old Value")]
        public string? OldValue { get; set; }

        [Display(Name = "New Value")]
        public string? NewValue { get; set; }

        [Required]
        [Display(Name = "Changed At")]
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Changed By")]
        public string? ChangedByUserId { get; set; }

        public ApplicationUser? ChangedByUser { get; set; }

        [Display(Name = "IP Address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Display(Name = "User Agent")]
        [StringLength(500)]
        public string? UserAgent { get; set; }
    }
}
