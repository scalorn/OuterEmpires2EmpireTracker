// -----------------------------------------------------------------------
// <copyright file="ExternalCharacterEndpoints.cs" company="OE2EmpireTracker">
//     Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using OE2EmpireTracker.Common.Interfaces;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Server.Endpoints.Typed;

/// <summary>
/// Typed CRUD endpoint handler for ExternalCharacter entities.
/// </summary>
public class ExternalCharacterEndpoints : TypedEndpointBase<ExternalCharacter, ExternalCharacterCreateRequest, ExternalCharacterUpdateRequest>
{
    /// <inheritdoc/>
    protected override string EntityTypeName => "ExternalCharacter";

    /// <inheritdoc/>
    protected override string RoutePrefix => "contacts";

    /// <inheritdoc/>
    protected override string? ValidateCreate(ExternalCharacterCreateRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "Name is required";
        }

        return null;
    }

    /// <inheritdoc/>
    protected override string? ValidateUpdate(ExternalCharacterUpdateRequest dto) => null;

    /// <inheritdoc/>
    protected override ExternalCharacter ApplyCreate(ExternalCharacterCreateRequest dto)
    {
        return new ExternalCharacter
        {
            UUID = Guid.NewGuid().ToString(),
            Name = dto.Name,
            FactionUUID = dto.FactionUUID ?? string.Empty,
        };
    }

    /// <inheritdoc/>
    protected override ExternalCharacter ApplyUpdate(ExternalCharacter existing, ExternalCharacterUpdateRequest dto)
    {
        if (dto.Name != null)
        {
            existing.Name = dto.Name;
        }

        if (dto.FactionUUID != null)
        {
            existing.FactionUUID = dto.FactionUUID;
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override Task<IReadOnlyList<ExternalCharacter>> GetAllFromStorage(string characterUUID, IStorageBackend storage)
        => storage.GetAllExternalCharactersAsync(characterUUID);

    /// <inheritdoc/>
    protected override Task<ExternalCharacter?> GetFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.GetExternalCharacterAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override Task UpsertToStorage(string characterUUID, ExternalCharacter entity, IStorageBackend storage)
        => storage.UpsertExternalCharacterAsync(characterUUID, entity);

    /// <inheritdoc/>
    protected override Task DeleteFromStorage(string characterUUID, string entityUUID, IStorageBackend storage)
        => storage.DeleteExternalCharacterAsync(characterUUID, entityUUID);

    /// <inheritdoc/>
    protected override string GetEntityUuid(ExternalCharacter entity) => entity.UUID;
}

/// <summary>
/// Extension methods for registering ExternalCharacter endpoints.
/// </summary>
public static class ExternalCharacterEndpointsExtensions
{
    /// <summary>
    /// Maps the ExternalCharacter CRUD endpoints to the application.
    /// </summary>
    /// <param name="app">The web application to register endpoints on.</param>
    public static void MapExternalCharacterEndpoints(this WebApplication app)
    {
        var endpoints = new ExternalCharacterEndpoints();
        var group = app.MapGroup("/api/v1/characters/{uuid}/contacts")
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