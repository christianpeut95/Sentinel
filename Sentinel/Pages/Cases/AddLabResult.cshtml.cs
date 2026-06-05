using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Text.Json;

namespace Sentinel.Pages.Cases
{
    [Authorize]
    public class AddLabResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AddLabResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LabResult LabResult { get; set; } = new LabResult();

        [BindProperty]
        public IFormFile? LabResultAttachment { get; set; }

        // JSON string to receive markers from client-side form
        [BindProperty]
        public string? MarkersJson { get; set; }

        public SelectList SpecimenTypesList { get; set; }
        public SelectList ResultUnitsList { get; set; }
        public SelectList LaboratoriesList { get; set; }
        public SelectList OrderingProvidersList { get; set; }
        public string CaseId { get; set; }
        public Case Case { get; set; }

        public async Task<IActionResult> OnGetAsync(string caseId)
        {
            if (string.IsNullOrWhiteSpace(caseId))
            {
                return BadRequest($"Case ID is required. Received: '{caseId ?? "null"}'");
            }

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest($"Invalid case ID format. Received: '{caseId}'. Expected a valid GUID format (xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).");
            }

            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == caseGuid);

            if (Case == null)
            {
                return NotFound($"Case with ID '{caseId}' not found.");
            }

            // Pre-populate with case disease
            LabResult.TestedDiseaseId = Case.DiseaseId;
            LabResult.CaseId = Case.Id;

            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string caseId)
        {
            if (string.IsNullOrWhiteSpace(caseId))
            {
                return BadRequest($"Case ID is required. Received: '{caseId ?? "null"}'");
            }

            if (!Guid.TryParse(caseId, out var caseGuid))
            {
                return BadRequest($"Invalid case ID format. Received: '{caseId}'. Expected a valid GUID format.");
            }

            CaseId = caseId;

            Case = await _context.Cases
                .Include(c => c.Patient)
                .Include(c => c.Disease)
                .FirstOrDefaultAsync(c => c.Id == caseGuid);

            if (Case == null)
            {
                return NotFound($"Case with ID '{caseId}' not found.");
            }

            // Parse markers from JSON
            List<LabResultMarker> markers = new List<LabResultMarker>();
            if (!string.IsNullOrWhiteSpace(MarkersJson))
            {
                try
                {
                    var markerDtos = JsonSerializer.Deserialize<List<MarkerDto>>(MarkersJson);
                    if (markerDtos != null && markerDtos.Any())
                    {
                        int order = 1;
                        foreach (var dto in markerDtos)
                        {
                            var marker = new LabResultMarker
                            {
                                PathogenId = dto.PathogenId,
                                TestMethodId = dto.TestMethodId,
                                TestResultId = dto.TestResultId,
                                QualitativeResultText = dto.QualitativeResult,
                                QuantitativeValue = dto.QuantitativeValue,
                                QuantitativeUnit = dto.QuantitativeUnit,
                                ReferenceRangeLow = dto.ReferenceRangeLow,
                                ReferenceRangeHigh = dto.ReferenceRangeHigh,
                                InterpretationFlag = dto.InterpretationFlag,
                                LOINCCode = dto.LOINCCode,
                                Notes = dto.Notes,
                                DisplayOrder = order++,
                                CreatedAt = DateTime.UtcNow
                            };
                            markers.Add(marker);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("", $"Error parsing markers: {ex.Message}");
                    await LoadSelectLists();
                    return Page();
                }
            }

            // Validate at least one marker
            if (!markers.Any())
            {
                ModelState.AddModelError("", "Please add at least one pathogen/biomarker test result.");
                await LoadSelectLists();
                return Page();
            }

            // Set case ID and create lab result
            LabResult.CaseId = Case.Id;
            LabResult.CreatedAt = DateTime.UtcNow;
            LabResult.FriendlyId = await GenerateLabResultIdAsync();
            LabResult.Markers = markers;

            // Handle attachment upload
            if (LabResultAttachment != null && LabResultAttachment.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "lab-results");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}_{LabResultAttachment.FileName}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await LabResultAttachment.CopyToAsync(fileStream);
                }

                LabResult.AttachmentPath = $"/uploads/lab-results/{uniqueFileName}";
                LabResult.AttachmentFileName = LabResultAttachment.FileName;
                LabResult.AttachmentSize = LabResultAttachment.Length;
            }

            _context.LabResults.Add(LabResult);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lab result {LabResult.FriendlyId} added successfully with {markers.Count} marker(s).";
            return RedirectToPage("/Cases/Details", new { id = Case.Id });
        }

        private async Task<string> GenerateLabResultIdAsync()
        {
            var prefix = "LR";
            var today = DateTime.Today;

            var lastResult = await _context.LabResults
                .Where(lr => lr.FriendlyId.StartsWith(prefix))
                .OrderByDescending(lr => lr.CreatedAt)
                .Select(lr => lr.FriendlyId)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastResult != null && lastResult.Length > prefix.Length)
            {
                var numberPart = lastResult.Substring(prefix.Length);
                if (int.TryParse(numberPart, out int currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D5}";
        }

        private async Task LoadSelectLists()
        {
            SpecimenTypesList = new SelectList(
                await _context.SpecimenTypes.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name");

            ResultUnitsList = new SelectList(
                await _context.ResultUnits.OrderBy(r => r.Name).ToListAsync(),
                "Id", "Name");

            // Load all active organizations (both laboratories and providers)
            LaboratoriesList = new SelectList(
                await _context.Organizations
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Name)
                    .ToListAsync(),
                "Id", "Name");

            // Use same list for ordering providers
            OrderingProvidersList = new SelectList(
                await _context.Organizations
                    .Where(o => o.IsActive)
                    .OrderBy(o => o.Name)
                    .ToListAsync(),
                "Id", "Name");
        }

        // API endpoint for searching pathogens
        public async Task<JsonResult> OnGetSearchPathogensAsync(string? disease, string? term)
        {
            var query = _context.Pathogens
                .Include(p => p.Disease)
                .Where(p => p.IsActive);

            if (Guid.TryParse(disease, out var diseaseId))
            {
                query = query.Where(p => p.DiseaseId == diseaseId);
            }

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(p => 
                    p.Name.Contains(term) || 
                    p.ShortName.Contains(term) ||
                    (p.LOINCCode != null && p.LOINCCode.Contains(term)));
            }

            var pathogens = await query
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Name)
                .Take(50)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    shortName = p.ShortName,
                    loincCode = p.LOINCCode,
                    category = p.Category.ToString(),
                    resultType = p.ResultType.ToString(),
                    defaultUnit = p.DefaultUnit,
                    refLow = p.DefaultReferenceRangeLow,
                    refHigh = p.DefaultReferenceRangeHigh,
                    disease = p.Disease != null ? p.Disease.Name : null
                })
                .ToListAsync();

            return new JsonResult(pathogens);
        }

        // API endpoint for searching organizations
        public async Task<JsonResult> OnGetSearchOrganizationsAsync(string? term)
        {
            var query = _context.Organizations
                .Where(o => o.IsActive);

            if (!string.IsNullOrWhiteSpace(term))
            {
                query = query.Where(o => 
                    o.Name.Contains(term) || 
                    (o.FriendlyId != null && o.FriendlyId.Contains(term)));
            }

            var organizations = await query
                .OrderBy(o => o.Name)
                .Take(20)
                .Select(o => new
                {
                    id = o.Id,
                    name = o.Name
                })
                .ToListAsync();

            return new JsonResult(organizations);
        }

        // API endpoint for getting test methods
        public async Task<JsonResult> OnGetTestMethodsAsync()
        {
            var methods = await _context.TestMethods
                .Where(tm => tm.IsActive)
                .OrderBy(tm => tm.DisplayOrder)
                .ThenBy(tm => tm.Name)
                .Select(tm => new
                {
                    id = tm.Id,
                    name = tm.Name,
                    description = tm.Description,
                    exportCode = tm.ExportCode
                })
                .ToListAsync();

            return new JsonResult(methods);
        }

        // API endpoint for getting test results (qualitative values)
        public async Task<JsonResult> OnGetTestResultsAsync()
        {
            var results = await _context.TestResults
                .Where(tr => tr.IsActive)
                .OrderBy(tr => tr.DisplayOrder)
                .ThenBy(tr => tr.Name)
                .Select(tr => new
                {
                    id = tr.Id,
                    name = tr.Name,
                    description = tr.Description,
                    snomedCode = tr.SnomedCode,
                    hl7Code = tr.Hl7Code
                })
                .ToListAsync();

            return new JsonResult(results);
        }

        // DTO for receiving marker data from client
        public class MarkerDto
        {
            public Guid PathogenId { get; set; }
            public int? TestMethodId { get; set; }
            public int? TestResultId { get; set; }
            public string? QualitativeResult { get; set; }
            public decimal? QuantitativeValue { get; set; }
            public string? QuantitativeUnit { get; set; }
            public decimal? ReferenceRangeLow { get; set; }
            public decimal? ReferenceRangeHigh { get; set; }
            public string? InterpretationFlag { get; set; }
            public string? LOINCCode { get; set; }
            public string? Notes { get; set; }
        }
    }
}
