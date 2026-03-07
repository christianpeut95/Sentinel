using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class ExposureEvent : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Exposed Case")]
        public Guid ExposedCaseId { get; set; }
        public Case? ExposedCase { get; set; }

        [Required]
        [Display(Name = "Exposure Type")]
        public ExposureType ExposureType { get; set; }

        [Required]
        [Display(Name = "Exposure Start Date/Time")]
        public DateTime ExposureStartDate { get; set; }

        [Display(Name = "Exposure End Date/Time")]
        public DateTime? ExposureEndDate { get; set; }

        // For Event-based exposures
        [Display(Name = "Event")]
        public Guid? EventId { get; set; }
        public Event? Event { get; set; }

        // For Location-based exposures
        [Display(Name = "Location")]
        public Guid? LocationId { get; set; }
        public Location? Location { get; set; }

        // For Contact-based exposures
        [Display(Name = "Source Case")]
        public Guid? SourceCaseId { get; set; }
        public Case? SourceCase { get; set; }

        [Display(Name = "Contact Classification")]
        public int? ContactClassificationId { get; set; }
        public Lookups.ContactClassification? ContactClassification { get; set; }

        // For Travel-based exposures
        [Display(Name = "Country")]
        [StringLength(3)]
        public string? CountryCode { get; set; }

        // General fields
        [Display(Name = "Free-text Location")]
        [StringLength(500)]
        public string? FreeTextLocation { get; set; }

        [Display(Name = "Description")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? Description { get; set; }

        [Display(Name = "Exposure Status")]
        public ExposureStatus ExposureStatus { get; set; } = ExposureStatus.Unknown;

        [Display(Name = "Confidence Level")]
        [StringLength(50)]
        public string? ConfidenceLevel { get; set; }

        [Display(Name = "Defaulted from Residential Address")]
        public bool IsDefaultedFromResidentialAddress { get; set; } = false;

        [Display(Name = "Primary Reporting Exposure")]
        public bool IsReportingExposure { get; set; } = false;

        [Display(Name = "Interstate Travel")]
        public bool IsInterstateTravel { get; set; } = false;

        [Display(Name = "Interstate Origin State")]
        [StringLength(100)]
        public string? InterstateOriginState { get; set; }

        // Structured Address Fields (for better reporting and geocoding)
        [Display(Name = "Address Line")]
        [StringLength(200)]
        public string? AddressLine { get; set; }

        [Display(Name = "Suburb/City")]
        [StringLength(100)]
        public string? City { get; set; }

        [Display(Name = "State/Region")]
        [StringLength(100)]
        public string? State { get; set; }

        [Display(Name = "Postcode")]
        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Country")]
        [StringLength(100)]
        public string? Country { get; set; }

        // Geocoding Fields
        [Display(Name = "Latitude")]
        public decimal? Latitude { get; set; }

        [Display(Name = "Longitude")]
        public decimal? Longitude { get; set; }

        [Display(Name = "Geocoding Accuracy")]
        [StringLength(50)]
        public string? GeocodingAccuracy { get; set; }

        [Display(Name = "Geocoded Date")]
        public DateTime? GeocodedDate { get; set; }

        [Display(Name = "Investigation Notes")]
        [StringLength(2000)]
        [DataType(DataType.MultilineText)]
        public string? InvestigationNotes { get; set; }

        [Display(Name = "Status Changed Date")]
        public DateTime? StatusChangedDate { get; set; }

        [Display(Name = "Status Changed By")]
        [StringLength(450)]
        public string? StatusChangedByUserId { get; set; }

        // Audit fields - these will be populated by the DbContext
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; }

        [Display(Name = "Created By")]
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        [Display(Name = "Last Modified")]
        public DateTime? LastModified { get; set; }

        [Display(Name = "Last Modified By")]
        [StringLength(450)]
        public string? LastModifiedByUserId { get; set; }

        // Soft delete fields
        [Display(Name = "Is Deleted")]
        public bool IsDeleted { get; set; }

        [Display(Name = "Deleted At")]
        public DateTime? DeletedAt { get; set; }

        [Display(Name = "Deleted By")]
        [StringLength(450)]
        public string? DeletedByUserId { get; set; }
    }
}
