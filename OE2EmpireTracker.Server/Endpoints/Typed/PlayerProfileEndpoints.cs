// -----------------------------------------------------------------------
// <copyright file="PlayerProfileEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Server.Storage;

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
                CurrentXP = dto.PublicCurrentXP,
                NextXP = dto.PublicNextXP,
                Title = dto.PublicTitle ?? string.Empty,
            },
            Private = new PlayerRank
            {
                Rank = dto.PrivateRank,
                CurrentXP = dto.PrivateCurrentXP,
                NextXP = dto.PrivateNextXP,
                Title = dto.PrivateTitle ?? string.Empty,
            },
            Military = new PlayerRank
            {
                Rank = dto.MilitaryRank,
                CurrentXP = dto.MilitaryCurrentXP,
                NextXP = dto.MilitaryNextXP,
                Title = dto.MilitaryTitle ?? string.Empty,
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
            CurrentXP = dto.PublicCurrentXP,
            NextXP = dto.PublicNextXP,
            Title = dto.PublicTitle ?? existing.Public.Title,
        };

        existing.Private = new PlayerRank
        {
            Rank = dto.PrivateRank,
            CurrentXP = dto.PrivateCurrentXP,
            NextXP = dto.PrivateNextXP,
            Title = dto.PrivateTitle ?? existing.Private.Title,
        };

        existing.Military = new PlayerRank
        {
            Rank = dto.MilitaryRank,
            CurrentXP = dto.MilitaryCurrentXP,
            NextXP = dto.MilitaryNextXP,
            Title = dto.MilitaryTitle ?? existing.Military.Title,
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
    }
}
