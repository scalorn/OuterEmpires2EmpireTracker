using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the station DataGrid.
/// </summary>
public sealed partial class StationRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _stationName = string.Empty;

    [ObservableProperty]
    private string _stationType = string.Empty;

    [ObservableProperty]
    private int _holdItemCount;

    /// <summary>Gets or sets the UUID of the underlying Station.</summary>
    [ObservableProperty]
    private string _uuid = string.Empty;
}

/// <summary>
/// Row item for the station hold items DataGrid.
/// </summary>
public sealed partial class HoldItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _itemType = string.Empty;

    [ObservableProperty]
    private string _itemUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Station document tab.
/// Shows stations with hold item management.
/// </summary>
public sealed partial class StationViewModel : DocumentViewModel
{
    [ObservableProperty]
    private StationRowViewModel? _selectedStation;

    [ObservableProperty]
    private HoldItemRowViewModel? _selectedHoldItem;

    public StationViewModel()
    {
        Title = "Stations";
        LoadData();
    }

    public ObservableCollection<StationRowViewModel> Stations { get; } = new();

    public ObservableCollection<HoldItemRowViewModel> HoldItems { get; } = new();

    [RelayCommand]
    private void AddHoldItem()
    {
        var newItem = new HoldItemRowViewModel
        {
            ItemName = "New Item",
            Quantity = 1,
            ItemType = "Resource",
            ItemUuid = string.Empty,
        };
        HoldItems.Add(newItem);
        SelectedHoldItem = newItem;
    }

    [RelayCommand]
    private void RemoveHoldItem()
    {
        if (SelectedHoldItem is null)
        {
            return;
        }

        HoldItems.Remove(SelectedHoldItem);
        SelectedHoldItem = HoldItems.LastOrDefault();
    }

    partial void OnSelectedStationChanged(StationRowViewModel? value)
    {
        LoadHoldItems(value);
    }

    private void LoadHoldItems(StationRowViewModel? row)
    {
        HoldItems.Clear();
        SelectedHoldItem = null;

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var station = dataService.Stations
            .FirstOrDefault(s => s.UUID == row.Uuid);
        if (station is null)
        {
            return;
        }

        var currentPlayerUuid = dataService.CurrentPlayerUUID;
        if (string.IsNullOrEmpty(currentPlayerUuid))
        {
            return;
        }

        if (!station.Holds.TryGetValue(currentPlayerUuid, out var hold))
        {
            return;
        }

        foreach (var item in hold.Items.Values)
        {
            HoldItems.Add(new HoldItemRowViewModel
            {
                ItemName = item.Name ?? string.Empty,
                Quantity = item.Quantity,
                ItemType = item.ItemType.ToString(),
                ItemUuid = item.UUID ?? string.Empty,
            });
        }
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var station in dataService.Stations)
        {
            var holdCount = station.Holds?.Values.Sum(h => h.Count()) ?? 0;
            Stations.Add(new StationRowViewModel
            {
                StationName = station.Name ?? string.Empty,
                StationType = station.StationType.ToString(),
                HoldItemCount = holdCount,
                Uuid = station.UUID ?? string.Empty,
            });
        }

        if (Stations.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Stations.Add(new StationRowViewModel
        {
            StationName = "Alpha Station",
            StationType = "Station",
            HoldItemCount = 42,
        });
        Stations.Add(new StationRowViewModel
        {
            StationName = "Beta Outpost",
            StationType = "Outpost",
            HoldItemCount = 15,
        });
    }
}
