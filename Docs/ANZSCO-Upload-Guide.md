# ANZSCO Occupation Upload Feature

## Overview
This feature allows you to bulk-import ANZSCO (Australian and New Zealand Standard Classification of Occupations) data from an Excel file downloaded from the ABS website.

## How to Use

### Step 1: Download the Official File
1. Go to the ABS website: https://www.abs.gov.au/statistics/classifications/anzsco-australian-and-new-zealand-standard-classification-occupations/2022
2. Download the Excel file: `anzsco 2022 structure 062023.xlsx`

### Step 2: Upload the File
1. Navigate to **Settings** ? **Occupations**
2. Click the **"Upload Excel File"** button
3. Select the downloaded .xlsx file
4. Click **"Upload and Import"**

### Step 3: Review Results
The importer will display:
- **Records Imported**: Number of new occupations added
- **Records Skipped**: Number of existing codes (no duplicates)
- **Warnings**: Any parsing issues encountered
- **Errors**: Critical issues that prevented import

## How It Works

### File Format Detection
The ABS file uses an **indented hierarchy layout** where each level appears in different columns as you read down the rows:

**Example from ABS file:**
```
Major Group
    Sub-Major Group
        Minor Group
            Unit Group
                Occupation        Skill Level
1   Managers
    11  Chief Executives, General Managers and Legislators
        111 Chief Executives, General Managers and Legislators
            1111    Chief Executives and Managing Directors
                    111111  Chief Executive or Managing Director    1
            1112    General Managers
                    111211  Corporate General Manager               1
```

**How it works:**
- **Column A (1)**: Major Group codes appear here
- **Column B (2)**: Sub-Major Group codes (indented one level)
- **Column C (3)**: Minor Group codes (indented two levels)
- **Column D (4)**: Unit Group codes (indented three levels)
- **Column E (5)**: Occupation codes (indented four levels)
- The name always appears in the next column after the code

### Hierarchy Building
The service automatically:
1. Reads each row from the Excel file
2. Scans across columns to find where a code appears (determines hierarchy level)
3. Maintains hierarchy context as it processes down the rows
4. Links each occupation to its parent groups based on the current context
5. Skips duplicate codes to prevent data conflicts

### Example Row Structure
The ABS file uses indentation to show hierarchy:
```
Row with Major:     1 | Managers        | (empty columns)
Row with SubMajor:  (empty) | 11 | Chief Executives... | (empty columns)
Row with Minor:     (empty) | (empty) | 111 | Chief Executives... | (empty)
Row with Unit:      (empty) | (empty) | (empty) | 1111 | Chief Executives... | (empty)
Row with Occup:     (empty) | (empty) | (empty) | (empty) | 111111 | Chief Executive | 1
```

The parser scans each row left-to-right to find where the code appears, and maintains context as it goes down rows.

### Example Structure
After import, you'll have the complete hierarchy:

```
1 - Managers
  11 - Chief Executives, General Managers
    111 - Chief Executives and Managing Directors
      1111 - Chief Executives and Managing Directors
        111111 - Chief Executive or Managing Director
        111211 - Corporate General Manager
```

## Features

### Smart Parsing
- Handles various code formats (with or without spaces)
- Validates code structure (numeric, 1-6 digits)
- Skips invalid or empty rows

### Duplicate Prevention
- Checks existing database codes before import
- Skips records that already exist
- Reports skipped records in the summary

### Error Handling
- Continues processing even if individual rows fail
- Collects all warnings and errors
- Provides detailed feedback after import

### Data Validation
- File size limit: 10MB
- Accepted format: .xlsx only
- Validates Excel structure

## Technical Details

### Service: `OccupationImportService`
- **Location**: `Services/OccupationImportService.cs`
- **Dependencies**: EPPlus 7.5.2 (Excel parsing)
- **Database**: Bulk insert using EF Core

### Page Model: `Upload.cshtml.cs`
- **Location**: `Pages/Settings/Occupations/Upload.cshtml.cs`
- **Route**: `/Settings/Occupations/Upload`
- **Security**: Inherits authentication from Razor Pages

### License Note
EPPlus uses a NonCommercial license context. If you're using this commercially, you may need to purchase an EPPlus license or use an alternative library like ClosedXML.

## Troubleshooting

### "No valid occupation records found"
- Ensure the Excel file has the correct structure (Code in Column 1, Name in Column 2)
- Check that the file isn't empty
- Verify the file isn't corrupted

### "Row X: Invalid code format"
- The code must be numeric
- The code must be 1-6 digits
- Check for special characters or formatting issues

### High number of skipped records
- Records are skipped if they already exist in the database
- This is normal if you're re-importing the same file
- The importer normalizes all codes to 6 digits (e.g., "1" becomes "000001")

### "Value cannot be null. (Parameter 'key')" error
- This has been fixed in the latest version
- The issue was related to dictionary lookup for hierarchy names
- If you still see this, ensure you're using the latest code

## Future Enhancements

Potential improvements:
- Add a "Clear All Occupations" button for re-importing
- Support for updating existing records
- Progress indicator for large files
- Preview mode before importing
- Export feature to Excel
- Validation against official ABS checksums
