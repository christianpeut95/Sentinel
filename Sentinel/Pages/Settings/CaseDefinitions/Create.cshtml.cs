using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.CaseDefinitions;
using System.ComponentModel.DataAnnotations;

namespace Sentinel.Pages.Settings.CaseDefinitions
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public SelectList DiseasesSelectList { get; set; } = null!;
        public SelectList CaseStatusesSelectList { get; set; } = null!;

        public class InputModel
        {
            [Required]
            [StringLength(200)]
            [Display(Name = "Definition Name")]
            public string Name { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Disease")]
            public Guid DiseaseId { get; set; }

            [Required]
            [Display(Name = "Classification Status")]
            public int ConfirmationStatusId { get; set; }

            [Required]
            [Display(Name = "Active From")]
            [DataType(DataType.Date)]
            public DateTime DateActiveFrom { get; set; } = DateTime.Today;

            [Display(Name = "Active Until")]
            [DataType(DataType.Date)]
            public DateTime? DateActiveTo { get; set; }

            [Display(Name = "Apply to Child Diseases")]
            public bool ApplyToChildDiseases { get; set; }

            [Required]
            [Display(Name = "Classification Mode")]
            public string ClassificationMode { get; set; } = "Suggest";
        }

        public async Task OnGetAsync(int? duplicateFrom)
        {
            await LoadSelectListsAsync();

            // If duplicating, load source definition
            if (duplicateFrom.HasValue)
            {
                var source = await _context.CaseDefinitions
                    .Include(cd => cd.Disease)
                    .Include(cd => cd.ConfirmationStatus)
                    .FirstOrDefaultAsync(cd => cd.Id == duplicateFrom.Value);

                if (source != null)
                {
                    Input.Name = $"{source.Name} (Copy)";
                    Input.DiseaseId = source.DiseaseId;
                    Input.ConfirmationStatusId = source.ConfirmationStatusId;
                    Input.DateActiveFrom = DateTime.Today;
                    Input.ApplyToChildDiseases = source.ApplyToChildDiseases;

                    // Set classification mode based on flags
                    if (!source.AllowAutoClassification)
                    {
                        Input.ClassificationMode = "Manual";
                    }
                    else if (source.CreateReviewQueueOnSuggestion)
                    {
                        Input.ClassificationMode = "Suggest";
                    }
                    else if (source.CreateReviewQueueOnChange)
                    {
                        Input.ClassificationMode = "AutoWithReview";
                    }
                    else
                    {
                        Input.ClassificationMode = "AutoSilent";
                    }

                    // Store source ID in TempData for criteria duplication in next step
                    TempData["DuplicateFromId"] = duplicateFrom.Value;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadSelectListsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate date range
            if (Input.DateActiveTo.HasValue && Input.DateActiveTo.Value <= Input.DateActiveFrom)
            {
                ModelState.AddModelError("Input.DateActiveTo", "End date must be after start date");
                return Page();
            }

            // Create the case definition
            var definition = new CaseDefinition
            {
                Name = Input.Name,
                DiseaseId = Input.DiseaseId,
                ConfirmationStatusId = Input.ConfirmationStatusId,
                Status = CaseDefinitionStatus.Draft,
                DateActiveFrom = Input.DateActiveFrom,
                DateActiveTo = Input.DateActiveTo,
                ApplyToChildDiseases = Input.ApplyToChildDiseases,
                CreatedBy = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow
            };

            // Set classification behavior flags based on mode
            switch (Input.ClassificationMode)
            {
                case "Manual":
                    definition.AllowAutoClassification = false;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = false;
                    break;
                case "Suggest":
                    definition.AllowAutoClassification = true;
                    definition.CreateReviewQueueOnSuggestion = true;
                    definition.CreateReviewQueueOnChange = false;
                    break;
                case "AutoWithReview":
                    definition.AllowAutoClassification = true;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = true;
                    break;
                case "AutoSilent":
                    definition.AllowAutoClassification = true;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = false;
                    break;
            }

            _context.CaseDefinitions.Add(definition);
            await _context.SaveChangesAsync();

            // Store definition ID and classification mode in TempData for next step
            TempData["DefinitionId"] = definition.Id;
            TempData["ClassificationMode"] = Input.ClassificationMode;

            // Redirect to criteria builder
            return RedirectToPage("./BuildCriteria", new { id = definition.Id });
        }

        private async Task LoadSelectListsAsync()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .Select(d => new { d.Id, d.Name })
                .ToListAsync();

            DiseasesSelectList = new SelectList(diseases, "Id", "Name");

            var statuses = await _context.CaseStatuses
                .OrderBy(cs => cs.Name)
                .Select(cs => new { cs.Id, cs.Name })
                .ToListAsync();

            CaseStatusesSelectList = new SelectList(statuses, "Id", "Name");
        }
    }
}
