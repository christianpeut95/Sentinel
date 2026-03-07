using System.ComponentModel.DataAnnotations;

namespace Sentinel.Models;

public class OutbreakTimeline
{
    public int Id { get; set; }
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    public DateTime EventDate { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Description { get; set; }
    
    public TimelineEventType EventType { get; set; }
    
    public Guid? RelatedCaseId { get; set; }
    public int? RelatedNoteId { get; set; }
    
    public DateTime CreatedDate { get; set; }
    public string? CreatedBy { get; set; }
}
