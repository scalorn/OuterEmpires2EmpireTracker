$ErrorActionPreference = "Stop"
$DeployDir = "T:\oe2server"
$PublishDir = "./publish/linux"
$ServerProject = "OE2EmpireTracker.Server/OE2EmpireTracker.Server.csproj"
$WebDir = "OE2EmpireTracker.Web"

Write-Host "=== OE2 Server Publish ===" -ForegroundColor Cyan
Write-Host ""

# 0. Verify we're in the right directory
if (-not (Test-Path $ServerProject)) {
    Write-Host "ERROR: Cannot find $ServerProject. Run this from the repo root." -ForegroundColor Red
    exit 1
}

# 1. Build the frontend
Write-Host "[1/5] Building frontend..." -ForegroundColor Yellow
Push-Location $WebDir
try {
    npm run build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Frontend build failed (exit code $LASTEXITCODE)" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Frontend build OK" -ForegroundColor Green
} finally {
    Pop-Location
}

# 2. Clean stale obj/bin to prevent cached builds
Write-Host "[2/5] Cleaning stale build artifacts..." -ForegroundColor Yellow
Remove-Item -Recurse -Force "OE2EmpireTracker.Server/obj/Release" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "OE2EmpireTracker.Server/bin/Release" -ErrorAction SilentlyContinue
Write-Host "  Clean OK" -ForegroundColor Green

# 3. Publish the server
Write-Host "[3/5] Publishing server (linux-x64, self-contained)..." -ForegroundColor Yellow
dotnet publish $ServerProject -c Release -r linux-x64 --self-contained true -o $PublishDir
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: dotnet publish failed (exit code $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

# Verify the DLL was actually produced
if (-not (Test-Path "$PublishDir/OE2EmpireTracker.Server.dll")) {
    Write-Host "ERROR: Published DLL not found at $PublishDir/OE2EmpireTracker.Server.dll" -ForegroundColor Red
    exit 1
}
$dll = Get-Item "$PublishDir/OE2EmpireTracker.Server.dll"
Write-Host "  Publish OK: $($dll.Length) bytes, $($dll.LastWriteTime)" -ForegroundColor Green

# 4. Copy to deploy directory
Write-Host "[4/5] Copying to $DeployDir..." -ForegroundColor Yellow
if (-not (Test-Path $DeployDir)) {
    Write-Host "ERROR: Deploy directory $DeployDir does not exist or is not mounted." -ForegroundColor Red
    exit 1
}
xcopy "$PublishDir\." "$DeployDir\." /e /h /r /c /y | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: xcopy failed (exit code $LASTEXITCODE)" -ForegroundColor Red
    exit 1
}

# Verify the deployed DLL matches
$deployed = Get-Item "$DeployDir/OE2EmpireTracker.Server.dll"
if ($deployed.Length -ne $dll.Length) {
    Write-Host "ERROR: Deployed DLL size mismatch (expected $($dll.Length), got $($deployed.Length))" -ForegroundColor Red
    exit 1
}
Write-Host "  Deploy OK: $($deployed.Length) bytes at $DeployDir" -ForegroundColor Green

# 5. Print summary with git commit for verification
Write-Host ""
Write-Host "[5/5] Publish complete." -ForegroundColor Green
$commitHash = git rev-parse HEAD 2>$null
$commitShort = if ($commitHash) { $commitHash.Substring(0, 8) } else { "unknown" }
$commitMsg = git log -1 --format="%s" 2>$null
Write-Host "  Commit: $commitShort ($commitMsg)" -ForegroundColor Cyan
Write-Host "  DLL:    $($deployed.LastWriteTime)" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Restart the server to pick up the new binary." -ForegroundColor Yellow
Write-Host ""
