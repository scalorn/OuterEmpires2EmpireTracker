using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the blueprint list.
/// </summary>
public sealed partial class BlueprintRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _blueprintType = string.Empty;

    [ObservableProperty]
    private string _techLevel = string.Empty;

    [ObservableProperty]
    private int _evolution;

    [ObservableProperty]
    private int _shipClass;
}

/// <summary>
/// Row item for the blueprint statistics DataGrid.
/// </summary>
public sealed partial class BlueprintStatRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statName = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}

/// <summary>
/// Row item for the blueprint resources DataGrid.
/// </summary>
public sealed partial class BlueprintResourceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _quantity = string.Empty;
}

/// <summary>
/// ViewModel for the Blueprint document tab.
/// Shows blueprint list with detail, statistics, resources, and evolution tabs.
/// </summary>
public sealed partial class BlueprintViewModel : DocumentViewModel
{
    [ObservableProperty]
    private BlueprintRowViewModel? _selectedBlueprint;

    [ObservableProperty]
    private string _detailName = string.Empty;

    [ObservableProperty]
    private string _detailType = string.Empty;

    [ObservableProperty]
    private string _detailTechLevel = string.Empty;

    [ObservableProperty]
    private int _detailShipClass;

    [ObservableProperty]
    private int _detailEvolution;

    [ObservableProperty]
    private BlueprintStatRowViewModel? _selectedStat;

    [ObservableProperty]
    private BlueprintResourceRowViewModel? _selectedResource;

    [ObservableProperty]
    private bool _isDirty;

    public BlueprintViewModel()
    {
        Title = "Blueprints";
        LoadData();
    }

    public ObservableCollection<BlueprintRowViewModel> Blueprints { get; } = new ();

    public ObservableCollection<BlueprintStatRowViewModel> Stats { get; } = new ();

    public ObservableCollection<BlueprintResourceRowViewModel> Resources { get; } = new ();

    partial void OnSelectedBlueprintChanged(BlueprintRowViewModel? value)
    {
        LoadBlueprintDetail(value);
        IsDirty = false;
    }

    /// <summary>
    /// Adds a new empty stat row to the Stats grid.
    /// </summary>
    [RelayCommand]
    private void AddStat()
    {
        Stats.Add(new BlueprintStatRowViewModel { StatName = "NewStat", Value = "0" });
        IsDirty = true;
    }

    /// <summary>
    /// Removes the selected stat row from the Stats grid.
    /// </summary>
    [RelayCommand]
    private void RemoveStat()
    {
        if (SelectedStat is not null)
        {
            Stats.Remove(SelectedStat);
            SelectedStat = null;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Adds a new empty resource row to the Resources grid.
    /// </summary>
    [RelayCommand]
    private void AddResource()
    {
        Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "New Resource", Quantity = "0" });
        IsDirty = true;
    }

    /// <summary>
    /// Removes the selected resource row from the Resources grid.
    /// </summary>
    [RelayCommand]
    private void RemoveResource()
    {
        if (SelectedResource is not null)
        {
            Resources.Remove(SelectedResource);
            SelectedResource = null;
            IsDirty = true;
        }
    }

