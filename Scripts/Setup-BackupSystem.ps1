# ===============================================
# Add Backup System Migration
# ===============================================
# Run this to add backup functionality to your database

# Navigate to project directory
cd "C:\Users\Christian\source\repos\Surveillance-MVP\Surveillance-MVP"

# Create migration
Write-Host "Creating migration for Backup System..." -ForegroundColor Cyan
dotnet ef migrations add AddBackupSystem

# Apply migration
Write-Host "`nApplying migration to database..." -ForegroundColor Cyan
dotnet ef database update

# Create backup directory
$backupPath = "C:\DatabaseBackups\SurveillanceMVP"
if (-not (Test-Path $backupPath)) {
    Write-Host "`nCreating backup directory..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null
    Write-Host "? Backup directory created: $backupPath" -ForegroundColor Green
} else {
    Write-Host "? Backup directory already exists: $backupPath" -ForegroundColor Green
}

Write-Host "`n? Backup system setup complete!" -ForegroundColor Green
Write-Host "`nAccess the backup UI at: https://localhost:5001/Settings/Backups" -ForegroundColor Yellow
