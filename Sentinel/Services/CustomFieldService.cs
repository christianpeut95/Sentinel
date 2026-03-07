using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.Lookups;

namespace Sentinel.Services
{
    public class CustomFieldService
    {
        private readonly ApplicationDbContext _context;

        public CustomFieldService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all effective custom fields for a disease, including inherited fields from parent diseases
        /// </summary>
        public async Task<List<CustomFieldDefinition>> GetEffectiveFieldsForDiseaseAsync(Guid diseaseId)
        {
            var disease = await _context.Diseases
                .Include(d => d.DiseaseCustomFields)
                    .ThenInclude(dcf => dcf.CustomFieldDefinition)
                        .ThenInclude(cf => cf.LookupTable)
                            .ThenInclude(lt => lt.Values)
                .FirstOrDefaultAsync(d => d.Id == diseaseId);

            if (disease == null)
                return new List<CustomFieldDefinition>();

            // Get direct fields for this disease
            var directFields = disease.DiseaseCustomFields
                .Select(dcf => dcf.CustomFieldDefinition)
                .Where(cf => cf.IsActive && cf.ShowOnCaseForm)
                .ToList();

            // Get inherited fields from parent diseases
            var inheritedFields = new List<CustomFieldDefinition>();
            
            if (!string.IsNullOrEmpty(disease.PathIds))
            {
                // PathIds format is like "/parentId1/parentId2/currentId/"
                var parentIds = disease.PathIds
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Where(id => Guid.TryParse(id, out var guid) && guid != diseaseId)
                    .Select(id => Guid.Parse(id))
                    .ToList();

                if (parentIds.Any())
                {
                    inheritedFields = await _context.DiseaseCustomFields
                        .Where(dcf => parentIds.Contains(dcf.DiseaseId) && dcf.InheritToChildDiseases)
                        .Include(dcf => dcf.CustomFieldDefinition)
                            .ThenInclude(cf => cf.LookupTable)
                                .ThenInclude(lt => lt.Values)
                        .Select(dcf => dcf.CustomFieldDefinition)
                        .Where(cf => cf.IsActive)
                        .ToListAsync();
                }
            }

            // Combine and remove duplicates
            return directFields
                .Union(inheritedFields)
                .Distinct()
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToList();
        }

        /// <summary>
        /// Gets custom field values for a case
        /// </summary>
        public async Task<Dictionary<int, object>> GetCaseCustomFieldValuesAsync(Guid caseId)
        {
            var values = new Dictionary<int, object>();

            var stringValues = await _context.CaseCustomFieldStrings
                .Where(cf => cf.CaseId == caseId)
                .ToDictionaryAsync(cf => cf.FieldDefinitionId, cf => (object)cf.Value);

            var numberValues = await _context.CaseCustomFieldNumbers
                .Where(cf => cf.CaseId == caseId)
                .ToDictionaryAsync(cf => cf.FieldDefinitionId, cf => (object)cf.Value);

            var dateValues = await _context.CaseCustomFieldDates
                .Where(cf => cf.CaseId == caseId)
                .ToDictionaryAsync(cf => cf.FieldDefinitionId, cf => (object)cf.Value);

            var boolValues = await _context.CaseCustomFieldBooleans
                .Where(cf => cf.CaseId == caseId)
                .ToDictionaryAsync(cf => cf.FieldDefinitionId, cf => (object)cf.Value);

            var lookupValues = await _context.CaseCustomFieldLookups
                .Include(cf => cf.LookupValue)
                .Where(cf => cf.CaseId == caseId)
                .ToDictionaryAsync(cf => cf.FieldDefinitionId, cf => (object)cf.LookupValueId);

            foreach (var kvp in stringValues) values[kvp.Key] = kvp.Value;
            foreach (var kvp in numberValues) values[kvp.Key] = kvp.Value;
            foreach (var kvp in dateValues) values[kvp.Key] = kvp.Value;
            foreach (var kvp in boolValues) values[kvp.Key] = kvp.Value;
            foreach (var kvp in lookupValues) values[kvp.Key] = kvp.Value;

            return values;
        }

        /// <summary>
        /// Saves custom field values for a case
        /// </summary>
        public async Task SaveCaseCustomFieldValuesAsync(Guid caseId, IFormCollection form, List<CustomFieldDefinition> fields)
        {
            foreach (var field in fields)
            {
                var fieldKey = $"customfield_{field.Id}";
                if (!form.ContainsKey(fieldKey))
                    continue;

                var value = form[fieldKey].ToString();

                switch (field.FieldType)
                {
                    case CustomFieldType.Text:
                    case CustomFieldType.TextArea:
                    case CustomFieldType.Email:
                    case CustomFieldType.Phone:
                        await SaveStringFieldAsync(caseId, field.Id, value);
                        break;

                    case CustomFieldType.Number:
                        if (decimal.TryParse(value, out var numValue))
                            await SaveNumberFieldAsync(caseId, field.Id, numValue);
                        break;

                    case CustomFieldType.Date:
                        if (DateTime.TryParse(value, out var dateValue))
                            await SaveDateFieldAsync(caseId, field.Id, dateValue);
                        break;

                    case CustomFieldType.Checkbox:
                        var boolValue = value == "true";
                        await SaveBooleanFieldAsync(caseId, field.Id, boolValue);
                        break;

                    case CustomFieldType.Dropdown:
                        if (int.TryParse(value, out var lookupValueId))
                            await SaveLookupFieldAsync(caseId, field.Id, lookupValueId);
                        break;
                }
            }
        }

        private async Task SaveStringFieldAsync(Guid caseId, int fieldDefinitionId, string value)
        {
            var existing = await _context.CaseCustomFieldStrings
                .FirstOrDefaultAsync(cf => cf.CaseId == caseId && cf.FieldDefinitionId == fieldDefinitionId);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldStrings.Add(new CaseCustomFieldString
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task SaveNumberFieldAsync(Guid caseId, int fieldDefinitionId, decimal value)
        {
            var existing = await _context.CaseCustomFieldNumbers
                .FirstOrDefaultAsync(cf => cf.CaseId == caseId && cf.FieldDefinitionId == fieldDefinitionId);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldNumbers.Add(new CaseCustomFieldNumber
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task SaveDateFieldAsync(Guid caseId, int fieldDefinitionId, DateTime value)
        {
            var existing = await _context.CaseCustomFieldDates
                .FirstOrDefaultAsync(cf => cf.CaseId == caseId && cf.FieldDefinitionId == fieldDefinitionId);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldDates.Add(new CaseCustomFieldDate
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task SaveBooleanFieldAsync(Guid caseId, int fieldDefinitionId, bool value)
        {
            var existing = await _context.CaseCustomFieldBooleans
                .FirstOrDefaultAsync(cf => cf.CaseId == caseId && cf.FieldDefinitionId == fieldDefinitionId);

            if (existing != null)
            {
                existing.Value = value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldBooleans.Add(new CaseCustomFieldBoolean
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = value,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        private async Task SaveLookupFieldAsync(Guid caseId, int fieldDefinitionId, int lookupValueId)
        {
            var existing = await _context.CaseCustomFieldLookups
                .FirstOrDefaultAsync(cf => cf.CaseId == caseId && cf.FieldDefinitionId == fieldDefinitionId);

            if (existing != null)
            {
                existing.LookupValueId = lookupValueId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.CaseCustomFieldLookups.Add(new CaseCustomFieldLookup
                {
                    CaseId = caseId,
                    FieldDefinitionId = fieldDefinitionId,
                    LookupValueId = lookupValueId,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
    }
}
