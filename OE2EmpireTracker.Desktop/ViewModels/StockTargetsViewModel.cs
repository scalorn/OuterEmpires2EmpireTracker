using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the stock plan list.
/// </summary>
public sealed partial class StockPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private int _targetCount;

    [ObservableProperty]
    private bool _isActive;

    /// <summary>Gets or sets the UUID of the underlying StockPlan.</summary>
    public string PlanUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the stock target DataGrid.
/// </summary>
public sealed partial class StockTargetRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _targetQuantity;

    [ObservableProperty]
    private int _currentQuantity;

    [ObservableProperty]
    private int _shortfall;

    [ObservableProperty]
    private string _scope = string.Empty;

    /// <summary>Gets or sets the stock level color indicator (Green/Yellow/Red).</summary>
    [ObservableProperty]
    private string _stockLevelColor = "Green";
}

/// <summary>
/// Row item for the stock profile list.
/// </summary>
public sealed partial class StockProfileRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _profileName = string.Empty;

    [ObservableProperty]
    private int _entryCount;

    [ObservableProperty]
    private bool _isActive;
}

/// <summary>
/// Row item for the stock profile entry DataGrid.
/// </summary>
public sealed partial class StockProfileEntryRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _groupId = string.Empty;

    [ObservableProperty]
    private string _stockPlanName = string.Empty;
}

/// <summary>
/// ViewModel for the Stock Targets document tab.
/// Shows stock plans with targets and stock profiles with entries.
/// Provides Check and Generate Orders functionality (M5).
/// </summary>
public sealed partial class StockTargetsViewModel : DocumentViewModel
{
    [ObservableProperty]
    private StockPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private StockProfileRowViewModel? _selectedProfile;

    [ObservableProperty]
    private StockTargetRowViewModel? _selectedTarget;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public StockTargetsViewModel()
    {
        Title = "Stock Targets";
        LoadData();
    }

    public ObservableCollection<StockPlanRowViewModel> StockPlans { get; } = new ();

    public ObservableCollection<StockTargetRowViewModel> Targets { get; } = new ();

    public ObservableCollection<StockProfileRowViewModel> StockProfiles { get; } = new ();

    public ObservableCollection<StockProfileEntryRowViewModel> ProfileEntries { get; } = new ();

    private static string ComputeStockLevelColor(int current, int target)
    {
        if (target <= 0)
        {
            return "Green";
        }

        double ratio = (double)current / target;
        if (ratio >= 1.0)
        {
            return "Green";
        }

        if (ratio >= 0.5)
        {
            return "Yellow";
        }

        return "Red";
    }

    private static int GetCurrentInventory(DataService dataService, StockTarget target)
    {
        int total = 0;
        var colonies = dataService.Colonies
            .Where(c => c.OwnerUUID == dataService.CurrentPlayerUUID)
            .ToList();

        switch (target.Scope)
        {
            case StockTargetScope.EmpireWide:
                foreach (var colony in colonies)
                {
                    total += GetItemQuantityInColony(colony, target.ItemName);
                }

                break;

            case StockTargetScope.Colony:
                var targetColony = colonies.FirstOrDefault(c => c.UUID == target.LocationUUID);
                if (targetColony is not null)
                {
                    total = GetItemQuantityInColony(targetColony, target.ItemName);
                }

                break;

            case StockTargetScope.Station:
                // Station inventory not tracked in colony items; return 0
                break;
        }

        return total;
    }

    private static int GetItemQuantityInColony(Colony colony, string itemName)
    {
        if (colony.Items?.Items is null)
        {
            return 0;
        }

        return colony.Items.Items.Values
            .Where(i => string.Equals(i.Name, itemName, StringComparison.OrdinalIgnoreCase))
            .Sum(i => i.Quantity);
    }

    /// <summary>Adds a new target to the selected stock plan.</summary>
    [RelayCommand]
    private void AddTarget()
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

        var plan = dataService.StockPlans
            .FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (plan is null)
        {
            return;
        }

        plan.Targets ??= new System.Collections.Generic.List<StockTarget>();
        plan.Targets.Add(new StockTarget
        {
            ItemName = "New Item",
            TargetQuantity = 100,
            Scope = StockTargetScope.EmpireWide,
        });

