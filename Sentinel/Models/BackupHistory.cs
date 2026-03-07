using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sentinel.Models
{
    public class BackupHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string BackupType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string BackupFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string BackupFilePath { get; set; } = string.Empty;

        [Required]
        public long SizeInBytes { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public bool Success { get; set; } = true;

        public string? ErrorMessage { get; set; }

        [MaxLength(256)]
        public string? CreatedBy { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public double SizeInMB => SizeInBytes / (1024.0 * 1024.0);

        [NotMapped]
        public TimeSpan Duration => EndTime - StartTime;

        [NotMapped]
        public bool FileExists => System.IO.File.Exists(BackupFilePath);
    }
}
