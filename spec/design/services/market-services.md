<!-- Extracted from spec/design/services.md — Market domain -->
# Services — Market

## MarketService (Iteration 5)

Static service in `Services/MarketService.cs`.

```csharp
public static class MarketService
{
    public static MarketTransaction RecordSale(
        MarketListing listing, int quantitySold, decimal pricePerUnit,
        string counterpartyName, string counterpartyFactionName, string stationUUID);
    public static MarketTransaction RecordPurchase(
        ItemType.ItemTypeEnum itemType, string itemName, string itemReferenceID,
        int quantity, decimal pricePerUnit, string stationUUID,
        string counterpartyName, string counterpartyFactionName,
        string ownerUUID, Func<string, Station> stationFinder);
    public static ProfitLossSummary ComputeProfitLoss(
        IEnumerable<MarketTransaction> transactions,
        DateTime? startDate = null, DateTime? endDate = null,
        string itemNameFilter = null, string stationUUIDFilter = null);
}
```

## MarketListingService (BL-114)

Instance service in `Services/MarketListingService.cs`. Sole mutator of MarketListing entities.
Uses MarketListingCreateRequest and MarketListingUpdateRequest DTOs for input.

```csharp
public class MarketListingService
{
    public MarketListingService(PlayerContext playerContext);
    public ReadOnlyMarketListing CreateListing(MarketListingCreateRequest request);
    public ReadOnlyMarketListing UpdateListing(string uuid, MarketListingUpdateRequest request);
    public void DeleteListing(string uuid);
    public MarketTransaction RecordSale(string listingUUID, int quantity,
        decimal pricePerUnit, string counterparty, string counterpartyFaction, string stationUUID);
}
```

## Class Diagram

```mermaid
classDiagram
    class MarketService {
        <<static>>
        +RecordSale(listing, quantitySold, pricePerUnit, counterpartyName, counterpartyFactionName, stationUUID) MarketTransaction
        +RecordPurchase(itemType, itemName, itemReferenceID, quantity, pricePerUnit, stationUUID, counterpartyName, counterpartyFactionName, ownerUUID, stationFinder) MarketTransaction
        +ComputeProfitLoss(transactions, startDate, endDate, itemNameFilter, stationUUIDFilter) ProfitLossSummary
    }

    class MarketListingService {
        -Logger Log
        -PlayerContext _playerContext
        +MarketListingService(playerContext)
        +CreateListing(request) ReadOnlyMarketListing
        +UpdateListing(uuid, request) ReadOnlyMarketListing
        +DeleteListing(uuid) void
        +RecordSale(listingUUID, quantity, pricePerUnit, counterparty, counterpartyFaction, stationUUID) MarketTransaction
    }

    class MarketListingCreateRequest {
        +string ItemName
        +ItemTypeEnum ItemType
        +string ItemReferenceID
        +string StationUUID
        +int Quantity
        +decimal PricePerUnit
        +int CurrentHP
        +int MaxHP
        +int MaxRepairPercent
    }

    class MarketListingUpdateRequest {
        +string ItemName
        +ItemTypeEnum ItemType
        +string ItemReferenceID
        +string StationUUID
        +int Quantity
        +decimal PricePerUnit
        +int CurrentHP
        +int MaxHP
        +int MaxRepairPercent
    }

    class ReadOnlyMarketListing {
        +string UUID
        +string ItemName
        +decimal PricePerUnit
        +int Quantity
    }

    class MarketTransaction {
        +string UUID
        +string ItemName
        +int Quantity
        +decimal PricePerUnit
        +DateTime Timestamp
    }

    class ProfitLossSummary {
        +decimal TotalRevenue
        +decimal TotalCost
        +decimal NetProfit
    }

    MarketListingService --> PlayerContext : uses
    MarketListingService --> MarketService : delegates RecordSale
    MarketListingService --> ReadOnlyMarketListing : returns
    MarketListingService --> MarketTransaction : returns
    MarketService --> MarketTransaction : creates
    MarketService --> ProfitLossSummary : creates
    MarketListingService ..> MarketListingCreateRequest : accepts
    MarketListingService ..> MarketListingUpdateRequest : accepts
```
