using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the build plan list.
/// </summary>
public sealed partial class BuildPlanRowViewModel : ObservableObject
{
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
            .FirstOrDefault(p => p.Name == row.PlanName);
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
                ItemName = !string.IsNullOrEmpty(item.ItemName) ? item.ItemName : item.CommodityName,
                Quantity = item.Quantity,
                ItemType = item.ItemType.ToString(),
                Status = item.Status.ToString(),
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
