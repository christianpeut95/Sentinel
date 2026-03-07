using Sentinel.Models.Lookups;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class UserDiseaseAccess
    {
        public int Id { get; set; }
        
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }
        
        public bool IsAllowed { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Expires At")]
        public DateTime? ExpiresAt { get; set; }
        
        [Display(Name = "Granted By")]
        public string? GrantedByUserId { get; set; }
        public ApplicationUser? GrantedByUser { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Reason")]
        public string? Reason { get; set; }
        
        /// <summary>
        /// If true, this access applies to all child/descendant diseases automatically
        /// </summary>
        public bool ApplyToChildren { get; set; } = false;
        
        /// <summary>
        /// If set, this access is inherited from a parent disease (not directly granted)
        /// </summary>
        public Guid? InheritedFromDiseaseId { get; set; }
    }
}
