# ============================================
# PowerShell Script to Seed HL7 Test Data
# Queries actual schema to avoid missing columns
# ============================================

$ErrorActionPreference = "Stop"

# Database connection string - UPDATE THIS
$connectionString = "Server=(localdb)\mssqllocaldb;Database=aspnet-Sentinel-a57ac8ba-ccc6-4d7a-b380-485d37f1148d;Trusted_Connection=True;MultipleActiveResultSets=true"

Write-Host "=== HL7 Case Definition Test Data Seeder ===" -ForegroundColor Cyan
Write-Host ""

# Helper function to execute SQL
function Invoke-SqlCommand {
    param(
        [string]$Query,
        [string]$ConnectionString
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $command = $connection.CreateCommand()
    $command.CommandText = $Query

    try {
        $connection.Open()
        $result = $command.ExecuteNonQuery()
        return $result
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        throw
    }
    finally {
        $connection.Close()
    }
}

# Helper function to execute scalar query
function Invoke-SqlScalar {
    param(
        [string]$Query,
        [string]$ConnectionString
    )

    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $command = $connection.CreateCommand()
    $command.CommandText = $Query

    try {
        $connection.Open()
        $result = $command.ExecuteScalar()
        return $result
    }
    catch {
        Write-Host "Error: $_" -ForegroundColor Red
        throw
    }
    finally {
        $connection.Close()
    }
}

# Helper function to get table columns
function Get-RequiredColumns {
    param(
        [string]$TableName,
        [string]$ConnectionString
    )

    $query = @"
SELECT COLUMN_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = '$TableName'
  AND IS_NULLABLE = 'NO'
  AND COLUMN_DEFAULT IS NULL
  AND COLUMNPROPERTY(OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME), COLUMN_NAME, 'IsIdentity') = 0
ORDER BY ORDINAL_POSITION
"@

    $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
    $command = $connection.CreateCommand()
    $command.CommandText = $query

    try {
        $connection.Open()
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
        $dataset = New-Object System.Data.DataSet
        $adapter.Fill($dataset) | Out-Null
        return $dataset.Tables[0].Rows | ForEach-Object { $_[0] }
    }
    finally {
        $connection.Close()
    }
}

# Test GUIDs (fixed for consistency)
$COVID19Id = '11111111-1111-1111-1111-111111111111'
$InfluenzaId = '22222222-2222-2222-2222-222222222222'
$InfluenzaAId = '33333333-3333-3333-3333-333333333333'
$InfluenzaBId = '44444444-4444-4444-4444-444444444444'
$MeaslesId = '55555555-5555-5555-5555-555555555555'

$SARSCoV2Id = 'AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA'
$InfluenzaAVirusId = 'BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB'
$InfluenzaBVirusId = 'CCCCCCCC-CCCC-CCCC-CCCC-CCCCCCCCCCCC'
$MeaslesVirusId = 'DDDDDDDD-DDDD-DDDD-DDDD-DDDDDDDDDDDD'

Write-Host "Step 1: Checking required columns..." -ForegroundColor Yellow
$diseaseColumns = Get-RequiredColumns -TableName "Diseases" -ConnectionString $connectionString
Write-Host "  Required Disease columns: $($diseaseColumns -join ', ')" -ForegroundColor Gray

# ============================================
# 1. CASE STATUSES
# ============================================
Write-Host ""
Write-Host "Step 2: Seeding Case Statuses..." -ForegroundColor Yellow

$statusExists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM CaseStatuses WHERE Id = 1" -ConnectionString $connectionString

