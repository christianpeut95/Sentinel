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
    public class EditLabResultModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditLabResultModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LabResult LabResult { get; set; }

        [BindProperty]
        public IFormFile? LabResultAttachment { get; set; }

        [BindProperty]
        public string? MarkersJson { get; set; }

        [BindProperty]
        public string? DeletedMarkerIds { get; set; }

        public SelectList SpecimenTypesList { get; set; }
        public SelectList ResultUnitsList { get; set; }
        public string CaseId { get; set; }
        public List<LabResultMarker> ExistingMarkers { get; set; } = new List<LabResultMarker>();

        public async Task<IActionResult> OnGetAsync(Guid labResultId, string caseId)
        {
            CaseId = caseId;

            LabResult = await _context.LabResults
                .Include(lr => lr.Laboratory)
                .Include(lr => lr.OrderingProvider)
                .Include(lr => lr.SpecimenType)
                .Include(lr => lr.ResultUnits)
                .Include(lr => lr.TestedDisease)
                .Include(lr => lr.Markers)
                    .ThenInclude(m => m.Pathogen)
                .Include(lr => lr.Markers)
                    .ThenInclude(m => m.TestMethod)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId);

            if (LabResult == null)
            {
                return NotFound();
            }

            ExistingMarkers = LabResult.Markers.OrderBy(m => m.DisplayOrder).ToList();
            await LoadSelectLists();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid labResultId, string caseId)
        {
            CaseId = caseId;

            var labResultToUpdate = await _context.LabResults
                .Include(lr => lr.Markers)
                .FirstOrDefaultAsync(lr => lr.Id == labResultId);

            if (labResultToUpdate == null)
            {
                return NotFound();
            }

            // Handle deleted markers
            if (!string.IsNullOrWhiteSpace(DeletedMarkerIds))
            {
                var deletedIds = JsonSerializer.Deserialize<List<Guid>>(DeletedMarkerIds);
                if (deletedIds != null && deletedIds.Any())
                {
                    var markersToDelete = labResultToUpdate.Markers
                        .Where(m => deletedIds.Contains(m.Id))
                        .ToList();

                    foreach (var marker in markersToDelete)
                    {
                        _context.LabResultMarkers.Remove(marker);
                    }
                }
            }

            // Handle new/updated markers
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
                            LabResultMarker marker;
                            if (dto.Id.HasValue && dto.Id != Guid.Empty)
                            {
                                // Update existing marker
                                marker = labResultToUpdate.Markers.FirstOrDefault(m => m.Id == dto.Id);
                                if (marker != null)
                                {
                                    marker.PathogenId = dto.PathogenId;
                                    marker.TestMethodId = dto.TestMethodId;
                                    marker.TestResultId = dto.TestResultId;
                                    marker.QualitativeResultText = dto.QualitativeResult;
                                    marker.QuantitativeValue = dto.QuantitativeValue;
                                    marker.QuantitativeUnit = dto.QuantitativeUnit;
                                    marker.ReferenceRangeLow = dto.ReferenceRangeLow;
                                    marker.ReferenceRangeHigh = dto.ReferenceRangeHigh;
                                    marker.InterpretationFlag = dto.InterpretationFlag;
                                    marker.LOINCCode = dto.LOINCCode;
                                    marker.Notes = dto.Notes;
                                    marker.DisplayOrder = order++;
                                    marker.ModifiedAt = DateTime.UtcNow;
                                }
                            }
                            else
                            {
                                // Add new marker
                                marker = new LabResultMarker
                                {
                                    LabResultId = labResultId,
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
                                _context.LabResultMarkers.Add(marker);
                            }
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

            // Update lab result properties
            labResultToUpdate.LaboratoryId = LabResult.LaboratoryId;
            labResultToUpdate.OrderingProviderId = LabResult.OrderingProviderId;
            labResultToUpdate.AccessionNumber = LabResult.AccessionNumber;
            labResultToUpdate.TestedDiseaseId = LabResult.TestedDiseaseId;
            labResultToUpdate.SpecimenCollectionDate = LabResult.SpecimenCollectionDate;
            labResultToUpdate.SpecimenTypeId = LabResult.SpecimenTypeId;
            labResultToUpdate.ResultDate = LabResult.ResultDate;
            labResultToUpdate.ResultUnitsId = LabResult.ResultUnitsId;
            labResultToUpdate.IsAmended = LabResult.IsAmended;
            labResultToUpdate.Notes = LabResult.Notes;
            labResultToUpdate.LabInterpretation = LabResult.LabInterpretation;
            labResultToUpdate.ModifiedAt = DateTime.UtcNow;

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

                labResultToUpdate.AttachmentPath = $"/uploads/lab-results/{uniqueFileName}";
                labResultToUpdate.AttachmentFileName = LabResultAttachment.FileName;
                labResultToUpdate.AttachmentSize = LabResultAttachment.Length;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Lab result {labResultToUpdate.FriendlyId} updated successfully.";
            return RedirectToPage("/Cases/Details", new { id = caseId });
        }

        private async Task LoadSelectLists()
        {
            SpecimenTypesList = new SelectList(
                await _context.SpecimenTypes.OrderBy(s => s.Name).ToListAsync(),
                "Id", "Name", LabResult?.SpecimenTypeId);

            // LEGACY: TestTypes removed - use Pathogen/Markers system instead
            // TestTypesList = new SelectList(
            //     await _context.TestTypes.OrderBy(t => t.Name).ToListAsync(),
            //     "Id", "Name", LabResult?.TestTypeId);

            ResultUnitsList = new SelectList(
                await _context.ResultUnits.OrderBy(r => r.Name).ToListAsync(),
                "Id", "Name", LabResult?.ResultUnitsId);
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

        // DTO for receiving marker data from client
        public class MarkerDto
        {
            public Guid? Id { get; set; }
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
