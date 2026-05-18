using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for delivery route CRUD operations.
/// </summary>
public sealed class DeliveryRouteService
{
    private readonly DataService _dataService;
    private readonly ILogger<DeliveryRouteService> _logger;

    public DeliveryRouteService(DataService dataService, ILogger<DeliveryRouteService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(DeliveryRouteCreateRequest request)
    {
        var route = new DeliveryRoute
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            Stops = request.Stops ?? new List<RouteStop>(),
        };

        _dataService.AddDeliveryRoute(route);
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Created delivery route {Name} ({UUID})", route.Name, route.UUID);
    }

    public void Update(string uuid, DeliveryRouteUpdateRequest request)
    {
        var route = _dataService.DeliveryRoutes.FirstOrDefault(r => r.UUID == uuid);
        if (route is null)
        {
            _logger.LogWarning("Update failed: delivery route {UUID} not found", uuid);
            return;
        }

        route.Name = request.Name;
        route.Stops = request.Stops ?? new List<RouteStop>();
        _dataService.IsDirty = true;
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Updated delivery route {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveDeliveryRoute(uuid);
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Deleted delivery route {UUID}", uuid);
    }
}
