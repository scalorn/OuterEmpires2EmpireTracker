using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for ship CRUD operations.
/// </summary>
public sealed class ShipService
{
    private readonly DataService _dataService;
    private readonly ILogger<ShipService> _logger;

    public ShipService(DataService dataService, ILogger<ShipService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(ShipCreateRequest request)
    {
        var ship = new Ship
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
        };

        _dataService.AddShip(ship);
        _dataService.OnShipDataChanged(ship.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created ship {Name} ({UUID})", ship.Name, ship.UUID);
    }

    public void Update(string uuid, ShipUpdateRequest request)
    {
        var ship = _dataService.Ships.FirstOrDefault(s => s.UUID == uuid);
        if (ship is null)
        {
            _logger.LogWarning("Update failed: ship {UUID} not found", uuid);
            return;
        }

        ship.Name = request.Name;
        ship.TemplateUUID = request.TemplateUUID;
        ship.HullBlueprintUUID = request.HullBlueprintUUID;
        ship.LocationType = request.LocationType;
        ship.LocationUUID = request.LocationUUID;
        ship.HullCurrentHP = request.HullCurrentHP;
        ship.HullMaxHP = request.HullMaxHP;
        ship.HullMaxRepairPercent = request.HullMaxRepairPercent;
        ship.Components = request.Components ?? new List<ShipComponentSlot>();

        if (request.Cargo is not null)
        {
            ship.Cargo = request.Cargo;
        }

        if (request.Hopper is not null)
        {
            ship.Hopper = request.Hopper;
        }

        _dataService.IsDirty = true;
        _dataService.OnShipDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated ship {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveShip(uuid);
        _dataService.OnShipDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted ship {UUID}", uuid);
    }
}