if ($statusExists -eq 0) {
    $sql = @"
SET IDENTITY_INSERT CaseStatuses ON;
INSERT INTO CaseStatuses (Id, Name, Description, DisplayOrder, IsActive, ApplicableTo)
VALUES 
    (1, 'Laboratory Confirmed', 'Confirmed by laboratory testing', 1, 1, 3),
    (2, 'Probable', 'Meets clinical and epidemiological criteria', 2, 1, 3),
    (3, 'Suspect', 'Under investigation', 3, 1, 3),
    (4, 'Not a Case', 'Ruled out', 4, 1, 3);
SET IDENTITY_INSERT CaseStatuses OFF;
"@
    Invoke-SqlCommand -Query $sql -ConnectionString $connectionString
    Write-Host "  [OK] Case Statuses inserted" -ForegroundColor Green
}
else {
    Write-Host "  [OK] Case Statuses already exist" -ForegroundColor Green
}

# ============================================
# 2. DISEASES (using dynamic INSERT based on existing disease)
# ============================================
Write-Host ""
Write-Host "Step 3: Seeding Test Diseases..." -ForegroundColor Yellow

# Get a template disease to copy column structure
$templateQuery = "SELECT TOP 1 * FROM Diseases WHERE IsActive = 1"
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$command = $connection.CreateCommand()
$command.CommandText = $templateQuery

try {
    $connection.Open()
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset) | Out-Null

    if ($dataset.Tables[0].Rows.Count -gt 0) {
        $templateDisease = $dataset.Tables[0].Rows[0]
        Write-Host "  Found template disease: $($templateDisease['Name'])" -ForegroundColor Gray

        # Build column list (exclude identity and navigation properties)
        $columns = $dataset.Tables[0].Columns | Where-Object { 
            $_.ColumnName -notin @('ParentDisease', 'SubDiseases', 'DiseaseCategory') -and
            $_.ColumnName -notlike '*Navigation*'
        } | ForEach-Object { $_.ColumnName }

        Write-Host "  Using columns: $($columns -join ', ')" -ForegroundColor Gray
    }
}
finally {
    $connection.Close()
}

# Function to insert disease with all columns from template
function Insert-TestDisease {
    param(
        [string]$Id,
        [string]$Name,
        [string]$Code,
        [string]$ExportCode,
        [int]$Level,
        [string]$PathIds,
        [string]$ParentDiseaseId = $null,
        [string]$ConnectionString
    )

    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM Diseases WHERE Id = '$Id'" -ConnectionString $ConnectionString

    if ($exists -eq 0) {
        # Build INSERT with all boolean/bit columns set to sensible defaults
        $parentClause = if ($ParentDiseaseId) { "'$ParentDiseaseId'" } else { "NULL" }

        $sql = @"
INSERT INTO Diseases (
    Id, Name, Code, ExportCode, Level, PathIds, ParentDiseaseId,
    IsActive, DisplayOrder, AccessLevel, ExposureTrackingMode,
    DefaultToResidentialAddress, AlwaysPromptForLocation, SyncWithPatientAddressUpdates,
    InheritAddressSettingsFromParent, RequireGeographicCoordinates, AllowDomesticAcquisition,
    ReviewGroupingWindowHours, ReviewAutoQueueLabResults, ReviewAutoQueueExposures, ReviewAutoQueueContacts,
    ReviewAutoQueueConfirmationChanges, ReviewAutoQueueDiseaseChanges, ReviewAutoQueueClinicalNotifications,
    ReviewAutoQueueNewCases, ReviewDefaultPriority,
    CheckJurisdictionCrossing, CreatedAt
)
VALUES (
    '$Id', '$Name', '$Code', '$ExportCode', $Level, '$PathIds', $parentClause,
    1, 1, 0, 0,
    0, 0, 0,
    1, 0, 1,
    72, 1, 1, 1,
    1, 1, 1,
    1, 2,
    1, GETUTCDATE()
);
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $ConnectionString
        Write-Host "  [OK] $Name inserted" -ForegroundColor Green
    }
    else {
        Write-Host "  [OK] $Name already exists" -ForegroundColor Green
    }
}

