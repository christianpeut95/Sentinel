namespace Surveillance_MVP.Models
{
    public interface ISoftDeletable
    {
        bool IsDeleted { get; set; }
        DateTime? DeletedAt { get; set; }
        string? DeletedByUserId { get; set; }
    }
}
