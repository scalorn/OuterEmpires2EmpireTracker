// -----------------------------------------------------------------------
// <copyright file="BulkImportValidator.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Validates entities for the bulk import endpoint using the same rules
/// as the individual typed endpoints.
/// </summary>
internal static class BulkImportValidator
{
    private const int MaxErrors = 100;

    /// <summary>
    /// Validates all collections in a deserialized PlayerRoot and collects errors.
    /// </summary>
    /// <param name="playerRoot">The deserialized player root.</param>
    /// <param name="characterUuid">The URL character UUID for ownership validation.</param>
    /// <returns>A tuple of the error list and total error count.</returns>
    public static (List<BulkImportError> Errors, int TotalErrors) ValidateAll(
        OE2EmpireTracker.Services.PlayerRoot playerRoot,
        string characterUuid)
    {
        var errors = new List<BulkImportError>();
        var totalErrors = 0;

        ValidateCollection(playerRoot.PlayerProfile, "PlayerProfile", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Blueprint, "Blueprint", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Survey, "Survey", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Colony, "Colony", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.DeliveryRoute, "DeliveryRoute", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.DeliveryPlan, "DeliveryPlan", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.PricingPlan, "PricingPlan", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.BuildPlan, "BuildPlan", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.ShipTemplate, "ShipTemplate", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Ship, "Ship", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Station, "Station", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.MarketListing, "MarketListing", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.MarketTransaction, "MarketTransaction", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.StockPlan, "StockPlan", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.StockProfile, "StockProfile", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.SupplyChain, "SupplyChain", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.WarehouseOverflowRule, "WarehouseOverflowRule", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Faction, "Faction", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.ExternalCharacter, "ExternalCharacter", characterUuid, errors, ref totalErrors);
        ValidateCollection(playerRoot.Asteroid, "Asteroid", characterUuid, errors, ref totalErrors);

        return (errors, totalErrors);
    }

    private static void ValidateCollection<T>(
        T[] entities,
        string entityType,
        string characterUuid,
        List<BulkImportError> errors,
        ref int totalErrors)
    {
        if (entities == null)
        {
            return;
        }

        for (int i = 0; i < entities.Length; i++)
        {
            var entity = entities[i];
            if (entity == null)
            {
                totalErrors++;
                if (errors.Count < MaxErrors)
                {
                    errors.Add(new BulkImportError
                    {
                        EntityType = entityType,
                        Error = $"Invalid JSON for entity at index {i}",
                    });
                }

                continue;
            }

            var entityErrors = ValidateEntity(entity, entityType, characterUuid);
            foreach (var error in entityErrors)
            {
                totalErrors++;
                if (errors.Count < MaxErrors)
                {
                    errors.Add(error);
                }
            }
        }
    }

    private static List<BulkImportError> ValidateEntity(
        object entity,
        string entityType,
        string characterUuid)
    {
        var errors = new List<BulkImportError>();
        string? entityUuid = null;

        switch (entity)
        {
            case Colony c:
                entityUuid = c.UUID;
                ValidateOwnerUuid(c.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(c.PlanetName))
                {
                    errors.Add(NewError(entityType, entityUuid, "PlanetName", "PlanetName is required"));
                }

                if (string.IsNullOrWhiteSpace(c.ColonyName))
                {
                    errors.Add(NewError(entityType, entityUuid, "ColonyName", "ColonyName is required"));
                }

                break;

            case Blueprint b:
                entityUuid = b.UUID;
                ValidateOwnerUuid(b.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(b.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                if (string.IsNullOrWhiteSpace(b.BluePrintType))
                {
                    errors.Add(NewError(entityType, entityUuid, "BluePrintType", "BluePrintType is required"));
                }

                break;

            case Survey s:
                entityUuid = s.UUID;
                ValidateOwnerUuid(s.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(s.PlanetName))
                {
                    errors.Add(NewError(entityType, entityUuid, "PlanetName", "PlanetName is required"));
                }

                break;

            case PlayerProfile p:
                entityUuid = p.UUID;
                if (string.IsNullOrWhiteSpace(p.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case DeliveryRoute dr:
                entityUuid = dr.UUID;
                ValidateOwnerUuid(dr.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(dr.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case DeliveryPlan dp:
                entityUuid = dp.UUID;
                ValidateOwnerUuid(dp.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(dp.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case PricingPlan pp:
                entityUuid = pp.UUID;
                ValidateOwnerUuid(pp.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(pp.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case BuildPlan bp:
                entityUuid = bp.UUID;
                ValidateOwnerUuid(bp.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(bp.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case ShipTemplate st:
                entityUuid = st.UUID;
                ValidateOwnerUuid(st.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(st.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case Ship ship:
                entityUuid = ship.UUID;
                ValidateOwnerUuid(ship.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(ship.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case Station station:
                entityUuid = station.UUID;
                if (string.IsNullOrWhiteSpace(station.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case MarketListing ml:
                entityUuid = ml.UUID;
                ValidateOwnerUuid(ml.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(ml.ItemName))
                {
                    errors.Add(NewError(entityType, entityUuid, "ItemName", "ItemName is required"));
                }

                if (string.IsNullOrWhiteSpace(ml.StationUUID))
                {
                    errors.Add(NewError(entityType, entityUuid, "StationUUID", "StationUUID is required"));
                }

                break;

            case MarketTransaction mt:
                entityUuid = mt.UUID;
                ValidateOwnerUuid(mt.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                break;

            case StockPlan sp:
                entityUuid = sp.UUID;
                ValidateOwnerUuid(sp.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(sp.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case StockProfile spf:
                entityUuid = spf.UUID;
                ValidateOwnerUuid(spf.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(spf.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case SupplyChain sc:
                entityUuid = sc.UUID;
                ValidateOwnerUuid(sc.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                if (string.IsNullOrWhiteSpace(sc.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case WarehouseOverflowRule wor:
                entityUuid = wor.UUID;
                ValidateOwnerUuid(wor.OwnerUUID, characterUuid, entityType, entityUuid, errors);
                break;

            case Faction f:
                entityUuid = f.UUID;
                if (string.IsNullOrWhiteSpace(f.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case ExternalCharacter ec:
                entityUuid = ec.UUID;
                if (string.IsNullOrWhiteSpace(ec.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;

            case Asteroid a:
                entityUuid = a.UUID;
                if (string.IsNullOrWhiteSpace(a.Name))
                {
                    errors.Add(NewError(entityType, entityUuid, "Name", "Name is required"));
                }

                break;
        }

        return errors;
    }

    private static void ValidateOwnerUuid(
        string ownerUuid,
        string characterUuid,
        string entityType,
        string? entityUuid,
        List<BulkImportError> errors)
    {
        if (!string.IsNullOrEmpty(ownerUuid) && ownerUuid != characterUuid)
        {
            errors.Add(NewError(entityType, entityUuid, "OwnerUUID", "OwnerUUID does not match character UUID"));
        }
    }

    private static BulkImportError NewError(string entityType, string? entityUuid, string? field, string error)
    {
        return new BulkImportError
        {
            EntityType = entityType,
            EntityUUID = entityUuid,
            Field = field,
            Error = error,
        };
    }
}
