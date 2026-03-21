using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services
{
    public class PatientMergeService : IPatientMergeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IPatientCustomFieldService _customFieldService;

        public PatientMergeService(
            ApplicationDbContext context,
            IAuditService auditService,
            IPatientCustomFieldService customFieldService)
        {
            _context = context;
            _auditService = auditService;
            _customFieldService = customFieldService;
        }

        public async Task<PatientMergeComparison> GetMergeComparisonAsync(Guid sourcePatientId, Guid targetPatientId)
        {
            var sourcePatient = await _context.Patients
                .Include(p => p.SexAtBirth)
                .Include(p => p.Gender)
                .Include(p => p.CountryOfBirth)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Ancestry)
                .Include(p => p.AtsiStatus)
                .Include(p => p.Occupation)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == sourcePatientId);

            var targetPatient = await _context.Patients
                .Include(p => p.SexAtBirth)
                .Include(p => p.Gender)
                .Include(p => p.CountryOfBirth)
                .Include(p => p.LanguageSpokenAtHome)
                .Include(p => p.Ancestry)
                .Include(p => p.AtsiStatus)
                .Include(p => p.Occupation)
                .Include(p => p.CreatedByUser)
                .FirstOrDefaultAsync(p => p.Id == targetPatientId);

            if (sourcePatient == null || targetPatient == null)
            {
                throw new InvalidOperationException("One or both patients not found");
            }

            var comparison = new PatientMergeComparison
            {
                SourcePatient = sourcePatient,
                TargetPatient = targetPatient,
                SourceCustomFields = await GetCustomFieldValuesAsync(sourcePatientId),
                TargetCustomFields = await GetCustomFieldValuesAsync(targetPatientId),
                SourceAuditLogCount = await _auditService.GetAuditLogCountAsync("Patient", sourcePatientId.ToString()),
                TargetAuditLogCount = await _auditService.GetAuditLogCountAsync("Patient", targetPatientId.ToString())
            };

            return comparison;
        }

        public async Task<bool> ValidateMergeAsync(Guid sourcePatientId, Guid targetPatientId)
        {
            if (sourcePatientId == targetPatientId)
                return false;

            var sourceExists = await _context.Patients.AnyAsync(p => p.Id == sourcePatientId);
            var targetExists = await _context.Patients.AnyAsync(p => p.Id == targetPatientId);

            return sourceExists && targetExists;
        }

        public async Task<MergeResult> MergePatientsAsync(
            Guid sourcePatientId,
            Guid targetPatientId,
            PatientMergeSelection selection,
            string? userId,
            string? ipAddress)
        {
            var result = new MergeResult();

            try
            {
                var sourcePatient = await _context.Patients.FindAsync(sourcePatientId);
                var targetPatient = await _context.Patients.FindAsync(targetPatientId);

                if (sourcePatient == null || targetPatient == null)
                {
                    result.ErrorMessage = "One or both patients not found";
                    return result;
                }

                // Use execution strategy to handle retries with transactions
                var strategy = _context.Database.CreateExecutionStrategy();
                
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // Update target patient with selected values
                        await ApplySelectedValuesAsync(targetPatient, selection, sourcePatient, userId, ipAddress);

                        // Merge custom fields
                        await MergeCustomFieldsAsync(sourcePatientId, targetPatientId, selection, userId, ipAddress);

                        // Reassign audit logs from source to target
                        await ReassignAuditLogsAsync(sourcePatientId, targetPatientId);

                        // Reassign all FK references before deleting source patient
                        await ReassignRelatedEntitiesAsync(sourcePatientId, targetPatientId);

                        // Mark source patient as deleted
                        _context.Patients.Remove(sourcePatient);
                        await _context.SaveChangesAsync();

                        // Log the merge operation
                        await _auditService.LogChangeAsync(
                            "Patient",
                            targetPatientId.ToString(),
                            "Merge",
                            $"Patient {sourcePatientId}",
                            $"Merged into Patient {targetPatientId}",
                            userId,
                            ipAddress);

                        await transaction.CommitAsync();

                        result.Success = true;
                        result.MergedPatientId = targetPatientId;
                        result.DeletedPatientId = sourcePatientId;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        result.ErrorMessage = $"Merge failed: {ex.Message}";
                        throw; // Re-throw to allow strategy to retry if needed
                    }
                });
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Transaction failed: {ex.Message}";
            }

            return result;
        }

        private async Task<Dictionary<string, PatientCustomFieldValue>> GetCustomFieldValuesAsync(Guid patientId)
        {
            var result = new Dictionary<string, PatientCustomFieldValue>();

            var definitions = await _context.CustomFieldDefinitions
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayOrder)
                .ToListAsync();

            foreach (var definition in definitions)
            {
                var value = new PatientCustomFieldValue
                {
                    FieldDefinitionId = definition.Id,
                    Label = definition.Label,
                    FieldType = definition.FieldType.ToString()
                };

                switch (definition.FieldType)
                {
                    case CustomFieldType.Text:
                    case CustomFieldType.TextArea:
                    case CustomFieldType.Email:
                    case CustomFieldType.Phone:
                        var stringValue = await _context.PatientCustomFieldStrings
                            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == definition.Id);
                        value.DisplayValue = stringValue?.Value;
                        value.RawValue = stringValue?.Value;
                        break;

                    case CustomFieldType.Number:
                        var numberValue = await _context.PatientCustomFieldNumbers
                            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == definition.Id);
                        value.DisplayValue = numberValue?.Value?.ToString();
                        value.RawValue = numberValue?.Value;
                        break;

                    case CustomFieldType.Date:
                        var dateValue = await _context.PatientCustomFieldDates
                            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == definition.Id);
                        value.DisplayValue = dateValue?.Value?.ToString("yyyy-MM-dd");
                        value.RawValue = dateValue?.Value;
                        break;

                    case CustomFieldType.Checkbox:
                        var boolValue = await _context.PatientCustomFieldBooleans
                            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == definition.Id);
                        value.DisplayValue = boolValue?.Value.ToString();
                        value.RawValue = boolValue?.Value;
                        break;

                    case CustomFieldType.Dropdown:
                        var lookupValue = await _context.PatientCustomFieldLookups
                            .Include(f => f.LookupValue)
                            .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == definition.Id);
                        value.DisplayValue = lookupValue?.LookupValue?.Value;
                        value.RawValue = lookupValue?.LookupValueId;
                        break;
                }

                result[definition.Id.ToString()] = value;
            }

            return result;
        }

        private async Task ApplySelectedValuesAsync(
            Patient targetPatient,
            PatientMergeSelection selection,
            Patient sourcePatient,
            string? userId,
            string? ipAddress)
        {
            foreach (var kvp in selection.SelectedValues)
            {
                var propertyName = kvp.Key;
                var selectedValue = kvp.Value;

                var property = typeof(Patient).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    var oldValue = property.GetValue(targetPatient);
                    var oldValueStr = oldValue?.ToString();
                    var newValueStr = selectedValue?.ToString();

                    if (oldValueStr != newValueStr)
                    {
                        property.SetValue(targetPatient, selectedValue);

                        await _auditService.LogChangeAsync(
                            "Patient",
                            targetPatient.Id.ToString(),
                            propertyName,
                            oldValueStr,
                            newValueStr,
                            userId,
                            ipAddress);
                    }
                }
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();
        }

        private async Task MergeCustomFieldsAsync(
            Guid sourcePatientId,
            Guid targetPatientId,
            PatientMergeSelection selection,
            string? userId,
            string? ipAddress)
        {
            foreach (var kvp in selection.SelectedCustomFieldValues)
            {
                var fieldDefinitionId = kvp.Key;
                var selectedValue = kvp.Value;

                var definition = await _context.CustomFieldDefinitions
                    .FirstOrDefaultAsync(d => d.Id == fieldDefinitionId);

                if (definition == null) continue;

                switch (definition.FieldType)
                {
                    case CustomFieldType.Text:
                    case CustomFieldType.TextArea:
                    case CustomFieldType.Email:
                    case CustomFieldType.Phone:
                        await MergeCustomFieldStringAsync(targetPatientId, fieldDefinitionId, selectedValue as string, definition.Label, userId, ipAddress);
                        break;

                    case CustomFieldType.Number:
                        await MergeCustomFieldNumberAsync(targetPatientId, fieldDefinitionId, selectedValue as decimal?, definition.Label, userId, ipAddress);
                        break;

                    case CustomFieldType.Date:
                        await MergeCustomFieldDateAsync(targetPatientId, fieldDefinitionId, selectedValue as DateTime?, definition.Label, userId, ipAddress);
                        break;

                    case CustomFieldType.Checkbox:
                        await MergeCustomFieldBooleanAsync(targetPatientId, fieldDefinitionId, selectedValue as bool?, definition.Label, userId, ipAddress);
                        break;

                    case CustomFieldType.Dropdown:
                        await MergeCustomFieldLookupAsync(targetPatientId, fieldDefinitionId, selectedValue as int?, definition.Label, userId, ipAddress);
                        break;
                }
            }
        }

        private async Task MergeCustomFieldStringAsync(Guid patientId, int fieldDefinitionId, string? value, string label, string? userId, string? ipAddress)
        {
            var existing = await _context.PatientCustomFieldStrings
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            var oldValue = existing?.Value;

            if (value != null)
            {
                if (existing != null)
                {
                    existing.Value = value;
                }
                else
                {
                    _context.PatientCustomFieldStrings.Add(new PatientCustomFieldString
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = value
                    });
                }
            }
            else if (existing != null)
            {
                _context.PatientCustomFieldStrings.Remove(existing);
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();

            if (oldValue != value)
            {
                await _auditService.LogCustomFieldChangeAsync(patientId, label, oldValue, value, userId, ipAddress);
            }
        }

        private async Task MergeCustomFieldNumberAsync(Guid patientId, int fieldDefinitionId, decimal? value, string label, string? userId, string? ipAddress)
        {
            var existing = await _context.PatientCustomFieldNumbers
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            var oldValue = existing?.Value?.ToString();

            if (value.HasValue)
            {
                if (existing != null)
                {
                    existing.Value = value.Value;
                }
                else
                {
                    _context.PatientCustomFieldNumbers.Add(new PatientCustomFieldNumber
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = value.Value
                    });
                }
            }
            else if (existing != null)
            {
                _context.PatientCustomFieldNumbers.Remove(existing);
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();

            if (oldValue != value?.ToString())
            {
                await _auditService.LogCustomFieldChangeAsync(patientId, label, oldValue, value?.ToString(), userId, ipAddress);
            }
        }

        private async Task MergeCustomFieldDateAsync(Guid patientId, int fieldDefinitionId, DateTime? value, string label, string? userId, string? ipAddress)
        {
            var existing = await _context.PatientCustomFieldDates
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            var oldValue = existing?.Value?.ToString("yyyy-MM-dd");

            if (value.HasValue)
            {
                if (existing != null)
                {
                    existing.Value = value.Value;
                }
                else
                {
                    _context.PatientCustomFieldDates.Add(new PatientCustomFieldDate
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = value.Value
                    });
                }
            }
            else if (existing != null)
            {
                _context.PatientCustomFieldDates.Remove(existing);
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();

            if (oldValue != value?.ToString("yyyy-MM-dd"))
            {
                await _auditService.LogCustomFieldChangeAsync(patientId, label, oldValue, value?.ToString("yyyy-MM-dd"), userId, ipAddress);
            }
        }

        private async Task MergeCustomFieldBooleanAsync(Guid patientId, int fieldDefinitionId, bool? value, string label, string? userId, string? ipAddress)
        {
            var existing = await _context.PatientCustomFieldBooleans
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            var oldValue = existing?.Value.ToString();

            if (value.HasValue)
            {
                if (existing != null)
                {
                    existing.Value = value.Value;
                }
                else
                {
                    _context.PatientCustomFieldBooleans.Add(new PatientCustomFieldBoolean
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        Value = value.Value
                    });
                }
            }
            else if (existing != null)
            {
                _context.PatientCustomFieldBooleans.Remove(existing);
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();

            if (oldValue != value?.ToString())
            {
                await _auditService.LogCustomFieldChangeAsync(patientId, label, oldValue, value?.ToString(), userId, ipAddress);
            }
        }

        private async Task MergeCustomFieldLookupAsync(Guid patientId, int fieldDefinitionId, int? value, string label, string? userId, string? ipAddress)
        {
            var existing = await _context.PatientCustomFieldLookups
                .Include(f => f.LookupValue)
                .FirstOrDefaultAsync(f => f.PatientId == patientId && f.FieldDefinitionId == fieldDefinitionId);

            var oldValue = existing?.LookupValue?.Value;

            string? newValue = null;
            if (value.HasValue)
            {
                var lookupValue = await _context.LookupValues.FindAsync(value.Value);
                newValue = lookupValue?.Value;
            }

            if (value.HasValue)
            {
                if (existing != null)
                {
                    existing.LookupValueId = value.Value;
                }
                else
                {
                    _context.PatientCustomFieldLookups.Add(new PatientCustomFieldLookup
                    {
                        PatientId = patientId,
                        FieldDefinitionId = fieldDefinitionId,
                        LookupValueId = value.Value
                    });
                }
            }
            else if (existing != null)
            {
                _context.PatientCustomFieldLookups.Remove(existing);
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();

            if (oldValue != newValue)
            {
                await _auditService.LogCustomFieldChangeAsync(patientId, label, oldValue, newValue, userId, ipAddress);
            }
        }

        private async Task ReassignRelatedEntitiesAsync(Guid sourcePatientId, Guid targetPatientId)
        {
            // Reassign Cases - CRITICAL: Case.PatientId is non-nullable, cascade delete would
            // permanently destroy all case history for the source patient
            var cases = await _context.Cases
                .Where(c => c.PatientId == sourcePatientId)
                .ToListAsync();
            foreach (var c in cases)
                c.PatientId = targetPatientId;

            // Reassign Notes (Note.PatientId has Restrict delete - would FK-fail otherwise)
            var notes = await _context.Notes
                .Where(n => n.PatientId == sourcePatientId)
                .ToListAsync();
            foreach (var n in notes)
                n.PatientId = targetPatientId;

            // Reassign ReviewQueue items
            var reviewItems = await _context.ReviewQueue
                .Where(r => r.PatientId == sourcePatientId)
                .ToListAsync();
            foreach (var r in reviewItems)
                r.PatientId = targetPatientId;

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();
        }

        private async Task ReassignAuditLogsAsync(Guid sourcePatientId, Guid targetPatientId)
        {
            var auditLogs = await _context.AuditLogs
                .Where(a => a.EntityType == "Patient" && a.EntityId == sourcePatientId.ToString())
                .ToListAsync();

            foreach (var log in auditLogs)
            {
                log.EntityId = targetPatientId.ToString();
                log.FieldName = $"[From Patient {sourcePatientId}] {log.FieldName}";
            }

            // Don't save yet - will be saved at the end of the transaction
            // await _context.SaveChangesAsync();
        }
    }
}
