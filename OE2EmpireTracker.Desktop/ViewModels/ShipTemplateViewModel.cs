using System.Collections.ObjectModel;
using System.Linq;
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
/// Shows ship templates with slot counts and stats.
/// </summary>
public sealed partial class ShipTemplateViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ShipTemplateRowViewModel? _selectedTemplate;

    [ObservableProperty]
    private int _totalSlots;

    [ObservableProperty]
    private int _filledSlots;

    [ObservableProperty]
    private string _statsDisplay = string.Empty;

    public ShipTemplateViewModel()
    {
        Title = "Ship Templates";
        LoadData();
    }

    public ObservableCollection<ShipTemplateRowViewModel> Templates { get; } = new ObservableCollection<ShipTemplateRowViewModel>();

    protected override void RefreshData()
    {
        Templates.Clear();
        LoadData();
    }

    partial void OnSelectedTemplateChanged(ShipTemplateRowViewModel? value)
    {
        ComputeStats(value);
    }

    private void ComputeStats(ShipTemplateRowViewModel? row)
    {
        if (row is null)
        {
            TotalSlots = 0;
            FilledSlots = 0;
            StatsDisplay = string.Empty;
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            TotalSlots = row.SlotCount;
            FilledSlots = row.SlotCount;
            StatsDisplay = $"Slots: {row.SlotCount} / {row.SlotCount}";
            return;
        }

        var template = dataService.ShipTemplates
            .FirstOrDefault(t => t.Name == row.TemplateName);
        if (template is null)
        {
            TotalSlots = row.SlotCount;
            FilledSlots = 0;
            StatsDisplay = $"Slots: 0 / {row.SlotCount}";
            return;
        }

        TotalSlots = template.Components?.Count ?? 0;
        FilledSlots = template.Components?
            .Count(c => !string.IsNullOrEmpty(c.BlueprintUUID)) ?? 0;
        StatsDisplay = $"Slots: {FilledSlots} / {TotalSlots}";
    }

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
