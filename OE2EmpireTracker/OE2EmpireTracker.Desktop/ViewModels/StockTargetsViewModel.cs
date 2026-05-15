using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the stock plan list.
/// </summary>
public sealed partial class StockPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planUuid = string.Empty;

    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private int _targetCount;

    [ObservableProperty]
    private bool _isActive;
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
    private int _criticalThreshold;

    [ObservableProperty]
    private string _scope = string.Empty;
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
/// Includes CRUD operations for stock plans.
/// </summary>
public sealed partial class StockTargetsViewModel : DocumentViewModel
{
    [ObservableProperty]
    private StockPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private StockProfileRowViewModel? _selectedProfile;

    [ObservableProperty]
    private string _editPlanName = string.Empty;

    public StockTargetsViewModel()
    {
        Title = "Stock Targets";

        WeakReferenceMessenger.Default.Register<StockDataChangedMessage>(this, (r, m) =>
        {
            ((StockTargetsViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<StockPlanRowViewModel> StockPlans { get; } = new ObservableCollection<StockPlanRowViewModel>();

    public ObservableCollection<StockTargetRowViewModel> Targets { get; } = new ObservableCollection<StockTargetRowViewModel>();

    public ObservableCollection<StockProfileRowViewModel> StockProfiles { get; } = new ObservableCollection<StockProfileRowViewModel>();

    public ObservableCollection<StockProfileEntryRowViewModel> ProfileEntries { get; } = new ObservableCollection<StockProfileEntryRowViewModel>();

    [RelayCommand]
    private void NewPlan()
    {
        var svc = App.Services?.GetService(typeof(StockTargetService)) as StockTargetService;
        svc?.Create(new StockPlanCreateRequest { Name = "New Stock Plan" });
    }

    [RelayCommand]
    private void SavePlan()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(StockTargetService)) as StockTargetService;
        svc?.Update(SelectedPlan.PlanUuid, new StockPlanUpdateRequest { Name = EditPlanName });
    }

    [RelayCommand]
    private void DeletePlan()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(StockTargetService)) as StockTargetService;
        svc?.Delete(SelectedPlan.PlanUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        StockPlans.Clear();
        Targets.Clear();
        StockProfiles.Clear();
        ProfileEntries.Clear();
        SelectedPlan = null;
        SelectedProfile = null;
        EditPlanName = string.Empty;
        LoadData();
    }

    partial void OnSelectedPlanChanged(StockPlanRowViewModel? value)
    {
        EditPlanName = value?.PlanName ?? string.Empty;
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
            Targets.Add(new StockTargetRowViewModel
            {
                ItemName = target.ItemName ?? string.Empty,
                TargetQuantity = target.TargetQuantity,
                CriticalThreshold = target.CriticalThreshold,
                Scope = target.Scope.ToString(),
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
            Targets.Add(new StockTargetRowViewModel { ItemName = "Iron", TargetQuantity = 1000, CriticalThreshold = 200, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Copper", TargetQuantity = 500, CriticalThreshold = 100, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Fuel Cells", TargetQuantity = 200, CriticalThreshold = 50, Scope = "Colony" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Food Rations", TargetQuantity = 300, CriticalThreshold = 75, Scope = "EmpireWide" });
        }
        else
        {
            Targets.Add(new StockTargetRowViewModel { ItemName = "Missiles", TargetQuantity = 100, CriticalThreshold = 25, Scope = "Station" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Armor Plates", TargetQuantity = 50, CriticalThreshold = 10, Scope = "EmpireWide" });
            Targets.Add(new StockTargetRowViewModel { ItemName = "Shield Cells", TargetQuantity = 75, CriticalThreshold = 15, Scope = "Station" });
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
