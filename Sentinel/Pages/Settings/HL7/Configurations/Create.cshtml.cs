using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Pages.Settings.HL7.Configurations
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HL7Configuration Configuration { get; set; } = new HL7Configuration
        {
            IsActive = true,
            Priority = 10,
            CharacterEncoding = "UTF8",
            PatientMatchingStrategy = PatientMatchingStrategy.StrictMatch,
            AutoCreatePatients = true,
            AutoCreateCases = true,
            DuplicateDetectionWindowHours = 72,
            DuplicateDetectionStrategy = DuplicateDetectionStrategy.Combined,
            FilePattern = "*.hl7",
            ProcessOnReceipt = true,
            ArchiveProcessedFiles = true,
            AutoCreateOrganizations = false
        };

        public SelectList LaboratorySelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            // Validate FileDropPath
            if (string.IsNullOrWhiteSpace(Configuration.FileDropPath))
            {
                ModelState.AddModelError("Configuration.FileDropPath", "File drop path is required.");
                await LoadSelectListsAsync();
                return Page();
            }

            // Check for duplicate SendingFacility
            var existingConfig = await _context.HL7Configurations
                .AnyAsync(c => c.SendingFacility == Configuration.SendingFacility 
                            && c.SendingApplication == Configuration.SendingApplication);

            if (existingConfig)
            {
                ModelState.AddModelError("Configuration.SendingFacility", 
                    "A configuration with this sending facility and application already exists.");
                await LoadSelectListsAsync();
                return Page();
            }

            // Create directory if it doesn't exist
            try
            {
                if (!Directory.Exists(Configuration.FileDropPath))
                {
                    Directory.CreateDirectory(Configuration.FileDropPath);

                    // Create subdirectories
                    Directory.CreateDirectory(Path.Combine(Configuration.FileDropPath, "Processed"));
                    Directory.CreateDirectory(Path.Combine(Configuration.FileDropPath, "Error"));
                    Directory.CreateDirectory(Path.Combine(Configuration.FileDropPath, "Review"));
                }

                if (Configuration.ArchiveProcessedFiles && !string.IsNullOrWhiteSpace(Configuration.ArchivePath))
                {
                    if (!Directory.Exists(Configuration.ArchivePath))
                    {
                        Directory.CreateDirectory(Configuration.ArchivePath);
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Configuration.FileDropPath", 
                    $"Could not create directory: {ex.Message}");
                await LoadSelectListsAsync();
                return Page();
            }

            Configuration.Id = Guid.NewGuid();
            Configuration.CreatedAt = DateTime.UtcNow;
            Configuration.ModifiedAt = DateTime.UtcNow;

            _context.HL7Configurations.Add(Configuration);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Configuration '{Configuration.ConfigurationName}' has been created successfully. File monitoring will start automatically.";
            return RedirectToPage("./Index");
        }

        private async Task LoadSelectListsAsync()
        {
            var laboratories = await _context.Organizations
                .Where(o => o.OrganizationType.Name == "Laboratory" && o.IsActive)
                .OrderBy(o => o.Name)
                .Select(o => new { o.Id, o.Name })
                .ToListAsync();

            LaboratorySelectList = new SelectList(laboratories, "Id", "Name");
        }
    }
}
