using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using Sentinel.Services;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.Jurisdictions
{
    // TEMPORARY: Authorization disabled for testing - re-enable in production
    // [Authorize(Policy = "Permission.Settings.Edit")]
    [Authorize]
    [RequestSizeLimit(100_000_000)] // Allow up to 100MB
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)] // Allow up to 100MB multipart
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IJurisdictionService _jurisdictionService;

        public EditModel(ApplicationDbContext context, IJurisdictionService jurisdictionService)
        {
            _context = context;
            _jurisdictionService = jurisdictionService;
        }

        [BindProperty]
        public JurisdictionInputModel Input { get; set; } = default!;

        [BindProperty]
        public IFormFile? ShapefileUpload { get; set; }
        
        [BindProperty]
        public bool RemoveBoundaryData { get; set; }

        public SelectList JurisdictionTypes { get; set; } = default!;
        public SelectList ParentJurisdictions { get; set; } = default!;
        public bool HasBoundaryData { get; set; }

        public class JurisdictionInputModel
        {
            public int Id { get; set; }

            [Required]
            [Display(Name = "Jurisdiction Type")]
            public int JurisdictionTypeId { get; set; }

            [Required]
            [StringLength(200)]
            [Display(Name = "Name")]
            public string Name { get; set; } = string.Empty;

            [StringLength(50)]
            [Display(Name = "Code")]
            public string? Code { get; set; }

            [StringLength(1000)]
            [Display(Name = "Description")]
            public string? Description { get; set; }

            [Display(Name = "Parent Jurisdiction")]
            public int? ParentJurisdictionId { get; set; }

            [Display(Name = "Boundary Data (GeoJSON)")]
            public string? BoundaryData { get; set; }

            [Display(Name = "Active")]
            public bool IsActive { get; set; }

            [Display(Name = "Display Order")]
            public int DisplayOrder { get; set; }

            [Display(Name = "Population")]
            public long? Population { get; set; }

            [Display(Name = "Population Year")]
            public int? PopulationYear { get; set; }

            [StringLength(200)]
            [Display(Name = "Population Source")]
            public string? PopulationSource { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Load jurisdiction WITHOUT the potentially large BoundaryData field
            // Use projection to only get needed fields
            var jurisdiction = await _context.Jurisdictions
                .Where(j => j.Id == id)
                .Select(j => new
                {
                    j.Id,
                    j.JurisdictionTypeId,
                    j.Name,
                    j.Code,
                    j.Description,
                    j.ParentJurisdictionId,
                    j.IsActive,
                    j.DisplayOrder,
                    j.Population,
                    j.PopulationYear,
                    j.PopulationSource,
                    HasBoundaryData = !string.IsNullOrEmpty(j.BoundaryData)
                })
                .FirstOrDefaultAsync();

            if (jurisdiction == null)
            {
                return NotFound();
            }

            HasBoundaryData = jurisdiction.HasBoundaryData;

            Input = new JurisdictionInputModel
            {
                Id = jurisdiction.Id,
                JurisdictionTypeId = jurisdiction.JurisdictionTypeId,
                Name = jurisdiction.Name,
                Code = jurisdiction.Code,
                Description = jurisdiction.Description,
                ParentJurisdictionId = jurisdiction.ParentJurisdictionId,
                BoundaryData = null, // Don't load the large JSON by default
                IsActive = jurisdiction.IsActive,
                DisplayOrder = jurisdiction.DisplayOrder,
                Population = jurisdiction.Population,
                PopulationYear = jurisdiction.PopulationYear,
                PopulationSource = jurisdiction.PopulationSource
            };

            await LoadDropdowns(id.Value);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(Input.Id);
                return Page();
            }

            var jurisdiction = await _context.Jurisdictions.FindAsync(Input.Id);

            if (jurisdiction == null)
            {
                return NotFound();
            }

            // Handle boundary data removal
            if (RemoveBoundaryData)
            {
                Input.BoundaryData = null;
            }
            // Handle shapefile upload if provided
            else if (ShapefileUpload != null && ShapefileUpload.Length > 0)
            {
                using var stream = ShapefileUpload.OpenReadStream();
                
                // Validate shapefile
                var (isValid, validationError) = await _jurisdictionService.ValidateShapefileAsync(stream);
                if (!isValid)
                {
                    ModelState.AddModelError("ShapefileUpload", validationError ?? "Invalid shapefile");
                    await LoadDropdowns(Input.Id);
                    return Page();
                }

                // Convert to GeoJSON
                stream.Position = 0;
                var geoJson = await _jurisdictionService.ConvertShapefileToGeoJsonAsync(stream);
                
                if (geoJson == null)
                {
                    ModelState.AddModelError("ShapefileUpload", "Failed to convert shapefile to GeoJSON");
                    await LoadDropdowns(Input.Id);
                    return Page();
                }

                // Override any manual GeoJSON input with shapefile data
                Input.BoundaryData = geoJson;
                TempData["SuccessMessage"] = "Shapefile converted successfully to GeoJSON.";
            }
            // Validate boundary data if manually provided
            else if (!string.IsNullOrWhiteSpace(Input.BoundaryData))
            {
                if (!Input.BoundaryData.TrimStart().StartsWith("{") || !Input.BoundaryData.Contains("\"type\""))
                {
                    ModelState.AddModelError("Input.BoundaryData", "Invalid GeoJSON format. Must be valid GeoJSON.");
                    await LoadDropdowns(Input.Id);
                    return Page();
                }
            }
            // If no new boundary data provided and not removing, keep existing
            else
            {
                // Don't modify boundary data - keep what's in database
                Input.BoundaryData = jurisdiction.BoundaryData;
            }

            // Prevent circular parent reference
            if (Input.ParentJurisdictionId.HasValue && Input.ParentJurisdictionId.Value == Input.Id)
            {
                ModelState.AddModelError("Input.ParentJurisdictionId", "A jurisdiction cannot be its own parent.");
                await LoadDropdowns(Input.Id);
                return Page();
            }

            jurisdiction.JurisdictionTypeId = Input.JurisdictionTypeId;
            jurisdiction.Name = Input.Name;
            jurisdiction.Code = Input.Code;
            jurisdiction.Description = Input.Description;
            jurisdiction.ParentJurisdictionId = Input.ParentJurisdictionId;
            jurisdiction.BoundaryData = Input.BoundaryData;
            jurisdiction.IsActive = Input.IsActive;
            jurisdiction.DisplayOrder = Input.DisplayOrder;
            jurisdiction.Population = Input.Population;
            jurisdiction.PopulationYear = Input.PopulationYear;
            jurisdiction.PopulationSource = Input.PopulationSource;

            _context.Jurisdictions.Update(jurisdiction);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Jurisdiction '{jurisdiction.Name}' updated successfully.";
            return RedirectToPage("./Index");
        }

        private async Task LoadDropdowns(int currentJurisdictionId)
        {
            var types = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            JurisdictionTypes = new SelectList(types, "Id", "Name");

            // Exclude current jurisdiction from parent selection to prevent circular reference
            // Use projection to avoid loading BoundaryData and other heavy fields
            var allJurisdictions = await _context.Jurisdictions
                .Where(j => j.IsActive && j.Id != currentJurisdictionId)
                .OrderBy(j => j.JurisdictionType!.FieldNumber)
                .ThenBy(j => j.DisplayOrder)
                .ThenBy(j => j.Name)
                .Select(j => new
                {
                    j.Id,
                    j.Name,
                    JurisdictionTypeName = j.JurisdictionType!.Name,
                    JurisdictionTypeFieldNumber = j.JurisdictionType!.FieldNumber
                })
                .ToListAsync();

            ParentJurisdictions = new SelectList(
                allJurisdictions.Select(j => new
                {
                    j.Id,
                    DisplayName = $"{j.JurisdictionTypeName}: {j.Name}"
                }),
                "Id",
                "DisplayName"
            );
        }
    }
}
