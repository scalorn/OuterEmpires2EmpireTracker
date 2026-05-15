using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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
/// ViewModel for the Market document tab.
/// Shows market listings and transactions.
/// </summary>
public sealed partial class MarketViewModel : DocumentViewModel
{
    public MarketViewModel()
    {
        Title = "Market";
        LoadData();
    }

    public ObservableCollection<MarketListingRowViewModel> Listings { get; } = new ObservableCollection<MarketListingRowViewModel>();

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var listings = dataService.MarketListings;
        foreach (var listing in listings)
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

        if (Listings.Count == 0)
        {
            LoadSampleData();
        }
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
