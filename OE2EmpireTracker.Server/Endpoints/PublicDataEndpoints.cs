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
        publicGroup.MapGet("/surveys", GetPublicSurveys);
        publicGroup.MapGet("/colonies", GetPublicColonies);
    }

    private static async Task<IResult> GetPublicBlueprints(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        return await GetPublicDataAsync(storage, "Blueprints", page, pageSize);
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

    private record PaginatedResult(
        object[] items,
        int page,
        int pageSize,
        int totalCount);
}
