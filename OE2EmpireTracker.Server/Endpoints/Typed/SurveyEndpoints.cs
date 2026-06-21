// -----------------------------------------------------------------------
// <copyright file="SurveyEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Text.Json;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Push;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for Survey entities.
/// </summary>
public class SurveyEndpoints : TypedEndpointBase<Survey, SurveyCreateRequest, SurveyUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "Survey";

    /// <inheritdoc/>
    protected override string RoutePrefix => "surveys";

    /// <summary>
    /// Handles POST /import requests to import a full Survey object.
    /// If a survey with the same UUID exists, merges and returns 200.
    /// If no matching survey exists, inserts and returns 201 with Location header.
    /// Performs asteroid auto-linking: if the survey has an AsteroidUUID,
    /// verifies the asteroid exists in storage.
    /// </summary>
    /// <param name="uuid">The character UUID from the URL path.</param>
    /// <param name="ctx">The current HTTP context.</param>
    /// <param name="storage">The storage backend.</param>
    /// <returns>An <see cref="IResult"/> containing the imported entity or an error response.</returns>
    public async Task<IResult> HandleImport(string uuid, HttpContext ctx, IStorageBackend storage)
    {
        if (string.IsNullOrWhiteSpace(uuid))
        {
            return Results.BadRequest(new { error = "Invalid character UUID" });
        }

        if (!CanAccessCharacterData(ctx, uuid))
        {
            return Results.Json(new { error = "Access denied" }, statusCode: 403);
        }

        if (!HasJsonContentType(ctx))
        {
            return Results.Json(new { error = "Unsupported media type" }, statusCode: 415);
        }

        Survey? imported;
        try
        {
            imported = await ctx.Request.ReadFromJsonAsync<Survey>();
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { error = "Invalid request body" });
        }

        if (imported == null)
        {
            return Results.BadRequest(new { error = "Request body is required" });
        }

        if (string.IsNullOrWhiteSpace(imported.UUID))
        {
            imported.UUID = Guid.NewGuid().ToString();
        }

        // Asteroid auto-linking: verify the asteroid exists if AsteroidUUID is specified
        if (!string.IsNullOrWhiteSpace(imported.AsteroidUUID))
        {
            var asteroid = await storage.GetAsteroidAsync(uuid, imported.AsteroidUUID);
            if (asteroid == null)
            {
                // Clear the invalid asteroid link rather than rejecting the import
                imported.AsteroidUUID = string.Empty;
            }
        }

        var existing = await GetFromStorage(uuid, imported.UUID, storage);
        bool isNew = existing == null;

        await UpsertToStorage(uuid, imported, storage);

        string action = isNew ? "Created" : "Updated";
        try
        {
            LogMutation(ctx, action, imported.UUID);
        }
        catch (Exception)
        {
            if (isNew)
            {
                await DeleteFromStorage(uuid, imported.UUID, storage);
            }
            else
            {
                await UpsertToStorage(uuid, existing!, storage);
            }

            return Results.Json(new { error = "Internal server error" }, statusCode: 500);
        }

        var eventType = isNew ? ServerEventType.Created : ServerEventType.Updated;
        try
        {
            await DispatchEvent(ctx, eventType, imported.UUID, uuid);
        }
        catch (Exception)
        {
            if (isNew)
            {
                await DeleteFromStorage(uuid, imported.UUID, storage);
            }
            else
            {
                await UpsertToStorage(uuid, existing!, storage);
            }

            return Results.Json(new { error = "Event system temporarily unavailable" }, statusCode: 503);
        }

        if (isNew)
        {
            var location = $"/api/v1/characters/{uuid}/{RoutePrefix}/{imported.UUID}";
            return Results.Created(location, imported);
        }

        return Results.Ok(imported);
    }

    /// <inheritdoc/>
    protected override string? ValidateCreate(SurveyCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlanetName))
        {
            return "PlanetName is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(SurveyUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override Survey ApplyCreate(SurveyCreateRequest dto)
    {
        return new Survey
        {
            UUID = Guid.NewGuid().ToString(),
            PlanetName = dto.PlanetName,
            SystemName = dto.SystemName ?? string.Empty,
            SurveyID = dto.SurveyID ?? string.Empty,
            NickName = dto.NickName ?? string.Empty,
            ScannedBy = dto.ScannedBy ?? string.Empty,
            DateTime = dto.DateTime ?? string.Empty,
            ScannerBlueprintUUID = dto.ScannerBlueprintUUID ?? string.Empty,
            AsteroidUUID = dto.AsteroidUUID ?? string.Empty,
            SurveyType = dto.SurveyType,
            Resources = dto.Resources ?? new Dictionary<string, SurveyResource>(),
            Properties = dto.Properties ?? new Dictionary<string, string>(),
        };
    }

    /// <inheritdoc/>
    protected override Survey ApplyUpdate(Survey existing, SurveyUpdateRequest dto)
    {
        if (dto.PlanetName != null)
        {
            existing.PlanetName = dto.PlanetName;
        }

        if (dto.SystemName != null)
        {
            existing.SystemName = dto.SystemName;
        }

        if (dto.SurveyID != null)
        {
            existing.SurveyID = dto.SurveyID;
        }

        if (dto.NickName != null)
        {
            existing.NickName = dto.NickName;
        }

        if (dto.ScannedBy != null)
        {
            existing.ScannedBy = dto.ScannedBy;
        }

        if (dto.DateTime != null)
        {
            existing.DateTime = dto.DateTime;
        }

        if (dto.ScannerBlueprintUUID != null)
        {
            existing.ScannerBlueprintUUID = dto.ScannerBlueprintUUID;
        }

        if (dto.AsteroidUUID != null)
        {
            existing.AsteroidUUID = dto.AsteroidUUID;
        }

        existing.SurveyType = dto.SurveyType;

        if (dto.Resources != null)
        {
            existing.Resources = dto.Resources;
        }

        if (dto.Properties != null)
        {
            existing.Properties = dto.Properties;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<Survey>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllSurveysAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<Survey?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetSurveyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, Survey entity, IStorageBackend storage)
        => storage.UpsertSurveyAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteSurveyAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(Survey entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering Survey endpoints.
/// </summary>
public static class SurveyEndpointsExtensions
{
    /// <summary>
    /// Maps the Survey CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapSurveyEndpoints(this WebApplication app)
    {
        var endpoints = new SurveyEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/surveys")
            .RequireAuthorization("Authenticated");

        group.MapGet("/", (string uuid, int? limit, int? offset, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetAll(uuid, limit, offset, ctx, storage));
        group.MapGet("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleGetOne(uuid, entityUuid, ctx, storage));
        group.MapPost("/", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleCreate(uuid, ctx, storage));
        group.MapPut("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleUpdate(uuid, entityUuid, ctx, storage));
        group.MapDelete("/{entityUuid}", (string uuid, string entityUuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleDelete(uuid, entityUuid, ctx, storage));
        group.MapPost("/import", (string uuid, HttpContext ctx, IStorageBackend storage)
            => endpoints.HandleImport(uuid, ctx, storage));
    }
}