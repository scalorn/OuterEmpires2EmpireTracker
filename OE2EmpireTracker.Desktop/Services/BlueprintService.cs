using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for blueprint CRUD operations.
/// </summary>
public sealed class BlueprintService
{
    private readonly DataService _dataService;
    private readonly ILogger<BlueprintService> _logger;

    public BlueprintService(DataService dataService, ILogger<BlueprintService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(BlueprintCreateRequest request)
    {
        var blueprint = new Blueprint(request.Name)
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            NickName = request.NickName,
            Description = request.Description,
            BluePrintType = request.BluePrintType,
            Evolution = request.Evolution,
            TechLevel = request.TechLevel,
            Class = request.Class,
            CopyCost = request.CopyCost,
            BaseBlueprintUUID = request.BaseBlueprintUUID,
        };

        if (request.Properties is not null)
        {
            foreach (var kvp in request.Properties)
            {
                blueprint.Properties.SetProperty(kvp.Key, kvp.Value);
            }
        }

        if (request.Resources is not null)
        {
            blueprint.Resources = new Dictionary<string, string>(request.Resources);
        }

        _dataService.AddBlueprint(blueprint);
        _dataService.OnBlueprintDataChanged(blueprint.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created blueprint {Name} ({UUID})", blueprint.Name, blueprint.UUID);
    }

    public void Update(string uuid, BlueprintUpdateRequest request)
    {
        var blueprint = _dataService.Blueprints.FirstOrDefault(b => b.UUID == uuid);
        if (blueprint is null)
        {
            _logger.LogWarning("Update failed: blueprint {UUID} not found", uuid);
            return;
        }

        blueprint.Name = request.Name;
        blueprint.NickName = request.NickName;
        blueprint.Description = request.Description;
        blueprint.BluePrintType = request.BluePrintType;
        blueprint.Evolution = request.Evolution;
        blueprint.TechLevel = request.TechLevel;
        blueprint.Class = request.Class;
        blueprint.CopyCost = request.CopyCost;
        blueprint.BaseBlueprintUUID = request.BaseBlueprintUUID;

        if (request.Properties is not null)
        {
            blueprint.Properties = new PropertyBag();
            foreach (var kvp in request.Properties)
            {
                blueprint.Properties.SetProperty(kvp.Key, kvp.Value);
            }
        }

        if (request.Resources is not null)
        {
            blueprint.Resources = new Dictionary<string, string>(request.Resources);
        }

        _dataService.IsDirty = true;
        _dataService.OnBlueprintDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated blueprint {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveBlueprint(uuid);
        _dataService.OnBlueprintDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted blueprint {UUID}", uuid);
    }
}
