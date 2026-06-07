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

## File Writing — MANDATORY: Use fwrite MCP Server

**NEVER use the built-in `fsWrite`, `fsAppend`, or `strReplace` tools. ALWAYS use the `mcp_fwrite_*` MCP tools for every file write, append, and replace operation — no exceptions.**

The built-in tools have size limits that cause silent failures. The fwrite MCP server does not. Use it for everything.

### Available MCP Tools

| Tool | Purpose |
|------|---------|
| `mcp_fwrite_write_file` | Write/overwrite a file (atomic write with verification) |
| `mcp_fwrite_append_file` | Append to a file (creates if missing) |
| `mcp_fwrite_replace_in_file` | Replace a unique string in a file |
| `mcp_fwrite_delete_file` | Delete a file |
| `mcp_fwrite_delete_directory` | Delete a directory recursively |
| `mcp_fwrite_create_directory` | Create a directory (and parents) |
| `mcp_fwrite_batch_write` | Multiple operations atomically (all-or-nothing) |

### Path Convention

All paths are **relative to the workspace root** (`D:\projects\OuterEmpires2\OE2EmpireTracker`). Use forward slashes. Error messages include the resolved absolute path for debugging.

- Main project files: `OE2EmpireTracker/path/to/file.cs`
- Test project files: `OE2EmpireTracker.Tests/path/to/file.cs`
- Spec files (external): `spec/requirements/Feature.md`
- Kiro specs: `.kiro/specs/feature-name/design.md`
- Tools: `.kiro/tools/script.js`
- Steering: `.kiro/steering/file.md`

**CRITICAL**: `.kiro/` and `spec/` live at the workspace root. Do NOT prefix them with `OE2EmpireTracker/`. Only source code files under the `OE2EmpireTracker/` and `OE2EmpireTracker.Tests/` project folders get that prefix.

### Large File Writes — MANDATORY chunking

**Tool calls with very large `content` parameters will be aborted by the platform.** If the content exceeds ~2000 characters, split the write into multiple calls:

1. `mcp_fwrite_write_file` with the first chunk (header/first section)
2. `mcp_fwrite_append_file` for each subsequent chunk

Keep each chunk under 2000 characters to stay safely within limits. This applies to all file writes — source code, spec documents, test files, etc.

### write_file — Write or overwrite entire file
```
mcp_fwrite_write_file(path="OE2EmpireTracker/Models/NewFile.cs", content="file content here")
```

### append_file — Append to end of file
```
mcp_fwrite_append_file(path="OE2EmpireTracker/Models/File.cs", content="\n// appended content")
```

### replace_in_file — Find and replace unique string
```
mcp_fwrite_replace_in_file(path="OE2EmpireTracker/Models/File.cs", old_text="text to find", new_text="replacement text")
```
- `old_text` must appear exactly once in the file (fails otherwise)
- Idempotent: if `new_text` is already present, succeeds without changes
- Normalizes line endings for matching

### delete_file — Delete a single file
```
mcp_fwrite_delete_file(path="path/to/unwanted-file.tmp")
```

### delete_directory — Delete a directory recursively
```
mcp_fwrite_delete_directory(path="path/to/unwanted-dir")
```

### create_directory — Create directory (and parents)
```
mcp_fwrite_create_directory(path="path/to/new/dir")
```

### batch_write — Atomic multi-file operations
```
mcp_fwrite_batch_write(operations=[
  {"op": "write", "path": "path/to/file1.cs", "content": "full content"},
  {"op": "append", "path": "path/to/file2.cs", "content": "appended"},
  {"op": "replace", "path": "path/to/file3.cs", "old_text": "old", "new_text": "new"}
])
```
All operations succeed or all are rolled back.

## Banned Tools
- **Do NOT use `fsWrite`** — silent failures on large content. Use `mcp_fwrite_write_file` instead.
- **Do NOT use `fsAppend`** — silent failures on large content. Use `mcp_fwrite_append_file` instead.
- **Do NOT use `strReplace`** — silent failures on large content, parameter ordering bugs. Use `mcp_fwrite_replace_in_file` instead.
- **Do NOT use `deleteFile`** — Use `mcp_fwrite_delete_file` instead for consistent error reporting.
- **Do NOT use `semanticRename`** — does not work with old-style csproj / .NET Framework 4.8.1. Use `mcp_fwrite_replace_in_file` for manual find-and-replace.

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
- Test commands (vstest.console — full suite): `timeout: 960000` (16 minutes) — the full suite has slow property tests (ErrorIsolation ~3.5min, RateGovernorThroughput ~1.5min) that use real rate limiters
- Test commands (vstest.console — filtered subset): `timeout: 120000` (2 minutes)
- Backup script (oebackup.ps1): `timeout: 60000` (1 minute)
- commit.js: `timeout: 30000` (30 seconds)
- trxparse.js: `timeout: 10000` (10 seconds)
- Other quick commands: `timeout: 30000` (30 seconds)

Without a timeout, if a command hangs (SSH connection stalls, remote is slow), the tool waits indefinitely and the user has to manually exit the shell. Always set a timeout.

### Known Slow Tests

The full WinForms test suite takes ~14 minutes due to property tests that use real rate limiters:
- `ErrorIsolationPropertyTests.FailedItems_DoNotPrevent_OtherItems_FromCompleting` (~3.5 min) — runs 100 iterations of GameApiRequestQueue with real token-bucket rate limiting at 200 TPS, 5-50 items per iteration
- `ErrorIsolationPropertyTests.PartialFailure_StillCompletes_WithPositiveSucceeded` (part of the above)
- `GameApiRequestQueuePropertyTests.RateGovernorThroughput` (~1.5 min) — runs 100 iterations testing actual rate governor timing at 5-20 TPS

These tests use `SystemClock.Reset()` but the rate limiter's `SemaphoreSlim` waits are real wall-clock delays, not frozen-time delays. Consider refactoring to use a testable clock in the rate limiter to eliminate the 5-minute overhead.
