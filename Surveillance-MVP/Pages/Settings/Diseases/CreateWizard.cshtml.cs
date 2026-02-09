using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;
using Surveillance_MVP.Models.Lookups;
using System.Security.Claims;

namespace Surveillance_MVP.Pages.Settings.Diseases
{
    [Authorize(Policy = "Permission.Settings.ManageSystemLookups")]
    public class CreateWizardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateWizardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Disease Disease { get; set; } = new Disease();

        [BindProperty]
        public string ChildDiseaseList { get; set; } = string.Empty;

        [BindProperty]
        public bool CreateChildren { get; set; }

        public SelectList ParentDiseases { get; set; } = default!;
        public SelectList Categories { get; set; } = default!;
        public List<Symptom> AllSymptoms { get; set; } = new();
        public List<CustomFieldDefinition> AvailableCustomFields { get; set; } = new();

        // Temporary storage for wizard state
        public List<string> ChildDiseaseNames { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(Guid? parentId)
        {
            await LoadDropdowns();
            await LoadSymptoms();
            await LoadCustomFields();

            if (parentId.HasValue)
            {
                Disease.ParentDiseaseId = parentId.Value;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? action = null)
        {
            // Set ExportCode to Code if not provided
            if (string.IsNullOrWhiteSpace(Disease.ExportCode))
            {
                Disease.ExportCode = Disease.Code;
            }

            // Remove validation for child diseases list if not creating children
            if (!CreateChildren)
            {
                ModelState.Remove(nameof(ChildDiseaseList));
            }
            
            // Remove action parameter from validation
            ModelState.Remove(nameof(action));

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                await LoadSymptoms();
                await LoadCustomFields();
                TempData["ErrorMessage"] = "Please correct the errors and try again.";
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // Validate disease code is unique
                if (await _context.Diseases.AnyAsync(d => d.Code == Disease.Code))
                {
                    ModelState.AddModelError("Disease.Code", "A disease with this code already exists.");
                    await LoadDropdowns();
                    await LoadSymptoms();
                    await LoadCustomFields();
                    return Page();
                }

                // Step 1: Create parent disease
                // Let EF Core generate the ID
                Disease.CreatedAt = DateTime.UtcNow;
                _context.Diseases.Add(Disease);
                await _context.SaveChangesAsync();

                // Now Disease.Id is populated by EF Core
                var diseaseId = Disease.Id;

                // Step 2: Save symptoms for parent disease
                await SaveSymptomsAsync(diseaseId, userId);

                // Step 3: Create child diseases (they will inherit symptoms from parent)
                if (CreateChildren && !string.IsNullOrWhiteSpace(ChildDiseaseList))
                {
                    await CreateChildDiseasesAsync(diseaseId, userId);
                }

                // Step 4: Save custom fields
                await SaveCustomFieldsAsync(diseaseId, userId);

                TempData["SuccessMessage"] = CreateChildren 
                    ? $"Disease '{Disease.Name}' and its child diseases created successfully."
                    : $"Disease '{Disease.Name}' created successfully.";

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                await LoadDropdowns();
                await LoadSymptoms();
                await LoadCustomFields();
                return Page();
            }
        }

        private async Task CreateChildDiseasesAsync(Guid parentId, string? userId)
        {
            var lines = ChildDiseaseList.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            var parentDisease = await _context.Diseases.FindAsync(parentId);

            // Track codes to ensure uniqueness within this batch
            var usedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Get existing codes from database to avoid conflicts
            var existingCodes = await _context.Diseases
                .Select(d => d.Code)
                .ToListAsync();
            
            foreach (var code in existingCodes)
            {
                usedCodes.Add(code);
            }

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                // Parse line format: "Name|Code|ExportCode" or just "Name"
                var parts = trimmedLine.Split('|');
                var name = parts[0].Trim();
                var code = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])
                    ? parts[1].Trim()
                    : GenerateUniqueCode(name, usedCodes);
                var exportCode = parts.Length > 2 ? parts[2].Trim() : code;

                // Ensure the code is unique (in case user provided duplicate)
                code = EnsureUniqueCode(code, usedCodes);
                usedCodes.Add(code);

                var childDisease = new Disease
                {
                    // Let EF Core generate ID - don't set it manually
                    Name = name,
                    Code = code,
                    ExportCode = exportCode,
                    ParentDiseaseId = parentId,
                    DiseaseCategoryId = Disease.DiseaseCategoryId,
                    IsActive = true,
                    Description = $"Subtype of {Disease.Name}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Diseases.Add(childDisease);
            }

            await _context.SaveChangesAsync();

            // Apply symptoms to children if parent has symptoms
            var parentSymptoms = await _context.DiseaseSymptoms
                .Where(ds => ds.DiseaseId == parentId)
                .ToListAsync();

