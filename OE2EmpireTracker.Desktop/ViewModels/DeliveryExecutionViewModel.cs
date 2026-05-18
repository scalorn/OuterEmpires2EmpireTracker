using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;
using OE2EmpireTracker.Desktop.Views;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the delivery plan ComboBox.
/// </summary>
public sealed partial class DeliveryPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private string _routeName = string.Empty;

    [ObservableProperty]
    private bool _completed;

    /// <summary>Gets or sets the UUID of the underlying DeliveryPlan.</summary>
    public string PlanUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the delivery stop list.
/// </summary>
public sealed partial class DeliveryStopRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private string _destinationName = string.Empty;

    [ObservableProperty]
    private bool _stopCompleted;

    /// <summary>Gets or sets the destination UUID for colony data change notifications.</summary>
    public string DestinationUuid { get; set; } = string.Empty;
}

/// <summary>
/// Row item for the delivery load list DataGrid.
/// </summary>
public sealed partial class DeliveryLoadItemRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _itemName = string.Empty;

    [ObservableProperty]
    private int _quantity;

    [ObservableProperty]
    private bool _delivered;

    /// <summary>Gets or sets the item key for matching back to the model.</summary>
    public string ItemKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the destination colony UUID for this item.</summary>
    public string DestinationColonyUuid { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for the Delivery Execution document tab.
/// Shows plan selection, stop execution, and load list with mark-delivered support.
/// </summary>
public sealed partial class DeliveryExecutionViewModel : DocumentViewModel
{
    [ObservableProperty]
    private DeliveryPlanRowViewModel? _selectedPlan;

    [ObservableProperty]
    private DeliveryLoadItemRowViewModel? _selectedLoadItem;

    [ObservableProperty]
    private string _cargoVolumeDisplay = string.Empty;

    public DeliveryExecutionViewModel()
    {
        Title = "Delivery Execution";

        WeakReferenceMessenger.Default.Register<DeliveryDataChangedMessage>(this, (r, _) =>
        {
            ((DeliveryExecutionViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<DeliveryPlanRowViewModel> Plans { get; } = new ObservableCollection<DeliveryPlanRowViewModel>();

    public ObservableCollection<DeliveryStopRowViewModel> Stops { get; } = new ObservableCollection<DeliveryStopRowViewModel>();

    public ObservableCollection<DeliveryLoadItemRowViewModel> LoadItems { get; } = new ObservableCollection<DeliveryLoadItemRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Plans.Clear();
        Stops.Clear();
        LoadItems.Clear();
        SelectedPlan = null;
        SelectedLoadItem = null;
        LoadData();
    }

    /// <summary>Marks the selected load item as delivered and fires ColonyDataChanged (REQ-DCE-030a).
    /// For flatpack items, also stages the structure at the destination colony (H3.5).</summary>
    [RelayCommand]
    private void MarkDelivered()
    {
        if (SelectedPlan is null || SelectedLoadItem is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            return;
        }

        // Mark the item as delivered in the UI
        SelectedLoadItem.Delivered = true;

        // Find the plan model and mark the corresponding item delivered
        var planModel = dataService.DeliveryPlans
            .FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        DeliveryItem? matchedItem = null;
        if (planModel?.Stops is not null)
        {
            foreach (var stop in planModel.Stops)
            {
                foreach (var dropItem in stop.DropOff)
                {
                    string key = $"{dropItem.ItemType}|{dropItem.BaseItemTypeID}|{dropItem.ResourcePurity}";
                    if (key == SelectedLoadItem.ItemKey && !dropItem.Delivered)
                    {
                        dropItem.Delivered = true;
                        matchedItem = dropItem;
                        break;
                    }
                }

                if (matchedItem is not null)
                {
                    break;
                }
            }
        }

        // H3.5: If the delivered item is a flatpack, stage the structure at the destination colony
        if (matchedItem is not null
            && matchedItem.ItemType == ItemType.ItemTypeEnum.Flatpack
            && !string.IsNullOrEmpty(SelectedLoadItem.DestinationColonyUuid))
        {
            var colonyService = App.Services?.GetService(typeof(Desktop.Services.ColonyService)) as Desktop.Services.ColonyService;
            colonyService?.StageFlatpackStructure(
                SelectedLoadItem.DestinationColonyUuid,
                matchedItem.BaseItemTypeID);
        }

        dataService.IsDirty = true;
        dataService.WriteContext();

        // Fire ColonyDataChanged for the destination colony (REQ-DCE-030a)
        string destColony = SelectedLoadItem.DestinationColonyUuid;
        if (!string.IsNullOrEmpty(destColony))
        {
            dataService.OnColonyDataChanged(destColony);
        }
    }

    /// <summary>Opens the Auto-Fill dialog to generate delivery items from a build plan (H2.4).</summary>
    [RelayCommand]
    private async System.Threading.Tasks.Task AutoFill()
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var vm = new AutoFillDialogViewModel();
        var dialog = new AutoFillDialog { DataContext = vm };

        Window? owner = null;
        if (Application.Current?.ApplicationLifetime
            is IClassicDesktopStyleApplicationLifetime desktop)
        {
            owner = desktop.MainWindow;
        }

        if (owner is not null)
        {
            await dialog.ShowDialog(owner);
        }
        else
        {
            dialog.Show();
            return;
        }

        if (!vm.Confirmed || vm.GeneratedItems.Count == 0)
        {
            return;
        }

        ApplyAutoFillItems(vm.GeneratedItems);
    }

    private void ApplyAutoFillItems(
        System.Collections.ObjectModel.ObservableCollection<AutoFillItemRowViewModel> items)
    {
        if (SelectedPlan is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var planService = App.Services?.GetService(typeof(Desktop.Services.DeliveryPlanService))
            as Desktop.Services.DeliveryPlanService;
        if (dataService is null || !dataService.IsLoaded || planService is null)
        {
            return;
        }

        var planModel = dataService.DeliveryPlans
            .FirstOrDefault(p => p.UUID == SelectedPlan.PlanUuid);
        if (planModel is null)
        {
            return;
        }

        // Group items by destination to create stops
        var grouped = items
            .GroupBy(i => i.DestinationUuid)
            .ToList();

        var stops = new List<DeliveryPlanStop>();
        int seq = 1;

        foreach (var group in grouped)
        {
            var stop = new DeliveryPlanStop
            {
                Sequence = seq++,
                DestinationType = DestinationType.Colony,
                DestinationUUID = group.Key,
                DropOff = group.Select(i => new DeliveryItem
                {
                    Name = i.ItemName,
                    Quantity = i.Quantity,
                    ItemType = ItemType.ItemTypeEnum.Resource,
                }).ToList(),
            };

            stops.Add(stop);
        }

        planService.Update(planModel.UUID, new DeliveryPlanUpdateRequest
        {
            Name = planModel.Name,
            Stops = stops,
        });
    }

    partial void OnSelectedPlanChanged(DeliveryPlanRowViewModel? value)
    {
        SelectedLoadItem = null;
        LoadStopsForPlan(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var plan in dataService.DeliveryPlans)
        {
            var route = dataService.DeliveryRoutes
                .FirstOrDefault(r => r.UUID == plan.RouteUUID);
            Plans.Add(new DeliveryPlanRowViewModel
            {
                PlanUuid = plan.UUID ?? string.Empty,
                PlanName = plan.Name ?? string.Empty,
                RouteName = route?.Name ?? string.Empty,
                Completed = plan.Completed,
            });
        }

        if (Plans.Count == 0)
        {
            LoadSampleData();
        }

        if (Plans.Count > 0)
        {
            SelectedPlan = Plans[0];
        }
    }

    private void LoadStopsForPlan(DeliveryPlanRowViewModel? plan)
    {
        Stops.Clear();
        LoadItems.Clear();
        if (plan is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStops();
            return;
        }

        var planModel = dataService.DeliveryPlans
            .FirstOrDefault(p => p.UUID == plan.PlanUuid);
        if (planModel?.Stops is null)
        {
            LoadSampleStops();
            return;
        }

        foreach (var stop in planModel.Stops.OrderBy(s => s.Sequence))
        {
            Stops.Add(new DeliveryStopRowViewModel
            {
                Sequence = stop.Sequence,
                DestinationName = stop.DestinationUUID ?? string.Empty,
                DestinationUuid = stop.DestinationUUID ?? string.Empty,
                StopCompleted = stop.StopCompleted,
            });
        }

        // Build load list with destination tracking
        var loadList = planModel.CalculateLoadList();
        string firstDestColony = planModel.Stops
            .OrderBy(s => s.Sequence)
            .FirstOrDefault()?.DestinationUUID ?? string.Empty;

        foreach (var item in loadList)
        {
            string key = $"{item.ItemType}|{item.BaseItemTypeID}|{item.ResourcePurity}";
            LoadItems.Add(new DeliveryLoadItemRowViewModel
            {
                ItemKey = key,
                ItemName = item.ExtendedName ?? item.Name ?? string.Empty,
                Quantity = item.Quantity,
                Delivered = item.Delivered,
                DestinationColonyUuid = firstDestColony,
            });
        }

        // Compute cargo volume display (H3.4)
        UpdateCargoVolumeDisplay(loadList, dataService);

        if (Stops.Count == 0)
        {
            LoadSampleStops();
        }
    }

    private void LoadSampleData()
    {
        Plans.Add(new DeliveryPlanRowViewModel
        {
            PlanName = "Weekly Supply Run",
            RouteName = "Alpha Circuit",
            Completed = false,
        });
    }

    private void LoadSampleStops()
    {
        Stops.Add(new DeliveryStopRowViewModel { Sequence = 1, DestinationName = "Colony Alpha", StopCompleted = true });
        Stops.Add(new DeliveryStopRowViewModel { Sequence = 2, DestinationName = "Station Beta", StopCompleted = false });
        Stops.Add(new DeliveryStopRowViewModel { Sequence = 3, DestinationName = "Colony Gamma", StopCompleted = false });

        LoadItems.Add(new DeliveryLoadItemRowViewModel { ItemName = "Iron (High)", Quantity = 500, Delivered = false });
        LoadItems.Add(new DeliveryLoadItemRowViewModel { ItemName = "Copper (Medium)", Quantity = 300, Delivered = false });
        LoadItems.Add(new DeliveryLoadItemRowViewModel { ItemName = "Electronics", Quantity = 100, Delivered = true });
        LoadItems.Add(new DeliveryLoadItemRowViewModel { ItemName = "Fuel Cells", Quantity = 50, Delivered = false });
        LoadItems.Add(new DeliveryLoadItemRowViewModel { ItemName = "Food Rations", Quantity = 200, Delivered = false });
    }

    /// <summary>Computes and sets the CargoVolumeDisplay from the load list (H3.4).</summary>
    private void UpdateCargoVolumeDisplay(
        System.Collections.Generic.List<OE2EmpireTracker.Models.DeliveryItem> loadList,
        DataService dataService)
    {
        ReadOnlyBlueprint? FindBlueprint(string id)
        {
            var bp = dataService.Blueprints.FirstOrDefault(b => b.UUID == id);
            return bp is not null ? new ReadOnlyBlueprint(bp) : null;
        }

        var result = OE2EmpireTracker.Services.CargoVolumeService.ComputeLoadVolume(
            loadList,
            FindBlueprint!);

        CargoVolumeDisplay = $"Vol: {result.TotalVolume:N0}  Mass: {result.TotalMass:N0}";
    }
}
