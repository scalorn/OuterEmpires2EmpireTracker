// -----------------------------------------------------------------------
// <copyright file="SurveyEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

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
    }
}
