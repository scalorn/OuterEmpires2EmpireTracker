using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for faction and external character CRUD operations.
/// </summary>
public sealed class ContactsService
{
    private readonly DataService _dataService;
    private readonly ReferenceCountService _referenceCountService;
    private readonly ILogger<ContactsService> _logger;

    public ContactsService(DataService dataService, ReferenceCountService referenceCountService, ILogger<ContactsService> logger)
    {
        _dataService = dataService;
        _referenceCountService = referenceCountService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a faction with a deterministic UUID derived from the name.
    /// </summary>
    public void CreateFaction(FactionCreateRequest request)
    {
        string uuid = GenerateDeterministicUuid(request.Name);

        var faction = new Faction
        {
            UUID = uuid,
            Name = request.Name,
            Description = request.Description,
        };

        _dataService.AddFaction(faction);
        _dataService.OnContactDataChanged(faction.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created faction {Name} ({UUID})", faction.Name, faction.UUID);
    }

    /// <summary>
    /// Attempts to delete a faction. Returns an error message if references exist.
    /// </summary>
    /// <returns>Null on success, or an error message if deletion is blocked.</returns>
    public string? DeleteFaction(string uuid)
    {
        int refs = _referenceCountService.GetFactionReferenceCount(uuid);
        if (refs > 0)
        {
            string msg = $"Cannot delete: {refs} character(s) still reference this faction";
            _logger.LogWarning("Delete blocked for faction {UUID}: {Count} references", uuid, refs);
            return msg;
        }

        _dataService.RemoveFaction(uuid);
        _dataService.OnContactDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted faction {UUID}", uuid);
        return null;
    }

    public void UpdateFaction(string uuid, FactionUpdateRequest request)
    {
        var faction = _dataService.Factions.FirstOrDefault(f => f.UUID == uuid);
        if (faction is null)
        {
            _logger.LogWarning("Update failed: faction {UUID} not found", uuid);
            return;
        }

        faction.Name = request.Name;
        faction.Description = request.Description;
        _dataService.IsDirty = true;
        _dataService.OnContactDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated faction {UUID}", uuid);
    }

    public void CreateCharacter(ExternalCharacterCreateRequest request)
    {
        var character = new ExternalCharacter
        {
            UUID = Guid.NewGuid().ToString(),
            Name = request.Name,
            FactionUUID = request.FactionUUID,
        };

        _dataService.AddExternalCharacter(character);
        _dataService.OnContactDataChanged(character.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created external character {Name} ({UUID})", character.Name, character.UUID);
    }

    public void UpdateCharacter(string uuid, ExternalCharacterUpdateRequest request)
    {
        var character = _dataService.ExternalCharacters.FirstOrDefault(c => c.UUID == uuid);
        if (character is null)
        {
            _logger.LogWarning("Update failed: external character {UUID} not found", uuid);
            return;
        }

        character.Name = request.Name;
        character.FactionUUID = request.FactionUUID;
        _dataService.IsDirty = true;
        _dataService.OnContactDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated external character {UUID}", uuid);
    }

    public void DeleteCharacter(string uuid)
    {
        _dataService.RemoveExternalCharacter(uuid);
        _dataService.OnContactDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted external character {UUID}", uuid);
    }

    /// <summary>
    /// Generates a deterministic UUID from a faction name using MD5 hash.
    /// </summary>
    private static string GenerateDeterministicUuid(string name)
    {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes("faction:" + name));
        return new Guid(hash).ToString();
    }
}
