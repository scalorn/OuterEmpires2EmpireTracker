# Contributing to OE2EmpireTracker

Thank you for your interest in contributing to OE2EmpireTracker! This guide explains how we work, what we expect, and how to get your changes merged.

## Contribution Tiers

### Tier 1 — No spec needed

These contributions can go straight to a PR:

- **Bug reports** (open an issue with reproduction steps)
- **Data corrections** (fixes to `BaselineData.json` — typos, wrong values, missing entries)
- **Documentation fixes** (typos, broken links, clarifications in `docs/` or `spec/`)
- **Typo fixes** in code comments or string literals

### Tier 2 — Spec required

These require an approved spec *before* any code is written:

- New features (new forms, new services, new data models)
- Behavior changes (altering how existing features work)
- UI additions (new controls, new tabs, layout changes)

See [The Spec-First Process](#the-spec-first-process) below.

### Tier 3 — Maintainer only

These are not open for external contribution:

- Architecture changes (layer boundaries, persistence strategy, singleton patterns)
- Migration code (data format upgrades, schema changes)
- Audit tools (`.kiro/tools/` scripts)
- Steering files (`.kiro/steering/`)

---

## The Spec-First Process

For Tier 2 contributions, follow this workflow:

1. **Open a Discussion or Issue** describing your idea. Explain the use case, not just the solution.

2. **Create a spec PR** with at minimum:
   - `spec/contributions/{feature-name}/requirements.md` — what the feature must do (SHALL statements)
   - `spec/contributions/{feature-name}/design.md` — how it fits into the existing architecture

3. **Maintainer reviews the spec** — checking architecture fit, conflict with planned work, and scope.

4. **Once the spec is approved**, implement on a feature branch.

5. **Submit a code PR** referencing the approved spec. The PR must pass all CI gates.

6. **CI gates** must all pass:
   - Zero warnings (including StyleCop)
   - All tests pass
   - Audit clean

7. **Maintainer reviews code** against the approved spec — no surprises, no scope creep.

> **Why spec-first?** This project has 19 automated audit checks, full spec traceability, and mockups for every form. A code PR without a spec will be closed — not because we don't value the work, but because unplanned changes break the traceability chain.

---

## Quality Gates

Every PR must pass these checks:

| Gate | What it checks |
|------|---------------|
| **Build** | Zero errors AND zero warnings (including all StyleCop SA* rules) |
| **Tests** | All NUnit + FsCheck property-based tests pass |
| **Audit** | `node .kiro/tools/audit.js` reports zero new findings |
| **Spec traceability** | New code is referenced in spec files |

### StyleCop rules we enforce

- SA1201 — Member ordering by kind (fields, constructors, properties, methods)
- SA1202 — Member ordering by access level (public before private)
- SA1500 — Brace placement (opening brace on new line)
- All other SA* rules enabled in `stylecop.json`

---

## Development Setup

### Prerequisites

- **Windows** (WinForms application — no cross-platform support)
- **.NET Framework 4.8.1** (not .NET Core / .NET 5+)
- **Visual Studio 2022+** (Community Edition works fine)
- **NuGet CLI** (included in repo as `nuget.exe`)
- **Node.js** (for audit tools)

### Getting started

```bash
# Clone the repo
git clone <repo-url>
cd OE2EmpireTracker

# Restore NuGet packages
nuget restore OE2EmpireTracker.sln

# Build (use MSBuild, not dotnet build)
msbuild OE2EmpireTracker.sln /p:Configuration=Debug

# Run tests (use vstest.console, not dotnet test)
vstest.console OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx

# Run audit
node .kiro/tools/audit.js
```

### Why not `dotnet build` / `dotnet test`?

This project uses old-style `.csproj` with `packages.config` NuGet management. The `dotnet` CLI is incompatible with this format. Use MSBuild and vstest.console directly.

---

## Code Standards

### Architecture

- **Layered:** Models (POCOs) → Services (domain logic) → ViewModels → Forms (UI)
- All entity mutation goes through dedicated **Service** classes — never modify models directly from Forms
- Data model collections are **unordered bags** — sort via `CollectionSortHelper` at the point of consumption, never store sorted

### Required patterns

- All forms implement `IProgrammaticUpdateSource`
- All services and ViewModels have an NLog `Logger` field
- No magic strings — use `Constants/*.cs` (BlueprintTypes, GameConstants, etc.)
- JSON persistence via `Newtonsoft.Json`
- Singletons for context objects (`PlayerContext.getInstance()`, `EmpireContext.getInstance()`)

### StyleCop compliance

The build enforces zero warnings. StyleCop SA* rules are not optional. Common gotchas:

- Members must be ordered: fields → constructors → properties → methods
- Within each kind, order by access: public → internal → protected → private
- Opening braces go on a new line
- Documentation comments required on public members

---

## What NOT to Do

| Don't | Why |
|-------|-----|
| Submit code PRs without an approved spec (Tier 2) | Breaks traceability; will be closed |
| Modify `*.Designer.cs` files by hand | Use the WinForms designer; hand edits corrupt the file |
| Use `dotnet test` | Incompatible with `packages.config` — tests won't run |
| Add new NuGet packages without discussion | Dependencies affect the entire project |
| Modify audit tools or steering files | Tier 3 — maintainer only |
| Sort data model collections in-place | Use `CollectionSortHelper` at consumption |
| Put business logic in Forms | Goes in Services; Forms only do UI binding |

---

## Reporting Bugs

Use the [Bug Report](.github/ISSUE_TEMPLATE/bug_report.md) issue template. Include:

- Steps to reproduce
- Expected vs. actual behavior
- Screenshots if it's a UI issue
- Your OS and .NET Framework version

---

## Questions?

Open a Discussion. We're happy to help you find the right approach before you invest time in a spec or implementation.
