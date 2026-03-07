namespace Sentinel.Models;

public class OutbreakTeamMember
{
    public int Id { get; set; }
    public int OutbreakId { get; set; }
    public Outbreak Outbreak { get; set; } = null!;
    
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    
    public OutbreakRole Role { get; set; }
    
    public DateTime AssignedDate { get; set; }
    public string? AssignedBy { get; set; }
    public DateTime? RemovedDate { get; set; }
    public string? RemovedBy { get; set; }
    public bool IsActive { get; set; } = true;
}
