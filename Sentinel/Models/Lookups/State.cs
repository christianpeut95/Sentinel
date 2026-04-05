namespace Sentinel.Models.Lookups
{
    public class State
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;   // NSW, VIC, QLD, etc.
        public string Name { get; set; } = string.Empty;   // New South Wales, Victoria, etc.
        public bool IsActive { get; set; } = true;
    }
}
