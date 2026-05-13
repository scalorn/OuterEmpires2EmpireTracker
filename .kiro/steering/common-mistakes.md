# Common Mistakes

Patterns that have caused bugs in this project. Check this list before writing code.

## WinForms Initialization

### Combo/dropdown controls must be populated in the constructor

**What went wrong:** A pricing plan dropdown was added to FormShipTemplate but never populated on initial open — it only got filled when OnCurrentPlayerChanged fired, which doesn't happen on first load.

**Root cause:** The agent copied the event handler pattern (populate on player change) but missed that the constructor also needs an initial population call.

**Rule:** When adding any combo box or dropdown to a form, it MUST be populated in the constructor (or Load handler) in addition to any refresh-on-event logic. Look at how existing combos on the same form are initialized and follow the same pattern.

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
