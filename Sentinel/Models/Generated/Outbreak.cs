using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Outbreak
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int Type { get; set; }

    public int Status { get; set; }

    public int? ConfirmationStatusId { get; set; }

    public int? ParentOutbreakId { get; set; }

    public Guid? IndexCaseId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public Guid? PrimaryDiseaseId { get; set; }

    public Guid? PrimaryLocationId { get; set; }

    public Guid? PrimaryEventId { get; set; }

    public string? LeadInvestigatorId { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public bool IsDeleted { get; set; }

    public virtual CaseStatus? ConfirmationStatus { get; set; }

    public virtual Case? IndexCase { get; set; }

    public virtual ICollection<Outbreak> InverseParentOutbreak { get; set; } = new List<Outbreak>();

    public virtual AspNetUser? LeadInvestigator { get; set; }

    public virtual ICollection<Note> Notes { get; set; } = new List<Note>();

    public virtual ICollection<OutbreakCaseDefinition> OutbreakCaseDefinitions { get; set; } = new List<OutbreakCaseDefinition>();

    public virtual ICollection<OutbreakCase> OutbreakCases { get; set; } = new List<OutbreakCase>();

    public virtual ICollection<OutbreakLineListConfiguration> OutbreakLineListConfigurations { get; set; } = new List<OutbreakLineListConfiguration>();

    public virtual ICollection<OutbreakSearchQuery> OutbreakSearchQueries { get; set; } = new List<OutbreakSearchQuery>();

    public virtual ICollection<OutbreakTeamMember> OutbreakTeamMembers { get; set; } = new List<OutbreakTeamMember>();

    public virtual ICollection<OutbreakTimeline> OutbreakTimelines { get; set; } = new List<OutbreakTimeline>();

    public virtual Outbreak? ParentOutbreak { get; set; }

    public virtual Disease? PrimaryDisease { get; set; }

    public virtual Event? PrimaryEvent { get; set; }

    public virtual Location? PrimaryLocation { get; set; }
}
