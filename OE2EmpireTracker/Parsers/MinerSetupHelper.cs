using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Services.Migration;

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
        /// Runs survey assignment and timer setup for all mining rigs in the colony.
        /// maxRates maps structure UUID -> maxRate from the game JSON.
        /// Called after the merge loop in ParseColonyBuildingsFromJson.
        /// </summary>
        public static void SetupMiners(Colony colony, EmpireContext empireContext,
            Dictionary<string, decimal> maxRates)
        {
            if (colony == null || empireContext == null || maxRates == null)
            {
                return;
            }

            PlayerContext playerContext = PlayerContext.GetInstance();
            int surveysAssigned = 0;
            int timersStarted = 0;
            int warehouseResourcesCreated = 0;

            Log.Info("SetupMiners starting for colony {0}: {1} structures, {2} maxRate entries",
                colony.PlanetName, colony.Structures.Count, maxRates.Count);

            // Diagnostic: snapshot of default surveys for this planet before processing
            var defaultSurveysBefore = playerContext.SurveyList
                .Where(s => string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Log.Info("SetupMiners: {0} DEFAULT surveys for planet '{1}' before processing: [{2}]",
                defaultSurveysBefore.Count, colony.PlanetName,
                string.Join(", ", defaultSurveysBefore.Select(s => s.UUID)));
            Log.Info("SetupMiners: total survey count before={0}", playerContext.SurveyList.Count);

            foreach (var structure in colony.Structures)
            {
                // Identify mining rigs by non-empty MiningSurveyResource
                if (string.IsNullOrEmpty(structure.MiningSurveyResource))
                {
                    continue;
                }

                decimal maxRate;
                if (!maxRates.TryGetValue(structure.UUID, out maxRate))
                {
                    maxRate = 0m;
                }

                Log.Info("Processing miner {0} (FlatpackBP={1}): MiningSurveyResource='{2}', RefiningResourcePurity='{3}', maxRate={4}, existingSurvey='{5}'",
                    structure.UUID,
                    structure.FlatpackBlueprintUUID ?? "(null)",
                    structure.MiningSurveyResource,
                    structure.RefiningResourcePurity ?? "(null)",
                    maxRate,
                    structure.MiningSurvey ?? "(null)");

                // Step a: Assign survey
                bool assigned = AssignSurvey(structure, colony, playerContext, maxRate);
                if (assigned)
                {
                    surveysAssigned++;

                    // Step b: Ensure warehouse resource exists
                    if (!string.IsNullOrEmpty(structure.MiningSurveyResource))
                    {
                        int itemCountBefore = colony.Items.Count();
                        EnsureWarehouseResource(colony, structure.MiningSurveyResource, structure.RefiningResourcePurity);
                        if (colony.Items.Count() > itemCountBefore)
                        {
                            warehouseResourcesCreated++;
                        }
                    }

                    // Step c: Setup timer
                    bool hadTimer = structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IsRepeating;
                    SetupTimer(structure, maxRate);
                    if (!hadTimer && structure.ProcessCompletionTime != null && structure.ProcessCompletionTime.IsRepeating)
                    {
                        timersStarted++;
                    }
                }
            }

            // Step d: Cleanup stale default survey resources
            CleanupDefaultSurvey(colony, playerContext, empireContext);

            Log.Info("SetupMiners complete for colony {0}: {1} surveys assigned, {2} timers started, {3} warehouse resources created",
                colony.PlanetName, surveysAssigned, timersStarted, warehouseResourcesCreated);

            // Diagnostic: snapshot of default surveys for this planet after processing
            var defaultSurveysAfter = playerContext.SurveyList
                .Where(s => string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Log.Info("SetupMiners: {0} DEFAULT surveys for planet '{1}' after processing: [{2}]",
                defaultSurveysAfter.Count, colony.PlanetName,
                string.Join(", ", defaultSurveysAfter.Select(s => s.UUID)));
            if (defaultSurveysAfter.Count > 1)
            {
                Log.Warn("BL-050 DIAGNOSTIC: Multiple DEFAULT surveys remain for planet '{0}' after SetupMiners! UUIDs: [{1}]",
                    colony.PlanetName,
                    string.Join(", ", defaultSurveysAfter.Select(s => $"{s.UUID} (OwnerUUID={s.OwnerUUID})")));
            }

            Log.Info("SetupMiners: total survey count after={0}", playerContext.SurveyList.Count);
        }

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
                Log.Debug("FindBestSurvey: 0 candidate surveys for planet='{0}', resource='{1}', purity='{2}'",
                    planetName, resourceName, purity);
                return null;
            }

            Log.Debug("FindBestSurvey: {0} candidate surveys for planet='{1}', resource='{2}', purity='{3}', maxRate={4}",
                candidates.Count, planetName, resourceName, purity, maxRate);

            if (maxRate > 0m)
            {
                // Select the survey whose resource Amount most closely matches maxRate
                var selected = candidates
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
                Log.Info("FindBestSurvey: selected survey {0} (closest match to maxRate={1}) for {2} ({3})",
                    selected.UUID, maxRate, resourceName, purity);
                return selected;
            }
            else
            {
                // maxRate == 0: select the survey with the highest Amount
                var selected = candidates
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
                Log.Info("FindBestSurvey: selected survey {0} (highest amount, maxRate=0) for {1} ({2})",
                    selected.UUID, resourceName, purity);
                return selected;
            }
        }

        /// <summary>
        /// Assigns the best survey to a single mining rig structure.
        /// Returns true if a survey was assigned (or preserved), false if skipped.
        /// </summary>
        /// <remarks>
        /// Logic:
        /// 1. If structure has no MiningSurveyResource -> skip (return false)
        /// 2. If structure already has a MiningSurvey:
        ///    a. Valid real survey -> preserve it
        ///    b. DEFAULT survey -> check if real survey now exists, upgrade if so
        ///    c. Deleted survey -> fall through to step 3
        /// 3. No valid survey -> FindBestSurvey; if none, CreateOrUpdateDefaultSurvey
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
            string purity = structure.RefiningResourcePurity ?? string.Empty;

            // Step 2: check existing survey assignment
            if (!string.IsNullOrEmpty(structure.MiningSurvey))
            {
                Survey existing = playerContext.FindSurvey(structure.MiningSurvey);

                if (existing != null)
                {
                    // 2a: valid real survey -> preserve
                    if (!string.Equals(existing.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Info("Preserved existing real survey {0} on structure {1} for {2}",
                            structure.MiningSurvey, structure.UUID, resource);
                        return true;
                    }

                    // 2b: DEFAULT survey -> check if a real survey now exists
                    Survey realSurvey = FindBestSurvey(colony.PlanetName, resource, purity, maxRate, playerContext);
                    if (realSurvey != null)
                    {
                        structure.MiningSurvey = realSurvey.UUID;
                        Log.Info("Upgraded default survey to real survey {0} on structure {1} for {2}",
                            realSurvey.UUID, structure.UUID, resource);
                        return true;
                    }

                    // No real survey available -> keep the default
                    Log.Info("Kept default survey {0} on structure {1} for {2} (no real survey available)",
                        structure.MiningSurvey, structure.UUID, resource);
                    return true;
                }

                // 2c: survey no longer exists (deleted) -> fall through to step 3
                Log.Info("Existing survey {0} no longer exists for structure {1}, reassigning",
                    structure.MiningSurvey, structure.UUID);
            }

            // Step 3: no existing valid survey -> find best or create default
            Survey bestSurvey = FindBestSurvey(colony.PlanetName, resource, purity, maxRate, playerContext);
            if (bestSurvey != null)
            {
                structure.MiningSurvey = bestSurvey.UUID;
                Log.Info("Assigned real survey {0} to structure {1} for {2}",
                    bestSurvey.UUID, structure.UUID, resource);
                return true;
            }

            // No real survey -> create/update default
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
            DateTime now = SystemClock.UtcNow;
            DateTime nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
            int secondsUntilNextHour = (int)(nextHour - now).TotalSeconds;

            structure.ProcessCompletionTime = new CountDownTime();
            structure.ProcessCompletionTime.StartRepeating(GameConstants.SecondsPerHour, secondsUntilNextHour);

            Log.Info("Created repeating mining timer on structure {0}, next fire in {1}s",
                structure.UUID, secondsUntilNextHour);
        }

        /// <summary>
        /// Removes resource entries from the colony's default survey that are no longer
        /// being mined. Removes the default survey entirely if no resources remain.
        /// </summary>
        internal static void CleanupDefaultSurvey(
            Colony colony, PlayerContext playerContext,
            EmpireContext empireContext)
        {
            string defaultUUID = DeterministicUUID.GenerateDefaultSurvey(
                colony.OwnerUUID, colony.PlanetName, colony.SystemName);

            Log.Info("CleanupDefaultSurvey: expected UUID={0} for planet='{1}', owner='{2}', system='{3}'",
                defaultUUID, colony.PlanetName, colony.OwnerUUID, colony.SystemName);

            // Log all DEFAULT surveys in the list for this planet (regardless of owner)
            var allDefaults = playerContext.SurveyList
                .Where(s => string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Log.Info("CleanupDefaultSurvey: {0} total DEFAULT surveys for planet '{1}': [{2}]",
                allDefaults.Count, colony.PlanetName,
                string.Join(", ", allDefaults.Select(s => $"UUID={s.UUID}, Owner={s.OwnerUUID}")));

            // Remove duplicate DEFAULT surveys for the same planet/owner (stale orphans
            // from earlier bugs). Keep only the one with the correct deterministic UUID.
            var duplicates = playerContext.SurveyList
                .Where(s =>
                    s.UUID != defaultUUID &&
                    string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(s.OwnerUUID, colony.OwnerUUID, StringComparison.Ordinal))
                .ToList();

            foreach (var dup in duplicates)
            {
                playerContext.RemoveSurvey(dup);
                Log.Info("Removed duplicate default survey {0} for planet {1} (correct UUID is {2})",
                    dup.UUID, colony.PlanetName, defaultUUID);
            }

            Survey defaultSurvey = playerContext.SurveyList
                .FirstOrDefault(s => s.UUID == defaultUUID);

            if (defaultSurvey == null)
            {
                return;
            }

            // Collect the set of resources actively being mined from this default survey
            var activeResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var structure in colony.Structures)
            {
                if (!string.IsNullOrEmpty(structure.MiningSurveyResource) &&
                    string.Equals(structure.MiningSurvey, defaultUUID, StringComparison.OrdinalIgnoreCase))
                {
                    activeResources.Add(structure.MiningSurveyResource);
                }
            }

            // Remove stale resources from the default survey
            var staleKeys = defaultSurvey.Resources.Keys
                .Where(k => !activeResources.Contains(k))
                .ToList();

            foreach (var key in staleKeys)
            {
                defaultSurvey.Resources.Remove(key);
                Log.Info("Removed stale resource {0} from default survey {1} for colony {2}",
                    key, defaultUUID, colony.PlanetName);
            }

            // If no resources remain, remove the default survey entirely
            if (defaultSurvey.Resources.Count == 0)
            {
                playerContext.RemoveSurvey(defaultSurvey);
                Log.Info("Removed empty default survey {0} for colony {1}",
                    defaultUUID, colony.PlanetName);
            }
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

            Log.Info("CreateOrUpdateDefaultSurvey: looking for UUID={0} (planet='{1}', owner='{2}', system='{3}'), resource={4} ({5}), maxRate={6}",
                uuid, colony.PlanetName, colony.OwnerUUID, colony.SystemName, resourceName, purity, maxRate);

            // Search for existing default survey with this UUID
            Survey defaultSurvey = playerContext.SurveyList
                .FirstOrDefault(s => s.UUID == uuid);

            Log.Info("CreateOrUpdateDefaultSurvey: UUID match={0}", defaultSurvey != null ? "found" : "not found");

            // Fallback: find any DEFAULT survey for the same planet (handles stale
            // duplicates from earlier bugs where temp parse created orphan surveys)
            if (defaultSurvey == null)
            {
                defaultSurvey = playerContext.SurveyList
                    .FirstOrDefault(s =>
                        string.Equals(s.SurveyID, "DEFAULT", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.OwnerUUID, colony.OwnerUUID, StringComparison.Ordinal));

                if (defaultSurvey != null)
                {
                    // Fix the UUID to the correct deterministic value
                    Log.Info("Found stale default survey {0} for planet {1}, updating UUID to {2}",
                        defaultSurvey.UUID, colony.PlanetName, uuid);
                    defaultSurvey.UUID = uuid;
                }
            }

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
                    NickName = string.Empty,
                    PlanetName = colony.PlanetName,
                    SystemName = colony.SystemName,
                    OwnerUUID = colony.OwnerUUID
                };

                defaultSurvey.Resources[resourceName] =
                    new SurveyResource(resourceName, purity, amount);
                playerContext.AddSurvey(defaultSurvey);
                Log.Info("Created default survey {0} for colony {1} with resource {2} ({3}) amount={4}",
                    uuid, colony.PlanetName, resourceName, purity, amount);
            }

            return defaultSurvey;
        }
    }
}
