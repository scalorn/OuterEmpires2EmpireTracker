// <copyright file="ProfileMergeService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Common.Client.Generated;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Provides merge logic for synchronizing API profile data with local player profile.
    /// Extracted from GameApiSyncScheduler to allow reuse by QueueSyncService.
    /// </summary>
    public static class ProfileMergeService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Merges remote profile data into the local player profile using "API wins" strategy.
        /// </summary>
        /// <param name="local">The local player profile to update.</param>
        /// <param name="remote">The remote profile data from the game API (PublicCharacter DTO).</param>
        /// <param name="skills">The remote skills data from the game API, or null if not fetched.</param>
        /// <returns>True if any fields were changed; otherwise false.</returns>
        public static bool MergeProfileData(PlayerProfile local, PublicCharacter remote, CharacterSkills skills = null)
        {
            if (local == null || remote == null)
            {
                return false;
            }

            bool changed = false;

            // Merge CharacterId (Req 1, 9.1)
            if (remote.CharacterId != 0 && remote.CharacterId != local.CharacterId)
            {
                Log.Info(
                    "Profile merge conflict: CharacterId '{0}' -> '{1}' (strategy: API wins)",
                    local.CharacterId,
                    remote.CharacterId);
                local.CharacterId = remote.CharacterId;
                changed = true;
            }

            // Merge FirstName (Req 2, 9.2)
            if (remote.FirstName != null && remote.FirstName != local.FirstName)
            {
                Log.Info(
                    "Profile merge conflict: FirstName '{0}' -> '{1}' (strategy: API wins)",
                    local.FirstName,
                    remote.FirstName);
                local.FirstName = remote.FirstName;
                changed = true;
            }

            // Merge LastName (Req 2, 9.2)
            if (remote.LastName != null && remote.LastName != local.LastName)
            {
                Log.Info(
                    "Profile merge conflict: LastName '{0}' -> '{1}' (strategy: API wins)",
                    local.LastName,
                    remote.LastName);
                local.LastName = remote.LastName;
                changed = true;
            }

            // Merge ActiveTimeMinutes (Req 3, 9.3)
            if (remote.ActiveTimeMinutes != 0 || local.ActiveTimeMinutes != 0)
            {
                int value = remote.ActiveTimeMinutes < 0 ? 0 : remote.ActiveTimeMinutes;
                if (value != local.ActiveTimeMinutes)
                {
                    Log.Info(
                        "Profile merge conflict: ActiveTimeMinutes '{0}' -> '{1}' (strategy: API wins, clamped)",
                        local.ActiveTimeMinutes,
                        value);
                    local.ActiveTimeMinutes = value;
                    changed = true;
                }
            }

            // Merge Ranks from Levels collection
            if (remote.Levels != null)
            {
                foreach (var level in remote.Levels)
                {
                    if (string.Equals(level.Track, "public", StringComparison.OrdinalIgnoreCase))
                    {
                        changed |= MergeRank(local.Public, level, "Public");
                    }
                    else if (string.Equals(level.Track, "private", StringComparison.OrdinalIgnoreCase))
                    {
                        changed |= MergeRank(local.Private, level, "Private");
                    }
                    else if (string.Equals(level.Track, "military", StringComparison.OrdinalIgnoreCase))
                    {
                        changed |= MergeRank(local.Military, level, "Military");
                    }
                }
            }

            // Merge Skills (overwrite levels from API, preserve TrainingStarted and CompletionTime)
            if (skills?.Skills != null)
            {
                changed |= MergeSkills(local, skills.Skills, skills.SkillInTraining);
            }

            return changed;
        }

        /// <summary>
        /// Merges a single rank category (Public, Private, or Military) from a CharacterLevel DTO.
        /// </summary>
        /// <param name="localRank">The local rank to update.</param>
        /// <param name="remoteLevel">The remote level data from the API.</param>
        /// <param name="rankName">The display name of the rank category for logging.</param>
        /// <returns>True if any rank fields were changed; otherwise false.</returns>
        private static bool MergeRank(PlayerRank localRank, CharacterLevel remoteLevel, string rankName)
        {
            if (localRank == null || remoteLevel == null)
            {
                return false;
            }

            bool changed = false;

            if (remoteLevel.LevelId != localRank.Rank)
            {
                Log.Info(
                    "Profile merge conflict: {0}.Rank '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.Rank,
                    remoteLevel.LevelId);
                localRank.Rank = remoteLevel.LevelId;
                changed = true;
            }

            if (remoteLevel.LevelName != null && remoteLevel.LevelName != localRank.RankName)
            {
                Log.Info(
                    "Profile merge conflict: {0}.RankName '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.RankName,
                    remoteLevel.LevelName);
                localRank.RankName = remoteLevel.LevelName;
                changed = true;
            }

            if (remoteLevel.XpToNextLevel != localRank.XpToNextLevel)
            {
                Log.Info(
                    "Profile merge conflict: {0}.XpToNextLevel '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.XpToNextLevel,
                    remoteLevel.XpToNextLevel);
                localRank.XpToNextLevel = remoteLevel.XpToNextLevel;
                changed = true;
            }

            if (remoteLevel.CurrentXP != localRank.CurrentXp)
            {
                Log.Info(
                    "Profile merge conflict: {0}.CurrentXp '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.CurrentXp,
                    remoteLevel.CurrentXP);
                localRank.CurrentXp = remoteLevel.CurrentXP;
                changed = true;
            }

            return changed;
        }

        /// <summary>
        /// Merges skill levels from the API response into the local profile.
        /// Preserves local TrainingStarted and CompletionTime fields.
        /// </summary>
        /// <param name="local">The local player profile containing skills.</param>
        /// <param name="remoteSkills">The remote skills collection from the API.</param>
        /// <param name="skillInTraining">The skill currently in training, or null.</param>
        /// <returns>True if any skill fields were changed; otherwise false.</returns>
        private static bool MergeSkills(
            PlayerProfile local,
            ICollection<Skill> remoteSkills,
            SkillInTraining skillInTraining)
        {
            bool changed = false;

            foreach (var remoteSkill in remoteSkills)
            {
                string skillName = remoteSkill.SkillName;
                if (string.IsNullOrEmpty(skillName))
                {
                    continue;
                }

                PlayerSkill localSkill = local.GetSkill(skillName);

                if (remoteSkill.SkillLevel != localSkill.Level)
                {
                    Log.Info(
                        "Profile merge conflict: Skills[{0}].Level '{1}' -> '{2}' (strategy: API wins)",
                        skillName,
                        localSkill.Level,
                        remoteSkill.SkillLevel);
                    localSkill.Level = remoteSkill.SkillLevel;
                    changed = true;
                }

                // Merge metadata (Req 6, 9.6)
                if (remoteSkill.SkillId != localSkill.SkillId)
                {
                    localSkill.SkillId = remoteSkill.SkillId;
                    changed = true;
                }

                string effectDesc = remoteSkill.EffectDescription ?? string.Empty;
                if (effectDesc != localSkill.EffectDescription)
                {
                    localSkill.EffectDescription = effectDesc;
                    changed = true;
                }

                if ((int)remoteSkill.AmountPerLevel != localSkill.AmountPerLevel)
                {
                    localSkill.AmountPerLevel = (int)remoteSkill.AmountPerLevel;
                    changed = true;
                }

                string groupName = remoteSkill.SkillGroupName ?? string.Empty;
                if (groupName != localSkill.SkillGroupName)
                {
                    localSkill.SkillGroupName = groupName;
                    changed = true;
                }

                if (remoteSkill.IsUnlocked != localSkill.IsUnlocked)
                {
                    localSkill.IsUnlocked = remoteSkill.IsUnlocked;
                    changed = true;
                }

                // Merge training progress (Req 7, 9.7, 9.8)
                bool isTraining = skillInTraining != null &&
                    string.Equals(skillInTraining.SkillName, skillName, StringComparison.OrdinalIgnoreCase);

                int targetLevel = isTraining ? skillInTraining.TargetLevel : 0;
                int pctComplete = isTraining ? skillInTraining.TrainingPercentageComplete : 0;
                int remainingMin = isTraining ? skillInTraining.RemainingMinutes : 0;

                if (targetLevel != localSkill.TargetLevel)
                {
                    localSkill.TargetLevel = targetLevel;
                    changed = true;
                }

                if (pctComplete != localSkill.TrainingPercentageComplete)
                {
                    localSkill.TrainingPercentageComplete = pctComplete;
                    changed = true;
                }

                if (remainingMin != localSkill.RemainingMinutes)
                {
                    localSkill.RemainingMinutes = remainingMin;
                    changed = true;
                }
            }

            // Build a set of remote skill names for lookup
            var remoteSkillNames = new HashSet<string>(
                remoteSkills.Where(s => !string.IsNullOrEmpty(s.SkillName)).Select(s => s.SkillName),
                StringComparer.OrdinalIgnoreCase);

            // Reset training fields for skills NOT in remoteSkills (Req 7.3, 7.4)
            foreach (var kvp in local.Skills)
            {
                if (!remoteSkillNames.Contains(kvp.Key))
                {
                    PlayerSkill skill = kvp.Value;
                    if (skill.TargetLevel != 0 || skill.TrainingPercentageComplete != 0 || skill.RemainingMinutes != 0)
                    {
                        skill.TargetLevel = 0;
                        skill.TrainingPercentageComplete = 0;
                        skill.RemainingMinutes = 0;
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
