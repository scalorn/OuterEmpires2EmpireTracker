# Delivery Plans

## Phase 4: Delivery Planning — Data Model

**REQ-DEL-030** A `DeliveryPlan` SHALL have: UUID, Name, OwnerUUID, RouteUUID, and an ordered list of `DeliveryPlanStop` entries.
**REQ-DEL-031** A `DeliveryPlanStop` SHALL have: ColonyUUID, Sequence, a list of `DeliveryItem` for drop-off, and a list of `DeliveryItem` for pick-up.
**REQ-DEL-032** A `DeliveryItem` SHALL have: ItemType (same enum as colony warehouse), BaseItemTypeID, Name, Quantity, and a Delivered boolean (default false).
**REQ-DEL-033** DeliveryPlan SHALL be persisted to JSON as part of PlayerData.json (new `DeliveryPlan[]` array on PlayerRoot).
**REQ-DEL-034** DeliveryPlan.OwnerUUID SHALL match the owning player's UUID. Plans are per-player.
**REQ-DEL-035** A delivery item can be any item type that can be placed in a colony warehouse: Resource, Commodity, WorkDetail, Blueprint, Survey, ShipPart, ShipHull, Munition, Flatpack, SpaceBuildPackage, Share.

## Phase 5: Delivery Planning — UI

**REQ-DEL-040** The Route Builder form SHALL have a second tab "Plan" for delivery planning.
**REQ-DEL-041** The Plan tab SHALL show a plan selector (list with filter, "Show Completed" checkbox, New/Delete buttons) allowing multiple plans per route.
**REQ-DEL-041a** New plans SHALL auto-suggest a name of "RouteName - YYYY-MM-DD". The user can edit the name.
**REQ-DEL-041b** Completed plans SHALL be hidden by default. The "Show Completed" checkbox reveals them.
**REQ-DEL-041c** Plans SHALL be deletable from the plan list.
**REQ-DEL-042** The Plan tab SHALL show the selected stop's drop-off and pick-up item lists for the selected plan.
**REQ-DEL-043** The user SHALL be able to add items to drop-off or pick-up lists using the same item type → filter → item picker → quantity pattern as the colony warehouse.
**REQ-DEL-044** The user SHALL be able to remove items from the lists.
**REQ-DEL-045** DeliveryPlan SHALL have a `Completed` boolean property (default false) and a `Name` string property.

## Phase 7: Auto-Fill Delivery Plans

**REQ-DEL-060** The planning tab SHALL offer auto-fill options by request type:
  - Commodity requests: fill from unfulfilled CommodityRequested entries
  - Workers: fill from ideal vs actual worker gaps
  - Manufacturing resources: fill from active manufacturing resource needs
  - Flatpacks: fill from planned/unbuilt structures
**REQ-DEL-061** Flatpack auto-fill SHALL support a time horizon parameter (e.g. "next N days of building"). The time horizon value SHALL be persisted as a user preference so it survives application restarts.  
**REQ-DEL-062** Auto-fill SHALL be additive — it adds to existing plan items, not replaces them.

## Phase 8: Ship Assignment

**REQ-DEL-070** DeliveryPlan SHALL have a ShipUUID field referencing the assigned ship for the delivery.  
**REQ-DEL-071** When a ship is assigned, delivery plans SHALL respect the ship's cargo capacity. Items exceeding capacity SHALL be split across multiple trips.  
**REQ-DEL-072** Cargo volume for a delivery plan SHALL be computed by summing Item.Volume × Quantity for all items across all stops.  
**REQ-DEL-073** The delivery planning UI SHALL display the assigned ship and cargo volume utilization.  
