using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public interface IJurisdictionService
    {
        Task<List<JurisdictionType>> GetActiveJurisdictionTypesAsync();
        Task<List<Jurisdiction>> GetJurisdictionsForTypeAsync(int jurisdictionTypeId, bool includeInactive = false);
        Task<Dictionary<int, List<Jurisdiction>>> GetGroupedJurisdictionsAsync();
        
        Task<List<Jurisdiction>> FindJurisdictionsByPostcodeAsync(string? postcode, string? state);
        Task<List<Jurisdiction>> FindJurisdictionsContainingPointAsync(double latitude, double longitude);
        
        Task<(bool success, string? error)> ValidateShapefileAsync(Stream fileStream);
        Task<string?> ConvertShapefileToGeoJsonAsync(Stream fileStream);
        
        // Bulk import methods
        Task<List<(string name, Dictionary<string, object> attributes, string geoJson)>> ExtractAllFeaturesFromShapefileAsync(Stream fileStream);
        Task<List<string>> GetShapefileAttributeNamesAsync(Stream fileStream);
    }
}
