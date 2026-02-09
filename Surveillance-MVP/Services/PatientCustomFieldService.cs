using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public class PatientCustomFieldService : IPatientCustomFieldService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public PatientCustomFieldService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        public async Task<List<CustomFieldDefinition>> GetCreateEditFieldsAsync()
        {
            return await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                    .ThenInclude(lt => lt!.Values.Where(v => v.IsActive))
                .Where(f => f.IsActive && f.ShowOnCreateEdit)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<List<CustomFieldDefinition>> GetDetailsFieldsAsync()
        {
            return await _context.CustomFieldDefinitions
                .Include(f => f.LookupTable)
                    .ThenInclude(lt => lt!.Values.Where(v => v.IsActive))
                .Where(f => f.IsActive && f.ShowOnDetails && f.ShowOnPatientForm)
                .OrderBy(f => f.Category)
                .ThenBy(f => f.DisplayOrder)
                .ToListAsync();
        }

        public async Task<Dictionary<int, string?>> GetPatientFieldValuesAsync(Guid patientId)
        {
            var values = new Dictionary<int, string?>();

            var stringFields = await _context.PatientCustomFieldStrings
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value);

            var numberFields = await _context.PatientCustomFieldNumbers
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value?.ToString());

            var dateFields = await _context.PatientCustomFieldDates
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value?.ToString("yyyy-MM-dd"));

            var boolFields = await _context.PatientCustomFieldBooleans
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value.ToString());

            var lookupFields = await _context.PatientCustomFieldLookups
                .Include(f => f.LookupValue)
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.LookupValueId.ToString());

            foreach (var kvp in stringFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in numberFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in dateFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in boolFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in lookupFields) values[kvp.Key] = kvp.Value;

            return values;
        }

        public async Task<Dictionary<int, string?>> GetPatientFieldDisplayValuesAsync(Guid patientId)
        {
            var values = new Dictionary<int, string?>();

            var stringFields = await _context.PatientCustomFieldStrings
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value);

            var numberFields = await _context.PatientCustomFieldNumbers
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value?.ToString());

            var dateFields = await _context.PatientCustomFieldDates
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value?.ToString("yyyy-MM-dd"));

            var boolFields = await _context.PatientCustomFieldBooleans
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.Value.ToString());

            var lookupFields = await _context.PatientCustomFieldLookups
                .Include(f => f.LookupValue)
                .Where(f => f.PatientId == patientId)
                .ToDictionaryAsync(f => f.FieldDefinitionId, f => f.LookupValue?.DisplayText);

            foreach (var kvp in stringFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in numberFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in dateFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in boolFields) values[kvp.Key] = kvp.Value;
            foreach (var kvp in lookupFields) values[kvp.Key] = kvp.Value;

            return values;
        }

        public async Task SavePatientFieldValuesAsync(Guid patientId, Dictionary<string, string?> fieldValues, string? userId, string? ipAddress)
        {
            // Get old values for audit comparison
            var oldValues = await GetPatientFieldDisplayValuesAsync(patientId);
            
            foreach (var kvp in fieldValues)
            {
                if (!kvp.Key.StartsWith("customfield_")) continue;

                var fieldIdStr = kvp.Key.Replace("customfield_", "");
                if (!int.TryParse(fieldIdStr, out int fieldDefinitionId)) continue;

                var fieldDef = await _context.CustomFieldDefinitions.FindAsync(fieldDefinitionId);
                if (fieldDef == null || !fieldDef.IsActive) continue;

                var value = kvp.Value;
                var hasValue = !string.IsNullOrWhiteSpace(value);

                // Get old value for this field
                oldValues.TryGetValue(fieldDefinitionId, out var oldValue);
                string? newDisplayValue = null;

                switch (fieldDef.FieldType)
                {
                    case CustomFieldType.Text:
                    case CustomFieldType.TextArea:
                    case CustomFieldType.Email:
                    case CustomFieldType.Phone:
                        await SaveStringFieldAsync(patientId, fieldDefinitionId, value, hasValue);
                        newDisplayValue = value;
                        break;

                    case CustomFieldType.Number:
                        await SaveNumberFieldAsync(patientId, fieldDefinitionId, value, hasValue);
                        newDisplayValue = value;
                        break;

                    case CustomFieldType.Date:
                        await SaveDateFieldAsync(patientId, fieldDefinitionId, value, hasValue);
                        if (hasValue && DateTime.TryParse(value, out var dateVal))
                        {
                            newDisplayValue = dateVal.ToString("dd MMM yyyy");
                        }
                        break;

                    case CustomFieldType.Checkbox:
                        await SaveBooleanFieldAsync(patientId, fieldDefinitionId, value, hasValue);
                        newDisplayValue = (value == "true" || value == "on") ? "Yes" : "No";
                        break;

                    case CustomFieldType.Dropdown:
                        await SaveLookupFieldAsync(patientId, fieldDefinitionId, value, hasValue);
                        if (hasValue && int.TryParse(value, out var lookupId))
                        {
                            var lookupValue = await _context.LookupValues.FindAsync(lookupId);
                            newDisplayValue = lookupValue?.DisplayText;
                        }
                        break;
                }

                // Log audit if value changed
                if (oldValue != newDisplayValue)
                {
                    await _auditService.LogCustomFieldChangeAsync(
                        patientId,
                        fieldDef.Label,
                        oldValue,
                        newDisplayValue,
                        userId,
                        ipAddress
                    );
                }
            }
        }

        private async Task SaveStringFieldAsync(Guid patientId, int fieldDefinitionId, string? value, bool hasValue)
        {
            var existing = await _context.PatientCustomFieldStrings
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            if (!hasValue && existing != null)
            {
                _context.PatientCustomFieldStrings.Remove(existing);
            }
            else if (hasValue)
            {
                if (existing == null)
                {
                    _context.PatientCustomFieldStrings.Add(new PatientCustomFieldString
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = value,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Value = value;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task SaveNumberFieldAsync(Guid patientId, int fieldDefinitionId, string? value, bool hasValue)
        {
            var existing = await _context.PatientCustomFieldNumbers
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            if (!hasValue && existing != null)
            {
                _context.PatientCustomFieldNumbers.Remove(existing);
            }
            else if (hasValue && decimal.TryParse(value, out decimal numValue))
            {
                if (existing == null)
                {
                    _context.PatientCustomFieldNumbers.Add(new PatientCustomFieldNumber
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = numValue,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Value = numValue;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task SaveDateFieldAsync(Guid patientId, int fieldDefinitionId, string? value, bool hasValue)
        {
            var existing = await _context.PatientCustomFieldDates
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            if (!hasValue && existing != null)
            {
                _context.PatientCustomFieldDates.Remove(existing);
            }
            else if (hasValue && DateTime.TryParse(value, out DateTime dateValue))
            {
                if (existing == null)
                {
                    _context.PatientCustomFieldDates.Add(new PatientCustomFieldDate
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = dateValue,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.Value = dateValue;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task SaveBooleanFieldAsync(Guid patientId, int fieldDefinitionId, string? value, bool hasValue)
        {
            var existing = await _context.PatientCustomFieldBooleans
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            bool boolValue = value == "true" || value == "on";

            if (existing == null)
            {
                _context.PatientCustomFieldBooleans.Add(new PatientCustomFieldBoolean
                {
                    PatientId = patientId,
                    FieldDefinitionId = fieldDefinitionId,
                    Value = boolValue,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.Value = boolValue;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task SaveLookupFieldAsync(Guid patientId, int fieldDefinitionId, string? value, bool hasValue)
        {
            var existing = await _context.PatientCustomFieldLookups
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            if (!hasValue && existing != null)
            {
                _context.PatientCustomFieldLookups.Remove(existing);
            }
            else if (hasValue && int.TryParse(value, out int lookupValueId))
            {
                if (existing == null)
                {
                    _context.PatientCustomFieldLookups.Add(new PatientCustomFieldLookup
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        LookupValueId = lookupValueId,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.LookupValueId = lookupValueId;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
