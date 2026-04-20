<!-- Extracted from .kiro/specs/empire-systems/design.md, lines 1842-1947 — PlayerContext Changes -->
# PlayerContext Changes

## Thread Safety

All new `List<T>` fields are protected by the existing `_listLock`. Access patterns:
- **Read**: acquire `lock(_listLock)`, snapshot the list (`new List<T>(list)`), release lock, iterate the snapshot.
- **Write**: acquire `lock(_listLock)`, mutate the list, release lock, then fire events and call WriteContext outside the lock.
- **WriteContext**: snapshots all lists (including new ones) under `_listLock`, serializes outside the lock.
- **Find methods**: acquire `lock(_listLock)` for cache rebuild/lookup, release before returning.

## New Fields

```csharp
// Existing entities — remain as BindingList<T> until BL-069 migration
public BindingList<DeliveryRoute> DeliveryRouteList;
public BindingList<DeliveryPlan> DeliveryPlanList;
public BindingList<PricingPlan> PricingPlanList;

// New entities use List<T> — forms build their own display lists
// from filtered queries, so BindingList change notifications aren't needed.
public List<BuildPlan> BuildPlanList;
public List<ShipTemplate> ShipTemplateList;
public List<Ship> ShipList;
public List<Station> StationList;
public List<MarketListing> MarketListingList;
public List<MarketTransaction> MarketTransactionList;
public List<StockPlan> StockPlanList;
public List<StockProfile> StockProfileList;
public List<SupplyChain> SupplyChainList;
public List<WarehouseOverflowRule> WarehouseOverflowRuleList;
public List<Faction> FactionList;
public List<ExternalCharacter> ExternalCharacterList;
public List<Asteroid> AsteroidList;
```

## New Init Methods

Each follows the existing pattern (null-coalesce, sort by name, create List):

```csharp
public void InitBuildPlans(PlayerRoot playerRoot)
public void InitShipTemplates(PlayerRoot playerRoot)
public void InitShips(PlayerRoot playerRoot)
public void InitStations(PlayerRoot playerRoot)
public void InitMarketListings(PlayerRoot playerRoot)
public void InitMarketTransactions(PlayerRoot playerRoot)
public void InitStockPlans(PlayerRoot playerRoot)
public void InitStockProfiles(PlayerRoot playerRoot)
public void InitSupplyChains(PlayerRoot playerRoot)
public void InitWarehouseOverflowRules(PlayerRoot playerRoot)
public void InitFactions(PlayerRoot playerRoot)
public void InitExternalCharacters(PlayerRoot playerRoot)
public void InitAsteroids(PlayerRoot playerRoot)
```

## WriteContext Changes

Add to WriteContext after existing serialization:

```csharp
playerRoot.BuildPlan = BuildPlanList.ToArray();
playerRoot.ShipTemplate = ShipTemplateList.ToArray();
playerRoot.Ship = ShipList.ToArray();
playerRoot.Station = StationList.ToArray();
playerRoot.MarketListing = MarketListingList.ToArray();
playerRoot.MarketTransaction = MarketTransactionList.ToArray();
playerRoot.StockPlan = StockPlanList.ToArray();
playerRoot.StockProfile = StockProfileList.ToArray();
playerRoot.SupplyChain = SupplyChainList.ToArray();
playerRoot.WarehouseOverflowRule = WarehouseOverflowRuleList.ToArray();
playerRoot.Faction = FactionList.ToArray();
playerRoot.ExternalCharacter = ExternalCharacterList.ToArray();
playerRoot.Asteroid = AsteroidList.ToArray();
```

## CascadeDeletePlayer Changes

Add removal loops for all new entity types with OwnerUUID matching the deleted player. Government stations (empty OwnerUUID) are excluded.

## CleanupOrphanedData Changes

Add orphan cleanup for all new entity types, same pattern as existing.

## New Events

```csharp
public event EventHandler BuildPlanDataChanged;
public event EventHandler MarketDataChanged;
public event EventHandler StationDataChanged;
```

## New Convenience Methods

```csharp
public List<BuildPlan> GetCurrentPlayerBuildPlans()
public List<ShipTemplate> GetCurrentPlayerShipTemplates()
public List<Ship> GetCurrentPlayerShips()
public List<Station> GetCurrentPlayerStations()  // Includes government stations
public List<MarketListing> GetCurrentPlayerListings()
public List<MarketTransaction> GetCurrentPlayerTransactions()
public List<StockPlan> GetCurrentPlayerStockPlans()
public List<StockProfile> GetCurrentPlayerStockProfiles()
public List<SupplyChain> GetCurrentPlayerSupplyChains()
public List<WarehouseOverflowRule> GetCurrentPlayerOverflowRules()
```
