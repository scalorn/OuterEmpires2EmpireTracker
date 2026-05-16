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

    /// <summary>
    /// Stages a flatpack structure at the destination colony (H3.5).
    /// Creates a new ColonyStructure with the flatpack blueprint UUID and Staged state.
    /// </summary>
    /// <param name="colonyUuid">The destination colony UUID.</param>
    /// <param name="flatpackBlueprintUuid">The blueprint UUID of the flatpack being delivered.</param>
    public void StageFlatpackStructure(string colonyUuid, string flatpackBlueprintUuid)
    {
        var colony = _dataService.Colonies.FirstOrDefault(c => c.UUID == colonyUuid);
        if (colony is null)
        {
            _logger.LogWarning(
                "StageFlatpackStructure: colony {UUID} not found",
                colonyUuid);
            return;
        }

        var structure = new ColonyStructure
        {
            UUID = Guid.NewGuid().ToString(),
            FlatpackBlueprintUUID = flatpackBlueprintUuid,
        };

        structure.Properties.SetProperty(
            OE2EmpireTracker.Constants.GameConstants.PropStaged,
            true);

        colony.Structures ??= new System.Collections.Generic.List<ColonyStructure>();
        colony.Structures.Add(structure);
        _dataService.IsDirty = true;

        _logger.LogInformation(
            "Staged flatpack {Blueprint} at colony {Colony}",
            flatpackBlueprintUuid,
            colonyUuid);
    }
}
