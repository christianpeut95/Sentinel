using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models
{
    public class ApplicationUser : IdentityUser
    {
        public List<UserGroup> UserGroups { get; set; } = new();
        public List<UserPermission> UserPermissions { get; set; } = new();
        public List<UserDiseaseAccess> UserDiseaseAccess { get; set; } = new();

        [StringLength(100)]
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Primary Language")]
        [StringLength(50)]
        public string? PrimaryLanguage { get; set; }

        [Display(Name = "Languages Spoken (JSON)")]
        [DataType(DataType.MultilineText)]
        public string? LanguagesSpokenJson { get; set; }

        [Display(Name = "Is Interview Worker")]
        public bool IsInterviewWorker { get; set; }

        [Display(Name = "Available for Auto-Assignment")]
        public bool AvailableForAutoAssignment { get; set; }

        [Display(Name = "Current Task Capacity")]
        public int CurrentTaskCapacity { get; set; } = 10;
    }
}
