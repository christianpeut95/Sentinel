using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using System.Text;
using System.Text.Json;

namespace Sentinel.Services;

public class LineListService : ILineListService
{
    private readonly ApplicationDbContext _context;
    
    public LineListService(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<LineListField>> GetAvailableFieldsAsync(int outbreakId)
    {
        var fields = new List<LineListField>
        {
            // Patient Demographics
            new LineListField { FieldPath = "Patient.FriendlyId", DisplayName = "Patient ID", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.GivenName", DisplayName = "First Name", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.FamilyName", DisplayName = "Last Name", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.DateOfBirth", DisplayName = "Date of Birth", Category = "Patient", DataType = "date" },
            new LineListField { FieldPath = "Patient.Age", DisplayName = "Age (Years)", Category = "Patient", DataType = "number" },
            new LineListField { FieldPath = "Patient.SexAtBirth.Name", DisplayName = "Sex at Birth", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.Gender.Name", DisplayName = "Gender", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.AddressLine", DisplayName = "Address", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.City", DisplayName = "Suburb/City", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.State", DisplayName = "State", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.PostalCode", DisplayName = "Postcode", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.MobilePhone", DisplayName = "Mobile Phone", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.EmailAddress", DisplayName = "Email", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.CountryOfBirth.Name", DisplayName = "Country of Birth", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.Ethnicity.Name", DisplayName = "Ethnicity", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.AtsiStatus.Name", DisplayName = "ATSI Status", Category = "Patient", DataType = "string" },
            new LineListField { FieldPath = "Patient.Occupation.Name", DisplayName = "Occupation", Category = "Patient", DataType = "string" },
            
            // Case Core Fields
            new LineListField { FieldPath = "Case.FriendlyId", DisplayName = "Case ID", Category = "Case", DataType = "string" },
            new LineListField { FieldPath = "Case.Type", DisplayName = "Case Type", Category = "Case", DataType = "string" },
            new LineListField { FieldPath = "Case.DateOfOnset", DisplayName = "Date of Onset", Category = "Case", DataType = "date" },
            new LineListField { FieldPath = "Case.DateOfNotification", DisplayName = "Date of Notification", Category = "Case", DataType = "date" },
            new LineListField { FieldPath = "Case.Disease.Name", DisplayName = "Disease", Category = "Case", DataType = "string" },
            new LineListField { FieldPath = "Case.ConfirmationStatus.Name", DisplayName = "Confirmation Status", Category = "Case", DataType = "string" },
            new LineListField { FieldPath = "Case.CreatedAt", DisplayName = "Case Created Date", Category = "Case", DataType = "date" },
            
            // Outbreak-specific Case Fields
            new LineListField { FieldPath = "OutbreakCase.Classification", DisplayName = "Outbreak Classification", Category = "Outbreak", DataType = "string" },
            new LineListField { FieldPath = "OutbreakCase.ClassificationDate", DisplayName = "Classification Date", Category = "Outbreak", DataType = "date" },
            new LineListField { FieldPath = "OutbreakCase.ClassifiedBy", DisplayName = "Classified By", Category = "Outbreak", DataType = "string" },
            new LineListField { FieldPath = "OutbreakCase.LinkedDate", DisplayName = "Linked to Outbreak Date", Category = "Outbreak", DataType = "date" },
            new LineListField { FieldPath = "OutbreakCase.LinkMethod", DisplayName = "Link Method", Category = "Outbreak", DataType = "string" },
            
            // Exposure Event Fields (Primary Exposure)
            new LineListField { FieldPath = "Exposure.Type", DisplayName = "Exposure Type", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.StartDate", DisplayName = "Exposure Start Date", Category = "Exposure", DataType = "date" },
            new LineListField { FieldPath = "Exposure.EndDate", DisplayName = "Exposure End Date", Category = "Exposure", DataType = "date" },
            new LineListField { FieldPath = "Exposure.Location", DisplayName = "Exposure Location", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.Event", DisplayName = "Exposure Event", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.Status", DisplayName = "Exposure Status", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.City", DisplayName = "Exposure City", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.State", DisplayName = "Exposure State", Category = "Exposure", DataType = "string" },
            new LineListField { FieldPath = "Exposure.Country", DisplayName = "Exposure Country", Category = "Exposure", DataType = "string" },
            
            // Lab Result Fields (Latest Result)
            new LineListField { FieldPath = "Lab.SpecimenDate", DisplayName = "Specimen Collection Date", Category = "Laboratory", DataType = "date" },
            new LineListField { FieldPath = "Lab.SpecimenType", DisplayName = "Specimen Type", Category = "Laboratory", DataType = "string" },
            new LineListField { FieldPath = "Lab.TestType", DisplayName = "Test Type", Category = "Laboratory", DataType = "string" },
            new LineListField { FieldPath = "Lab.Result", DisplayName = "Test Result", Category = "Laboratory", DataType = "string" },
            new LineListField { FieldPath = "Lab.ResultDate", DisplayName = "Result Date", Category = "Laboratory", DataType = "date" },
            new LineListField { FieldPath = "Lab.Laboratory", DisplayName = "Laboratory", Category = "Laboratory", DataType = "string" },
            new LineListField { FieldPath = "Lab.AccessionNumber", DisplayName = "Accession Number", Category = "Laboratory", DataType = "string" },
        };
        
        // Add Custom Fields dynamically based on outbreak's disease
        var outbreak = await _context.Outbreaks
            .Include(o => o.PrimaryDisease)
            .FirstOrDefaultAsync(o => o.Id == outbreakId);
        
        if (outbreak?.PrimaryDiseaseId != null)
        {
            var customFieldDefs = await _context.DiseaseCustomFields
                .Where(dcf => dcf.DiseaseId == outbreak.PrimaryDiseaseId)
                .Include(dcf => dcf.CustomFieldDefinition)
                    .ThenInclude(cf => cf.LookupTable)
                .Select(dcf => dcf.CustomFieldDefinition)
                .Where(cf => cf.IsActive)
                .OrderBy(cf => cf.DisplayOrder)
                .ToListAsync();
            
            foreach (var fieldDef in customFieldDefs)
            {
                var dataType = fieldDef.FieldType switch
                {
                    CustomFieldType.Text => "string",
                    CustomFieldType.TextArea => "string",
                    CustomFieldType.Number => "number",
                    CustomFieldType.Date => "date",
                    CustomFieldType.Checkbox => "boolean",
                    CustomFieldType.Dropdown => "string",
                    CustomFieldType.Email => "string",
                    CustomFieldType.Phone => "string",
                    _ => "string"
                };
                
                fields.Add(new LineListField
                {
                    FieldPath = $"Case.CustomField.{fieldDef.Name}",
                    DisplayName = fieldDef.Label ?? fieldDef.Name,
                    Category = "Custom Fields",
                    DataType = dataType
                });
            }
        }
        
        return fields;
    }
    
    public async Task<List<LineListDataRow>> GetLineListDataAsync(int outbreakId, List<string> fieldPaths, string? sortConfig = null, string? filterConfig = null)
    {
        // Build base query with AsSplitQuery to avoid cartesian explosion with many includes
        // This is critical for proper data loading with complex navigation properties
        var query = _context.OutbreakCases
            .Where(oc => oc.OutbreakId == outbreakId && oc.IsActive)
            .Include(oc => oc.Outbreak)
                .ThenInclude(o => o.PrimaryDisease)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.SexAtBirth)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.Gender)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.CountryOfBirth)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.Ancestry)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.AtsiStatus)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Patient)
                    .ThenInclude(p => p.Occupation)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.Disease)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.ConfirmationStatus)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldStrings)
                    .ThenInclude(cf => cf.FieldDefinition)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldNumbers)
                    .ThenInclude(cf => cf.FieldDefinition)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldDates)
                    .ThenInclude(cf => cf.FieldDefinition)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldBooleans)
                    .ThenInclude(cf => cf.FieldDefinition)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldLookups)
                    .ThenInclude(cf => cf.FieldDefinition)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.CustomFieldLookups)
                    .ThenInclude(cf => cf.LookupValue)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.ExposureEvents)
                    .ThenInclude(e => e.Event)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.ExposureEvents)
                    .ThenInclude(e => e.Location)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.LabResults)
                    .ThenInclude(lr => lr.Laboratory)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.LabResults)
                    .ThenInclude(lr => lr.SpecimenType)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.LabResults)
                    .ThenInclude(lr => lr.TestType)
            .Include(oc => oc.Case)
                .ThenInclude(c => c.LabResults)
                    .ThenInclude(lr => lr.TestResult)
            .AsSplitQuery() // CRITICAL: Use split queries to properly load all navigation properties
            .IgnoreQueryFilters() // CRITICAL: Ignore global query filters (soft delete, IsActive, etc.)
            .AsNoTracking();
        
        // Apply sorting if provided (simplified - just use default sort for now)
        // TODO: Implement custom sorting logic if needed
        query = query.OrderBy(oc => oc.Case.DateOfOnset ?? oc.Case.DateOfNotification);
        
        var outbreakCases = await query.ToListAsync();
        
        // Debug logging to verify data is loaded
        if (outbreakCases.Any())
        {
            var firstCase = outbreakCases.First();
            var hasPatient = firstCase.Case?.Patient != null;
            var hasSexAtBirth = firstCase.Case?.Patient?.SexAtBirth != null;
            var hasDisease = firstCase.Case?.Disease != null;
            
            Console.WriteLine($"[LineList Debug] First case - Patient loaded: {hasPatient}, SexAtBirth loaded: {hasSexAtBirth}, Disease loaded: {hasDisease}");
            if (hasPatient && firstCase.Case?.Patient != null)
            {
                Console.WriteLine($"[LineList Debug] Patient ID: {firstCase.Case.Patient.Id}, SexAtBirthId: {firstCase.Case.Patient.SexAtBirthId}");
            }
            if (firstCase.Case != null)
            {
                Console.WriteLine($"[LineList Debug] Case DiseaseId: {firstCase.Case.DiseaseId}");
            }
        }
        
        // Convert to line list rows
        var rows = new List<LineListDataRow>();
        
        foreach (var oc in outbreakCases)
        {
            var row = new LineListDataRow
            {
                CaseId = oc.CaseId,
                OutbreakCaseId = oc.Id
            };
            
            foreach (var fieldPath in fieldPaths)
            {
                row.Values[fieldPath] = ExtractFieldValue(oc, fieldPath);
            }
            
            rows.Add(row);
        }
        
        return rows;
    }
    
    private object? ExtractFieldValue(OutbreakCase outbreakCase, string fieldPath)
    {
        try
        {
            var parts = fieldPath.Split('.');
            
            if (parts[0] == "Patient")
            {
                var patient = outbreakCase.Case?.Patient;
                if (patient == null)
                {
                    Console.WriteLine($"[LineList Debug] Patient is NULL for case {outbreakCase.CaseId}, fieldPath: {fieldPath}");
                    return null;
                }
                
                // Handle both simple (Patient.FriendlyId) and nested (Patient.SexAtBirth.Name) field paths
                object? result;
                
                if (parts.Length == 2)
                {
                    // Simple property: Patient.FriendlyId
                    result = parts[1] switch
                    {
                        "FriendlyId" => patient.FriendlyId,
                        "GivenName" => patient.GivenName,
                        "FamilyName" => patient.FamilyName,
                        "DateOfBirth" => patient.DateOfBirth?.ToString("yyyy-MM-dd"),
                        "Age" => patient.DateOfBirth.HasValue ? 
                            (DateTime.Now.Year - patient.DateOfBirth.Value.Year) : (int?)null,
                        "AddressLine" => patient.AddressLine,
                        "City" => patient.City,
                        "State" => patient.State,
                        "PostalCode" => patient.PostalCode,
                        "MobilePhone" => patient.MobilePhone,
                        "EmailAddress" => patient.EmailAddress,
                        _ => null
                    };
                }
                else if (parts.Length == 3)
                {
                    // Navigation property: Patient.SexAtBirth.Name
                    result = parts[1] switch
                    {
                        "SexAtBirth" => patient.SexAtBirth?.Name,
                        "Gender" => patient.Gender?.Name,
                        "CountryOfBirth" => patient.CountryOfBirth?.Name,
                        "Ancestry" => patient.Ancestry?.Name,
                        "AtsiStatus" => patient.AtsiStatus?.Name,
                        "Occupation" => patient.Occupation?.Name,
                        _ => null
                    };
                }
                else
                {
                    result = null;
                }
                
                return result;
            }
            else if (parts[0] == "Case")
            {
                var caseData = outbreakCase.Case;
                if (caseData == null)
                {
                    Console.WriteLine($"[LineList Debug] Case is NULL for outbreakCase {outbreakCase.Id}, fieldPath: {fieldPath}");
                    return null;
                }
                
                // Handle both simple (Case.FriendlyId) and nested (Case.Disease.Name) field paths
                object? result;
                
                if (parts.Length == 2)
                {
                    // Simple property: Case.FriendlyId
                    result = parts[1] switch
                    {
                        "FriendlyId" => caseData.FriendlyId,
                        "Type" => caseData.Type.ToString(),
                        "DateOfOnset" => caseData.DateOfOnset?.ToString("yyyy-MM-dd"),
                        "DateOfNotification" => caseData.DateOfNotification?.ToString("yyyy-MM-dd"),
                        _ => HandleCustomField(caseData, parts)
                    };
                }
                else if (parts.Length == 3)
                {
                    // Navigation property: Case.Disease.Name
                    result = parts[1] switch
                    {
                        "Disease" => caseData.Disease?.Name ?? outbreakCase.Outbreak?.PrimaryDisease?.Name,
                        "ConfirmationStatus" => caseData.ConfirmationStatus?.Name,
                        _ => null
                    };
                }
                else
                {
                    result = null;
                }
                
                // Debug disease field
                if (fieldPath == "Case.Disease.Name")
                {
                    Console.WriteLine($"[LineList Debug] Disease - CaseId: {caseData.Id}, DiseaseId: {caseData.DiseaseId}, Disease loaded: {caseData.Disease != null}, Value: {result}");
                }
                
                return result;
            }
            else if (parts[0] == "OutbreakCase")
            {
                return parts[1] switch
                {
                    "Classification" => outbreakCase.Classification?.ToString(),
                    "ClassificationDate" => outbreakCase.ClassificationDate?.ToString("yyyy-MM-dd"),
                    "ClassifiedBy" => outbreakCase.ClassifiedBy,
                    "LinkedDate" => outbreakCase.LinkedDate.ToString("yyyy-MM-dd"),
                    "LinkMethod" => outbreakCase.LinkMethod.ToString(),
                    _ => null
                };
            }
            else if (parts[0] == "Exposure")
            {
                // Handle exposure fields (aggregate or first)
                return HandleExposureField(outbreakCase.Case, parts);
            }
            else if (parts[0] == "Lab")
            {
                // Handle lab result fields (aggregate or latest)
                return HandleLabField(outbreakCase.Case, parts);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }
    
    private object? HandleCustomField(Case caseData, string[] parts)
    {
        if (parts.Length < 3 || parts[1] != "CustomField") return null;
        
        var fieldName = parts[2];
        
        // Try each custom field type
        var stringField = caseData.CustomFieldStrings?.FirstOrDefault(cf => cf.FieldDefinition?.Name == fieldName);
        if (stringField != null) return stringField.Value;
        
        var numberField = caseData.CustomFieldNumbers?.FirstOrDefault(cf => cf.FieldDefinition?.Name == fieldName);
        if (numberField != null) return numberField.Value;
        
        var dateField = caseData.CustomFieldDates?.FirstOrDefault(cf => cf.FieldDefinition?.Name == fieldName);
        if (dateField != null) return dateField.Value?.ToString("yyyy-MM-dd");
        
        var boolField = caseData.CustomFieldBooleans?.FirstOrDefault(cf => cf.FieldDefinition?.Name == fieldName);
        if (boolField != null) return boolField.Value;
        
        var lookupField = caseData.CustomFieldLookups?.FirstOrDefault(cf => cf.FieldDefinition?.Name == fieldName);
        if (lookupField != null) return lookupField.LookupValue?.Value;
        
        return null;
    }
    
    private object? HandleExposureField(Case? caseData, string[] parts)
    {
        if (caseData?.ExposureEvents == null || !caseData.ExposureEvents.Any())
        {
            Console.WriteLine($"[LineList Debug] Exposure - CaseId: {caseData?.Id}, ExposureEvents: {caseData?.ExposureEvents?.Count() ?? 0}");
            return null;
        }
        
        var primaryExposure = caseData.ExposureEvents
            .OrderByDescending(e => e.IsReportingExposure)
            .ThenBy(e => e.ExposureStartDate)
            .FirstOrDefault();
        
        if (primaryExposure == null)
        {
            Console.WriteLine($"[LineList Debug] Exposure - CaseId: {caseData?.Id}, Primary exposure is null");
            return null;
        }
        
        var result = parts.Length > 1 ? parts[1] switch
        {
            "Type" => primaryExposure.ExposureType.ToString(),
            "StartDate" => primaryExposure.ExposureStartDate.ToString("yyyy-MM-dd"),
            "EndDate" => primaryExposure.ExposureEndDate?.ToString("yyyy-MM-dd"),
            "Location" => primaryExposure.Location?.Name ?? primaryExposure.FreeTextLocation,
            "Event" => primaryExposure.Event?.Name,
            "Status" => primaryExposure.ExposureStatus.ToString(),
            "City" => primaryExposure.City,
            "State" => primaryExposure.State,
            "Country" => primaryExposure.Country,
            _ => null
        } : null;
        
        Console.WriteLine($"[LineList Debug] Exposure - CaseId: {caseData?.Id}, Field: {parts[1]}, Value: {result}");
        return result;
    }
    
    private object? HandleLabField(Case? caseData, string[] parts)
    {
        if (caseData?.LabResults == null || !caseData.LabResults.Any())
        {
            Console.WriteLine($"[LineList Debug] Lab - CaseId: {caseData?.Id}, LabResults: {caseData?.LabResults?.Count() ?? 0}, Field: {(parts.Length > 1 ? parts[1] : "unknown")}");
            return null;
        }
        
        var latestLab = caseData.LabResults
            .OrderByDescending(lr => lr.ResultDate ?? lr.SpecimenCollectionDate)
            .FirstOrDefault();
        
        if (latestLab == null)
        {
            Console.WriteLine($"[LineList Debug] Lab - CaseId: {caseData?.Id}, Latest lab is null");
            return null;
        }
        
        var result = parts.Length > 1 ? parts[1] switch
        {
            "SpecimenDate" => latestLab.SpecimenCollectionDate?.ToString("yyyy-MM-dd"),
            "SpecimenType" => latestLab.SpecimenType?.Name,
            "TestType" => latestLab.TestType?.Name,
            "Result" => latestLab.TestResult?.Name,
            "ResultDate" => latestLab.ResultDate?.ToString("yyyy-MM-dd"),
            "Laboratory" => latestLab.Laboratory?.Name,
            "AccessionNumber" => latestLab.AccessionNumber,
            _ => null
        } : null;
        
        Console.WriteLine($"[LineList Debug] Lab - CaseId: {caseData?.Id}, Field: {parts[1]}, LabId: {latestLab.Id}, Value: {result}");
        return result;
    }
    
    private string MapFieldPath(string fieldPath)
    {
        // Map field paths to LINQ expressions
        return fieldPath.Replace(".", "?.");
    }
    
    public async Task<OutbreakLineListConfiguration> SaveConfigurationAsync(OutbreakLineListConfiguration config)
    {
        config.ModifiedAt = DateTime.UtcNow;
        
        if (config.Id == 0)
        {
            config.CreatedAt = DateTime.UtcNow;
            _context.OutbreakLineListConfigurations.Add(config);
        }
        else
        {
            _context.OutbreakLineListConfigurations.Update(config);
        }
        
        await _context.SaveChangesAsync();
        return config;
    }
    
    public async Task<List<OutbreakLineListConfiguration>> GetUserConfigurationsAsync(int outbreakId, string userId)
    {
        return await _context.OutbreakLineListConfigurations
            .Where(c => c.OutbreakId == outbreakId && c.UserId == userId)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }
    
    public async Task<List<OutbreakLineListConfiguration>> GetSharedConfigurationsAsync(int outbreakId)
    {
        return await _context.OutbreakLineListConfigurations
            .Where(c => c.OutbreakId == outbreakId && c.IsShared)
            .OrderBy(c => c.Name)
            .Include(c => c.CreatedByUser)
            .ToListAsync();
    }
    
    public async Task<bool> DeleteConfigurationAsync(int configId, string userId)
    {
        var config = await _context.OutbreakLineListConfigurations
            .FirstOrDefaultAsync(c => c.Id == configId && (c.UserId == userId || c.IsShared));
        
        if (config == null) return false;
        
        _context.OutbreakLineListConfigurations.Remove(config);
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<bool> SetDefaultConfigurationAsync(int configId, string userId)
    {
        var config = await _context.OutbreakLineListConfigurations
            .FirstOrDefaultAsync(c => c.Id == configId && c.UserId == userId);
        
        if (config == null) return false;
        
        // Clear other defaults for this user/outbreak
        var otherDefaults = await _context.OutbreakLineListConfigurations
            .Where(c => c.OutbreakId == config.OutbreakId && c.UserId == userId && c.Id != configId)
            .ToListAsync();
        
        foreach (var other in otherDefaults)
        {
            other.IsDefault = false;
        }
        
        config.IsDefault = true;
        await _context.SaveChangesAsync();
        return true;
    }
    
    public async Task<byte[]> ExportToCsvAsync(int outbreakId, List<string> fieldPaths, string? sortConfig = null)
    {
        var data = await GetLineListDataAsync(outbreakId, fieldPaths, sortConfig);
        var availableFields = await GetAvailableFieldsAsync(outbreakId);
        
        var csv = new StringBuilder();
        
        // Header row
        var headers = fieldPaths.Select(fp => 
            availableFields.FirstOrDefault(f => f.FieldPath == fp)?.DisplayName ?? fp);
        csv.AppendLine(string.Join(",", headers.Select(CsvEscape)));
        
        // Data rows
        foreach (var row in data)
        {
            var values = fieldPaths.Select(fp => row.Values.GetValueOrDefault(fp)?.ToString() ?? "");
            csv.AppendLine(string.Join(",", values.Select(CsvEscape)));
        }
        
        return Encoding.UTF8.GetBytes(csv.ToString());
    }
    
    private string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
