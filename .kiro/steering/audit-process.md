---
inclusion: always
---
# Audit Process

Comprehensive audit checklist for verifying spec-to-code and code-to-spec traceability. Run after every significant change. Automated checks run via `node .kiro/tools/audit.js`. Manual checks are performed by reviewing the output and investigating findings.

## Automated Checks (audit.js)

These run automatically via the post-task-audit hook:

1. **Magic Strings** (magic-strings.js) — Every string constant in Constants/*.cs cross-referenced against all code. No raw literals where constants exist.
2. **Spec Coverage** (spec-coverage.js) — Every .cs class name appears in at least one spec/*.md file.
3. **PERF Timing** (perf-check.js) — Every Populate*/Refresh*/Rebuild* method in Form*.cs has Stopwatch + PERF logging. 83 findings for small combo methods are accepted baseline.
4. **Reference Counters** (refcount-check.js) — Every source in spec/design/reference-counting.md is counted by the corresponding counter.
5. **Control Wiring** (control-wiring.js) — Every form implements IProgrammaticUpdateSource, has NLog Logger, subscribes to CurrentPlayerChanged, and unsubscribes in OnFormClosed.
6. **Mockup Controls** (mockup-controls.js) — Cross-references mockup control names against Designer.cs files. Supports section-level mapping (multi-form mockups), alias maps (mockup name → code name), and partial-form sections (e.g. colony-overflow.md covers only the Overflow tab). Findings are genuine gaps: "IN MOCKUP NOT CODE" = designed but not yet implemented, "IN CODE NOT MOCKUP" = implemented but not documented in mockup.

## Manual Audit Checklist

### Forward Traceability (Spec → Code)

For each requirement in spec/requirements/:
- [ ] Requirement has corresponding code that implements it
- [ ] Requirement traces to a design element in spec/design/
- [ ] Requirement traces to a user flow in spec/flows/ (if user-facing)
- [ ] Requirement traces to a mockup in spec/mockups/ (if it involves UI)
- [ ] Requirement traces to a help doc in docs/ (if user-facing)
- [ ] Requirement traces to at least one test

### Backward Traceability (Code → Spec)

For each .cs file in the main project:
- [ ] Class is mentioned in spec/ (automated: spec-coverage.js)
- [ ] Service has requirements in spec/requirements/
- [ ] Form has a mockup in spec/mockups/
- [ ] Form has a help doc in docs/
- [ ] Data model has field definitions in spec/design/data-models.md
- [ ] Data model has requirements in spec/requirements/DataModel.md or domain file

### Magic Numbers/Strings

- [ ] No hardcoded string literals that match defined constants (automated: magic-strings.js)
- [ ] No hardcoded numeric literals that represent game mechanics (multipliers, rates, thresholds)
- [ ] Property key strings used in PropertyBag lookups are in BlueprintPropertyKeys or GameConstants
- [ ] Purity names use GameConstants.PurityHigh/Medium/Low/Refined
- [ ] Structure state strings use GameConstants.PropBuilt/PropStaged/PropOnline
- [ ] Slot type strings use SlotTypes constants
- [ ] Blueprint type strings use BlueprintTypes constants
- [ ] Item volume/mass values use GameConstants.Volume*/Mass* constants
- [ ] Skill multiplier rates use GameConstants.*RatePerLevel constants

### Controls and Wiring

For each Form*.cs:
- [ ] Every control declared in Designer.cs has an event handler wired in the constructor
- [ ] Every event handler has a `if (_isProgrammaticUpdate > 0) return;` guard (where applicable)
- [ ] Every data-modifying control writes through to the data model immediately (write-through pattern)
- [ ] Every form implements IProgrammaticUpdateSource with BeginProgrammaticUpdate/EndProgrammaticUpdate (automated: control-wiring.js)
- [ ] Every form subscribes to CurrentPlayerChanged and refreshes on player switch (automated: control-wiring.js)
- [ ] Every form unsubscribes from events in OnFormClosed (automated: control-wiring.js)
- [ ] Every form has NLog Logger (automated: control-wiring.js)
- [ ] Key methods have PERF timing (automated: perf-check.js)
- [ ] Every control listed in the mockup exists in the Designer.cs (automated: mockup-controls.js)
- [ ] Every control in the Designer.cs is documented in the mockup (automated: mockup-controls.js)

### Data Flows and Indexes

- [ ] All data flows in spec/flows/ have corresponding code paths
- [ ] All PlayerContext UUID caches listed in spec/design/indexing.md exist in code
- [ ] All cross-entity indexes listed in spec/design/indexing.md exist in code
- [ ] Reference counters count all sources listed in spec/design/reference-counting.md (automated: refcount-check.js)
- [ ] Reference counter completeness tests exist for every counter

### Documentation

- [ ] Every user-facing feature has a help doc in docs/
- [ ] docs/README.md lists all help docs
- [ ] README.md describes all features
- [ ] spec/BACKLOG.md has no completed items in the Open section
- [ ] spec/COMPLETED.md lists all completed work
- [ ] spec/Ambiguities.md has no unresolved items that should be fixed

## Running the Full Audit

```
# 1. Run automated checks
node .kiro/tools/audit.js --fix

# 2. Build and test
& "D:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" OE2EmpireTracker.sln /p:Configuration=Debug /v:minimal
& "D:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe" OE2EmpireTracker.Tests/bin/Debug/OE2EmpireTracker.Tests.dll /Logger:trx
node .kiro/tools/trxparse.js

# 3. Review findings and add to spec/Ambiguities.md
# 4. Fix what can be fixed, log the rest as AMB-xxx items
```

## New Check Ideas

When you identify a new category of issue during an audit, add it here so it becomes part of the standard process:

- (add new check categories here as they're discovered)

## Maintaining mockup-controls.js

When adding a new form or mockup:
1. Add a new entry to `SECTION_MAP` in `.kiro/tools/mockup-controls.js`
2. The `header` regex should match the `### FormXxx` line in the mockup
3. If the mockup uses domain-prefixed control names (e.g. `txtAsteroidFilter`) but the code uses generic names (e.g. `txtFilter`), add entries to the `aliases` map: `{ 'txtAsteroidFilter': 'txtFilter' }`
4. If the mockup section covers only part of a form (e.g. one tab), set `partial: true` — this suppresses "IN CODE NOT MOCKUP" findings for controls belonging to other parts of the form
5. Multi-form mockups (e.g. ships.md with FormShipTemplate + FormShipInstance) need separate section entries with distinct header regexes
