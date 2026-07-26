using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class OutbreakLineListConfiguration
{
    public int Id { get; set; }

    public int OutbreakId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string SelectedFields { get; set; } = null!;

    public string SortConfiguration { get; set; } = null!;

    public string? FilterConfiguration { get; set; }

    public string? UserId { get; set; }

    public bool IsShared { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public string? CreatedByUserId { get; set; }

    public virtual AspNetUser? CreatedByUser { get; set; }

    public virtual Outbreak Outbreak { get; set; } = null!;

    public virtual AspNetUser? User { get; set; }
}
