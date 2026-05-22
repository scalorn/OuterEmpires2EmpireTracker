using System.Security.Claims;
using System.Text.Json;
using OE2EmpireTracker.Server.Storage;

using OE2EmpireTracker.Services;
namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Global/baseline data, sync, and export endpoints.
/// </summary>
public static class DataEndpoints
{
    public static void MapDataEndpoints(this WebApplication app)
    {
        // Global/baseline data
        var global = app.MapGroup("/api/v1/global")
            .RequireAuthorization("Authenticated");

        global.MapGet("/{dataType}", GetGlobalData);
        global.MapPut("/{dataType}", PutGlobalData);

        // Sync
        app.MapGet("/api/v1/sync", GetSync)
            .RequireAuthorization("Authenticated");

        // Export
        app.MapGet("/api/v1/characters/{uuid}/export", ExportCharacterData)
            .RequireAuthorization("Authenticated");
    }

    // --- Global/Baseline Data ---

    private static async Task<IResult> GetGlobalData(
        string dataType,
        IStorageBackend storage)
    {
        var json = await storage.GetGlobalDataAsync(dataType);
        if (json == null)
        {
            return Results.NotFound(new { error = "Global data type not found" });
        }

        return Results.Content(json, "application/json");
    }

    private static async Task<IResult> PutGlobalData(
        string dataType,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!IsOwner(httpContext))
        {
            return Results.Forbid();
        }

        var body = await ReadBodyAsStringAsync(httpContext);
        if (string.IsNullOrWhiteSpace(body))
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        // Validate JSON
        try
        {
            JsonDocument.Parse(body);
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid JSON body" });
        }

        await storage.UpsertGlobalDataAsync(dataType, body);

        LogMutation(httpContext, "Updated", $"Global/{dataType}", dataType);

        return Results.Ok(JsonDocument.Parse(body).RootElement);
    }

    // --- Sync ---

    private static async Task<IResult> GetSync(
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var allFactions = await storage.GetAllFactionsAsync();
        var allCharacters = await storage.GetAllCharactersAsync();

        if (AuthorizationHelper.IsOwner(httpContext))
        {
            return Results.Ok(new { factions = allFactions, characters = allCharacters, serverTimestamp = SystemClock.UtcNow });
        }

        var callerUUID = AuthorizationHelper.GetCallerCharacterUUID(httpContext);

        // Filter factions to only those caller is a member of
        var accessibleFactions = new List<ServerFaction>();
        foreach (var faction in allFactions)
        {
            if (!string.IsNullOrEmpty(callerUUID) && await AuthorizationHelper.IsFactionMember(callerUUID, faction.UUID, storage))
            {
                accessibleFactions.Add(faction);
            }
        }

        // Filter characters to only those caller has access to
        var accessibleCharacters = new List<ServerCharacter>();
        foreach (var character in allCharacters)
        {
            if (await AuthorizationHelper.CanAccessCharacterData(httpContext, character.UUID, storage))
            {
                accessibleCharacters.Add(character);
            }
        }

        return Results.Ok(new { factions = accessibleFactions, characters = accessibleCharacters, serverTimestamp = SystemClock.UtcNow });
    }

    // --- Export ---

    private static async Task<IResult> ExportCharacterData(
        string uuid,
        HttpContext httpContext,
        IStorageBackend storage)
    {
        if (!CanAccessCharacterData(httpContext, uuid))
        {
            return Results.Forbid();
        }

        var colonies = await storage.GetAllColoniesAsync(uuid);
        var blueprints = await storage.GetAllBlueprintsAsync(uuid);
        var surveys = await storage.GetAllSurveysAsync(uuid);
        var playerProfiles = await storage.GetAllPlayerProfilesAsync(uuid);
        var deliveryRoutes = await storage.GetAllDeliveryRoutesAsync(uuid);
        var deliveryPlans = await storage.GetAllDeliveryPlansAsync(uuid);
        var ships = await storage.GetAllShipsAsync(uuid);
        var shipTemplates = await storage.GetAllShipTemplatesAsync(uuid);
        var marketListings = await storage.GetAllMarketListingsAsync(uuid);
        var marketTransactions = await storage.GetAllMarketTransactionsAsync(uuid);
        var pricingPlans = await storage.GetAllPricingPlansAsync(uuid);
        var stockPlans = await storage.GetAllStockPlansAsync(uuid);
        var stockProfiles = await storage.GetAllStockProfilesAsync(uuid);
        var buildPlans = await storage.GetAllBuildPlansAsync(uuid);
        var supplyChains = await storage.GetAllSupplyChainsAsync(uuid);
        var asteroids = await storage.GetAllAsteroidsAsync(uuid);
        var stations = await storage.GetAllStationsAsync(uuid);
        var factions = await storage.GetAllFactionsForCharacterAsync(uuid);
        var externalCharacters = await storage.GetAllExternalCharactersAsync(uuid);

        var result = new
        {
            Colony = colonies,
            Blueprint = blueprints,
            Survey = surveys,
            PlayerProfile = playerProfiles,
            DeliveryRoute = deliveryRoutes,
            DeliveryPlan = deliveryPlans,
            Ship = ships,
            ShipTemplate = shipTemplates,
            MarketListing = marketListings,
            MarketTransaction = marketTransactions,
            PricingPlan = pricingPlans,
            StockPlan = stockPlans,
            StockProfile = stockProfiles,
            BuildPlan = buildPlans,
            SupplyChain = supplyChains,
            Asteroid = asteroids,
            Station = stations,
            Faction = factions,
            ExternalCharacter = externalCharacters,
        };

        return Results.Ok(result);
    }

    // --- Helpers ---

    private static bool IsOwner(HttpContext httpContext)
    {
        return httpContext.User.IsInRole(TokenRole.Owner.ToString());
    }

    private static bool CanAccessCharacterData(HttpContext httpContext, string characterUuid)
    {
        if (IsOwner(httpContext))
        {
            return true;
        }

        var callerCharUUID = httpContext.User.FindFirstValue("CharacterUUID");
        return callerCharUUID == characterUuid;
    }

    private static async Task<string> ReadBodyAsStringAsync(HttpContext httpContext)
    {
        using var reader = new StreamReader(httpContext.Request.Body);
        return await reader.ReadToEndAsync();
    }

    private static void LogMutation(HttpContext httpContext, string action, string entityType, string uuid)
    {
        var tokenId = httpContext.User.FindFirstValue("TokenId") ?? "unknown";
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var logger = httpContext.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DataEndpoints");

        logger.LogInformation(
            "Mutation: {Action} {EntityType}/{UUID} by token {TokenId} from {IP}",
            action,
            entityType,
            uuid,
            tokenId,
            remoteIp);
    }
}
