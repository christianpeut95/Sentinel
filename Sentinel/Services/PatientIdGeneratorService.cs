using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services
{
    public class PatientIdGeneratorService : IPatientIdGeneratorService
    {
        private readonly ApplicationDbContext _context;
        
        // Static thread-safe cache of recently attempted IDs
        // This persists across service instances and prevents trying the same ID twice
        private static readonly ConcurrentDictionary<string, DateTime> _recentlyAttemptedIds = new();
        private static readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(2);

        public PatientIdGeneratorService(ApplicationDbContext context)
        {
            _context = context;
            
            // Clean up expired entries periodically
            CleanupExpiredAttempts();
        }

        private void CleanupExpiredAttempts()
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _recentlyAttemptedIds
                .Where(kvp => now - kvp.Value > _cacheExpiry)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                _recentlyAttemptedIds.TryRemove(key, out _);
            }
        }

        public async Task<string> GenerateNextPatientIdAsync()
        {
            // Get current year
            int currentYear = DateTime.UtcNow.Year;
            string yearPrefix = $"P-{currentYear}-";
            
            // Query with explicit no tracking and fresh read
            var existingIds = await _context.Patients
                .AsNoTracking()
                .Where(p => !string.IsNullOrEmpty(p.FriendlyId) && p.FriendlyId.StartsWith(yearPrefix))
                .Select(p => p.FriendlyId)
                .ToListAsync();

            // Parse all sequence numbers that exist in DB
            var existingSequences = new HashSet<int>();
            foreach (var id in existingIds)
            {
                var parts = id.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
                {
                    existingSequences.Add(sequence);
                }
            }

            // Also add recently attempted IDs (even if not in DB yet)
            foreach (var attemptedId in _recentlyAttemptedIds.Keys)
            {
                if (attemptedId.StartsWith(yearPrefix))
                {
                    var parts = attemptedId.Split('-');
                    if (parts.Length == 3 && int.TryParse(parts[2], out int sequence))
                    {
                        existingSequences.Add(sequence);
                    }
                }
            }

            // Find max sequence
            int maxSequence = existingSequences.Any() ? existingSequences.Max() : 0;
            
            // Try to find the next available ID by checking multiple candidates
            int maxAttempts = 100;
            
            for (int offset = 1; offset <= maxAttempts; offset++)
            {
                int candidateSequence = maxSequence + offset;
                string candidateId = $"P-{currentYear}-{candidateSequence:D4}";
                
                // Skip if we already know this exists or was recently attempted
                if (existingSequences.Contains(candidateSequence))
                {
                    continue;
                }
                
                // Double-check with a fresh query to the database
                bool exists = await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(p => p.FriendlyId == candidateId);
                
                if (!exists)
                {
                    // Mark this ID as attempted (with timestamp)
                    _recentlyAttemptedIds.TryAdd(candidateId, DateTime.UtcNow);
                    
                    // Found a unique ID!
                    return candidateId;
                }
                
                // This ID was taken - add to our memory and try next
                existingSequences.Add(candidateSequence);
                _recentlyAttemptedIds.TryAdd(candidateId, DateTime.UtcNow);
            }
            
            // If we get here, something is very wrong
            throw new InvalidOperationException($"Could not find an available Patient ID after checking {maxAttempts} candidates starting from P-{currentYear}-{maxSequence + 1:D4}.");
        }
    }
}


