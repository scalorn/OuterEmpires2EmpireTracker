using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

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

    /// <summary>Gets or sets the UUID of the underlying ShipTemplate.</summary>
    [ObservableProperty]
    private string _uuid = string.Empty;
}

/// <summary>
/// Row item for the ship template slot DataGrid.
/// </summary>
public sealed partial class ShipSlotRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _slotType = string.Empty;

    [ObservableProperty]
    private int _slotIndex;

    [ObservableProperty]
    private string _blueprintName = string.Empty;

    [ObservableProperty]
    private string _blueprintUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Ship Template document tab.
/// Shows ship templates with slot management.
/// </summary>
public sealed partial class ShipTemplateViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ShipTemplateRowViewModel? _selectedTemplate;

    [ObservableProperty]
    private ShipSlotRowViewModel? _selectedSlot;

    public ShipTemplateViewModel()
    {
        Title = "Ship Templates";
        LoadData();
    }

    public ObservableCollection<ShipTemplateRowViewModel> Templates { get; } = new();

    public ObservableCollection<ShipSlotRowViewModel> Slots { get; } = new();

    [RelayCommand]
    private void AddSlot()
    {
        var newSlot = new ShipSlotRowViewModel
        {
            SlotType = "Weapon",
            SlotIndex = Slots.Count,
            BlueprintName = string.Empty,
            BlueprintUuid = string.Empty,
        };
        Slots.Add(newSlot);
        SelectedSlot = newSlot;
    }

    [RelayCommand]
    private void RemoveSlot()
    {
        if (SelectedSlot is null)
        {
            return;
        }

        Slots.Remove(SelectedSlot);
        SelectedSlot = Slots.LastOrDefault();
    }

    [RelayCommand]
    private void SaveSlots()
    {
        if (SelectedTemplate is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var template = dataService.ShipTemplates
            .FirstOrDefault(t => t.UUID == SelectedTemplate.Uuid);
        if (template is null)
        {
            return;
        }

        template.Components.Clear();
        foreach (var slot in Slots)
        {
            template.Components.Add(new ShipComponentSlot
            {
                SlotType = slot.SlotType,
                SlotIndex = slot.SlotIndex,
                BlueprintUUID = slot.BlueprintUuid,
            });
        }

        dataService.IsDirty = true;
        dataService.WriteContext();
        SelectedTemplate.SlotCount = Slots.Count;
    }

    partial void OnSelectedTemplateChanged(ShipTemplateRowViewModel? value)
    {
        LoadSlots(value);
    }

    private void LoadSlots(ShipTemplateRowViewModel? row)
    {
        Slots.Clear();
        SelectedSlot = null;

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var template = dataService.ShipTemplates
            .FirstOrDefault(t => t.UUID == row.Uuid);
        if (template is null)
        {
            return;
        }

        foreach (var slot in template.Components)
        {
            var bpName = ResolveBlueprintName(dataService, slot.BlueprintUUID);
            Slots.Add(new ShipSlotRowViewModel
            {
                SlotType = slot.SlotType,
                SlotIndex = slot.SlotIndex,
                BlueprintName = bpName,
                BlueprintUuid = slot.BlueprintUUID,
            });
        }
    }

    private static string ResolveBlueprintName(DataService dataService, string uuid)
    {
        if (string.IsNullOrEmpty(uuid))
        {
            return string.Empty;
        }

        var bp = dataService.Blueprints.FirstOrDefault(b => b.UUID == uuid);
        return bp?.Name ?? "(unknown)";
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
                Uuid = template.UUID ?? string.Empty,
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
