# Tech Stack & Build

## Platform
- .NET Framework 4.8.1 (not .NET Core / .NET 5+)
- C# with Windows Forms (WinForms) UI
- Visual Studio solution (`OE2EmpireTracker.sln`)
- MSBuild-based `.csproj` (old-style, not SDK-style)

## Key Libraries
- Newtonsoft.Json 13.0.4 — JSON serialization for all persistence
- NLog 5.3.4 — logging (file + debugger targets, configured in `NLog.config`)
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
```

## Important: Testing
- Do NOT use `dotnet test` — it is incompatible with old-style csproj and packages.config
- Use `vstest.console` against the built test DLL, or rely on `getDiagnostics` for compile checks
- **Always pass `/Logger:trx`** when running vstest.console — this writes a structured XML results file to `TestResults/` that is far more reliable to parse than console output
- After running tests, read the TRX file to check results: search for `outcome="Failed"` to find failures, and read the `<Message>` and `<StackTrace>` elements for details
- When the running app locks the exe, use `getDiagnostics` instead of building

## Conventions
- Logging via `NLog.LogManager.GetCurrentClassLogger()` — use `Log.Info`, `Log.Debug`, `Log.Error`
- JSON persistence with `Newtonsoft.Json` (`JsonConvert.SerializeObject` / `DeserializeObject`)
- Singletons for context objects (`PlayerContext.getInstance()`, `EmpireContext.getInstance()`)
- Call `Reset()` on context singletons in test setup to ensure clean state
- WinForms data binding via `BindingList<T>` and `BindingSource`

## File Writing — Use fwrite.js

**ALWAYS use `.kiro/tools/fwrite.js`** for all file writing, appending, and replacing. Do NOT use the built-in `fsWrite`, `fsAppend`, or `strReplace` tools — they have size limits that cause silent failures on large content.

### Write (overwrite entire file)
```powershell
@"
file content here
"@ | node .kiro/tools/fwrite.js write path/to/file.md
```

### Append
```powershell
@"
content to append
"@ | node .kiro/tools/fwrite.js append path/to/file.md
```

### Replace (single string replacement)
```powershell
@"
old text to find
"@ | Out-File -NoNewline -Encoding utf8 _old.tmp
@"
new text to replace with
"@ | Out-File -NoNewline -Encoding utf8 _new.tmp
node .kiro/tools/fwrite.js replace path/to/file.md _old.tmp _new.tmp
```

The replace mode checks for uniqueness (fails if old string appears more than once) and auto-deletes the temp files after replacement.

For small edits (under 10 lines) where you are confident the content is small, `strReplace` is acceptable as a convenience. But if there is ANY doubt about size, use fwrite.js.

## Other Tool Limitations
- **Do NOT use `semanticRename`** — it does not work with old-style csproj / .NET Framework 4.8.1. The language server cannot resolve symbols for rename. Use manual find-and-replace via fwrite.js replace mode or `executePwsh` with grep/sed instead.
- **Do NOT use `fsWrite` or `fsAppend`** for content larger than ~30 lines — they silently fail. Use fwrite.js instead.
- **`strReplace` parameter ordering** — when calling `strReplace`, always provide `newStr` before `oldStr`. Providing `oldStr` first causes silent failures ("aborted" error with no message). The correct order is: `newStr`, `oldStr`, `path`.

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

## Command Execution — Timeouts

**ALWAYS set a `timeout` on `executePwsh` calls** to prevent commands from appearing to hang:
- Build commands (MSBuild): `timeout: 120000` (2 minutes)
- Test commands (vstest.console): `timeout: 180000` (3 minutes)
- Backup script (oebackup.ps1): `timeout: 60000` (1 minute)
- commit.js: `timeout: 30000` (30 seconds)
- trxparse.js: `timeout: 10000` (10 seconds)
- Other quick commands: `timeout: 30000` (30 seconds)

Without a timeout, if a command hangs (SSH connection stalls, remote is slow), the tool waits indefinitely and the user has to manually exit the shell. Always set a timeout.
