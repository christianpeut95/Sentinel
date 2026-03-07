using System.Collections.Generic;
using System.Threading.Tasks;
using Sentinel.Models;

namespace Sentinel.Services
{
    public interface IPatientDuplicateCheckService
    {
        Task<List<PotentialDuplicate>> FindPotentialDuplicatesAsync(Patient patient);
    }

    public class PotentialDuplicate
    {
        public Patient Patient { get; set; } = default!;
        public List<string> MatchReasons { get; set; } = new();
        public int MatchScore { get; set; }
    }
}