    /// <summary>
    /// Saves stats and resources back to the blueprint model via the service layer.
    /// </summary>
    [RelayCommand]
    private void SaveBlueprint()
    {
        if (SelectedBlueprint is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var blueprintService = App.Services?.GetService(typeof(BlueprintService)) as BlueprintService;
        if (dataService is null || !dataService.IsLoaded || blueprintService is null)
        {
            return;
        }

        var bp = dataService.GetCurrentPlayerBlueprints()
            .FirstOrDefault(b => b.Name == SelectedBlueprint.Name && b.Evolution == SelectedBlueprint.Evolution);
        if (bp is null)
        {
            return;
        }

        // Build properties from the stats grid
        var properties = new Dictionary<string, string>();
        foreach (var row in Stats)
        {
            if (!string.IsNullOrWhiteSpace(row.StatName))
            {
                properties[row.StatName] = row.Value;
            }
        }

        // Build resources from the resources grid
        var resources = new Dictionary<string, string>();
        foreach (var row in Resources)
        {
            if (!string.IsNullOrWhiteSpace(row.ResourceName))
            {
                resources[row.ResourceName] = row.Quantity;
            }
        }

        var request = new BlueprintUpdateRequest
        {
            Name = bp.Name,
            NickName = bp.NickName,
            Description = bp.Description,
            BluePrintType = bp.BluePrintType,
            Evolution = bp.Evolution,
            TechLevel = bp.TechLevel,
            Class = bp.Class,
            CopyCost = bp.CopyCost,
            BaseBlueprintUUID = bp.BaseBlueprintUUID,
            Properties = properties,
            Resources = resources,
        };

        blueprintService.Update(bp.UUID, request);
        IsDirty = false;
    }

    /// <summary>
    /// Marks the blueprint as dirty when a cell is edited.
    /// </summary>
    [RelayCommand]
    private void MarkDirty()
    {
        IsDirty = true;
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var blueprints = dataService.GetCurrentPlayerBlueprints();
        if (blueprints.Count == 0)
        {
            LoadSampleData();
            return;
        }

        foreach (var bp in blueprints)
        {
            Blueprints.Add(new BlueprintRowViewModel
            {
                Name = bp.Name ?? string.Empty,
                BlueprintType = bp.BluePrintType ?? string.Empty,
                TechLevel = bp.TechLevel ?? string.Empty,
                Evolution = bp.Evolution,
                ShipClass = bp.Class,
            });
        }

        if (Blueprints.Count > 0)
        {
            SelectedBlueprint = Blueprints[0];
        }
    }

    private void LoadBlueprintDetail(BlueprintRowViewModel? row)
    {
        Stats.Clear();
        Resources.Clear();

        if (row is null)
        {
            DetailName = string.Empty;
            DetailType = string.Empty;
            DetailTechLevel = string.Empty;
            DetailShipClass = 0;
            DetailEvolution = 0;
            return;
        }

        DetailName = row.Name;
        DetailType = row.BlueprintType;
        DetailTechLevel = row.TechLevel;
        DetailShipClass = row.ShipClass;
        DetailEvolution = row.Evolution;

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStats(row.Name);
            LoadSampleResources(row.Name);
            return;
        }

        var bp = dataService.GetCurrentPlayerBlueprints()
            .FirstOrDefault(b => b.Name == row.Name && b.Evolution == row.Evolution);
        if (bp is null)
        {
            LoadSampleStats(row.Name);
            LoadSampleResources(row.Name);
            return;
        }

        foreach (var key in bp.Properties.Properties.Keys)
        {
            if (key.StartsWith("_"))
            {
                continue;
            }

            bp.Properties.GetString(key, string.Empty, out string val);
            Stats.Add(new BlueprintStatRowViewModel { StatName = key, Value = val });
        }

        foreach (var kvp in bp.Resources)
        {
            Resources.Add(new BlueprintResourceRowViewModel
            {
                ResourceName = kvp.Key,
                Quantity = kvp.Value,
            });
        }

        if (Stats.Count == 0)
        {
            LoadSampleStats(row.Name);
        }

        if (Resources.Count == 0)
        {
            LoadSampleResources(row.Name);
        }
    }

    private void LoadSampleData()
    {
        Blueprints.Add(new BlueprintRowViewModel { Name = "Laser Mk1", BlueprintType = "Weapon", TechLevel = "T1", Evolution = 3, ShipClass = 1 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Shield Generator", BlueprintType = "Component", TechLevel = "T2", Evolution = 1, ShipClass = 2 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Mining Rig Alpha", BlueprintType = "MiningRig", TechLevel = "T1", Evolution = 5, ShipClass = 0 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Cargo Pod XL", BlueprintType = "Component", TechLevel = "T3", Evolution = 2, ShipClass = 3 });
        Blueprints.Add(new BlueprintRowViewModel { Name = "Refinery Standard", BlueprintType = "Refinery", TechLevel = "T1", Evolution = 4, ShipClass = 0 });

        if (Blueprints.Count > 0)
        {
            SelectedBlueprint = Blueprints[0];
        }
    }

    private void LoadSampleStats(string name)
    {
        if (name == "Laser Mk1")
        {
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Damage", Value = "45" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Range", Value = "120" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Power Draw", Value = "15" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Mass", Value = "8" });
        }
        else
        {
            Stats.Add(new BlueprintStatRowViewModel { StatName = "HP", Value = "200" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Mass", Value = "12" });
            Stats.Add(new BlueprintStatRowViewModel { StatName = "Volume", Value = "5" });
        }
    }

    private void LoadSampleResources(string name)
    {
        if (name == "Laser Mk1")
        {
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Iron (Refined)", Quantity = "50" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Copper (Refined)", Quantity = "30" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Crystal (Refined)", Quantity = "10" });
        }
        else
        {
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Iron (Refined)", Quantity = "80" });
            Resources.Add(new BlueprintResourceRowViewModel { ResourceName = "Titanium (Refined)", Quantity = "40" });
        }
    }
}
