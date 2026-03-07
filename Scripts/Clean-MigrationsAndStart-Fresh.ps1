# Clean-MigrationsAndStart-Fresh.ps1
# Complete cleanup and fresh start for EF Core migrations

Write-Host "=== EF Core Migration Clean Reset ===" -ForegroundColor Cyan
Write-Host ""

$projectPath = "Surveillance-MVP"

# Step 1: Clean bin and obj folders
Write-Host "Step 1: Cleaning bin and obj folders..." -ForegroundColor Yellow
if (Test-Path "$projectPath\bin") {
    Remove-Item "$projectPath\bin" -Recurse -Force
    Write-Host "  ? Removed bin folder" -ForegroundColor Green
}
if (Test-Path "$projectPath\obj") {
    Remove-Item "$projectPath\obj" -Recurse -Force
    Write-Host "  ? Removed obj folder" -ForegroundColor Green
}

# Step 2: Remove any remaining Migrations folder
Write-Host "`nStep 2: Removing Migrations folder..." -ForegroundColor Yellow
if (Test-Path "$projectPath\Migrations") {
    Remove-Item "$projectPath\Migrations" -Recurse -Force
    Write-Host "  ? Removed Migrations folder" -ForegroundColor Green
} else {
    Write-Host "  ? Migrations folder already removed" -ForegroundColor Gray
}

# Step 3: Verify database is dropped
Write-Host "`nStep 3: Verifying database state..." -ForegroundColor Yellow
Write-Host "  Attempting to drop database (if exists)..." -ForegroundColor Gray

try {
    dotnet ef database drop --project $projectPath --force --no-build 2>$null
    Write-Host "  ? Database dropped" -ForegroundColor Green
} catch {
    Write-Host "  ? Database already dropped or doesn't exist" -ForegroundColor Gray
}

# Step 4: Create new InitialCreate migration
Write-Host "`nStep 4: Creating fresh InitialCreate migration..." -ForegroundColor Yellow
Write-Host "  Building project first..." -ForegroundColor Gray

dotnet build $projectPath --no-restore

Write-Host "  Creating migration..." -ForegroundColor Gray
dotnet ef migrations add InitialCreate --project $projectPath

# Step 5: Apply migration
Write-Host "`nStep 5: Applying InitialCreate migration..." -ForegroundColor Yellow
dotnet ef database update --project $projectPath

Write-Host "`n=== Migration Reset Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Verify the Migrations folder contains only InitialCreate" -ForegroundColor White
Write-Host "  2. Check database structure is correct" -ForegroundColor White
Write-Host "  3. Run the application to test" -ForegroundColor White
