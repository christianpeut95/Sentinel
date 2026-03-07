# Drop Old Views Script
# Drops buggy views so fresh migrations can run

$ErrorActionPreference = "Stop"

Write-Host "`n?? DROP OLD VIEWS" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$DatabaseName = "Surveillance"
$ServerInstance = "(localdb)\MSSQLLocalDB"

$dropViewsSQL = @"
USE [$DatabaseName];
GO

PRINT '???  Dropping old views with type mismatches...';
GO

DROP VIEW IF EXISTS vw_CaseContactTasksFlattened;
DROP VIEW IF EXISTS vw_OutbreakTasksFlattened;
DROP VIEW IF EXISTS vw_ContactTracingMindMapDetail;
DROP VIEW IF EXISTS vw_ContactTracingMindMapNode;
DROP VIEW IF EXISTS vw_CaseTimelineAll;
DROP VIEW IF EXISTS vw_ContactsListSimple;
GO

PRINT '';
PRINT '? Old views dropped successfully!';
PRINT '? You can now run the app (F5) and migrations will create fixed views.';
GO
"@

try {
    Write-Host "?? Connecting to database..." -ForegroundColor Yellow
    
    $conn = New-Object System.Data.SqlClient.SqlConnection
    $conn.ConnectionString = "Server=$ServerInstance;Database=$DatabaseName;Integrated Security=true;Connection Timeout=30;"
    $conn.Open()
    
    Write-Host "? Connected" -ForegroundColor Green
    Write-Host ""
    Write-Host "???  Dropping views..." -ForegroundColor Yellow
    
    $cmd = $conn.CreateCommand()
    
    # Drop each view individually with better output
    $views = @(
        'vw_CaseContactTasksFlattened',
        'vw_OutbreakTasksFlattened',
        'vw_ContactTracingMindMapDetail',
        'vw_ContactTracingMindMapNode',
        'vw_CaseTimelineAll',
        'vw_ContactsListSimple'
    )
    
    foreach ($view in $views) {
        $cmd.CommandText = @"
IF OBJECT_ID('$view', 'V') IS NOT NULL
BEGIN
    DROP VIEW $view;
    SELECT 1 AS Dropped
END
ELSE
    SELECT 0 AS Dropped
"@
        $result = $cmd.ExecuteScalar()
        
        if ($result -eq 1) {
            Write-Host "  ? Dropped: $view" -ForegroundColor Green
        }
        else {
            Write-Host "  ??  Not found: $view" -ForegroundColor Gray
        }
    }
    
    $conn.Close()
    
    Write-Host ""
    Write-Host "? VIEWS DROPPED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "?? Next Steps:" -ForegroundColor Cyan
    Write-Host "  1. Press F5 in Visual Studio" -ForegroundColor White
    Write-Host "  2. Migrations will apply (2 clean migrations)" -ForegroundColor White
    Write-Host "  3. New views will be created with NVARCHAR fix" -ForegroundColor White
    Write-Host "  4. App will start successfully! ??" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "? ERROR: $($_.Exception.Message)" -ForegroundColor Red
    if ($conn.State -eq 'Open') {
        $conn.Close()
    }
    exit 1
}
