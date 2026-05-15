using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using OE2EmpireTracker.Desktop.Services;
using OE2EmpireTracker.Desktop.ViewModels.Messages;

namespace OE2EmpireTracker.Desktop.ViewModels;

/// <summary>
/// Row item for the delivery plan ComboBox.
/// </summary>
public sealed partial class DeliveryPlanRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _planUuid = string.Empty;

    [ObservableProperty]
    private string _planName = string.Empty;

    [ObservableProperty]
    private string _routeName = string.Empty;

    [ObservableProperty]
    private bool _completed;
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
}

/// <summary>
/// ViewModel for the Delivery Execution document tab.
/// Shows plan selection, stop execution, and load list.
/// Subscribes to <see cref="DeliveryDataChangedMessage"/> for auto-refresh.
/// </summary>
public sealed partial class DeliveryExecutionViewModel : DocumentViewModel
{
    [ObservableProperty]
    private DeliveryPlanRowViewModel? _selectedPlan;

    public DeliveryExecutionViewModel()
    {
        Title = "Delivery Execution";

        WeakReferenceMessenger.Default.Register<DeliveryDataChangedMessage>(this, (r, m) =>
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
        LoadData();
    }

    partial void OnSelectedPlanChanged(DeliveryPlanRowViewModel? value)
    {
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
                StopCompleted = stop.StopCompleted,
            });
        }

        var loadList = planModel.CalculateLoadList();
        foreach (var item in loadList)
        {
            LoadItems.Add(new DeliveryLoadItemRowViewModel
            {
                ItemName = item.ExtendedName ?? item.Name ?? string.Empty,
                Quantity = item.Quantity,
                Delivered = item.Delivered,
            });
        }

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
}
