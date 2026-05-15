using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for stock plan CRUD operations.
/// </summary>
public sealed class StockTargetService
{
    private readonly DataService _dataService;
    private readonly ILogger<StockTargetService> _logger;

    public StockTargetService(DataService dataService, ILogger<StockTargetService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(StockPlanCreateRequest request)
    {
        var plan = new StockPlan
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            IsActive = request.IsActive,
            ReplenishmentBuildPlanUUID = request.ReplenishmentBuildPlanUUID,
            Targets = request.Targets ?? new List<StockTarget>(),
        };

        _dataService.AddStockPlan(plan);
        _dataService.OnStockDataChanged(plan.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created stock plan {Name} ({UUID})", plan.Name, plan.UUID);
    }

    public void Update(string uuid, StockPlanUpdateRequest request)
    {
        var plan = _dataService.StockPlans.FirstOrDefault(p => p.UUID == uuid);
        if (plan is null)
        {
            _logger.LogWarning("Update failed: stock plan {UUID} not found", uuid);
            return;
        }

        plan.Name = request.Name;
        plan.IsActive = request.IsActive;
        plan.ReplenishmentBuildPlanUUID = request.ReplenishmentBuildPlanUUID;
        plan.Targets = request.Targets ?? new List<StockTarget>();
        _dataService.IsDirty = true;
        _dataService.OnStockDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated stock plan {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveStockPlan(uuid);
        _dataService.OnStockDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted stock plan {UUID}", uuid);
    }
}
