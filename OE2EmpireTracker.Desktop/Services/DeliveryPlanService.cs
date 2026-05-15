using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for delivery plan CRUD operations.
/// </summary>
public sealed class DeliveryPlanService
{
    private readonly DataService _dataService;
    private readonly ILogger<DeliveryPlanService> _logger;

    public DeliveryPlanService(DataService dataService, ILogger<DeliveryPlanService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(string name, string routeUuid, string shipUuid)
    {
        var plan = new DeliveryPlan
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = name,
            RouteUUID = routeUuid,
            ShipUUID = shipUuid,
            Stops = new List<DeliveryPlanStop>(),
        };

        _dataService.AddDeliveryPlan(plan);
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Created delivery plan {Name} ({UUID})", plan.Name, plan.UUID);
    }

    public void Update(string uuid, DeliveryPlanUpdateRequest request)
    {
        var plan = _dataService.DeliveryPlans.FirstOrDefault(p => p.UUID == uuid);
        if (plan is null)
        {
            _logger.LogWarning("Update failed: delivery plan {UUID} not found", uuid);
            return;
        }

        plan.Name = request.Name;
        plan.Stops = request.Stops ?? new List<DeliveryPlanStop>();
        _dataService.IsDirty = true;
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Updated delivery plan {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveDeliveryPlan(uuid);
        _dataService.OnDeliveryDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Deleted delivery plan {UUID}", uuid);
    }
}
