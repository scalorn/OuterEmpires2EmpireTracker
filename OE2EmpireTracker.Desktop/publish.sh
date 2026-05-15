#!/bin/bash
# Publish OE2EmpireTracker.Desktop for Linux and Windows
# Run from the solution root directory

set -e

echo "=== Publishing OE2 Empire Tracker Desktop ==="
echo ""

# Clean previous publish output
rm -rf OE2EmpireTracker.Desktop/bin/publish

# Publish for Windows x64
echo "--- Publishing for Windows x64 ---"
dotnet publish OE2EmpireTracker.Desktop/OE2EmpireTracker.Desktop.csproj \
    -c Release \
    -r win-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o OE2EmpireTracker.Desktop/bin/publish/win-x64

echo ""

# Publish for Linux x64
echo "--- Publishing for Linux x64 ---"
dotnet publish OE2EmpireTracker.Desktop/OE2EmpireTracker.Desktop.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o OE2EmpireTracker.Desktop/bin/publish/linux-x64

echo ""
echo "=== Publish complete ==="
echo "Windows: OE2EmpireTracker.Desktop/bin/publish/win-x64/"
echo "Linux:   OE2EmpireTracker.Desktop/bin/publish/linux-x64/"
