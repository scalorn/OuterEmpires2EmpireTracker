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
/// Row item for the station DataGrid.
/// </summary>
public sealed partial class StationRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _stationUuid = string.Empty;

    [ObservableProperty]
    private string _stationName = string.Empty;

    [ObservableProperty]
    private string _stationType = string.Empty;

    [ObservableProperty]
    private int _holdItemCount;
}

/// <summary>
/// ViewModel for the Station document tab.
/// Shows stations with CRUD operations.
/// </summary>
public sealed partial class StationViewModel : DocumentViewModel
{
    [ObservableProperty]
    private StationRowViewModel? _selectedStation;

    [ObservableProperty]
    private string _editStationName = string.Empty;

    public StationViewModel()
    {
        Title = "Stations";

        WeakReferenceMessenger.Default.Register<StationDataChangedMessage>(this, (r, m) =>
        {
            ((StationViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<StationRowViewModel> Stations { get; } = new ObservableCollection<StationRowViewModel>();

    [RelayCommand]
    private void NewStation()
    {
        var svc = App.Services?.GetService(typeof(StationService)) as StationService;
        svc?.Create(new StationCreateRequest { Name = "New Station" });
    }

    [RelayCommand]
    private void SaveStation()
    {
        if (SelectedStation is null || string.IsNullOrEmpty(SelectedStation.StationUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(StationService)) as StationService;
        svc?.Update(SelectedStation.StationUuid, new StationUpdateRequest { Name = EditStationName });
    }

    [RelayCommand]
    private void DeleteStation()
    {
        if (SelectedStation is null || string.IsNullOrEmpty(SelectedStation.StationUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(StationService)) as StationService;
        svc?.Delete(SelectedStation.StationUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Stations.Clear();
        SelectedStation = null;
        EditStationName = string.Empty;
        LoadData();
    }

    partial void OnSelectedStationChanged(StationRowViewModel? value)
    {
        EditStationName = value?.StationName ?? string.Empty;
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
