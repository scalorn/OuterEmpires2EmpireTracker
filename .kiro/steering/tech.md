# Tech Stack & Build

## Platform
- .NET Framework 4.8.1 (not .NET Core / .NET 5+)
- C# with Windows Forms (WinForms) UI
- Visual Studio solution (`OE2EmpireTracker.sln`)
- MSBuild-based `.csproj` (old-style, not SDK-style)

## Key Libraries
- Newtonsoft.Json 13.0.4 — JSON serialization for all persistence
- NLog 5.3.4 — logging (file + debugger targets, configured in `NLog.config`)
- Polly 7.2.4 — resilience and transient-fault handling (rate limiting, retry, circuit breaker) for future API integration
- Microsoft.Xml.SgmlReader — HTML parsing (survey data scraping)
- AWSSDK.Core — AWS integration (future/planned features)
- System.Text.Json — secondary JSON support

## Test Project
- NUnit 4.5.1 with NUnit3TestAdapter 6.2.0
- Microsoft Testing Platform 2.1.0
- Project: `OE2EmpireTracker.Tests`
- Test data in `OE2EmpireTracker.Tests/TestData/` (HTML files copied to output)

## Package Management
- NuGet via `packages.config` (not PackageReference)
- Packages restored to solution-level `packages/` folder

## Visual Studio Installation
- Visual Studio 2026 Community Edition (version 18)
- Install path: `D:\Program Files\Microsoft Visual Studio\18\Community`
- MSBuild path: `"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe"`
- vstest.console path: `"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"`
- **Always use absolute paths** to VS tools — `msbuild` and `vstest.console` are not on PATH

## Common Commands
```
# Build the solution
"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug

# Restore NuGet packages
nuget restore OE2EmpireTracker.sln

# Run tests via vstest with TRX output (dotnet test does NOT work with old-style csproj + packages.config)
"D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx

# Generate Code Metrics XML (build artifact, not committed to source control)
# Output: OE2EmpireTracker/OE2EmpireTracker.Metrics.xml
"D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /t:Metrics
```

## Important: Testing
- Do NOT use `dotnet test` — it is incompatible with old-style csproj and packages.config
- Use `vstest.console` against the built test DLL, or rely on `getDiagnostics` for compile checks
- **Always pass `/Logger:trx`** when running vstest.console — this writes a structured XML results file to `TestResults/` that is far more reliable to parse than console output
- After running tests, read the TRX file to check results: search for `outcome="Failed"` to find failures, and read the `<Message>` and `<StackTrace>` elements for details
- When the running app locks the exe, use `getDiagnostics` instead of building

## Important: Zero Warnings Policy
- The build MUST produce **zero warnings** — this includes all StyleCop SA* warnings (SA1201, SA1202, SA1500, etc.)
- StyleCop warnings are NOT cosmetic — they are enforced coding standards. Every SA* warning must be fixed before committing.
- Common StyleCop rules: SA1201 (member ordering by kind), SA1202 (member ordering by access), SA1500 (brace placement)
- After building, grep the output for "warning" and fix every one. Do not dismiss them as "pre-existing" or "style-only".

## Conventions
- Logging via `NLog.LogManager.GetCurrentClassLogger()` — use `Log.Info`, `Log.Debug`, `Log.Error`
- JSON persistence with `Newtonsoft.Json` (`JsonConvert.SerializeObject` / `DeserializeObject`)
- Singletons for context objects (`PlayerContext.getInstance()`, `EmpireContext.getInstance()`)
- Call `Reset()` on context singletons in test setup to ensure clean state
- WinForms data binding via `BindingList<T>` and `BindingSource`

## File Writing — MANDATORY: Use fwrite.js for ALL Edits

**NEVER use the built-in `fsWrite`, `fsAppend`, or `strReplace` tools. ALWAYS use `.kiro/tools/fwrite.js` for every file write, append, and replace operation — no exceptions, no size thresholds, no "small edit" carve-outs.**

The built-in tools have size limits that cause silent failures. fwrite.js does not. Use it for everything: one-line changes, full file rewrites, string replacements. There is no case where the built-in tools are acceptable.

