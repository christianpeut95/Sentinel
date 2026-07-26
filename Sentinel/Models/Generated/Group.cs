using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class Group
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ReportFolderShare> ReportFolderShares { get; set; } = new List<ReportFolderShare>();

    public virtual ICollection<AspNetUser> Users { get; set; } = new List<AspNetUser>();
}
