# Common Mistakes

Patterns that have caused bugs in this project. Check this list before writing code.

## WinForms Initialization

### Combo/dropdown controls must be populated in the constructor

**What went wrong:** A pricing plan dropdown was added to FormShipTemplate but never populated on initial open — it only got filled when OnCurrentPlayerChanged fired, which doesn't happen on first load.

**Root cause:** The agent copied the event handler pattern (populate on player change) but missed that the constructor also needs an initial population call.

**Rule:** When adding any combo box or dropdown to a form, it MUST be populated in the constructor (or Load handler) in addition to any refresh-on-event logic. Look at how existing combos on the same form are initialized and follow the same pattern.

### PopulateForm must call all display-update methods

**What went wrong:** The pricing label disappeared when switching templates because `PopulateForm()` set the hull and slots but never called `UpdateTemplatePrice()`. The hull-change handler that normally triggers price recalculation was suppressed by the ProgrammaticUpdateGuard.

**Root cause:** The agent added recalculation triggers to individual change handlers but missed that `PopulateForm()` runs inside a ProgrammaticUpdateGuard, which suppresses those handlers.

**Rule:** When adding a computed display (label, stats panel, etc.) that depends on form state, ensure `PopulateForm()` explicitly calls the update method. Do NOT rely on change handlers firing during `PopulateForm()` — they are suppressed by the programmatic update guard.

## Library Versions

### FsCheck is version 2.16.6 — no 3.x APIs

**What went wrong:** Test files were written using `FsCheck.Fluent`, `Shrink.Default<T>()`, and certain `Arb.From` overloads that only exist in FsCheck 3.x. The test project failed to compile.

**Root cause:** The agent used documentation or training data from FsCheck 3.x without checking the installed version in packages.config.

**Rule:** Before using any library API, check the version in `packages.config` (or the .csproj for SDK-style projects). For FsCheck 2.16.6 specifically:
- Do NOT use `FsCheck.Fluent` namespace
- Do NOT use `Shrink.Default<T>()`
- Use `Arb.From<T>(gen)` for creating arbitraries from generators
- Use `[FsCheck.NUnit.Property(MaxTest = 100)]` attribute for property tests
- Use LINQ query syntax (`from x in Gen.Choose(...)`) for generators

## Build Verification

### Always build the full solution, not just one project

**What went wrong:** The star system implementation was committed with broken test compilation because only the main project was verified.

**Root cause:** The agent ran MSBuild against the .csproj instead of the .sln, or used getDiagnostics on only the source files.

**Rule:** Always build `OE2EmpireTracker.sln` (the full solution). Never build individual .csproj files in isolation. The postTaskExecution hook enforces this, but do it proactively too.

## Hook Commands

### Hook runCommand uses CMD, not PowerShell

**What went wrong:** A postTaskExecution hook used `& 'path'` syntax (PowerShell) but hooks execute in CMD. CMD reported `& was unexpected at this time`.

**Root cause:** The agent assumed hooks run in PowerShell because `executePwsh` uses PowerShell. Hook `runCommand` uses CMD.

**Rule:** Hook commands must use CMD syntax:
```
WRONG (PowerShell): & 'D:\Program Files\...\MSBuild.exe' args
WRONG (PowerShell): & "D:\Program Files\...\MSBuild.exe" args
RIGHT (CMD):        "D:\Program Files\...\MSBuild.exe" args
```
In CMD, just double-quote the path directly — no call operator needed.

## StyleCop

### SA1133: Each attribute on its own line

**What went wrong:** Properties were decorated with combined attributes like `[JsonProperty("x"), DefaultValue(0)]` which violates SA1133.

**Root cause:** The agent optimized for brevity over StyleCop compliance.

**Rule:** Every attribute goes in its own set of square brackets on its own line:
```csharp
// WRONG
[JsonProperty("x"), DefaultValue(0)]
public int X { get; set; }

// RIGHT
[JsonProperty("x")]
[DefaultValue(0)]
public int X { get; set; }
```


## Audit Enforcement

### Never dismiss audit findings as "pre-existing" or "baseline"

**What went wrong:** Subagents ran audit, saw flatpack integrity findings, classified them as "pre-existing" or "not caused by my changes," and proceeded without fixing them. This allowed 390 duplicate UUID findings to persist across multiple task completions.

**Root cause:** The audit hook was an `askAgent` type (advisory) rather than `runCommand` (blocking). Subagents treated the instruction as optional guidance rather than a hard gate.

**Rule:** Every audit finding is a real issue that must be fixed before proceeding. There is no such thing as "pre-existing," "accepted baseline," or "not my fault." If the audit exits with code 1, you stop and fix. The postTaskExecution hook now runs audit as a `runCommand` — if it fails, the hook output shows the failure and the agent must address it.


## Test Verification

### ALL test suites must pass — not just the ones you touched

**What went wrong:** During the asteroid-reserves-display spec execution, the orchestrator only verified TypeScript (vitest) and the specific server tests the subagent wrote. It never ran the full `dotnet test OE2EmpireTracker.Server.Tests` suite. 29 pre-existing failures were committed without investigation because the orchestrator assumed "if the build passes and my specific tests pass, everything is fine."

**Root cause:** The postTaskExecution hooks only ran MSBuild (compile check) and audit. No hook ran the full test suite. The orchestrator treated hook failures with no output as "infrastructure issues" instead of investigating.

**Rule:** Before any commit, ALL test suites must pass:
1. `dotnet test OE2EmpireTracker.Server.Tests --no-build` (server tests)
2. `npx vitest run` (from OE2EmpireTracker.Web — TypeScript tests)
3. `vstest.console` against `OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll` (WinForms tests)

If any test fails, investigate and fix it. Do NOT dismiss failures as "pre-existing" or "not caused by my changes." The steering already says this but it was ignored — the hooks now enforce it.

### Hook failures with no output must be investigated immediately

**What went wrong:** The audit hook exited with code 1 but produced no output. The agent dismissed it as "infrastructure issue" and continued. It turned out to be real audit findings.

**Root cause:** The agent treated "no output" as "nothing to act on" rather than "something is broken and needs investigation."

**Rule:** If a hook exits with code 1 (failure) but produces no output, run the command manually with full output to see what's happening. Never dismiss a hook failure without understanding it.


### Frontend verification must use `npm run build`, not `tsc --noEmit`

**What went wrong:** After fixing API contract mismatches, the agent ran `tsc --noEmit` which passed. But the production build (`npm run build`) failed because it first runs `generate-types` which regenerates `generated.ts` from the server schema. The regenerated types had `assignedWorkers: Record<string, boolean>` which conflicted with the domain type's `Record<string, string>`.

**Root cause:** `tsc --noEmit` checks source files as they exist on disk. The production build regenerates `generated.ts` first, which may introduce new type conflicts. Skipping the generation step means you're checking against stale types.

**Rule:** Always use `npm run build` (from OE2EmpireTracker.Web/) for frontend verification. Never use `tsc --noEmit` alone — it skips the type generation step and gives false confidence.
