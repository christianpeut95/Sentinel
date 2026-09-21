using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// Administrative account status. Disabled accounts cannot sign in and their
        /// existing sessions are invalidated when the status changes.
        /// </summary>
        [Display(Name = "Account Enabled")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Records acceptance of the current WebDataRocks vendor terms before
        /// the user's browser is permitted to load the interactive pivot
        /// component. This is separate from the organisation-level decision in
        /// <see cref="SystemSettings"/> to enable the optional feature.
        /// </summary>
        [StringLength(40)]
        public string? WebDataRocksLicenseVersion { get; set; }

        public DateTime? WebDataRocksLicenseAcceptedAt { get; set; }

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

        [Display(Name = "Dashboard Configuration")]
        [DataType(DataType.MultilineText)]
        public string? DashboardConfigJson { get; set; }
    }
}
