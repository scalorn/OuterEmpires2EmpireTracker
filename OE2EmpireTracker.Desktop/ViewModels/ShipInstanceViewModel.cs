using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

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
}

/// <summary>
/// ViewModel for the Ship Instance document tab.
/// Shows ships with template and location info.
/// </summary>
public sealed partial class ShipInstanceViewModel : DocumentViewModel
{
    public ShipInstanceViewModel()
    {
        Title = "Ships";
        LoadData();
    }

    public ObservableCollection<ShipInstanceRowViewModel> Ships { get; } = new ObservableCollection<ShipInstanceRowViewModel>();

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
