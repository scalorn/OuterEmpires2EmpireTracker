using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the asteroid DataGrid.
/// </summary>
public sealed partial class AsteroidRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _asteroidUuid = string.Empty;

    [ObservableProperty]
    private string _asteroidName = string.Empty;

    [ObservableProperty]
    private string _systemName = string.Empty;

    [ObservableProperty]
    private int _reserveCount;
}

/// <summary>
/// ViewModel for the Asteroid document tab.
/// Shows asteroid list with CRUD operations.
/// </summary>
public sealed partial class AsteroidViewModel : DocumentViewModel
{
    [ObservableProperty]
    private AsteroidRowViewModel? _selectedAsteroid;

    [ObservableProperty]
    private string _editAsteroidName = string.Empty;

    [ObservableProperty]
    private string _editSystemName = string.Empty;

    public AsteroidViewModel()
    {
        Title = "Asteroids";

        WeakReferenceMessenger.Default.Register<AsteroidDataChangedMessage>(this, (r, m) =>
        {
            ((AsteroidViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<AsteroidRowViewModel> Asteroids { get; } = new ObservableCollection<AsteroidRowViewModel>();

    [RelayCommand]
    private void NewAsteroid()
    {
        var svc = App.Services?.GetService(typeof(AsteroidService)) as AsteroidService;
        svc?.Create(new AsteroidCreateRequest { Name = "New Asteroid", SystemName = "Unknown" });
    }

    [RelayCommand]
    private void SaveAsteroid()
    {
        if (SelectedAsteroid is null || string.IsNullOrEmpty(SelectedAsteroid.AsteroidUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(AsteroidService)) as AsteroidService;
        svc?.Update(SelectedAsteroid.AsteroidUuid, new AsteroidUpdateRequest
        {
            Name = EditAsteroidName,
            SystemName = EditSystemName,
        });
    }

    [RelayCommand]
    private void DeleteAsteroid()
    {
        if (SelectedAsteroid is null || string.IsNullOrEmpty(SelectedAsteroid.AsteroidUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(AsteroidService)) as AsteroidService;
        svc?.Delete(SelectedAsteroid.AsteroidUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Asteroids.Clear();
        SelectedAsteroid = null;
        EditAsteroidName = string.Empty;
        EditSystemName = string.Empty;
        LoadData();
    }

    partial void OnSelectedAsteroidChanged(AsteroidRowViewModel? value)
    {
        EditAsteroidName = value?.AsteroidName ?? string.Empty;
        EditSystemName = value?.SystemName ?? string.Empty;
    }

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
                AsteroidUuid = asteroid.UUID ?? string.Empty,
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
