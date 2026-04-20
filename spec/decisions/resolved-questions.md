<!-- Extracted from .kiro/specs/empire-systems/design.md -->
# Resolved Design Questions

## Open Design Questions

These questions were identified during design review and need resolution before implementation of the affected iteration. Each is tagged with the iteration it blocks.

### OQ-30: StockTarget Persistence â€” Nested vs Flat (Iteration 7) â€” RESOLVED

**Decision:** Eliminate standalone StockTargets entirely. Everything is a StockPlan with nested targets. What was previously a "standalone target" (e.g. "20k Munitions") is modeled as a StockPlan with a single target inside it. This simplifies the data model, persistence, UI, and reference counting.

Changes:
- `PlayerRoot.StockTarget[]` removed. All targets live inside `StockPlan.Targets`.
- `StockTarget.OwnerUUID` removed (ownership is on the plan).
- `StockTarget.StockPlanUUID` removed (targets are always nested, no foreign key needed).
- `StockTarget.ReplenishmentBuildPlanUUID` removed (lives on StockPlan only).
- `StockTargetList` on PlayerContext removed.
- `StockProfileEntry.StockTargetUUID` removed â€” entries only reference StockPlans.
- `StockTargetReferenceCounter` eliminated â€” no standalone targets to reference-count.
- FormStockTargets simplified to a single plan list (no separate standalone section).

The AND/OR composition system works the same way â€” StockProfiles reference StockPlans. A "simple" target like "20k Munitions" is just a plan named "20k Munitions" with one target inside it. The UI can streamline creation of single-target plans with a "Quick Add" button that creates the plan and target in one step.

### OQ-31: ShipComponentSlot SlotType Values (Iteration 2) â€” RESOLVED

**Decision:** Weapons use three separate slot types (`WeaponSmall`, `WeaponMedium`, `WeaponLarge`). Hull modifications are separate types. The complete mapping from hull property to SlotType is defined below.

| Hull Property | SlotType String | Max Count Source | BlueprintType IDs |
|---|---|---|---|
| Reactor Slots | `Reactor` | Hull property value | `Reactor` |
| Main Drive Slots | `MainDrive` | Hull property value | `MainDrive` |
| Cargo Pod Slots | `CargoPod` | Hull property value | `CargoPod` |
| Fuel Tank Slots | `FuelTank` | Hull property value | `FuelTank` |
| Jump Drive Slots | `JumpDrive` | Hull property value | `JumpDrive` |
| Nav Comp Slots | `NavComp` | Hull property value | `NavComp` |
| Shield Slots | `Shield` | Hull property value | `Shield` |
| Thruster Slots | `Thruster` | Hull property value | `Thruster` |
| Coupler Slots | `Coupler` | Hull property value | `UniversalCoupler` |
| GERTY Slots | `GERTY` | Hull property value | `GERTYDroneRack` |
| Scanner Slots | `Scanner` | Hull property value | `SystemObjectScanner` |
| Small Weapon Mounts | `WeaponSmall` | Hull property value | `Beamer/Small`, `Railgun/Small`, `CoilGun/Small`, `MissileLauncher/Small`, `TorpedoLauncher/Small` |
| Medium Weapon Mounts | `WeaponMedium` | Hull property value | `Beamer/Medium`, `Railgun/Medium`, `CoilGun/Medium`, `MissileLauncher/Medium`, `TorpedoLauncher/Medium` |
| Large Weapon Mounts | `WeaponLarge` | Hull property value | `Beamer/Large`, `Railgun/Large`, `CoilGun/Large`, `MissileLauncher/Large`, `TorpedoLauncher/Large` |
| Max Hull Plating | `HullPlating` | Hull property value | `HullPlating` |
| Max Hull Reinforcement | `HullReinforcement` | Hull property value | `HullReinforcement` |
| Max Hull Sealant Units | `HullSealant` | Hull property value | `HullSealantInjectionUnit` |
| Max Mining Lasers | `MiningLaser` | Hull property value | `MiningLaser` |
| Max Mining Grapples | `MiningGrapple` | Hull property value | `AsteroidGrapple` |
| Max Ore Hoppers | `OreHopper` | Hull property value | `OreHopper` |

