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

## Tool Limitations
- **Do NOT use `semanticRename`** — it does not work with old-style csproj / .NET Framework 4.8.1. The language server cannot resolve symbols for rename. Use manual find-and-replace (`strReplace` or `executePwsh` with grep/sed) instead.
- **`strReplace` parameter ordering** — when calling `strReplace`, always provide `newStr` before `oldStr`. Providing `oldStr` first causes silent failures ("aborted" error with no message). The correct order is: `newStr`, `oldStr`, `path`.
- **Large file edits** — for inserting large blocks of content (50+ lines), prefer `fsWrite` to create a temp file with the new content, then use `readFile` + `strReplace` to insert it. Or use multiple smaller `strReplace` calls. Single massive `newStr` values can be unreliable.
