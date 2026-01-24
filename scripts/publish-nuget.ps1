# PowerShell script to publish Bipins.AI packages to NuGet.org
# Usage: .\scripts\publish-nuget.ps1 [-ApiKey <your-api-key>] [-Version <version>] [-SkipBuild]

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = $env:NUGET_API_KEY,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Bipins.AI NuGet Publishing Script ===" -ForegroundColor Cyan

# Check if API key is provided
if ([string]::IsNullOrWhiteSpace($ApiKey)) {
    Write-Host "ERROR: NuGet API key is required." -ForegroundColor Red
    Write-Host "Provide it via -ApiKey parameter or NUGET_API_KEY environment variable." -ForegroundColor Yellow
    Write-Host "Get your API key from: https://www.nuget.org/account/apikeys" -ForegroundColor Yellow
    exit 1
}

# Get version from git or use provided version
if ([string]::IsNullOrWhiteSpace($Version)) {
    $gitTag = git describe --tags --abbrev=0 2>$null
    if ($gitTag) {
        $Version = $gitTag.TrimStart('v')
        Write-Host "Using version from git tag: $Version" -ForegroundColor Green
    } else {
        # Try to get version from Directory.Build.props or use default
        $Version = "1.0.0"
        Write-Host "No git tag found, using default version: $Version" -ForegroundColor Yellow
    }
} else {
    Write-Host "Using provided version: $Version" -ForegroundColor Green
}

# Build solution if not skipped
if (-not $SkipBuild) {
    Write-Host "`nBuilding solution..." -ForegroundColor Cyan
    dotnet build Bipins.AI.sln --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed!" -ForegroundColor Red
        exit 1
    }
}

# Create artifacts directory
$artifactsDir = ".\artifacts"
if (Test-Path $artifactsDir) {
    Remove-Item $artifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $artifactsDir | Out-Null

# Pack all projects that should be published
Write-Host "`nPacking NuGet packages..." -ForegroundColor Cyan

$projectsToPack = @(
    "src\Bipins.AI.Core\Bipins.AI.Core.csproj",
    "src\Bipins.AI.Runtime\Bipins.AI.Runtime.csproj",
    "src\Bipins.AI.Ingestion\Bipins.AI.Ingestion.csproj",
    "src\Bipins.AI.Providers\Bipins.AI.Providers.OpenAI\Bipins.AI.Providers.OpenAI.csproj",
    "src\Bipins.AI.Providers\Bipins.AI.Providers.Anthropic\Bipins.AI.Providers.Anthropic.csproj",
    "src\Bipins.AI.Providers\Bipins.AI.Providers.AzureOpenAI\Bipins.AI.Providers.AzureOpenAI.csproj",
    "src\Bipins.AI.Providers\Bipins.AI.Providers.Bedrock\Bipins.AI.Providers.Bedrock.csproj",
    "src\Bipins.AI.Vectors\Bipins.AI.Vectors.Qdrant\Bipins.AI.Vectors.Qdrant.csproj",
    "src\Bipins.AI.Vectors\Bipins.AI.Vectors.Pinecone\Bipins.AI.Vectors.Pinecone.csproj",
    "src\Bipins.AI.Vectors\Bipins.AI.Vectors.Weaviate\Bipins.AI.Vectors.Weaviate.csproj",
    "src\Bipins.AI.Vectors\Bipins.AI.Vectors.Milvus\Bipins.AI.Vectors.Milvus.csproj"
)

$packedFiles = @()

foreach ($project in $projectsToPack) {
    if (Test-Path $project) {
        Write-Host "  Packing $project..." -ForegroundColor Gray
        
        $packArgs = @(
            "pack",
            $project,
            "--configuration", "Release",
            "--output", $artifactsDir,
            "--no-build"
        )
        
        if (-not [string]::IsNullOrWhiteSpace($Version)) {
            $packArgs += "--version-suffix"
            $packArgs += $Version
        }
        
        dotnet $packArgs
        
        if ($LASTEXITCODE -eq 0) {
            $nupkgFile = Get-ChildItem -Path $artifactsDir -Filter "*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
            if ($nupkgFile) {
                $packedFiles += $nupkgFile.FullName
                Write-Host "    ✓ Packed: $($nupkgFile.Name)" -ForegroundColor Green
            }
        } else {
            Write-Host "    ✗ Failed to pack $project" -ForegroundColor Red
        }
    } else {
        Write-Host "  ⚠ Skipping $project (not found)" -ForegroundColor Yellow
    }
}

if ($packedFiles.Count -eq 0) {
    Write-Host "`nNo packages were packed!" -ForegroundColor Red
    exit 1
}

Write-Host "`nPacked $($packedFiles.Count) package(s)" -ForegroundColor Green

# Publish to NuGet.org
if ($DryRun) {
    Write-Host "`n=== DRY RUN MODE ===" -ForegroundColor Yellow
    Write-Host "Would publish the following packages:" -ForegroundColor Yellow
    foreach ($file in $packedFiles) {
        Write-Host "  - $file" -ForegroundColor Gray
    }
    Write-Host "`nTo actually publish, run without -DryRun flag" -ForegroundColor Yellow
} else {
    Write-Host "`nPublishing to NuGet.org..." -ForegroundColor Cyan
    
    foreach ($nupkgFile in $packedFiles) {
        Write-Host "  Publishing $([System.IO.Path]::GetFileName($nupkgFile))..." -ForegroundColor Gray
        
        dotnet nuget push $nupkgFile `
            --api-key $ApiKey `
            --source https://api.nuget.org/v3/index.json `
            --skip-duplicate
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    ✓ Published successfully" -ForegroundColor Green
        } else {
            Write-Host "    ✗ Failed to publish" -ForegroundColor Red
        }
    }
    
    Write-Host "`n=== Publishing Complete ===" -ForegroundColor Green
    Write-Host "View packages at: https://www.nuget.org/profiles/$env:USERNAME" -ForegroundColor Cyan
}
