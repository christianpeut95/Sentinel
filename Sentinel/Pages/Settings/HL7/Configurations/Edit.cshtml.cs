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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HL7Configuration Configuration { get; set; } = null!;

        public SelectList LaboratorySelectList { get; set; } = null!;

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var config = await _context.HL7Configurations.FindAsync(id);
            if (config == null)
            {
                return NotFound();
            }

            Configuration = config;
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

            var existingConfig = await _context.HL7Configurations
                .FirstOrDefaultAsync(c => c.Id == Configuration.Id);

            if (existingConfig == null)
            {
                return NotFound();
            }

            // Update properties
            existingConfig.ConfigurationName = Configuration.ConfigurationName;
            existingConfig.SendingFacility = Configuration.SendingFacility;
            existingConfig.SendingApplication = Configuration.SendingApplication;
            existingConfig.FileDropPath = Configuration.FileDropPath;
            existingConfig.FilePattern = Configuration.FilePattern;
            existingConfig.CharacterEncoding = Configuration.CharacterEncoding;
            existingConfig.ProcessOnReceipt = Configuration.ProcessOnReceipt;
            existingConfig.ArchiveProcessedFiles = Configuration.ArchiveProcessedFiles;
            existingConfig.ArchivePath = Configuration.ArchivePath;
            existingConfig.PatientMatchingStrategy = Configuration.PatientMatchingStrategy;
            existingConfig.AutoCreatePatients = Configuration.AutoCreatePatients;
            existingConfig.DuplicateDetectionStrategy = Configuration.DuplicateDetectionStrategy;
            existingConfig.DuplicateDetectionWindowHours = Configuration.DuplicateDetectionWindowHours;
            existingConfig.DefaultLaboratoryId = Configuration.DefaultLaboratoryId;
            existingConfig.AutoCreateOrganizations = Configuration.AutoCreateOrganizations;
            existingConfig.AutoCreateCases = Configuration.AutoCreateCases;
            existingConfig.Priority = Configuration.Priority;
            existingConfig.IsActive = Configuration.IsActive;
            existingConfig.ModifiedAt = DateTime.UtcNow;

            // Create directories if they don't exist
            try
            {
                if (!Directory.Exists(Configuration.FileDropPath))
                {
                    Directory.CreateDirectory(Configuration.FileDropPath);
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
                ModelState.AddModelError("Configuration.FileDropPath", $"Could not create directory: {ex.Message}");
                await LoadSelectListsAsync();
                return Page();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Configuration '{Configuration.ConfigurationName}' has been updated successfully.";
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
