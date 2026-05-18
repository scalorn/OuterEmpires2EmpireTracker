using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for pricing plan CRUD operations.
/// </summary>
public sealed class PricingPlanService
{
    private readonly DataService _dataService;
    private readonly ILogger<PricingPlanService> _logger;

    public PricingPlanService(DataService dataService, ILogger<PricingPlanService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(PricingPlanCreateRequest request)
    {
        var plan = new PricingPlan
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            Description = request.Description,
            FixedCostPerItem = request.FixedCostPerItem,
            HourlyCostRate = request.HourlyCostRate,
            ResourcePrices = request.ResourcePrices ?? new Dictionary<string, decimal>(),
        };

        _dataService.AddPricingPlan(plan);
        _dataService.OnPricingDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Created pricing plan {Name} ({UUID})", plan.Name, plan.UUID);
    }

    public void Update(string uuid, PricingPlanUpdateRequest request)
    {
        var plan = _dataService.PricingPlans.FirstOrDefault(p => p.UUID == uuid);
        if (plan is null)
        {
            _logger.LogWarning("Update failed: pricing plan {UUID} not found", uuid);
            return;
        }

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.FixedCostPerItem = request.FixedCostPerItem;
        plan.HourlyCostRate = request.HourlyCostRate;
        plan.ResourcePrices = request.ResourcePrices ?? new Dictionary<string, decimal>();
        _dataService.IsDirty = true;
        _dataService.OnPricingDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Updated pricing plan {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemovePricingPlan(uuid);
        _dataService.OnPricingDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Deleted pricing plan {UUID}", uuid);
    }
}
