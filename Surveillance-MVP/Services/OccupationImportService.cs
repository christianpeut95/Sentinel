using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using Surveillance_MVP.Data;
using Surveillance_MVP.Models.Lookups;

namespace Surveillance_MVP.Services
{
    public class OccupationImportService : IOccupationImportService
    {
        private readonly ApplicationDbContext _context;

        public OccupationImportService(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<ImportResult> ImportFromExcelAsync(Stream fileStream)
        {
            var result = new ImportResult { Success = true };

            try
            {
                using var package = new ExcelPackage(fileStream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                {
                    result.Success = false;
                    result.Errors.Add("No worksheet found in the Excel file.");
                    return result;
                }

                // Get existing occupation codes to avoid duplicates
                var existingCodes = await _context.Occupations
                    .Select(o => o.Code)
                    .ToHashSetAsync();

                var occupations = new List<Occupation>();

                // Find the data start row (skip header rows)
                int dataStartRow = 1;
                for (int row = 1; row <= 20; row++)
                {
                    var cellValue = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                    // Look for the first numeric code (major group starts with "1")
                    if (cellValue != null && cellValue.All(char.IsDigit) && cellValue.Length >= 1)
                    {
                        dataStartRow = row;
                        break;
                    }
                }

                int rowCount = worksheet.Dimension?.Rows ?? 0;

                // Track current hierarchy context as we iterate rows
                string? currentMajorCode = null;
                string? currentMajorName = null;
                string? currentSubMajorCode = null;
                string? currentSubMajorName = null;
                string? currentMinorCode = null;
                string? currentMinorName = null;
                string? currentUnitCode = null;
                string? currentUnitName = null;

                for (int row = dataStartRow; row <= rowCount; row++)
                {
                    try
                    {
                        // Read across columns to find where the code is
                        // The ABS file uses indentation - each level is in a different column
                        string? code = null;
                        string? name = null;
                        int codeColumn = -1;

                        // Check columns 1-6 for a code (codes appear in different columns based on hierarchy)
                        for (int col = 1; col <= 6; col++)
                        {
                            var cellValue = worksheet.Cells[row, col].Value?.ToString()?.Trim().Replace(" ", "");
                            if (!string.IsNullOrWhiteSpace(cellValue) && cellValue.All(char.IsDigit))
                            {
                                code = cellValue;
                                codeColumn = col;
                                // Name is in the next column
                                name = worksheet.Cells[row, col + 1].Value?.ToString()?.Trim();
                                break;
                            }
                        }

                        // Skip if no valid code found
                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
                            continue;

                        // Determine hierarchy level based on code length and column position
                        int hierarchyLevel = DetermineHierarchyLevel(code, codeColumn);

                        switch (hierarchyLevel)
                        {
                            case 1: // Major Group
                                currentMajorCode = code;
                                currentMajorName = name;
                                currentSubMajorCode = null;
                                currentSubMajorName = null;
                                currentMinorCode = null;
                                currentMinorName = null;
                                currentUnitCode = null;
                                currentUnitName = null;

                                AddOccupation(occupations, existingCodes, result, code, name,
                                    currentMajorCode, currentMajorName, null, null, null, null, null, null);
                                break;

                            case 2: // Sub-Major Group
                                currentSubMajorCode = code;
                                currentSubMajorName = name;
                                currentMinorCode = null;
                                currentMinorName = null;
                                currentUnitCode = null;
                                currentUnitName = null;

                                AddOccupation(occupations, existingCodes, result, code, name,
                                    currentMajorCode, currentMajorName, currentSubMajorCode, currentSubMajorName, null, null, null, null);
                                break;

                            case 3: // Minor Group
                                currentMinorCode = code;
                                currentMinorName = name;
                                currentUnitCode = null;
                                currentUnitName = null;

                                AddOccupation(occupations, existingCodes, result, code, name,
                                    currentMajorCode, currentMajorName, currentSubMajorCode, currentSubMajorName,
                                    currentMinorCode, currentMinorName, null, null);
                                break;

                            case 4: // Unit Group
                                currentUnitCode = code;
                                currentUnitName = name;

                                AddOccupation(occupations, existingCodes, result, code, name,
                                    currentMajorCode, currentMajorName, currentSubMajorCode, currentSubMajorName,
                                    currentMinorCode, currentMinorName, currentUnitCode, currentUnitName);
                                break;

                            case 5: // Occupation (specific)
                                AddOccupation(occupations, existingCodes, result, code, name,
                                    currentMajorCode, currentMajorName, currentSubMajorCode, currentSubMajorName,
                                    currentMinorCode, currentMinorName, currentUnitCode, currentUnitName);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Warnings.Add($"Row {row}: Error parsing - {ex.Message}");
                        result.RecordsSkipped++;
                    }
                }

                // Save to database
                if (occupations.Any())
                {
                    await _context.Occupations.AddRangeAsync(occupations);
                    await _context.SaveChangesAsync();
                    result.RecordsImported = occupations.Count;
                }
                else
                {
                    result.Success = false;
                    result.Errors.Add("No valid occupation records found in the file.");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error processing file: {ex.Message}");
            }

            return result;
        }

        private int DetermineHierarchyLevel(string code, int columnPosition)
        {
            // Determine hierarchy based on code length
            // The column position provides additional validation
            int codeLength = code.Length;

            if (codeLength == 1) return 1; // Major Group
            if (codeLength == 2) return 2; // Sub-Major Group
            if (codeLength == 3) return 3; // Minor Group
            if (codeLength == 4) return 4; // Unit Group
            if (codeLength >= 5) return 5; // Occupation

            // Fallback to column-based detection if code length is ambiguous
            return columnPosition;
        }

        private void AddOccupation(List<Occupation> occupations, HashSet<string> existingCodes, ImportResult result,
            string code, string name,
            string? majorCode, string? majorName,
            string? subMajorCode, string? subMajorName,
            string? minorCode, string? minorName,
            string? unitCode, string? unitName)
        {
            // Normalize code to 6 digits
            var normalizedCode = code.PadLeft(6, '0');

            // Skip if already exists
            if (existingCodes.Contains(normalizedCode))
            {
                result.RecordsSkipped++;
                return;
            }

            var occupation = new Occupation
            {
                Code = normalizedCode,
                Name = name,
                MajorGroupCode = majorCode,
                MajorGroupName = majorName,
                SubMajorGroupCode = subMajorCode,
                SubMajorGroupName = subMajorName,
                MinorGroupCode = minorCode,
                MinorGroupName = minorName,
                UnitGroupCode = unitCode,
                UnitGroupName = unitName,
                IsActive = true
            };

            occupations.Add(occupation);
            existingCodes.Add(normalizedCode);
        }
    }
}