# Insert test diseases
Insert-TestDisease -Id $COVID19Id -Name "COVID-19 Test" -Code "COVID19T" -ExportCode "COVID19T" -Level 1 -PathIds $COVID19Id -ConnectionString $connectionString
Insert-TestDisease -Id $InfluenzaId -Name "Influenza Test" -Code "FLUT" -ExportCode "FLUT" -Level 1 -PathIds $InfluenzaId -ConnectionString $connectionString
Insert-TestDisease -Id $InfluenzaAId -Name "Influenza A Test" -Code "FLUAT" -ExportCode "FLUAT" -Level 2 -PathIds "$InfluenzaId,$InfluenzaAId" -ParentDiseaseId $InfluenzaId -ConnectionString $connectionString
Insert-TestDisease -Id $InfluenzaBId -Name "Influenza B Test" -Code "FLUBT" -ExportCode "FLUBT" -Level 2 -PathIds "$InfluenzaId,$InfluenzaBId" -ParentDiseaseId $InfluenzaId -ConnectionString $connectionString
Insert-TestDisease -Id $MeaslesId -Name "Measles Test" -Code "MEASLEST" -ExportCode "MEASLEST" -Level 1 -PathIds $MeaslesId -ConnectionString $connectionString

# ============================================
# 3. SPECIMEN TYPES
# ============================================
Write-Host ""
Write-Host "Step 4: Seeding Specimen Types..." -ForegroundColor Yellow

$specimens = @(
    @{ Name='Nasopharyngeal swab'; SnomedCode='258500001'; Hl7Code='NPS'; LoincSystemCode='697989009'; IsInvasive=0; IsSterileSite=0 },
    @{ Name='Nasal swab'; SnomedCode='258411007'; Hl7Code='NS'; LoincSystemCode='697989009'; IsInvasive=0; IsSterileSite=0 },
    @{ Name='Oropharyngeal swab'; SnomedCode='258529004'; Hl7Code='OPS'; LoincSystemCode='123038009'; IsInvasive=0; IsSterileSite=0 },
    @{ Name='Blood'; SnomedCode='119297000'; Hl7Code='BLD'; LoincSystemCode='87612001'; IsInvasive=1; IsSterileSite=1 },
    @{ Name='Serum'; SnomedCode='119364003'; Hl7Code='SER'; LoincSystemCode='119364003'; IsInvasive=1; IsSterileSite=1 },
    @{ Name='Sputum'; SnomedCode='119334006'; Hl7Code='SPT'; LoincSystemCode='119334006'; IsInvasive=0; IsSterileSite=0 }
)

foreach ($spec in $specimens) {
    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM SpecimenTypes WHERE SnomedCode = '$($spec.SnomedCode)'" -ConnectionString $connectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO SpecimenTypes (Name, SnomedCode, Hl7Code, LoincSystemCode, IsInvasive, IsSterileSite, IsActive, DisplayOrder, CreatedAt)
VALUES ('$($spec.Name)', '$($spec.SnomedCode)', '$($spec.Hl7Code)', '$($spec.LoincSystemCode)', $($spec.IsInvasive), $($spec.IsSterileSite), 1, 1, GETUTCDATE());
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $connectionString
    }
}
Write-Host "  [OK] Specimen types seeded" -ForegroundColor Green

# ============================================
# 4. TEST METHODS
# ============================================
Write-Host ""
Write-Host "Step 5: Seeding Test Methods..." -ForegroundColor Yellow

$methods = @(
    @{ Name='PCR'; SnomedCode='258066000'; LoincMethodCode='PCR'; ExportCode='PCR' },
    @{ Name='RT-PCR'; SnomedCode='414464004'; LoincMethodCode='RT-PCR'; ExportCode='RTPCR' },
    @{ Name='Culture'; SnomedCode='252398009'; LoincMethodCode='CULTURE'; ExportCode='CULT' },
    @{ Name='Serology/ELISA'; SnomedCode='68793005'; LoincMethodCode='ELISA'; ExportCode='ELISA' },
    @{ Name='Rapid Antigen'; SnomedCode='108252007'; LoincMethodCode='ANTIGEN'; ExportCode='RAT' }
)

