using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

namespace OE2EmpireTracker.Server.Endpoints;

/// <summary>
/// Public data endpoints that serve publicly shared data without authentication.
/// </summary>
public static class PublicDataEndpoints
{
    public static void MapPublicDataEndpoints(this WebApplication app)
    {
        var publicGroup = app.MapGroup("/api/v1/public")
            .AllowAnonymous();

        publicGroup.MapGet("/blueprints", GetPublicBlueprints);
        publicGroup.MapGet("/blueprints/{uuid}", GetPublicBlueprintDetail);
        publicGroup.MapGet("/surveys", GetPublicSurveys);
        publicGroup.MapGet("/colonies", GetPublicColonies);
        publicGroup.MapGet("/systems", GetPublicSystems);
        publicGroup.MapGet("/systems/{systemId}/planets", GetPublicPlanets);
        publicGroup.MapGet("/systems/{systemId}/asteroids", GetPublicAsteroids);
        publicGroup.MapGet("/systems/{systemId}/colonies", GetPublicColonySummaries);
    }

    private static async Task<IResult> GetPublicBlueprints(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 10000);

        var publicBlueprints = new List<BlueprintSummary>();

        // Include global blueprints (characterUUID="")
        var globalBlueprints = await storage.GetAllBlueprintsAsync(string.Empty);
        publicBlueprints.AddRange(globalBlueprints.Select(ProjectToSummary));

        // Include character-shared blueprints
        var allCharacters = await storage.GetAllCharactersAsync();

        foreach (var character in allCharacters)
        {
            var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
            var hasPublicData = rules.Any(r =>
                r.TargetType == SharingTargetType.Public &&
                (r.DataType == null || r.DataType == "Blueprints" || r.DataType == "All"));

            if (!hasPublicData)
            {
                continue;
            }

            var blueprints = await storage.GetAllBlueprintsAsync(character.UUID);
            publicBlueprints.AddRange(blueprints.Select(ProjectToSummary));
        }

        var totalCount = publicBlueprints.Count;
        var items = publicBlueprints
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Cast<object>()
            .ToArray();

