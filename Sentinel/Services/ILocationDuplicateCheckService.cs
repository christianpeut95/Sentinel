using Sentinel.Models;

namespace Sentinel.Services
{
    public interface ILocationDuplicateCheckService
    {
        /// <summary>
        /// Finds potential duplicate locations based on name and address similarity.
        /// </summary>
        Task<List<LocationDuplicate>> FindPotentialDuplicatesAsync(Location location);
    }

    public class LocationDuplicate
    {
        public Location Location { get; set; } = default!;
        public int MatchScore { get; set; }
        public List<string> MatchReasons { get; set; } = new();
    }
}
