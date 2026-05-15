using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for market listing and transaction operations.
/// </summary>
public sealed class MarketService
{
    private readonly DataService _dataService;
    private readonly ILogger<MarketService> _logger;

    public MarketService(DataService dataService, ILogger<MarketService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void CreateListing(MarketListingCreateRequest request)
    {
        var listing = new MarketListing
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            ItemName = request.ItemName,
            ItemType = request.ItemType,
            ItemReferenceID = request.ItemReferenceID,
            StationUUID = request.StationUUID,
            Quantity = request.Quantity,
            PricePerUnit = request.PricePerUnit,
            CurrentHP = request.CurrentHP,
            MaxHP = request.MaxHP,
            MaxRepairPercent = request.MaxRepairPercent,
        };

        _dataService.AddMarketListing(listing);
        _dataService.OnMarketDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Created market listing {Item} ({UUID})", listing.ItemName, listing.UUID);
    }

    public void UpdateListing(string uuid, MarketListingUpdateRequest request)
    {
        var listing = _dataService.MarketListings.FirstOrDefault(m => m.UUID == uuid);
        if (listing is null)
        {
            _logger.LogWarning("Update failed: market listing {UUID} not found", uuid);
            return;
        }

        listing.ItemName = request.ItemName;
        listing.ItemType = request.ItemType;
        listing.ItemReferenceID = request.ItemReferenceID;
        listing.StationUUID = request.StationUUID;
        listing.Quantity = request.Quantity;
        listing.PricePerUnit = request.PricePerUnit;
        listing.CurrentHP = request.CurrentHP;
        listing.MaxHP = request.MaxHP;
        listing.MaxRepairPercent = request.MaxRepairPercent;
        _dataService.IsDirty = true;
        _dataService.OnMarketDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Updated market listing {UUID}", uuid);
    }

    public void RecordSale(string listingUuid, int quantity, decimal pricePerUnit, string counterparty)
    {
        var transaction = new MarketTransaction
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            TransactionType = TransactionType.Sell,
            ListingUUID = listingUuid,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            TotalPrice = quantity * pricePerUnit,
            Counterparty = counterparty,
            Timestamp = SystemClock.UtcNow.ToString("o"),
        };

        var listing = _dataService.MarketListings.FirstOrDefault(m => m.UUID == listingUuid);
        if (listing is not null)
        {
            transaction.ItemName = listing.ItemName;
            transaction.ItemType = listing.ItemType;
            transaction.ItemReferenceID = listing.ItemReferenceID;
            transaction.StationUUID = listing.StationUUID;
        }

        _dataService.AddMarketTransaction(transaction);
        _dataService.OnMarketDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Recorded sale {UUID} qty={Qty}", transaction.UUID, quantity);
    }

    public void RecordPurchase(string itemName, ItemType.ItemTypeEnum itemType, int quantity, decimal pricePerUnit, string counterparty, string stationUuid)
    {
        var transaction = new MarketTransaction
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            TransactionType = TransactionType.Buy,
            ItemName = itemName,
            ItemType = itemType,
            Quantity = quantity,
            PricePerUnit = pricePerUnit,
            TotalPrice = quantity * pricePerUnit,
            Counterparty = counterparty,
            StationUUID = stationUuid,
            Timestamp = SystemClock.UtcNow.ToString("o"),
        };

        _dataService.AddMarketTransaction(transaction);
        _dataService.OnMarketDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Recorded purchase {UUID} qty={Qty}", transaction.UUID, quantity);
    }

    public void DeleteListing(string uuid)
    {
        _dataService.RemoveMarketListing(uuid);
        _dataService.OnMarketDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Deleted market listing {UUID}", uuid);
    }
}
