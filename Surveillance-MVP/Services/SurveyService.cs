using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models;

namespace Surveillance_MVP.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SurveyService> _logger;

        public SurveyService(ApplicationDbContext context, ILogger<SurveyService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SurveyDefinitionWithData> GetSurveyForTaskAsync(Guid taskId)
        {
            var task = await _context.CaseTasks
                .Include(t => t.TaskTemplate)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                .Include(t => t.Case)
                    .ThenInclude(c => c.Disease)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new ArgumentException($"Task {taskId} not found");

            string? surveyJson = null;
            string? defaultInputMappings = null;
            string? defaultOutputMappings = null;

            // 1. Check if TaskTemplate uses Survey Library
            if (task.TaskTemplate?.SurveyTemplateId != null)
            {
                // First, get the survey template the task originally pointed to (may be archived)
                var originalTemplate = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId);
                
                SurveyTemplate? surveyTemplate = null;
                
                if (originalTemplate != null)
                {
                    // Determine the root parent of this survey family
                    var rootParentId = originalTemplate.ParentSurveyTemplateId ?? originalTemplate.Id;
                    
                    // Always use the ACTIVE version from this survey family
                    surveyTemplate = await _context.SurveyTemplates
                        .AsNoTracking()
                        .Where(st => (st.Id == rootParentId || st.ParentSurveyTemplateId == rootParentId))
                        .Where(st => st.VersionStatus == SurveyVersionStatus.Active)
                        .FirstOrDefaultAsync();
                    
                    if (surveyTemplate == null)
                    {
                        // Fallback: if no active version, use the original template even if archived
                        _logger.LogWarning("No active version found for survey family {RootParentId}, using original template {OriginalId}", 
                            rootParentId, originalTemplate.Id);
                        surveyTemplate = originalTemplate;
                    }
                    else if (surveyTemplate.Id != originalTemplate.Id)
                    {
                        _logger.LogInformation("Task {TaskId} originally linked to version {OriginalVersion}, now using active version {ActiveVersion}", 
                            taskId, originalTemplate.VersionNumber, surveyTemplate.VersionNumber);
                    }
                }
                
                if (surveyTemplate != null)
                {
                    surveyJson = surveyTemplate.SurveyDefinitionJson;
                    defaultInputMappings = surveyTemplate.DefaultInputMappingJson;
                    defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;

                    _logger.LogInformation("Using Survey Library template {TemplateId} (Version {VersionNumber}) for Task {TaskId}", 
                        surveyTemplate.Id, surveyTemplate.VersionNumber, taskId);

                    // Update usage tracking on the version being used
                    var templateToUpdate = await _context.SurveyTemplates.FindAsync(surveyTemplate.Id);
                    if (templateToUpdate != null)
                    {
                        templateToUpdate.UsageCount++;
                        templateToUpdate.LastUsedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            
            // 2. Fall back to embedded survey (backwards compatible)
            if (surveyJson == null && !string.IsNullOrEmpty(task.TaskTemplate?.SurveyDefinitionJson))
            {
                surveyJson = task.TaskTemplate.SurveyDefinitionJson;
                defaultInputMappings = task.TaskTemplate.DefaultInputMappingJson;
                defaultOutputMappings = task.TaskTemplate.DefaultOutputMappingJson;

                _logger.LogInformation("Using embedded survey from TaskTemplate {TaskTemplateId} for Task {TaskId}", 
                    task.TaskTemplateId, taskId);
            }

            var result = new SurveyDefinitionWithData
            {
                HasSurvey = !string.IsNullOrEmpty(surveyJson)
            };

            if (!result.HasSurvey)
                return result;

            result.SurveyDefinitionJson = surveyJson!;

            // 3. Get disease task template for field mappings (disease-specific overrides)
            var diseaseTaskTemplate = await _context.DiseaseTaskTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => 
                    dt.DiseaseId == task.Case.DiseaseId && 
                    dt.TaskTemplateId == task.TaskTemplateId);

            // Determine which input mappings to use: disease-specific or default
            string? inputMappingJson = diseaseTaskTemplate?.InputMappingJson;
            if (string.IsNullOrWhiteSpace(inputMappingJson))
            {
                // Fall back to defaults (from library or embedded)
                inputMappingJson = defaultInputMappings;
                if (!string.IsNullOrWhiteSpace(inputMappingJson))
                {
                    _logger.LogInformation("Using default input mappings for Task {TaskId}", taskId);
                }
            }

            if (inputMappingJson != null)
            {
                // Build context
                var context = await BuildSurveyDataContextAsync(task);

                // Parse input mappings
                var inputMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    inputMappingJson) ?? new();

                // Pre-populate data
                foreach (var mapping in inputMappings)
                {
                    var surveyFieldName = mapping.Key;
                    var sourceFieldPath = mapping.Value;

                    try
                    {
                        var value = ResolveFieldPath(sourceFieldPath, context);
                        if (value != null)
                        {
                            result.PrePopulatedData[surveyFieldName] = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, 
                            "Failed to resolve field path '{FieldPath}' for survey field '{SurveyField}'",
                            sourceFieldPath, surveyFieldName);
                    }
                }
            }

            return result;
        }

        public async Task SaveSurveyResponseAsync(Guid taskId, Dictionary<string, object> responses)
        {
            var task = await _context.CaseTasks
                .Include(t => t.Case)
                    .ThenInclude(c => c.Patient)
                .Include(t => t.TaskTemplate)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                throw new ArgumentException($"Task {taskId} not found");

            // Save response JSON
            task.SurveyResponseJson = JsonSerializer.Serialize(responses);

            string? defaultOutputMappings = null;

            // 1. Check if TaskTemplate uses Survey Library
            if (task.TaskTemplate?.SurveyTemplateId != null)
            {
                var surveyTemplate = await _context.SurveyTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(st => st.Id == task.TaskTemplate.SurveyTemplateId);
                
                if (surveyTemplate != null)
                {
                    defaultOutputMappings = surveyTemplate.DefaultOutputMappingJson;
                    _logger.LogInformation("Using Survey Library output mappings for Task {TaskId}", taskId);
                }
            }
            
            // 2. Fall back to embedded mappings (backwards compatible)
            if (defaultOutputMappings == null)
            {
                defaultOutputMappings = task.TaskTemplate?.DefaultOutputMappingJson;
            }

            // 3. Get disease task template for output mappings (disease-specific overrides)
            var diseaseTaskTemplate = await _context.DiseaseTaskTemplates
                .Include(dt => dt.TaskTemplate)
                .AsNoTracking()
                .FirstOrDefaultAsync(dt => 
                    dt.DiseaseId == task.Case.DiseaseId && 
                    dt.TaskTemplateId == task.TaskTemplateId);

            // Determine which output mappings to use: disease-specific or default
            string? outputMappingJson = diseaseTaskTemplate?.OutputMappingJson;
            if (string.IsNullOrWhiteSpace(outputMappingJson))
            {
                // Fall back to defaults (from library or embedded)
                outputMappingJson = defaultOutputMappings;
                if (!string.IsNullOrWhiteSpace(outputMappingJson))
                {
                    _logger.LogInformation("Using default output mappings for Task {TaskId}", taskId);
                }
            }

            if (outputMappingJson != null)
            {
                var context = await BuildSurveyDataContextAsync(task);

                // Parse output mappings
                var outputMappings = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    outputMappingJson) ?? new();

                // Apply mappings
                foreach (var mapping in outputMappings)
                {
                    var surveyFieldName = mapping.Key;
                    var targetFieldPath = mapping.Value;

                    if (responses.TryGetValue(surveyFieldName, out var value))
                    {
                        try
                        {
                            await SetFieldValueAsync(targetFieldPath, value, context);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, 
                                "Failed to set field path '{FieldPath}' from survey field '{SurveyField}'",
                                targetFieldPath, surveyFieldName);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        public bool ValidateSurveyDefinition(string surveyJson)
        {
            if (string.IsNullOrWhiteSpace(surveyJson))
                return false;

            try
            {
                using var document = JsonDocument.Parse(surveyJson);
                var root = document.RootElement;

                // Basic SurveyJS validation - must have pages or elements
                return root.TryGetProperty("pages", out _) || 
                       root.TryGetProperty("elements", out _);
            }
            catch (JsonException)
            {
                return false;
            }
        }

        public async Task<Dictionary<string, object>?> GetSurveyResponseAsync(Guid taskId)
        {
            var task = await _context.CaseTasks
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task?.SurveyResponseJson == null)
                return null;

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(task.SurveyResponseJson);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse survey response for task {TaskId}", taskId);
                return null;
            }
        }

        public object? ResolveFieldPath(string fieldPath, SurveyDataContext context)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                return null;

            var parts = fieldPath.Split('.');
            if (parts.Length < 2)
                return null;

            var rootObject = parts[0];
            var propertyPath = parts.Skip(1).ToArray();

            object? current = rootObject.ToLower() switch
            {
                "patient" => context.Patient,
                "case" => context.Case,
                "task" => context.Task,
                "exposures" => context.Exposures,
                "labresults" => context.LabResults,
                "customfields" => context.CustomFields,
                _ => null
            };

            if (current == null)
                return null;

            // Navigate property path
            foreach (var prop in propertyPath)
            {
                if (current is Dictionary<string, object> dict)
                {
                    if (!dict.TryGetValue(prop, out current))
                        return null;
                }
                else
                {
                    var property = current.GetType().GetProperty(prop);
                    if (property == null)
                        return null;
                    current = property.GetValue(current);
                }

                if (current == null)
                    return null;
            }

            return current;
        }

        public async Task SetFieldValueAsync(string fieldPath, object? value, SurveyDataContext context)
        {
            if (string.IsNullOrWhiteSpace(fieldPath))
                return;

            var parts = fieldPath.Split('.');
            if (parts.Length < 2)
                return;

            var rootObject = parts[0].ToLower();
            var propertyPath = parts.Skip(1).ToArray();

            // Navigate to the parent object
            object? current = rootObject switch
            {
                "patient" => context.Patient,
                "case" => context.Case,
                _ => null
            };

            if (current == null)
            {
                _logger.LogWarning("Cannot set field on unknown root object: {RootObject}", rootObject);
                return;
            }

            // Navigate to parent
            for (int i = 0; i < propertyPath.Length - 1; i++)
            {
                var property = current.GetType().GetProperty(propertyPath[i]);
                if (property == null)
                {
                    _logger.LogWarning("Property {Property} not found on {Type}", 
                        propertyPath[i], current.GetType().Name);
                    return;
                }
                current = property.GetValue(current);
                if (current == null)
                    return;
            }

            // Set the final property
            var targetProperty = current.GetType().GetProperty(propertyPath.Last());
            if (targetProperty == null || !targetProperty.CanWrite)
            {
                _logger.LogWarning("Property {Property} not found or not writable on {Type}", 
                    propertyPath.Last(), current.GetType().Name);
                return;
            }

            // Convert value to target type
            try
            {
                var convertedValue = ConvertValue(value, targetProperty.PropertyType);
                targetProperty.SetValue(current, convertedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert and set value for {FieldPath}", fieldPath);
            }
        }

        private async Task<SurveyDataContext> BuildSurveyDataContextAsync(CaseTask task)
        {
            // Load exposures
            var exposures = await _context.ExposureEvents
                .Where(e => e.CaseId == task.CaseId)
                .Include(e => e.Location)
                .Include(e => e.Event)
                .ToListAsync();

            // Load lab results
            var labResults = await _context.LabResults
                .Where(l => l.CaseId == task.CaseId)
                .Include(l => l.TestType)
                .Include(l => l.TestResult)
                .ToListAsync();

            return new SurveyDataContext
            {
                Task = task,
                Case = task.Case,
                Patient = task.Case.Patient,
                Exposures = exposures,
                LabResults = labResults
            };
        }

        private object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
                return null;

            // Handle JsonElement from deserialization
            if (value is JsonElement jsonElement)
            {
                value = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.TryGetInt32(out var i) ? i : jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => jsonElement.ToString()
                };
            }

            if (value == null)
                return null;

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType == typeof(string))
                return value.ToString();

            if (underlyingType == typeof(DateTime))
            {
                if (value is string dateStr && DateTime.TryParse(dateStr, out var date))
                    return date;
                if (value is DateTime dt)
                    return dt;
            }

            if (underlyingType == typeof(int))
            {
                if (value is int i)
                    return i;
                if (value is string intStr && int.TryParse(intStr, out var intVal))
                    return intVal;
                if (value is double d)
                    return (int)d;
            }

            if (underlyingType == typeof(bool))
            {
                if (value is bool b)
                    return b;
                if (value is string boolStr && bool.TryParse(boolStr, out var boolVal))
                    return boolVal;
            }

            if (underlyingType == typeof(decimal))
            {
                if (value is decimal dec)
                    return dec;
                if (value is string decStr && decimal.TryParse(decStr, out var decVal))
                    return decVal;
                if (value is double dbl)
                    return (decimal)dbl;
            }

            if (underlyingType == typeof(Guid))
            {
                if (value is Guid guid)
                    return guid;
                if (value is string guidStr && Guid.TryParse(guidStr, out var guidVal))
                    return guidVal;
            }

            return Convert.ChangeType(value, underlyingType);
        }
    }
}
