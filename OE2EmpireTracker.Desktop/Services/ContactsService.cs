using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for faction and external character CRUD operations.
/// </summary>
public sealed class ContactsService
{
    private readonly DataService _dataService;
    private readonly ILogger<ContactsService> _logger;

    public ContactsService(DataService dataService, ILogger<ContactsService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void CreateFaction(FactionCreateRequest request)
    {
        var faction = new Faction
        {
            UUID = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
        };

        _dataService.AddFaction(faction);
        _dataService.OnContactDataChanged(faction.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created faction {Name} ({UUID})", faction.Name, faction.UUID);
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

    public void DeleteFaction(string uuid)
    {
        _dataService.RemoveFaction(uuid);
        _dataService.OnContactDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted faction {UUID}", uuid);
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
}