            if (parentSymptoms.Any())
            {
                var childDiseases = await _context.Diseases
                    .Where(d => d.ParentDiseaseId == parentId)
                    .ToListAsync();

                foreach (var child in childDiseases)
                {
                    foreach (var parentSymptom in parentSymptoms)
                    {
                        _context.DiseaseSymptoms.Add(new DiseaseSymptom
                        {
                            DiseaseId = child.Id,
                            SymptomId = parentSymptom.SymptomId,
                            IsCommon = parentSymptom.IsCommon,
                            SortOrder = parentSymptom.SortOrder,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private string GenerateUniqueCode(string name, HashSet<string> usedCodes)
        {
            // Try to extract meaningful parts from the name
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Strategy 1: Use first word + last word (better for "Meningococcal Serogroup A")
            if (words.Length >= 3)
            {
                // For "Meningococcal Serogroup A" -> "MENI-A"
                var firstPart = words[0].Substring(0, Math.Min(4, words[0].Length)).ToUpper();
                var lastPart = words[^1].ToUpper(); // Last word
                var baseCode = $"{firstPart}-{lastPart}";
                
                if (!usedCodes.Contains(baseCode))
                    return baseCode;
            }
            
            // Strategy 2: First two words
            if (words.Length >= 2)
            {
                var baseCode = $"{words[0].Substring(0, Math.Min(4, words[0].Length)).ToUpper()}-{words[1].Substring(0, Math.Min(4, words[1].Length)).ToUpper()}";
                
                if (!usedCodes.Contains(baseCode))
                    return baseCode;
                
                // If duplicate, will be handled by EnsureUniqueCode
                return baseCode;
            }
            
            // Strategy 3: Single word
            return name.Substring(0, Math.Min(8, name.Length)).ToUpper();
        }

        private string EnsureUniqueCode(string baseCode, HashSet<string> usedCodes)
        {
            var code = baseCode;
            var counter = 1;
            
            // If code already used, append number
            while (usedCodes.Contains(code))
            {
                code = $"{baseCode}{counter}";
                counter++;
            }
            
            return code;
        }

        private string GenerateCodeFromName(string name)
        {
            // Generate a code from the name (e.g., "Salmonella Typhimurium" -> "SALM-TYPH")
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                return $"{words[0].Substring(0, Math.Min(4, words[0].Length)).ToUpper()}-{words[1].Substring(0, Math.Min(4, words[1].Length)).ToUpper()}";
            }
            return name.Substring(0, Math.Min(8, name.Length)).ToUpper();
        }

        private async Task SaveSymptomsAsync(Guid diseaseId, string? userId)
        {
            var selectedSymptomIds = Request.Form.Keys
                .Where(k => k.StartsWith("symptom_") && 
                           !k.Contains("_common_") && 
                           !k.Contains("_order_"))
                .Select(k => int.Parse(k.Replace("symptom_", "")))
                .ToList();

            foreach (var symptomId in selectedSymptomIds)
            {
                var isCommonKey = $"symptom_common_{symptomId}";
                var sortOrderKey = $"symptom_order_{symptomId}";

                var isCommon = Request.Form.ContainsKey(isCommonKey) && Request.Form[isCommonKey] == "true";
                var sortOrder = 0;
                if (Request.Form.ContainsKey(sortOrderKey))
                {
                    int.TryParse(Request.Form[sortOrderKey], out sortOrder);
                }

                _context.DiseaseSymptoms.Add(new DiseaseSymptom
                {
                    DiseaseId = diseaseId,
                    SymptomId = symptomId,
                    IsCommon = isCommon,
                    SortOrder = sortOrder,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SaveCustomFieldsAsync(Guid diseaseId, string? userId)
        {
            var selectedFieldIds = Request.Form.Keys
                .Where(k => k.StartsWith("field_"))
                .Select(k => int.Parse(k.Replace("field_", "")))
                .ToList();

            // Track which fields should inherit to children
            var inheritFields = new List<int>();

            // Save custom fields for parent disease
            foreach (var fieldId in selectedFieldIds)
            {
                var inheritKey = $"inherit_{fieldId}";
                var inherit = Request.Form.ContainsKey(inheritKey) && Request.Form[inheritKey] == "true";

                _context.DiseaseCustomFields.Add(new DiseaseCustomField
                {
                    DiseaseId = diseaseId,
                    CustomFieldDefinitionId = fieldId,
                    InheritToChildDiseases = inherit,
                    CreatedAt = DateTime.UtcNow
                });

                if (inherit)
                {
                    inheritFields.Add(fieldId);
                }
            }

            await _context.SaveChangesAsync();

            // Apply custom fields to children if they were created
            if (CreateChildren && !string.IsNullOrWhiteSpace(ChildDiseaseList))
            {
                var childDiseases = await _context.Diseases
                    .Where(d => d.ParentDiseaseId == diseaseId)
                    .ToListAsync();

                foreach (var child in childDiseases)
                {
                    foreach (var fieldId in selectedFieldIds)
                    {
                        // Check if this field should be inherited
                        var shouldInherit = inheritFields.Contains(fieldId);

                        _context.DiseaseCustomFields.Add(new DiseaseCustomField
                        {
                            DiseaseId = child.Id,
                            CustomFieldDefinitionId = fieldId,
                            InheritToChildDiseases = shouldInherit,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task LoadDropdowns()
        {
            var diseases = await _context.Diseases
                .Where(d => d.IsActive)
                .OrderBy(d => d.Level)
                .ThenBy(d => d.Name)
                .Select(d => new
                {
                    d.Id,
                    DisplayName = new string('—', d.Level) + " " + d.Name
                })
                .ToListAsync();

            ParentDiseases = new SelectList(diseases, "Id", "DisplayName");

            Categories = new SelectList(
                await _context.DiseaseCategories
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToListAsync(),
                "Id", "Name");
        }

        private async Task LoadSymptoms()
        {
            AllSymptoms = await _context.Symptoms
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        private async Task LoadCustomFields()
        {
            AvailableCustomFields = await _context.CustomFieldDefinitions
                .Where(f => f.ShowOnCaseForm && f.IsActive)
                .Include(f => f.LookupTable)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
        }
    }
}
