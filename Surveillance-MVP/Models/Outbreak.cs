using System.ComponentModel.DataAnnotations;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Models;

public class Outbreak
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string? Description { get; set; }
    
    public OutbreakStatus Status { get; set; } = OutbreakStatus.Active;
    
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
