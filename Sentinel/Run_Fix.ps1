# Run HL7 Resolution Fix
# This script executes the SQL fix for specimen type resolution issues

$scriptPath = "Fix_HL7_Resolution_Issues.sql"
$server = "(localdb)\MSSQLLocalDB"  # Change this to your SQL Server instance
$database = "SentinelDb"             # Change this to your database name

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "HL7 Field Resolution Fix" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Server:   $server" -ForegroundColor Yellow
Write-Host "Database: $database" -ForegroundColor Yellow
Write-Host "Script:   $scriptPath" -ForegroundColor Yellow
Write-Host ""

# Check if SQL file exists
if (-not (Test-Path $scriptPath)) {
    Write-Host "❌ ERROR: SQL script not found: $scriptPath" -ForegroundColor Red
    exit 1
}

Write-Host "Executing SQL script..." -ForegroundColor Green
Write-Host ""

try {
    # Execute the SQL script
    sqlcmd -S $server -d $database -i $scriptPath -b

    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✅ SUCCESS!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Copy your test HL7 file back to the monitored folder" -ForegroundColor White
        Write-Host "2. Watch the logs for successful specimen type resolution" -ForegroundColor White
        Write-Host "3. Verify disease identification and case creation" -ForegroundColor White
        Write-Host ""
    } else {
        Write-Host ""
        Write-Host "❌ SQL execution failed. See errors above." -ForegroundColor Red
        Write-Host ""
    }
} catch {
    Write-Host ""
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
}

# Keep window open
Write-Host "Press any key to continue..."
$null = $host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
