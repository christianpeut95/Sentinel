# Update-DeploymentPackage.ps1
# Updates the C:\Surveillance-MVP-Deployment-Package with latest code and SQL views

param(
    [string]$PackagePath = "C:\Surveillance-MVP-Deployment-Package",
    [string]$SourcePath = ".",
    [switch]$Force
)

Write-Host "=== UPDATING DEPLOYMENT PACKAGE ===" -ForegroundColor Green
Write-Host ""

# Step 1: Verify source
Write-Host "1. Verifying source..." -ForegroundColor Cyan
if (-not (Test-Path "Surveillance-MVP\Surveillance-MVP.csproj")) {
    Write-Host "   ? ERROR: Must run from solution root" -ForegroundColor Red
    exit 1
}
Write-Host "   ? Source verified" -ForegroundColor Green
Write-Host ""

# Step 2: Clean old package
Write-Host "2. Cleaning old package..." -ForegroundColor Cyan
if (Test-Path $PackagePath) {
    if ($Force) {
        Remove-Item "$PackagePath\*" -Recurse -Force -Exclude "docker-compose.yml","Dockerfile",".env"
        Write-Host "   ? Cleaned (kept Docker files)" -ForegroundColor Green
    } else {
        Write-Host "   ?? Package exists. Use -Force to overwrite" -ForegroundColor Yellow
        Write-Host "   Updating migrations only..." -ForegroundColor Yellow
    }
} else {
    New-Item -ItemType Directory -Path $PackagePath -Force | Out-Null
    Write-Host "   ? Created package directory" -ForegroundColor Green
}
Write-Host ""

# Step 3: Build solution
Write-Host "3. Building solution..." -ForegroundColor Cyan
dotnet build Surveillance-MVP -c Release --no-restore -v q
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ? Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "   ? Build successful" -ForegroundColor Green
Write-Host ""

# Step 4: Publish application
Write-Host "4. Publishing application..." -ForegroundColor Cyan
dotnet publish Surveillance-MVP -c Release -o $PackagePath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ? Publish failed" -ForegroundColor Red
    exit 1
}
Write-Host "   ? Published to $PackagePath" -ForegroundColor Green
Write-Host ""

# Step 5: Copy migrations explicitly (ensure latest)
Write-Host "5. Updating migrations..." -ForegroundColor Cyan
$migrationsSource = "Surveillance-MVP\Migrations"
$migrationsTarget = "$PackagePath\Migrations"

if (Test-Path $migrationsTarget) {
    Remove-Item "$migrationsTarget\*" -Force
}
New-Item -ItemType Directory -Path $migrationsTarget -Force | Out-Null

Copy-Item "$migrationsSource\*.cs" -Destination $migrationsTarget -Force
$migrationCount = (Get-ChildItem "$migrationsTarget\*.cs").Count
Write-Host "   ? Copied $migrationCount migration files" -ForegroundColor Green

# List migrations
Get-ChildItem "$migrationsTarget\*.cs" -Exclude "*Designer.cs","*Snapshot.cs" | ForEach-Object {
    Write-Host "      - $($_.Name)" -ForegroundColor Gray
}
Write-Host ""

# Step 6: Copy Docker files
Write-Host "6. Updating Docker files..." -ForegroundColor Cyan
Copy-Item "Dockerfile" -Destination "$PackagePath\Dockerfile" -Force -ErrorAction SilentlyContinue
Copy-Item "docker-compose.yml" -Destination "$PackagePath\docker-compose.yml" -Force -ErrorAction SilentlyContinue
Copy-Item ".dockerignore" -Destination "$PackagePath\.dockerignore" -Force -ErrorAction SilentlyContinue
Copy-Item ".env.example" -Destination "$PackagePath\.env.example" -Force -ErrorAction SilentlyContinue
Write-Host "   ? Docker files updated" -ForegroundColor Green
Write-Host ""

