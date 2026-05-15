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
}

/// <summary>
/// ViewModel for the Station document tab.
/// Shows stations with hold item counts.
/// </summary>
public sealed partial class StationViewModel : DocumentViewModel
{
    public StationViewModel()
    {
        Title = "Stations";
        LoadData();
    }

    public ObservableCollection<StationRowViewModel> Stations { get; } = new ObservableCollection<StationRowViewModel>();

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
