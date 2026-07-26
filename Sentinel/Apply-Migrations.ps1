#!/usr/bin/env pwsh
<#
.SYNOPSIS
	Applies all Entity Framework Core migrations to the Sentinel database.

.DESCRIPTION
	This script ensures the database schema is up-to-date by applying all pending
	EF Core migrations. It includes error handling and verification steps.

.PARAMETER ConnectionString
	Optional. The database connection string. If not provided, uses the connection
	string from appsettings.json.

.PARAMETER Validate
	If specified, only validates that migrations can be applied without actually
	applying them.

.EXAMPLE
	.\Apply-Migrations.ps1
	Applies all pending migrations using the default connection string.

.EXAMPLE
	.\Apply-Migrations.ps1 -Validate
	Validates migrations without applying them.

.EXAMPLE
	.\Apply-Migrations.ps1 -ConnectionString "Server=myserver;Database=Sentinel;..."
	Applies migrations using a custom connection string.
#>

param(
	[string]$ConnectionString,
	[switch]$Validate
)

$ErrorActionPreference = "Stop"

# Change to the Sentinel project directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $scriptPath "Sentinel"

if (-not (Test-Path $projectPath)) {
	Write-Error "Sentinel project directory not found at: $projectPath"
	exit 1
}

Set-Location $projectPath

Write-Host "=== Sentinel Database Migration Script ===" -ForegroundColor Cyan
Write-Host "Project Path: $projectPath" -ForegroundColor Gray
Write-Host ""

# Build the project first
Write-Host "[1/4] Building project..." -ForegroundColor Yellow
$buildResult = dotnet build Sentinel.csproj --configuration Release --no-incremental 2>&1
if ($LASTEXITCODE -ne 0) {
	Write-Error "Build failed. Please fix build errors before applying migrations."
	Write-Host $buildResult -ForegroundColor Red
	exit 1
}
Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# List all migrations
Write-Host "[2/4] Checking migration status..." -ForegroundColor Yellow
$migrationList = dotnet ef migrations list --context ApplicationDbContext --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
	Write-Error "Failed to list migrations."
	Write-Host $migrationList -ForegroundColor Red
	exit 1
}

# Count total migrations
$totalMigrations = ($migrationList | Where-Object { $_ -match '^\d{14}_' }).Count
Write-Host "✓ Found $totalMigrations migrations" -ForegroundColor Green
Write-Host ""

# Check for pending changes
Write-Host "[3/4] Checking for pending model changes..." -ForegroundColor Yellow
$pendingCheck = dotnet ef migrations has-pending-model-changes --context ApplicationDbContext --no-build 2>&1
if ($pendingCheck -match "Changes have been made") {
	Write-Warning "⚠ Warning: Model changes detected that are not yet in a migration!"
	Write-Host "You may need to create a new migration with:" -ForegroundColor Yellow
	Write-Host "  dotnet ef migrations add YourMigrationName --context ApplicationDbContext" -ForegroundColor Yellow
	Write-Host ""
} else {
	Write-Host "✓ No pending model changes" -ForegroundColor Green
	Write-Host ""
}

# Apply migrations (or validate only)
if ($Validate) {
	Write-Host "[4/4] Validation mode - skipping actual migration" -ForegroundColor Yellow
	Write-Host "✓ Migrations are ready to be applied" -ForegroundColor Green
} else {
	Write-Host "[4/4] Applying migrations to database..." -ForegroundColor Yellow

	$updateArgs = @(
		"ef", "database", "update",
		"--context", "ApplicationDbContext"
	)

	if ($ConnectionString) {
		$updateArgs += "--connection"
		$updateArgs += $ConnectionString
	}

	$updateResult = & dotnet $updateArgs 2>&1

	if ($LASTEXITCODE -ne 0) {
		Write-Error "Migration failed!"
		Write-Host $updateResult -ForegroundColor Red
		Write-Host ""
		Write-Host "Common issues:" -ForegroundColor Yellow
		Write-Host "  1. Database server is not accessible" -ForegroundColor Gray
		Write-Host "  2. Connection string is invalid" -ForegroundColor Gray
		Write-Host "  3. Database user lacks necessary permissions" -ForegroundColor Gray
		Write-Host "  4. A previous migration failed and needs to be rolled back" -ForegroundColor Gray
		exit 1
	}

	Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Migration Complete ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Verify the database schema is correct" -ForegroundColor Gray
Write-Host "  2. Run the application and test core functionality" -ForegroundColor Gray
Write-Host "  3. Check application logs for any startup errors" -ForegroundColor Gray
Write-Host ""

# Return to original directory
Set-Location $scriptPath
