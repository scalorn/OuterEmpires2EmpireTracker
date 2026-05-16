using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the pricing plan list DataGrid.
/// </summary>
public sealed partial class PricingPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private int _resourceCount;

    [ObservableProperty]
    private string _ownerUuid = string.Empty;

    /// <summary>Gets or sets the UUID of the underlying PricingPlan.</summary>
    [ObservableProperty]
    private string _uuid = string.Empty;
}

/// <summary>
/// Row item for the resource price DataGrid.
/// </summary>
public sealed partial class ResourcePriceRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _resourceName = string.Empty;

    [ObservableProperty]
    private string _purity = string.Empty;

    /// <summary>
    /// Price as string to distinguish empty (unpriced) from "0" (free).
    /// </summary>
    [ObservableProperty]
    private string _price = string.Empty;
}

/// <summary>
/// ViewModel for the Pricing Plan document tab.
/// Shows pricing plans with resource price editing.
/// </summary>
public sealed partial class PricingPlanViewModel : DocumentViewModel
{
    [ObservableProperty]
    private PricingPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private string _planDescription = string.Empty;

    [ObservableProperty]
    private string _fixedCostPerItem = "0";

    [ObservableProperty]
    private string _hourlyCostRate = "0";

    public PricingPlanViewModel()
    {
        Title = "Pricing Plans";
        LoadData();
    }

    public ObservableCollection<PricingPlanRowViewModel> Plans { get; } = new();

    public ObservableCollection<ResourcePriceRowViewModel> ResourcePrices { get; } = new();

    [RelayCommand]
    private void SavePlan()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.PricingPlans
            .FirstOrDefault(p => p.UUID == SelectedPlan.Uuid);
        if (plan is null)
        {
            return;
        }

        plan.Name = PlanName;
        plan.Description = PlanDescription;

        if (decimal.TryParse(FixedCostPerItem, out var fixedCost) && fixedCost >= 0)
        {
            plan.FixedCostPerItem = fixedCost;
        }

        if (decimal.TryParse(HourlyCostRate, out var hourlyRate) && hourlyRate >= 0)
        {
            plan.HourlyCostRate = hourlyRate;
        }

        // Write prices back: empty = remove entry, "0" = zero price
        plan.ResourcePrices.Clear();
        foreach (var row in ResourcePrices)
        {
            if (string.IsNullOrWhiteSpace(row.Price))
            {
                continue;
            }

            if (!decimal.TryParse(row.Price, out var price) || price < 0)
            {
                continue;
            }

            var key = $"{row.ResourceName}|{row.Purity}";
            plan.ResourcePrices[key] = price;
        }

        dataService.IsDirty = true;
        dataService.WriteContext();
        SelectedPlan.PlanName = PlanName;
        SelectedPlan.ResourceCount = plan.ResourcePrices.Count;
    }

    partial void OnSelectedPlanChanged(PricingPlanRowViewModel? value)
    {
        LoadPlanDetail(value);
    }

    private void LoadPlanDetail(PricingPlanRowViewModel? row)
    {
        ResourcePrices.Clear();
        PlanName = string.Empty;
        PlanDescription = string.Empty;
        FixedCostPerItem = "0";
        HourlyCostRate = "0";

        if (row is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        var plan = dataService.PricingPlans
            .FirstOrDefault(p => p.UUID == row.Uuid);
        if (plan is null)
        {
            return;
        }

        PlanName = plan.Name;
        PlanDescription = plan.Description;
        FixedCostPerItem = plan.FixedCostPerItem.ToString();
        HourlyCostRate = plan.HourlyCostRate.ToString();

        // Load all known resources with their prices
        LoadResourcePriceRows(plan);
    }

    private void LoadResourcePriceRows(PricingPlan plan)
    {
        // Add all known resources at Refined, S1, S2 purities
        foreach (var resource in Resource.Resources)
        {
            var name = resource.Name;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            // Determine purity based on resource name prefix
            string purity;
            if (name.StartsWith("S1.") || name.StartsWith("S1 "))
            {
                purity = "S1";
            }
            else if (name.StartsWith("S2.") || name.StartsWith("S2 "))
            {
                purity = "S2";
            }
            else
            {
                purity = "Refined";
            }

            var key = $"{name}|{purity}";
            var price = plan.ResourcePrices.TryGetValue(key, out var p)
                ? p.ToString()
                : string.Empty;

            ResourcePrices.Add(new ResourcePriceRowViewModel
            {
                ResourceName = name,
                Purity = purity,
                Price = price,
            });
        }
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var plan in dataService.PricingPlans)
        {
            Plans.Add(new PricingPlanRowViewModel
            {
                PlanName = plan.Name ?? string.Empty,
                ResourceCount = plan.ResourcePrices?.Count ?? 0,
                OwnerUuid = plan.OwnerUUID ?? string.Empty,
                Uuid = plan.UUID ?? string.Empty,
            });
        }

        if (Plans.Count == 0)
        {
            LoadSampleData();
        }
    }

    private void LoadSampleData()
    {
        Plans.Add(new PricingPlanRowViewModel
        {
            PlanName = "Standard Pricing",
            ResourceCount = 12,
            OwnerUuid = "player-1",
        });
        Plans.Add(new PricingPlanRowViewModel
        {
            PlanName = "Premium Pricing",
            ResourceCount = 8,
            OwnerUuid = "player-1",
        });
    }
}
