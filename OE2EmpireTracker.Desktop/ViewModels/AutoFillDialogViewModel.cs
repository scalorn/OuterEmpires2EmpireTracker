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
/// Row item for the auto-fill generated items DataGrid.
/// </summary>
public sealed partial class AutoFillItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private string _destination = string.Empty;

    /// <summary>Gets or sets the destination colony UUID.</summary>
    public string DestinationUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the build plan ComboBox in the auto-fill dialog.
/// </summary>
public sealed partial class AutoFillBuildPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    /// <summary>Gets or sets the UUID of the build plan.</summary>
    public string PlanUuid { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for the Auto-Fill Delivery Plan dialog.
/// Generates delivery plan items from build plan resource shortfalls.
/// </summary>
public sealed partial class AutoFillDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private AutoFillBuildPlanRowViewModel? _selectedBuildPlan;

    [ObservableProperty]
    private bool _confirmed;

    public AutoFillDialogViewModel()
    {
        LoadBuildPlans();
    }

    public ObservableCollection<AutoFillBuildPlanRowViewModel> BuildPlans { get; } = new ();

    public ObservableCollection<AutoFillItemRowViewModel> GeneratedItems { get; } = new ();

    private static string ResolveLocationName(
        DataService dataService, string? locationUuid)
    {
        if (string.IsNullOrEmpty(locationUuid))
        {
            return "Colony";
        }

        var colony = dataService.Colonies
            .FirstOrDefault(c => c.UUID == locationUuid);
        if (colony is not null)
        {
            return colony.ColonyName ?? "Colony";
        }

        var station = dataService.Stations
            .FirstOrDefault(s => s.UUID == locationUuid);
        if (station is not null)
        {
            return station.Name ?? "Station";
        }

        return "Colony";
    }

    /// <summary>
    /// Generates delivery items from the selected build plan's staged items.
    /// For each staged build item, looks up the blueprint's resource requirements
    /// and creates a delivery row per resource.
    /// </summary>
    [RelayCommand]
    private void Generate()
    {
        GeneratedItems.Clear();
        if (SelectedBuildPlan is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.BuildPlans
            .FirstOrDefault(p => p.UUID == SelectedBuildPlan.PlanUuid);
        if (plan?.Items is null)
        {
            return;
        }

        var stagedItems = plan.Items
            .Where(i => i.Status == BuildItemStatus.Staged
                     || i.Status == BuildItemStatus.Delivering)
            .ToList();

        foreach (var item in stagedItems)
        {
            var bp = dataService.Blueprints
                .FirstOrDefault(b => b.UUID == item.BlueprintUUID);
            if (bp?.Resources is null || bp.Resources.Count == 0)
            {
                continue;
            }

            string destName = ResolveLocationName(
                dataService, item.BuildLocationUUID);

            foreach (var res in bp.Resources)
            {
                if (!int.TryParse(res.Value, out int qty) || qty <= 0)
                {
                    continue;
                }

                GeneratedItems.Add(new AutoFillItemRowViewModel
                {
                    ItemName = res.Key,
                    Quantity = qty * item.Quantity,
                    Source = "Station",
                    Destination = destName,
                    DestinationUuid = item.BuildLocationUUID ?? string.Empty,
                });
            }
        }
    }

    /// <summary>
    /// Confirms the generated items and signals the dialog to close.
    /// </summary>
    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
    }

    /// <summary>
    /// Cancels the dialog without generating items.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Confirmed = false;
    }

    private void LoadBuildPlans()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plans = dataService.BuildPlans
            .Where(p => p.OwnerUUID == dataService.CurrentPlayerUUID
                     && p.IsActive)
            .ToList();

        foreach (var plan in plans)
        {
            BuildPlans.Add(new AutoFillBuildPlanRowViewModel
            {
                PlanUuid = plan.UUID ?? string.Empty,
                PlanName = plan.Name ?? string.Empty,
            });
        }

        if (BuildPlans.Count > 0)
        {
            SelectedBuildPlan = BuildPlans[0];
        }
    }
}
