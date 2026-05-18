using System;
using System.Linq;
using OE2EmpireTracker.Interfaces;
using OE2EmpireTracker.Models;

#nullable disable

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Implements IColonyProcessingContext for the Desktop project.
/// Provides data lookups needed by Colony.ProcessColony().
/// </summary>
public sealed class ColonyProcessingContext : IColonyProcessingContext
{
    private readonly DataService _dataService;

    public ColonyProcessingContext(DataService dataService)
    {
        _dataService = dataService;
    }

    public ReadOnlyBlueprint FindBlueprint(string uuid)
    {
        var bp = _dataService.Blueprints.FirstOrDefault(b => b.UUID == uuid);
        return bp is not null ? new ReadOnlyBlueprint(bp) : null;
    }

    public Survey FindSurvey(string uuid)
    {
        return _dataService.Surveys.FirstOrDefault(s => s.UUID == uuid);
    }

    public PlayerProfile FindPlayerProfile(string ownerUUID)
    {
        return _dataService.PlayerProfiles.FirstOrDefault(p => p.UUID == ownerUUID);
    }

    public BlueprintType FindBlueprintType(string typeName)
    {
        return _dataService.BlueprintTypes.FirstOrDefault(bt => bt.Name == typeName);
    }

    public void AddBlueprint(Blueprint blueprint)
    {
        // For research evolution — add new blueprint to the data.
        // Full implementation deferred to service layer (A5.2).
        _ = blueprint;
    }
}
