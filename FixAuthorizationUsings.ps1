# PowerShell script to add missing Authorization using statements
# Run this from the solution root directory

$filesToFix = @(
    'Surveillance-MVP\Pages\Settings\Lookups\EditLocationType.cshtml.cs',
    'Surveillance-MVP\Pages\Locations\Delete.cshtml.cs',
    'Surveillance-MVP\Pages\Tasks\CompleteSurvey.cshtml.cs',
    'Surveillance-MVP\Pages\Organizations\Index.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\LocationTypes.cshtml.cs',
    'Surveillance-MVP\Pages\Organizations\Create.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\TaskTypes.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Users\Details.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EditTestResult.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Users\Edit.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\Edit.cshtml.cs',
    'Surveillance-MVP\Pages\Organizations\Edit.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\CreateSpecimenType.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EditResultUnit.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\ClassifyCases.cshtml.cs',
    'Surveillance-MVP\Pages\Events\Delete.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\Details.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\CreateTestType.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EditSpecimenType.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\CreateOrganizationType.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\SpecimenTypes.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\TestResults.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EditOrganizationType.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\CaseDefinitions.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Users\Create.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Users\Permissions.cshtml.cs',
    'Surveillance-MVP\Pages\Events\Details.cshtml.cs',
    'Surveillance-MVP\Pages\Locations\Edit.cshtml.cs',
    'Surveillance-MVP\Pages\Cases\Exposures\Create.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EventTypes.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\ManageTeam.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\BulkActions.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\TaskTemplates.cshtml.cs',
    'Surveillance-MVP\Pages\Outbreaks\LinkCases.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\EditTaskTemplate.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\CreateTaskTemplate.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\TestTypes.cshtml.cs',
    'Surveillance-MVP\Pages\Cases\Exposures\Edit.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\CreateTestResult.cshtml.cs',
    'Surveillance-MVP\Pages\Settings\Lookups\ResultUnits.cshtml.cs'
)

$usingStatement = 'using Microsoft.AspNetCore.Authorization;'
$fixed = 0
$skipped = 0

Write-Host "Starting Authorization using statement fixes..." -ForegroundColor Cyan
Write-Host ""

foreach ($file in $filesToFix) {
    if (-not (Test-Path $file)) {
        Write-Host "  [SKIP] File not found: $file" -ForegroundColor Yellow
        $skipped++
        continue
    }

    $content = Get-Content $file -Raw
    
    # Check if using statement already exists
    if ($content -match 'using Microsoft\.AspNetCore\.Authorization;') {
        Write-Host "  [OK] Already has using: $file" -ForegroundColor Green
        $skipped++
        continue
    }

    # Find the first using statement
    $lines = Get-Content $file
    $insertIndex = -1
    
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^using ') {
            $insertIndex = $i
            break
        }
    }

    if ($insertIndex -eq -1) {
        Write-Host "  [ERROR] No using statements found in: $file" -ForegroundColor Red
        continue
    }

    # Insert the using statement
    $newLines = @()
    $newLines += $lines[0..$insertIndex]
    $newLines += $usingStatement
    $newLines += $lines[($insertIndex + 1)..($lines.Count - 1)]

    # Write back to file
    $newLines | Set-Content $file -Encoding UTF8
    Write-Host "  [FIXED] $file" -ForegroundColor Green
    $fixed++
}

Write-Host ""
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "  Fixed: $fixed files" -ForegroundColor Green
Write-Host "  Skipped: $skipped files" -ForegroundColor Yellow
Write-Host "=" * 60 -ForegroundColor Cyan
Write-Host ""
Write-Host "Next step: Run 'dotnet build' to verify all files compile" -ForegroundColor Yellow