Notes:
- Mining-capable hulls (e.g. Hostile Environment Mining Rig) have additional properties not present on non-mining hulls: `Max Ore Hoppers` (slot count for Ore Hopper components), `Raw Material Capacity` (base hopper capacity from the hull itself), and `Eng Capacity Available` (total engineering capacity the hull provides, as opposed to `Eng Capacity Required` which components consume). The Hull BlueprintType definition in BaselineData.json needs to be updated to include these properties: `Max Ore Hoppers`, `Raw Material Capacity`, `Eng Capacity Available`.
- `Raw Material Capacity` on the hull is the base hopper volume. Ore Hopper components add their own `Raw Material Capacity` on top. Total hopper capacity = hull `Raw Material Capacity` + sum(Ore Hopper `Raw Material Capacity`).
- `Eng Capacity Available` on the hull is the total engineering budget. Components consume `Eng Capacity Required`. The ShipStats `EngCapacityUsed` should be compared against the hull's `Eng Capacity Available` to detect over-engineering.
- The SlotType string is used as-is in `ShipComponentSlot.SlotType`. The mapping from BlueprintType ID to SlotType is done by a lookup table in `ShipBuildService` (or a constants class).
- When installing a component, the service resolves the blueprint's BluePrintType to a SlotType via this mapping, then checks the hull's available count for that SlotType.

### OQ-32: Weapon Slot Size Enforcement (Iteration 2) â€” RESOLVED

**Decision:** Option (a) â€” SlotType encodes size. Weapons use `WeaponSmall`, `WeaponMedium`, `WeaponLarge` as separate slot types. The weapon blueprint's BluePrintType ID encodes the size (e.g. `Beamer/Small` â†’ `WeaponSmall`, `Railgun/Large` â†’ `WeaponLarge`). The mapping table above defines which BlueprintType IDs map to which SlotType. No additional `SlotSize` field is needed on `ShipComponentSlot`.

Validation: When installing a weapon, the service extracts the size from the BlueprintType ID (the `/Small`, `/Medium`, `/Large` suffix), maps it to the corresponding `WeaponSmall`/`WeaponMedium`/`WeaponLarge` SlotType, and checks the hull's available mount count for that size.

### OQ-33: WarehouseOverflowRule Delivery Route (Iteration 6) â€” RESOLVED

**Decision:** Option (a) â€” the rule includes a `DeliveryRouteUUID` field. The user picks the route when creating the overflow rule. The background processor uses that route when generating the delivery plan.

Model change â€” add to `WarehouseOverflowRule`:
```csharp
public string DeliveryRouteUUID { get; set; } = string.Empty;
```

The Overflow tab on FormColony includes a route selector combo in the add-rule panel. If the selected route doesn't include both the source colony and the destination as stops, the UI shows a validation warning.

### OQ-34: BuildItem Status â€” Cascade vs Manual Override (Iteration 1) â€” RESOLVED

**Decision:** Manual status transitions always win. The user has an incomplete view of the data (no game API), so the tool must trust the user's judgment. The cascade processor only advances status automatically in one direction (Staged â†’ Delivering â†’ Ready) and never overwrites a status that the user has manually set forward.

Rules:
- The cascade can set `Delivering` (when a delivery plan is generated) and `Ready` (when resources are confirmed available). It never sets `InProgress` or `Completed` â€” those are always manual.
- If the user manually sets a status forward (e.g. skips from Staged straight to Ready because they know resources are there), the cascade respects that and does not revert it.
- If the user manually sets `Completed`, the cascade skips that item entirely â€” it's done.
- If the user manually sets `InProgress`, the cascade does not revert to Ready or Delivering even if the resource check says resources are missing. The user is saying "I started this in-game" and the tool trusts that.
- The cascade only moves status forward, never backward. The user can move status backward manually if they made a mistake.

