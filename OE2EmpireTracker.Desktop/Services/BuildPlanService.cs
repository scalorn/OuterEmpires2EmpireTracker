using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for build plan CRUD operations.
/// </summary>
public sealed class BuildPlanService
{
    private readonly DataService _dataService;
    private readonly ILogger<BuildPlanService> _logger;

    public BuildPlanService(DataService dataService, ILogger<BuildPlanService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(BuildPlanCreateRequest request)
    {
        var plan = new BuildPlan
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive,
            Items = request.Items ?? new List<BuildItem>(),
        };

        _dataService.AddBuildPlan(plan);
        _dataService.OnBuildPlanDataChanged(plan.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created build plan {Name} ({UUID})", plan.Name, plan.UUID);
    }

    public void Update(string uuid, BuildPlanUpdateRequest request)
    {
        var plan = _dataService.BuildPlans.FirstOrDefault(p => p.UUID == uuid);
        if (plan is null)
        {
            _logger.LogWarning("Update failed: build plan {UUID} not found", uuid);
            return;
        }

        plan.Name = request.Name;
        plan.Description = request.Description;
        plan.IsActive = request.IsActive;
        plan.Items = request.Items ?? new List<BuildItem>();
        _dataService.IsDirty = true;
        _dataService.OnBuildPlanDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated build plan {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveBuildPlan(uuid);
        _dataService.OnBuildPlanDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted build plan {UUID}", uuid);
    }
}