foreach ($method in $methods) {
    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM TestMethods WHERE SnomedCode = '$($method.SnomedCode)'" -ConnectionString $connectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO TestMethods (Name, SnomedCode, LoincMethodCode, ExportCode, IsActive, DisplayOrder)
VALUES ('$($method.Name)', '$($method.SnomedCode)', '$($method.LoincMethodCode)', '$($method.ExportCode)', 1, 1);
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $connectionString
    }
}
Write-Host "  [OK] Test methods seeded" -ForegroundColor Green

# ============================================
# 5. TEST RESULTS
# ============================================
Write-Host ""
Write-Host "Step 6: Seeding Test Results..." -ForegroundColor Yellow

$results = @(
    @{ Name='Positive'; SnomedCode='10828004'; Hl7Code='POS'; ExportCode='POS' },
    @{ Name='Negative'; SnomedCode='260385009'; Hl7Code='NEG'; ExportCode='NEG' },
    @{ Name='Detected'; SnomedCode='260373001'; Hl7Code='DET'; ExportCode='DET' },
    @{ Name='Not Detected'; SnomedCode='260415000'; Hl7Code='ND'; ExportCode='ND' }
)

foreach ($result in $results) {
    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM TestResults WHERE SnomedCode = '$($result.SnomedCode)'" -ConnectionString $connectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO TestResults (Name, SnomedCode, Hl7Code, ExportCode, IsActive, DisplayOrder)
VALUES ('$($result.Name)', '$($result.SnomedCode)', '$($result.Hl7Code)', '$($result.ExportCode)', 1, 1);
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $connectionString
    }
}
Write-Host "  [OK] Test results seeded" -ForegroundColor Green

# ============================================
# 6. PATHOGENS
# ============================================
Write-Host ""
Write-Host "Step 7: Seeding Pathogens..." -ForegroundColor Yellow

function Insert-Pathogen {
    param($Id, $Name, $ShortName, $LOINCCode, $DiseaseId, $ConnectionString)

    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM Pathogens WHERE LOINCCode = '$LOINCCode'" -ConnectionString $ConnectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO Pathogens (Id, Name, ShortName, LOINCCode, DiseaseId, Category, ResultType, IsActive, DisplayOrder, CreatedAt)
VALUES ('$Id', '$Name', '$ShortName', '$LOINCCode', '$DiseaseId', 1, 1, 1, 1, GETUTCDATE());
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $ConnectionString
        Write-Host "  [OK] $ShortName pathogen inserted" -ForegroundColor Green
        return $Id
    }
    else {
        $actualId = Invoke-SqlScalar -Query "SELECT Id FROM Pathogens WHERE LOINCCode = '$LOINCCode'" -ConnectionString $ConnectionString
        Write-Host "  [OK] $ShortName pathogen exists (reusing)" -ForegroundColor Green
        return $actualId
    }
}

$SARSCoV2Id = Insert-Pathogen -Id $SARSCoV2Id -Name "SARS-CoV-2 RNA Test" -ShortName "SARS-CoV-2" -LOINCCode "94500-6" -DiseaseId $COVID19Id -ConnectionString $connectionString
$InfluenzaAVirusId = Insert-Pathogen -Id $InfluenzaAVirusId -Name "Influenza A RNA Test" -ShortName "Flu A" -LOINCCode "92142-9" -DiseaseId $InfluenzaAId -ConnectionString $connectionString
$InfluenzaBVirusId = Insert-Pathogen -Id $InfluenzaBVirusId -Name "Influenza B RNA Test" -ShortName "Flu B" -LOINCCode "92141-1" -DiseaseId $InfluenzaBId -ConnectionString $connectionString
$MeaslesVirusId = Insert-Pathogen -Id $MeaslesVirusId -Name "Measles IgM Test" -ShortName "Measles IgM" -LOINCCode "22502-9" -DiseaseId $MeaslesId -ConnectionString $connectionString

