---
description: Data model collections are unordered bags. Sort at consumption via CollectionSortHelper.
inclusion: auto
---

# Data Model Ordering Invariant

## The Rule

**All data model collections (List<T>, Dictionary<K,V>) are unordered bags.** Their element order may change at any time due to serialization, deserialization, migration, or concurrent access. The SerializationSorter reorders collections by UUID for deterministic JSON diffs, which means the live list order after a save/load cycle may differ from the order the application originally established.

**Never write code that assumes a meaningful order from a data model collection.** If you need elements in a specific order, you must sort explicitly at the point of consumption.

## Approved Consumption Patterns

There are exactly three approved patterns for obtaining ordered data from model collections:

### Pattern 1: Sort At Consumption (via CollectionSortHelper)

Call the appropriate CollectionSortHelper method immediately before use. This is the most common pattern.

`csharp
// CORRECT: Sort at consumption
var sorted = CollectionSortHelper.OrderStructures(colony.Structures);
foreach (var structure in sorted)
{
    // process in BuildQueueSequence order
}

// CORRECT: Sort for display
var colonies = CollectionSortHelper.OrderColonies(playerContext.ColonyList);
foreach (var c in colonies)
    listBox.Items.Add(c.ColonyName);
`

### Pattern 2: Sorted Cache (ViewModel pattern)

Build an IReadOnlyList<T> sorted via CollectionSortHelper and invalidate the cache when the underlying collection changes.

`csharp
// CORRECT: ViewModel maintains a sorted cache
private IReadOnlyList<ColonyStructureViewModel> _structureViewModels;

public IReadOnlyList<ColonyStructureViewModel> StructureViewModels
{
    get
    {
        if (_structureViewModels == null)
        {
            var sorted = CollectionSortHelper.OrderStructures(_colony.Structures);
            _structureViewModels = sorted.Select(s => new ColonyStructureViewModel(s)).ToList().AsReadOnly();
        }
        return _structureViewModels;
    }
}

// Invalidate when collection changes
public void InvalidateStructures() => _structureViewModels = null;
`

### Pattern 3: Parameter Pattern (pre-sorted parameter)

Accept a pre-sorted IReadOnlyList<T> from the caller. Document via XML doc or parameter naming.

`csharp
// CORRECT: Method receives pre-sorted data
/// <summary>
/// Processes structures in build queue order.
/// </summary>
/// <param name="sortedStructures">Structures sorted by BuildQueueSequence via CollectionSortHelper.</param>
public void ProcessStructures(IReadOnlyList<ColonyStructure> sortedStructures)
{
    // Use directly  caller is responsible for sorting
}
`

## What NOT To Do

`csharp
// WRONG: Inline .OrderBy() on a model collection
var sorted = colony.Structures.OrderBy(s => s.BuildQueueSequence).ToList();

// WRONG: Assuming list order is meaningful
var first = colony.Structures[0]; // NOT necessarily the first by any domain key

// WRONG: Modifying the data model to enforce ordering
public SortedList<int, ColonyStructure> Structures { get; set; } // NO

// WRONG: Auto-sorting on deserialization
[JsonConverter(typeof(SortedListConverter))] // NO
`

## CollectionSortHelper Methods

All sorting of data model collections MUST go through CollectionSortHelper (in OE2EmpireTracker/Services/CollectionSortHelper.cs). The only other file allowed to contain sort expressions on model types is SerializationSorter.cs.

Key methods:

