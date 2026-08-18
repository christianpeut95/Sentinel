using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.IO.Compression;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Text.Json;

namespace Sentinel.Services
{
    public class JurisdictionService : IJurisdictionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<JurisdictionService> _logger;
        private const string GEOMETRY_CACHE_KEY = "JurisdictionGeometries";
        private const int MaximumArchiveEntries = 100;
        private const long MaximumArchiveUncompressedBytes = 1_073_741_824; // 1 GB
        private const double MaximumArchiveCompressionRatio = 200;
        private const int MaximumImportedFeatures = 50_000;

        public JurisdictionService(ApplicationDbContext context, IMemoryCache cache, ILogger<JurisdictionService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<JurisdictionType>> GetActiveJurisdictionTypesAsync()
        {
            return await _context.JurisdictionTypes
                .Where(jt => jt.IsActive)
                .OrderBy(jt => jt.FieldNumber)
                .ToListAsync();
        }

        public async Task<List<Jurisdiction>> GetJurisdictionsForTypeAsync(int jurisdictionTypeId, bool includeInactive = false)
        {
            var query = _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Include(j => j.ParentJurisdiction)
                .Where(j => j.JurisdictionTypeId == jurisdictionTypeId);

            if (!includeInactive)
            {
                query = query.Where(j => j.IsActive);
            }

            return await query
                .OrderBy(j => j.DisplayOrder)
                .ThenBy(j => j.Name)
                .ToListAsync();
        }

        public async Task<Dictionary<int, List<Jurisdiction>>> GetGroupedJurisdictionsAsync()
        {
            var jurisdictions = await _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Where(j => j.IsActive && j.JurisdictionType!.IsActive)
                .OrderBy(j => j.JurisdictionType!.FieldNumber)
                .ThenBy(j => j.DisplayOrder)
                .ThenBy(j => j.Name)
                .ToListAsync();

            return jurisdictions
                .GroupBy(j => j.JurisdictionTypeId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public async Task<List<Jurisdiction>> FindJurisdictionsByPostcodeAsync(string? postcode, string? state)
        {
            if (string.IsNullOrWhiteSpace(postcode))
            {
                return new List<Jurisdiction>();
            }

            // TODO: Implement postcode mapping table lookup
            // For now, return empty list - will be implemented in Phase 2
            return new List<Jurisdiction>();
        }

        public async Task<List<Jurisdiction>> FindJurisdictionsContainingPointAsync(double latitude, double longitude)
        {
            // Try to get cached geometries
            if (!_cache.TryGetValue(GEOMETRY_CACHE_KEY, out Dictionary<int, (Jurisdiction jurisdiction, Geometry geometry)>? cachedGeometries))
            {
                // Cache miss - load and parse all geometries
                cachedGeometries = await LoadAndCacheGeometriesAsync();
            }

            if (cachedGeometries == null || cachedGeometries.Count == 0)
                return new List<Jurisdiction>();

            var matchingJurisdictions = new List<Jurisdiction>();
            var geometryFactory = new GeometryFactory();
            var point = geometryFactory.CreatePoint(new Coordinate(longitude, latitude));

            // Check each cached geometry
            foreach (var entry in cachedGeometries.Values)
            {
                try
                {
                    if (entry.geometry.Contains(point))
                    {
                        matchingJurisdictions.Add(entry.jurisdiction);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking jurisdiction {entry.jurisdiction.Name}: {ex.Message}");
                }
            }

            return matchingJurisdictions;
        }

        private async Task<Dictionary<int, (Jurisdiction jurisdiction, Geometry geometry)>> LoadAndCacheGeometriesAsync()
        {
            Console.WriteLine("Loading and parsing jurisdiction geometries...");
            var startTime = DateTime.Now;

            var jurisdictionsWithBoundaries = await _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Where(j => j.IsActive && !string.IsNullOrEmpty(j.BoundaryData))
                .AsNoTracking() // Don't track for better performance
                .ToListAsync();

            var cachedGeometries = new Dictionary<int, (Jurisdiction jurisdiction, Geometry geometry)>();

            foreach (var jurisdiction in jurisdictionsWithBoundaries)
            {
                try
                {
                    var geometry = ParseGeoJsonToGeometry(jurisdiction.BoundaryData);
                    if (geometry != null)
                    {
                        cachedGeometries[jurisdiction.Id] = (jurisdiction, geometry);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing jurisdiction {jurisdiction.Name}: {ex.Message}");
                }
            }

            // Cache for 1 hour
            _cache.Set(GEOMETRY_CACHE_KEY, cachedGeometries, TimeSpan.FromHours(1));

            var duration = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"Loaded and cached {cachedGeometries.Count} geometries in {duration}ms");

            return cachedGeometries;
        }

        private Geometry? ParseGeoJsonToGeometry(string geoJsonData)
        {
            try
            {
                var geoJsonObj = JsonDocument.Parse(geoJsonData);
                var root = geoJsonObj.RootElement;

                if (!root.TryGetProperty("coordinates", out var coordsElement))
                    return null;

                var coords = ExtractAllCoordinates(coordsElement);
                if (coords.Count < 3) return null;

                return CreateGeometryFromCoords(coords);
            }
            catch
            {
                return null;
            }
        }

        public void ClearGeometryCache()
        {
            _cache.Remove(GEOMETRY_CACHE_KEY);
            Console.WriteLine("Geometry cache cleared");
        }

        private List<Coordinate> ExtractAllCoordinates(JsonElement coordsElement)
        {
            var coords = new List<Coordinate>();
            
            void ExtractRecursive(JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Array)
                {
                    var arrayLength = elem.GetArrayLength();
                    if (arrayLength == 2 && elem[0].ValueKind == JsonValueKind.Number && elem[1].ValueKind == JsonValueKind.Number)
                    {
                        // This is a coordinate pair [lon, lat]
                        coords.Add(new Coordinate(elem[0].GetDouble(), elem[1].GetDouble()));
                    }
                    else
                    {
                        // Recurse into nested arrays
                        foreach (var child in elem.EnumerateArray())
                        {
                            ExtractRecursive(child);
                        }
                    }
                }
            }
            
            ExtractRecursive(coordsElement);
            return coords;
        }

        private Geometry? CreateGeometryFromCoords(List<Coordinate> coords)
        {
            if (coords.Count < 3) return null;
            
            var geometryFactory = new GeometryFactory();
            
            // Ensure the ring is closed
            if (!coords[0].Equals2D(coords[coords.Count - 1]))
            {
                coords.Add(new Coordinate(coords[0]));
            }
            
            try
            {
                var shell = geometryFactory.CreateLinearRing(coords.ToArray());
                return geometryFactory.CreatePolygon(shell);
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool success, string? error)> ValidateShapefileAsync(Stream fileStream)
        {
            string? tempShpPath = null;
            string? tempShxPath = null;
            string? tempDbfPath = null;

            try
            {
                using var tempStream = new MemoryStream();
                await fileStream.CopyToAsync(tempStream);
                tempStream.Position = 0;

                // Check if it's a ZIP file
                try
                {
                    using var archive = new ZipArchive(tempStream, ZipArchiveMode.Read, leaveOpen: true);
                    ValidateArchiveLimits(archive);
                    var shpEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
                    
                    if (shpEntry == null)
                    {
                        return (false, "ZIP file does not contain a .shp file");
                    }

                    // Extract shapefile components to temp files
                    tempShpPath = CreateTemporaryShapefilePath();
                    
                    using (var shpStream = shpEntry.Open())
                    using (var fileStream2 = File.Create(tempShpPath))
                    {
                        await shpStream.CopyToAsync(fileStream2);
                    }

                    // Extract .shx if present
                    var shxEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shx", StringComparison.OrdinalIgnoreCase));
                    if (shxEntry != null)
                    {
                        tempShxPath = Path.ChangeExtension(tempShpPath, ".shx");
                        using var shxStream = shxEntry.Open();
                        using var fileStream2 = File.Create(tempShxPath);
                        await shxStream.CopyToAsync(fileStream2);
                    }

                    // Extract .dbf if present
                    var dbfEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase));
                    if (dbfEntry != null)
                    {
                        tempDbfPath = Path.ChangeExtension(tempShpPath, ".dbf");
                        using var dbfStream = dbfEntry.Open();
                        using var fileStream2 = File.Create(tempDbfPath);
                        await dbfStream.CopyToAsync(fileStream2);
                    }

                    // Validate the shapefile
                    using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                    if (!reader.Read())
                    {
                        return (false, "Shapefile contains no features");
                    }

                    var geometry = reader.Geometry;
                    if (geometry == null)
                    {
                        return (false, "Shapefile features have no geometry");
                    }

                    // Check if it contains polygon/multipolygon data
                    if (geometry is not Polygon && geometry is not MultiPolygon)
                    {
                        return (false, $"Shapefile must contain Polygon or MultiPolygon geometry. Found: {geometry.GeometryType}");
                    }

                    return (true, null);
                }
                catch (InvalidDataException)
                {
                    // Not a ZIP file, try reading as raw shapefile
                    tempShpPath = CreateTemporaryShapefilePath();
                    tempStream.Position = 0;
                    
                    using (var fileStream2 = File.Create(tempShpPath))
                    {
                        await tempStream.CopyToAsync(fileStream2);
                    }

                    try
                    {
                        using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                        if (!reader.Read())
                        {
                            return (false, "Shapefile contains no features");
                        }

                        var geometry = reader.Geometry;
                        if (geometry == null)
                        {
                            return (false, "Shapefile features have no geometry");
                        }

                        if (geometry is not Polygon && geometry is not MultiPolygon)
                        {
                            return (false, $"Shapefile must contain Polygon or MultiPolygon geometry. Found: {geometry.GeometryType}");
                        }

                        return (true, null);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Raw shapefile validation failed");
                        return (false, "The file is not a valid shapefile.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shapefile validation failed");
                return (false, "The file could not be validated as a shapefile.");
            }
            finally
            {
                CleanupTemporaryShapefileFiles(tempShpPath, tempShxPath, tempDbfPath);
            }
        }

        public async Task<string?> ConvertShapefileToGeoJsonAsync(Stream fileStream)
        {
            string? tempShpPath = null;
            string? tempShxPath = null;
            string? tempDbfPath = null;

            try
            {
                using var tempStream = new MemoryStream();
                await fileStream.CopyToAsync(tempStream);
                tempStream.Position = 0;

                Geometry? geometry = null;

                // Try to extract from ZIP first
                try
                {
                    using var archive = new ZipArchive(tempStream, ZipArchiveMode.Read, leaveOpen: true);
                    ValidateArchiveLimits(archive);
                    var shpEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
                    
                    if (shpEntry != null)
                    {
                        // Extract to temp files
                        tempShpPath = CreateTemporaryShapefilePath();
                        using (var shpStream = shpEntry.Open())
                        using (var fileStream2 = File.Create(tempShpPath))
                        {
                            await shpStream.CopyToAsync(fileStream2);
                        }

                        // Extract .shx if present
                        var shxEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shx", StringComparison.OrdinalIgnoreCase));
                        if (shxEntry != null)
                        {
                            tempShxPath = Path.ChangeExtension(tempShpPath, ".shx");
                            using var shxStream = shxEntry.Open();
                            using var fileStream2 = File.Create(tempShxPath);
                            await shxStream.CopyToAsync(fileStream2);
                        }

                        // Extract .dbf if present
                        var dbfEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase));
                        if (dbfEntry != null)
                        {
                            tempDbfPath = Path.ChangeExtension(tempShpPath, ".dbf");
                            using var dbfStream = dbfEntry.Open();
                            using var fileStream2 = File.Create(tempDbfPath);
                            await dbfStream.CopyToAsync(fileStream2);
                        }

                        using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                        
                        // For a single jurisdiction, just read the first feature
                        if (reader.Read())
                        {
                            geometry = reader.Geometry;
                        }
                    }
                }
                catch (InvalidDataException)
                {
                    // Not a ZIP, try raw shapefile
                    tempShpPath = CreateTemporaryShapefilePath();
                    tempStream.Position = 0;
                    
                    using (var fileStream2 = File.Create(tempShpPath))
                    {
                        await tempStream.CopyToAsync(fileStream2);
                    }

                    using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                    
                    if (reader.Read())
                    {
                        geometry = reader.Geometry;
                    }
                }

                if (geometry == null)
                {
                    return null;
                }

                // Convert to GeoJSON manually
                var geoJsonObject = new
                {
                    type = geometry.GeometryType,
                    coordinates = GetCoordinates(geometry)
                };
                
                return JsonSerializer.Serialize(geoJsonObject);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shapefile to GeoJSON conversion failed");
                return null;
            }
            finally
            {
                CleanupTemporaryShapefileFiles(tempShpPath, tempShxPath, tempDbfPath);
            }
        }

        private object GetCoordinates(Geometry geometry)
        {
            if (geometry is Polygon polygon)
            {
                return new[] { GetPolygonCoordinates(polygon) };
            }
            else if (geometry is MultiPolygon multiPolygon)
            {
                return multiPolygon.Geometries.Cast<Polygon>().Select(p => GetPolygonCoordinates(p)).ToArray();
            }
            else if (geometry is Point point)
            {
                return new[] { point.X, point.Y };
            }
            else if (geometry is LineString lineString)
            {
                return lineString.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray();
            }

            return Array.Empty<object>();
        }

        private object[] GetPolygonCoordinates(Polygon polygon)
        {
            var rings = new List<object>();
            
            // Exterior ring
            rings.Add(polygon.ExteriorRing.Coordinates.Select(c => new[] { c.X, c.Y }).ToArray());
            
            // Interior rings (holes)
            for (int i = 0; i < polygon.NumInteriorRings; i++)
            {
                rings.Add(polygon.GetInteriorRingN(i).Coordinates.Select(c => new[] { c.X, c.Y }).ToArray());
            }

            return rings.ToArray();
        }

        public async Task<List<(string name, Dictionary<string, object> attributes, string geoJson)>> ExtractAllFeaturesFromShapefileAsync(Stream fileStream)
        {
            var results = new List<(string name, Dictionary<string, object> attributes, string geoJson)>();
            string? tempShpPath = null;
            string? tempShxPath = null;
            string? tempDbfPath = null;

            try
            {
                using var tempStream = new MemoryStream();
                await fileStream.CopyToAsync(tempStream);
                tempStream.Position = 0;

                // Extract from ZIP
                using var archive = new ZipArchive(tempStream, ZipArchiveMode.Read, leaveOpen: true);
                ValidateArchiveLimits(archive);
                var shpEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
                
                if (shpEntry != null)
                {
                    // Extract all required files
                    tempShpPath = CreateTemporaryShapefilePath();
                    using (var shpStream = shpEntry.Open())
                    using (var fileStream2 = File.Create(tempShpPath))
                    {
                        await shpStream.CopyToAsync(fileStream2);
                    }

                    var shxEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shx", StringComparison.OrdinalIgnoreCase));
                    if (shxEntry != null)
                    {
                        tempShxPath = Path.ChangeExtension(tempShpPath, ".shx");
                        using var shxStream = shxEntry.Open();
                        using var fileStream2 = File.Create(tempShxPath);
                        await shxStream.CopyToAsync(fileStream2);
                    }

                    var dbfEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase));
                    if (dbfEntry != null)
                    {
                        tempDbfPath = Path.ChangeExtension(tempShpPath, ".dbf");
                        using var dbfStream = dbfEntry.Open();
                        using var fileStream2 = File.Create(tempDbfPath);
                        await dbfStream.CopyToAsync(fileStream2);
                    }

                    // Read all features
                    using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                    
                    while (reader.Read())
                    {
                        if (results.Count >= MaximumImportedFeatures)
                        {
                            throw new InvalidDataException($"Shapefile contains more than {MaximumImportedFeatures:N0} features.");
                        }

                        var geometry = reader.Geometry;
                        if (geometry == null) continue;

                        // Get attributes
                        var attributes = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldName = reader.GetName(i);
                            var fieldValue = reader.GetValue(i);
                            attributes[fieldName] = fieldValue;
                        }

                        // Default name from first string attribute
                        var name = attributes.Values.FirstOrDefault(v => v is string)?.ToString() ?? "Unnamed";

                        // Convert geometry to GeoJSON
                        var geoJsonObject = new
                        {
                            type = geometry.GeometryType,
                            coordinates = GetCoordinates(geometry)
                        };
                        var geoJson = JsonSerializer.Serialize(geoJsonObject);

                        results.Add((name, attributes, geoJson));
                    }
                }

                return results;
            }
            catch (Exception)
            {
                return results;
            }
            finally
            {
                CleanupTemporaryShapefileFiles(tempShpPath, tempShxPath, tempDbfPath);
            }
        }

        public async Task<List<string>> GetShapefileAttributeNamesAsync(Stream fileStream)
        {
            var attributeNames = new List<string>();
            string? tempShpPath = null;
            string? tempDbfPath = null;

            try
            {
                using var tempStream = new MemoryStream();
                await fileStream.CopyToAsync(tempStream);
                tempStream.Position = 0;

                using var archive = new ZipArchive(tempStream, ZipArchiveMode.Read, leaveOpen: true);
                ValidateArchiveLimits(archive);
                var shpEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".shp", StringComparison.OrdinalIgnoreCase));
                
                if (shpEntry != null)
                {
                    tempShpPath = CreateTemporaryShapefilePath();
                    using (var shpStream = shpEntry.Open())
                    using (var fileStream2 = File.Create(tempShpPath))
                    {
                        await shpStream.CopyToAsync(fileStream2);
                    }

                    var dbfEntry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".dbf", StringComparison.OrdinalIgnoreCase));
                    if (dbfEntry != null)
                    {
                        tempDbfPath = Path.ChangeExtension(tempShpPath, ".dbf");
                        using var dbfStream = dbfEntry.Open();
                        using var fileStream2 = File.Create(tempDbfPath);
                        await dbfStream.CopyToAsync(fileStream2);
                    }

                    using var reader = new ShapefileDataReader(tempShpPath, new GeometryFactory());
                    
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        attributeNames.Add(reader.GetName(i));
                    }
                }

                return attributeNames;
            }
            catch (Exception)
            {
                return attributeNames;
            }
            finally
            {
                CleanupTemporaryShapefileFiles(tempShpPath, null, tempDbfPath);
            }
        }

        private static string CreateTemporaryShapefilePath()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"sentinel-shapefile-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, "upload.shp");
        }

        private static void CleanupTemporaryShapefileFiles(string? tempShpPath, string? tempShxPath, string? tempDbfPath)
        {
            try
            {
                var directory = tempShpPath == null ? null : Path.GetDirectoryName(tempShpPath);
                if (!string.IsNullOrWhiteSpace(directory)
                    && Path.GetFileName(directory).StartsWith("sentinel-shapefile-", StringComparison.Ordinal)
                    && Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive: true);
                    return;
                }

                foreach (var path in new[] { tempShpPath, tempShxPath, tempDbfPath })
                {
                    if (path != null && File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
            }
            catch
            {
                // Cleanup should never affect the request outcome.
            }
        }

        private static void ValidateArchiveLimits(ZipArchive archive)
        {
            if (archive.Entries.Count > MaximumArchiveEntries)
            {
                throw new InvalidDataException($"The ZIP archive contains too many files (maximum {MaximumArchiveEntries}).");
            }

            long totalUncompressed = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(entry.FullName))
                {
                    throw new InvalidDataException("The ZIP archive contains an unsafe entry path.");
                }

                totalUncompressed = checked(totalUncompressed + entry.Length);
                if (totalUncompressed > MaximumArchiveUncompressedBytes)
                {
                    throw new InvalidDataException("The ZIP archive expands beyond the permitted size.");
                }

                if (entry.Length > 0 && entry.CompressedLength > 0 &&
                    (double)entry.Length / entry.CompressedLength > MaximumArchiveCompressionRatio)
                {
                    throw new InvalidDataException("The ZIP archive compression ratio is too high.");
                }
            }
        }
    }
}
