[CmdletBinding()]
param(
    [string]$AssetsDirectory = (Join-Path $HOME "Sentinel\surveyjs-creator-assets"),
    [string]$ComposeDirectory,
    [switch]$AcceptSurveyJsCreatorTerms,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ComposeDirectory)) {
    $ComposeDirectory = Split-Path -Parent $PSScriptRoot
}

# Keep the Creator packages compatible with the SurveyJS Form Library version
# shipped with Sentinel. This script deliberately fetches the files at setup
# time rather than adding Creator assets to the Sentinel repository or image.
$creatorVersion = "1.9.131"
$creatorTermsUrl = "https://surveyjs.io/survey-creator/documentation/overview"

if (-not $AcceptSurveyJsCreatorTerms) {
    throw @"
SurveyJS Creator is a separately licensed visual survey-design component.
Review its licence and ensure you have the necessary rights before continuing:
$creatorTermsUrl

This helper downloads Creator assets directly from the public package source;
it does not include them in Sentinel. Run again with -AcceptSurveyJsCreatorTerms
only after you have reviewed and accepted the applicable terms.
"@
}

$composeDirectoryPath = [System.IO.Path]::GetFullPath($ComposeDirectory)
$composeFile = Join-Path $composeDirectoryPath "docker-compose.yml"
if (-not (Test-Path -LiteralPath $composeFile -PathType Leaf)) {
    throw "Could not find docker-compose.yml in '$composeDirectoryPath'. Specify -ComposeDirectory with the Sentinel deployment folder."
}

$assetsDirectoryPath = [System.IO.Path]::GetFullPath($AssetsDirectory)
$downloads = @(
    @{ Package = "survey-creator-core"; File = "survey-creator-core.min.css" },
    @{ Package = "survey-creator-core"; File = "survey-creator-core.min.js" },
    @{ Package = "survey-creator-knockout"; File = "survey-creator-knockout.min.js" }
)

foreach ($download in $downloads) {
    $destinationDirectory = Join-Path $assetsDirectoryPath $download.Package
    $destinationFile = Join-Path $destinationDirectory $download.File
    $sourceUrl = "https://unpkg.com/$($download.Package)@$creatorVersion/$($download.File)"

    if ((Test-Path -LiteralPath $destinationFile -PathType Leaf) -and -not $Force) {
        Write-Host "Keeping existing $destinationFile"
        continue
    }

    New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    $temporaryFile = "$destinationFile.download"
    Remove-Item -LiteralPath $temporaryFile -Force -ErrorAction SilentlyContinue

    Write-Host "Downloading $($download.Package)/$($download.File)..."
    Invoke-WebRequest -Uri $sourceUrl -OutFile $temporaryFile

    if ((Get-Item -LiteralPath $temporaryFile).Length -eq 0) {
        Remove-Item -LiteralPath $temporaryFile -Force
        throw "The download for '$sourceUrl' was empty."
    }

    Move-Item -LiteralPath $temporaryFile -Destination $destinationFile -Force
}

$envFile = Join-Path $composeDirectoryPath ".env"
$envExample = Join-Path $composeDirectoryPath ".env.example"
if (-not (Test-Path -LiteralPath $envFile -PathType Leaf)) {
    if (-not (Test-Path -LiteralPath $envExample -PathType Leaf)) {
        throw "Neither .env nor .env.example exists in '$composeDirectoryPath'."
    }

    Copy-Item -LiteralPath $envExample -Destination $envFile
    Write-Warning "Created .env from .env.example. Complete its database password and hostname settings before starting Docker."
}

$envLines = [System.Collections.Generic.List[string]]::new()
foreach ($line in Get-Content -LiteralPath $envFile) {
    $envLines.Add($line)
}

$assetsDirectoryForEnv = $assetsDirectoryPath.Replace("\", "/")
$setting = "SURVEYJS_CREATOR_ASSETS_DIR=$assetsDirectoryForEnv"
$settingIndex = -1
for ($index = 0; $index -lt $envLines.Count; $index++) {
    if ($envLines[$index] -match "^\s*SURVEYJS_CREATOR_ASSETS_DIR\s*=") {
        $settingIndex = $index
        break
    }
}

if ($settingIndex -ge 0) {
    $envLines[$settingIndex] = $setting
}
else {
    if ($envLines.Count -gt 0 -and -not [string]::IsNullOrWhiteSpace($envLines[$envLines.Count - 1])) {
        $envLines.Add("")
    }
    $envLines.Add("# Optional locally managed SurveyJS Creator assets")
    $envLines.Add($setting)
}

[System.IO.File]::WriteAllLines($envFile, $envLines, [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "Survey designer assets are ready in: $assetsDirectoryPath"
Write-Host "Updated: $envFile"
Write-Host ""
Write-Host "Start Sentinel with the optional designer override:"
Write-Host "docker compose -f docker-compose.yml -f docker-compose.survey-designer-demo.yml up -d"
