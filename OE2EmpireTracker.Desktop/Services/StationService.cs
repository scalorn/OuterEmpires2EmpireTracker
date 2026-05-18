using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for station CRUD operations.
/// </summary>
public sealed class StationService
{
    private readonly DataService _dataService;
    private readonly ILogger<StationService> _logger;

    public StationService(DataService dataService, ILogger<StationService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(StationCreateRequest request)
    {
        var station = new Station
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            Name = request.Name,
        };

        _dataService.AddStation(station);
        _dataService.OnStationDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Created station {Name} ({UUID})", station.Name, station.UUID);
    }

    public void Update(string uuid, StationUpdateRequest request)
    {
        var station = _dataService.Stations.FirstOrDefault(s => s.UUID == uuid);
        if (station is null)
        {
            _logger.LogWarning("Update failed: station {UUID} not found", uuid);
            return;
        }

        station.Name = request.Name;
        station.StationType = request.StationType;
        station.Ownership = request.Ownership;
        station.StationBlueprintUUID = request.StationBlueprintUUID;
        station.HullCurrentHP = request.HullCurrentHP;
        station.HullMaxHP = request.HullMaxHP;
        station.HullMaxRepairPercent = request.HullMaxRepairPercent;
        station.Components = request.Components ?? new List<ShipComponentSlot>();
        _dataService.IsDirty = true;
        _dataService.OnStationDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Updated station {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveStation(uuid);
        _dataService.OnStationDataChanged();
        _dataService.WriteContext();
        _logger.LogInformation("Deleted station {UUID}", uuid);
    }
}
