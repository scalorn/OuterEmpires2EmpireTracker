using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the pricing plan DataGrid.
/// </summary>
public sealed partial class PricingPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planUuid = string.Empty;

    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private int _resourceCount;

    [ObservableProperty]
    private string _ownerUuid = string.Empty;
}

/// <summary>
/// ViewModel for the Pricing Plan document tab.
/// Shows pricing plans with CRUD operations.
/// </summary>
public sealed partial class PricingPlanViewModel : DocumentViewModel
{
    [ObservableProperty]
    private PricingPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private string _editPlanName = string.Empty;

    public PricingPlanViewModel()
    {
        Title = "Pricing Plans";

        WeakReferenceMessenger.Default.Register<PricingDataChangedMessage>(this, (r, m) =>
        {
            ((PricingPlanViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<PricingPlanRowViewModel> Plans { get; } = new ObservableCollection<PricingPlanRowViewModel>();

    [RelayCommand]
    private void NewPlan()
    {
        var svc = App.Services?.GetService(typeof(PricingPlanService)) as PricingPlanService;
        svc?.Create(new PricingPlanCreateRequest { Name = "New Pricing Plan" });
    }

    [RelayCommand]
    private void SavePlan()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(PricingPlanService)) as PricingPlanService;
        svc?.Update(SelectedPlan.PlanUuid, new PricingPlanUpdateRequest { Name = EditPlanName });
    }

    [RelayCommand]
    private void DeletePlan()
    {
        if (SelectedPlan is null || string.IsNullOrEmpty(SelectedPlan.PlanUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(PricingPlanService)) as PricingPlanService;
        svc?.Delete(SelectedPlan.PlanUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Plans.Clear();
        SelectedPlan = null;
        EditPlanName = string.Empty;
        LoadData();
    }

    partial void OnSelectedPlanChanged(PricingPlanRowViewModel? value)
    {
        EditPlanName = value?.PlanName ?? string.Empty;
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
                PlanUuid = plan.UUID ?? string.Empty,
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
