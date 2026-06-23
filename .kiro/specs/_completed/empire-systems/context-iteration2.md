# Empire Systems ? Iteration Context: Iteration 2 (Ships)

## What This Phase Does
Implements ship templates, ship instances, and the ShipBuildService. Ship templates define hull + component configurations. Ship instances are physical ships with cargo, hoppers, damage tracking, and location.

## Prerequisites
- Iteration 1 complete (all models exist, PlayerContext updated)

## Key Files to Read
- .kiro/steering/empire-patterns.md
- .kiro/specs/empire-systems/design.md sections:
  - ShipTemplate, Ship, ShipComponentSlot models
  - ShipBuildService (GenerateShipBuildItems, ValidateAssemblyLocation, ComputeStats, ComputeStationStats)
  - ShipStats, StationStats classes
  - FormShipTemplate, FormShipInstance mockups
  - SlotType mapping (OQ-31/OQ-32)
- Constants/SlotTypes.cs (created in Iteration 1)

## Key Design Decisions
- ShipComponentSlot has damage fields (CurrentHP/MaxHP/MaxRepairPercent) ? used on Ship and Station instances, ignored on templates
- Ship hull has separate damage fields (HullCurrentHP/HullMaxHP/HullMaxRepairPercent)
- Ship duplicates hull + components from template (independent after creation)
- Hopper only accepts High/Medium/Low purity resources
- FormShipInstance components grid has editable Condition/MaxRepair columns, hull row always first
- Ship class assembly validation: 2-5 any location, 6 Station+Starbase, 7-8 Starbase only

## What Gets Built
- ShipBuildService (4 methods)
- FormShipTemplate with slot grid, stats panel, Order Build button
- FormShipInstance with Overview tab (components + stats) and Cargo tab (hold/hopper toggle, crate master-detail)
- ShipTemplateReferenceCounter, ShipReferenceCounter
- BlueprintReferenceCounter expansion (ship component BlueprintUUIDs)

## Completion Criteria
- Build passes, all tests pass
- Property test P6 (assembly validation)
- Unit tests for ShipBuildService, ComputeStats, reference counters