# ============================================
# 7. GET ACTUAL IDs
# ============================================
Write-Host ""
Write-Host "Step 8: Retrieving lookup IDs..." -ForegroundColor Yellow

$NPSId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '258500001'" -ConnectionString $connectionString
$NSId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '258411007'" -ConnectionString $connectionString
$OPSId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '258529004'" -ConnectionString $connectionString
$BloodId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '119297000'" -ConnectionString $connectionString
$SerumId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '119364003'" -ConnectionString $connectionString
$SputumId = Invoke-SqlScalar -Query "SELECT Id FROM SpecimenTypes WHERE SnomedCode = '119334006'" -ConnectionString $connectionString

$PCRId = Invoke-SqlScalar -Query "SELECT Id FROM TestMethods WHERE SnomedCode = '258066000'" -ConnectionString $connectionString
$RTPCRId = Invoke-SqlScalar -Query "SELECT Id FROM TestMethods WHERE SnomedCode = '414464004'" -ConnectionString $connectionString
$ELISAId = Invoke-SqlScalar -Query "SELECT Id FROM TestMethods WHERE SnomedCode = '68793005'" -ConnectionString $connectionString

$PosId = Invoke-SqlScalar -Query "SELECT Id FROM TestResults WHERE SnomedCode = '10828004'" -ConnectionString $connectionString
$DetId = Invoke-SqlScalar -Query "SELECT Id FROM TestResults WHERE SnomedCode = '260373001'" -ConnectionString $connectionString

Write-Host "  [OK] Retrieved all lookup IDs" -ForegroundColor Green

# ============================================
# 8. CASE DEFINITIONS
# ============================================
Write-Host ""
Write-Host "Step 9: Seeding Case Definitions..." -ForegroundColor Yellow

function Insert-CaseDefinition {
    param($Name, $DiseaseId, $ConnectionString)

    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM CaseDefinitions WHERE Name = '$Name'" -ConnectionString $ConnectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO CaseDefinitions (Name, DiseaseId, ConfirmationStatusId, Status, DateActiveFrom, AllowAutoClassification, EnableAutoEvaluation, ApplyToChildDiseases, CreateReviewQueueOnChange, CreateReviewQueueOnSuggestion, CreatedAt)
VALUES ('$Name', '$DiseaseId', 1, 2, DATEADD(DAY, -30, GETUTCDATE()), 1, 1, 0, 0, 1, GETUTCDATE());
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $ConnectionString
        Write-Host "  [OK] $Name inserted" -ForegroundColor Green
    }

    return Invoke-SqlScalar -Query "SELECT Id FROM CaseDefinitions WHERE Name = '$Name'" -ConnectionString $ConnectionString
}

$COVID19DefId = Insert-CaseDefinition -Name "COVID-19 Lab Test Definition" -DiseaseId $COVID19Id -ConnectionString $connectionString
$FluADefId = Insert-CaseDefinition -Name "Influenza A Lab Test Definition" -DiseaseId $InfluenzaAId -ConnectionString $connectionString
$FluBDefId = Insert-CaseDefinition -Name "Influenza B Lab Test Definition" -DiseaseId $InfluenzaBId -ConnectionString $connectionString
$MeaslesDefId = Insert-CaseDefinition -Name "Measles Serology Test Definition" -DiseaseId $MeaslesId -ConnectionString $connectionString

# ============================================
# 9. CASE DEFINITION LAB CRITERIA
# ============================================
Write-Host ""
Write-Host "Step 10: Seeding Lab Criteria..." -ForegroundColor Yellow

