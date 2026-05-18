using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for ship template CRUD operations.
/// </summary>
public sealed class ShipTemplateService
{
    private readonly DataService _dataService;
    private readonly ILogger<ShipTemplateService> _logger;

    public ShipTemplateService(DataService dataService, ILogger<ShipTemplateService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(ShipTemplateCreateRequest request)
    {
        var template = new ShipTemplate
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
            HullBlueprintUUID = request.HullBlueprintUUID,
            Components = request.Components ?? new List<ShipComponentSlot>(),
        };

        _dataService.AddShipTemplate(template);
        _dataService.OnShipTemplateDataChanged(template.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created ship template {Name} ({UUID})", template.Name, template.UUID);
    }

    public void Update(string uuid, ShipTemplateUpdateRequest request)
    {
        var template = _dataService.ShipTemplates.FirstOrDefault(t => t.UUID == uuid);
        if (template is null)
        {
            _logger.LogWarning("Update failed: ship template {UUID} not found", uuid);
            return;
        }

        template.Name = request.Name;
        template.HullBlueprintUUID = request.HullBlueprintUUID;
        template.Components = request.Components ?? new List<ShipComponentSlot>();
        _dataService.IsDirty = true;
        _dataService.OnShipTemplateDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated ship template {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveShipTemplate(uuid);
        _dataService.OnShipTemplateDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted ship template {UUID}", uuid);
    }
}
