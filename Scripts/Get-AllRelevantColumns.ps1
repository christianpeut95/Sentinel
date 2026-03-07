# Get-AllRelevantColumns.ps1
# Query all table columns needed for the views

$dbName = "aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d"

Write-Host "=== PATIENTS TABLE ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d $dbName -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Patients' ORDER BY ORDINAL_POSITION" -h -1

Write-Host "`n=== CASES TABLE ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d $dbName -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Cases' ORDER BY ORDINAL_POSITION" -h -1

Write-Host "`n=== CASETASKS TABLE ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d $dbName -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CaseTasks' ORDER BY ORDINAL_POSITION" -h -1

Write-Host "`n=== EXPOSUREEVENTS TABLE ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d $dbName -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ExposureEvents' ORDER BY ORDINAL_POSITION" -h -1

Write-Host "`n=== NOTES TABLE ===" -ForegroundColor Cyan
sqlcmd -S "(localdb)\mssqllocaldb" -d $dbName -Q "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Notes' ORDER BY ORDINAL_POSITION" -h -1
