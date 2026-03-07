# Quick Database Backup Script
# Backs up local Surveillance database before migration changes

$ErrorActionPreference = "Stop"

# Configuration
$DatabaseName = "aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d"
$BackupFolder = "C:\Surveillance-Backups\BeforeMigrationFix"
$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = Join-Path $BackupFolder "$DatabaseName`_BeforeMigrationFix_$Timestamp.bak"

Write-Host "`n?? QUICK DATABASE BACKUP" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Create backup folder if it doesn't exist
if (-not (Test-Path $BackupFolder)) {
    Write-Host "?? Creating backup folder..." -ForegroundColor Yellow
    New-Item -Path $BackupFolder -ItemType Directory -Force | Out-Null
}

Write-Host "?? Database: $DatabaseName" -ForegroundColor White
Write-Host "?? Backup to: $BackupFile" -ForegroundColor White
Write-Host ""

# SQL Server connection
$ServerInstance = "(localdb)\MSSQLLocalDB"

# Check if database exists
Write-Host "?? Checking database exists..." -ForegroundColor Yellow
$checkQuery = "SELECT COUNT(*) FROM sys.databases WHERE name = '$DatabaseName'"

try {
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = "Server=$ServerInstance;Integrated Security=true;Connection Timeout=30;"
    $conn.Open()
    
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $checkQuery
    $result = $cmd.ExecuteScalar()
    
    if ($result -eq 0) {
        Write-Host "? Database '$DatabaseName' not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Available databases:" -ForegroundColor Yellow
        $cmd.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name"
        $reader = $cmd.ExecuteReader()
        while ($reader.Read()) {
            Write-Host "  - $($reader[0])" -ForegroundColor White
        }
        $reader.Close()
        $conn.Close()
        exit 1
    }
    
    Write-Host "? Database found" -ForegroundColor Green
    
    # Get database size
    $cmd.CommandText = @"
SELECT 
    SUM(size * 8.0 / 1024) AS SizeMB
FROM sys.master_files
WHERE database_id = DB_ID('$DatabaseName')
"@
    $sizeMB = [math]::Round($cmd.ExecuteScalar(), 2)
    Write-Host "?? Database size: $sizeMB MB" -ForegroundColor White
    Write-Host ""
    
    # Backup command
    Write-Host "?? Starting backup..." -ForegroundColor Yellow
    $backupQuery = @"
BACKUP DATABASE [$DatabaseName]
TO DISK = N'$BackupFile'
WITH NOFORMAT, NOINIT,
NAME = N'$DatabaseName-Full Database Backup Before Migration Fix',
SKIP, NOREWIND, NOUNLOAD, COMPRESSION, STATS = 10
"@
    
    $cmd.CommandText = $backupQuery
    $cmd.CommandTimeout = 300  # 5 minutes
    
    $startTime = Get-Date
    $cmd.ExecuteNonQuery() | Out-Null
    $duration = (Get-Date) - $startTime
    
    $conn.Close()
    
    # Check backup file was created
    if (Test-Path $BackupFile) {
        $backupSizeMB = [math]::Round((Get-Item $BackupFile).Length / 1MB, 2)
        
        Write-Host ""
        Write-Host "? BACKUP COMPLETE!" -ForegroundColor Green
        Write-Host "================================" -ForegroundColor Green
        Write-Host "?? Location: $BackupFile" -ForegroundColor White
        Write-Host "?? Backup size: $backupSizeMB MB" -ForegroundColor White
        Write-Host "??  Duration: $($duration.TotalSeconds) seconds" -ForegroundColor White
        Write-Host ""
        Write-Host "? Your database is safely backed up!" -ForegroundColor Green
        Write-Host "   You can now proceed with dropping views and running migrations." -ForegroundColor White
        Write-Host ""
        
        # Open backup folder
        Write-Host "?? Opening backup folder..." -ForegroundColor Yellow
        Start-Process explorer.exe -ArgumentList $BackupFolder
    }
    else {
        Write-Host "? Backup file not found after backup!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "? ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($conn.State -eq 'Open') {
        $conn.Close()
    }
    exit 1
}
