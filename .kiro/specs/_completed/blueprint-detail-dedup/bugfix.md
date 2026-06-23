# Bugfix Requirements Document

## Introduction

When multiple blueprints share the same Name + Evolution (common in-game — players can have 6+ copies of the same blueprint type), the `CreateBlueprintDetailItem` method in `QueueSyncService.cs` incorrectly assigns `GameApiBlueprintId` to the FIRST matching local blueprint every time. This causes API ID stomping, index corruption, perpetual re-import on every sync cycle, and property data merging into a single blueprint while duplicates remain stale.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN multiple local blueprints share the same Name + Evolution AND the detail import runs for each API blueprint ID THEN the system assigns every `GameApiBlueprintId` to the same first-matching local blueprint, overwriting its API ID repeatedly

1.2 WHEN multiple API IDs (e.g. 101, 102, 103) are sequentially imported for blueprints with the same Name + Evolution THEN the `_blueprintByApiIdIndex` only retains the last API ID assigned because earlier index entries are replaced when the same blueprint object gets a new API ID

1.3 WHEN a subsequent sync cycle calls `FindBlueprintByApiId` for a previously-assigned API ID (e.g. 101, 102) THEN the lookup returns null because those IDs were overwritten in the index, causing the freshness check to fail and triggering a redundant re-import every cycle

1.4 WHEN blueprint detail properties are imported for duplicate Name + Evolution blueprints THEN properties from all API responses are repeatedly merged into the single first-matching local blueprint while the other local copies never receive their correct properties

1.5 WHEN a local blueprint already has a different `GameApiBlueprintId` assigned (belonging to another API item) THEN the system ignores this and still selects it as the match target based solely on Name + Evolution

### Expected Behavior (Correct)

2.1 WHEN the detail import runs for an API blueprint ID that has already been assigned to a local blueprint (via `FindBlueprintByApiId`) THEN the system SHALL use that already-assigned blueprint directly without performing a Name + Evolution search

2.2 WHEN multiple local blueprints share the same Name + Evolution AND the system needs to assign a new API ID THEN the system SHALL exclude candidates that already have a different `GameApiBlueprintId` assigned, limiting the search to unassigned blueprints only

2.3 WHEN multiple unassigned local blueprints remain as candidates after filtering THEN the system SHALL use property-based scoring (analogous to `FindBestMatch`) to select the most similar candidate rather than taking the first match

2.4 WHEN each API blueprint ID is assigned to a distinct local blueprint THEN the `_blueprintByApiIdIndex` SHALL contain a separate entry for each API ID mapping to its own unique local blueprint

2.5 WHEN subsequent sync cycles run the freshness check via `FindBlueprintByApiId` THEN every previously-assigned API ID SHALL resolve to its correct local blueprint, preventing redundant re-imports

### Unchanged Behavior (Regression Prevention)

3.1 WHEN only a single local blueprint exists with a given Name + Evolution THEN the system SHALL CONTINUE TO match and assign the API ID to that blueprint without additional scoring

3.2 WHEN a blueprint has no `GameApiBlueprintId` assigned and is the sole unassigned candidate matching Name + Evolution THEN the system SHALL CONTINUE TO assign the new API ID and set `LastDetailImportUtc`

3.3 WHEN `CrateImporter.ImportFromJson` returns a Skipped action THEN the system SHALL CONTINUE TO skip API ID assignment for that import entry

3.4 WHEN a blueprint's `LastDetailImportUtc` is recent (freshness check passes) THEN the system SHALL CONTINUE TO skip re-importing detail data for that blueprint

3.5 WHEN the imported blueprint's Name + Evolution matches no local blueprint at all THEN the system SHALL CONTINUE TO leave the API ID unassigned (no crash, no spurious assignment)
