using System.ComponentModel.DataAnnotations;
using Sentinel.Models.Lookups;

namespace Sentinel.Models;

public class Outbreak
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public OutbreakType Type { get; set; } = OutbreakType.Traditional;
    
    public OutbreakStatus Status { get; set; } = OutbreakStatus.Active;
    
    // Confirmation status (reusing CaseStatus table)
    public int? ConfirmationStatusId { get; set; }
    public CaseStatus? ConfirmationStatus { get; set; }
    
    // Parent-Child hierarchy for sub-investigations
    public int? ParentOutbreakId { get; set; }
    public Outbreak? ParentOutbreak { get; set; }
    public ICollection<Outbreak> ChildOutbreaks { get; set; } = new List<Outbreak>();
    
    public Guid? IndexCaseId { get; set; }
    public Case? IndexCase { get; set; }


    
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    public Guid? PrimaryDiseaseId { get; set; }
    public Disease? PrimaryDisease { get; set; }
    
    public Guid? PrimaryLocationId { get; set; }
    public Location? PrimaryLocation { get; set; }
    
    public Guid? PrimaryEventId { get; set; }
    public Event? PrimaryEvent { get; set; }
    
    public string? LeadInvestigatorId { get; set; }
    public ApplicationUser? LeadInvestigator { get; set; }
    
    public ICollection<OutbreakTeamMember> TeamMembers { get; set; } = new List<OutbreakTeamMember>();
    public ICollection<OutbreakCaseDefinition> CaseDefinitions { get; set; } = new List<OutbreakCaseDefinition>();
    public ICollection<OutbreakCase> OutbreakCases { get; set; } = new List<OutbreakCase>();
    public ICollection<OutbreakTimeline> TimelineEvents { get; set; } = new List<OutbreakTimeline>();
    public ICollection<OutbreakSearchQuery> SavedSearches { get; set; } = new List<OutbreakSearchQuery>();
    
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
}
