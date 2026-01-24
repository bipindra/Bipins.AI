##########################################################################
# This is the Cake bootstrapper script for PowerShell.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

$CAKE_VERSION = "3.1.0"
$DOTNET_VERSION = "8.0.x"

$PACKAGE_URI = "https://api.nuget.org/v3-flatcontainer/cake.tool/$CAKE_VERSION/cake.tool.$CAKE_VERSION.nupkg"
$TOOLS_DIR = Join-Path $PSScriptRoot "tools"
$CAKE_EXE = Join-Path $TOOLS_DIR "Cake/Cake.exe"
$CAKE_DLL = Join-Path $TOOLS_DIR "Cake/Cake.dll"

# Should we use the .NET Core version of Cake?
$USE_CORE = $true

# Should we show verbose output?
$SHOW_VERBOSE = $false

# Parse arguments.
$ScriptArgs = @()
$ScriptArgs = $args

# Make sure tools folder exists
if ((Test-Path $PSScriptRoot) -and !(Test-Path $TOOLS_DIR)) {
    New-Item -Path $TOOLS_DIR -Type directory | Out-Null
}

###########################################################################
# INSTALL .NET SDK
###########################################################################

Function Install-DotNetSdk {
    $env:DOTNET_INSTALL_DIR = "$env:USERPROFILE\.dotnet"

    if (!(Test-Path "$env:DOTNET_INSTALL_DIR\dotnet.exe")) {
        Write-Host "Installing .NET SDK..." -ForegroundColor Cyan
        $installScript = Join-Path $env:TEMP "install-dotnet.ps1"
        Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
        & $installScript -Version $DOTNET_VERSION -InstallDir "$env:DOTNET_INSTALL_DIR" -NoPath
        Remove-Item $installScript
    }

    $env:PATH = "$env:DOTNET_INSTALL_DIR;$env:PATH"
}

###########################################################################
# INSTALL CAKE
###########################################################################

Function Install-Cake {
    if ($USE_CORE) {
        if (!(Test-Path $CAKE_DLL)) {
            Write-Host "Installing Cake.CoreCLR $CAKE_VERSION..." -ForegroundColor Cyan
            dotnet tool install --tool-path $TOOLS_DIR Cake.Tool --version $CAKE_VERSION
        }
        $CAKE_EXE = "dotnet"
        $CAKE_DLL = Join-Path $TOOLS_DIR "dotnet-cake"
    } else {
        if (!(Test-Path $CAKE_EXE)) {
            Write-Host "Installing Cake $CAKE_VERSION..." -ForegroundColor Cyan
            $zipFile = Join-Path $TOOLS_DIR "cake.zip"
            $cakeDir = Join-Path $TOOLS_DIR "Cake"
            
            Invoke-WebRequest $PACKAGE_URI -OutFile $zipFile
            Expand-Archive $zipFile -DestinationPath $cakeDir -Force
            Remove-Item $zipFile
        }
    }
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

Write-Host "Preparing Cake..." -ForegroundColor Cyan

# Install .NET SDK if needed
if ($USE_CORE) {
    Install-DotNetSdk
}

# Install Cake
Install-Cake

Write-Host "Running build script..." -ForegroundColor Cyan

if ($USE_CORE) {
    & $CAKE_EXE $CAKE_DLL build.cake $ScriptArgs
} else {
    & $CAKE_EXE build.cake $ScriptArgs
}

exit $LASTEXITCODE
