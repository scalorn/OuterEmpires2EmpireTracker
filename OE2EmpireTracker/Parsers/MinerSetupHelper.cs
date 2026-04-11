using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Parsers
{
    /// <summary>
    /// Static helper for automating mining rig setup during colony import/reimport.
    /// Handles survey assignment, default survey creation, timer start, and warehouse resource seeding.
    /// </summary>
    public static class MinerSetupHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Finds the best matching real survey for the given planet, resource, purity, and maxRate.
        /// Returns null if no matching real survey exists.
        /// </summary>
        /// <remarks>
        /// Searches PlayerContext.SurveyList for real surveys (SurveyID != "DEFAULT") matching
        /// the colony planet name (case-insensitive) that contain the mined resource at the
        /// matching purity. When maxRate > 0, selects the survey whose SurveyResource Amount
        /// most closely matches the maxRate. When maxRate == 0, selects the survey with the
        /// highest Amount.
        /// </remarks>
        internal static Survey FindBestSurvey(
            string planetName, string resourceName, string purity,
            decimal maxRate, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(planetName) ||
                string.IsNullOrEmpty(resourceName) ||
                string.IsNullOrEmpty(purity) ||
                playerContext == null)
            {
                return null;
            }

            // Find all real surveys matching planet, resource, and purity
            var candidates = playerContext.SurveyList
                .Where(s =>
                    !string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.PlanetName, planetName, StringComparison.OrdinalIgnoreCase) &&
                    s.Resources != null &&
                    s.Resources.Values.Any(r =>
                        string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (maxRate > 0m)
            {
                // Select the survey whose resource Amount most closely matches maxRate
                return candidates
                    .OrderBy(s =>
                    {
                        var resource = s.Resources.Values.First(r =>
                            string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase));
                        decimal amount;
                        decimal.TryParse(resource.Amount, out amount);
                        return Math.Abs(amount - maxRate);
                    })
                    .First();
            }
            else
            {
                // maxRate == 0: select the survey with the highest Amount
                return candidates
                    .OrderByDescending(s =>
                    {
                        var resource = s.Resources.Values.First(r =>
                            string.Equals(r.Resource, resourceName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(r.Purity, purity, StringComparison.OrdinalIgnoreCase));
                        decimal amount;
                        decimal.TryParse(resource.Amount, out amount);
                        return amount;
                    })
                    .First();
            }
        }

        /// <summary>
        /// Assigns the best survey to a single mining rig structure.
        /// Returns true if a survey was assigned (or preserved), false if skipped.
        /// </summary>
        /// <remarks>
        /// Logic:
        /// 1. If structure has no MiningSurveyResource → skip (return false)
        /// 2. If structure already has a MiningSurvey:
        ///    a. Valid real survey → preserve it
        ///    b. DEFAULT survey → check if real survey now exists, upgrade if so
        ///    c. Deleted survey → fall through to step 3
        /// 3. No valid survey → FindBestSurvey; if none, CreateOrUpdateDefaultSurvey
        /// </remarks>
        internal static bool AssignSurvey(
            ColonyStructure structure, Colony colony,
            PlayerContext playerContext, decimal maxRate)
        {
            // Step 1: skip if no resource assigned
            if (string.IsNullOrEmpty(structure.MiningSurveyResource))
            {
                return false;
            }

            string resource = structure.MiningSurveyResource;
            string purity = structure.RefiningResourcePurity ?? "";

            // Step 2: check existing survey assignment
            if (!string.IsNullOrEmpty(structure.MiningSurvey))
            {
                Survey existing = playerContext.FindSurvey(structure.MiningSurvey);

                if (existing != null)
                {
                    // 2a: valid real survey → preserve
                    if (!string.Equals(existing.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Info("Preserved existing real survey {0} on structure {1} for {2}",
                            structure.MiningSurvey, structure.UUID, resource);
                        return true;
                    }

                    // 2b: DEFAULT survey → check if a real survey now exists
                    Survey realSurvey = FindBestSurvey(colony.PlanetName, resource, purity, maxRate, playerContext);
                    if (realSurvey != null)
                    {
                        structure.MiningSurvey = realSurvey.UUID;
                        Log.Info("Upgraded default survey to real survey {0} on structure {1} for {2}",
                            realSurvey.UUID, structure.UUID, resource);
                        return true;
                    }

                    // No real survey available → keep the default
                    Log.Info("Kept default survey {0} on structure {1} for {2} (no real survey available)",
                        structure.MiningSurvey, structure.UUID, resource);
                    return true;
                }

                // 2c: survey no longer exists (deleted) → fall through to step 3
                Log.Info("Existing survey {0} no longer exists for structure {1}, reassigning",
                    structure.MiningSurvey, structure.UUID);
            }

            // Step 3: no existing valid survey → find best or create default
            Survey bestSurvey = FindBestSurvey(colony.PlanetName, resource, purity, maxRate, playerContext);
            if (bestSurvey != null)
            {
                structure.MiningSurvey = bestSurvey.UUID;
                Log.Info("Assigned real survey {0} to structure {1} for {2}",
                    bestSurvey.UUID, structure.UUID, resource);
                return true;
            }

            // No real survey → create/update default
            Survey defaultSurvey = CreateOrUpdateDefaultSurvey(colony, resource, purity, maxRate, playerContext);
            structure.MiningSurvey = defaultSurvey.UUID;
            Log.Info("Assigned default survey {0} to structure {1} for {2}",
                defaultSurvey.UUID, structure.UUID, resource);
            return true;
        }

        /// <summary>
        /// Ensures the colony warehouse contains a resource record for the given
        /// resource name and purity. Creates one with quantity 0 if missing.
        /// Does NOT modify existing records.
        /// </summary>
        internal static void EnsureWarehouseResource(Colony colony, string resourceName, string purity)
        {
            if (colony == null ||
                string.IsNullOrEmpty(resourceName) ||
                string.IsNullOrEmpty(purity))
            {
                return;
            }

            var existing = colony.Items.FindResource(resourceName, purity);
            if (existing.Count == 0)
            {
                var item = new Item(ItemType.ItemTypeEnum.Resource, resourceName);
                item.UUID = Guid.NewGuid().ToString();
                item.ResourcePurity = purity;
                item.BaseItemTypeID = resourceName;
                item.Quantity = 0;
                colony.Items.AddItem(item);
                Log.Info("Created warehouse resource: {0} ({1}) for colony {2}",
                    resourceName, purity, colony.PlanetName);
            }
        }

        /// <summary>
        /// Sets up the mining timer on a structure if maxRate > 0 and no active timer exists.
        /// Creates a repeating timer aligned to the next clock-hour boundary.
        /// </summary>
        internal static void SetupTimer(ColonyStructure structure, decimal maxRate)
        {
            if (maxRate <= 0m)
            {
                Log.Info("Skipped timer creation for structure {0}: maxRate is 0 (miner assigned but not actively mining)",
                    structure.UUID);
                return;
            }

            // Preserve existing active repeating timer
            if (structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IsRepeating)
            {
                Log.Info("Preserved existing active timer on structure {0}", structure.UUID);
                return;
            }

            // Calculate seconds until next clock-hour boundary
            DateTime now = DateTime.Now;
            DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            int secondsUntilNextHour = (int)(nextHour - now).TotalSeconds;

            structure.ProcessCompletionTime = new CountDownTime();
            structure.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);

            Log.Info("Created repeating mining timer on structure {0}, next fire in {1}s",
                structure.UUID, secondsUntilNextHour);
        }

        /// <summary>
        /// Creates or updates a default survey for the colony, adding a resource entry
        /// for the given resource/purity/amount.
        /// Returns the default survey.
        /// </summary>
        internal static Survey CreateOrUpdateDefaultSurvey(
            Colony colony, string resourceName, string purity,
            decimal maxRate, PlayerContext playerContext)
        {
            string uuid = DeterministicUUID.GenerateDefaultSurvey(
                colony.OwnerUUID, colony.PlanetName, colony.SystemName);

            string amount = maxRate > 0m ? maxRate.ToString() : "0";

            // Search for existing default survey with this UUID
            Survey defaultSurvey = playerContext.SurveyList
                .FirstOrDefault(s => s.UUID == uuid);

            if (defaultSurvey != null)
            {
                // Update existing: add or update the resource entry
                defaultSurvey.Resources[resourceName] =
                    new SurveyResource(resourceName, purity, amount);
                Log.Info("Updated default survey {0} with resource {1} ({2}) amount={3}",
                    uuid, resourceName, purity, amount);
            }
            else
            {
                // Create new default survey
                defaultSurvey = new Survey("Default Survey")
                {
                    UUID = uuid,
                    SurveyID = "DEFAULT",
                    NickName = "",
                    PlanetName = colony.PlanetName,
                    SystemName = colony.SystemName,
                    OwnerUUID = colony.OwnerUUID
                };
                defaultSurvey.Resources[resourceName] =
                    new SurveyResource(resourceName, purity, amount);
                playerContext.SurveyList.Add(defaultSurvey);
                Log.Info("Created default survey {0} for colony {1} with resource {2} ({3}) amount={4}",
                    uuid, colony.PlanetName, resourceName, purity, amount);
            }

            return defaultSurvey;
        }
    }
}