        return Results.Ok(new PaginatedResult(items, page, pageSize, totalCount));
    }

    private static async Task<IResult> GetPublicBlueprintDetail(
        HttpContext httpContext,
        IStorageBackend storage,
        string uuid)
    {
        // Search global blueprints first
        var globalBlueprints = await storage.GetAllBlueprintsAsync(string.Empty);
        var blueprint = globalBlueprints.FirstOrDefault(b =>
            string.Equals(b.UUID, uuid, StringComparison.OrdinalIgnoreCase));

        // If not found in global, search character-shared blueprints
        if (blueprint == null)
        {
            var allCharacters = await storage.GetAllCharactersAsync();
            foreach (var character in allCharacters)
            {
                var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
                var hasPublicData = rules.Any(r =>
                    r.TargetType == SharingTargetType.Public &&
                    (r.DataType == null || r.DataType == "Blueprints" || r.DataType == "All"));

                if (!hasPublicData)
                {
                    continue;
                }

                var blueprints = await storage.GetAllBlueprintsAsync(character.UUID);
                blueprint = blueprints.FirstOrDefault(b =>
                    string.Equals(b.UUID, uuid, StringComparison.OrdinalIgnoreCase));

                if (blueprint != null)
                {
                    break;
                }
            }
        }

        if (blueprint == null)
        {
            return Results.NotFound(new { error = "Blueprint not found" });
        }

        // Return full blueprint detail without private fields
        return Results.Ok(new
        {
            uuid = blueprint.UUID ?? string.Empty,
            name = blueprint.Name ?? string.Empty,
            nickName = blueprint.NickName ?? string.Empty,
            bluePrintType = blueprint.BluePrintType ?? string.Empty,
            techLevel = blueprint.TechLevel ?? string.Empty,
            evolution = blueprint.Evolution,
            shipClass = blueprint.Class,
            description = blueprint.Description ?? string.Empty,
            copyCost = blueprint.CopyCost,
            properties = blueprint.Properties?.Properties ?? new Dictionary<string, string>(),
            resources = blueprint.Resources ?? new Dictionary<string, string>(),
        });
    }

    private static async Task<IResult> GetPublicSurveys(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        return await GetPublicDataAsync(storage, "Surveys", page, pageSize);
    }

    private static async Task<IResult> GetPublicColonies(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        return await GetPublicDataAsync(storage, "Colonies", page, pageSize);
    }

    private static async Task<IResult> GetPublicSystems(
        HttpContext httpContext,
        IStorageBackend storage)
    {
        var systems = await storage.GetAllStarSystemsAsync();
        return Results.Ok(systems);
    }

    private static async Task<IResult> GetPublicPlanets(
        HttpContext httpContext,
        IStorageBackend storage,
        int systemId)
    {
        var json = await storage.GetGlobalDataAsync($"Planets_{systemId}");
        if (json == null)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var planets = Newtonsoft.Json.JsonConvert.DeserializeObject<List<object>>(json)
            ?? new List<object>();
        return Results.Ok(planets);
    }

    private static async Task<IResult> GetPublicAsteroids(
        HttpContext httpContext,
        IStorageBackend storage,
        int systemId)
    {
        // Look up system name from star systems
        var systems = await storage.GetAllStarSystemsAsync();
        var system = systems.FirstOrDefault(s => s.Id == systemId);
        if (system == null)
        {
            return Results.Ok(Array.Empty<AsteroidSummary>());
        }

        var systemName = system.Name;
        var allCharacters = await storage.GetAllCharactersAsync();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var results = new List<AsteroidSummary>();

        foreach (var character in allCharacters)
        {
            var asteroids = await storage.GetAllAsteroidsAsync(character.UUID);
            foreach (var asteroid in asteroids)
            {
                if (string.Equals(asteroid.SystemName, systemName, StringComparison.OrdinalIgnoreCase)
                    && seen.Add(asteroid.UUID))
                {
                    results.Add(new AsteroidSummary
                    {
                        UUID = asteroid.UUID,
                        Name = asteroid.Name ?? string.Empty,
                        SystemId = systemId,
                    });
                }
            }
        }

        return Results.Ok(results);
    }

    private static async Task<IResult> GetPublicColonySummaries(
        HttpContext httpContext,
        IStorageBackend storage,
        int systemId)
    {
        var summaries = await storage.GetColonySummariesForSystemAsync(systemId);
        return Results.Ok(summaries);
    }

    private static async Task<IResult> GetPublicDataAsync(
        IStorageBackend storage,
        string dataType,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var allCharacters = await storage.GetAllCharactersAsync();
        var publicEntities = new List<object>();

        foreach (var character in allCharacters)
        {
            var rules = await storage.GetSharingRulesForCharacterAsync(character.UUID);
            var hasPublicData = rules.Any(r =>
                r.TargetType == SharingTargetType.Public &&
                (r.DataType == null || r.DataType == dataType || r.DataType == "All"));

            if (!hasPublicData)
            {
                continue;
            }

            var entities = await GetEntitiesAsync(storage, character.UUID, dataType);
            publicEntities.AddRange(entities);
        }

        var totalCount = publicEntities.Count;
        var items = publicEntities
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArray();

        return Results.Ok(new PaginatedResult(items, page, pageSize, totalCount));
    }

    private static async Task<IReadOnlyList<object>> GetEntitiesAsync(
        IStorageBackend storage,
        string characterUUID,
        string dataType)
    {
        return dataType switch
        {
            "Blueprints" => (await storage.GetAllBlueprintsAsync(characterUUID))
                .Cast<object>().ToList(),
            "Surveys" => (await storage.GetAllSurveysAsync(characterUUID))
                .Cast<object>().ToList(),
            "Colonies" => (await storage.GetAllColoniesAsync(characterUUID))
                .Cast<object>().ToList(),
            _ => Array.Empty<object>(),
        };
    }

    private static BlueprintSummary ProjectToSummary(Blueprint bp)
    {
        return new BlueprintSummary
        {
            UUID = bp.UUID ?? string.Empty,
            Name = bp.Name ?? string.Empty,
            NickName = bp.NickName ?? string.Empty,
            BluePrintType = bp.BluePrintType ?? string.Empty,
            TechLevel = bp.TechLevel ?? string.Empty,
            Evolution = bp.Evolution,
            Class = bp.Class,
            Description = bp.Description ?? string.Empty,
        };
    }

    private record PaginatedResult(
        object[] items,
        int page,
        int pageSize,
        int totalCount);
}