### Replace (single string replacement — most common operation)
```powershell
@"
old text to find
"@ | Out-File -NoNewline -Encoding utf8 _old.tmp
@"
new text to replace with
"@ | Out-File -NoNewline -Encoding utf8 _new.tmp
node .kiro/tools/fwrite.js replace path/to/file.cs _old.tmp _new.tmp
```
The replace mode checks for uniqueness (fails if old string appears more than once) and auto-deletes the temp files after replacement.

### Write entire file
```powershell
@"
file content here
"@ | Out-File -NoNewline -Encoding utf8 _content.tmp
node .kiro/tools/fwrite.js writefile path/to/file.md _content.tmp
```

### Append to file
```powershell
@"
content to append
"@ | Out-File -NoNewline -Encoding utf8 _content.tmp
node .kiro/tools/fwrite.js appendfile path/to/file.md _content.tmp
```

### Stdin pipe (small content, under ~20 lines)
```powershell
@"
small content
"@ | node .kiro/tools/fwrite.js write path/to/file.md
```

**For content larger than ~20 lines, ALWAYS use the temp file approach** (`writefile`/`appendfile`/`replace`) instead of piping through stdin. Piping large heredocs through PowerShell stdin causes the shell to hang due to pipe buffering.

## Banned Tools
- **Do NOT use `fsWrite`** — silent failures on large content. Use fwrite.js writefile instead.
- **Do NOT use `fsAppend`** — silent failures on large content. Use fwrite.js appendfile instead.
- **Do NOT use `strReplace`** — silent failures on large content, parameter ordering bugs. Use fwrite.js replace instead.
- **Do NOT use `semanticRename`** — does not work with old-style csproj / .NET Framework 4.8.1. Use fwrite.js replace for manual find-and-replace.

## Test Results — Use trxparse.js

After running vstest.console, **always use trxparse.js** to check results instead of manually reading TRX XML:
```powershell
node .kiro/tools/trxparse.js
```
Auto-finds the most recent .trx file in TestResults/. Outputs pass/fail counts and failure details. Exit code 1 on failures.

## Git Commits — Use commit.js

**Always use commit.js** for git commits to avoid PowerShell quoting issues with multi-line messages:
```powershell
node .kiro/tools/commit.js "Summary line" "Body text" "Prompt: user said this"
```
Options:
- `--files ".kiro/tools/*.js,src/file.cs"` — stage specific files instead of `git add -A`
- `--no-add` — skip staging, only commit what's already staged
- `--stdin` — read body from stdin for very large bodies

The tool writes the message to a temp file and uses `git commit -F`, bypassing all shell escaping issues. Always run backup script after.

## Dependency Graph — Use depgraph.js

**Before making changes to a class**, use depgraph.js to assess blast radius:
``powershell
# Who depends on this class? (assess impact of changes)
node .kiro/tools/depgraph.js dependents ColonyStructure

# What does this class depend on? (understand context)
node .kiro/tools/depgraph.js dependencies ColonyService

# Both directions at once
node .kiro/tools/depgraph.js blast-radius LockTracking

# Find unreferenced types (dead code candidates)
node .kiro/tools/depgraph.js orphans
```r
Runs in ~2-3 seconds. Use before refactoring, renaming, or deleting classes to understand what will be affected.

## Command Execution — Timeouts

**ALWAYS set a `timeout` on `executePwsh` calls** to prevent commands from appearing to hang:
- Build commands (MSBuild): `timeout: 120000` (2 minutes)
- Test commands (vstest.console): `timeout: 180000` (3 minutes)
- Backup script (oebackup.ps1): `timeout: 60000` (1 minute)
- commit.js: `timeout: 30000` (30 seconds)
- trxparse.js: `timeout: 10000` (10 seconds)
- Other quick commands: `timeout: 30000` (30 seconds)

Without a timeout, if a command hangs (SSH connection stalls, remote is slow), the tool waits indefinitely and the user has to manually exit the shell. Always set a timeout.
