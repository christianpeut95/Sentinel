# Seed Lab Criteria for Test Case Definitions
$connectionString = "Server=(localdb)\mssqllocaldb;Database=aspnet-Sentinel-a57ac8ba-ccc6-4d7a-b380-485d37f1148d;Trusted_Connection=True;MultipleActiveResultSets=true"

function Invoke-SqlCommand {
    param([string]$Query)
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $Query
    $result = $command.ExecuteNonQuery()
    $connection.Close()
    return $result
}

function Invoke-SqlScalar {
    param([string]$Query)
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $Query
    $result = $command.ExecuteScalar()
    $connection.Close()
    return $result
}

Write-Host "Starting lab criteria seed..." -ForegroundColor Cyan

# First, ensure pathogens exist
Write-Host "`nCreating pathogens..." -ForegroundColor Yellow

$pathogens = @(
    @{Name='SARS-CoV-2'; Category='Virus'},
    @{Name='Influenza A virus'; Category='Virus'},
    @{Name='Influenza B virus'; Category='Virus'},
    @{Name='Measles virus'; Category='Virus'},
    @{Name='Measles IgM antibody'; Category='Antibody'}
)

$pathogenIds = @{}
foreach ($pathogen in $pathogens) {
    $existing = Invoke-SqlScalar "SELECT Id FROM Pathogens WHERE Name = '$($pathogen.Name)'"
    if ($existing) {
        Write-Host "  ✓ Pathogen '$($pathogen.Name)' already exists: $existing" -ForegroundColor Green
        $pathogenIds[$pathogen.Name] = $existing
    } else {
        $newId = [guid]::NewGuid().ToString()
        $pathogenName = $pathogen.Name
        $pathogenCategory = $pathogen.Category
        $sql = 'INSERT INTO Pathogens (Id, Name, Category, CreatedAt, IsDeleted, DiseaseId) VALUES (''' + $newId + ''', ''' + $pathogenName + ''', ''' + $pathogenCategory + ''', GETDATE(), 0, NULL)'
        Invoke-SqlCommand $sql
        Write-Host "  +" "Created pathogen '$pathogenName': $newId" -ForegroundColor Green
        $pathogenIds[$pathogen.Name] = $newId
    }
}

# Get lookup IDs
Write-Host "`nResolving lookup IDs..." -ForegroundColor Yellow

$specimenIds = @{
    'Nasopharyngeal Swab' = Invoke-SqlScalar "SELECT Id FROM SpecimenTypes WHERE Name = 'Nasopharyngeal Swab'"
    'Oropharyngeal swab' = Invoke-SqlScalar "SELECT Id FROM SpecimenTypes WHERE Name = 'Oropharyngeal swab'"
    'Serum' = Invoke-SqlScalar "SELECT Id FROM SpecimenTypes WHERE Name = 'Serum'"
}

$testMethodIds = @{
    'RT-PCR' = Invoke-SqlScalar "SELECT Id FROM TestMethods WHERE Name = 'RT-PCR'"
    'PCR' = Invoke-SqlScalar "SELECT Id FROM TestMethods WHERE Name = 'PCR'"
    'ELISA' = Invoke-SqlScalar "SELECT Id FROM TestMethods WHERE Name = 'ELISA'"
}

$testResultIds = @{
    'Detected' = Invoke-SqlScalar "SELECT Id FROM TestResults WHERE Name = 'Detected'"
    'Positive' = Invoke-SqlScalar "SELECT Id FROM TestResults WHERE Name = 'Positive'"
}

$caseDefIds = @{
    'COVID-19' = Invoke-SqlScalar "SELECT Id FROM CaseDefinitions WHERE Name = 'COVID-19 Lab Test Definition'"
    'Influenza A' = Invoke-SqlScalar "SELECT Id FROM CaseDefinitions WHERE Name = 'Influenza A Lab Test Definition'"
    'Influenza B' = Invoke-SqlScalar "SELECT Id FROM CaseDefinitions WHERE Name = 'Influenza B Lab Test Definition'"
    'Measles' = Invoke-SqlScalar "SELECT Id FROM CaseDefinitions WHERE Name = 'Measles Serology Test Definition'"
}

Write-Host "  Specimen Types: $($specimenIds.Count)" -ForegroundColor Gray
Write-Host "  Test Methods: $($testMethodIds.Count)" -ForegroundColor Gray
Write-Host "  Test Results: $($testResultIds.Count)" -ForegroundColor Gray
Write-Host "  Case Definitions: $($caseDefIds.Count)" -ForegroundColor Gray

# Now create lab criteria for each case definition
Write-Host "`nCreating lab criteria..." -ForegroundColor Yellow

# 1. COVID-19 Lab Criteria
# Nasopharyngeal swab + SARS-CoV-2 + RT-PCR + Detected
if ($caseDefIds['COVID-19']) {
    $existingCount = Invoke-SqlScalar "SELECT COUNT(*) FROM CaseDefinitionLabCriteria WHERE CaseDefinitionId = $($caseDefIds['COVID-19'])"
    if ($existingCount -eq 0) {
        $sql = 'INSERT INTO CaseDefinitionLabCriteria (CaseDefinitionId, AcceptableSpecimenTypesJson, SpecimenStoragePreference, CanonicalSpecimenTypeId, AcceptablePathogensJson, BiomarkerStoragePreference, CanonicalPathogenId, AcceptableTestMethodsJson, TestMethodStoragePreference, CanonicalTestMethodId, AcceptableResultsJson, ResultStoragePreference, CanonicalTestResultId, GroupNumber, LogicalOperator, IsRequired, RequireAllElementsMatch, DisplayOrder, Description, CreatedAt) VALUES (' + $caseDefIds['COVID-19'] + ', ''[' + $specimenIds['Nasopharyngeal Swab'] + ']'', 1, ' + $specimenIds['Nasopharyngeal Swab'] + ', ''["' + $pathogenIds['SARS-CoV-2'] + '"]'', 1, ''' + $pathogenIds['SARS-CoV-2'] + ''', ''[' + $testMethodIds['RT-PCR'] + ']'', 1, ' + $testMethodIds['RT-PCR'] + ', ''[' + $testResultIds['Detected'] + ']'', 1, ' + $testResultIds['Detected'] + ', 1, 0, 1, 1, 1, ''COVID-19 RT-PCR Detection from Nasopharyngeal Swab'', GETDATE())'
        Invoke-SqlCommand $sql
        Write-Host "  ✓ COVID-19 lab criteria created" -ForegroundColor Green
    } else {
        Write-Host "  - COVID-19 lab criteria already exists" -ForegroundColor Gray
    }
}

# 2. Influenza A Lab Criteria
# Nasopharyngeal swab + Influenza A virus + RT-PCR + Detected
if ($caseDefIds['Influenza A']) {
    $existingCount = Invoke-SqlScalar "SELECT COUNT(*) FROM CaseDefinitionLabCriteria WHERE CaseDefinitionId = $($caseDefIds['Influenza A'])"
    if ($existingCount -eq 0) {
        $sql = 'INSERT INTO CaseDefinitionLabCriteria (CaseDefinitionId, AcceptableSpecimenTypesJson, SpecimenStoragePreference, CanonicalSpecimenTypeId, AcceptablePathogensJson, BiomarkerStoragePreference, CanonicalPathogenId, AcceptableTestMethodsJson, TestMethodStoragePreference, CanonicalTestMethodId, AcceptableResultsJson, ResultStoragePreference, CanonicalTestResultId, GroupNumber, LogicalOperator, IsRequired, RequireAllElementsMatch, DisplayOrder, Description, CreatedAt) VALUES (' + $caseDefIds['Influenza A'] + ', ''[' + $specimenIds['Nasopharyngeal Swab'] + ']'', 1, ' + $specimenIds['Nasopharyngeal Swab'] + ', ''["' + $pathogenIds['Influenza A virus'] + '"]'', 1, ''' + $pathogenIds['Influenza A virus'] + ''', ''[' + $testMethodIds['RT-PCR'] + ']'', 1, ' + $testMethodIds['RT-PCR'] + ', ''[' + $testResultIds['Detected'] + ']'', 1, ' + $testResultIds['Detected'] + ', 1, 0, 1, 1, 1, ''Influenza A RT-PCR Detection from Nasopharyngeal Swab'', GETDATE())'
        Invoke-SqlCommand $sql
        Write-Host "  ✓ Influenza A lab criteria created" -ForegroundColor Green
    } else {
        Write-Host "  - Influenza A lab criteria already exists" -ForegroundColor Gray
    }
}

# 3. Influenza B Lab Criteria
# Oropharyngeal swab + Influenza B virus + RT-PCR + Detected
if ($caseDefIds['Influenza B']) {
    $existingCount = Invoke-SqlScalar "SELECT COUNT(*) FROM CaseDefinitionLabCriteria WHERE CaseDefinitionId = $($caseDefIds['Influenza B'])"
    if ($existingCount -eq 0) {
        $sql = 'INSERT INTO CaseDefinitionLabCriteria (CaseDefinitionId, AcceptableSpecimenTypesJson, SpecimenStoragePreference, CanonicalSpecimenTypeId, AcceptablePathogensJson, BiomarkerStoragePreference, CanonicalPathogenId, AcceptableTestMethodsJson, TestMethodStoragePreference, CanonicalTestMethodId, AcceptableResultsJson, ResultStoragePreference, CanonicalTestResultId, GroupNumber, LogicalOperator, IsRequired, RequireAllElementsMatch, DisplayOrder, Description, CreatedAt) VALUES (' + $caseDefIds['Influenza B'] + ', ''[' + $specimenIds['Oropharyngeal swab'] + ']'', 1, ' + $specimenIds['Oropharyngeal swab'] + ', ''["' + $pathogenIds['Influenza B virus'] + '"]'', 1, ''' + $pathogenIds['Influenza B virus'] + ''', ''[' + $testMethodIds['RT-PCR'] + ']'', 1, ' + $testMethodIds['RT-PCR'] + ', ''[' + $testResultIds['Detected'] + ']'', 1, ' + $testResultIds['Detected'] + ', 1, 0, 1, 1, 1, ''Influenza B RT-PCR Detection from Oropharyngeal Swab'', GETDATE())'
        Invoke-SqlCommand $sql
        Write-Host "  ✓ Influenza B lab criteria created" -ForegroundColor Green
    } else {
        Write-Host "  - Influenza B lab criteria already exists" -ForegroundColor Gray
    }
}

# 4. Measles Lab Criteria
# Serum + Measles IgM antibody + ELISA + Positive
if ($caseDefIds['Measles']) {
    $existingCount = Invoke-SqlScalar "SELECT COUNT(*) FROM CaseDefinitionLabCriteria WHERE CaseDefinitionId = $($caseDefIds['Measles'])"
    if ($existingCount -eq 0) {
        $sql = 'INSERT INTO CaseDefinitionLabCriteria (CaseDefinitionId, AcceptableSpecimenTypesJson, SpecimenStoragePreference, CanonicalSpecimenTypeId, AcceptablePathogensJson, BiomarkerStoragePreference, CanonicalPathogenId, AcceptableTestMethodsJson, TestMethodStoragePreference, CanonicalTestMethodId, AcceptableResultsJson, ResultStoragePreference, CanonicalTestResultId, GroupNumber, LogicalOperator, IsRequired, RequireAllElementsMatch, DisplayOrder, Description, CreatedAt) VALUES (' + $caseDefIds['Measles'] + ', ''[' + $specimenIds['Serum'] + ']'', 1, ' + $specimenIds['Serum'] + ', ''["' + $pathogenIds['Measles IgM antibody'] + '"]'', 1, ''' + $pathogenIds['Measles IgM antibody'] + ''', ''[' + $testMethodIds['ELISA'] + ']'', 1, ' + $testMethodIds['ELISA'] + ', ''[' + $testResultIds['Positive'] + ']'', 1, ' + $testResultIds['Positive'] + ', 1, 0, 1, 1, 1, ''Measles IgM Antibody Detection by ELISA from Serum'', GETDATE())'
        Invoke-SqlCommand $sql
        Write-Host "  ✓ Measles lab criteria created" -ForegroundColor Green
    } else {
        Write-Host "  - Measles lab criteria already exists" -ForegroundColor Gray
    }
}

Write-Host "`n✓ Lab criteria seeding complete!" -ForegroundColor Cyan

# Verify results
Write-Host "`nVerification:" -ForegroundColor Yellow
$totalCriteria = Invoke-SqlScalar "SELECT COUNT(*) FROM CaseDefinitionLabCriteria"
Write-Host "  Total lab criteria: $totalCriteria" -ForegroundColor Gray

Write-Host "  By case definition:" -ForegroundColor Gray
$connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
$connection.Open()
$command = $connection.CreateCommand()
$command.CommandText = "SELECT cd.Name, COUNT(lc.Id) as CriteriaCount FROM CaseDefinitions cd LEFT JOIN CaseDefinitionLabCriteria lc ON cd.Id = lc.CaseDefinitionId WHERE cd.Id IN ($($caseDefIds['COVID-19']), $($caseDefIds['Influenza A']), $($caseDefIds['Influenza B']), $($caseDefIds['Measles'])) GROUP BY cd.Name ORDER BY cd.Name"
$reader = $command.ExecuteReader()
while($reader.Read()) {
    Write-Host "    - $($reader['Name']): $($reader['CriteriaCount']) criteria" -ForegroundColor Gray
}
$reader.Close()
$connection.Close()
