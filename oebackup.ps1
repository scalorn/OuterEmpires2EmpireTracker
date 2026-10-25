#!/usr/bin/env pwsh
<#
    oebackup.ps1 - push the repository to every configured backup remote.

    Identity and user are resolved from ~/.ssh/config (Host entries for
    github.com, 54.242.161.0, 192.168.4.36). This script deliberately does
    NOT set GIT_SSH_COMMAND; the previous version did, with an over-escaped
    Windows path that silently broke every push.

    Exit codes:
      0  every reachable remote accepted the push
      1  a reachable remote failed
#>

# Deliberately NOT 'Stop': git writes warnings and progress to stderr, which
# Windows PowerShell 5.1 would otherwise turn into a thrown NativeCommandError.
# Success is determined from $LASTEXITCODE instead.
$ErrorActionPreference = 'Continue'

# --- locate git, since it is not always on PATH on a fresh machine ---
$gitCommand = Get-Command git -ErrorAction SilentlyContinue
$git = if ($gitCommand) { $gitCommand.Source } else { $null }
if (-not $git) {
    foreach ($candidate in @(
        'D:\Program Files\Git\cmd\git.exe',
        'C:\Program Files\Git\cmd\git.exe',
        "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe")) {
        if (Test-Path $candidate) { $git = $candidate; break }
    }
}
if (-not $git) {
    Write-Error 'git not found on PATH or in any known install location.'
    exit 1
}

# --- remotes, with the host/port used for the pre-flight reachability probe ---
$remotes = @(
    @{ Name = 'origin'; Host = 'github.com';   Port = 22; Label = 'GitHub' },
    @{ Name = 'backup'; Host = '54.242.161.0'; Port = 22; Label = 'internet server' },
    @{ Name = 'niabus'; Host = '192.168.4.36'; Port = 22; Label = 'LAN server' }
)

function Test-Reachable($hostname, $port, $timeoutMs = 5000) {
    $client = New-Object System.Net.Sockets.TcpClient
    try { return $client.ConnectAsync($hostname, $port).Wait($timeoutMs) }
    catch { return $false }
    finally { $client.Close() }
}

$failed = @()
$skipped = @()
$pushed = @()

foreach ($r in $remotes) {
    $name = $r.Name

    if (-not (Test-Reachable $r.Host $r.Port)) {
        Write-Host ("SKIP   {0,-8} ({1}) - unreachable at {2}:{3}" -f $name, $r.Label, $r.Host, $r.Port) -ForegroundColor DarkYellow
        $skipped += $name
        continue
    }

    Write-Host ("PUSH   {0,-8} ({1})" -f $name, $r.Label) -ForegroundColor Cyan
    $output = & $git push $name 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host ("OK     {0,-8}" -f $name) -ForegroundColor Green
        $pushed += $name
    }
    else {
        Write-Host ("FAILED {0,-8}" -f $name) -ForegroundColor Red
        $output | ForEach-Object {
            # PS 5.1 wraps some native stderr lines in ErrorRecord objects, which
            # render as the exception type name unless unwrapped.
            $line = if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.Exception.Message } else { [string]$_ }
            if ($line.Trim()) { Write-Host "         $line" -ForegroundColor Red }
        }
        $failed += $name
    }
}

Write-Host ''
Write-Host ('Pushed:  ' + (($pushed  -join ', ') -replace '^$', '(none)'))
Write-Host ('Skipped: ' + (($skipped -join ', ') -replace '^$', '(none)'))
Write-Host ('Failed:  ' + (($failed  -join ', ') -replace '^$', '(none)'))

if ($failed.Count -gt 0) {
    Write-Host 'Backup INCOMPLETE - a reachable remote rejected the push.' -ForegroundColor Red
    exit 1
}

if ($pushed.Count -eq 0) {
    Write-Host 'Backup INCOMPLETE - no remote was reachable.' -ForegroundColor Red
    exit 1
}

Write-Host 'Backup complete.' -ForegroundColor Green
exit 0
