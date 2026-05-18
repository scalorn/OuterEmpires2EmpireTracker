using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OE2EmpireTracker.Desktop.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the pricing plan DataGrid.
/// </summary>
public sealed partial class PricingPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private int _resourceCount;

    [ObservableProperty]
    private string _ownerUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Pricing Plan document tab.
/// Shows pricing plans with resource price counts.
/// </summary>
public sealed partial class PricingPlanViewModel : DocumentViewModel
{
    public PricingPlanViewModel()
    {
        Title = "Pricing Plans";
        LoadData();
    }

    public ObservableCollection<PricingPlanRowViewModel> Plans { get; } = new ObservableCollection<PricingPlanRowViewModel>();

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
