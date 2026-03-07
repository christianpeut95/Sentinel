using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models
{
    public class TaskCallAttempt
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Task")]
        public Guid TaskId { get; set; }
        public CaseTask? Task { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Attempted By User")]
        public string AttemptedByUserId { get; set; } = string.Empty;
        public ApplicationUser? AttemptedByUser { get; set; }

        [Required]
        [Display(Name = "Attempted At")]
        public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Outcome")]
        public CallOutcome Outcome { get; set; }

        [StringLength(2000)]
        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        public string? Notes { get; set; }

        [Display(Name = "Call Duration (seconds)")]
        public int? DurationSeconds { get; set; }

        [Display(Name = "Next Callback Scheduled")]
        public DateTime? NextCallbackScheduled { get; set; }

        [StringLength(50)]
        [Display(Name = "Phone Number Called")]
        public string? PhoneNumberCalled { get; set; }
    }

    public enum CallOutcome
    {
        [Display(Name = "Completed")]
        Completed = 0,

        [Display(Name = "No Answer")]
        NoAnswer = 1,

        [Display(Name = "Busy")]
        Busy = 2,

        [Display(Name = "Voicemail")]
        Voicemail = 3,

        [Display(Name = "Refused")]
        Refused = 4,

        [Display(Name = "Wrong Number")]
        WrongNumber = 5,

        [Display(Name = "Language Barrier")]
        LanguageBarrier = 6,

        [Display(Name = "Disconnected")]
        Disconnected = 7,

        [Display(Name = "Call Back Requested")]
        CallBackRequested = 8
    }
}
