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

    private static Task<IResult> GetPublicBlueprints(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Placeholder: return empty paginated result until sharing system is wired
        var result = new PaginatedResult(
            items: Array.Empty<object>(),
            page: page,
            pageSize: pageSize,
            totalCount: 0);

        return Task.FromResult(Results.Ok(result));
    }

    private static Task<IResult> GetPublicSurveys(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = new PaginatedResult(
            items: Array.Empty<object>(),
            page: page,
            pageSize: pageSize,
            totalCount: 0);

        return Task.FromResult(Results.Ok(result));
    }

    private static Task<IResult> GetPublicColonies(
        HttpContext httpContext,
        IStorageBackend storage,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = new PaginatedResult(
            items: Array.Empty<object>(),
            page: page,
            pageSize: pageSize,
            totalCount: 0);

        return Task.FromResult(Results.Ok(result));
    }

    private record PaginatedResult(
        object[] items,
        int page,
        int pageSize,
        int totalCount);
}
