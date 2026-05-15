using System;
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
    private string _routeName = string.Empty;

    [ObservableProperty]
    private int _stopCount;

    /// <summary>Gets or sets the UUID of the underlying DeliveryRoute.</summary>
    public string RouteUuid { get; set; } = string.Empty;
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
    private RouteStopRowViewModel? _selectedStop;

    public DeliveryRouteViewModel()
    {
        Title = "Delivery Routes";

        WeakReferenceMessenger.Default.Register<DeliveryDataChangedMessage>(this, (r, _) =>
        {
            ((DeliveryRouteViewModel)r).RefreshData();
        });

        LoadData();
    }

    public ObservableCollection<RouteRowViewModel> Routes { get; } = new ObservableCollection<RouteRowViewModel>();

    public ObservableCollection<RouteStopRowViewModel> RouteStops { get; } = new ObservableCollection<RouteStopRowViewModel>();

    /// <inheritdoc/>
    protected override void RefreshData()
    {
        Routes.Clear();
        RouteStops.Clear();
        SelectedRoute = null;
        SelectedStop = null;
        LoadData();
    }

    /// <summary>Adds a new stop to the selected route.</summary>
    [RelayCommand]
    private void AddStop()
    {
        if (SelectedRoute is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var routeService = App.Services?.GetService(typeof(DeliveryRouteService)) as DeliveryRouteService;
        if (dataService is null || !dataService.IsLoaded || routeService is null)
        {
            return;
        }

        var route = dataService.DeliveryRoutes
            .FirstOrDefault(r => r.UUID == SelectedRoute.RouteUuid);
        if (route is null)
        {
            return;
        }

        int nextSequence = (route.Stops?.Count ?? 0) + 1;
        route.Stops ??= new System.Collections.Generic.List<RouteStop>();
        route.Stops.Add(new RouteStop
        {
            Sequence = nextSequence,
            DestinationUUID = string.Empty,
            Purpose = RouteStopPurpose.Cargo,
        });

        routeService.Update(route.UUID, new DeliveryRouteUpdateRequest
        {
            Name = route.Name,
            Stops = route.Stops,
        });
    }

    /// <summary>Removes the selected stop from the route.</summary>
    [RelayCommand]
    private void RemoveStop()
    {
        if (SelectedRoute is null || SelectedStop is null)
        {
            return;
        }

        var dataService = App.Services?.GetService(typeof(DataService)) as DataService;
        var routeService = App.Services?.GetService(typeof(DeliveryRouteService)) as DeliveryRouteService;
        if (dataService is null || !dataService.IsLoaded || routeService is null)
        {
            return;
        }

        var route = dataService.DeliveryRoutes
            .FirstOrDefault(r => r.UUID == SelectedRoute.RouteUuid);
        if (route?.Stops is null)
        {
            return;
        }

        int seq = SelectedStop.Sequence;
        route.Stops.RemoveAll(s => s.Sequence == seq);

        // Re-sequence remaining stops
        int i = 1;
        foreach (var stop in route.Stops.OrderBy(s => s.Sequence))
        {
            stop.Sequence = i++;
        }

        routeService.Update(route.UUID, new DeliveryRouteUpdateRequest
        {
            Name = route.Name,
            Stops = route.Stops,
        });
    }

    partial void OnSelectedRouteChanged(RouteRowViewModel? value)
    {
        SelectedStop = null;
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
