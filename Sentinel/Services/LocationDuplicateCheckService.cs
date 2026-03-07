using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public class LocationDuplicateCheckService : ILocationDuplicateCheckService
    {
        private readonly ApplicationDbContext _context;

        public LocationDuplicateCheckService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<LocationDuplicate>> FindPotentialDuplicatesAsync(Location location)
        {
            var duplicates = new List<LocationDuplicate>();

            if (string.IsNullOrWhiteSpace(location.Name))
            {
                return duplicates;
            }

            // Get all active locations for comparison
            var existingLocations = await _context.Locations
                .Include(l => l.LocationType)
                .Include(l => l.Organization)
                .Where(l => l.IsActive)
                .ToListAsync();

            foreach (var existing in existingLocations)
            {
                var matchScore = 0;
                var matchReasons = new List<string>();

                // Exact name match (case-insensitive) - high score
                if (existing.Name.Equals(location.Name, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 50;
                    matchReasons.Add("Exact Name Match");
                }
                // Similar name (contains) - medium score
                else if (existing.Name.Contains(location.Name, StringComparison.OrdinalIgnoreCase) ||
                         location.Name.Contains(existing.Name, StringComparison.OrdinalIgnoreCase))
                {
                    matchScore += 30;
                    matchReasons.Add("Similar Name");
                }

                // Address comparison
                if (!string.IsNullOrWhiteSpace(location.Address) && !string.IsNullOrWhiteSpace(existing.Address))
                {
                    // Exact address match
                    if (existing.Address.Equals(location.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 40;
                        matchReasons.Add("Exact Address Match");
                    }
                    // Similar address
                    else if (existing.Address.Contains(location.Address, StringComparison.OrdinalIgnoreCase) ||
                             location.Address.Contains(existing.Address, StringComparison.OrdinalIgnoreCase))
                    {
                        matchScore += 20;
                        matchReasons.Add("Similar Address");
                    }
                }

                // Same location type - adds confidence
                if (location.LocationTypeId.HasValue && 
                    existing.LocationTypeId.HasValue && 
                    location.LocationTypeId == existing.LocationTypeId)
                {
                    matchScore += 10;
                    matchReasons.Add("Same Type");
                }

                // Coordinates proximity check (if both have coordinates)
                if (location.Latitude.HasValue && location.Longitude.HasValue &&
                    existing.Latitude.HasValue && existing.Longitude.HasValue)
                {
                    var distance = CalculateDistance(
                        (double)location.Latitude.Value, (double)location.Longitude.Value,
                        (double)existing.Latitude.Value, (double)existing.Longitude.Value);

                    // Within 100 meters - very likely same location
                    if (distance < 0.1)
                    {
                        matchScore += 30;
                        matchReasons.Add("Same Coordinates");
                    }
                    // Within 1 km - nearby location
                    else if (distance < 1.0)
                    {
                        matchScore += 15;
                        matchReasons.Add("Nearby Location");
                    }
                }

                // Same organization - adds confidence
                if (location.OrganizationId.HasValue && 
                    existing.OrganizationId.HasValue && 
                    location.OrganizationId == existing.OrganizationId)
                {
                    matchScore += 10;
                    matchReasons.Add("Same Organization");
                }

                // Consider it a potential duplicate if score >= 50
                if (matchScore >= 50)
                {
                    duplicates.Add(new LocationDuplicate
                    {
                        Location = existing,
                        MatchScore = matchScore,
                        MatchReasons = matchReasons
                    });
                }
            }

            // Return sorted by match score (highest first)
            return duplicates.OrderByDescending(d => d.MatchScore).ToList();
        }

        /// <summary>
        /// Calculate distance between two coordinates in kilometers using Haversine formula.
        /// </summary>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
