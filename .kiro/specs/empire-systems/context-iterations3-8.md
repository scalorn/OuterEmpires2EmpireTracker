# Empire Systems ? Iteration Context: Iterations 3-8

## Iteration 3: Ship-Aware Delivery
- Ship assignment on delivery plans (ShipUUID)
- Cargo volume computation (crate contents recursive one level, crate itself = 0)
- Volume/mass display on execution form
- Volume warning (advisory, not blocking)
- Trip splitting when cargo exceeds capacity

## Iteration 4: Stations
- FormStation: Hold tab (CrateInventoryPanel, editable Condition/MaxRepair), Components tab (editable damage, hull row first), Munitions tab
- Station hold scoped to current player (no player picker)
- StationReferenceCounter (10+ reference sources)
- Update delivery routes/plans/execution for Station and Asteroid stops
- RouteStop Purpose column and FuelEstimate display
- Refuel stop checklist items in execution form

## Iteration 5: Market
- FormMarket: Listings tab, Transactions tab (faction filter, condition column), Summary tab
- MarketService: RecordSale (condition + faction snapshots), RecordPurchase, ComputeProfitLoss
- Cascade: sale sets CascadeStockTargetsDirty, purchase sets CascadeResourceCheckDirty
- MarketListingReferenceCounter

## Iteration 6: Production Queue + Supply Chain + Asteroids
- Enable Mining/Refining/Research BuildItemTypes
- Time-splitting (SequenceInStructure) and dependency tracking (DependsOnUUID)
- FormAsteroid with reserves grid, auto-create on asteroid survey import
- FormSupplyChain with PickUp/Refine/Research/Deliver stages, IsActive toggle
- SupplyChainService.CheckThresholds
- Warehouse Overflow tab on FormColony with Active checkbox column
- Asteroid survey integration (SurveyType filter, purity filter, min amount filter)

## Iteration 7: Stock Targets
- StockTargetService: CheckTargets (OR within plan, AND across plans), GenerateReplenishmentItems
- FormStockTargets: Targets & Plans tab (IsActive, Quick Add, Check & Generate Orders) + Profiles tab (IsActive, Group/Plan grid, logic summary)
- StockPlanReferenceCounter
- Cascade integration in BackgroundProcessor

## Iteration 8: Delivery Auto-Fill Time Horizon
- Time horizon parameter on flatpack auto-fill
- Filter by build completion time
- Persist as preference

## Key Files for All Iterations
- .kiro/steering/empire-patterns.md (always load first)
- .kiro/specs/empire-systems/design.md (relevant sections)
- .kiro/specs/empire-systems/tasks.md (specific task numbers)
- .kiro/steering/forms.md (WinForms patterns)
