using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the asteroid DataGrid.
/// </summary>
public sealed partial class AsteroidRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _asteroidName = string.Empty;

    [ObservableProperty]
    private string _systemName = string.Empty;

    [ObservableProperty]
    private int _reserveCount;
}

/// <summary>
/// ViewModel for the Asteroid document tab.
/// Shows asteroid list with reserve counts.
/// </summary>
public sealed partial class AsteroidViewModel : DocumentViewModel
{
    public AsteroidViewModel()
    {
        Title = "Asteroids";
        LoadData();
    }

    public ObservableCollection<AsteroidRowViewModel> Asteroids { get; } = new ObservableCollection<AsteroidRowViewModel>();

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var asteroid in dataService.Asteroids)
        {
            Asteroids.Add(new AsteroidRowViewModel
            {
                AsteroidName = asteroid.Name ?? string.Empty,
                SystemName = asteroid.SystemName ?? string.Empty,
                ReserveCount = asteroid.Reserves?.Count ?? 0,
            });
        }

        if (Asteroids.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Asteroids.Add(new AsteroidRowViewModel
        {
            AsteroidName = "Alpha Belt A-7",
            SystemName = "Sol",
            ReserveCount = 3,
        });
        Asteroids.Add(new AsteroidRowViewModel
        {
            AsteroidName = "Gamma Cluster B-12",
            SystemName = "Proxima",
            ReserveCount = 5,
        });
    }
}
