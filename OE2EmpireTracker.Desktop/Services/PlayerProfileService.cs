using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Desktop.Services;

/// <summary>
/// Service for player profile CRUD operations.
/// </summary>
public sealed class PlayerProfileService
{
    private readonly DataService _dataService;
    private readonly ILogger<PlayerProfileService> _logger;

    public PlayerProfileService(DataService dataService, ILogger<PlayerProfileService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public void Create(PlayerProfileCreateRequest request)
    {
        var profile = new PlayerProfile
        {
            UUID = Guid.NewGuid().ToString(),
            Name = request.Name,
            Faction = request.Faction,
            TotalCredits = request.TotalCredits,
            SkillPoints = request.SkillPoints,
            CitizenId = request.CitizenId,
            RegistrationDate = request.RegistrationDate,
            ActiveTime = request.ActiveTime,
            Public = new PlayerRank
            {
                Rank = request.PublicRank,
                CurrentXP = request.PublicCurrentXP,
                NextXP = request.PublicNextXP,
                Title = request.PublicTitle ?? string.Empty,
            },
            Private = new PlayerRank
            {
                Rank = request.PrivateRank,
                CurrentXP = request.PrivateCurrentXP,
                NextXP = request.PrivateNextXP,
                Title = request.PrivateTitle ?? string.Empty,
            },
            Military = new PlayerRank
            {
                Rank = request.MilitaryRank,
                CurrentXP = request.MilitaryCurrentXP,
                NextXP = request.MilitaryNextXP,
                Title = request.MilitaryTitle ?? string.Empty,
            },
        };

        if (request.Skills is not null)
        {
            foreach (var kvp in request.Skills)
            {
                var skill = profile.GetSkill(kvp.Key);
                skill.Level = kvp.Value.Level;
                skill.TrainingStarted = kvp.Value.TrainingStarted;
            }
        }

        _dataService.AddPlayerProfile(profile);
        _dataService.OnPlayerProfileDataChanged(profile.UUID);
        _dataService.WriteContext();
        _logger.LogInformation("Created player profile {Name} ({UUID})", profile.Name, profile.UUID);
    }

    public void Update(string uuid, PlayerProfileUpdateRequest request)
    {
        var profile = _dataService.PlayerProfiles.FirstOrDefault(p => p.UUID == uuid);
        if (profile is null)
        {
            _logger.LogWarning("Update failed: player profile {UUID} not found", uuid);
            return;
        }

        profile.Name = request.Name;
        profile.Faction = request.Faction;
        profile.TotalCredits = request.TotalCredits;
        profile.SkillPoints = request.SkillPoints;
        profile.CitizenId = request.CitizenId;
        profile.RegistrationDate = request.RegistrationDate;
        profile.ActiveTime = request.ActiveTime;
        profile.Public.Rank = request.PublicRank;
        profile.Public.CurrentXP = request.PublicCurrentXP;
        profile.Public.NextXP = request.PublicNextXP;
        profile.Public.Title = request.PublicTitle ?? string.Empty;
        profile.Private.Rank = request.PrivateRank;
        profile.Private.CurrentXP = request.PrivateCurrentXP;
        profile.Private.NextXP = request.PrivateNextXP;
        profile.Private.Title = request.PrivateTitle ?? string.Empty;
        profile.Military.Rank = request.MilitaryRank;
        profile.Military.CurrentXP = request.MilitaryCurrentXP;
        profile.Military.NextXP = request.MilitaryNextXP;
        profile.Military.Title = request.MilitaryTitle ?? string.Empty;

        if (request.Skills is not null)
        {
            foreach (var kvp in request.Skills)
            {
                var skill = profile.GetSkill(kvp.Key);
                skill.Level = kvp.Value.Level;
                skill.TrainingStarted = kvp.Value.TrainingStarted;
            }
        }

        _dataService.IsDirty = true;
        _dataService.OnPlayerProfileDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Updated player profile {UUID}", uuid);
    }

    public void Delete(string uuid)
    {
        _dataService.RemovePlayerProfile(uuid);
        _dataService.OnPlayerProfileDataChanged(uuid);
        _dataService.WriteContext();
        _logger.LogInformation("Deleted player profile {UUID}", uuid);
    }
}
