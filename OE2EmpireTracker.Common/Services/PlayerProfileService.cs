using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Centralizes all PlayerProfile mutation. The form and ViewModel never touch the entity directly.
    /// Only this service (plus deserialization and migration) mutates PlayerProfile objects.
    /// </summary>
    public class PlayerProfileService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly PlayerContext _playerContext;

        public PlayerProfileService(PlayerContext playerContext)
        {
            _playerContext = playerContext ?? throw new ArgumentNullException(nameof(playerContext));
        }

        /// <summary>
        /// Applies changes from the update request to an existing profile, persists, and fires the change event.
        /// </summary>
        public ReadOnlyPlayerProfile Update(string uuid, PlayerProfileUpdateRequest request)
        {
            if (string.IsNullOrEmpty(uuid)) throw new ArgumentNullException(nameof(uuid));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var profile = _playerContext.FindMutablePlayerProfile(uuid);
            if (profile == null) throw new InvalidOperationException("Profile not found: " + uuid);

            Log.Info(
                "PlayerProfileService.Update: UUID={0} name='{1}' -> '{2}'",
                uuid, profile.Name, request.Name);

            // Apply scalar fields
            profile.Name = request.Name;
            profile.Faction = request.Faction;
            profile.TotalCredits = request.TotalCredits;
            profile.SkillPoints = request.SkillPoints;
            profile.CitizenId = request.CitizenId;
            profile.RegistrationDate = request.RegistrationDate;
            profile.ActiveTime = request.ActiveTime;

            // Apply ranks
            ApplyRank(profile.Public, request.PublicRank, request.PublicCurrentXp,
                      request.PublicXpToNextLevel, request.PublicRankName);
            ApplyRank(profile.Private, request.PrivateRank, request.PrivateCurrentXp,
                      request.PrivateXpToNextLevel, request.PrivateRankName);
            ApplyRank(profile.Military, request.MilitaryRank, request.MilitaryCurrentXp,
                      request.MilitaryXpToNextLevel, request.MilitaryRankName);

            // Apply skills
            if (request.Skills != null)
            {
                foreach (var kvp in request.Skills)
                {
                    var skill = profile.GetSkill(kvp.Key);
                    skill.Level = kvp.Value.Level;
                    skill.TrainingStarted = kvp.Value.TrainingStarted;
                    skill.CompletionTime.StartTime = kvp.Value.CompletionStartTime;
                    skill.CompletionTime.EndTime = kvp.Value.CompletionEndTime;
                }
            }

            // Apply skill groups
            if (request.SkillGroups != null)
            {
                foreach (var kvp in request.SkillGroups)
                {
                    profile.SetSkillGroup(kvp.Key, kvp.Value);
                }
            }

            _playerContext.WriteContext();
            _playerContext.OnPlayerProfileDataChanged(uuid);
            return new ReadOnlyPlayerProfile(profile);
        }

        /// <summary>
        /// Creates a new profile, assigns a UUID, adds to the list, persists, and fires the change events.
        /// </summary>
        public ReadOnlyPlayerProfile Create(PlayerProfileCreateRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var profile = new PlayerProfile();
            profile.UUID = Guid.NewGuid().ToString();
            profile.Name = request.Name;
            profile.Faction = request.Faction;
            profile.TotalCredits = request.TotalCredits;
            profile.SkillPoints = request.SkillPoints;
            profile.CitizenId = request.CitizenId;
            profile.RegistrationDate = request.RegistrationDate;
            profile.ActiveTime = request.ActiveTime;

            Log.Info(
                "PlayerProfileService.Create: name='{0}' UUID={1}",
                profile.Name, profile.UUID);

            // Apply ranks
            ApplyRank(profile.Public, request.PublicRank, request.PublicCurrentXp,
                      request.PublicXpToNextLevel, request.PublicRankName);
            ApplyRank(profile.Private, request.PrivateRank, request.PrivateCurrentXp,
                      request.PrivateXpToNextLevel, request.PrivateRankName);
            ApplyRank(profile.Military, request.MilitaryRank, request.MilitaryCurrentXp,
                      request.MilitaryXpToNextLevel, request.MilitaryRankName);

            // Apply skills
            if (request.Skills != null)
            {
                foreach (var kvp in request.Skills)
                {
                    var skill = profile.GetSkill(kvp.Key);
                    skill.Level = kvp.Value.Level;
                    skill.TrainingStarted = kvp.Value.TrainingStarted;
                    skill.CompletionTime.StartTime = kvp.Value.CompletionStartTime;
                    skill.CompletionTime.EndTime = kvp.Value.CompletionEndTime;
                }
            }

            // Apply skill groups
            if (request.SkillGroups != null)
            {
                foreach (var kvp in request.SkillGroups)
                {
                    profile.SetSkillGroup(kvp.Key, kvp.Value);
                }
            }

            _playerContext.AddPlayerProfile(profile);
            _playerContext.WriteContext();
            _playerContext.OnPlayerProfilesChanged();
            _playerContext.OnPlayerProfileDataChanged(profile.UUID);
            return new ReadOnlyPlayerProfile(profile);
        }

        /// <summary>
        /// Removes a profile and cascades deletion of all owned data. No-op if UUID is empty or not found.
        /// </summary>
        public void Delete(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return;

            var profile = _playerContext.FindMutablePlayerProfile(uuid);
            if (profile == null) return;

            Log.Info("PlayerProfileService.Delete: UUID={0} name='{1}'", uuid, profile.Name);

            _playerContext.RemovePlayerProfile(profile);
            _playerContext.CascadeDeletePlayer(uuid);
            _playerContext.WriteContext();
            _playerContext.OnPlayerProfilesChanged();
            _playerContext.OnPlayerProfileDataChanged(uuid);
        }

        /// <summary>
        /// Imports a parsed profile. Merges into an existing profile if a name match is found (case-insensitive),
        /// otherwise creates a new profile with a generated UUID.
        /// </summary>
        public ReadOnlyPlayerProfile Import(PlayerProfile tempProfile)
        {
            if (tempProfile == null) throw new ArgumentNullException(nameof(tempProfile));

            // Find existing by name (case-insensitive)
            var existing = _playerContext.PlayerProfileList
                .FirstOrDefault(p => string.Equals(p.Name, tempProfile.Name,
                                                   StringComparison.OrdinalIgnoreCase));

            PlayerProfile target;
            if (existing != null)
            {
                Log.Info(
                    "PlayerProfileService.Import: merging into existing UUID={0} name='{1}'",
                    existing.UUID, existing.Name);
                target = _playerContext.FindMutablePlayerProfile(existing.UUID);
                MergeProfile(target, tempProfile);
            }
            else
            {
                Log.Info(
                    "PlayerProfileService.Import: creating new profile name='{0}'",
                    tempProfile.Name);
                tempProfile.UUID = Guid.NewGuid().ToString();
                _playerContext.AddPlayerProfile(tempProfile);
                target = tempProfile;
            }

            _playerContext.WriteContext();
            _playerContext.OnPlayerProfilesChanged();
            _playerContext.OnPlayerProfileDataChanged(target.UUID);
            return new ReadOnlyPlayerProfile(target);
        }

        /// <summary>
        /// Merges parsed profile data into an existing profile, preserving UUID.
        /// Moved from FormPlayerProfile.MergeProfile.
        /// </summary>
        internal static void MergeProfile(PlayerProfile existing, PlayerProfile parsed)
        {
            existing.Name = parsed.Name;
            existing.Faction = parsed.Faction;
            existing.TotalCredits = parsed.TotalCredits;
            existing.SkillPoints = parsed.SkillPoints;
            existing.CitizenId = parsed.CitizenId;
            existing.RegistrationDate = parsed.RegistrationDate;
            existing.ActiveTime = parsed.ActiveTime;

            ApplyRank(existing.Public, parsed.Public.Rank, parsed.Public.CurrentXp,
                      parsed.Public.XpToNextLevel, parsed.Public.RankName);
            ApplyRank(existing.Private, parsed.Private.Rank, parsed.Private.CurrentXp,
                      parsed.Private.XpToNextLevel, parsed.Private.RankName);
            ApplyRank(existing.Military, parsed.Military.Rank, parsed.Military.CurrentXp,
                      parsed.Military.XpToNextLevel, parsed.Military.RankName);

            foreach (SkillGroupName group in Enum.GetValues(typeof(SkillGroupName)))
            {
                existing.SetSkillGroup(group, parsed.GetSkillGroup(group));
            }

            foreach (var skillEntry in parsed.Skills)
            {
                var existingSkill = existing.GetSkill(skillEntry.Key);
                existingSkill.Level = skillEntry.Value.Level;
                existingSkill.TrainingStarted = skillEntry.Value.TrainingStarted;
                existingSkill.CompletionTime = skillEntry.Value.CompletionTime;
            }
        }

        private static void ApplyRank(PlayerRank rank, int level, long curXp, long xpToNext, string rankName)
        {
            rank.Rank = level;
            rank.CurrentXp = curXp;
            rank.XpToNextLevel = xpToNext;
            rank.RankName = rankName;
        }
    }
}
