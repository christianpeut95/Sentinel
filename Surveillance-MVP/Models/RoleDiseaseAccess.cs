using Microsoft.AspNetCore.Identity;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Models
{
    public class RoleDiseaseAccess
    {
        public int Id { get; set; }
        
        public string RoleId { get; set; } = string.Empty;
        public IdentityRole? Role { get; set; }
        
        public Guid DiseaseId { get; set; }
        public Disease? Disease { get; set; }
        
        public bool IsAllowed { get; set; } = true;
        
        /// <summary>
        /// If true, this access applies to all child/descendant diseases automatically
        /// </summary>
        public bool ApplyToChildren { get; set; } = false;
        
        /// <summary>
        /// If set, this access is inherited from a parent disease (not directly granted)
        /// </summary>
        public Guid? InheritedFromDiseaseId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }
    }
}