| Method | Sort Key |
|--------|----------|
| OrderStructures(IEnumerable<ColonyStructure>) | BuildQueueSequence asc |
| OrderStructuresDescending(IEnumerable<ColonyStructure>) | BuildQueueSequence desc |
| OrderRouteStops(IEnumerable<RouteStop>) | Sequence asc |
| OrderPlanStops(IEnumerable<DeliveryPlanStop>) | Sequence asc |
| OrderSupplyChainStages(IEnumerable<SupplyChainStage>) | Sequence asc |
| OrderColonies(IEnumerable<Colony>) | SystemName, PlanetName, ColonyName asc |
| OrderBlueprints(IEnumerable<Blueprint>) | ExtendedName asc |
| OrderBlueprintsByEvolution(IEnumerable<Blueprint>) | Evolution asc |
| OrderBlueprintsByEvolutionDescending(IEnumerable<Blueprint>) | Evolution desc |
| OrderPlayerProfiles(IEnumerable<PlayerProfile>) | Name asc |
| OrderSurveys(IEnumerable<Survey>) | PlanetName, GameID asc |
| OrderCommodityRequests(IEnumerable<CommodityRequested>) | Name asc |
| OrderCommodityRequestsByNeedBy(IEnumerable<CommodityRequested>) | NeedBy asc |
| OrderDeliveryItems(IEnumerable<DeliveryItem>) | Name asc |
| OrderBuildItems(IEnumerable<BuildItem>) | ItemName asc |
| OrderAsteroidReserves(IEnumerable<AsteroidReserve>) | ResourceName asc |
| OrderComponents(IEnumerable<ShipComponentSlot>) | SlotType, SlotIndex asc |
| OrderDeliveryRoutes(IEnumerable<DeliveryRoute>) | Name asc |
| OrderDeliveryPlans(IEnumerable<DeliveryPlan>) | Name asc |
| OrderPricingPlans(IEnumerable<PricingPlan>) | Name asc |
| OrderBuildPlans(IEnumerable<BuildPlan>) | Name asc |
| OrderShipTemplates(IEnumerable<ShipTemplate>) | Name asc |
| OrderShips(IEnumerable<Ship>) | Name asc |
| OrderStations(IEnumerable<Station>) | Name asc |
| OrderMarketListings(IEnumerable<MarketListing>) | ItemName asc |
| OrderMarketTransactions(IEnumerable<MarketTransaction>) | ItemName asc |
| OrderMarketTransactionsByTimestamp(IEnumerable<MarketTransaction>) | Timestamp desc |
| OrderStockPlans(IEnumerable<StockPlan>) | Name asc |
| OrderStockProfiles(IEnumerable<StockProfile>) | Name asc |
| OrderSupplyChains(IEnumerable<SupplyChain>) | Name asc |
| OrderWarehouseOverflowRules(IEnumerable<WarehouseOverflowRule>) | ResourceName asc |
| OrderFactions(IEnumerable<Faction>) | Name asc |
| OrderExternalCharacters(IEnumerable<ExternalCharacter>) | Name asc |
| OrderAsteroids(IEnumerable<Asteroid>) | Name asc |
| OrderActivityRowsByTimeRemaining(IEnumerable<ActivityRow>) | GetSecondsRemaining() asc |
| OrderCountdownsByTimeRemaining(IEnumerable<CountDownTimeReference>) | TimeRemaining asc |
| OrderByName<T>(IEnumerable<T>, Func<T, string>) | Generic name selector asc |

## Sort Key Registry

Complete list of ordered collections and their sort keys:

| Collection | Sort Key |
|-----------|----------|
| PlayerRoot.PlayerProfile | Name asc |
| PlayerRoot.Blueprint | ExtendedName asc |
| PlayerRoot.Survey | PlanetName asc, GameID asc |
| PlayerRoot.Colony | SystemName asc, PlanetName asc, ColonyName asc |
| PlayerRoot.DeliveryRoute | Name asc |
| PlayerRoot.DeliveryPlan | Name asc |
| PlayerRoot.PricingPlan | Name asc |
| PlayerRoot.BuildPlan | Name asc |
| PlayerRoot.ShipTemplate | Name asc |
| PlayerRoot.Ship | Name asc |
| PlayerRoot.Station | Name asc |
| PlayerRoot.MarketListing | ItemName asc |
| PlayerRoot.MarketTransaction | ItemName asc |
| PlayerRoot.StockPlan | Name asc |
| PlayerRoot.StockProfile | Name asc |
| PlayerRoot.SupplyChain | Name asc |
| PlayerRoot.WarehouseOverflowRule | ResourceName asc |
| PlayerRoot.Faction | Name asc |
| PlayerRoot.ExternalCharacter | Name asc |
| PlayerRoot.Asteroid | Name asc |
| Colony.Structures | BuildQueueSequence asc |
| Colony.Commodities | Name asc (or NeedBy for activity views) |
| Colony.Items (ItemBag) | ExtendedName asc |
| DeliveryRoute.Stops | Sequence asc |
| DeliveryPlan.Stops | Sequence asc |
| DeliveryPlanStop.DropOff / PickUp | Name asc |
| SupplyChain.Stages | Sequence asc |
| BuildPlan.Items | ItemName asc |
| StockPlan.Targets | ItemType asc, item-type-specific name asc |
| StockProfile.Entries | GroupID asc, StockPlan name asc |
| Asteroid.Reserves | ResourceName asc |
| Station.Holds | Player Name asc |
| Station.Components | SlotType asc, SlotIndex asc |
| ShipTemplate.Components | SlotType asc, SlotIndex asc |
| Ship.Components | SlotType asc, SlotIndex asc |
| PropertyBag dictionary | Property name asc |

## Adding New Collections

When adding a new List<T> or Dictionary<K,V> property to a data model entity:

1. Add the collection and its sort key to the Sort Key Registry table above
2. Add a corresponding method to CollectionSortHelper.cs
3. Add the collection to SerializationSorter if it needs deterministic JSON output
4. Ensure all consumers use the new CollectionSortHelper method (never inline .OrderBy())
5. Run 
ode .kiro/tools/inline-sort-check.js to verify no inline sorts were introduced

## Do Not Modify the Data Model

The data model (Models/ POCOs) must NOT be changed to enforce ordering:
- No SortedList<K,V> or SortedDictionary<K,V> properties
- No auto-sort on deserialization (no custom JSON converters that sort)
- No IComparable implementations that imply a natural order for list storage
- The model stores data; ordering is a consumption concern