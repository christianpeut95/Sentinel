using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models.Lookups;
using System.Text;

namespace Sentinel.Pages.Settings.Jurisdictions
{
    [Authorize]
    public class BulkPopulationUploadModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public BulkPopulationUploadModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public IFormFile? CsvFile { get; set; }

        [BindProperty]
        public int? DefaultPopulationYear { get; set; }

        [BindProperty]
        public string? DefaultPopulationSource { get; set; }

        [BindProperty]
        public bool OverwriteExisting { get; set; } = false;

        public List<PopulationPreview> PreviewData { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public int ProcessedCount { get; set; }
        public int SkippedCount { get; set; }
        public int UpdatedCount { get; set; }

        public class PopulationPreview
        {
            public string JurisdictionCode { get; set; } = string.Empty;
            public string JurisdictionName { get; set; } = string.Empty;
            public long Population { get; set; }
            public int? PopulationYear { get; set; }
            public string? PopulationSource { get; set; }
            public bool Exists { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostPreviewAsync()
        {
            if (CsvFile == null || CsvFile.Length == 0)
            {
                ModelState.AddModelError("CsvFile", "Please select a CSV file");
                return Page();
            }

            try
            {
                using var reader = new StreamReader(CsvFile.OpenReadStream());
                var csvContent = await reader.ReadToEndAsync();
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    ModelState.AddModelError("CsvFile", "CSV file must contain a header row and at least one data row");
                    return Page();
                }

                // Parse header
                var header = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToList();
                
                // Validate required columns
                var requiredColumns = new[] { "JurisdictionCode", "Population" };
                var missingColumns = requiredColumns.Where(rc => !header.Any(h => h.Equals(rc, StringComparison.OrdinalIgnoreCase))).ToList();
                
                if (missingColumns.Any())
                {
                    ModelState.AddModelError("CsvFile", $"Missing required columns: {string.Join(", ", missingColumns)}");
                    return Page();
                }

                // Get column indexes
                var codeIndex = header.FindIndex(h => h.Equals("JurisdictionCode", StringComparison.OrdinalIgnoreCase));
                var nameIndex = header.FindIndex(h => h.Equals("JurisdictionName", StringComparison.OrdinalIgnoreCase));
                var populationIndex = header.FindIndex(h => h.Equals("Population", StringComparison.OrdinalIgnoreCase));
                var yearIndex = header.FindIndex(h => h.Equals("PopulationYear", StringComparison.OrdinalIgnoreCase));
                var sourceIndex = header.FindIndex(h => h.Equals("PopulationSource", StringComparison.OrdinalIgnoreCase));

                // Load all jurisdictions for lookup
                var jurisdictions = await _context.Jurisdictions
                    .Where(j => j.Code != null)
                    .ToDictionaryAsync(j => j.Code!.ToUpper(), j => j);

                // Parse data rows
                for (int i = 1; i < Math.Min(lines.Length, 101); i++) // Preview first 100 rows
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    
                    if (values.Count < header.Count)
                    {
                        ValidationErrors.Add($"Line {i + 1}: Invalid CSV format");
                        continue;
                    }

                    var code = values[codeIndex].Trim().Trim('"');
                    var name = nameIndex >= 0 ? values[nameIndex].Trim().Trim('"') : "";
                    
                    if (!long.TryParse(values[populationIndex].Trim(), out var population))
                    {
                        ValidationErrors.Add($"Line {i + 1}: Invalid population value '{values[populationIndex]}'");
                        continue;
                    }

                    int? year = null;
                    if (yearIndex >= 0 && !string.IsNullOrWhiteSpace(values[yearIndex]))
                    {
                        if (int.TryParse(values[yearIndex].Trim(), out var parsedYear))
                            year = parsedYear;
                    }
                    year ??= DefaultPopulationYear;

                    var source = sourceIndex >= 0 && !string.IsNullOrWhiteSpace(values[sourceIndex])
                        ? values[sourceIndex].Trim().Trim('"')
                        : DefaultPopulationSource;

                    var exists = jurisdictions.ContainsKey(code.ToUpper());
                    if (!exists)
                    {
                        ValidationErrors.Add($"Line {i + 1}: Jurisdiction with code '{code}' not found");
                    }

                    PreviewData.Add(new PopulationPreview
                    {
                        JurisdictionCode = code,
                        JurisdictionName = exists ? jurisdictions[code.ToUpper()].Name : name,
                        Population = population,
                        PopulationYear = year,
                        PopulationSource = source,
                        Exists = exists
                    });
                }

                TempData["CsvContent"] = csvContent;
                TempData["DefaultYear"] = DefaultPopulationYear;
                TempData["DefaultSource"] = DefaultPopulationSource;
                TempData["OverwriteExisting"] = OverwriteExisting;

                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error processing file: {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostImportAsync()
        {
            var csvContent = TempData["CsvContent"]?.ToString();
            if (string.IsNullOrEmpty(csvContent))
            {
                TempData["Error"] = "Session expired. Please upload the file again.";
                return RedirectToPage();
            }

            DefaultPopulationYear = TempData["DefaultYear"] as int?;
            DefaultPopulationSource = TempData["DefaultSource"]?.ToString();
            OverwriteExisting = (bool)(TempData["OverwriteExisting"] ?? false);

            try
            {
                var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                var header = lines[0].Split(',').Select(h => h.Trim().Trim('"')).ToList();

                var codeIndex = header.FindIndex(h => h.Equals("JurisdictionCode", StringComparison.OrdinalIgnoreCase));
                var populationIndex = header.FindIndex(h => h.Equals("Population", StringComparison.OrdinalIgnoreCase));
                var yearIndex = header.FindIndex(h => h.Equals("PopulationYear", StringComparison.OrdinalIgnoreCase));
                var sourceIndex = header.FindIndex(h => h.Equals("PopulationSource", StringComparison.OrdinalIgnoreCase));

                var jurisdictions = await _context.Jurisdictions
                    .Where(j => j.Code != null)
                    .ToDictionaryAsync(j => j.Code!.ToUpper(), j => j);

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var values = ParseCsvLine(line);
                    if (values.Count < header.Count) continue;

                    var code = values[codeIndex].Trim().Trim('"');
                    if (!long.TryParse(values[populationIndex].Trim(), out var population)) continue;

                    if (!jurisdictions.TryGetValue(code.ToUpper(), out var jurisdiction))
                    {
                        SkippedCount++;
                        continue;
                    }

                    // Check if we should skip existing
                    if (!OverwriteExisting && jurisdiction.Population.HasValue)
                    {
                        SkippedCount++;
                        continue;
                    }

                    int? year = null;
                    if (yearIndex >= 0 && !string.IsNullOrWhiteSpace(values[yearIndex]))
                    {
                        if (int.TryParse(values[yearIndex].Trim(), out var parsedYear))
                            year = parsedYear;
                    }
                    year ??= DefaultPopulationYear;

                    var source = sourceIndex >= 0 && !string.IsNullOrWhiteSpace(values[sourceIndex])
                        ? values[sourceIndex].Trim().Trim('"')
                        : DefaultPopulationSource;

                    jurisdiction.Population = population;
                    jurisdiction.PopulationYear = year;
                    jurisdiction.PopulationSource = source;

                    ProcessedCount++;
                    if (jurisdiction.Population.HasValue)
                        UpdatedCount++;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Successfully updated {ProcessedCount} jurisdictions. {SkippedCount} skipped.";
                return RedirectToPage("/Settings/Jurisdictions/Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error importing data: {ex.Message}";
                return RedirectToPage();
            }
        }

        public IActionResult OnPostDownloadTemplate()
        {
            var csv = new StringBuilder();
            csv.AppendLine("JurisdictionCode,JurisdictionName,Population,PopulationYear,PopulationSource");
            csv.AppendLine("STATE01,Example State,1000000,2024,Census Bureau");
            csv.AppendLine("COUNTY01,Example County,50000,2024,Census Bureau");
            csv.AppendLine("CITY01,Example City,25000,2024,Census Bureau");

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", "population_template.csv");
        }

        private List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(ch);
                }
            }

            values.Add(currentValue.ToString());
            return values;
        }
    }
}
