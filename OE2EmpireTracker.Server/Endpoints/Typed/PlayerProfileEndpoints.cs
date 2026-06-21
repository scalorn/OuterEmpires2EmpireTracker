// -----------------------------------------------------------------------
// <copyright file="PlayerProfileEndpoints.cs" company="OE2EmpireTracker">
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
/// Typed CRUD endpoint handler for PlayerProfile entities.
/// </summary>
public class PlayerProfileEndpoints : TypedEndpointBase<PlayerProfile, PlayerProfileCreateRequest, PlayerProfileUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "PlayerProfile";

    /// <inheritdoc/>
    protected override string RoutePrefix => "profiles";

    /// <summary>
    /// Handles POST /import requests to import a full PlayerProfile object.
    /// If a profile with the same UUID exists, merges and returns 200.
    /// If no matching profile exists, inserts and returns 201 with Location header.
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

        PlayerProfile? imported;
        try
        {
            imported = await ctx.Request.ReadFromJsonAsync<PlayerProfile>();
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
    protected override string? ValidateCreate(PlayerProfileCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(PlayerProfileUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override PlayerProfile ApplyCreate(PlayerProfileCreateRequest dto)
    {
        return new PlayerProfile
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Faction = dto.Faction ?? string.Empty,
            TotalCredits = dto.TotalCredits,
            SkillPoints = dto.SkillPoints,
            CitizenId = dto.CitizenId ?? string.Empty,
            RegistrationDate = dto.RegistrationDate ?? string.Empty,
            ActiveTime = dto.ActiveTime ?? string.Empty,
            Public = new PlayerRank
            {
                Rank = dto.PublicRank,
                CurrentXp = dto.PublicCurrentXp,
                XpToNextLevel = dto.PublicXpToNextLevel,
                RankName = dto.PublicRankName ?? string.Empty,
            },
            Private = new PlayerRank
            {
                Rank = dto.PrivateRank,
                CurrentXp = dto.PrivateCurrentXp,
                XpToNextLevel = dto.PrivateXpToNextLevel,
                RankName = dto.PrivateRankName ?? string.Empty,
            },
            Military = new PlayerRank
            {
                Rank = dto.MilitaryRank,
                CurrentXp = dto.MilitaryCurrentXp,
                XpToNextLevel = dto.MilitaryXpToNextLevel,
                RankName = dto.MilitaryRankName ?? string.Empty,
            },
            Skills = dto.Skills != null
                ? ConvertSkills(dto.Skills)
                : new Dictionary<string, PlayerSkill>(),
        };
    }

    /// <inheritdoc/>
    protected override PlayerProfile ApplyUpdate(PlayerProfile existing, PlayerProfileUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.Faction != null)
        {
            existing.Faction = dto.Faction;
        }

        existing.TotalCredits = dto.TotalCredits;
        existing.SkillPoints = dto.SkillPoints;

        if (dto.CitizenId != null)
        {
            existing.CitizenId = dto.CitizenId;
        }

        if (dto.RegistrationDate != null)
        {
            existing.RegistrationDate = dto.RegistrationDate;
        }

        if (dto.ActiveTime != null)
        {
            existing.ActiveTime = dto.ActiveTime;
        }

        existing.Public = new PlayerRank
        {
            Rank = dto.PublicRank,
            CurrentXp = dto.PublicCurrentXp,
            XpToNextLevel = dto.PublicXpToNextLevel,
            RankName = dto.PublicRankName ?? existing.Public.RankName,
        };

        existing.Private = new PlayerRank
        {
            Rank = dto.PrivateRank,
            CurrentXp = dto.PrivateCurrentXp,
            XpToNextLevel = dto.PrivateXpToNextLevel,
            RankName = dto.PrivateRankName ?? existing.Private.RankName,
        };

        existing.Military = new PlayerRank
        {
            Rank = dto.MilitaryRank,
            CurrentXp = dto.MilitaryCurrentXp,
            XpToNextLevel = dto.MilitaryXpToNextLevel,
            RankName = dto.MilitaryRankName ?? existing.Military.RankName,
        };

        if (dto.Skills != null)
        {
            existing.Skills = ConvertSkills(dto.Skills);
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<PlayerProfile>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllPlayerProfilesAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<PlayerProfile?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetPlayerProfileAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, PlayerProfile entity, IStorageBackend storage)
        => storage.UpsertPlayerProfileAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeletePlayerProfileAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(PlayerProfile entity) => entity.UUID;

    private static Dictionary<string, PlayerSkill> ConvertSkills(Dictionary<string, SkillUpdateData> skills)
    {
        var result = new Dictionary<string, PlayerSkill>();
        foreach (var kvp in skills)
        {
            result[kvp.Key] = new PlayerSkill
            {
                Level = kvp.Value.Level,
                TrainingStarted = kvp.Value.TrainingStarted,
                CompletionTime = new CountDownTime
                {
                    StartTime = kvp.Value.CompletionStartTime,
                    EndTime = kvp.Value.CompletionEndTime,
                },
            };
        }

        return result;
    }
}

/// <summary>
/// Extension methods for registering PlayerProfile endpoints.
/// </summary>
public static class PlayerProfileEndpointsExtensions
{
    /// <summary>
    /// Maps the PlayerProfile CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapPlayerProfileEndpoints(this WebApplication app)
    {
        var endpoints = new PlayerProfileEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/profiles")
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