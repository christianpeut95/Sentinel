using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public enum ExposureType
    {
        [Display(Name = "Event")]
        Event = 1,

        [Display(Name = "Location")]
        Location = 2,

        [Display(Name = "Contact")]
        Contact = 3,

        [Display(Name = "Travel")]
        Travel = 4,

        [Display(Name = "Locally Acquired")]
        LocallyAcquired = 5
    }

    public enum ExposureStatus
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Potential Exposure")]
        PotentialExposure = 1,

        [Display(Name = "Under Investigation")]
        UnderInvestigation = 2,

        [Display(Name = "Confirmed Exposure")]
        ConfirmedExposure = 3,

        [Display(Name = "Ruled Out")]
        RuledOut = 4
    }

    public enum ExposureTrackingMode
    {
        [Display(Name = "Optional")]
        Optional = 0,

        [Display(Name = "Locally Acquired")]
        LocallyAcquired = 1,

        [Display(Name = "Local - Specific Region")]
        LocalSpecificRegion = 2,

        [Display(Name = "Overseas Acquired")]
        OverseasAcquired = 3
    }

    public enum ContactType
    {
        [Display(Name = "Household")]
        Household = 1,

        [Display(Name = "Healthcare")]
        Healthcare = 2,

        [Display(Name = "Social")]
        Social = 3,

        [Display(Name = "Workplace")]
        Workplace = 4,

        [Display(Name = "School")]
        School = 5,

        [Display(Name = "Unknown")]
        Unknown = 6
    }
}
