# PowerShell script to publish Bipins.AI NuGet package
# Usage: .\meta\publish-nuget.ps1 [-ApiKey <your-api-key>] [-SourceUrl <package-server-url>] [-SkipBuild]

param(
    [string]$ApiKey = $env:PACKAGE_API_KEY,
    [string]$SourceUrl = $env:PACKAGE_SOURCE_URL,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Defaults
if ([string]::IsNullOrWhiteSpace($SourceUrl)) {
    $SourceUrl = "https://api.nuget.org/v3/index.json"
}

if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "ERROR: API key is required. Set -ApiKey parameter or PACKAGE_API_KEY environment variable." -ForegroundColor Red
    exit 1
}

Write-Host "=== Publishing Bipins.AI NuGet Package ===" -ForegroundColor Cyan

# Build
if (-not $SkipBuild) {
    Write-Host "Building solution..." -ForegroundColor Cyan
    dotnet build Bipins.AI.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Pack
Write-Host "Packing NuGet package..." -ForegroundColor Cyan
$projectPath = "src\Bipins.AI\Bipins.AI.csproj"
$artifactsDir = ".\artifacts"

if (Test-Path $artifactsDir) {
    Remove-Item $artifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactsDir | Out-Null

dotnet pack $projectPath --configuration Release --output $artifactsDir --no-build

if ($LASTEXITCODE -ne 0) {
    Write-Host "Pack failed!" -ForegroundColor Red
    exit 1
}

# Find the .nupkg file
$nupkgFile = Get-ChildItem -Path $artifactsDir -Filter "*.nupkg" | Select-Object -First 1

if (-not $nupkgFile) {
    Write-Host "No .nupkg file found!" -ForegroundColor Red
    exit 1
}

Write-Host "Packed: $($nupkgFile.Name)" -ForegroundColor Green

# Publish
Write-Host "Publishing to $SourceUrl..." -ForegroundColor Cyan
dotnet nuget push $nupkgFile.FullName --source $SourceUrl --api-key $ApiKey --skip-duplicate

if ($LASTEXITCODE -eq 0) {
    Write-Host "Published successfully!" -ForegroundColor Green
} else {
    Write-Host "Failed to publish" -ForegroundColor Red
    exit 1
}
