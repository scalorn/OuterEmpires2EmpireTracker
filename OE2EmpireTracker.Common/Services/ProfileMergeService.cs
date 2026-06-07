// <copyright file="ProfileMergeService.cs" company="OE2EmpireTracker">
// Copyright (c) OE2EmpireTracker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using NLog;
using OE2EmpireTracker.Client;
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
        /// <param name="remote">The remote profile data from the game API.</param>
        /// <returns>True if any fields were changed; otherwise false.</returns>
        public static bool MergeProfileData(PlayerProfile local, GameApiProfileResponse remote)
        {
            if (local == null || remote == null)
            {
                return false;
            }

            bool changed = false;

            // Merge Faction
            if (remote.Faction != null && remote.Faction != local.Faction)
            {
                Log.Info(
                    "Profile merge conflict: Faction '{0}' -> '{1}' (strategy: API wins)",
                    local.Faction,
                    remote.Faction);
                local.Faction = remote.Faction;
                changed = true;
            }

            // Merge CitizenId
            if (remote.CitizenId != null && remote.CitizenId != local.CitizenId)
            {
                Log.Info(
                    "Profile merge conflict: CitizenId '{0}' -> '{1}' (strategy: API wins)",
                    local.CitizenId,
                    remote.CitizenId);
                local.CitizenId = remote.CitizenId;
                changed = true;
            }

            // Merge SkillPoints
            if (remote.SkillPoints != local.SkillPoints)
            {
                Log.Info(
                    "Profile merge conflict: SkillPoints '{0}' -> '{1}' (strategy: API wins)",
                    local.SkillPoints,
                    remote.SkillPoints);
                local.SkillPoints = remote.SkillPoints;
                changed = true;
            }

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

            // Merge Ranks
            if (remote.Ranks != null)
            {
                changed |= MergeRank(local.Public, remote.Ranks.Public, "Public");
                changed |= MergeRank(local.Private, remote.Ranks.Private, "Private");
                changed |= MergeRank(local.Military, remote.Ranks.Military, "Military");
            }

            // Merge Skills (overwrite levels from API, preserve TrainingStarted and CompletionTime)
            if (remote.Skills != null)
            {
                changed |= MergeSkills(local, remote.Skills, remote.SkillInTraining);
            }

            return changed;
        }

        /// <summary>
        /// Merges a single rank category (Public, Private, or Military) from the API response.
        /// </summary>
        /// <param name="localRank">The local rank to update.</param>
        /// <param name="remoteRank">The remote rank data from the API.</param>
        /// <param name="rankName">The display name of the rank category for logging.</param>
        /// <returns>True if any rank fields were changed; otherwise false.</returns>
        private static bool MergeRank(PlayerRank localRank, GameApiRankResponse remoteRank, string rankName)
        {
            if (localRank == null || remoteRank == null)
            {
                return false;
            }

            bool changed = false;

            if (remoteRank.Level != localRank.Rank)
            {
                Log.Info(
                    "Profile merge conflict: {0}.Rank '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.Rank,
                    remoteRank.Level);
                localRank.Rank = remoteRank.Level;
                changed = true;
            }

            if (remoteRank.LevelName != null && remoteRank.LevelName != localRank.RankName)
            {
                Log.Info(
                    "Profile merge conflict: {0}.RankName '{1}' -> '{2}' (strategy: API wins)",
                    rankName,
                    localRank.RankName,
                    remoteRank.LevelName);
                localRank.RankName = remoteRank.LevelName;
                changed = true;
            }

