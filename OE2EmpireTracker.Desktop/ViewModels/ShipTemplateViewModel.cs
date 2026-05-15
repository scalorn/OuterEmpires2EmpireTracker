using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the ship template DataGrid.
/// </summary>
public sealed partial class ShipTemplateRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _templateName = string.Empty;

    [ObservableProperty]
    private string _hullClass = string.Empty;

    [ObservableProperty]
    private int _slotCount;
}

/// <summary>
/// ViewModel for the Ship Template document tab.
/// Shows ship templates with slot counts.
/// </summary>
public sealed partial class ShipTemplateViewModel : DocumentViewModel
{
    public ShipTemplateViewModel()
    {
        Title = "Ship Templates";
        LoadData();
    }

    public ObservableCollection<ShipTemplateRowViewModel> Templates { get; } = new ObservableCollection<ShipTemplateRowViewModel>();

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var template in dataService.ShipTemplates)
        {
            Templates.Add(new ShipTemplateRowViewModel
            {
                TemplateName = template.Name ?? string.Empty,
                HullClass = template.HullBlueprintUUID ?? string.Empty,
                SlotCount = template.Components?.Count ?? 0,
            });
        }

        if (Templates.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Templates.Add(new ShipTemplateRowViewModel
        {
            TemplateName = "Scout Mk I",
            HullClass = "Light Fighter",
            SlotCount = 4,
        });
        Templates.Add(new ShipTemplateRowViewModel
        {
            TemplateName = "Hauler Mk II",
            HullClass = "Freighter",
            SlotCount = 8,
        });
    }
}
