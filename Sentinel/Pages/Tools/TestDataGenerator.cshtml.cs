using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Services;
using System.Threading.Tasks;

namespace Sentinel.Pages.Tools
{
    public class TestDataGeneratorModel : PageModel
    {
        private readonly TestDataGeneratorService _testDataGenerator;
        private readonly ApplicationDbContext _context;

        public TestDataGeneratorModel(TestDataGeneratorService testDataGenerator, ApplicationDbContext context)
        {
            _testDataGenerator = testDataGenerator;
            _context = context;
        }

        [BindProperty]
        public int PatientCount { get; set; } = 50;

        [BindProperty]
        public bool UseGeocoding { get; set; } = true;

        // Case Generation Properties
        [BindProperty]
        public int StartYear { get; set; } = DateTime.UtcNow.Year - 2;

        [BindProperty]
        public int EndYear { get; set; } = DateTime.UtcNow.Year;

        [BindProperty]
        public int CasesPerYear { get; set; } = 100;

        [BindProperty]
        public List<string> SelectedDiseaseIds { get; set; } = new();

        [BindProperty]
        public bool IncludeCustomFields { get; set; } = true;

        [BindProperty]
        public bool IncludeLabResults { get; set; } = false;

        [BindProperty]
        public bool UseSeasonalPatterns { get; set; } = true;

        public TestDataGenerationResult? Result { get; set; }
        public TestDataGenerationResult? CaseResult { get; set; }

        // System Statistics
        public int CurrentPatientCount { get; set; }
        public int CurrentCaseCount { get; set; }
        public int ActiveDiseaseCount { get; set; }
        public List<SelectListItem> AvailableDiseases { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadSystemStatisticsAsync();
        }

        private async Task LoadSystemStatisticsAsync()
        {
            CurrentPatientCount = await _context.Patients.CountAsync();
            CurrentCaseCount = await _context.Cases.CountAsync();
            ActiveDiseaseCount = await _context.Diseases.Where(d => d.IsActive).CountAsync();

            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToListAsync();

            AvailableDiseases = diseases.Select(d => new SelectListItem
            {
                Value = d.Id.ToString(),
                Text = d.Name
            }).ToList();
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            if (PatientCount < 1 || PatientCount > 500)
            {
                ModelState.AddModelError(nameof(PatientCount), "Patient count must be between 1 and 500");
                await LoadSystemStatisticsAsync();
                return Page();
            }

            Result = await _testDataGenerator.GeneratePatientsAsync(
                PatientCount,
                UseGeocoding,
                null);

            Result.EndTime = System.DateTime.UtcNow;

            TempData["SuccessMessage"] = $"Successfully generated {Result.PatientsCreated} patients!";

            await LoadSystemStatisticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostQuickAsync(int count)
        {
            PatientCount = count;
            UseGeocoding = count <= 50;

            return await OnPostGenerateAsync();
        }

        public async Task<IActionResult> OnPostGenerateCasesAsync()
        {
            if (StartYear < 2015 || StartYear > 2030 || EndYear < 2015 || EndYear > 2030)
            {
                ModelState.AddModelError("", "Years must be between 2015 and 2030");
                await LoadSystemStatisticsAsync();
                return Page();
            }

            if (EndYear < StartYear)
            {
                ModelState.AddModelError(nameof(EndYear), "End year must be after start year");
                await LoadSystemStatisticsAsync();
                return Page();
            }

            if (CasesPerYear < 10 || CasesPerYear > 500)
            {
                ModelState.AddModelError(nameof(CasesPerYear), "Cases per year must be between 10 and 500");
                await LoadSystemStatisticsAsync();
                return Page();
            }

            List<Guid>? diseaseIds = null;
            if (SelectedDiseaseIds != null && SelectedDiseaseIds.Any())
            {
                diseaseIds = SelectedDiseaseIds
                    .Where(id => Guid.TryParse(id, out _))
                    .Select(id => Guid.Parse(id))
                    .ToList();
            }

            CaseResult = await _testDataGenerator.GenerateCasesAsync(
                StartYear,
                EndYear,
                CasesPerYear,
                diseaseIds,
                IncludeCustomFields,
                IncludeLabResults,
                UseSeasonalPatterns,
                null);

            CaseResult.EndTime = DateTime.UtcNow;

            TempData["CaseSuccessMessage"] = $"Successfully generated {CaseResult.CasesCreated} cases across {(EndYear - StartYear + 1)} years!";

            await LoadSystemStatisticsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostQuickCasesAsync(string preset)
        {
            var currentYear = DateTime.UtcNow.Year;

            switch (preset)
            {
                case "test":
                    StartYear = currentYear - 1;
                    EndYear = currentYear;
                    CasesPerYear = 50;
                    IncludeCustomFields = true;
                    IncludeLabResults = false;
                    UseSeasonalPatterns = true;
                    break;

                case "comparison":
                    StartYear = currentYear - 4;
                    EndYear = currentYear;
                    CasesPerYear = 100;
                    IncludeCustomFields = true;
                    IncludeLabResults = true;
                    UseSeasonalPatterns = true;
                    break;

                case "full":
                    StartYear = currentYear - 6;
                    EndYear = currentYear;
                    CasesPerYear = 150;
                    IncludeCustomFields = true;
                    IncludeLabResults = true;
                    UseSeasonalPatterns = true;
                    break;
            }

            return await OnPostGenerateCasesAsync();
        }
    }
}
