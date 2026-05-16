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
/// Row item for the build plan list.
/// </summary>
public sealed partial class BuildPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planUuid = string.Empty;

    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private string _colonyName = string.Empty;

    [ObservableProperty]
    private int _itemCount;
}

/// <summary>
/// Row item for the build items DataGrid.
/// </summary>
public sealed partial class BuildItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _itemType = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    /// <summary>Gets or sets the UUID of the underlying BuildItem.</summary>
    public string ItemUuid { get; set; } = string.Empty;

    /// <summary>Gets or sets the build location UUID for this item.</summary>
    public string BuildLocationUuid { get; set; } = string.Empty;

    /// <summary>Gets or sets the structure UUID for this item.</summary>
    public string StructureUuid { get; set; } = string.Empty;

    /// <summary>Gets the status color based on the current status value.</summary>
    public string StatusColor => Status switch
    {
        "Staged" => "#FFD700",
        "Delivering" => "#87CEEB",
        "Ready" => "#90EE90",
        "InProgress" => "#FFA500",
        "Completed" => "#808080",
        _ => "Transparent",
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusColor));
    }
}

/// <summary>
/// Row item for the shortfall DataGrid.
/// </summary>
public sealed partial class ShortfallRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private int _required;

    [ObservableProperty]
    private int _available;

    [ObservableProperty]
    private int _deficit;
}

/// <summary>
/// ViewModel for the Build Planner document tab.
/// Shows build plans with items and shortfall analysis.
/// </summary>
public sealed partial class BuildPlannerViewModel : DocumentViewModel
{
    [ObservableProperty]
    private BuildPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private BuildItemRowViewModel? _selectedBuildItem;

    public BuildPlannerViewModel()
    {
        Title = "Build Planner";
        LoadData();
    }

    public ObservableCollection<BuildPlanRowViewModel> BuildPlans { get; } = new ();

    public ObservableCollection<BuildItemRowViewModel> BuildItems { get; } = new ();

    public ObservableCollection<ShortfallRowViewModel> Shortfalls { get; } = new ();

    private static string ResolveColonyName(DataService dataService, BuildPlan plan)
    {
        if (plan.Items is null || plan.Items.Count == 0)
        {
            return string.Empty;
        }

        var firstItem = plan.Items[0];
        if (string.IsNullOrEmpty(firstItem.BuildLocationUUID))
        {
            return string.Empty;
        }

        var colony = dataService.Colonies
            .FirstOrDefault(c => c.UUID == firstItem.BuildLocationUUID);
        return colony?.ColonyName ?? string.Empty;
    }

    private static Colony? FindColonyWithStructureType(List<Colony> colonies, BuildItemType itemType)
    {
        string targetType = itemType switch
        {
            BuildItemType.Manufactory => "Manufactory",
            BuildItemType.Mining => "MiningRig",
            BuildItemType.Refining => "Refinery",
            BuildItemType.Research => "ResearchLaboratory",
            BuildItemType.Commodity => "CommodityFactory",
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(targetType))
        {
            return null;
        }

        foreach (var colony in colonies)
        {
            if (colony.Structures is null)
            {
                continue;
            }

            bool hasMatch = colony.Structures.Any(s => s.IsBuiltAndOnline
                && !string.IsNullOrEmpty(s.FlatpackBlueprintUUID));
            if (hasMatch)
            {
                return colony;
            }
        }

        return colonies.FirstOrDefault(c => c.Structures is not null && c.Structures.Count > 0);
    }

    /// <summary>
    /// Adds a new build item to the selected plan's items grid.
    /// </summary>
    [RelayCommand]
    private void AddBuildItem()
    {
        BuildItems.Add(new BuildItemRowViewModel
        {
            ItemName = "New Item",
            Quantity = 1,
            ItemType = "Manufactory",
            Status = "Staged",
        });
    }

    /// <summary>
    /// Removes the selected build item from the items grid.
    /// </summary>
    [RelayCommand]
    private void RemoveBuildItem()
    {
        if (SelectedBuildItem is not null)
        {
            BuildItems.Remove(SelectedBuildItem);
            SelectedBuildItem = null;
        }
    }

