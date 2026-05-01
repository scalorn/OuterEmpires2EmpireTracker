# Project Constitution

Immutable principles governing all development on OE2EmpireTracker. These are non-negotiable and apply to every change regardless of scope.

## Article I: Traceability

Every piece of code traces to a requirement. Every requirement traces to code. Nothing exists undocumented. Nothing is documented without implementation.

- Requirements use IDs: REQ-{DOMAIN}-{NNN}
- Every service, form, and model has corresponding spec documentation
- Every form has a mockup in spec/mockups/
- Every user-facing feature has a help doc in docs/
- Spec updates ship in the same commit as code changes

## Article II: Write-Through

All editable UI controls write to the data model immediately on change. The Save button persists to disk; it does not transfer UI state to the model. The in-memory model is always current.

## Article III: Zero Warnings

The solution must compile with zero warnings. No suppression pragmas. No "we'll fix it later." If it warns, fix it before committing.

## Article IV: Audit Clean

`node .kiro/tools/audit.js` must report no new findings before every commit. Accepted baselines are documented in audit-process.md. New findings are blockers.

## Article V: Reference Counting

Entities referenced by other entities cannot be deleted. Every deletable entity has a ReferenceCounter service. The UI shows reference counts and disables deletion when in use.

## Article VI: Programmatic Update Guards

All forms implement IProgrammaticUpdateSource. All grid event handlers check `_isProgrammaticUpdate > 0` first. ProgrammaticUpdateGuard (using declaration) wraps all code-driven UI mutations.

## Article VII: PERF Timing

Every Populate*, Refresh*, and Rebuild* method in forms has Stopwatch + PERF logging. Performance regressions are visible in logs without instrumentation tools.

## Article VIII: Event Hygiene

All forms subscribe to PlayerContext events with named methods (not lambdas). All forms unsubscribe in OnFormClosed. All cross-thread handlers check IsDisposed and use BeginInvoke.

## Article IX: Deterministic Persistence

JSON serialization uses deterministic property ordering. Collections are serialized in a stable order. File writes use SafeFileWriter (atomic rename). No data loss on crash.

## Article X: Constants Over Literals

No magic strings where constants exist. Property keys use BlueprintPropertyKeys. Purity names use GameConstants. Structure states use GameConstants. Slot types use SlotTypes. Blueprint types use BlueprintTypes.

## Article XI: Test Before Commit

All tests must pass before committing. Use vstest.console with /Logger:trx. Parse results with trxparse.js. Never commit with known test failures.

---

**Ratified**: 2025-04-30  
**Source**: Consolidated from .kiro/steering/ rules (workflow.md, forms.md, empire-patterns.md, spec-traceability.md, audit-process.md)
