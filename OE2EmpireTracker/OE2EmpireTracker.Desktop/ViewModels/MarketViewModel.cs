using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the market listings DataGrid.
/// </summary>
public sealed partial class MarketListingRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _listingUuid = string.Empty;

    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _stationName = string.Empty;
}

/// <summary>
/// ViewModel for the Market document tab.
/// Shows market listings with CRUD operations.
/// </summary>
public sealed partial class MarketViewModel : DocumentViewModel
{
    [ObservableProperty]
    private MarketListingRowViewModel? _selectedListing;

    [ObservableProperty]
    private string _editItemName = string.Empty;

    [ObservableProperty]
    private int _editQuantity;

    [ObservableProperty]
    private decimal _editPricePerUnit;

    public MarketViewModel()
    {
        Title = "Market";

        WeakReferenceMessenger.Default.Register<MarketDataChangedMessage>(this, (r, m) =>
        {
            ((MarketViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<MarketListingRowViewModel> Listings { get; } = new ObservableCollection<MarketListingRowViewModel>();

    [RelayCommand]
    private void NewListing()
    {
        var svc = App.Services?.GetService(typeof(MarketService)) as MarketService;
        svc?.CreateListing(new MarketListingCreateRequest
        {
            ItemName = "New Item",
            Quantity = 1,
            PricePerUnit = 0m,
        });
    }

    [RelayCommand]
    private void SaveListing()
    {
        if (SelectedListing is null || string.IsNullOrEmpty(SelectedListing.ListingUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(MarketService)) as MarketService;
        svc?.UpdateListing(SelectedListing.ListingUuid, new MarketListingUpdateRequest
        {
            ItemName = EditItemName,
            Quantity = EditQuantity,
            PricePerUnit = EditPricePerUnit,
        });
    }

    [RelayCommand]
    private void DeleteListing()
    {
        if (SelectedListing is null || string.IsNullOrEmpty(SelectedListing.ListingUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(MarketService)) as MarketService;
        svc?.DeleteListing(SelectedListing.ListingUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Listings.Clear();
        SelectedListing = null;
        EditItemName = string.Empty;
        EditQuantity = 0;
        EditPricePerUnit = 0m;
        LoadData();
    }

    partial void OnSelectedListingChanged(MarketListingRowViewModel? value)
    {
        EditItemName = value?.ItemName ?? string.Empty;
        EditQuantity = value?.Quantity ?? 0;
        EditPricePerUnit = value?.Price ?? 0m;
    }

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
