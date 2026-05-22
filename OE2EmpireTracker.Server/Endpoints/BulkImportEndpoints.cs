// -----------------------------------------------------------------------
// <copyright file="BulkImportEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;

using OE2EmpireTracker.Server.Storage;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Bulk import endpoint for validated character data import.
/// Accepts a full PlayerRoot payload and validates all entities before persisting.
/// </summary>
public static class BulkImportEndpoints
{
    /// <summary>
    /// Registers the bulk import endpoint routes.
    /// </summary>
    /// <param name="app">The web application to register routes on.</param>
    public static void MapBulkImportEndpoints(this WebApplication app)
    {
        app.MapPut("/api/v1/characters/{uuid}/import", HandleImport)
            .RequireAuthorization("Authenticated");
    }

    private static async Task<IResult> HandleImport(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        // Authorization check BEFORE reading request body (Req 6 Criterion 2)
        var callerCharacterUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);
        var isOwner = AuthorizationHelper.IsOwner(httpContext);

        if (callerCharacterUUID != uuid && !isOwner)
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        // Read and deserialize with case-sensitive options (Req 7 Criteria 1-2)
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false };
        string body;
        try
        {
            using var reader = new StreamReader(httpContext.Request.Body);
            body = await reader.ReadToEndAsync();
        }
        catch (Exception)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        PlayerRoot? playerRoot;
        try
        {
            playerRoot = JsonSerializer.Deserialize<PlayerRoot>(body, options);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (playerRoot == null)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        // Validate all entities and collect errors (Req 9 Criteria 1-5)
        var (errors, totalErrors) = BulkImportValidator.ValidateAll(playerRoot, uuid);

        if (errors.Count > 0)
        {
            var response = new BulkImportErrorResponse
            {
                Errors = errors,
                Truncated = totalErrors > 100,
            };

            return Results.Json(response, statusCode: 400);
        }

        // All validation passed — persist all entities (Req 2 Criteria 3-4)
        var imported = new Dictionary<string, int>();

        await PersistCollectionAsync(playerRoot.Colony, "colonies", uuid, storage.UpsertColonyAsync, imported);
        await PersistCollectionAsync(playerRoot.Blueprint, "blueprints", uuid, storage.UpsertBlueprintAsync, imported);
        await PersistCollectionAsync(playerRoot.Survey, "surveys", uuid, storage.UpsertSurveyAsync, imported);
        await PersistCollectionAsync(playerRoot.PlayerProfile, "playerProfiles", uuid, storage.UpsertPlayerProfileAsync, imported);
        await PersistCollectionAsync(playerRoot.DeliveryRoute, "deliveryRoutes", uuid, storage.UpsertDeliveryRouteAsync, imported);
        await PersistCollectionAsync(playerRoot.DeliveryPlan, "deliveryPlans", uuid, storage.UpsertDeliveryPlanAsync, imported);
        await PersistCollectionAsync(playerRoot.Ship, "ships", uuid, storage.UpsertShipAsync, imported);
        await PersistCollectionAsync(playerRoot.ShipTemplate, "shipTemplates", uuid, storage.UpsertShipTemplateAsync, imported);
        await PersistCollectionAsync(playerRoot.MarketListing, "marketListings", uuid, storage.UpsertMarketListingAsync, imported);
        await PersistCollectionAsync(playerRoot.MarketTransaction, "marketTransactions", uuid, storage.UpsertMarketTransactionAsync, imported);
        await PersistCollectionAsync(playerRoot.PricingPlan, "pricingPlans", uuid, storage.UpsertPricingPlanAsync, imported);
        await PersistCollectionAsync(playerRoot.StockPlan, "stockPlans", uuid, storage.UpsertStockPlanAsync, imported);
        await PersistCollectionAsync(playerRoot.StockProfile, "stockProfiles", uuid, storage.UpsertStockProfileAsync, imported);
        await PersistCollectionAsync(playerRoot.BuildPlan, "buildPlans", uuid, storage.UpsertBuildPlanAsync, imported);
        await PersistCollectionAsync(playerRoot.SupplyChain, "supplyChains", uuid, storage.UpsertSupplyChainAsync, imported);
        await PersistCollectionAsync(playerRoot.Asteroid, "asteroids", uuid, storage.UpsertAsteroidAsync, imported);
        await PersistCollectionAsync(playerRoot.Station, "stations", uuid, storage.UpsertStationAsync, imported);
        await PersistCollectionAsync(playerRoot.Faction, "factions", uuid, storage.UpsertFactionForCharacterAsync, imported);
        await PersistCollectionAsync(playerRoot.ExternalCharacter, "externalCharacters", uuid, storage.UpsertExternalCharacterAsync, imported);

        var total = 0;
        foreach (var count in imported.Values)
        {
            total += count;
        }

        var result = new BulkImportResult
        {
            Imported = imported,
            Total = total,
        };

        return Results.Ok(result);
    }

    private static async Task PersistCollectionAsync<T>(
        T[] entities,
        string collectionName,
        string characterUuid,
        Func<string, T, Task> upsertAsync,
        Dictionary<string, int> imported)
    {
        if (entities == null || entities.Length == 0)
        {
            return;
        }

        foreach (var entity in entities)
        {
            await upsertAsync(characterUuid, entity);
        }

        imported[collectionName] = entities.Length;
    }
}

/// <summary>
/// Represents a single validation error from bulk import.
/// </summary>
public class BulkImportError
{
    /// <summary>
    /// Gets or sets the entity type that failed validation.
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UUID of the entity that failed validation, if available.
    /// </summary>
    public string? EntityUUID { get; set; }

    /// <summary>
    /// Gets or sets the field that failed validation, if applicable.
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// Response body returned when bulk import validation fails.
/// </summary>
public class BulkImportErrorResponse
{
    /// <summary>
    /// Gets or sets the list of validation errors.
    /// </summary>
    public List<BulkImportError> Errors { get; set; } = new List<BulkImportError>();

    /// <summary>
    /// Gets or sets a value indicating whether the error list was truncated
    /// because total errors exceeded the maximum of 100.
    /// </summary>
    public bool Truncated { get; set; }
}

/// <summary>
/// Response body returned when bulk import succeeds.
/// Contains counts of imported entities per collection and the total.
/// </summary>
public class BulkImportResult
{
    /// <summary>
    /// Gets or sets the dictionary of collection names to imported entity counts.
    /// </summary>
    public Dictionary<string, int> Imported { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets or sets the total number of entities imported across all collections.
    /// </summary>
    public int Total { get; set; }
}
