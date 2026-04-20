<!-- Extracted from .kiro/specs/empire-systems/design.md — Migration section -->
# Migration

## UUID Strategy

Entities that represent game-world objects shared across players use deterministic UUIDs (UUID v5 via DeterministicUUID) so that two players who independently create the same entity get the same UUID. This enables data sharing.

| Entity | UUID Type | Seed |
|---|---|---|
| Station | Deterministic | Station name (station namespace) |
| Asteroid | Deterministic | SystemName:AsteroidName (asteroid namespace) |
| Faction | Deterministic | Faction name (faction namespace) |
| ExternalCharacter | Deterministic | Character name (character namespace) |
| BuildPlan | Random | Player-specific work order |
| BuildItem | Random | Nested in plan |
| ShipTemplate | Deterministic | OwnerUUID + template name (template namespace) |
| Ship | Random | Player-owned instance |
| MarketListing | Random | Player-specific record |
| MarketTransaction | Random | Player-specific record |
| StockPlan | Random | Player-specific plan |
| StockTarget | Random | Nested in plan |
| StockProfile | Random | Player-specific composition |
| SupplyChain | Random | Player-specific definition |
| WarehouseOverflowRule | Random | Player-specific rule |

Each deterministic entity type uses its own UUID namespace to avoid collisions.

## Migration008_RouteStopDestinationMigration

A migration that:
1. Migrates existing RouteStop.ColonyUUID → DestinationUUID + DestinationType.Colony for all delivery routes (where DestinationUUID is empty).
2. Migrates existing DeliveryPlanStop.ColonyUUID → DestinationUUID + DestinationType.Colony for all delivery plans (where DestinationUUID is empty).

Note: Empty arrays for new entity types are initialized by PlayerContext during deserialization (null → empty array), not by a dedicated migration.

## Migration Sequence

The actual migration sequence is:

| Migration | Description |
|---|---|
| Migration001_DeterministicUUIDs | Assigns deterministic UUIDs to shared entities |
| Migration002_ColonyDeterministicUUIDs | Assigns deterministic UUIDs to colonies |
| Migration003_SurveyDateTimeNormalization | Normalizes survey date/time formats |
| Migration004_ColonyImportTimestampBackfill | Backfills colony import timestamps |
| Migration005_PropertyKeyCleanup | Cleans up blueprint property keys |
| Migration006_DisplaySequenceJsonKey | Migrates gameSequence → displaySequence JSON key |
| Migration007_RemoveClassFromProperties | Removes Class from blueprint properties |
| Migration008_RouteStopDestinationMigration | Migrates RouteStop/DeliveryPlanStop ColonyUUID → DestinationUUID |

The original spec described a Migration005_EmpireSystems but the actual implementation split this across PlayerContext initialization (null → empty array for new entity types) and Migration008 (route stop destination migration).