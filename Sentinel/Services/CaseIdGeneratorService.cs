using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services
{
    public class CaseIdGeneratorService : ICaseIdGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public CaseIdGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNextCaseIdAsync()
        {
            int currentYear = DateTime.UtcNow.Year;
            
            string yearPrefix = $"C-{currentYear}-";
            
            var cases = await _context.Cases
                .Where(c => !string.IsNullOrEmpty(c.FriendlyId) && c.FriendlyId.StartsWith(yearPrefix))
                .Select(c => c.FriendlyId)
                .ToListAsync();

            int maxSequence = 0;

            if (cases.Any())
            {
                foreach (var id in cases)
                {
                    var parts = id.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
                    {
                        if (sequence > maxSequence)
                        {
                            maxSequence = sequence;
                        }
                    }
                }
            }

            int nextSequence = maxSequence + 1;
            return $"C-{currentYear}-{nextSequence:D4}";
        }
    }
}
