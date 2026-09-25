using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public class LocationDuplicateCheckService : ILocationDuplicateCheckService
    {
        private const int MaximumCandidateLocations = 250;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LocationDuplicateCheckService> _logger;

        public LocationDuplicateCheckService(
            ApplicationDbContext context,
            ILogger<LocationDuplicateCheckService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<LocationDuplicate>> FindPotentialDuplicatesAsync(Location location)
        {
            var duplicates = new List<LocationDuplicate>();

            if (string.IsNullOrWhiteSpace(location.Name))
            {
                return duplicates;
            }

            var existingLocations = await GetCandidateLocationsAsync(location);

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
            return duplicates
                .OrderByDescending(d => d.MatchScore)
                .ThenBy(d => d.Location.Id)
                .ToList();
        }

        private async Task<List<Location>> GetCandidateLocationsAsync(Location location)
        {
            var namePrefix = GetCandidatePrefix(location.Name);
            var addressPrefix = GetCandidatePrefix(location.Address);
            var hasLocationId = location.Id != Guid.Empty;
            var hasCoordinates = location.Latitude.HasValue && location.Longitude.HasValue;

            // A small geographic bounding box is used only to select candidates.
            // The existing Haversine calculation remains the authority for the
            // final duplicate score.
            const decimal coordinateCandidateRange = 0.02m;
            var minimumLatitude = hasCoordinates ? location.Latitude!.Value - coordinateCandidateRange : default;
            var maximumLatitude = hasCoordinates ? location.Latitude!.Value + coordinateCandidateRange : default;
            var minimumLongitude = hasCoordinates ? location.Longitude!.Value - coordinateCandidateRange : default;
            var maximumLongitude = hasCoordinates ? location.Longitude!.Value + coordinateCandidateRange : default;

            // Keep duplicate checks bounded. Query predicates are compiled by
            // EF Core and use only typed application values; no SQL is built
            // from a location name or address.
            var candidates = await _context.Locations
                .AsNoTracking()
                .Where(existing =>
                    existing.IsActive &&
                    (!hasLocationId || existing.Id != location.Id) &&
                    (
                        (!string.IsNullOrEmpty(namePrefix) &&
                            existing.Name != null && existing.Name.StartsWith(namePrefix)) ||
                        (!string.IsNullOrEmpty(addressPrefix) &&
                            existing.Address != null && existing.Address.StartsWith(addressPrefix)) ||
                        (location.OrganizationId.HasValue && existing.OrganizationId == location.OrganizationId) ||
                        (hasCoordinates && existing.Latitude >= minimumLatitude && existing.Latitude <= maximumLatitude &&
                            existing.Longitude >= minimumLongitude && existing.Longitude <= maximumLongitude)
                    ))
                .OrderBy(existing => existing.Id)
                .Take(MaximumCandidateLocations + 1)
                .ToListAsync();

            if (candidates.Count > MaximumCandidateLocations)
            {
                _logger.LogWarning(
                    "Location duplicate candidate search reached the {CandidateLimit} record limit for location {LocationId}. " +
                    "Only bounded candidates were scored.",
                    MaximumCandidateLocations,
                    location.Id == Guid.Empty ? "(new)" : location.Id);

                candidates.RemoveAt(candidates.Count - 1);
            }

            return candidates;
        }

        private static string? GetCandidatePrefix(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalized = value.Trim();
            return normalized[..Math.Min(normalized.Length, 3)];
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
