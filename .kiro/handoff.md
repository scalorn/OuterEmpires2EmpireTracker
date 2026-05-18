# Session Handoff — 2026-05-18

## Branch: mainline (merged from net8maui, backed up to 3 remotes)

## What Was Done This Session

### Service Migration to Common — 90+ files moved
- All services, parsers, ViewModels, Migration classes moved from WinForms to Common
- Architectural cleanups: ColonyService import flow, BlueprintService data methods, BackgroundProcessor decoupling, MigrationRunner delegate
- Desktop dedup: deleted 9 duplicated files (parsers + pure logic services)

### Delivery Route Enhancements
- Added JAS distance column (system-to-system distance)
- Added ship picker combo with fuel estimate calculation (JumpFuelPerJAS × JAS)
- Ship/template change events refresh fuel estimates
- System import now uses file dialog instead of hardcoded path
- SystemData.json added to project with CopyToOutputDirectory

### Audit
- All 21 automated checks pass with zero findings
- Fixed 10 DateTime.UtcNow violations in Desktop project
- Updated event matrix, requirements, mockups for new features

## Next Task: BL-074 — Immutable Data Model

### Spec: `.kiro/specs/immutable-data-model/tasks.md`

Make entity property setters `internal` so only Common can mutate entities.
InternalsVisibleTo already added to Common.csproj for all consuming projects.

### Scope
- ~30 model files in Common/Models/
- ~525 entity constructions in tests
- ~698 property sets in tests
- All compile via InternalsVisibleTo

### Approach
1. InternalsVisibleTo already set up ✅
2. Change entity setters from `public set` to `internal set` one class at a time
3. Build after each class to catch any external mutation that slipped through
4. Newtonsoft.Json handles internal setters via reflection (no issue)
5. Start with Blueprint.cs, then Colony, ColonyStructure, Survey, etc.

### Key Insight
Since ALL consuming projects are in InternalsVisibleTo, this change is
primarily about documentation/intent rather than compile-level enforcement.
True enforcement would require removing WinForms/Desktop from the list —
but they still have services that need internal access. The mutation-audit.js
tool provides the actual enforcement at the Form/ViewModel level.

## What Stays in WinForms (genuinely UI-bound, 4 files)
- ClipboardHelper, ClipboardContentDetector
- ColonyAdminReportBuilder (RTF formatting)
- HelpRenderer, HelpTopicRegistry

## Current state
- All work committed and backed up to all 3 remotes
- Full solution builds with zero warnings
- All 2563 tests pass
- Audit: 21/21 checks pass, zero findings
