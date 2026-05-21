using System.Collections.Generic;
using System.Linq;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Provides pagination support for typed collection endpoints.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// The maximum number of items that can be returned in a single page.
    /// Requests exceeding this value are capped without error.
    /// </summary>
    public const int MaxLimit = 500;

    /// <summary>
    /// Applies pagination to a collection of items.
    /// Returns the raw array when no pagination parameters are provided (backward compatible).
    /// Returns a <see cref="PaginatedResponse{T}"/> when limit and/or offset are specified.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="allItems">The full collection of items.</param>
    /// <param name="limit">Optional maximum number of items to return.</param>
    /// <param name="offset">Optional number of items to skip.</param>
    /// <returns>An <see cref="IResult"/> containing either the raw array or a paginated response.</returns>
    public static IResult ApplyPagination<T>(
        IReadOnlyList<T> allItems, int? limit, int? offset)
    {
        if (limit == null && offset == null)
        {
            return Results.Ok(allItems);
        }

        int actualOffset = offset ?? 0;
        int actualLimit = Math.Min(limit ?? MaxLimit, MaxLimit);

        if (actualOffset < 0 || actualLimit < 0)
        {
            return Results.BadRequest(new { error = "Invalid pagination parameters" });
        }

        var page = allItems.Skip(actualOffset).Take(actualLimit).ToList();
        return Results.Ok(new PaginatedResponse<T>
        {
            Items = page,
            Total = allItems.Count,
            Limit = actualLimit,
            Offset = actualOffset,
        });
    }
}
