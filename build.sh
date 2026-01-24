#!/bin/bash

##########################################################################
# This is the Cake bootstrapper script for Linux/OS X.
# This file was downloaded from https://github.com/cake-build/resources
# Feel free to change this file to fit your needs.
##########################################################################

CAKE_VERSION="3.1.0"
DOTNET_VERSION="8.0.x"

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
TOOLS_DIR="$SCRIPT_DIR/tools"
CAKE_DLL="$TOOLS_DIR/dotnet-cake/dotnet-cake"

# Should we use the .NET Core version of Cake?
USE_CORE=true

# Should we show verbose output?
SHOW_VERBOSE=false

# Parse arguments.
SCRIPT_ARGS=()
for arg in "$@"; do
    SCRIPT_ARGS+=("$arg")
done

# Make sure tools folder exists
if [ ! -d "$TOOLS_DIR" ]; then
    mkdir -p "$TOOLS_DIR"
fi

###########################################################################
# INSTALL .NET SDK
###########################################################################

install_dotnet_sdk() {
    export DOTNET_INSTALL_DIR="$HOME/.dotnet"
    
    if [ ! -f "$DOTNET_INSTALL_DIR/dotnet" ]; then
        echo "Installing .NET SDK..."
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version "$DOTNET_VERSION" --install-dir "$DOTNET_INSTALL_DIR"
    fi
    
    export PATH="$DOTNET_INSTALL_DIR:$PATH"
}

###########################################################################
# INSTALL CAKE
###########################################################################

install_cake() {
    if [ "$USE_CORE" = true ]; then
        if [ ! -f "$CAKE_DLL" ]; then
            echo "Installing Cake.CoreCLR $CAKE_VERSION..."
            dotnet tool install --tool-path "$TOOLS_DIR" Cake.Tool --version "$CAKE_VERSION"
        fi
        CAKE_EXE="dotnet"
        CAKE_DLL="$TOOLS_DIR/dotnet-cake"
    else
        echo "Cake .NET Framework version is not supported on Linux/OS X"
        exit 1
    fi
}

###########################################################################
# RUN BUILD SCRIPT
###########################################################################

echo "Preparing Cake..."

# Install .NET SDK if needed
if [ "$USE_CORE" = true ]; then
    install_dotnet_sdk
fi

# Install Cake
install_cake

echo "Running build script..."

if [ "$USE_CORE" = true ]; then
    "$CAKE_EXE" "$CAKE_DLL" build.cake "${SCRIPT_ARGS[@]}"
else
    echo "Cake .NET Framework version is not supported on Linux/OS X"
    exit 1
fi

exit $?
