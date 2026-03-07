using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class Group
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

        public List<UserGroup> UserGroups { get; set; } = new();
    }
}
