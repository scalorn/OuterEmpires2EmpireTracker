using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Generates a foundation set of planned structures for a colony based on
    /// available surveys for the colony's planet.
    /// </summary>
    public class ColonyBootstrap
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly PlayerContext _playerContext;
        private readonly int _extractionFocusLevel;

        public ColonyBootstrap(PlayerContext playerContext, int extractionFocusLevel = 0)
        {
            _playerContext = playerContext;
            _extractionFocusLevel = extractionFocusLevel;
        }

        /// <summary>
        /// Generates bootstrap structures and appends them to the colony.
        /// Does NOT remove existing structures (REQ-COL-096f).
        /// </summary>
        public void Bootstrap(Colony colony)
        {
            if (string.IsNullOrEmpty(colony.PlanetName))
            {
                Log.Warn("Cannot bootstrap colony without a planet name");
                return;
            }

            // Find all surveys for this planet
            var surveys = _playerContext.SurveyList
                .Where(s => string.Equals(s.PlanetName, colony.PlanetName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (surveys.Count == 0)
            {
                Log.Warn("No surveys found for planet {0}", colony.PlanetName);
                return;
            }

            // REQ-COL-096a: For each unique resource, select the best survey entry
            var bestResources = SelectBestResources(surveys);

            var newStructures = new List<ColonyStructure>();

            // REQ-COL-096d: Fixed sequence -- Command Centre first
            var commandCentre = FindPlayerBlueprint(BlueprintTypes.ColonyCommandCentre);
            if (commandCentre != null)
            {
                newStructures.Add(CreateStructure(commandCentre.UUID));
            }

            // Remote Operations Array second
            var roa = FindPlayerBlueprint("Flatpacks/RemoteOperationsArray");
            if (roa != null)
            {
                newStructures.Add(CreateStructure(roa.UUID));
            }

            // REQ-COL-096b: One mining rig per resource
            var miningRigBp = FindPlayerBlueprint(BlueprintTypes.MiningRig);
            foreach (var entry in CollectionSortHelper.OrderByName(bestResources, r => r.ResourceName))
            {
                if (miningRigBp != null)
                {
                    var miner = CreateStructure(miningRigBp.UUID);
                    miner.MiningSurvey = entry.SurveyUUID;
                    miner.MiningSurveyResource = entry.ResourceName;
                    newStructures.Add(miner);
                }
            }

            // REQ-COL-096c: Refiners per resource based on mining rate
            var refineryBp = FindPlayerBlueprint(BlueprintTypes.Refinery);
            foreach (var entry in CollectionSortHelper.OrderByName(bestResources, r => r.ResourceName))
            {
                decimal miningRate = entry.RawAmount * (1.0m + (_extractionFocusLevel * 0.01m));
                int refinersNeeded = (int)Math.Ceiling((double)miningRate / GameConstants.RefiningBaseRate);

                for (int i = 0; i < refinersNeeded; i++)
                {
                    if (refineryBp != null)
                    {
                        var refiner = CreateStructure(refineryBp.UUID);
                        refiner.RefiningResource = entry.ResourceName;
                        refiner.RefiningResourcePurity = entry.Purity;
                        newStructures.Add(refiner);
                    }
                }
            }

            // Append to colony (REQ-COL-096f: don't remove existing)
            colony.Structures.AddRange(newStructures);

            Log.Info(
                "Bootstrap added {0} structures to colony {1} ({2} resources from {3} surveys)",
                newStructures.Count,
                colony.PlanetName,
                bestResources.Count,
                surveys.Count);

            // REQ-COL-096e: Run build order optimization
            var optimizer = new BuildOrderOptimizer(_playerContext);
            var optimized = optimizer.Optimize(colony);
            colony.Structures.Clear();
            colony.Structures.AddRange(optimized);
        }

        /// <summary>
        /// For each unique resource across all surveys, selects the entry with
        /// the highest refined output rate.
        /// </summary>
        private List<BestResourceEntry> SelectBestResources(List<Survey> surveys)
        {
            var candidates = new Dictionary<string, BestResourceEntry>();

            foreach (var survey in surveys)
            {
                foreach (var resource in survey.Resources.Values)
                {
                    if (string.IsNullOrEmpty(resource.Resource)) continue;
                    if (resource.Purity == GameConstants.PurityRefined) continue; // Skip already-refined

                    decimal amount;
                    if (!decimal.TryParse(resource.Amount, out amount)) continue;

                    decimal adjustedRate = SkillBonusCalculator.GetAdjustedMiningRate(amount, _extractionFocusLevel);
                    int refiningMultiplier = SkillBonusCalculator.GetPurityMultiplier(resource.Purity);
                    decimal refinedOutput = adjustedRate * refiningMultiplier;

                    BestResourceEntry existing;
                    if (!candidates.TryGetValue(resource.Resource, out existing) ||
                        refinedOutput > existing.RefinedOutputRate)
                    {
                        candidates[resource.Resource] = new BestResourceEntry
                        {
                            ResourceName = resource.Resource,
                            Purity = resource.Purity,
                            RawAmount = amount,
                            RefinedOutputRate = refinedOutput,
                            SurveyUUID = survey.UUID
                        };
                    }
                }
            }

            return candidates.Values.ToList();
        }

        private Blueprint FindPlayerBlueprint(string blueprintTypeId)
        {
            return _playerContext.GetAllBlueprints()
                .FirstOrDefault(bp => bp.BluePrintType == blueprintTypeId && bp.UUID != null);
        }

        private ColonyStructure CreateStructure(string flatpackBlueprintUUID)
        {
            return new ColonyStructure
            {
                UUID = Guid.NewGuid().ToString(),
                FlatpackBlueprintUUID = flatpackBlueprintUUID
            };
        }

        private class BestResourceEntry
        {
            public string ResourceName { get; set; }
            public string Purity { get; set; }
            public decimal RawAmount { get; set; }
            public decimal RefinedOutputRate { get; set; }
            public string SurveyUUID { get; set; }
        }
    }
}
