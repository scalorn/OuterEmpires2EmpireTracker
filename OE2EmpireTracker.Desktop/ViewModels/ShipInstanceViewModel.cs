using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the ship instance DataGrid.
/// </summary>
public sealed partial class ShipInstanceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _shipName = string.Empty;

    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private string _shipUuid = string.Empty;
}

/// <summary>
/// Row item for the ship component DataGrid.
/// </summary>
public sealed partial class ShipComponentRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _slotType = string.Empty;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private string _blueprintName = string.Empty;
}

/// <summary>
/// Row item for the cargo items DataGrid.
/// </summary>
public sealed partial class CargoItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private decimal _volume;

    [ObservableProperty]
    private string _itemUuid = string.Empty;
}

/// <summary>
/// Row item for the hopper items DataGrid.
/// </summary>
public sealed partial class HopperItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _purity = string.Empty;

    [ObservableProperty]
    private int _quantity;
}

/// <summary>
/// ViewModel for the Ship Instance document tab.
/// Shows ships with components, cargo, and hopper.
/// </summary>
public sealed partial class ShipInstanceViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ShipInstanceRowViewModel? _selectedShip;

    [ObservableProperty]
    private CargoItemRowViewModel? _selectedCargoItem;

    [ObservableProperty]
    private string _cargoVolumeDisplay = "0 / 0 m\u00B3";

    public ShipInstanceViewModel()
    {
        Title = "Ships";
        LoadData();
    }

    public ObservableCollection<ShipInstanceRowViewModel> Ships { get; } = new ObservableCollection<ShipInstanceRowViewModel>();

    public ObservableCollection<ShipComponentRowViewModel> Components { get; } = new ObservableCollection<ShipComponentRowViewModel>();

    public ObservableCollection<CargoItemRowViewModel> CargoItems { get; } = new ObservableCollection<CargoItemRowViewModel>();

    public ObservableCollection<HopperItemRowViewModel> HopperItems { get; } = new ObservableCollection<HopperItemRowViewModel>();

    [RelayCommand]
    public void AddCargoItem()
    {
        if (SelectedShip is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var shipService = App.Services?.GetService(typeof(ShipService)) as ShipService;
        if (dataService is null || shipService is null || !dataService.IsLoaded)
        {
            return;
        }

        var ship = dataService.Ships.FirstOrDefault(s => s.UUID == SelectedShip.ShipUuid);
        if (ship is null)
        {
            return;
        }

        var newItem = new Item(ItemType.ItemTypeEnum.Commodity, "New Item")
        {
            UUID = Guid.NewGuid().ToString(),
            Quantity = 1,
            Volume = 1m,
        };

        ship.Cargo.AddItem(newItem);
        dataService.IsDirty = true;
        dataService.WriteContext();
        dataService.OnShipDataChanged(ship.UUID);
        LoadShipDetails(SelectedShip);
    }

    [RelayCommand]
    public void RemoveCargoItem()
    {
        if (SelectedShip is null || SelectedCargoItem is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var ship = dataService.Ships.FirstOrDefault(s => s.UUID == SelectedShip.ShipUuid);
        if (ship is null)
        {
            return;
        }

        ship.Cargo.Remove(SelectedCargoItem.ItemUuid);
        dataService.IsDirty = true;
        dataService.WriteContext();
        dataService.OnShipDataChanged(ship.UUID);
        LoadShipDetails(SelectedShip);
    }

    [RelayCommand]
    public void CreateFromTemplate()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var shipService = App.Services?.GetService(typeof(ShipService)) as ShipService;
        if (dataService is null || shipService is null || !dataService.IsLoaded)
        {
            return;
        }

        var templates = dataService.ShipTemplates;
        if (templates.Count == 0)
        {
            return;
        }

        var template = templates[0];
        CreateShipFromTemplate(template, dataService);
        RefreshData();
    }

    protected override void RefreshData()
    {
        Ships.Clear();
        Components.Clear();
        CargoItems.Clear();
        HopperItems.Clear();
        LoadData();
    }

    private static void CreateShipFromTemplate(ShipTemplate template, DataService dataService)
    {
        var components = template.Components?
            .Select(c => new ShipComponentSlot
            {
                SlotType = c.SlotType,
                SlotIndex = c.SlotIndex,
                BlueprintUUID = c.BlueprintUUID,
                CurrentHP = c.CurrentHP,
                MaxHP = c.MaxHP,
                MaxRepairPercent = c.MaxRepairPercent,
            })
            .ToList() ?? new List<ShipComponentSlot>();

        var ship = new Ship
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = dataService.CurrentPlayerUUID,
            Name = $"{template.Name} (built)",
            TemplateUUID = template.UUID ?? string.Empty,
            HullBlueprintUUID = template.HullBlueprintUUID ?? string.Empty,
            Components = components,
        };

        dataService.AddShip(ship);
        dataService.OnShipDataChanged(ship.UUID);
        dataService.WriteContext();
    }

    partial void OnSelectedShipChanged(ShipInstanceRowViewModel? value)
    {
        LoadShipDetails(value);
    }

    private void LoadShipDetails(ShipInstanceRowViewModel? row)
    {
        Components.Clear();
        CargoItems.Clear();
        HopperItems.Clear();
        CargoVolumeDisplay = "0 / 0 m\u00B3";

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var ship = dataService.Ships.FirstOrDefault(s => s.UUID == row.ShipUuid);
        if (ship is null)
        {
            return;
        }

        foreach (var comp in ship.Components)
        {
            Components.Add(new ShipComponentRowViewModel
            {
                SlotType = comp.SlotType ?? string.Empty,
                SlotIndex = comp.SlotIndex,
                BlueprintName = comp.BlueprintUUID ?? string.Empty,
            });
        }

        decimal totalVolume = 0m;
        foreach (var kvp in ship.Cargo.Items)
        {
            var item = kvp.Value;
            CargoItems.Add(new CargoItemRowViewModel
            {
                ItemName = item.Name ?? string.Empty,
                Quantity = item.Quantity,
                Volume = item.Volume * item.Quantity,
                ItemUuid = item.UUID ?? string.Empty,
            });
            totalVolume += item.Volume * item.Quantity;
        }

        CargoVolumeDisplay = $"{totalVolume:N0} / ? m\u00B3";

        foreach (var kvp in ship.Hopper.Items)
        {
            var item = kvp.Value;
            HopperItems.Add(new HopperItemRowViewModel
            {
                ResourceName = item.Name ?? string.Empty,
                Purity = item.ResourcePurity ?? string.Empty,
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
