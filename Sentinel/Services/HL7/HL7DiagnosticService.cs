using Microsoft.EntityFrameworkCore;
using Sentinel.Data;

namespace Sentinel.Services.HL7
{
    /// <summary>
    /// Diagnostic service to help troubleshoot HL7 case creation issues
    /// </summary>
    public class HL7DiagnosticService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HL7DiagnosticService> _logger;

        public HL7DiagnosticService(
            ApplicationDbContext context,
            ILogger<HL7DiagnosticService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Analyzes why a case was not created for a given lab result
        /// </summary>
        public async Task<HL7DiagnosticReport> DiagnoseLabResultAsync(Guid labResultId)
        {
            var report = new HL7DiagnosticReport { LabResultId = labResultId };

            try
            {
                // 1. Check if lab result exists
                var labResult = await _context.LabResults
                    .Include(lr => lr.Markers)
                    .Include(lr => lr.Patient)
                    .Include(lr => lr.Case)
                        .ThenInclude(c => c!.Disease)
                    .FirstOrDefaultAsync(lr => lr.Id == labResultId);

                if (labResult == null)
                {
                    report.Issues.Add("❌ Lab result not found");
                    return report;
                }

                report.LabResultFriendlyId = labResult.FriendlyId;
                report.PatientFriendlyId = labResult.Patient?.FriendlyId;
                report.CaseFriendlyId = labResult.Case?.FriendlyId;

                // 2. Check if case already exists
                if (labResult.Case != null)
                {
                    report.Issues.Add($"✅ Case already exists: {labResult.Case.FriendlyId} ({labResult.Case.Disease?.Name})");
                    return report;
                }

                // 3. Check markers
                if (labResult.Markers == null || !labResult.Markers.Any())
                {
                    report.Issues.Add("❌ No markers found on lab result");
                    report.Recommendation = "Lab result has no test markers. Check OBX segments in the HL7 message.";
                    return report;
                }

                report.MarkersCount = labResult.Markers.Count;
                report.Issues.Add($"✅ Found {labResult.Markers.Count} marker(s)");

                // 4. Check each marker for pathogen/disease mapping
                foreach (var marker in labResult.Markers)
                {
                    var markerReport = new MarkerDiagnostic
                    {
                        TestCode = marker.TestCode ?? "N/A",
                        LOINCCode = marker.LOINCCode ?? "N/A",
                        Result = marker.QualitativeResultText ?? marker.QuantitativeValue?.ToString() ?? "N/A"
                    };

                    // Check if pathogen exists for this LOINC code
                    var pathogen = await _context.Pathogens
                        .Include(p => p.Disease)
                        .FirstOrDefaultAsync(p => p.LOINCCode == marker.TestCode && p.IsActive);

                    if (pathogen == null)
                    {
                        markerReport.Issue = $"❌ No active pathogen found with LOINC code '{marker.TestCode}'";
                        markerReport.Recommendation = $"Add a pathogen with LOINC code '{marker.TestCode}' and link it to a disease";
                    }
                    else
                    {
                        markerReport.PathogenName = pathogen.Name;

                        if (pathogen.Disease == null)
                        {
                            markerReport.Issue = $"❌ Pathogen '{pathogen.Name}' exists but is not linked to a disease";
                            markerReport.Recommendation = $"Link pathogen '{pathogen.Name}' to a disease in Settings > Pathogens";
                        }
                        else
                        {
                            markerReport.DiseaseName = pathogen.Disease.Name;
                            markerReport.Issue = $"✅ Mapped to disease: {pathogen.Disease.Name}";

                            // Check if disease is active
                            if (!pathogen.Disease.IsActive)
                            {
                                markerReport.Issue += " (⚠️ Disease is inactive)";
                                markerReport.Recommendation = $"Activate disease '{pathogen.Disease.Name}' in Settings > Diseases";
                            }
                        }
                    }

                    report.Markers.Add(markerReport);
                }

                // 5. Check if any marker has disease mapping
                if (!report.Markers.Any(m => m.DiseaseName != null))
                {
                    report.Issues.Add("❌ None of the markers are mapped to a disease");
                    report.Recommendation = "Configure LOINC mappings in Settings > Pathogens to link test codes to diseases";
                }
                else
                {
                    var mappedDiseases = report.Markers
                        .Where(m => m.DiseaseName != null)
                        .Select(m => m.DiseaseName)
                        .Distinct()
                        .ToList();

                    report.Issues.Add($"✅ Markers mapped to disease(s): {string.Join(", ", mappedDiseases)}");
                    report.Recommendation = "Check HL7 processing logs for errors during case creation. The system should have attempted to create a case.";
                }

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error diagnosing lab result {LabResultId}", labResultId);
                report.Issues.Add($"❌ Error during diagnosis: {ex.Message}");
                return report;
            }
        }

        /// <summary>
        /// Lists all active pathogens and their disease mappings
        /// </summary>
        public async Task<List<PathogenMapping>> GetAllPathogenMappingsAsync()
        {
            return await _context.Pathogens
                .Include(p => p.Disease)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .Select(p => new PathogenMapping
                {
                    PathogenName = p.Name,
                    LOINCCode = p.LOINCCode ?? "N/A",
                    DiseaseName = p.Disease != null ? p.Disease.Name : "⚠️ Not mapped",
                    DiseaseIsActive = p.Disease != null && p.Disease.IsActive
                })
                .ToListAsync();
        }
    }

    public class HL7DiagnosticReport
    {
        public Guid LabResultId { get; set; }
        public string? LabResultFriendlyId { get; set; }
        public string? PatientFriendlyId { get; set; }
        public string? CaseFriendlyId { get; set; }
        public int MarkersCount { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<MarkerDiagnostic> Markers { get; set; } = new();
        public string? Recommendation { get; set; }
    }

    public class MarkerDiagnostic
    {
        public string TestCode { get; set; } = "";
        public string LOINCCode { get; set; } = "";
        public string Result { get; set; } = "";
        public string? PathogenName { get; set; }
        public string? DiseaseName { get; set; }
        public string? Issue { get; set; }
        public string? Recommendation { get; set; }
    }

    public class PathogenMapping
    {
        public string PathogenName { get; set; } = "";
        public string LOINCCode { get; set; } = "";
        public string DiseaseName { get; set; } = "";
        public bool DiseaseIsActive { get; set; }
    }
}
