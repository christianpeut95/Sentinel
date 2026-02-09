using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;

namespace Surveillance_MVP.Services
{
    public class PatientIdGeneratorService : IPatientIdGeneratorService
    {
        private readonly ApplicationDbContext _context;

        public PatientIdGeneratorService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNextPatientIdAsync()
        {
            // Get current year
            int currentYear = DateTime.UtcNow.Year;
            
            // Find the highest existing patient ID number for the current year
            string yearPrefix = $"P-{currentYear}-";
            
            var patients = await _context.Patients
                .Where(p => !string.IsNullOrEmpty(p.FriendlyId) && p.FriendlyId.StartsWith(yearPrefix))
                .Select(p => p.FriendlyId)
                .ToListAsync();

            int maxSequence = 0;

            if (patients.Any())
            {
                foreach (var id in patients)
                {
                    // Extract sequence number after "P-YYYY-"
                    // Format is: P-2025-0001
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

            // Generate next ID for the current year
            int nextSequence = maxSequence + 1;
            return $"P-{currentYear}-{nextSequence:D4}"; // Format as P-2025-0001, P-2025-0002, etc.
        }
    }
}

