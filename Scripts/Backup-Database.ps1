# ===============================================
# Database Backup Script for Surveillance MVP
# ===============================================
# Description: Automated SQL Server backup with retention policy
# Usage: Run manually or via Windows Task Scheduler
# ===============================================

param(
    [string]$ServerInstance = "(localdb)\mssqllocaldb",
    [string]$DatabaseName = "aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d",
    [string]$BackupPath = "C:\DatabaseBackups\SurveillanceMVP",
    [int]$RetentionDays = 30,
    [switch]$Compress = $true
)

# Import SQL Server module
try {
    Import-Module SqlServer -ErrorAction Stop
} catch {
    Write-Error "SQL Server PowerShell module not found. Install with: Install-Module -Name SqlServer"
    exit 1
}

# Create backup directory if it doesn't exist
if (-not (Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
    Write-Host "? Created backup directory: $BackupPath" -ForegroundColor Green
}

# Generate backup filename with timestamp
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $BackupPath "SurveillanceMVP_Full_$timestamp.bak"

Write-Host "`n?? Starting database backup..." -ForegroundColor Cyan
Write-Host "Server: $ServerInstance" -ForegroundColor Gray
Write-Host "Database: $DatabaseName" -ForegroundColor Gray
Write-Host "Backup file: $backupFile" -ForegroundColor Gray

try {
    # Create backup
    $backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = '$backupFile'
WITH FORMAT,
     INIT,
     NAME = 'Surveillance MVP Full Backup',
     SKIP,
     NOREWIND,
     NOUNLOAD,
     COMPRESSION,
     STATS = 10;
"@

    Invoke-Sqlcmd -ServerInstance $ServerInstance -Query $backupQuery -QueryTimeout 600
    
    # Verify backup file
    if (Test-Path $backupFile) {
        $backupSize = (Get-Item $backupFile).Length / 1MB
        Write-Host "`n? Backup completed successfully!" -ForegroundColor Green
        Write-Host "Backup size: $([math]::Round($backupSize, 2)) MB" -ForegroundColor Green
        
        # Log to file
        $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] SUCCESS: Backup created - $backupFile ($([math]::Round($backupSize, 2)) MB)"
        Add-Content -Path (Join-Path $BackupPath "backup-log.txt") -Value $logEntry
    } else {
        throw "Backup file not found after backup operation"
    }
    
} catch {
    Write-Host "`n? Backup failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    
    # Log error
    $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: Backup failed - $_"
    Add-Content -Path (Join-Path $BackupPath "backup-log.txt") -Value $logEntry
    
    exit 1
}

# Cleanup old backups (retention policy)
Write-Host "`n?? Cleaning up old backups (retention: $RetentionDays days)..." -ForegroundColor Cyan

$cutoffDate = (Get-Date).AddDays(-$RetentionDays)
$oldBackups = Get-ChildItem -Path $BackupPath -Filter "*.bak" | Where-Object { $_.LastWriteTime -lt $cutoffDate }

if ($oldBackups.Count -gt 0) {
    foreach ($file in $oldBackups) {
        Remove-Item $file.FullName -Force
        Write-Host "Deleted old backup: $($file.Name)" -ForegroundColor Yellow
        
        $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] CLEANUP: Deleted old backup - $($file.Name)"
        Add-Content -Path (Join-Path $BackupPath "backup-log.txt") -Value $logEntry
    }
} else {
    Write-Host "No old backups to delete" -ForegroundColor Gray
}

Write-Host "`n? Backup process complete!" -ForegroundColor Green

# Display backup summary
Write-Host "`n?? Backup Summary:" -ForegroundColor Cyan
$allBackups = Get-ChildItem -Path $BackupPath -Filter "*.bak" | Sort-Object LastWriteTime -Descending
Write-Host "Total backups: $($allBackups.Count)" -ForegroundColor White
Write-Host "Latest backup: $($allBackups[0].Name)" -ForegroundColor White
Write-Host "Total size: $([math]::Round(($allBackups | Measure-Object -Property Length -Sum).Sum / 1GB, 2)) GB" -ForegroundColor White
