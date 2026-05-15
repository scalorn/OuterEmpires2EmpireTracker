using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the structures DataGrid.
/// </summary>
public sealed partial class ColonyStructureRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _structureName = string.Empty;

    [ObservableProperty]
    private string _structureType = string.Empty;

    [ObservableProperty]
    private string _state = string.Empty;

    [ObservableProperty]
    private int _buildingId;
}

/// <summary>
/// Row item for the commodities DataGrid.
/// </summary>
public sealed partial class ColonyCommodityRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _commodityName = string.Empty;

    [ObservableProperty]
    private int _requested;

    [ObservableProperty]
    private int _delivered;

    [ObservableProperty]
    private bool _fulfilled;
}

/// <summary>
/// Row item for the colony items DataGrid.
/// </summary>
public sealed partial class ColonyItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _itemType = string.Empty;
}

/// <summary>
/// Row item for the overflow rules DataGrid.
/// </summary>
public sealed partial class ColonyOverflowRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _purity = string.Empty;

    [ObservableProperty]
    private int _threshold;

    [ObservableProperty]
    private string _destination = string.Empty;
}

/// <summary>
/// ViewModel for the Colony document tab.
/// Shows colony list with structures, commodities, items, overflow, and activity tabs.
/// </summary>
public sealed partial class ColonyViewModel : DocumentViewModel
{
    [ObservableProperty]
    private ColonyRowViewModel? _selectedColony;

    [ObservableProperty]
    private string _activityStatus = "No active timers";

    public ColonyViewModel()
    {
        Title = "Colonies";
        LoadData();
    }

    public ObservableCollection<ColonyRowViewModel> Colonies { get; } = new ();

    public ObservableCollection<ColonyStructureRowViewModel> Structures { get; } = new ();

    public ObservableCollection<ColonyCommodityRowViewModel> Commodities { get; } = new ();

    public ObservableCollection<ColonyItemRowViewModel> Items { get; } = new ();

    public ObservableCollection<ColonyOverflowRowViewModel> OverflowRules { get; } = new ();

