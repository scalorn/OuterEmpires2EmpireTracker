<!-- Extracted from .kiro/specs/empire-systems/design.md — Reference Counting section -->
# Reference Counting & Delete Protection

Every entity that can be referenced by UUID from another entity needs a reference counter service and delete protection in its form. This extends the existing pattern (BlueprintReferenceCounter, ColonyReferenceCounter, SurveyReferenceCounter) to all new entity types.

## Reference Graph

| Entity | Referenced By | Reference Counter |
|---|---|---|
| Blueprint | ColonyStructure (Flatpack, Research, Manufacturing), BuildItem.BlueprintUUID, ShipTemplate.HullBlueprintUUID, ShipTemplate.Components[].BlueprintUUID, Ship.HullBlueprintUUID, Ship.Components[].BlueprintUUID, Station.Components[].BlueprintUUID, Station.StationBlueprintUUID, Survey.ScannerBlueprintUUID, Blueprint.BaseBlueprintUUID, MarketListing.ItemReferenceID (when Blueprint), MarketTransaction.ItemReferenceID (when Blueprint), StockPlan.Targets[].ItemReferenceID (when Blueprint) | BlueprintReferenceCounter (expand) |
| Colony | DeliveryRoute stops, DeliveryPlan stops, BuildItem.BuildLocationUUID (when Colony), SupplyChainStage.LocationUUID (when Colony), WarehouseOverflowRule.ColonyUUID, StockPlan.Targets[].LocationUUID (when Colony) | ColonyReferenceCounter (expand) |
| Survey | ColonyStructure.MiningSurvey, BuildItem.MiningSurveyUUID | SurveyReferenceCounter (expand) |
| Station | DeliveryRoute/Plan stops (when Station), Ship.LocationUUID (when Station), MarketListing.StationUUID, MarketTransaction.StationUUID, BuildItem.AssemblyLocationUUID (when Station), SupplyChainStage.LocationUUID (when Station), StockPlan.Targets[].LocationUUID (when Station), WarehouseOverflowRule.DestinationUUID (when Station) | StationReferenceCounter (new) |
| ShipTemplate | Ship.TemplateUUID, BuildItem.ShipTemplateUUID, StockPlan.Targets[].ShipTemplateUUID | ShipTemplateReferenceCounter (new) |
| Ship | DeliveryPlan.ShipUUID, BuildItem.BuildLocationUUID (when Ship, future) | ShipReferenceCounter (new) |
| Asteroid | Survey.AsteroidUUID, SupplyChainStage.LocationUUID (when Asteroid), DeliveryRoute stops (when Asteroid) | AsteroidReferenceCounter (new) |
| Faction | PlayerProfile.FactionUUID, ExternalCharacter.FactionUUID | FactionReferenceCounter (new) |
| BuildPlan | StockPlan.ReplenishmentBuildPlanUUID | BuildPlanReferenceCounter (new) |
| DeliveryRoute | DeliveryPlan.RouteUUID, WarehouseOverflowRule.DeliveryRouteUUID, SupplyChainStage.DeliveryRouteUUID | DeliveryRouteReferenceCounter (new) |
| DeliveryPlan | BuildPlan.DeliveryPlanUUID | DeliveryPlanReferenceCounter (new) |
| StockPlan | StockProfileEntry.StockPlanUUID | StockPlanReferenceCounter (new) |
| MarketListing | MarketTransaction.ListingUUID | MarketListingReferenceCounter (new) |
| SupplyChain | — (not referenced) | — |
| WarehouseOverflowRule | — (not referenced) | — |
| ExternalCharacter | — (combo lookups, not UUID refs) | — |
| StockProfile | — (not referenced) | — |

## Existing Reference Counters — Required Expansions

**BlueprintReferenceCounter** — currently counts: ColonyStructure, Blueprint.BaseBlueprintUUID, Survey.ScannerBlueprintUUID. Must add:
- BuildItem.BlueprintUUID
- ShipTemplate.HullBlueprintUUID and Components[].BlueprintUUID
- Ship.HullBlueprintUUID and Components[].BlueprintUUID
- Station.StationBlueprintUUID and Components[].BlueprintUUID
- MarketListing.ItemReferenceID (when Blueprint)
- StockPlan.Targets[].ItemReferenceID (when Blueprint)

**ColonyReferenceCounter** — currently counts: DeliveryRoute stops, DeliveryPlan stops. Must add:
- BuildItem.BuildLocationUUID (when Colony)
- SupplyChainStage.LocationUUID (when Colony)
- WarehouseOverflowRule.ColonyUUID and .DestinationUUID (when Colony)
- StockPlan.Targets[].LocationUUID (when Colony)

**SurveyReferenceCounter** — currently counts: ColonyStructure.MiningSurvey. Must add:
- BuildItem.MiningSurveyUUID

## New Reference Counter Services

Each follows the same pattern: constructor takes collections to search, `CountReferences(uuid)` returns typed report with per-source counts and `TotalCount`.

```csharp
// StationReferenceCounter, ShipTemplateReferenceCounter, ShipReferenceCounter,
// AsteroidReferenceCounter, FactionReferenceCounter, DeliveryRouteReferenceCounter,
// DeliveryPlanReferenceCounter, StockPlanReferenceCounter,
// MarketListingReferenceCounter, BuildPlanReferenceCounter
```

See design.md for full constructor signatures.

## Form Integration

| Form | Entity | Reference Counter | Refs Column |
|---|---|---|---|
| FormBuildPlanner | BuildPlan | BuildPlanReferenceCounter | Yes |
| FormShipTemplate | ShipTemplate | ShipTemplateReferenceCounter | Yes |
| FormShipInstance | Ship | ShipReferenceCounter | Yes |
| FormStation | Station | StationReferenceCounter | Yes |
| FormMarket (Listings) | MarketListing | MarketListingReferenceCounter | Yes |
| FormStockTargets (Plans) | StockPlan | StockPlanReferenceCounter | Yes |
| FormContacts (Factions) | Faction | FactionReferenceCounter | Yes |
| FormContacts (ExtChars) | ExternalCharacter | — | No |
| FormAsteroid | Asteroid | AsteroidReferenceCounter | Yes |
| FormSupplyChain | SupplyChain | — | No |
| FormDeliveryRoute | DeliveryRoute | DeliveryRouteReferenceCounter | Yes (new) |
| FormColony (Overflow) | WarehouseOverflowRule | — | No |
| FormStockTargets (Profiles) | StockProfile | — | No |
| FormBlueprintV2 | Blueprint | BlueprintReferenceCounter (expanded) | Yes (existing) |
| FormColonyV2 | Colony | ColonyReferenceCounter (expanded) | Yes (existing) |
| FormSurvey | Survey | SurveyReferenceCounter (expanded) | Yes (existing) |

Pattern: Create counter → show Refs column → disable Delete when TotalCount > 0 → block delete with MessageBox listing sources.

## Reference Counter Caching

Pre-compute `Dictionary<string, int>` mapping UUID → total count in one pass:

```csharp
public Dictionary<string, int> BuildReferenceMap()
{
    var map = new Dictionary<string, int>();
    // Single pass through all referencing collections
    // Increment map[referencedUUID] for each reference found
    return map;
}
```

Changes counting from O(items × sources) to O(sources) + O(items).