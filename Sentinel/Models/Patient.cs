using Sentinel.Models.Lookups;
using System;
using System.ComponentModel.DataAnnotations;


namespace Sentinel.Models
{
    public class Patient : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Display(Name = "Patient ID")]
        [StringLength(20)]
        public string FriendlyId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "First Name")]
        public string GivenName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string FamilyName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Sex at Birth")]
        public int? SexAtBirthId { get; set; }
        public SexAtBirth? SexAtBirth { get; set; }

        [Display(Name = "Gender")]
        public int? GenderId { get; set; }
        public Gender? Gender { get; set; }

        [Display(Name = "Home Phone")]
        [DataType(DataType.PhoneNumber)]
        public string? HomePhone { get; set; }

        [Display(Name = "Mobile Phone")]
        [DataType(DataType.PhoneNumber)]
        public string? MobilePhone { get; set; }

        [Display(Name = "Email Address")]
        [DataType(DataType.EmailAddress)]
        public string? EmailAddress { get; set; }

        [Display(Name = "Address")]
        public string? AddressLine { get; set; }

        [Display(Name = "Suburb")]
        public string? City { get; set; }

        public string? State { get; set; }

        [Display(Name = "Postcode")]
        public string? PostalCode { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        [Display(Name = "Country of Birth")]
        public int? CountryOfBirthId { get; set; }
        public Country? CountryOfBirth { get; set; }

        [Display(Name = "Language Spoken at Home")]
        public int? LanguageSpokenAtHomeId { get; set; }
        public Language? LanguageSpokenAtHome { get; set; }

        [Display(Name = "Ancestry")]
        public int? AncestryId { get; set; }
        public Ancestry? Ancestry { get; set; }

        [Display(Name = "Aboriginal and Torres Strait Islander Status")]
        public int? AtsiStatusId { get; set; }
        public AboriginalTorresStraitIslanderStatus? AtsiStatus { get; set; }

        [Display(Name = "Occupation")]
        public int? OccupationId { get; set; }
        public Occupation? Occupation { get; set; }

        [Display(Name = "Deceased")]
        public bool IsDeceased { get; set; } = false;

        [Display(Name = "Date of Death")]
        [DataType(DataType.Date)]
        public DateTime? DateOfDeath { get; set; }

        // Jurisdiction Fields
        public int? Jurisdiction1Id { get; set; }
        public Jurisdiction? Jurisdiction1 { get; set; }

        public int? Jurisdiction2Id { get; set; }
        public Jurisdiction? Jurisdiction2 { get; set; }

        public int? Jurisdiction3Id { get; set; }
        public Jurisdiction? Jurisdiction3 { get; set; }

        public int? Jurisdiction4Id { get; set; }
        public Jurisdiction? Jurisdiction4 { get; set; }

        public int? Jurisdiction5Id { get; set; }
        public Jurisdiction? Jurisdiction5 { get; set; }

        public ICollection<Note> Notes { get; set; } = new List<Note>();

        [Display(Name = "Created")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Created By")]
        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        // Soft Delete Properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }
}
