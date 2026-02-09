# PowerShell script to verify authorization implementation
# Checks all page files for proper [Authorize] attributes and using statements

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "AUTHORIZATION VERIFICATION REPORT" -ForegroundColor Cyan
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

$pagesPath = "Surveillance-MVP\Pages"
$allPages = Get-ChildItem -Path $pagesPath -Filter "*.cshtml.cs" -Recurse -Exclude "_*"

# Categories
$properlyAuthorized = @()
$missingAuthorize = @()
$missingUsing = @()
$utilityPages = @()

# Files that should be excluded (utility pages that don't need authorization)
$excludeList = @(
    "Error.cshtml.cs",
    "Index.cshtml.cs",
    "Privacy.cshtml.cs",
    "Api\OccupationSearch.cshtml.cs",
    "Patients\AuditHistory.cshtml.cs",
    "Patients\Search.cshtml.cs",
    "DebugPermissions.cshtml.cs"
)

foreach ($file in $allPages) {
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $fileName = $file.Name
    
    # Skip if in exclude list
    $shouldExclude = $false
    foreach ($excluded in $excludeList) {
        if ($relativePath -like "*$excluded") {
            $shouldExclude = $true
            break
        }
    }
    
    if ($shouldExclude) {
        $utilityPages += $relativePath
        continue
    }
    
    $content = Get-Content $file.FullName -Raw
    
    # Check for [Authorize] attribute
    $hasAuthorize = $content -match '\[Authorize'
    
    # Check for using statement
    $hasUsing = $content -match 'using Microsoft\.AspNetCore\.Authorization;'
    
    if ($hasAuthorize -and $hasUsing) {
        $properlyAuthorized += $relativePath
    }
    elseif ($hasAuthorize -and -not $hasUsing) {
        $missingUsing += $relativePath
    }
    elseif (-not $hasAuthorize) {
        $missingAuthorize += $relativePath
    }
}

# Report Results
Write-Host "SUMMARY" -ForegroundColor Yellow
Write-Host "-------" -ForegroundColor Yellow
Write-Host "Total Pages Scanned: $($allPages.Count)" -ForegroundColor White
Write-Host "Properly Authorized: $($properlyAuthorized.Count)" -ForegroundColor Green
Write-Host "Missing [Authorize]: $($missingAuthorize.Count)" -ForegroundColor Red
Write-Host "Missing using stmt: $($missingUsing.Count)" -ForegroundColor Yellow
Write-Host "Utility (Excluded): $($utilityPages.Count)" -ForegroundColor Gray
Write-Host ""

# Show properly authorized files
if ($properlyAuthorized.Count -gt 0) {
    Write-Host "? PROPERLY AUTHORIZED ($($properlyAuthorized.Count) files)" -ForegroundColor Green
    Write-Host "-----------------------------------" -ForegroundColor Green
    foreach ($file in $properlyAuthorized | Sort-Object) {
        Write-Host "  ? $file" -ForegroundColor Green
    }
    Write-Host ""
}

# Show missing [Authorize] attribute
if ($missingAuthorize.Count -gt 0) {
    Write-Host "? MISSING [Authorize] ATTRIBUTE ($($missingAuthorize.Count) files)" -ForegroundColor Red
    Write-Host "----------------------------------------" -ForegroundColor Red
    foreach ($file in $missingAuthorize | Sort-Object) {
        Write-Host "  ? $file" -ForegroundColor Red
    }
    Write-Host ""
}

# Show missing using statement
if ($missingUsing.Count -gt 0) {
    Write-Host "??  MISSING using STATEMENT ($($missingUsing.Count) files)" -ForegroundColor Yellow
    Write-Host "-----------------------------------" -ForegroundColor Yellow
    foreach ($file in $missingUsing | Sort-Object) {
        Write-Host "  ! $file" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Show utility pages (for reference)
if ($utilityPages.Count -gt 0) {
    Write-Host "??  UTILITY PAGES (Excluded from check)" -ForegroundColor Gray
    Write-Host "--------------------------------------" -ForegroundColor Gray
    foreach ($file in $utilityPages | Sort-Object) {
        Write-Host "  - $file" -ForegroundColor Gray
    }
    Write-Host ""
}

# Coverage percentage
$totalRelevantPages = $allPages.Count - $utilityPages.Count
$coveragePercent = [math]::Round(($properlyAuthorized.Count / $totalRelevantPages) * 100, 1)

Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host "COVERAGE: $coveragePercent% ($($properlyAuthorized.Count)/$totalRelevantPages pages)" -ForegroundColor $(if ($coveragePercent -eq 100) { "Green" } elseif ($coveragePercent -ge 90) { "Yellow" } else { "Red" })
Write-Host "=" * 80 -ForegroundColor Cyan
Write-Host ""

# Final status
if ($missingAuthorize.Count -eq 0 -and $missingUsing.Count -eq 0) {
    Write-Host "?? ALL PAGES PROPERLY AUTHORIZED!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "1. Run 'dotnet build' to verify compilation" -ForegroundColor White
    Write-Host "2. Test with different user roles" -ForegroundColor White
    Write-Host "3. Verify permission enforcement works" -ForegroundColor White
}
else {
    Write-Host "??  ISSUES FOUND - Review above sections" -ForegroundColor Yellow
    if ($missingAuthorize.Count -gt 0) {
        Write-Host ""
        Write-Host "Fix missing [Authorize] by adding:" -ForegroundColor White
        Write-Host "  [Authorize(Policy = ""Permission.Module.Action"")]" -ForegroundColor Gray
    }
    if ($missingUsing.Count -gt 0) {
        Write-Host ""
        Write-Host "Fix missing using by running:" -ForegroundColor White
        Write-Host "  .\FixAuthorizationUsings.ps1" -ForegroundColor Gray
    }
}

Write-Host ""
