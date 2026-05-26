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
                CurrentXp = request.PublicCurrentXp,
                XpToNextLevel = request.PublicXpToNextLevel,
                RankName = request.PublicRankName ?? string.Empty,
            },
            Private = new PlayerRank
            {
                Rank = request.PrivateRank,
                CurrentXp = request.PrivateCurrentXp,
                XpToNextLevel = request.PrivateXpToNextLevel,
                RankName = request.PrivateRankName ?? string.Empty,
            },
            Military = new PlayerRank
            {
                Rank = request.MilitaryRank,
                CurrentXp = request.MilitaryCurrentXp,
                XpToNextLevel = request.MilitaryXpToNextLevel,
                RankName = request.MilitaryRankName ?? string.Empty,
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
        profile.Public.CurrentXp = request.PublicCurrentXp;
        profile.Public.XpToNextLevel = request.PublicXpToNextLevel;
        profile.Public.RankName = request.PublicRankName ?? string.Empty;
        profile.Private.Rank = request.PrivateRank;
        profile.Private.CurrentXp = request.PrivateCurrentXp;
        profile.Private.XpToNextLevel = request.PrivateXpToNextLevel;
        profile.Private.RankName = request.PrivateRankName ?? string.Empty;
        profile.Military.Rank = request.MilitaryRank;
        profile.Military.CurrentXp = request.MilitaryCurrentXp;
        profile.Military.XpToNextLevel = request.MilitaryXpToNextLevel;
        profile.Military.RankName = request.MilitaryRankName ?? string.Empty;

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
