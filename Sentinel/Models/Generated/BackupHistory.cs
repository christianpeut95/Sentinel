using System;
using System.Collections.Generic;

namespace Sentinel.Models.Generated;

public partial class BackupHistory
{
    public int Id { get; set; }

    public string BackupType { get; set; } = null!;

    public string BackupFileName { get; set; } = null!;

    public string BackupFilePath { get; set; } = null!;

    public long SizeInBytes { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
}