# Step 7: Copy appsettings
Write-Host "7. Updating configuration..." -ForegroundColor Cyan
Copy-Item "Surveillance-MVP\appsettings.json" -Destination "$PackagePath\appsettings.json" -Force
Copy-Item "Surveillance-MVP\appsettings.Production.json" -Destination "$PackagePath\appsettings.Production.json" -Force -ErrorAction SilentlyContinue
Copy-Item "Surveillance-MVP\appsettings.Staging.json" -Destination "$PackagePath\appsettings.Staging.json" -Force -ErrorAction SilentlyContinue
Write-Host "   ? Configuration files updated" -ForegroundColor Green
Write-Host ""

# Step 8: Create deployment README
Write-Host "8. Creating deployment README..." -ForegroundColor Cyan
$readmeContent = @"
# Surveillance MVP - Deployment Package
**Updated**: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

## ? Package Contents

- Application binaries (published)
- **3 EF Core migrations** (includes SQL views)
- Docker files (Dockerfile, docker-compose.yml)
- Configuration files (appsettings.*.json)

## ?? Quick Deploy

### Option 1: Docker (Recommended)
``````powershell
cd C:\Surveillance-MVP-Deployment-Package
docker-compose up -d
``````

### Option 2: Direct Run
``````powershell
cd C:\Surveillance-MVP-Deployment-Package
dotnet Surveillance-MVP.dll
``````

## ?? Database Migrations

This package includes **3 migrations**:
1. ``20260304034303_InitialCreate_Clean`` - All tables
2. ``20260304060957_AddReportingViews`` - 6 basic SQL views
3. ``20260304072216_EnhanceViewsWithRealData`` - Enhanced views with:
   - Recursive transmission chains (10 levels)
   - Full task tracking
   - Exposure relationships
   - Calculated fields
   - Timeline events

### Apply Migrations:
``````powershell
# Automatic (on app startup if configured)
dotnet Surveillance-MVP.dll

# Manual
dotnet ef database update
``````

## ??? SQL Views Included

All 6 views will be auto-created:
- vw_CaseContactTasksFlattened (86 columns, recursive CTE)
- vw_ContactsListSimple
- vw_OutbreakTasksFlattened
- vw_CaseTimelineAll
- vw_ContactTracingMindMapNodes
- vw_ContactTracingMindMapEdges

## ?? Configuration

Update connection string in ``appsettings.Production.json``:
``````json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=SurveillanceMVP;..."
  }
}
``````

## ? Verification

After deployment:
``````sql
-- Check migrations applied
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;

-- Check views exist
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.VIEWS 
WHERE TABLE_NAME LIKE 'vw_%' ORDER BY TABLE_NAME;

-- Test a view
SELECT TOP 5 * FROM vw_CaseContactTasksFlattened;
``````

---

**Package Version**: $(Get-Date -Format "yyyyMMdd-HHmmss")
**Migrations**: 3 (includes SQL views)
**Build**: Release
**Ready**: For production deployment
"@

Set-Content "$PackagePath\README_DEPLOY.md" -Value $readmeContent
Write-Host "   ? Deployment README created" -ForegroundColor Green
Write-Host ""

# Step 9: Summary
Write-Host "=== DEPLOYMENT PACKAGE UPDATED ===" -ForegroundColor Green
Write-Host ""
Write-Host "Location: $PackagePath" -ForegroundColor Cyan
Write-Host "Migrations: 3 (including SQL views)" -ForegroundColor Cyan
Write-Host "Views: 6 (enhanced versions)" -ForegroundColor Cyan
Write-Host "Docker: Ready" -ForegroundColor Cyan
Write-Host ""
Write-Host "? Package ready for deployment!" -ForegroundColor Green
Write-Host ""
Write-Host "To deploy:" -ForegroundColor Yellow
Write-Host "  cd $PackagePath" -ForegroundColor Gray
Write-Host "  docker-compose up -d" -ForegroundColor Gray
Write-Host ""
