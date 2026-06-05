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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public SelectList DiseasesSelectList { get; set; } = null!;
        public SelectList CaseStatusesSelectList { get; set; } = null!;
        public CaseDefinition Definition { get; set; } = null!;

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

            [Required]
            [Display(Name = "Classification Mode")]
            public string ClassificationMode { get; set; } = "suggest";

            [Display(Name = "Enable Auto-Evaluation")]
            public bool EnableAutoEvaluation { get; set; } = true;
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var definition = await _context.CaseDefinitions
                .Include(d => d.Disease)
                .Include(d => d.ConfirmationStatus)
                .Include(d => d.Criteria)
                    .ThenInclude(c => c.ChildCriteria)
                .Include(d => d.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory))
                    .ThenInclude(lc => lc.CanonicalSpecimenType)
                .Include(d => d.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory))
                    .ThenInclude(lc => lc.CanonicalPathogen)
                .Include(d => d.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory))
                    .ThenInclude(lc => lc.CanonicalTestMethod)
                .Include(d => d.Criteria.Where(c => c.CriterionType == CriterionType.Laboratory))
                    .ThenInclude(lc => lc.CanonicalTestResult)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (definition == null)
            {
                return NotFound();
            }

            Definition = definition;

            // Map definition to input model
            Input = new InputModel
            {
                Name = definition.Name,
                DiseaseId = definition.DiseaseId,
                ConfirmationStatusId = definition.ConfirmationStatusId,
                DateActiveFrom = definition.DateActiveFrom,
                DateActiveTo = definition.DateActiveTo,
                ClassificationMode = GetClassificationMode(definition),
                EnableAutoEvaluation = definition.EnableAutoEvaluation
            };

            await LoadSelectListsAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                await LoadSelectListsAsync();
                return Page();
            }

            var definition = await _context.CaseDefinitions.FindAsync(id);

            if (definition == null)
            {
                return NotFound();
            }

            // Validate date range
            if (Input.DateActiveTo.HasValue && Input.DateActiveTo < Input.DateActiveFrom)
            {
                ModelState.AddModelError("Input.DateActiveTo", "End date must be after start date");
                await LoadSelectListsAsync();
                return Page();
            }

            // Update basic properties
            definition.Name = Input.Name;
            definition.DiseaseId = Input.DiseaseId;
            definition.ConfirmationStatusId = Input.ConfirmationStatusId;
            definition.DateActiveFrom = Input.DateActiveFrom;
            definition.DateActiveTo = Input.DateActiveTo;
            definition.EnableAutoEvaluation = Input.EnableAutoEvaluation;

            // Map classification mode to boolean flags
            switch (Input.ClassificationMode)
            {
                case "manual":
                    definition.AllowAutoClassification = false;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = false;
                    break;
                case "suggest":
                    definition.AllowAutoClassification = false;
                    definition.CreateReviewQueueOnSuggestion = true;
                    definition.CreateReviewQueueOnChange = false;
                    break;
                case "auto-review":
                    definition.AllowAutoClassification = true;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = true;
                    break;
                case "auto-silent":
                    definition.AllowAutoClassification = true;
                    definition.CreateReviewQueueOnSuggestion = false;
                    definition.CreateReviewQueueOnChange = false;
                    break;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Case definition updated successfully";
                return RedirectToPage("./Index");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CaseDefinitionExistsAsync(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task LoadSelectListsAsync()
        {
            var diseases = await _context.Diseases
                .OrderBy(d => d.Name)
                .ToListAsync();

            var caseStatuses = await _context.CaseStatuses
                .OrderBy(s => s.Name)
                .ToListAsync();

            DiseasesSelectList = new SelectList(diseases, "Id", "Name");
            CaseStatusesSelectList = new SelectList(caseStatuses, "Id", "Name");
        }

        private string GetClassificationMode(CaseDefinition definition)
        {
            if (!definition.AllowAutoClassification && !definition.CreateReviewQueueOnSuggestion)
                return "manual";
            else if (!definition.AllowAutoClassification && definition.CreateReviewQueueOnSuggestion)
                return "suggest";
            else if (definition.AllowAutoClassification && definition.CreateReviewQueueOnChange)
                return "auto-review";
            else
                return "auto-silent";
        }

        private async Task<bool> CaseDefinitionExistsAsync(int id)
        {
            return await _context.CaseDefinitions.AnyAsync(e => e.Id == id);
        }
    }
}
