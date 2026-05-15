using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the star system DataGrid.
/// </summary>
public sealed partial class SystemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _systemName = string.Empty;

    [ObservableProperty]
    private int _asteroidCount;

    [ObservableProperty]
    private string _systemUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Star Systems document tab.
/// Shows a DataGrid of all star systems.
/// </summary>
public sealed partial class SystemListViewModel : DocumentViewModel
{
    public SystemListViewModel()
    {
        Title = "Systems";
        LoadData();
    }

    public ObservableCollection<SystemRowViewModel> Systems { get; } = new ObservableCollection<SystemRowViewModel>();

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        // Group asteroids by system name to build system list
        var asteroids = dataService.Asteroids;
        var systemGroups = asteroids
            .Where(a => !string.IsNullOrEmpty(a.SystemName))
            .GroupBy(a => a.SystemName)
            .OrderBy(g => g.Key);

        foreach (var group in systemGroups)
        {
            Systems.Add(new SystemRowViewModel
            {
                SystemName = group.Key ?? string.Empty,
                AsteroidCount = group.Count(),
            });
        }

        if (Systems.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Systems.Add(new SystemRowViewModel { SystemName = "Sol", AsteroidCount = 12 });
        Systems.Add(new SystemRowViewModel { SystemName = "Alpha Centauri", AsteroidCount = 5 });
        Systems.Add(new SystemRowViewModel { SystemName = "Proxima Centauri", AsteroidCount = 3 });
        Systems.Add(new SystemRowViewModel { SystemName = "Kepler-442", AsteroidCount = 8 });
        Systems.Add(new SystemRowViewModel { SystemName = "TRAPPIST-1", AsteroidCount = 7 });
    }
}