    /// <summary>
    /// Saves the build items back to the plan model and persists.
    /// </summary>
    [RelayCommand]
    private void SavePlan()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(BuildPlanService)) as BuildPlanService;
        if (svc is null)
        {
            return;
        }

        var items = BuildItems.Select(row => new BuildItem
        {
            UUID = Guid.NewGuid().ToString(),
            ItemName = row.ItemName,
            Quantity = row.Quantity,
            ItemType = Enum.TryParse<BuildItemType>(row.ItemType, out var t) ? t : BuildItemType.Manufactory,
            Status = Enum.TryParse<BuildItemStatus>(row.Status, out var s) ? s : BuildItemStatus.Staged,
        }).ToList();

        svc.Update(SelectedPlan.PlanUuid, new BuildPlanUpdateRequest
        {
            Name = SelectedPlan.PlanName,
            Items = items,
        });

        SelectedPlan.ItemCount = items.Count;
    }

    // --- K6: Auto-Assign (simplified) ---

    /// <summary>
    /// For each unallocated item, finds the first colony with a matching structure type
    /// and sets BuildLocationUUID to that colony's UUID.
    /// </summary>
    [RelayCommand]
    private void AutoAssign()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.BuildPlans.FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (plan?.Items is null)
        {
            return;
        }

        var colonies = dataService.GetCurrentPlayerColonies();
        bool changed = false;

        foreach (var item in plan.Items)
        {
            if (!string.IsNullOrEmpty(item.BuildLocationUUID))
            {
                continue;
            }

            var matchingColony = FindColonyWithStructureType(colonies, item.ItemType);
            if (matchingColony is not null)
            {
                item.BuildLocationUUID = matchingColony.UUID;
                changed = true;
            }
        }

        if (changed)
        {
            dataService.IsDirty = true;
            dataService.WriteContext();
            LoadPlanDetail(SelectedPlan);
        }
    }

    // --- K7: Generate Delivery Plan ---

    /// <summary>
    /// For each item with status "Staged" that has a location, creates a delivery plan.
    /// </summary>
    [RelayCommand]
    private void GenerateDelivery()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var deliveryService = App.Services?.GetService(typeof(DeliveryPlanService)) as DeliveryPlanService;
        if (dataService is null || !dataService.IsLoaded || deliveryService is null)
        {
            return;
        }

        var plan = dataService.BuildPlans.FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (plan?.Items is null)
        {
            return;
        }

        var stagedWithLocation = plan.Items
            .Where(i => i.Status == BuildItemStatus.Staged && !string.IsNullOrEmpty(i.BuildLocationUUID))
            .ToList();

        if (stagedWithLocation.Count == 0)
        {
            return;
        }

        string planName = $"Delivery for {plan.Name}";
        deliveryService.Create(planName, string.Empty, string.Empty);

        foreach (var item in stagedWithLocation)
        {
            item.Status = BuildItemStatus.Delivering;
        }

        dataService.IsDirty = true;
        dataService.WriteContext();
        LoadPlanDetail(SelectedPlan);
    }

    // --- K8: Start Manufacturing ---

    /// <summary>
    /// For the selected "Ready" item, finds the colony and structure,
    /// sets ManufacturingBlueprintUUID and quantity, and advances status to InProgress.
    /// </summary>
    [RelayCommand]
    private void StartManufacturing()
    {
        if (SelectedPlan is null || SelectedBuildItem is null)
        {
            return;
        }

        if (SelectedBuildItem.Status != "Ready")
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.BuildPlans.FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (plan?.Items is null)
        {
            return;
        }

        var buildItem = plan.Items.FirstOrDefault(i => i.UUID == SelectedBuildItem.ItemUuid);
        if (buildItem is null || string.IsNullOrEmpty(buildItem.BuildLocationUUID))
        {
            return;
        }

        var colony = dataService.Colonies.FirstOrDefault(c => c.UUID == buildItem.BuildLocationUUID);
        if (colony?.Structures is null)
        {
            return;
        }

        ColonyStructure? structure = null;
        if (!string.IsNullOrEmpty(buildItem.StructureUUID))
        {
            structure = colony.Structures.FirstOrDefault(s => s.UUID == buildItem.StructureUUID);
        }

        structure ??= colony.Structures.FirstOrDefault(s => s.IsBuiltAndOnline
            && string.IsNullOrEmpty(s.ManufacturingBlueprintUUID));

        if (structure is null)
        {
            return;
        }

        structure.ManufacturingBlueprintUUID = buildItem.BlueprintUUID;
        structure.ManufacturingQuantity = buildItem.Quantity;
        buildItem.Status = BuildItemStatus.InProgress;

        dataService.IsDirty = true;
        dataService.OnColonyDataChanged(colony.UUID);
        dataService.WriteContext();
        LoadPlanDetail(SelectedPlan);
    }

    partial void OnSelectedPlanChanged(BuildPlanRowViewModel? value)
    {
        LoadPlanDetail(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        var plans = dataService.BuildPlans
            .Where(p => p.OwnerUUID == dataService.CurrentPlayerUUID)
            .ToList();
        if (plans.Count == 0)
        {
            LoadSampleData();
            return;
        }

        foreach (var plan in plans)
        {
            BuildPlans.Add(new BuildPlanRowViewModel
            {
                PlanUuid = plan.UUID ?? string.Empty,
                PlanName = plan.Name ?? string.Empty,
                ColonyName = ResolveColonyName(dataService, plan),
                ItemCount = plan.Items?.Count ?? 0,
            });
        }

        if (BuildPlans.Count > 0)
        {
            SelectedPlan = BuildPlans[0];
        }
    }

    private void LoadPlanDetail(BuildPlanRowViewModel? row)
    {
        BuildItems.Clear();
        Shortfalls.Clear();
        SelectedBuildItem = null;

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleItems(row.PlanName);
            LoadSampleShortfalls(row.PlanName);
            return;
        }

        var plan = dataService.BuildPlans
            .FirstOrDefault(p => p.UUID == row.PlanUuid);
        if (plan?.Items is null || plan.Items.Count == 0)
        {
            LoadSampleItems(row.PlanName);
            LoadSampleShortfalls(row.PlanName);
            return;
        }

        foreach (var item in plan.Items)
        {
            BuildItems.Add(new BuildItemRowViewModel
            {
                ItemUuid = item.UUID ?? string.Empty,
                ItemName = !string.IsNullOrEmpty(item.ItemName) ? item.ItemName : item.CommodityName,
                Quantity = item.Quantity,
                ItemType = item.ItemType.ToString(),
                Status = item.Status.ToString(),
                BuildLocationUuid = item.BuildLocationUUID ?? string.Empty,
                StructureUuid = item.StructureUUID ?? string.Empty,
            });
        }

        if (BuildItems.Count == 0)
        {
            LoadSampleItems(row.PlanName);
        }

        LoadSampleShortfalls(row.PlanName);
    }

    private void LoadSampleData()
    {
        BuildPlans.Add(new BuildPlanRowViewModel { PlanName = "Colony Alpha Expansion", ColonyName = "Alpha Prime", ItemCount = 4 });
        BuildPlans.Add(new BuildPlanRowViewModel { PlanName = "Fleet Refit", ColonyName = "Starbase Omega", ItemCount = 3 });

        if (BuildPlans.Count > 0)
        {
            SelectedPlan = BuildPlans[0];
        }
    }

    private void LoadSampleItems(string planName)
    {
        if (planName == "Colony Alpha Expansion")
        {
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Mining Rig Mk2", Quantity = 2, ItemType = "Manufactory", Status = "Staged" });
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Refinery Standard", Quantity = 1, ItemType = "Manufactory", Status = "InProgress" });
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Research Lab", Quantity = 1, ItemType = "Manufactory", Status = "Staged" });
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Iron", Quantity = 500, ItemType = "Mining", Status = "Ready" });
        }
        else
        {
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Shield Generator", Quantity = 4, ItemType = "Manufactory", Status = "Delivering" });
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Laser Mk3", Quantity = 8, ItemType = "Manufactory", Status = "Staged" });
            BuildItems.Add(new BuildItemRowViewModel { ItemName = "Armor Plate", Quantity = 12, ItemType = "Manufactory", Status = "Staged" });
        }
    }

    private void LoadSampleShortfalls(string planName)
    {
        if (planName == "Colony Alpha Expansion")
        {
            Shortfalls.Add(new ShortfallRowViewModel { ResourceName = "Iron (Refined)", Required = 200, Available = 150, Deficit = 50 });
            Shortfalls.Add(new ShortfallRowViewModel { ResourceName = "Copper (Refined)", Required = 100, Available = 30, Deficit = 70 });
            Shortfalls.Add(new ShortfallRowViewModel { ResourceName = "Crystal (Refined)", Required = 50, Available = 50, Deficit = 0 });
        }
        else
        {
            Shortfalls.Add(new ShortfallRowViewModel { ResourceName = "Titanium (Refined)", Required = 300, Available = 100, Deficit = 200 });
            Shortfalls.Add(new ShortfallRowViewModel { ResourceName = "Iron (Refined)", Required = 500, Available = 400, Deficit = 100 });
        }
    }
}
