using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Static utility class providing shared rate calculations consumed by the
    /// report builder, inactivity collector, and activity collector.
    /// </summary>
    public static class ColonyResourceRateCalculator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// Computes total warehouse volume using standard volume constants.
        /// Formula: totalVolume = Σ(item.Quantity × VolumeForType(item.ItemType))
        /// </summary>
        public static decimal ComputeWarehouseVolume(Colony colony)
        {
            if (colony?.Items == null)
            {
                return 0m;
            }

            decimal totalVolume = 0m;
            foreach (var kvp in colony.Items.Items)
            {
                totalVolume += kvp.Value.Volume;
            }

            return totalVolume;
        }

        /// <summary>
        /// Returns the net hourly volume growth rate for the entire warehouse,
        /// considering all mining and refining across all resource+purity combinations.
        /// Positive = warehouse growing, negative = warehouse shrinking.
        /// </summary>
        public static decimal GetNetWarehouseVolumeGrowthRate(
            Colony colony, PlayerContext playerContext)
        {
            if (colony?.Structures == null)
            {
                return 0m;
            }

            // Collect all active resource+purity combinations from miners and refiners
            var resourcePurityPairs = GetActiveResourcePurityPairs(colony, playerContext);

            decimal netVolumeRate = 0m;
            foreach (var pair in resourcePurityPairs)
            {
                decimal miningRate = GetTotalMiningRate(colony, playerContext, pair.Resource, pair.Purity);
                decimal consumptionRate = GetTotalRefiningConsumption(colony, playerContext, pair.Resource, pair.Purity);

                // Mining adds resources (volume = VolumeResource per unit)
                // Refining consumes resources (removes volume = VolumeResource per unit)
                // Refining also produces refined resources (adds volume = VolumeResource per unit)
                // Net effect on volume for this resource+purity:
                // miningRate adds VolumeResource per unit mined
                // consumptionRate removes VolumeResource per unit consumed
                // But refining output (refined resources) also has VolumeResource volume
                netVolumeRate += (miningRate - consumptionRate) * GameConstants.VolumeResource;
            }

            // Also account for refining output (refined resources produced)
            netVolumeRate += GetTotalRefiningOutputVolumeRate(colony, playerContext);

            return netVolumeRate;
        }

        /// <summary>
        /// Returns total hourly mining output for a resource+purity across all
        /// active miners in the colony (with ExtractionFocus skill bonus).
        /// </summary>
        public static decimal GetTotalMiningRate(
            Colony colony, PlayerContext playerContext,
            string resource, string purity)
        {
            if (colony?.Structures == null)
            {
                return 0m;
            }

            int extractionFocusLevel = GetOwnerExtractionFocusLevel(colony, playerContext);
            decimal extractionMultiplier = 1.0m + (extractionFocusLevel * GameConstants.ExtractionFocusRatePerLevel);

            decimal totalRate = 0m;
            int structureCount = 0;
            int skippedNotBuiltOnline = 0;
            int skippedNoActiveProcess = 0;

            foreach (var structure in colony.Structures)
            {
                if (!structure.IsBuiltAndOnline)
                {
                    skippedNotBuiltOnline++;
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    skippedNoActiveProcess++;
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.MiningRig)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(structure.MiningSurvey) ||
                    string.IsNullOrEmpty(structure.MiningSurveyResource))
                {
                    continue;
                }

                Survey survey = playerContext.FindSurvey(structure.MiningSurvey);
                if (survey == null || !survey.Resources.ContainsKey(structure.MiningSurveyResource))
                {
                    continue;
                }

                SurveyResource surveyResource = survey.Resources[structure.MiningSurveyResource];
                if (surveyResource.Resource != resource || surveyResource.Purity != purity)
                {
                    continue;
                }

                decimal amount;
                if (!decimal.TryParse(surveyResource.Amount, out amount))
                {
                    continue;
                }

                decimal rate = amount * extractionMultiplier;
                totalRate += rate;
                structureCount++;
            }

            Log.Debug("GetTotalMiningRate: colony={0}, resource={1} ({2}), miners={3}, rate={4}, skipped: notBuiltOnline={5}, noActiveProcess={6}",
                colony.ColonyName, resource, purity, structureCount, totalRate, skippedNotBuiltOnline, skippedNoActiveProcess);

            return totalRate;
        }

        /// <summary>
        /// Returns total hourly refining consumption for a resource+purity across
        /// all active refiners in the colony.
        /// </summary>
        public static decimal GetTotalRefiningConsumption(
            Colony colony, PlayerContext playerContext,
            string resource, string purity)
        {
            if (colony?.Structures == null)
            {
                return 0m;
            }

            decimal totalConsumption = 0m;
            int refinerCount = 0;

            foreach (var structure in colony.Structures)
            {
                if (!structure.IsBuiltAndOnline)
                {
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.Refinery)
                {
                    continue;
                }

                if (structure.RefiningResource != resource ||
                    structure.RefiningResourcePurity != purity)
                {
                    continue;
                }

                int consumeRate = GetRefinerConsumptionRate(structure);
                totalConsumption += consumeRate;
                refinerCount++;
            }

            Log.Debug("GetTotalRefiningConsumption: colony={0}, resource={1} ({2}), refiners={3}, consumption={4}",
                colony.ColonyName, resource, purity, refinerCount, totalConsumption);

            return totalConsumption;
        }

        /// <summary>
        /// Computes the net hourly accumulation rate for a specific resource+purity.
        /// Positive = stockpile growing, negative = stockpile shrinking.
        /// </summary>
        public static decimal GetNetHourlyRate(
            Colony colony, PlayerContext playerContext,
            string resource, string purity)
        {
            return GetTotalMiningRate(colony, playerContext, resource, purity)
                - GetTotalRefiningConsumption(colony, playerContext, resource, purity);
        }

        /// <summary>
        /// Returns the warehouse stockpile for a resource+purity, with fallback
        /// for items stored with "X Purity" format from game API imports.
        /// </summary>
        public static int GetWarehouseStockpile(Colony colony, string resource, string purity)
        {
            if (colony?.Items == null)
            {
                return 0;
            }

            var items = colony.Items.FindResource(resource, purity);
            int total = items.Sum(i => i.Quantity);

            // Fallback: try "X Purity" format for items imported before normalization fix
            if (total == 0 && !string.IsNullOrEmpty(purity) &&
                !purity.EndsWith("Purity", System.StringComparison.OrdinalIgnoreCase))
            {
                var fallbackItems = colony.Items.FindResource(resource, purity + " Purity");
                total = fallbackItems.Sum(i => i.Quantity);
            }

            return total;
        }

        /// <summary>
        /// Returns the volume per unit for an item based on its ItemType.
        /// </summary>
        private static decimal GetVolumePerUnit(Item item)
        {
            switch (item.ItemType)
            {
                case ItemType.ItemTypeEnum.Resource:
                    return GameConstants.VolumeResource;
                case ItemType.ItemTypeEnum.Commodity:
                    return GameConstants.VolumeCommodity;
                case ItemType.ItemTypeEnum.WorkDetail:
                    return GameConstants.VolumeWorkDetail;
                case ItemType.ItemTypeEnum.Blueprint:
                    return GameConstants.VolumeBlueprint;
                case ItemType.ItemTypeEnum.Survey:
                    return GameConstants.VolumeSurvey;
                case ItemType.ItemTypeEnum.Flatpack:
                    return 0m;
                default:
                    return item.Volume;
            }
        }

        /// <summary>
        /// Returns true if the structure has an active process timer with time remaining.
        /// </summary>
        private static bool HasActiveProcess(ColonyStructure structure)
        {
            return structure.ProcessCompletionTime != null &&
                structure.ProcessCompletionTime.TimeRemaining > 0;
        }

        /// <summary>
        /// Gets the ExtractionFocus skill level for the colony owner.
        /// </summary>
        private static int GetOwnerExtractionFocusLevel(Colony colony, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(colony.OwnerUUID))
            {
                return 0;
            }

            var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == colony.OwnerUUID);
            if (owner == null)
            {
                return 0;
            }

            return owner.GetSkill(SkillName.ExtractionFocus).Level;
        }

        /// <summary>
        /// Returns the per-cycle consumption rate for a refiner.
        /// Normal refining: GameConstants.RefiningBaseRate (25).
        /// Synthetic refining: RefiningRecipe.ConsumeRate.
        /// </summary>
        private static int GetRefinerConsumptionRate(ColonyStructure refiner)
        {
            if (!string.IsNullOrEmpty(refiner.RefiningResource) &&
                !string.IsNullOrEmpty(refiner.RefiningResourcePurity))
            {
                var recipe = RefiningRecipes.FindByInput(refiner.RefiningResource, refiner.RefiningResourcePurity);
                if (recipe != null)
                {
                    return recipe.ConsumeRate;
                }
            }

            return GameConstants.RefiningBaseRate;
        }

        /// <summary>
        /// Collects all distinct resource+purity pairs from active miners and refiners.
        /// </summary>
        private static HashSet<ResourcePurityPair> GetActiveResourcePurityPairs(
            Colony colony, PlayerContext playerContext)
        {
            var pairs = new HashSet<ResourcePurityPair>();

            foreach (var structure in colony.Structures)
            {
                if (!structure.IsBuiltAndOnline)
                {
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null)
                {
                    continue;
                }

                if (blueprint.BluePrintType == BlueprintTypes.MiningRig)
                {
                    if (string.IsNullOrEmpty(structure.MiningSurvey) ||
                        string.IsNullOrEmpty(structure.MiningSurveyResource))
                    {
                        continue;
                    }

                    Survey survey = playerContext.FindSurvey(structure.MiningSurvey);
                    if (survey == null || !survey.Resources.ContainsKey(structure.MiningSurveyResource))
                    {
                        continue;
                    }

                    SurveyResource surveyResource = survey.Resources[structure.MiningSurveyResource];
                    pairs.Add(new ResourcePurityPair(surveyResource.Resource, surveyResource.Purity));
                }
                else if (blueprint.BluePrintType == BlueprintTypes.Refinery)
                {
                    if (!string.IsNullOrEmpty(structure.RefiningResource) &&
                        !string.IsNullOrEmpty(structure.RefiningResourcePurity))
                    {
                        pairs.Add(new ResourcePurityPair(structure.RefiningResource, structure.RefiningResourcePurity));
                    }
                }
            }

            return pairs;
        }

        /// <summary>
        /// Computes the total volume rate from refining output (refined resources produced per hour).
        /// </summary>
        private static decimal GetTotalRefiningOutputVolumeRate(Colony colony, PlayerContext playerContext)
        {
            if (colony?.Structures == null)
            {
                return 0m;
            }

            int refiningFocusLevel = GetOwnerRefiningFocusLevel(colony, playerContext);
            decimal refiningMultiplier = 1.0m + (refiningFocusLevel * GameConstants.RefiningFocusRatePerLevel);

            decimal totalOutputVolume = 0m;
            foreach (var structure in colony.Structures)
            {
                if (!structure.IsBuiltAndOnline)
                {
                    continue;
                }

                if (!HasActiveProcess(structure))
                {
                    continue;
                }

                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (blueprint == null || blueprint.BluePrintType != BlueprintTypes.Refinery)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(structure.RefiningResource) ||
                    string.IsNullOrEmpty(structure.RefiningResourcePurity))
                {
                    continue;
                }

                decimal outputRate = GetRefinerOutputRate(structure, refiningMultiplier);
                // Refined resources have VolumeResource volume per unit
                totalOutputVolume += outputRate * GameConstants.VolumeResource;
            }

            return totalOutputVolume;
        }

        /// <summary>
        /// Gets the RefiningFocus skill level for the colony owner.
        /// </summary>
        private static int GetOwnerRefiningFocusLevel(Colony colony, PlayerContext playerContext)
        {
            if (string.IsNullOrEmpty(colony.OwnerUUID))
            {
                return 0;
            }

            var owner = playerContext.PlayerProfileList.FirstOrDefault(p => p.UUID == colony.OwnerUUID);
            if (owner == null)
            {
                return 0;
            }

            return owner.GetSkill(SkillName.RefiningFocus).Level;
        }

        /// <summary>
        /// Returns the per-cycle output rate for a refiner (units produced).
        /// Normal refining: consumeRate × purityMultiplier × refiningMultiplier.
        /// Synthetic refining: ProduceRate × refiningMultiplier.
        /// </summary>
        private static decimal GetRefinerOutputRate(ColonyStructure refiner, decimal refiningMultiplier)
        {
            var recipe = RefiningRecipes.FindByInput(refiner.RefiningResource, refiner.RefiningResourcePurity);
            if (recipe != null)
            {
                return recipe.ProduceRate * refiningMultiplier;
            }

            int baseRate = GameConstants.RefiningBaseRate;
            int outputMultiplier;
            switch (refiner.RefiningResourcePurity)
            {
                case GameConstants.PurityLow:
                    outputMultiplier = GameConstants.PurityMultiplierLow;
                    break;
                case GameConstants.PurityMedium:
                    outputMultiplier = GameConstants.PurityMultiplierMedium;
                    break;
                case GameConstants.PurityHigh:
                    outputMultiplier = GameConstants.PurityMultiplierHigh;
                    break;
                default:
                    outputMultiplier = GameConstants.PurityMultiplierLow;
                    break;
            }

            return baseRate * outputMultiplier * refiningMultiplier;
        }

        /// <summary>
        /// Represents a resource+purity combination for deduplication.
        /// </summary>
        private struct ResourcePurityPair
        {
            public ResourcePurityPair(string resource, string purity)
            {
                Resource = resource;
                Purity = purity;
            }

            public string Resource { get; }

            public string Purity { get; }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Resource?.GetHashCode() ?? 0) * 397) ^ (Purity?.GetHashCode() ?? 0);
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is ResourcePurityPair other))
                {
                    return false;
                }

                return Resource == other.Resource && Purity == other.Purity;
            }
        }
    }
}
