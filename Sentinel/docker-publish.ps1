# Docker Hub Publish Script for Sentinel
# This script builds and pushes both main and demo versions to Docker Hub

param(
    [Parameter(Mandatory=$true)]
    [string]$DockerHubUsername,
    
    [Parameter(Mandatory=$false)]
    [string]$ImageName = "sentinel",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipLogin
)

$ErrorActionPreference = "Stop"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  Sentinel Docker Hub Publishing Script" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# Login to Docker Hub (unless skipped)
if (-not $SkipLogin) {
    Write-Host "?? Logging in to Docker Hub..." -ForegroundColor Yellow
    docker login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Docker Hub login failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "? Logged in successfully!" -ForegroundColor Green
    Write-Host ""
}

# Get current branch
$currentBranch = git branch --show-current
Write-Host "Current branch: $currentBranch" -ForegroundColor Cyan
Write-Host ""

# Function to build and push image
function Build-And-Push {
    param(
        [string]$Branch,
        [string]$Tag,
        [string]$Username,
        [string]$Image
    )
    
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "  Building $Tag version" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    
    # Checkout branch
    Write-Host "?? Switching to $Branch branch..." -ForegroundColor Yellow
    git checkout $Branch
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Failed to checkout $Branch!" -ForegroundColor Red
        return $false
    }
    
    # Build Docker image
    $fullTag = "${Username}/${Image}:${Tag}"
    Write-Host "?? Building Docker image: $fullTag" -ForegroundColor Yellow
    Write-Host "This may take 5-10 minutes..." -ForegroundColor Gray
    
    docker build -t $fullTag -f ./Dockerfile ./Sentinel
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Docker build failed for $Tag!" -ForegroundColor Red
        return $false
    }
    Write-Host "? Build successful!" -ForegroundColor Green
    Write-Host ""
    
    # Push to Docker Hub
    Write-Host "?? Pushing to Docker Hub: $fullTag" -ForegroundColor Yellow
    docker push $fullTag
    if ($LASTEXITCODE -ne 0) {
        Write-Host "? Docker push failed for $Tag!" -ForegroundColor Red
        return $false
    }
    Write-Host "? Push successful!" -ForegroundColor Green
    Write-Host ""
    
    return $true
}

# Build and push MAIN version (from master branch)
Write-Host ""
Write-Host "?? Starting MAIN version build..." -ForegroundColor Magenta
if (-not (Build-And-Push -Branch "master" -Tag "latest" -Username $DockerHubUsername -Image $ImageName)) {
    Write-Host "?? Main version failed, stopping here." -ForegroundColor Red
    exit 1
}

# Also tag as version number if needed
Write-Host "??? Creating version tag (main)..." -ForegroundColor Yellow
$mainVersionTag = "${DockerHubUsername}/${ImageName}:main"
docker tag "${DockerHubUsername}/${ImageName}:latest" $mainVersionTag
docker push $mainVersionTag
Write-Host "? Main version also tagged as 'main'" -ForegroundColor Green
Write-Host ""

# Build and push DEMO version (from demo branch)
Write-Host ""
Write-Host "?? Starting DEMO version build..." -ForegroundColor Magenta
if (-not (Build-And-Push -Branch "demo" -Tag "demo" -Username $DockerHubUsername -Image $ImageName)) {
    Write-Host "?? Demo version failed!" -ForegroundColor Red
    git checkout $currentBranch
    exit 1
}

# Return to original branch
Write-Host "?? Returning to original branch: $currentBranch" -ForegroundColor Yellow
git checkout $currentBranch

Write-Host ""
Write-Host "==================================================" -ForegroundColor Green
Write-Host "  ? All images published successfully!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "?? Published images:" -ForegroundColor Cyan
Write-Host "  • ${DockerHubUsername}/${ImageName}:latest (main/production)" -ForegroundColor White
Write-Host "  • ${DockerHubUsername}/${ImageName}:main (main/production)" -ForegroundColor White
Write-Host "  • ${DockerHubUsername}/${ImageName}:demo (demo version)" -ForegroundColor White
Write-Host ""
Write-Host "?? View on Docker Hub:" -ForegroundColor Cyan
Write-Host "  https://hub.docker.com/r/${DockerHubUsername}/${ImageName}" -ForegroundColor Blue
Write-Host ""
Write-Host "?? To run:" -ForegroundColor Cyan
Write-Host "  Main:  docker run -p 8080:8080 ${DockerHubUsername}/${ImageName}:latest" -ForegroundColor White
Write-Host "  Demo:  docker run -p 8080:8080 ${DockerHubUsername}/${ImageName}:demo" -ForegroundColor White
Write-Host ""
