using System.ComponentModel.DataAnnotations;

namespace Surveillance_MVP.Models
{
    public class Note : IAuditable, ISoftDeletable
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(5000)]
        public string Content { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Subject")]
        public string? Subject { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Type")]
        public string Type { get; set; } = "Note";

        [StringLength(200)]
        [Display(Name = "Recipient")]
        public string? Recipient { get; set; }

        // Polymorphic relationship - can link to Patient OR Case OR Outbreak
        public Guid? PatientId { get; set; }
        public Patient? Patient { get; set; }

        public Guid? CaseId { get; set; }
        public Case? Case { get; set; }

        public int? OutbreakId { get; set; }
        public Outbreak? Outbreak { get; set; }

        [StringLength(500)]
        [Display(Name = "Attachment Path")]
        public string? AttachmentPath { get; set; }

        [StringLength(200)]
        [Display(Name = "Attachment Name")]
        public string? AttachmentFileName { get; set; }

        [Display(Name = "Attachment Size (bytes)")]
        public long? AttachmentSize { get; set; }

        [Display(Name = "Created By")]
        [StringLength(450)]
        public string? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedAt { get; set; }

        // Soft Delete Properties
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByUserId { get; set; }
    }

    public static class NoteType
    {
        public const string Note = "Note";
        public const string SMS = "SMS";
        public const string PhoneCall = "Phone Call";
        public const string Email = "Email";

        public static string[] GetAll() => new[] { Note, SMS, PhoneCall, Email };
    }
}


