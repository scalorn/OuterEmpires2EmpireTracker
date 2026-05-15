using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for colony CRUD operations.
/// </summary>
public sealed class ColonyService
{
    private readonly DataService _dataService;
    private readonly ILogger<ColonyService> _logger;

    public ColonyService(DataService dataService, ILogger<ColonyService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(ColonyCreateRequest request)
    {
        var colony = new Colony
        {
            UUID = Guid.NewGuid().ToString(),
            OwnerUUID = _dataService.CurrentPlayerUUID,
            ColonyName = request.ColonyName,
            PlanetName = request.PlanetName,
            SystemName = request.SystemName,
        };

        _dataService.AddColony(colony);
        _dataService.OnColonyDataChanged(colony.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created colony {Name} ({UUID})", colony.ColonyName, colony.UUID);
    }

    public void Update(string uuid, ColonyUpdateRequest request)
    {
        var colony = _dataService.Colonies.FirstOrDefault(c => c.UUID == uuid);
        if (colony is null)
        {
            _logger.LogWarning("Update failed: colony {UUID} not found", uuid);
            return;
        }

        colony.ColonyName = request.ColonyName;
        colony.PlanetName = request.PlanetName;
        colony.SystemName = request.SystemName;
        _dataService.IsDirty = true;
        _dataService.OnColonyDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated colony {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemoveColony(uuid);
        _dataService.OnColonyDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted colony {UUID}", uuid);
    }
}
