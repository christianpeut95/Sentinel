using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Services;

namespace Sentinel.Pages.Settings.Jurisdictions
{
    [Authorize]
    [RequestSizeLimit(100_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    public class BulkImportModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IJurisdictionService _jurisdictionService;
        private readonly IMemoryCache _cache;

        public BulkImportModel(ApplicationDbContext context, IJurisdictionService jurisdictionService, IMemoryCache cache)
        {
            _context = context;
            _jurisdictionService = jurisdictionService;
            _cache = cache;
        }

        [BindProperty]
        public int JurisdictionTypeId { get; set; }

        [BindProperty]
        public IFormFile? ShapefileUpload { get; set; }

        [BindProperty]
        public string? NameFieldMapping { get; set; }

        [BindProperty]
        public string? CodeFieldMapping { get; set; }
        
        [BindProperty]
        public string? UploadId { get; set; }

        public SelectList JurisdictionTypes { get; set; } = default!;
        public List<string> AttributeNames { get; set; } = new();
        public List<PreviewJurisdiction> PreviewJurisdictions { get; set; } = new();

        public class PreviewJurisdiction
        {
            public string Name { get; set; } = string.Empty;
            public string? Code { get; set; }
            public Dictionary<string, object> Attributes { get; set; } = new();
            public string GeoJson { get; set; } = string.Empty;
        }

        // Serializable version for TempData
        public class StoredFeature
        {
            public string Name { get; set; } = string.Empty;
            public Dictionary<string, string> Attributes { get; set; } = new();
            public string GeoJson { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadJurisdictionTypes();
            
            // Restore state from cache if available using stable upload ID
            var uploadId = TempData.Peek("UploadId")?.ToString();
            if (!string.IsNullOrEmpty(uploadId))
            {
                var cacheKey = GetCacheKey(uploadId);
                if (_cache.TryGetValue(cacheKey, out List<StoredFeature>? cachedFeatures) && cachedFeatures != null)
                {
                    var attributeNames = cachedFeatures.First().Attributes.Keys.ToList();
                    AttributeNames = attributeNames;
                    UploadId = uploadId;
                    TempData["FeatureCount"] = cachedFeatures.Count;
                    TempData["JurisdictionTypeId"] = TempData.Peek("JurisdictionTypeId");
                }
            }
            
            return Page();
        }

        private string GetCacheKey(string? uploadId = null)
        {
            // Use stable GUID-based key instead of unstable Session.Id
            // This fixes Azure session expiration issues
            var keyId = uploadId ?? UploadId ?? TempData.Peek("UploadId")?.ToString() ?? Guid.NewGuid().ToString();
            return $"BulkImport_{keyId}";
        }

        public async Task<IActionResult> OnPostAnalyzeAsync()
        {
            if (ShapefileUpload == null || ShapefileUpload.Length == 0)
            {
                ModelState.AddModelError("ShapefileUpload", "Please select a shapefile to upload");
                await LoadJurisdictionTypes();
                return Page();
            }

            if (JurisdictionTypeId == 0)
            {
                ModelState.AddModelError("JurisdictionTypeId", "Please select a jurisdiction type");
                await LoadJurisdictionTypes();
                return Page();
            }

            try
            {
                using var stream = ShapefileUpload.OpenReadStream();
                
                // Get attribute names
                AttributeNames = await _jurisdictionService.GetShapefileAttributeNamesAsync(stream);
                
                if (!AttributeNames.Any())
                {
                    ModelState.AddModelError("ShapefileUpload", "Could not read shapefile attributes");
                    await LoadJurisdictionTypes();
                    return Page();
                }

                // Extract all features
                stream.Position = 0;
                var features = await _jurisdictionService.ExtractAllFeaturesFromShapefileAsync(stream);
                
                // Store features in memory cache (not TempData - too large!)
                var storedFeatures = features.Select(f => new StoredFeature
                {
                    Name = f.name,
                    Attributes = f.attributes.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.ToString() ?? ""
                    ),
                    GeoJson = f.geoJson
                }).ToList();

                // Generate stable upload ID for Azure compatibility
                var uploadId = Guid.NewGuid().ToString();
                var cacheKey = GetCacheKey(uploadId);
                _cache.Set(cacheKey, storedFeatures, TimeSpan.FromMinutes(30));

                // Store upload ID for retrieval on import
                TempData["UploadId"] = uploadId;
                TempData["FeatureCount"] = features.Count;
                TempData["JurisdictionTypeId"] = JurisdictionTypeId;

                // Preview first 10
                PreviewJurisdictions = features.Take(10).Select(f => new PreviewJurisdiction
                {
                    Name = f.name,
                    Attributes = f.attributes,
                    GeoJson = f.geoJson.Substring(0, Math.Min(100, f.geoJson.Length)) + "..."
                }).ToList();
                
                AttributeNames = storedFeatures.First().Attributes.Keys.ToList();
                
                await LoadJurisdictionTypes();
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error analyzing shapefile: {ex.Message}");
                await LoadJurisdictionTypes();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostImportAsync()
        {
            // Retrieve upload ID from TempData or form
            var uploadId = UploadId ?? TempData.Peek("UploadId")?.ToString();
            
            if (string.IsNullOrEmpty(uploadId))
            {
                TempData["ErrorMessage"] = "Upload session expired. Please re-upload and analyze the shapefile.";
                return RedirectToPage();
            }
            
            // Retrieve stored features from memory cache using stable key
            var cacheKey = GetCacheKey(uploadId);
            if (!_cache.TryGetValue(cacheKey, out List<StoredFeature>? storedFeatures) || storedFeatures == null || !storedFeatures.Any())
            {
                TempData["ErrorMessage"] = "Shapefile data expired. Please re-upload and analyze the shapefile.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(NameFieldMapping))
            {
                TempData["ErrorMessage"] = "Please select a field for jurisdiction name";
                TempData.Keep("FeatureCount");
                TempData.Keep("JurisdictionTypeId");
                return RedirectToPage();
            }

            try
            {
                // Get jurisdiction type ID from TempData (it was stored during Analyze)
                var jurisdictionTypeId = TempData["JurisdictionTypeId"] != null 
                    ? Convert.ToInt32(TempData["JurisdictionTypeId"]) 
                    : JurisdictionTypeId;

                var jurisdictionsToCreate = new List<Jurisdiction>();
                int displayOrder = 0;

                foreach (var feature in storedFeatures)
                {
                    // Map name from selected attribute
                    var name = feature.Attributes.ContainsKey(NameFieldMapping) 
                        ? feature.Attributes[NameFieldMapping] 
                        : feature.Name;

                    // Map code if mapping specified
                    string? code = null;
                    if (!string.IsNullOrWhiteSpace(CodeFieldMapping) && feature.Attributes.ContainsKey(CodeFieldMapping))
                    {
                        code = feature.Attributes[CodeFieldMapping];
                    }

                    var jurisdiction = new Jurisdiction
                    {
                        JurisdictionTypeId = jurisdictionTypeId,
                        Name = name ?? "Unnamed",
                        Code = code,
                        BoundaryData = feature.GeoJson,
                        IsActive = true,
                        DisplayOrder = displayOrder++
                    };

                    jurisdictionsToCreate.Add(jurisdiction);
                }

                // Bulk insert
                await _context.Jurisdictions.AddRangeAsync(jurisdictionsToCreate);
                await _context.SaveChangesAsync();

                // Clear cache after successful import
                _cache.Remove(cacheKey);

                TempData["SuccessMessage"] = $"Successfully imported {jurisdictionsToCreate.Count} jurisdictions!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error importing jurisdictions: {ex.Message}";
                return RedirectToPage();
            }
        }

        private async Task LoadJurisdictionTypes()
        {
            var types = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            JurisdictionTypes = new SelectList(types, "Id", "Name");
        }
    }
}
