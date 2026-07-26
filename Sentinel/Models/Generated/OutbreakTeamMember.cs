using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakTeamMember
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public string UserId { get; set; } = null!;

    public int Role { get; set; }

    public DateTime AssignedDate { get; set; }

    public string? AssignedBy { get; set; }

    public DateTime? RemovedDate { get; set; }

    public string? RemovedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Outbreak Outbreak { get; set; } = null!;

    public virtual AspNetUser User { get; set; } = null!;
}
