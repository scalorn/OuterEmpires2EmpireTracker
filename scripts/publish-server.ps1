# Server Publish Script
# Produces self-contained single-file executables for Windows x64 and Linux x64.
# Output: publish/win-x64/ and publish/linux-x64/

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$publishRoot = Join-Path $repoRoot "publish"
$project = Join-Path (Join-Path $repoRoot "OE2EmpireTracker.Server") "OE2EmpireTracker.Server.csproj"

Write-Host "=== Server Publish ===" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration"
Write-Host "Output: $publishRoot"
Write-Host ""

# Clean previous output
if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
}

# Windows x64
Write-Host "Publishing win-x64..." -ForegroundColor Yellow
dotnet publish $project -c $Configuration -r win-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $publishRoot "win-x64")

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED: win-x64 publish" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Linux x64
Write-Host "Publishing linux-x64..." -ForegroundColor Yellow
dotnet publish $project -c $Configuration -r linux-x64 --self-contained `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o (Join-Path $publishRoot "linux-x64")

if ($LASTEXITCODE -ne 0) {
    Write-Host "FAILED: linux-x64 publish" -ForegroundColor Red
    exit 1
}
Write-Host "  Done." -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "=== Publish Complete ===" -ForegroundColor Cyan
$winExe = Join-Path (Join-Path $publishRoot "win-x64") "OE2EmpireTracker.Server.exe"
$linuxBin = Join-Path (Join-Path $publishRoot "linux-x64") "OE2EmpireTracker.Server"

if (Test-Path $winExe) {
    $winSize = [math]::Round((Get-Item $winExe).Length / 1MB, 1)
    Write-Host "  win-x64:   $winExe ($winSize MB)"
}
if (Test-Path $linuxBin) {
    $linuxSize = [math]::Round((Get-Item $linuxBin).Length / 1MB, 1)
    Write-Host "  linux-x64: $linuxBin ($linuxSize MB)"
}
