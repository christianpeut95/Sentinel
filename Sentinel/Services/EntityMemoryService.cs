using Sentinel.Models.Timeline;
using Microsoft.Extensions.Caching.Memory;

namespace Sentinel.Services
{
    /// <summary>
    /// Service for tracking entities across timeline entries (for autocomplete)
    /// </summary>
    public interface IEntityMemoryService
    {
        /// <summary>
        /// Get all known people mentioned in this case's timeline
        /// </summary>
        Task<List<EntitySuggestion>> GetKnownPeopleAsync(Guid caseId);

        /// <summary>
        /// Get all known locations from this case's timeline
        /// </summary>
        Task<List<EntitySuggestion>> GetKnownLocationsAsync(Guid caseId);

        /// <summary>
        /// Get convention locations defined for this case
        /// </summary>
        Task<Dictionary<string, ConventionLocation>> GetConventionsAsync(Guid caseId);

        /// <summary>
        /// Add or update a convention location
        /// </summary>
        Task AddConventionAsync(Guid caseId, string conventionName, ConventionLocation location);

        /// <summary>
        /// Clear cache for a case (call after saving timeline)
        /// </summary>
        void ClearCache(Guid caseId);
    }

    public class EntityMemoryService : IEntityMemoryService
    {
        private readonly ITimelineStorageService _storageService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EntityMemoryService> _logger;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

        public EntityMemoryService(
            ITimelineStorageService storageService,
            IMemoryCache cache,
            ILogger<EntityMemoryService> logger)
        {
            _storageService = storageService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<EntitySuggestion>> GetKnownPeopleAsync(Guid caseId)
        {
            var cacheKey = $"timeline_people_{caseId}";
            
            if (_cache.TryGetValue(cacheKey, out List<EntitySuggestion>? cached))
            {
                return cached ?? new List<EntitySuggestion>();
            }

            var timeline = await _storageService.LoadTimelineAsync(caseId);
            if (timeline == null)
                return new List<EntitySuggestion>();

            var people = new Dictionary<string, EntitySuggestion>();

            foreach (var entry in timeline.Entries)
            {
                foreach (var entity in entry.Entities.Where(e => e.EntityType == EntityType.Person))
                {
                    var key = entity.NormalizedValue?.ToLowerInvariant() ?? entity.RawText.ToLowerInvariant();
                    
                    if (!people.ContainsKey(key))
                    {
                        people[key] = new EntitySuggestion
                        {
                            DisplayText = entity.LinkedRecordDisplayName ?? entity.RawText,
                            Description = entity.IsConfirmed ? "Previously mentioned" : "Unconfirmed",
                            RecordId = entity.LinkedRecordId,
                            RecordType = entity.LinkedRecordType,
                            Score = entity.IsConfirmed ? 1.0 : 0.5
                        };
                    }
                }
            }

            var result = people.Values.OrderByDescending(p => p.Score).ToList();
            _cache.Set(cacheKey, result, _cacheExpiration);
            
            return result;
        }

        public async Task<List<EntitySuggestion>> GetKnownLocationsAsync(Guid caseId)
        {
            var cacheKey = $"timeline_locations_{caseId}";
            
            if (_cache.TryGetValue(cacheKey, out List<EntitySuggestion>? cached))
            {
                return cached ?? new List<EntitySuggestion>();
            }

            var timeline = await _storageService.LoadTimelineAsync(caseId);
            if (timeline == null)
                return new List<EntitySuggestion>();

            var locations = new Dictionary<string, EntitySuggestion>();

            // Add convention locations first (highest priority)
            foreach (var convention in timeline.Conventions)
            {
                locations[convention.Key] = new EntitySuggestion
                {
                    DisplayText = convention.Value.LocationName ?? convention.Key,
                    Description = convention.Value.FreeTextAddress ?? "User-defined location",
                    RecordId = convention.Value.LocationId,
                    RecordType = "Location",
                    Latitude = convention.Value.Latitude,
                    Longitude = convention.Value.Longitude,
                    Address = convention.Value.FreeTextAddress,
                    Score = 1.0
                };
            }

            // Add locations from entries
            foreach (var entry in timeline.Entries)
            {
                foreach (var entity in entry.Entities.Where(e => e.EntityType == EntityType.Location))
                {
                    var key = entity.NormalizedValue?.ToLowerInvariant() ?? entity.RawText.ToLowerInvariant();
                    
                    if (!locations.ContainsKey(key))
                    {
                        locations[key] = new EntitySuggestion
                        {
                            DisplayText = entity.LinkedRecordDisplayName ?? entity.RawText,
                            Description = entity.IsConfirmed ? "Previously mentioned" : "Unconfirmed",
                            RecordId = entity.LinkedRecordId,
                            RecordType = entity.LinkedRecordType,
                            Score = entity.IsConfirmed ? 0.8 : 0.4
                        };
                    }
                }
            }

            var result = locations.Values.OrderByDescending(l => l.Score).ToList();
            _cache.Set(cacheKey, result, _cacheExpiration);
            
            return result;
        }

        public async Task<Dictionary<string, ConventionLocation>> GetConventionsAsync(Guid caseId)
        {
            var timeline = await _storageService.LoadTimelineAsync(caseId);
            return timeline?.Conventions ?? new Dictionary<string, ConventionLocation>();
        }

        public async Task AddConventionAsync(Guid caseId, string conventionName, ConventionLocation location)
        {
            var timeline = await _storageService.LoadTimelineAsync(caseId);
            
            if (timeline == null)
            {
                timeline = new CaseTimelineData { CaseId = caseId };
            }

            timeline.Conventions[conventionName.ToLowerInvariant()] = location;
            await _storageService.SaveTimelineAsync(timeline);
            
            ClearCache(caseId);
            
            _logger.LogInformation("Convention '{ConventionName}' added for case {CaseId}", conventionName, caseId);
        }

        public void ClearCache(Guid caseId)
        {
            _cache.Remove($"timeline_people_{caseId}");
            _cache.Remove($"timeline_locations_{caseId}");
        }
    }
}
