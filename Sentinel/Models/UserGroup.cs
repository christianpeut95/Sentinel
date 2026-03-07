namespace Sentinel.Models
{
    public class UserGroup
    {
        public string UserId { get; set; } = string.Empty;
        public int GroupId { get; set; }

        public ApplicationUser? User { get; set; }
        public Group? Group { get; set; }
    }
}
