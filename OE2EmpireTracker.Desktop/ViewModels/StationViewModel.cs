using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
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

    /// <summary>Gets or sets the UUID of the station.</summary>
    public string StationUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the station components DataGrid.
/// </summary>
public sealed partial class StationComponentRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _slotType = string.Empty;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private string _blueprintName = string.Empty;
}

/// <summary>
/// Row item for the station munitions DataGrid.
/// </summary>
public sealed partial class StationMunitionRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;
}

/// <summary>
/// ViewModel for the Station document tab.
/// Shows stations with hold, components, and munitions tabs.
/// </summary>
public sealed partial class StationViewModel : DocumentViewModel
{
    [ObservableProperty]
    private StationRowViewModel? _selectedStation;

    public StationViewModel()
    {
        Title = "Stations";
        LoadData();
    }

    public ObservableCollection<StationRowViewModel> Stations { get; } = new ObservableCollection<StationRowViewModel>();

    public ObservableCollection<StationComponentRowViewModel> Components { get; } = new ObservableCollection<StationComponentRowViewModel>();

    public ObservableCollection<StationMunitionRowViewModel> MunitionsItems { get; } = new ObservableCollection<StationMunitionRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Stations.Clear();
        Components.Clear();
        MunitionsItems.Clear();
        SelectedStation = null;
        LoadData();
    }

    partial void OnSelectedStationChanged(StationRowViewModel? value)
    {
        LoadStationDetail(value);
    }

    private void LoadStationDetail(StationRowViewModel? row)
    {
        Components.Clear();
        MunitionsItems.Clear();

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
            .FirstOrDefault(s => s.UUID == row.StationUuid);
        if (station is null)
        {
            return;
        }

        LoadComponents(dataService, station);
        LoadMunitions(station);
    }

    private void LoadComponents(DataService dataService, OE2EmpireTracker.Models.Station station)
    {
        if (station.Components is null)
        {
            return;
        }

        foreach (var slot in station.Components)
        {
            string bpName = string.Empty;
            if (!string.IsNullOrEmpty(slot.BlueprintUUID))
            {
                var bp = dataService.Blueprints
                    .FirstOrDefault(b => b.UUID == slot.BlueprintUUID);
                bpName = bp?.Name ?? slot.BlueprintUUID;
            }

            Components.Add(new StationComponentRowViewModel
            {
                SlotType = slot.SlotType,
                SlotIndex = slot.SlotIndex,
                BlueprintName = bpName,
            });
        }
    }

    private void LoadMunitions(OE2EmpireTracker.Models.Station station)
    {
        if (station.MunitionsHold?.Items is null)
        {
            return;
        }

        foreach (var kvp in station.MunitionsHold.Items)
        {
            var item = kvp.Value;
            MunitionsItems.Add(new StationMunitionRowViewModel
            {
                ItemName = item.Name ?? string.Empty,
                Quantity = item.Quantity,
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
                StationUuid = station.UUID ?? string.Empty,
                StationName = station.Name ?? string.Empty,
                StationType = station.StationType.ToString(),
                HoldItemCount = holdCount,
            });
        }

        if (Stations.Count == 0)
        {
            LoadSampleData();
        }

        if (Stations.Count > 0)
        {
            SelectedStation = Stations[0];
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
