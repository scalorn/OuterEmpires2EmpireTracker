using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Models
{
    public class Colony
    {
        public const int ReadLockTimeoutMs = 1000;

        public const int WriteLockTimeoutMs = 5000;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public Colony() : base()
        {
            Items = new ItemBag();
            Structures = new List<ColonyStructure>();
            Commodities = new List<CommodityRequested>();
            Locks = new OE2EmpireTracker.Models.LockTracking();
        }

        public string UUID { get; set; }

        public string OwnerUUID { get; set; } = string.Empty;

        [DefaultValue(null)]
        public string LegacyUUID { get; set; }

        public string PlanetName { get; set; }

        public string SystemName { get; set; } = string.Empty;

        public string ColonyName { get; set; }

        public ItemBag Items { get; set; }

        public List<ColonyStructure> Structures { get; set; }

        public List<CommodityRequested> Commodities { get; set; }

        public string LastImportDateTime { get; set; }

        public OE2EmpireTracker.Models.LockTracking Locks { get; set; }

        [JsonIgnore]
        public ReaderWriterLockSlim ColonyLock { get; } = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        public bool HasExpiredTimers()
        {
            foreach (var structure in Structures)
            {
                if (structure.BuildCompletionTime != null &&
                    structure.BuildCompletionTime.TimeRemaining <= 0)
                {
                    return true;
                }

                if (structure.ProcessCompletionTime != null &&
                    (structure.ProcessCompletionTime.IntervalsPassed > 0 ||
                     (!structure.ProcessCompletionTime.IsRepeating && structure.ProcessCompletionTime.TimeRemaining <= 0)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Assigns sequential BuildQueueSequence values (1-based) to all structures
        /// based on their current list position. Call after import, optimizer, or any
        /// operation that establishes a new structure order.
        /// </summary>
        public void StampBuildQueueSequence()
        {
            for (int i = 0; i < Structures.Count; i++)
            {
                Structures[i].BuildQueueSequence = i + 1;
            }

            Log.Info(
                "StampBuildQueueSequence: stamped {0} structures for colony {1}",
                Structures.Count,
                UUID ?? "(no UUID)");
        }

        public void ProcessColony()
        {
            // Processing order per REQ-COL-100 / REQ-ARCH-080:
            // 1. Structure Building
            // 2. Mining
            // 3. Refining Base Resources (tier 0)
            // 4. Refining S1 Synthetics (tier 1)
            // 5. Refining S2 Synthetics (tier 2)
            // 6. Manufacturing + Commodity Manufacturing
            // 7. Research
            // This ordering ensures mined resources are available for refining,
            // and refined resources are available for manufacturing in the same cycle.

            var pc = PlayerContext.GetInstance();

            // Step 0: Clean up orphaned manufacturing state on structures whose
            // blueprint type doesn't support it (e.g. mining rig with stale
            // ManufacturingBlueprintUUID from a data import or prior bug)
            foreach (ColonyStructure structure in Structures)
            {
                Blueprint bp = pc.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null) continue;

                bool isManufactory = bp.BluePrintType == BlueprintTypes.Manufactory;
                bool isCommodityFactory = bp.BluePrintType.IsCommodityFactory();
                bool isResearchLab = bp.BluePrintType == BlueprintTypes.ResearchLaboratory;

                if (!isManufactory && !string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                {
                    Log.Warn(
                        "Clearing orphaned ManufacturingBlueprintUUID on {0} (type={1})",
                        structure.UUID,
                        bp.BluePrintType);
                    structure.ManufacturingBlueprintUUID = null;
                    structure.ManufacturingQuantity = 0;
                    structure.ManufacturingCompleted = 0;
                    structure.StagingResources = false;
                }

                // Also clear if the referenced manufacturing blueprint doesn't exist
                if (isManufactory && !string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)
                    && pc.FindBlueprint(structure.ManufacturingBlueprintUUID) == null)
                {
                    Log.Warn(
                        "Clearing ManufacturingBlueprintUUID on {0} (blueprint {1} not found)",
                        structure.UUID,
                        structure.ManufacturingBlueprintUUID);
                    structure.ManufacturingBlueprintUUID = null;
                    structure.ManufacturingQuantity = 0;
                    structure.ManufacturingCompleted = 0;
                    structure.StagingResources = false;
                    if (structure.ProcessCompletionTime != null)
                        structure.ProcessCompletionTime = null;
                }

                if (!isCommodityFactory && !string.IsNullOrEmpty(structure.ManufacturingCommodityName))
                {
                    Log.Warn(
                        "Clearing orphaned ManufacturingCommodityName on {0} (type={1})",
                        structure.UUID,
                        bp.BluePrintType);
                    structure.ManufacturingCommodityName = null;
                    structure.ManufacturingQuantity = 0;
                    structure.ManufacturingCompleted = 0;
                    structure.StagingResources = false;
                }

                if (!isResearchLab && !string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                {
                    Log.Warn(
                        "Clearing orphaned ResearchingBlueprintUUID on {0} (type={1})",
                        structure.UUID,
                        bp.BluePrintType);
                    structure.ResearchingBlueprintUUID = null;
                }

                if (isResearchLab && !string.IsNullOrEmpty(structure.ResearchingBlueprintUUID)
                    && pc.FindBlueprint(structure.ResearchingBlueprintUUID) == null)
                {
                    Log.Warn(
                        "Clearing ResearchingBlueprintUUID on {0} (blueprint {1} not found)",
                        structure.UUID,
                        structure.ResearchingBlueprintUUID);
                    structure.ResearchingBlueprintUUID = null;
                    if (structure.ProcessCompletionTime != null)
                        structure.ProcessCompletionTime = null;
                }

                // Clear ProcessCompletionTime on manufactories/commodity factories/research labs
                // that have a timer but no active job
                if (isManufactory && string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)
                    && structure.ProcessCompletionTime != null)
                {
                    Log.Warn("Clearing orphaned ProcessCompletionTime on manufactory {0}", structure.UUID);
                    structure.ProcessCompletionTime = null;
                }

                if (isCommodityFactory && string.IsNullOrEmpty(structure.ManufacturingCommodityName)
                    && structure.ProcessCompletionTime != null)
                {
                    Log.Warn("Clearing orphaned ProcessCompletionTime on commodity factory {0}", structure.UUID);
                    structure.ProcessCompletionTime = null;
                }

                if (isResearchLab && string.IsNullOrEmpty(structure.ResearchingBlueprintUUID)
                    && structure.ProcessCompletionTime != null)
                {
                    Log.Warn("Clearing orphaned ProcessCompletionTime on research lab {0}", structure.UUID);
                    structure.ProcessCompletionTime = null;
                }
            }

            // Step 1: Structure Building -- check BuildCompletionTime expiration
            foreach (ColonyStructure structure in Structures)
            {
                if (structure.BuildCompletionTime != null &&
                    structure.BuildCompletionTime.TimeRemaining <= 0)
                {
                    structure.Properties.SetProperty(GameConstants.PropBuilt, true);
                    structure.Properties.SetProperty(GameConstants.PropStaged, false);
                    structure.BuildCompletionTime = null;
                }
            }

            // Build a list of ready structures with their blueprints for steps 2-7
            var ready = new List<(ColonyStructure structure, Blueprint blueprint)>();
            foreach (ColonyStructure structure in Structures)
            {
                if (structure.ProcessCompletionTime != null &&
                    (structure.ProcessCompletionTime.IntervalsPassed > 0 ||
                     (!structure.ProcessCompletionTime.IsRepeating && structure.ProcessCompletionTime.TimeRemaining <= 0)))
                {
                    Blueprint bp = pc.FindBlueprint(structure.FlatpackBlueprintUUID);
                    if (bp == null)
                    {
                        Log.Warn(
                            "ProcessColony: blueprint not found for structure {0} (FlatpackBP={1}), skipping",
                            structure.UUID,
                            structure.FlatpackBlueprintUUID ?? "(null)");
                        continue;
                    }

                    ready.Add((structure, bp));
                }
            }

            // Step 2: Mining
            foreach (var (structure, bp) in ready)
                if (bp.BluePrintType == BlueprintTypes.MiningRig)
                    ProcessMiningRig(structure);

            // Steps 3-5: Refining in tier order (base=0, S1=1, S2=2)
            foreach (var (structure, bp) in ready
                .Where(r => r.blueprint.BluePrintType == BlueprintTypes.Refinery)
                .OrderBy(r => RefiningRecipes.GetTier(r.structure.RefiningResource, r.structure.RefiningResourcePurity)))
                ProcessRefinery(structure);

            // Step 6: Manufacturing and Commodity Manufacturing
            foreach (var (structure, bp) in ready)
                if (bp.BluePrintType == BlueprintTypes.Manufactory)
                    ProcessManufactory(structure);
            foreach (var (structure, bp) in ready)
                if (bp.BluePrintType.IsCommodityFactory())
                    ProcessCommodityFactory(structure);

            // Step 7: Research
            foreach (var (structure, bp) in ready)
                if (bp.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    ProcessResearchLab(structure);
        }

        /// <summary>
        /// Returns the owner's skill level for the given skill, or 0 if no owner.
        /// </summary>
        private int GetOwnerSkillLevel(SkillName skill)
        {
            if (string.IsNullOrEmpty(OwnerUUID)) return 0;
            PlayerContext pc = PlayerContext.GetInstance();
            var owner = pc.PlayerProfileList.FirstOrDefault(p => p.UUID == OwnerUUID);
            if (owner == null) return 0;
            return owner.GetSkill(skill).Level;
        }

        private void ProcessMiningRig(ColonyStructure structure)
        {
            Survey survey = PlayerContext.GetInstance().FindSurvey(structure.MiningSurvey);
            if (survey == null)
            {
                Log.Warn(
                    "ProcessMiningRig: survey {0} not found for structure {1}, skipping",
                    structure.MiningSurvey ?? "(null)",
                    structure.UUID);
                return;
            }

            SurveyResource surveyResource;
            if (string.IsNullOrEmpty(structure.MiningSurveyResource) ||
                !survey.Resources.TryGetValue(structure.MiningSurveyResource, out surveyResource))
            {
                Log.Warn(
                    "ProcessMiningRig: resource '{0}' not found in survey {1} for structure {2}, skipping",
                    structure.MiningSurveyResource ?? "(null)",
                    structure.MiningSurvey,
                    structure.UUID);
                return;
            }

            List<Item> items = Items.FindResource(surveyResource.Resource, surveyResource.Purity);
            int quantityInt = 0;
            decimal leftOver = structure.MiningLeftOvers;
            Item item = null;
            if (items.Count > 0)
            {
                item = items[0];
            }
            else
            {
                item = new Item();
                item.UUID = Guid.NewGuid().ToString();
                item.ItemType = ItemType.ItemTypeEnum.Resource;
                item.BaseItemTypeID = surveyResource.Resource;
                item.Name = surveyResource.Resource;
                item.ResourcePurity = surveyResource.Purity;
                item.Quantity = 0;
                Items.AddItem(item);
            }

            // ExtractionFocus: +1% per level
            decimal extractionMultiplier = 1.0m + (GetOwnerSkillLevel(SkillName.ExtractionFocus) * GameConstants.ExtractionFocusRatePerLevel);

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                decimal quantity = (decimal.Parse(surveyResource.Amount) * extractionMultiplier) + leftOver;

                quantityInt += (int)quantity;

                leftOver += quantity - quantityInt;
                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }

            item.Quantity += quantityInt;
            structure.MiningLeftOvers = leftOver;

            Log.Info(
                "ProcessMiningRig: structure={0} resource={1} ({2}) mined={3} newQty={4} leftOver={5:F4}",
                structure.UUID,
                surveyResource.Resource,
                surveyResource.Purity,
                quantityInt,
                item.Quantity,
                leftOver);
        }

        private void ProcessRefinery(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.RefiningResource) ||
                string.IsNullOrEmpty(structure.RefiningResourcePurity))
                return;

            // Check for synthetic recipe
            var recipe = RefiningRecipes.FindByInput(structure.RefiningResource, structure.RefiningResourcePurity);

            if (recipe != null)
            {
                ProcessSyntheticRefinery(structure, recipe);
            }
            else
            {
                ProcessNormalRefinery(structure);
            }
        }

        private void ProcessNormalRefinery(ColonyStructure structure)
        {
            int baseRate = GameConstants.RefiningBaseRate;
            // RefiningFocus: +2% per level
            decimal refiningMultiplier = 1.0m + (GetOwnerSkillLevel(SkillName.RefiningFocus) * GameConstants.RefiningFocusRatePerLevel);
            int outputMultiplier;
            switch (structure.RefiningResourcePurity)
            {
                case GameConstants.PurityLow: outputMultiplier = GameConstants.PurityMultiplierLow; break;
                case GameConstants.PurityMedium: outputMultiplier = GameConstants.PurityMultiplierMedium; break;
                case GameConstants.PurityHigh: outputMultiplier = GameConstants.PurityMultiplierHigh; break;
                default: outputMultiplier = GameConstants.PurityMultiplierLow; break;
            }

            List<Item> sourceItems = Items.FindResource(structure.RefiningResource, structure.RefiningResourcePurity);

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                int available = 0;
                Item sourceItem = null;
                if (sourceItems.Count > 0)
                {
                    sourceItem = sourceItems[0];
                    available = sourceItem.Quantity;
                }

                int consumed = Math.Min(baseRate, available);
                if (consumed <= 0)
                {
                    structure.ProcessCompletionTime.ConsumeIntervals(1);
                    continue;
                }

                sourceItem.Quantity -= consumed;
                if (sourceItem.Quantity <= 0)
                {
                    Items.Remove(sourceItem.UUID);
                    sourceItems.Remove(sourceItem);
                }

                int produced = (int)(consumed * outputMultiplier * refiningMultiplier);
                List<Item> refinedItems = Items.FindResource(structure.RefiningResource, GameConstants.PurityRefined);
                Item refinedItem;
                if (refinedItems.Count > 0)
                {
                    refinedItem = refinedItems[0];
                }
                else
                {
                    refinedItem = new Item();
                    refinedItem.UUID = Guid.NewGuid().ToString();
                    refinedItem.ItemType = ItemType.ItemTypeEnum.Resource;
                    refinedItem.BaseItemTypeID = structure.RefiningResource;
                    refinedItem.Name = structure.RefiningResource;
                    refinedItem.ResourcePurity = GameConstants.PurityRefined;
                    refinedItem.Volume = 1;
                    refinedItem.Quantity = 0;
                    Items.AddItem(refinedItem);
                }

                refinedItem.Quantity += produced;

                Log.Info(
                    "ProcessNormalRefinery: structure={0} consumed={1} {2} ({3}) produced={4} {5} (Refined) newQty={6}",
                    structure.UUID,
                    consumed,
                    structure.RefiningResource,
                    structure.RefiningResourcePurity,
                    produced,
                    structure.RefiningResource,
                    refinedItem.Quantity);

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
        }

        private void ProcessSyntheticRefinery(ColonyStructure structure, RefiningRecipe recipe)
        {
            List<Item> sourceItems = Items.FindResource(recipe.InputResource, recipe.InputPurity);

            // Per-unit cost: how many input resources per 1 output unit
            int perUnitCost = recipe.ConsumeRate / recipe.ProduceRate;
            // RefiningFocus: +2% per level
            decimal refiningMultiplier = 1.0m + (GetOwnerSkillLevel(SkillName.RefiningFocus) * GameConstants.RefiningFocusRatePerLevel);

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                int available = 0;
                Item sourceItem = null;
                if (sourceItems.Count > 0)
                {
                    sourceItem = sourceItems[0];
                    available = sourceItem.Quantity;
                }

                // Only consume whole units / floor to nearest multiple of perUnitCost
                int wholeUnits = available / perUnitCost;
                int maxUnits = recipe.ProduceRate; // cap at full batch size
                int produced = (int)(Math.Min(wholeUnits, maxUnits) * refiningMultiplier);

                if (produced <= 0)
                {
                    structure.ProcessCompletionTime.ConsumeIntervals(1);
                    continue;
                }

                int consumed = produced * perUnitCost;
                sourceItem.Quantity -= consumed;
                if (sourceItem.Quantity <= 0)
                {
                    Items.Remove(sourceItem.UUID);
                    sourceItems.Remove(sourceItem);
                }

                if (produced > 0)
                {
                    List<Item> outputItems = Items.FindResource(recipe.OutputResource, GameConstants.PurityRefined);
                    Item outputItem;
                    if (outputItems.Count > 0)
                    {
                        outputItem = outputItems[0];
                    }
                    else
                    {
                        outputItem = new Item();
                        outputItem.UUID = Guid.NewGuid().ToString();
                        outputItem.ItemType = ItemType.ItemTypeEnum.Resource;
                        outputItem.BaseItemTypeID = recipe.OutputResource;
                        outputItem.Name = recipe.OutputResource;
                        outputItem.ResourcePurity = GameConstants.PurityRefined;
                        outputItem.Volume = 1;
                        outputItem.Quantity = 0;
                        Items.AddItem(outputItem);
                    }

                    outputItem.Quantity += produced;
                }

                Log.Info(
                    "ProcessSyntheticRefinery: structure={0} consumed={1} {2} ({3}) produced={4} {5} (Refined) tier=S{6}",
                    structure.UUID,
                    produced * (recipe.ConsumeRate / recipe.ProduceRate),
                    recipe.InputResource,
                    recipe.InputPurity,
                    produced,
                    recipe.OutputResource,
                    recipe.Tier);

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
        }

        private void ProcessResearchLab(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                return;

            PlayerContext pc = PlayerContext.GetInstance();
            Blueprint sourceBp = pc.FindBlueprint(structure.ResearchingBlueprintUUID);
            if (sourceBp == null)
                return;

            // Research is a one-shot timer / check if time has expired
            if (structure.ProcessCompletionTime.TimeRemaining > 0)
                return;

            // Create the evolved blueprint
            Blueprint newBp = new Blueprint(sourceBp.Name);
            newBp.UUID = System.Guid.NewGuid().ToString();
            newBp.BluePrintType = sourceBp.BluePrintType;
            newBp.Class = sourceBp.Class;
            newBp.TechLevel = sourceBp.TechLevel;
            newBp.Evolution = sourceBp.Evolution + 1;
            newBp.CopyCost = sourceBp.CopyCost;
            newBp.BaseBlueprintUUID = sourceBp.BaseBlueprintUUID;
            newBp.Description = sourceBp.Description;
            newBp.NickName = "NEEDS SCANNED"; // Searchable marker for unscanned blueprints
            // Properties copied, resources left empty for user to import
            foreach (var prop in sourceBp.Properties.Properties)
            {
                newBp.Properties.SetProperty(prop.Key, prop.Value);
            }

            // Resources intentionally empty / user imports via Blueprint Form

            // Add to player's blueprint list
            pc.AddBlueprint(newBp);

            // Add an item to the colony warehouse
            Item bpItem = new Item(ItemType.ItemTypeEnum.Blueprint, newBp.Name);
            bpItem.UUID = System.Guid.NewGuid().ToString();
            bpItem.BaseItemTypeID = newBp.UUID;
            bpItem.Quantity = 1;
            bpItem.Volume = 0;
            Items.AddItem(bpItem);

            Log.Info(
                "ProcessResearchLab: structure={0} evolved {1} evo {2}->{3} newBpUUID={4}",
                structure.UUID,
                sourceBp.Name,
                sourceBp.Evolution,
                newBp.Evolution,
                newBp.UUID);

            structure.ProcessCompletionTime.ConsumeIntervals(1);
        }

        private void ProcessManufactory(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                return;

            PlayerContext pc = PlayerContext.GetInstance();
            Blueprint sourceBp = pc.FindBlueprint(structure.ManufacturingBlueprintUUID);
            if (sourceBp == null)
                return;

            // Find the output item type from the BlueprintType
            EmpireContext ec = EmpireContext.GetInstance();
            BlueprintType bpType = ec.FindBlueprintType(sourceBp.BluePrintType);
            ItemType.ItemTypeEnum outputType = ItemType.ItemTypeEnum.None;
            if (bpType != null && !string.IsNullOrEmpty(bpType.OutputItemType))
            {
                Enum.TryParse(bpType.OutputItemType, out outputType);
            }

            // Process each completed interval / one item per interval
            while (structure.ProcessCompletionTime.IntervalsPassed > 0 &&
                   structure.ManufacturingCompleted < structure.ManufacturingQuantity)
            {
                structure.ManufacturingCompleted++;

                // Stack with existing item if same type and blueprint
                List<Item> existing = Items.FindByType(outputType, sourceBp.UUID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity++;
                }
                else
                {
                    Item mfgItem = new Item(outputType, sourceBp.Name);
                    mfgItem.UUID = Guid.NewGuid().ToString();
                    mfgItem.BaseItemTypeID = sourceBp.UUID;
                    mfgItem.Quantity = 1;

                    decimal vol = 0m;
                    sourceBp.Properties.GetDecimal(BlueprintPropertyKeys.CargoVolumeSize, 0m, out vol);
                    mfgItem.Volume = vol;

                    Items.AddItem(mfgItem);
                }

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }

            // Clear timer when all items are complete
            if (structure.ManufacturingCompleted >= structure.ManufacturingQuantity)
            {
                structure.ProcessCompletionTime = null;
            }

            Log.Info(
                "ProcessManufactory: structure={0} blueprint={1} completed={2}/{3}",
                structure.UUID,
                sourceBp.ExtendedName,
                structure.ManufacturingCompleted,
                structure.ManufacturingQuantity);
        }

        private void ProcessCommodityFactory(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingCommodityName))
                return;

            Commodity commodity;
            if (!Commodity.ResourceMapByString.TryGetValue(structure.ManufacturingCommodityName, out commodity))
                return;

            // Process each completed cycle -- 10 commodities per cycle
            while (structure.ProcessCompletionTime.IntervalsPassed > 0 &&
                   structure.ManufacturingCompleted < structure.ManufacturingQuantity)
            {
                structure.ManufacturingCompleted++;

                // Stack with existing commodity item
                List<Item> existing = Items.FindByType(ItemType.ItemTypeEnum.Commodity, commodity.Name);
                if (existing.Count > 0)
                {
                    existing[0].Quantity += GameConstants.CommoditiesPerCycle;
                }
                else
                {
                    Item commodityItem = new Item(ItemType.ItemTypeEnum.Commodity, commodity.Name);
                    commodityItem.UUID = Guid.NewGuid().ToString();
                    commodityItem.BaseItemTypeID = commodity.Name;
                    commodityItem.Quantity = GameConstants.CommoditiesPerCycle;
                    commodityItem.Volume = 10;
                    Items.AddItem(commodityItem);
                }

                // Consume construction resources
                foreach (var resource in commodity.ConstructionResources)
                {
                    string resourceName = resource.Key;
                    int perCycle = 0;
                    int.TryParse(resource.Value, out perCycle);
                    if (perCycle <= 0) continue;

                    List<Item> sourceItems = Items.FindResource(resourceName, GameConstants.PurityRefined);
                    if (sourceItems.Count > 0)
                    {
                        sourceItems[0].Quantity -= perCycle;
                        if (sourceItems[0].Quantity <= 0)
                        {
                            Items.Remove(sourceItems[0].UUID);
                        }
                    }
                }

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }

            // Clear timer when all cycles are complete
            if (structure.ManufacturingCompleted >= structure.ManufacturingQuantity)
            {
                structure.ProcessCompletionTime = null;
            }

            Log.Info(
                "ProcessCommodityFactory: structure={0} commodity={1} completed={2}/{3}",
                structure.UUID,
                structure.ManufacturingCommodityName,
                structure.ManufacturingCompleted,
                structure.ManufacturingQuantity);
        }
    }
}