This means the cascade is advisory â€” it advances items through the pipeline when it can confirm conditions are met, but the user can always override by manually setting any status. No `ManualOverride` flag is needed; the rule is simply "cascade never decreases status ordinal."

Status ordinal: Staged(0) < Delivering(1) < Ready(2) < InProgress(3) < Completed(4). Cascade sets `max(currentStatus, computedStatus)`.

### OQ-35: StockTarget Replenishment Plan Designation (Iteration 7) â€” RESOLVED

**Decision:** Option (c) â€” user designates a target build plan on the StockPlan. When the stock target check finds shortfalls, replenishment items are created in the designated plan.

Model change â€” add to `StockPlan`:
```csharp
public string ReplenishmentBuildPlanUUID { get; set; } = string.Empty;
```

FormStockTargets shows a build plan selector combo on the plan detail panel. If no replenishment plan is designated when "Check & Generate Orders" is clicked, the form prompts the user to select or create one before proceeding.

### OQ-36: Crate Volume in Delivery Planning (Iteration 3) â€” RESOLVED

**Decision:** Crates have no volume or mass of their own â€” they are purely an organizational concept. A crate's volume is the sum of the volumes of items inside it. A crate's mass is the sum of the masses of items inside it. This applies everywhere: ship cargo capacity checks, delivery plan trip splitting, and any other volume/mass computation.

For delivery planning, crate contents are included in the cargo volume computation. A crate with 10 items totaling 500 mÂ³ counts as 500 mÂ³ toward the ship's cargo capacity, not as a single item with its own volume.

### OQ-37: Station Blueprint Type (Iteration 4) â€” DEFERRED

**Decision:** Deferred until the game releases the rest of the player station features. In-game, stations use "build packages" rather than blueprints, but build packages currently have no properties (no slot counts, no stats). Until the game defines what properties a station build package has, we cannot model station slot counts or component limits.

For now:
- `Station.StationBlueprintUUID` remains in the model but is unused. Player-owned station component management is deferred.
- Government stations and basic station data (name, type, ownership, holds) work without a blueprint.
- The FormStation Components tab is deferred until the game provides build package properties. The Hold and Munitions tabs are implemented in Iteration 4; the Components tab is added when the game data is available.
- `StationStats` computation is deferred alongside the Components tab.
- The `StationBlueprintUUID` field and `Components` list on Station are present in the model with empty defaults so no migration is needed when the feature is eventually implemented.

### OQ-38: ExternalCharacter Name Collision (Iteration 1) â€” RESOLVED

**Decision:** Not an issue. The game has a single server and character names are unique â€” no duplicate names are allowed. Deterministic UUID from character name is safe.

### OQ-39: SupplyChain Delivery Route Selection (Iteration 6) â€” RESOLVED

**Decision:** Consistent with OQ-33 â€” each `SupplyChainStage` that triggers a delivery (PickUp, Refine, Deliver stages with accumulation thresholds) includes a `DeliveryRouteUUID` field. The user picks the route when defining the stage.

Model change â€” add to `SupplyChainStage`:
```csharp
public string DeliveryRouteUUID { get; set; } = string.Empty;  // Route for threshold-triggered deliveries
```

Mine and AsteroidMine stages don't need a route (they produce at a location, they don't move resources). PickUp/Refine/Deliver stages that have an `AccumulationThreshold > 0` require a route to be set. FormSupplyChain validates this on save.

### OQ-40: Resource Check Scope â€” Colony Warehouse Only or Also Station Holds? (Iteration 1) â€” RESOLVED

**Decision:** Station holds are included in resource checks. Only the current player's hold at each station is checked â€” `station.Holds[currentPlayerUUID]`. All plans, stock levels, and automations are owned by a specific player; there are no cross-player automations.

For ResourceCheckService, the scope is:
- Colony warehouse at the allocated colony (primary)
- Station holds for the current player at stations on the associated delivery route (secondary)

This reduces false shortfalls â€” if the player already has resources at a station on the route, the system knows they're available and doesn't flag them as missing. The resource check takes a `Func<string, Station> stationFinder` and the current player UUID to look up the relevant holds.
