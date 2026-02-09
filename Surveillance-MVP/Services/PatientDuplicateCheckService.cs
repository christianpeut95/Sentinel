using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public class PatientDuplicateCheckService : IPatientDuplicateCheckService
    {
        private readonly ApplicationDbContext _context;

        public PatientDuplicateCheckService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PotentialDuplicate>> FindPotentialDuplicatesAsync(Patient patient)
        {
            var potentialDuplicates = new List<PotentialDuplicate>();

            // Get all patients for comparison (in production, add better filtering)
            var existingPatients = await _context.Patients
                .Include(p => p.CountryOfBirth)
                .Include(p => p.Ethnicity)
                .Include(p => p.LanguageSpokenAtHome)
                .AsNoTracking()
                .ToListAsync();

            foreach (var existing in existingPatients)
            {
                var matchReasons = new List<string>();
                var score = 0;

                // Check name similarity
                var nameMatch = CheckNameMatch(patient, existing);
                if (nameMatch.isMatch)
                {
                    matchReasons.Add(nameMatch.reason);
                    score += nameMatch.score;
                }

                // Check date of birth match
                if (patient.DateOfBirth.HasValue && existing.DateOfBirth.HasValue &&
                    patient.DateOfBirth.Value.Date == existing.DateOfBirth.Value.Date)
                {
                    matchReasons.Add("Same date of birth");
                    score += 30;
                }

                // Check address similarity
                var addressMatch = CheckAddressMatch(patient, existing);
                if (addressMatch.isMatch)
                {
                    matchReasons.Add(addressMatch.reason);
                    score += addressMatch.score;
                }

                // Check sex at birth match
                if (patient.SexAtBirthId.HasValue && existing.SexAtBirthId.HasValue &&
                    patient.SexAtBirthId == existing.SexAtBirthId)
                {
                    matchReasons.Add("Same sex at birth");
                    score += 5;
                }

                // If we have any matches with a reasonable score, add to results
                if (matchReasons.Any() && score >= 20)
                {
                    potentialDuplicates.Add(new PotentialDuplicate
                    {
                        Patient = existing,
                        MatchReasons = matchReasons,
                        MatchScore = score
                    });
                }
            }

            // Return top matches sorted by score
            return potentialDuplicates
                .OrderByDescending(d => d.MatchScore)
                .Take(5)
                .ToList();
        }

        private (bool isMatch, string reason, int score) CheckNameMatch(Patient patient, Patient existing)
        {
            var givenMatch = StringSimilarity(patient.GivenName ?? "", existing.GivenName ?? "");
            var familyMatch = StringSimilarity(patient.FamilyName ?? "", existing.FamilyName ?? "");

            // Exact match on both names
            if (givenMatch >= 0.95 && familyMatch >= 0.95)
            {
                return (true, "Exact name match", 40);
            }

            // Very close match on both names
            if (givenMatch >= 0.8 && familyMatch >= 0.8)
            {
                return (true, "Very similar name", 30);
            }

            // Swapped first/last name (common data entry error)
            var swappedGiven = StringSimilarity(patient.GivenName ?? "", existing.FamilyName ?? "");
            var swappedFamily = StringSimilarity(patient.FamilyName ?? "", existing.GivenName ?? "");
            if (swappedGiven >= 0.9 && swappedFamily >= 0.9)
            {
                return (true, "Possible swapped first/last name", 35);
            }

            // Similar names
            if ((givenMatch >= 0.7 && familyMatch >= 0.7) || 
                (givenMatch >= 0.9 || familyMatch >= 0.9))
            {
                return (true, "Similar name", 20);
            }

            return (false, string.Empty, 0);
        }

        private (bool isMatch, string reason, int score) CheckAddressMatch(Patient patient, Patient existing)
        {
            // Check if both have addresses
            if (string.IsNullOrWhiteSpace(patient.AddressLine) && string.IsNullOrWhiteSpace(existing.AddressLine))
            {
                return (false, string.Empty, 0);
            }

            var addressMatch = StringSimilarity(patient.AddressLine ?? "", existing.AddressLine ?? "");
            var cityMatch = StringSimilarity(patient.City ?? "", existing.City ?? "");
            var postalMatch = StringSimilarity(patient.PostalCode ?? "", existing.PostalCode ?? "");

            // Exact address match
            if (addressMatch >= 0.95 && cityMatch >= 0.95)
            {
                return (true, "Same address", 25);
            }

            // Same postcode
            if (postalMatch >= 0.95 && !string.IsNullOrWhiteSpace(patient.PostalCode))
            {
                return (true, "Same postcode", 15);
            }

            // Similar address
            if (addressMatch >= 0.7 && cityMatch >= 0.7)
            {
                return (true, "Similar address", 15);
            }

            return (false, string.Empty, 0);
        }

        private double StringSimilarity(string s1, string s2)
        {
            if (string.IsNullOrWhiteSpace(s1) && string.IsNullOrWhiteSpace(s2))
                return 0;

            if (string.IsNullOrWhiteSpace(s1) || string.IsNullOrWhiteSpace(s2))
                return 0;

            s1 = s1.ToLowerInvariant().Trim();
            s2 = s2.ToLowerInvariant().Trim();

            if (s1 == s2)
                return 1.0;

            // Levenshtein distance-based similarity
            var distance = LevenshteinDistance(s1, s2);
            var maxLength = Math.Max(s1.Length, s2.Length);
            return 1.0 - ((double)distance / maxLength);
        }

        private int LevenshteinDistance(string s1, string s2)
        {
            var d = new int[s1.Length + 1, s2.Length + 1];

            for (var i = 0; i <= s1.Length; i++)
                d[i, 0] = i;

            for (var j = 0; j <= s2.Length; j++)
                d[0, j] = j;

            for (var i = 1; i <= s1.Length; i++)
            {
                for (var j = 1; j <= s2.Length; j++)
                {
                    var cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[s1.Length, s2.Length];
        }
    }
}
