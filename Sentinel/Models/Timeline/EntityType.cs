using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models.Timeline
{
    /// <summary>
    /// Types of entities that can be extracted from natural language timeline entries
    /// </summary>
    public enum EntityType
    {
        [Display(Name = "Person")]
        Person = 1,

        [Display(Name = "Location")]
        Location = 2,

        [Display(Name = "Event")]
        Event = 3,

        [Display(Name = "Transport")]
        Transport = 4,

        [Display(Name = "Date/Time")]
        DateTime = 5,

        [Display(Name = "Duration")]
        Duration = 6,

        [Display(Name = "Activity")]
        Activity = 7
    }

    /// <summary>
    /// Confidence level for extracted entities
    /// </summary>
    public enum ConfidenceLevel
    {
        [Display(Name = "High")]
        High = 3,

        [Display(Name = "Medium")]
        Medium = 2,

        [Display(Name = "Low")]
        Low = 1,

        [Display(Name = "Uncertain")]
        Uncertain = 0
    }

    /// <summary>
    /// Relationship types between entities
    /// </summary>
    public enum RelationshipType
    {
        [Display(Name = "With (accompanied by)")]
        With = 1,

        [Display(Name = "Accompaniment")]
        Accompaniment = 1, // Alias for With

        [Display(Name = "At (located at)")]
        At = 2,

        [Display(Name = "At Location")]
        AtLocation = 2, // Alias for At

        [Display(Name = "Travel By")]
        TravelBy = 3,

        [Display(Name = "Via Transport")]
        ViaTransport = 3, // Alias for TravelBy

        [Display(Name = "During")]
        During = 4,

        [Display(Name = "At Event")]
        AtEvent = 4, // Alias for During

        [Display(Name = "For (duration)")]
        For = 5,

        [Display(Name = "For Duration")]
        ForDuration = 5, // Alias for For

        [Display(Name = "At Time")]
        AtTime = 6,

        [Display(Name = "Co-Occurrence")]
        CoOccurrence = 7,

        [Display(Name = "Sequence (then/after)")]
        Sequence = 8,

        [Display(Name = "Person to Person")]
        PersonToPerson = 9,

        [Display(Name = "Activity")]
        Activity = 10
    }
}