    private static string GetStructureState(ColonyStructure structure)
    {
        structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out bool built);
        if (!built)
        {
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out bool staged);
            return staged ? "Staged" : "Unbuilt";
        }

        structure.Properties.GetBoolean(GameConstants.PropOnline, false, out bool online);
        return online ? "Online" : "Offline";
    }

    private static List<WarehouseOverflowRule> GetOverflowRulesForColony(
        DataService dataService,
        string colonyUuid)
    {
        // WarehouseOverflowRule is not directly exposed on DataService,
        // so we return empty for now. Real data would come from PlayerRoot.
        _ = dataService;
        _ = colonyUuid;
        return new List<WarehouseOverflowRule>();
    }

    private static void UpdateActivityStatus(Colony colony)
    {
        // Activity status is informational only
        _ = colony;
    }

    partial void OnSelectedColonyChanged(ColonyRowViewModel? value)
    {
        LoadColonyDetail(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var colonies = dataService.GetCurrentPlayerColonies();
        if (colonies.Count == 0)
        {
            LoadSampleData();
            return;
        }

        foreach (var colony in colonies)
        {
            Colonies.Add(new ColonyRowViewModel
            {
                ColonyName = colony.ColonyName ?? string.Empty,
                PlanetName = colony.PlanetName ?? string.Empty,
                SystemName = colony.SystemName ?? string.Empty,
                StructureCount = colony.Structures?.Count ?? 0,
            });
        }

        if (Colonies.Count > 0)
        {
            SelectedColony = Colonies[0];
        }
    }

    private void LoadColonyDetail(ColonyRowViewModel? row)
    {
        Structures.Clear();
        Commodities.Clear();
        Items.Clear();
        OverflowRules.Clear();
        ActivityStatus = "No active timers";

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStructures(row.ColonyName);
            LoadSampleCommodities(row.ColonyName);
            LoadSampleItems(row.ColonyName);
            LoadSampleOverflow(row.ColonyName);
            return;
        }

        var colony = dataService.GetCurrentPlayerColonies()
            .FirstOrDefault(c => c.ColonyName == row.ColonyName);
        if (colony is null)
        {
            LoadSampleStructures(row.ColonyName);
            LoadSampleCommodities(row.ColonyName);
            LoadSampleItems(row.ColonyName);
            LoadSampleOverflow(row.ColonyName);
            return;
        }

        LoadStructuresFromColony(dataService, colony);
        LoadCommoditiesFromColony(colony);
        LoadItemsFromColony(colony);
        LoadOverflowFromColony(dataService, colony);
        UpdateActivityStatus(colony);
    }

    private void LoadStructuresFromColony(DataService dataService, Colony colony)
    {
        if (colony.Structures is null)
        {
            return;
        }

        foreach (var structure in colony.Structures)
        {
            string typeName = string.Empty;
            if (!string.IsNullOrEmpty(structure.FlatpackBlueprintUUID))
            {
                var bp = dataService.Blueprints
                    .FirstOrDefault(b => b.UUID == structure.FlatpackBlueprintUUID);
                typeName = bp?.BluePrintType ?? string.Empty;
            }

            string state = GetStructureState(structure);

            Structures.Add(new ColonyStructureRowViewModel
            {
                StructureName = typeName,
                StructureType = typeName,
                State = state,
                BuildingId = structure.BuildingID,
            });
        }

        if (Structures.Count == 0)
        {
            LoadSampleStructures(colony.ColonyName ?? string.Empty);
        }
    }

    private void LoadCommoditiesFromColony(Colony colony)
    {
        if (colony.Commodities is null)
        {
            return;
        }

        foreach (var commodity in colony.Commodities)
        {
            Commodities.Add(new ColonyCommodityRowViewModel
            {
                CommodityName = commodity.Name ?? string.Empty,
                Requested = commodity.Requested,
                Delivered = commodity.Delivered,
                Fulfilled = commodity.Fulfilled,
            });
        }
    }

    private void LoadItemsFromColony(Colony colony)
    {
        if (colony.Items?.Items is null)
        {
            return;
        }

        foreach (var kvp in colony.Items.Items)
        {
            var item = kvp.Value;
            Items.Add(new ColonyItemRowViewModel
            {
                ItemName = item.Name ?? string.Empty,
                Quantity = item.Quantity,
                ItemType = item.ItemType.ToString(),
            });
        }
    }

    private void LoadOverflowFromColony(DataService dataService, Colony colony)
    {
        var rules = dataService.Colonies is not null
            ? GetOverflowRulesForColony(dataService, colony.UUID)
            : new List<WarehouseOverflowRule>();

        foreach (var rule in rules)
        {
            OverflowRules.Add(new ColonyOverflowRowViewModel
            {
                ResourceName = rule.ResourceName ?? string.Empty,
                Purity = rule.ResourcePurity ?? string.Empty,
                Threshold = rule.TriggerThreshold,
                Destination = rule.DestinationType.ToString(),
            });
        }
    }

    private void LoadSampleData()
    {
        Colonies.Add(new ColonyRowViewModel { ColonyName = "Alpha Prime", PlanetName = "Kepler-442b", SystemName = "Kepler", StructureCount = 6 });
        Colonies.Add(new ColonyRowViewModel { ColonyName = "Mining Outpost", PlanetName = "Proxima c", SystemName = "Proxima", StructureCount = 4 });
        Colonies.Add(new ColonyRowViewModel { ColonyName = "Trade Hub", PlanetName = "Ross 128b", SystemName = "Ross", StructureCount = 5 });

        if (Colonies.Count > 0)
        {
            SelectedColony = Colonies[0];
        }
    }

    private void LoadSampleStructures(string colonyName)
    {
        if (colonyName == "Alpha Prime")
        {
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Mining Rig Mk2", StructureType = "MiningRig", State = "Online", BuildingId = 1 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Mining Rig Mk2", StructureType = "MiningRig", State = "Online", BuildingId = 2 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Refinery Standard", StructureType = "Refinery", State = "Online", BuildingId = 3 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Manufactory", StructureType = "Manufactory", State = "Offline", BuildingId = 4 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Research Lab", StructureType = "ResearchLaboratory", State = "Online", BuildingId = 5 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Commodity Factory", StructureType = "CommodityFactory", State = "Staged", BuildingId = 6 });
        }
        else if (colonyName == "Mining Outpost")
        {
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Mining Rig Mk1", StructureType = "MiningRig", State = "Online", BuildingId = 1 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Mining Rig Mk1", StructureType = "MiningRig", State = "Online", BuildingId = 2 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Refinery Basic", StructureType = "Refinery", State = "Online", BuildingId = 3 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Storage Depot", StructureType = "Storage", State = "Online", BuildingId = 4 });
        }
        else
        {
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Manufactory Mk3", StructureType = "Manufactory", State = "Online", BuildingId = 1 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Commodity Factory", StructureType = "CommodityFactory", State = "Online", BuildingId = 2 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Research Lab", StructureType = "ResearchLaboratory", State = "Online", BuildingId = 3 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Refinery Advanced", StructureType = "Refinery", State = "Online", BuildingId = 4 });
            Structures.Add(new ColonyStructureRowViewModel { StructureName = "Mining Rig Mk3", StructureType = "MiningRig", State = "Staged", BuildingId = 5 });
        }
    }

    private void LoadSampleCommodities(string colonyName)
    {
        if (colonyName == "Alpha Prime")
        {
            Commodities.Add(new ColonyCommodityRowViewModel { CommodityName = "Electronics", Requested = 100, Delivered = 60, Fulfilled = false });
            Commodities.Add(new ColonyCommodityRowViewModel { CommodityName = "Fuel Cells", Requested = 50, Delivered = 50, Fulfilled = true });
            Commodities.Add(new ColonyCommodityRowViewModel { CommodityName = "Construction Materials", Requested = 200, Delivered = 120, Fulfilled = false });
        }
        else
        {
            Commodities.Add(new ColonyCommodityRowViewModel { CommodityName = "Mining Equipment", Requested = 30, Delivered = 30, Fulfilled = true });
            Commodities.Add(new ColonyCommodityRowViewModel { CommodityName = "Fuel Cells", Requested = 20, Delivered = 10, Fulfilled = false });
        }
    }

    private void LoadSampleItems(string colonyName)
    {
        if (colonyName == "Alpha Prime")
        {
            Items.Add(new ColonyItemRowViewModel { ItemName = "Iron (Refined)", Quantity = 450, ItemType = "Resource" });
            Items.Add(new ColonyItemRowViewModel { ItemName = "Copper (Refined)", Quantity = 230, ItemType = "Resource" });
            Items.Add(new ColonyItemRowViewModel { ItemName = "Crystal (Low)", Quantity = 80, ItemType = "Resource" });
            Items.Add(new ColonyItemRowViewModel { ItemName = "Laser Mk1", Quantity = 3, ItemType = "Blueprint" });
        }
        else
        {
            Items.Add(new ColonyItemRowViewModel { ItemName = "Iron (High)", Quantity = 1200, ItemType = "Resource" });
            Items.Add(new ColonyItemRowViewModel { ItemName = "Copper (Medium)", Quantity = 600, ItemType = "Resource" });
        }
    }

    private void LoadSampleOverflow(string colonyName)
    {
        if (colonyName == "Alpha Prime")
        {
            OverflowRules.Add(new ColonyOverflowRowViewModel { ResourceName = "Iron", Purity = "Refined", Threshold = 1000, Destination = "Station" });
            OverflowRules.Add(new ColonyOverflowRowViewModel { ResourceName = "Copper", Purity = "Refined", Threshold = 500, Destination = "Station" });
        }
        else
        {
            OverflowRules.Add(new ColonyOverflowRowViewModel { ResourceName = "Iron", Purity = "High", Threshold = 2000, Destination = "Colony" });
        }
    }
}
