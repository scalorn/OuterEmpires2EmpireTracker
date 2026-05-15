using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the ship instance DataGrid.
/// </summary>
public sealed partial class ShipInstanceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _shipUuid = string.Empty;

    [ObservableProperty]
    private string _shipName = string.Empty;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;
}

/// <summary>
/// ViewModel for the Ship Instance document tab.
/// Shows ships with CRUD operations.
/// </summary>
public sealed partial class ShipInstanceViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ShipInstanceRowViewModel? _selectedShip;

    [ObservableProperty]
    private string _editShipName = string.Empty;

    public ShipInstanceViewModel()
    {
        Title = "Ships";

        WeakReferenceMessenger.Default.Register<ShipDataChangedMessage>(this, (r, m) =>
        {
            ((ShipInstanceViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<ShipInstanceRowViewModel> Ships { get; } = new ObservableCollection<ShipInstanceRowViewModel>();

    [RelayCommand]
    private void NewShip()
    {
        var svc = App.Services?.GetService(typeof(ShipService)) as ShipService;
        svc?.Create(new ShipCreateRequest { Name = "New Ship" });
    }

    [RelayCommand]
    private void SaveShip()
    {
        if (SelectedShip is null || string.IsNullOrEmpty(SelectedShip.ShipUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ShipService)) as ShipService;
        svc?.Update(SelectedShip.ShipUuid, new ShipUpdateRequest { Name = EditShipName });
    }

    [RelayCommand]
    private void DeleteShip()
    {
        if (SelectedShip is null || string.IsNullOrEmpty(SelectedShip.ShipUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(ShipService)) as ShipService;
        svc?.Delete(SelectedShip.ShipUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Ships.Clear();
        SelectedShip = null;
        EditShipName = string.Empty;
        LoadData();
    }

    partial void OnSelectedShipChanged(ShipInstanceRowViewModel? value)
    {
        EditShipName = value?.ShipName ?? string.Empty;
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var ship in dataService.Ships)
        {
            Ships.Add(new ShipInstanceRowViewModel
            {
                ShipUuid = ship.UUID ?? string.Empty,
                ShipName = ship.Name ?? string.Empty,
                TemplateName = ship.TemplateUUID ?? string.Empty,
                Location = ship.LocationUUID ?? string.Empty,
            });
        }

        if (Ships.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Ships.Add(new ShipInstanceRowViewModel
        {
            ShipName = "SS Enterprise",
            TemplateName = "Scout Mk I",
            Location = "Alpha Station",
        });
        Ships.Add(new ShipInstanceRowViewModel
        {
            ShipName = "MV Cargo King",
            TemplateName = "Hauler Mk II",
            Location = "Beta Outpost",
        });
    }
}
