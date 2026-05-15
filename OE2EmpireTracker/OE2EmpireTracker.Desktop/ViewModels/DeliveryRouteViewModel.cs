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
/// Row item for the delivery route list.
/// </summary>
public sealed partial class RouteRowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _routeUuid = string.Empty;

    [ObservableProperty]
    private string _routeName = string.Empty;

    [ObservableProperty]
    private int _stopCount;
}

/// <summary>
/// Row item for the route stops DataGrid.
/// </summary>
public sealed partial class RouteStopRowViewModel : ObservableObject
{
    [ObservableProperty]
    private int _sequence;

    [ObservableProperty]
    private string _destinationName = string.Empty;

    [ObservableProperty]
    private string _purpose = string.Empty;
}

/// <summary>
/// ViewModel for the Delivery Route document tab.
/// Shows routes with stops DataGrid and CRUD operations.
/// </summary>
public sealed partial class DeliveryRouteViewModel : DocumentViewModel
{
    [ObservableProperty]
    private RouteRowViewModel? _selectedRoute;

    [ObservableProperty]
    private string _editName = string.Empty;

    public DeliveryRouteViewModel()
    {
        Title = "Delivery Routes";

        WeakReferenceMessenger.Default.Register<DeliveryDataChangedMessage>(this, (r, m) =>
        {
            ((DeliveryRouteViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<RouteRowViewModel> Routes { get; } = new ObservableCollection<RouteRowViewModel>();

    public ObservableCollection<RouteStopRowViewModel> RouteStops { get; } = new ObservableCollection<RouteStopRowViewModel>();

    [RelayCommand]
    private void NewRoute()
    {
        var svc = App.Services?.GetService(typeof(DeliveryRouteService)) as DeliveryRouteService;
        svc?.Create(new DeliveryRouteCreateRequest { Name = "New Route" });
    }

    [RelayCommand]
    private void SaveRoute()
    {
        if (SelectedRoute is null || string.IsNullOrEmpty(SelectedRoute.RouteUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(DeliveryRouteService)) as DeliveryRouteService;
        svc?.Update(SelectedRoute.RouteUuid, new DeliveryRouteUpdateRequest { Name = EditName });
    }

    [RelayCommand]
    private void DeleteRoute()
    {
        if (SelectedRoute is null || string.IsNullOrEmpty(SelectedRoute.RouteUuid))
        {
            return;
        }

        var svc = App.Services?.GetService(typeof(DeliveryRouteService)) as DeliveryRouteService;
        svc?.Delete(SelectedRoute.RouteUuid);
    }

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Routes.Clear();
        RouteStops.Clear();
        SelectedRoute = null;
        EditName = string.Empty;
        LoadData();
    }

    partial void OnSelectedRouteChanged(RouteRowViewModel? value)
    {
        EditName = value?.RouteName ?? string.Empty;
        LoadStopsForRoute(value);
    }

    private void LoadData()
    {
        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleData();
            return;
        }

        foreach (var route in dataService.DeliveryRoutes)
        {
            Routes.Add(new RouteRowViewModel
            {
                RouteUuid = route.UUID ?? string.Empty,
                RouteName = route.Name ?? string.Empty,
                StopCount = route.Stops?.Count ?? 0,
            });
        }

        if (Routes.Count == 0)
        {
            LoadSampleData();
        }

        if (Routes.Count > 0)
        {
            SelectedRoute = Routes[0];
        }
    }

    private void LoadStopsForRoute(RouteRowViewModel? route)
    {
        RouteStops.Clear();
        if (route is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        if (dataService is null || !dataService.IsLoaded)
        {
            LoadSampleStops(route.RouteName);
            return;
        }

        var routeModel = dataService.DeliveryRoutes
            .FirstOrDefault(r => r.UUID == route.RouteUuid);
        if (routeModel?.Stops is null)
        {
            LoadSampleStops(route.RouteName);
            return;
        }

        foreach (var stop in routeModel.Stops.OrderBy(s => s.Sequence))
        {
            RouteStops.Add(new RouteStopRowViewModel
            {
                Sequence = stop.Sequence,
                DestinationName = stop.DestinationUUID ?? string.Empty,
                Purpose = stop.Purpose.ToString(),
            });
        }

        if (RouteStops.Count == 0)
        {
            LoadSampleStops(route.RouteName);
        }
    }

    private void LoadSampleData()
    {
        Routes.Add(new RouteRowViewModel { RouteName = "Alpha Circuit", StopCount = 4 });
        Routes.Add(new RouteRowViewModel { RouteName = "Beta Express", StopCount = 3 });
    }

    private void LoadSampleStops(string routeName)
    {
        if (routeName == "Alpha Circuit")
        {
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 1, DestinationName = "Colony Alpha", Purpose = "Cargo" });
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 2, DestinationName = "Station Omega", Purpose = "Refuel" });
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 3, DestinationName = "Colony Beta", Purpose = "Cargo" });
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 4, DestinationName = "Colony Gamma", Purpose = "CargoAndRefuel" });
        }
        else
        {
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 1, DestinationName = "Station Delta", Purpose = "Cargo" });
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 2, DestinationName = "Colony Epsilon", Purpose = "Cargo" });
            RouteStops.Add(new RouteStopRowViewModel { Sequence = 3, DestinationName = "Colony Zeta", Purpose = "CargoAndRefuel" });
        }
    }
}
