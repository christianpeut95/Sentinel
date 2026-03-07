using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Lookups
{
    public class Language
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string? Code { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;
    }
}
