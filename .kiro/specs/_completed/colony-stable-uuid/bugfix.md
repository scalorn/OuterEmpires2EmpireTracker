# Bugfix Requirements Document

## Introduction

Two interrelated bugs cause data integrity issues when importing colonies from the game clipboard:

1. **Structure duplication on reimport** — When a colony is reimported, all structures are duplicated instead of being merged with existing ones. The root cause is that `Migration001_DeterministicUUIDs` changed global blueprint UUIDs from random GUIDs to deterministic UUID v5 values, and `RemapUUID.Remap()` updated `ColonyStructure.FlatpackBlueprintUUID` references in memory. However, if the player data file was not saved after migration (or was loaded from a pre-migration backup), existing colony structures still carry the old random `FlatpackBlueprintUUID` values. When the user reimports, `BuildFlatpackLookup` maps building names to the new deterministic UUIDs, so the compound merge key `(FlatpackBlueprintUUID, displaySequence)` never matches — every parsed structure has the new UUID while every existing structure has the old UUID. All structures are added as new entries. Deleting and reimporting works because the fresh import uses the new UUIDs from the start with no old-UUID structures to mismatch against.

2. **Colony UUID instability** — Colonies get `Guid.NewGuid()` on creation and are deduped by `(PlanetName, SystemName)` at import time. The UUID has no stable relationship to the colony's identity. If a colony is deleted and recreated, it gets a new UUID, which silently breaks all `ColonyUUID` references in `RouteStop` (delivery routes) and `DeliveryPlanStop` (delivery plans). The codebase already solved this for blueprints via `DeterministicUUID` + `Migration001_DeterministicUUIDs`, but colonies were not included.

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN a colony is reimported from the game clipboard AND the existing colony structures carry old (pre-migration) `FlatpackBlueprintUUID` values THEN the merge logic fails to match any parsed structure to any existing structure because `BuildFlatpackLookup` produces new deterministic UUIDs while existing structures still reference old random UUIDs, resulting in all structures being duplicated as new entries

1.2 WHEN a colony is deleted and recreated (or created fresh from import) THEN the system assigns a random `Guid.NewGuid()` UUID that has no deterministic relationship to the colony's identity fields `(OwnerUUID, PlanetName, SystemName)`

1.3 WHEN a colony is deleted and reimported THEN the system assigns a new random UUID, silently orphaning all `RouteStop.ColonyUUID` references in delivery routes that pointed to the old UUID

1.4 WHEN a colony is deleted and reimported THEN the system assigns a new random UUID, silently orphaning all `DeliveryPlanStop.ColonyUUID` references in delivery plans that pointed to the old UUID

1.5 WHEN `RemapUUID.Remap()` is called to update a UUID THEN the system does not walk `RouteStop.ColonyUUID` or `DeliveryPlanStop.ColonyUUID` references, leaving delivery route and plan stops pointing to stale UUIDs

### Expected Behavior (Correct)

2.1 WHEN the application loads existing data THEN the system SHALL ensure all `ColonyStructure.FlatpackBlueprintUUID` values reference current (deterministic) global blueprint UUIDs, remapping any stale pre-migration UUIDs that were not saved after `Migration001` ran

2.2 WHEN a colony is reimported from the game clipboard THEN the structure merge logic SHALL match parsed structures to existing ones using the compound key `(FlatpackBlueprintUUID, displaySequence)` where both sides use the same (current) blueprint UUIDs, with commodity factory types continuing to use positional matching within each sub-type

2.3 WHEN a colony is created (via import or manually) THEN the system SHALL generate a deterministic UUID v5 from the dedup key `(OwnerUUID, PlanetName, SystemName)` using `DeterministicUUID`, ensuring the same colony always gets the same UUID regardless of when it is created

2.4 WHEN the application loads existing data THEN the system SHALL run a colony UUID migration to remap all colony UUIDs from random GUIDs to deterministic UUIDs, updating all `RouteStop.ColonyUUID` references in delivery routes and all `DeliveryPlanStop.ColonyUUID` references in delivery plans

2.5 WHEN `RemapUUID.Remap()` is called to update a UUID THEN the system SHALL also walk all `RouteStop.ColonyUUID` and `DeliveryPlanStop.ColonyUUID` references and update any that match the old UUID to the new UUID

2.6 WHEN a colony UUID is migrated to a deterministic UUID THEN the system SHALL preserve the original random UUID in a `LegacyUUID` field on the Colony model for traceability (matching the pattern established by `Migration001_DeterministicUUIDs` for blueprints)

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a colony is reimported AND structures are commodity factory types THEN the system SHALL CONTINUE TO use positional matching within each sub-type for commodity factories (existing special-case logic)

3.2 WHEN `RemapUUID.Remap()` is called for a blueprint UUID THEN the system SHALL CONTINUE TO remap `Blueprint.UUID`, `Blueprint.BaseBlueprintUUID`, `ColonyStructure.FlatpackBlueprintUUID`, `ColonyStructure.ResearchingBlueprintUUID`, and `ColonyStructure.ManufacturingBlueprintUUID` as before

3.3 WHEN `Migration001_DeterministicUUIDs` runs for blueprint UUIDs THEN the system SHALL CONTINUE TO generate deterministic blueprint UUIDs and remap all blueprint references correctly

3.4 WHEN a colony already has a deterministic UUID (e.g. after migration has run once) THEN the system SHALL CONTINUE TO skip migration for that colony (idempotent behavior)

3.5 WHEN colonies are deduped during import by `(PlanetName, SystemName)` via `ColonyImportHelper.FindByPlanet` THEN the system SHALL CONTINUE TO find existing colonies correctly and merge identity fields rather than creating duplicates

3.6 WHEN delivery routes and plans reference colony UUIDs THEN the system SHALL CONTINUE TO resolve those references correctly after migration (no broken route stops or plan stops)

3.7 WHEN the `gameSequence` (displaySequence) values are stable across reimports for non-commodity structures THEN the system SHALL CONTINUE TO use the compound key `(FlatpackBlueprintUUID, displaySequence)` for merge matching, as this key is reliable when both sides reference the same blueprint UUIDs