        dataService.IsDirty = true;
        SelectedPlan.TargetCount = plan.Targets.Count;
        LoadTargetsForPlan(SelectedPlan);
    }

    /// <summary>Removes the selected target from the stock plan.</summary>
    [RelayCommand]
    private void RemoveTarget()
    {
        if (SelectedPlan is null || SelectedTarget is null
            || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.StockPlans
            .FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (plan?.Targets is null)
        {
            return;
        }

        var toRemove = plan.Targets
            .FirstOrDefault(t => t.ItemName == SelectedTarget.ItemName);
        if (toRemove is not null)
        {
            plan.Targets.Remove(toRemove);
            dataService.IsDirty = true;
            SelectedPlan.TargetCount = plan.Targets.Count;
            LoadTargetsForPlan(SelectedPlan);
        }
    }

    /// <summary>
    /// Evaluates all active stock plans, checks current inventory vs targets,
    /// computes shortfalls, and creates build items in the linked replenishment build plan.
    /// </summary>
    [RelayCommand]
    private void CheckAndGenerate()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            StatusMessage = "Data not loaded";
            return;
        }

        var activePlans = dataService.StockPlans
            .Where(p => p.IsActive && p.OwnerUUID == dataService.CurrentPlayerUUID)
            .ToList();

        if (activePlans.Count == 0)
        {
            StatusMessage = "No active stock plans";
            return;
        }

        int totalGenerated = 0;
        string lastPlanName = string.Empty;

        foreach (var plan in activePlans)
        {
            if (plan.Targets is null || plan.Targets.Count == 0)
            {
                continue;
            }

            var buildPlan = dataService.BuildPlans
                .FirstOrDefault(bp => bp.UUID == plan.ReplenishmentBuildPlanUUID);
            if (buildPlan is null)
            {
                StatusMessage = $"Build plan not found for '{plan.Name}'";
                return;
            }

            int planItems = 0;
            foreach (var target in plan.Targets)
            {
                int current = GetCurrentInventory(dataService, target);
                int shortfall = target.TargetQuantity - current;
                if (shortfall > 0)
                {
                    var buildItem = new BuildItem
                    {
                        UUID = Guid.NewGuid().ToString(),
                        ItemType = BuildItemType.Manufactory,
                        Status = BuildItemStatus.Staged,
                        ItemName = target.ItemName,
                        Quantity = shortfall,
                    };

                    buildPlan.Items.Add(buildItem);
                    planItems++;
                }
            }

            if (planItems > 0)
            {
                totalGenerated += planItems;
                lastPlanName = plan.Name ?? string.Empty;
                dataService.IsDirty = true;
                dataService.OnBuildPlanDataChanged(buildPlan.UUID);
            }
        }

        if (totalGenerated > 0)
        {
            dataService.WriteContext();
            StatusMessage = $"Generated {totalGenerated} items in plan '{lastPlanName}'";
        }
        else
        {
            StatusMessage = "All targets met \u2014 no orders needed";
        }

        // Refresh targets grid to show updated current/shortfall
        LoadTargetsForPlan(SelectedPlan);
    }

    partial void OnSelectedPlanChanged(StockPlanRowViewModel? value)
    {
        LoadTargetsForPlan(value);
    }

    partial void OnSelectedProfileChanged(StockProfileRowViewModel? value)
    {
        LoadEntriesForProfile(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var plan in dataService.StockPlans)
        {
            StockPlans.Add(new StockPlanRowViewModel
            {
                PlanUuid = plan.UUID ?? string.Empty,
                PlanName = plan.Name ?? string.Empty,
                TargetCount = plan.Targets?.Count ?? 0,
                IsActive = plan.IsActive,
            });
        }

        foreach (var profile in dataService.StockProfiles)
        {
            StockProfiles.Add(new StockProfileRowViewModel
            {
                ProfileName = profile.Name ?? string.Empty,
                EntryCount = profile.Entries?.Count ?? 0,
                IsActive = profile.IsActive,
            });
        }

        if (StockPlans.Count == 0 && StockProfiles.Count == 0)
        {
            LoadSampleData();
        }

        if (StockPlans.Count > 0)
        {
            SelectedPlan = StockPlans[0];
        }

        if (StockProfiles.Count > 0)
        {
            SelectedProfile = StockProfiles[0];
        }
    }

    private void LoadTargetsForPlan(StockPlanRowViewModel? plan)
    {
        Targets.Clear();
        if (plan is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleTargets(plan.PlanName);
            return;
        }

        var planModel = dataService.StockPlans
            .FirstOrDefault(p => p.UUID == plan.PlanUuid);
        if (planModel?.Targets is null)
        {
            LoadSampleTargets(plan.PlanName);
            return;
        }

        foreach (var target in planModel.Targets)
        {
            int current = GetCurrentInventory(dataService, target);
            int shortfall = Math.Max(0, target.TargetQuantity - current);

            // M3: Show "(template)" suffix when ShipTemplateUUID is set
            string displayName = target.ItemName ?? string.Empty;
            if (!string.IsNullOrEmpty(target.ShipTemplateUUID))
            {
                displayName += " (template)";
            }

            // M6: Stock level color based on current vs target
            string color = ComputeStockLevelColor(current, target.TargetQuantity);

            Targets.Add(new StockTargetRowViewModel
            {
                ItemName = displayName,
                TargetQuantity = target.TargetQuantity,
                CurrentQuantity = current,
                Shortfall = shortfall,
                Scope = target.Scope.ToString(),
                StockLevelColor = color,
            });
        }

        if (Targets.Count == 0)
        {
            LoadSampleTargets(plan.PlanName);
        }
    }

    private void LoadEntriesForProfile(StockProfileRowViewModel? profile)
    {
        ProfileEntries.Clear();
        if (profile is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleEntries(profile.ProfileName);
            return;
        }

        var profileModel = dataService.StockProfiles
            .FirstOrDefault(p => p.Name == profile.ProfileName);
        if (profileModel?.Entries is null)
        {
            LoadSampleEntries(profile.ProfileName);
            return;
        }

        foreach (var entry in profileModel.Entries)
        {
            var planName = dataService.StockPlans
                .FirstOrDefault(p => p.UUID == entry.StockPlanUUID)?.Name ?? string.Empty;
            ProfileEntries.Add(new StockProfileEntryRowViewModel
            {
                GroupId = entry.GroupID ?? string.Empty,
                StockPlanName = planName,
            });
        }

        if (ProfileEntries.Count == 0)
        {
            LoadSampleEntries(profile.ProfileName);
        }
    }

    private void LoadSampleData()
    {
        StockPlans.Add(new StockPlanRowViewModel { PlanName = "Essential Supplies", TargetCount = 4, IsActive = true });
        StockPlans.Add(new StockPlanRowViewModel { PlanName = "Military Reserves", TargetCount = 3, IsActive = true });

        StockProfiles.Add(new StockProfileRowViewModel { ProfileName = "Mining Colony", EntryCount = 2, IsActive = true });
        StockProfiles.Add(new StockProfileRowViewModel { ProfileName = "Trade Hub", EntryCount = 3, IsActive = true });
    }

    private void LoadSampleTargets(string planName)
    {
        if (planName == "Essential Supplies")
        {
            Targets.Add(new StockTargetRowViewModel { ItemName = "Iron", TargetQuantity = 1000, CurrentQuantity = 450, Shortfall = 550, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Copper", TargetQuantity = 500, CurrentQuantity = 230, Shortfall = 270, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Fuel Cells", TargetQuantity = 200, CurrentQuantity = 200, Shortfall = 0, Scope = "Colony" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Food Rations", TargetQuantity = 300, CurrentQuantity = 300, Shortfall = 0, Scope = "EmpireWide" });
        }
        else
        {
            Targets.Add(new StockTargetRowViewModel { ItemName = "Missiles", TargetQuantity = 100, CurrentQuantity = 60, Shortfall = 40, Scope = "Station" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Armor Plates", TargetQuantity = 50, CurrentQuantity = 50, Shortfall = 0, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Shield Cells", TargetQuantity = 75, CurrentQuantity = 30, Shortfall = 45, Scope = "Station" });
        }
    }

    private void LoadSampleEntries(string profileName)
    {
        if (profileName == "Mining Colony")
        {
            ProfileEntries.Add(new StockProfileEntryRowViewModel { GroupId = "Resources", StockPlanName = "Essential Supplies" });
            ProfileEntries.Add(new StockProfileEntryRowViewModel { GroupId = "Equipment", StockPlanName = "Essential Supplies" });
        }
        else
        {
            ProfileEntries.Add(new StockProfileEntryRowViewModel { GroupId = "Trade Goods", StockPlanName = "Essential Supplies" });
            ProfileEntries.Add(new StockProfileEntryRowViewModel { GroupId = "Military", StockPlanName = "Military Reserves" });
            ProfileEntries.Add(new StockProfileEntryRowViewModel { GroupId = "Fuel", StockPlanName = "Essential Supplies" });
        }
    }
}
