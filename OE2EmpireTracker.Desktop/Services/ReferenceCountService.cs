using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Counts references to entities across the data model.
/// Used to prevent deletion of entities that are still referenced elsewhere.
/// </summary>
public sealed class ReferenceCountService
{
    private readonly DataService _dataService;
    private readonly ILogger<ReferenceCountService> _logger;

    public ReferenceCountService(DataService dataService, ILogger<ReferenceCountService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    /// <summary>
    /// Counts how many times a colony is referenced from delivery routes,
    /// build plans, and overflow rules.
    /// </summary>
    public int GetColonyReferenceCount(string colonyUuid)
    {
        int count = 0;

        // Check delivery route stops
        foreach (var route in _dataService.DeliveryRoutes)
        {
            foreach (var stop in route.Stops ?? Enumerable.Empty<RouteStop>())
            {
                if (stop.DestinationUUID == colonyUuid)
                {
                    count++;
                }
            }
        }

        // Check build plan items
        foreach (var plan in _dataService.BuildPlans)
        {
            foreach (var item in plan.Items ?? Enumerable.Empty<BuildItem>())
            {
                if (item.BuildLocationUUID == colonyUuid)
                {
                    count++;
                }
            }
        }

        // Check overflow rules
        foreach (var rule in _dataService.WarehouseOverflowRules)
        {
            if (rule.ColonyUUID == colonyUuid)
            {
                count++;
            }
        }

        _logger.LogDebug(
            "Colony {UUID} has {Count} references",
            colonyUuid,
            count);

        return count;
    }

    /// <summary>
    /// Counts how many times a blueprint is referenced from colony structures,
    /// build plans, and ship templates.
    /// </summary>
    public int GetBlueprintReferenceCount(string blueprintUuid)
    {
        int count = 0;

        // Check colony structures
        foreach (var colony in _dataService.Colonies)
        {
            if (colony.Structures is null)
            {
                continue;
            }

            foreach (var structure in colony.Structures)
            {
                if (structure.FlatpackBlueprintUUID == blueprintUuid)
                {
                    count++;
                }
            }
        }

        // Check build plan items
        foreach (var plan in _dataService.BuildPlans)
        {
            foreach (var item in plan.Items ?? Enumerable.Empty<BuildItem>())
            {
                if (item.BlueprintUUID == blueprintUuid)
                {
                    count++;
                }
            }
        }

        // Check ship templates (hull blueprint and component blueprints)
        foreach (var template in _dataService.ShipTemplates)
        {
            if (template.HullBlueprintUUID == blueprintUuid)
            {
                count++;
            }

            if (template.Components is not null)
            {
                foreach (var component in template.Components)
                {
                    if (component.BlueprintUUID == blueprintUuid)
                    {
                        count++;
                    }
                }
            }
        }

        _logger.LogDebug(
            "Blueprint {UUID} has {Count} references",
            blueprintUuid,
            count);

        return count;
    }

    /// <summary>
    /// Counts how many times a build plan is referenced from stock plans
    /// (StockPlan.ReplenishmentBuildPlanUUID).
    /// </summary>
    public int GetBuildPlanReferenceCount(string buildPlanUuid)
    {
        int count = 0;

        foreach (var stockPlan in _dataService.StockPlans)
        {
            if (stockPlan.ReplenishmentBuildPlanUUID == buildPlanUuid)
            {
                count++;
            }
        }

        _logger.LogDebug(
            "BuildPlan {UUID} has {Count} references",
            buildPlanUuid,
            count);

        return count;
    }

    /// <summary>
    /// Counts how many times a faction is referenced from player profiles
    /// and external characters.
    /// </summary>
    public int GetFactionReferenceCount(string factionUuid)
    {
        int count = 0;

        foreach (var profile in _dataService.PlayerProfiles)
        {
            if (profile.FactionUUID == factionUuid)
            {
                count++;
            }
        }

        foreach (var character in _dataService.ExternalCharacters)
        {
            if (character.FactionUUID == factionUuid)
            {
                count++;
            }
        }

        _logger.LogDebug(
            "Faction {UUID} has {Count} references",
            factionUuid,
            count);

        return count;
    }

    /// <summary>
    /// Counts how many times a station is referenced from delivery routes
    /// and market listings.
    /// </summary>
    public int GetStationReferenceCount(string stationUuid)
    {
        int count = 0;

        // Check delivery route stops
        foreach (var route in _dataService.DeliveryRoutes)
        {
            foreach (var stop in route.Stops ?? Enumerable.Empty<RouteStop>())
            {
                if (stop.DestinationUUID == stationUuid
                    && stop.DestinationType == DestinationType.Station)
                {
                    count++;
                }
            }
        }

        // Check market listings
        foreach (var listing in _dataService.MarketListings)
        {
            if (listing.StationUUID == stationUuid)
            {
                count++;
            }
        }

        _logger.LogDebug(
            "Station {UUID} has {Count} references",
            stationUuid,
            count);

        return count;
    }
}
