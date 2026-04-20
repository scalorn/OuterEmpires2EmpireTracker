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

## Migration005_EmpireSystems

A new migration that:
1. Adds empty arrays for all new entity types if missing from PlayerRoot.
2. Migrates existing RouteStop.ColonyUUID → DestinationUUID + DestinationType.Colony for all delivery routes.
3. Migrates existing DeliveryPlanStop.ColonyUUID → DestinationUUID + DestinationType.Colony for all delivery plans.
4. Increments DataVersion.

This migration is safe to run on existing data — it only adds defaults and copies existing fields.