using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the market listings DataGrid.
/// </summary>
public sealed partial class MarketListingRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _stationName = string.Empty;

    [ObservableProperty]
    private string _listingUuid = string.Empty;
}

/// <summary>
/// Row item for the market transactions DataGrid.
/// </summary>
public sealed partial class MarketTransactionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _transactionType = string.Empty;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _pricePerUnit;

    [ObservableProperty]
    private decimal _totalPrice;

    [ObservableProperty]
    private string _counterparty = string.Empty;

    [ObservableProperty]
    private string _timestamp = string.Empty;
}

/// <summary>
/// ViewModel for the Market document tab.
/// Shows market listings, transactions, and summary.
/// </summary>
public sealed partial class MarketViewModel : DocumentViewModel
{
    [ObservableProperty]
    private MarketListingRowViewModel? _selectedListing;

    [ObservableProperty]
    private decimal _totalSales;

    [ObservableProperty]
    private decimal _totalPurchases;

    [ObservableProperty]
    private decimal _netProfitLoss;

    public MarketViewModel()
    {
        Title = "Market";
        LoadData();
    }

    public ObservableCollection<MarketListingRowViewModel> Listings { get; } = new ObservableCollection<MarketListingRowViewModel>();

    public ObservableCollection<MarketTransactionRowViewModel> MarketTransactions { get; } = new ObservableCollection<MarketTransactionRowViewModel>();

    [RelayCommand]
    public void RecordSale()
    {
        if (SelectedListing is null)
        {
            return;
        }

        var marketService = App.Services?.GetService(typeof(MarketService)) as MarketService;
        if (marketService is null)
        {
            return;
        }

        marketService.RecordSale(
            SelectedListing.ListingUuid,
            SelectedListing.Quantity,
            SelectedListing.Price,
            "Player");

        RefreshData();
    }

    [RelayCommand]
    public void RecordPurchase()
    {
        if (SelectedListing is null)
        {
            return;
        }

        var marketService = App.Services?.GetService(typeof(MarketService)) as MarketService;
        if (marketService is null)
        {
            return;
        }

        marketService.RecordPurchase(
            SelectedListing.ItemName,
            ItemType.ItemTypeEnum.None,
            SelectedListing.Quantity,
            SelectedListing.Price,
            "Player",
            string.Empty);

        RefreshData();
    }

    protected override void RefreshData()
    {
        Listings.Clear();
        MarketTransactions.Clear();
        LoadData();
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var listing in dataService.MarketListings)
        {
            Listings.Add(new MarketListingRowViewModel
            {
                ListingUuid = listing.UUID ?? string.Empty,
                ItemName = listing.ItemName ?? string.Empty,
                Quantity = listing.Quantity,
                Price = listing.PricePerUnit,
                StationName = listing.StationUUID ?? string.Empty,
            });
        }

        foreach (var tx in dataService.MarketTransactions)
        {
            MarketTransactions.Add(new MarketTransactionRowViewModel
            {
                TransactionType = tx.TransactionType.ToString(),
                ItemName = tx.ItemName ?? string.Empty,
                Quantity = tx.Quantity,
                PricePerUnit = tx.PricePerUnit,
                TotalPrice = tx.TotalPrice,
                Counterparty = tx.Counterparty ?? string.Empty,
                Timestamp = tx.Timestamp ?? string.Empty,
            });
        }

        ComputeSummary(dataService);

        if (Listings.Count == 0 && MarketTransactions.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void ComputeSummary(DataService dataService)
    {
        var transactions = dataService.MarketTransactions;
        TotalSales = transactions
            .Where(t => t.TransactionType == TransactionType.Sell)
            .Sum(t => t.TotalPrice);
        TotalPurchases = transactions
            .Where(t => t.TransactionType == TransactionType.Buy)
            .Sum(t => t.TotalPrice);
        NetProfitLoss = TotalSales - TotalPurchases;
    }

    private void LoadSampleData()
    {
        Listings.Add(new MarketListingRowViewModel
        {
            ItemName = "Iron Ore (High)",
            Quantity = 500,
            Price = 12.50m,
            StationName = "Alpha Station",
        });
        Listings.Add(new MarketListingRowViewModel
        {
            ItemName = "Titanium Ingot",
            Quantity = 100,
            Price = 45.00m,
            StationName = "Beta Outpost",
        });
        Listings.Add(new MarketListingRowViewModel
        {
            ItemName = "Circuit Board",
            Quantity = 50,
            Price = 120.00m,
            StationName = "Alpha Station",
        });
    }
}
