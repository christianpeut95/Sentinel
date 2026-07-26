using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class ExposureEvent
{
    public Guid Id { get; set; }

    public Guid ExposedCaseId { get; set; }

    public int ExposureType { get; set; }

    public DateTime ExposureStartDate { get; set; }

    public DateTime? ExposureEndDate { get; set; }

    public Guid? EventId { get; set; }

    public Guid? LocationId { get; set; }

    public Guid? SourceCaseId { get; set; }

    public int? ContactClassificationId { get; set; }

    public string? CountryCode { get; set; }

    public string? FreeTextLocation { get; set; }

    public string? Description { get; set; }

    public int ExposureStatus { get; set; }

    public string? ConfidenceLevel { get; set; }

    public bool IsDefaultedFromResidentialAddress { get; set; }

    public bool IsReportingExposure { get; set; }

    public bool IsInterstateTravel { get; set; }

    public string? InterstateOriginState { get; set; }

    public string? AddressLine { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public string? GeocodingAccuracy { get; set; }

    public DateTime? GeocodedDate { get; set; }

    public string? InvestigationNotes { get; set; }

    public DateTime? StatusChangedDate { get; set; }

    public string? StatusChangedByUserId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedByUserId { get; set; }

    public DateTime? LastModified { get; set; }

    public string? LastModifiedByUserId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedByUserId { get; set; }

    public virtual ContactClassification? ContactClassification { get; set; }

    public virtual Event? Event { get; set; }

    public virtual Case ExposedCase { get; set; } = null!;

    public virtual Location? Location { get; set; }

    public virtual Case? SourceCase { get; set; }
}