function Insert-LabCriteria {
    param($CaseDefId, $SpecimenIds, $PathogenId, $MethodIds, $ResultIds, $CanonicalSpecimenId, $CanonicalPathogenId, $CanonicalMethodId, $CanonicalResultId, $ConnectionString)

    $exists = Invoke-SqlScalar -Query "SELECT COUNT(*) FROM CaseDefinitionLabCriteria WHERE CaseDefinitionId = $CaseDefId" -ConnectionString $ConnectionString
    if ($exists -eq 0) {
        $sql = @"
INSERT INTO CaseDefinitionLabCriteria (
    CaseDefinitionId, GroupNumber, LogicalOperator, IsRequired,
    AcceptableSpecimenTypesJson, AcceptablePathogensJson, AcceptableTestMethodsJson, AcceptableResultsJson,
    SpecimenStoragePreference, BiomarkerStoragePreference, TestMethodStoragePreference, ResultStoragePreference,
    CanonicalSpecimenTypeId, CanonicalPathogenId, CanonicalTestMethodId, CanonicalTestResultId,
    DisplayOrder, CreatedAt
)
VALUES (
    $CaseDefId, 1, 1, 1,
    '[$SpecimenIds]', '["$PathogenId"]', '[$MethodIds]', '[$ResultIds]',
    2, 2, 2, 2,
    $CanonicalSpecimenId, '$CanonicalPathogenId', $CanonicalMethodId, $CanonicalResultId,
    1, GETUTCDATE()
);
"@
        Invoke-SqlCommand -Query $sql -ConnectionString $ConnectionString
        Write-Host "  [OK] Lab criteria for case definition $CaseDefId inserted" -ForegroundColor Green
    }
    else {
        Write-Host "  [OK] Lab criteria for case definition $CaseDefId already exists" -ForegroundColor Green
    }
}

Insert-LabCriteria -CaseDefId $COVID19DefId -SpecimenIds "$NPSId,$NSId,$OPSId" -PathogenId $SARSCoV2Id -MethodIds "$RTPCRId" -ResultIds "$PosId,$DetId" -CanonicalSpecimenId $NPSId -CanonicalPathogenId $SARSCoV2Id -CanonicalMethodId $RTPCRId -CanonicalResultId $PosId -ConnectionString $connectionString
Insert-LabCriteria -CaseDefId $FluADefId -SpecimenIds "$NPSId,$NSId,$OPSId,$SputumId" -PathogenId $InfluenzaAVirusId -MethodIds "$PCRId,$RTPCRId" -ResultIds "$PosId,$DetId" -CanonicalSpecimenId $NPSId -CanonicalPathogenId $InfluenzaAVirusId -CanonicalMethodId $RTPCRId -CanonicalResultId $PosId -ConnectionString $connectionString
Insert-LabCriteria -CaseDefId $FluBDefId -SpecimenIds "$NPSId,$NSId,$OPSId,$SputumId" -PathogenId $InfluenzaBVirusId -MethodIds "$PCRId,$RTPCRId" -ResultIds "$PosId,$DetId" -CanonicalSpecimenId $NPSId -CanonicalPathogenId $InfluenzaBVirusId -CanonicalMethodId $RTPCRId -CanonicalResultId $PosId -ConnectionString $connectionString
Insert-LabCriteria -CaseDefId $MeaslesDefId -SpecimenIds "$BloodId,$SerumId" -PathogenId $MeaslesVirusId -MethodIds "$ELISAId" -ResultIds "$PosId" -CanonicalSpecimenId $SerumId -CanonicalPathogenId $MeaslesVirusId -CanonicalMethodId $ELISAId -CanonicalResultId $PosId -ConnectionString $connectionString

# ============================================
# DONE
# ============================================
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   SEED DATA COMPLETE [OK]" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Created:" -ForegroundColor Yellow
Write-Host "  - 5 Test Diseases (COVID-19, Influenza, Flu A, Flu B, Measles)" -ForegroundColor White
Write-Host "  - 6 Specimen Types" -ForegroundColor White
Write-Host "  - 5 Test Methods" -ForegroundColor White
Write-Host "  - 4 Test Results" -ForegroundColor White
Write-Host "  - 4 Pathogens" -ForegroundColor White
Write-Host "  - 4 Case Definitions with Lab Criteria" -ForegroundColor White
Write-Host ""
Write-Host "NEXT: Apply EF Core migration for text search fields" -ForegroundColor Yellow
Write-Host ""
