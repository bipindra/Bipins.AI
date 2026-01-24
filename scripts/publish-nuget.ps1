# PowerShell script to publish Bipins.AI packages to a NuGet package server
# Usage: .\scripts\publish-nuget.ps1 [-ApiKey <your-api-key>] [-SourceUrl <package-server-url>] [-Version <version>] [-SkipBuild] [-DryRun]

param(
    [Parameter(Mandatory=$false)]
    [string]$ApiKey = $env:PACKAGE_API_KEY,
    
    [Parameter(Mandatory=$false)]
    [string]$SourceUrl = $env:PACKAGE_SOURCE_URL,
    
    [Parameter(Mandatory=$false)]
    [string]$SourceName = $env:PACKAGE_SOURCE_NAME,
    
    [Parameter(Mandatory=$false)]
    [string]$SourceUsername = $env:PACKAGE_SOURCE_USERNAME,
    
    [Parameter(Mandatory=$false)]
    [string]$Version = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipBuild = $false,
    
    [Parameter(Mandatory=$false)]
    [switch]$DryRun = $false
)

$ErrorActionPreference = "Stop"

Write-Host "=== Bipins.AI Package Publishing Script ===" -ForegroundColor Cyan

# Set default source URL if not provided
if ([string]::IsNullOrWhiteSpace($SourceUrl)) {
    $SourceUrl = "https://api.nuget.org/v3/index.json"
    Write-Host "Using default NuGet.org source" -ForegroundColor Gray
} else {
    Write-Host "Using package source: $SourceUrl" -ForegroundColor Green
}

# Set default source name if not provided
if ([string]::IsNullOrWhiteSpace($SourceName)) {
    $SourceName = "default"
}

# Check if API key is provided (unless dry run)
if ([string]::IsNullOrWhiteSpace($ApiKey) -and -not $DryRun) {
    Write-Host "ERROR: Package API key is required." -ForegroundColor Red
    Write-Host "Provide it via -ApiKey parameter or PACKAGE_API_KEY environment variable." -ForegroundColor Yellow
    Write-Host "For NuGet.org, get your API key from: https://www.nuget.org/account/apikeys" -ForegroundColor Yellow
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

# Add custom source if needed (and not NuGet.org)
if (-not $DryRun -and $SourceUrl -ne "https://api.nuget.org/v3/index.json" -and -not [string]::IsNullOrWhiteSpace($SourceUrl)) {
    Write-Host "`nAdding package source: $SourceUrl" -ForegroundColor Cyan
    
    $addSourceArgs = @(
        "nuget", "add", "source", $SourceUrl,
        "--name", $SourceName
    )
    
    if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
        $addSourceArgs += "--password", $ApiKey
    }
    
    if (-not [string]::IsNullOrWhiteSpace($SourceUsername)) {
        $addSourceArgs += "--username", $SourceUsername
    }
    
    $addSourceArgs += "--store-password-in-clear-text"
    
    dotnet $addSourceArgs 2>&1 | Out-Null
}

# Publish packages
if ($DryRun) {
    Write-Host "`n=== DRY RUN MODE ===" -ForegroundColor Yellow
    Write-Host "Would publish the following packages to $SourceUrl:" -ForegroundColor Yellow
    foreach ($file in $packedFiles) {
        Write-Host "  - $file" -ForegroundColor Gray
    }
    Write-Host "`nTo actually publish, run without -DryRun flag" -ForegroundColor Yellow
} else {
    Write-Host "`nPublishing to $SourceUrl..." -ForegroundColor Cyan
    
    foreach ($nupkgFile in $packedFiles) {
        Write-Host "  Publishing $([System.IO.Path]::GetFileName($nupkgFile))..." -ForegroundColor Gray
        
        $pushArgs = @(
            "nuget", "push", $nupkgFile,
            "--source", $SourceUrl,
            "--skip-duplicate"
        )
        
        if (-not [string]::IsNullOrWhiteSpace($ApiKey)) {
            $pushArgs += "--api-key", $ApiKey
        }
        
        dotnet $pushArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "    ✓ Published successfully" -ForegroundColor Green
        } else {
            Write-Host "    ✗ Failed to publish" -ForegroundColor Red
        }
    }
    
    Write-Host "`n=== Publishing Complete ===" -ForegroundColor Green
    if ($SourceUrl -eq "https://api.nuget.org/v3/index.json") {
        Write-Host "View packages at: https://www.nuget.org/profiles/$env:USERNAME" -ForegroundColor Cyan
    } else {
        Write-Host "Packages published to: $SourceUrl" -ForegroundColor Cyan
    }
}
