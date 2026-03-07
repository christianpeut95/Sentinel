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
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IJurisdictionService _jurisdictionService;

        public CreateModel(ApplicationDbContext context, IJurisdictionService jurisdictionService)
        {
            _context = context;
            _jurisdictionService = jurisdictionService;
        }

        [BindProperty]
        public JurisdictionInputModel Input { get; set; } = default!;

        [BindProperty]
        public IFormFile? ShapefileUpload { get; set; }

        public SelectList JurisdictionTypes { get; set; } = default!;
        public SelectList ParentJurisdictions { get; set; } = default!;

        public class JurisdictionInputModel
        {
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
            public bool IsActive { get; set; } = true;

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

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                await LoadDropdowns();

                if (JurisdictionTypes == null || !JurisdictionTypes.Any())
                {
                    TempData["ErrorMessage"] = "Please configure at least one jurisdiction type before creating jurisdictions.";
                    return RedirectToPage("/Settings/JurisdictionTypes/Index");
                }

                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading page: {ex.Message}";
                return RedirectToPage("/Settings/Index");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== OnPostAsync START ===");
                System.Diagnostics.Debug.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
                System.Diagnostics.Debug.WriteLine($"Input is null: {Input == null}");
                
                if (Input != null)
                {
                    System.Diagnostics.Debug.WriteLine($"JurisdictionTypeId: {Input.JurisdictionTypeId}");
                    System.Diagnostics.Debug.WriteLine($"Name: {Input.Name}");
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    System.Diagnostics.Debug.WriteLine($"Validation errors: {string.Join(", ", errors)}");
                    
                    await LoadDropdowns();
                    TempData["ErrorMessage"] = $"Validation failed: {string.Join(", ", errors)}";
                    return Page();
                }

                System.Diagnostics.Debug.WriteLine("Validation passed, creating jurisdiction...");

                var jurisdiction = new Jurisdiction
                {
                    JurisdictionTypeId = Input.JurisdictionTypeId,
                    Name = Input.Name,
                    Code = Input.Code,
                    Description = Input.Description,
                    ParentJurisdictionId = Input.ParentJurisdictionId,
                    BoundaryData = Input.BoundaryData,
                    IsActive = Input.IsActive,
                    DisplayOrder = Input.DisplayOrder,
                    Population = Input.Population,
                    PopulationYear = Input.PopulationYear,
                    PopulationSource = Input.PopulationSource
                };

                _context.Jurisdictions.Add(jurisdiction);
                await _context.SaveChangesAsync();

                System.Diagnostics.Debug.WriteLine($"Jurisdiction created: {jurisdiction.Name}");
                TempData["SuccessMessage"] = $"Jurisdiction '{jurisdiction.Name}' created successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== EXCEPTION in OnPostAsync ===");
                System.Diagnostics.Debug.WriteLine($"Message: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                await LoadDropdowns();
                return Page();
            }
        }

        private async Task LoadDropdowns()
        {
            var types = await _jurisdictionService.GetActiveJurisdictionTypesAsync();
            JurisdictionTypes = new SelectList(types, "Id", "Name");

            var allJurisdictions = await _context.Jurisdictions
                .Include(j => j.JurisdictionType)
                .Where(j => j.IsActive)
                .OrderBy(j => j.JurisdictionType!.FieldNumber)
                .ThenBy(j => j.DisplayOrder)
                .ThenBy(j => j.Name)
                .ToListAsync();

            ParentJurisdictions = new SelectList(
                allJurisdictions.Select(j => new
                {
                    j.Id,
                    DisplayName = $"{j.JurisdictionType!.Name}: {j.Name}"
                }),
                "Id",
                "DisplayName"
            );
        }
    }
}
