#!/bin/bash
set -e

echo "Setting up environment..."

# Ensure scripts directory exists
mkdir -p scripts

# Install .NET 9.0 if not already available
if ! dotnet --list-sdks | grep -q "9.0"; then
    echo "Installing .NET 9.0 SDK..."
    # Check if dotnet-install.sh exists in root, else download it
    if [ -f "./dotnet-install.sh" ]; then
        ./dotnet-install.sh --channel 9.0
    else
        wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
        chmod +x dotnet-install.sh
        ./dotnet-install.sh --channel 9.0
    fi

    export DOTNET_ROOT=$HOME/.dotnet
    export PATH=$PATH:$HOME/.dotnet:$HOME/.dotnet/tools
else
    echo ".NET 9.0 SDK already installed."
fi

dotnet --version

echo "Cleaning solution..."
dotnet clean src/S7Tools.sln

echo "Restoring packages..."
dotnet restore src/S7Tools.sln

echo "Building solution..."
# Capture output to a log file for analysis
dotnet build src/S7Tools.sln --configuration Debug > build_output.log 2>&1

if [ $? -eq 0 ]; then
    echo "Build successful."
else
    echo "Build failed. Check build_output.log for details."
    cat build_output.log
    exit 1
fi

echo "Build warnings/errors:"
grep -E "warning|error" build_output.log || echo "No warnings or errors found."
