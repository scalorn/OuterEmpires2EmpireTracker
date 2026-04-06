using Newtonsoft.Json;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Baseline
{
    public class Colony
    {
        public string UUID { get; set; }
        public string OwnerUUID { get; set; } = string.Empty;
        public string PlanetName { get; set; }
        public string SystemName { get; set; } = string.Empty;
        public string ColonyName { get; set; }
        public ItemBag Items { get; set; }

        public List<ColonyStructure> Structures { get; set; }

        public List<CommodityRequested> Commodities { get; set; }
        public OE2EmpireTracker.Data.LockTracking Locks { get; set; }

        [JsonIgnore]
        public object ProcessingLock { get; } = new object();

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

        public Colony() : base()
        {
            Items = new ItemBag();
            Structures = new List<ColonyStructure>();
            Commodities = new List<CommodityRequested>();
            Locks = new OE2EmpireTracker.Data.LockTracking();
        }

        /// <summary>
        /// Returns the owner's skill level for the given skill, or 0 if no owner.
        /// </summary>
        private int GetOwnerSkillLevel(SkillName skill)
        {
            if (string.IsNullOrEmpty(OwnerUUID)) return 0;
            PlayerContext pc = PlayerContext.getInstance();
            var owner = pc.playerProfileList.FirstOrDefault(p => p.UUID == OwnerUUID);
            if (owner == null) return 0;
            return owner.GetSkill(skill).Level;
        }

        public void ProcessColony()
        {
            // Step 1: Structure Building — check BuildCompletionTime expiration
            foreach (ColonyStructure structure in Structures)
            {
                if (structure.BuildCompletionTime != null &&
                    structure.BuildCompletionTime.TimeRemaining <= 0)
                {
                    structure.Properties.setProperty(GameConstants.PropBuilt, true);
                    structure.Properties.setProperty(GameConstants.PropStaged, false);
                    structure.BuildCompletionTime = null;
                }
            }

            // Step 2: Mining, Refining, Research, Manufacturing, Commodity processing
            var pendingRefineries = new List<ColonyStructure>();

            foreach (ColonyStructure structure in Structures)
            {
                if (structure.ProcessCompletionTime != null &&
                    (structure.ProcessCompletionTime.IntervalsPassed > 0 || 
                     (!structure.ProcessCompletionTime.IsRepeating && structure.ProcessCompletionTime.TimeRemaining <= 0)))
                {
                    Blueprint FlatpackBlueprint = PlayerContext.getInstance().FindBlueprint(structure.FlatpackBlueprintUUID);
                    if (FlatpackBlueprint.BluePrintType == BlueprintTypes.MiningRig)
                    {
                        ProcessMiningRig(structure);
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Refinery)
                    {
                        pendingRefineries.Add(structure);
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.ResearchLaboratory)
                    {
                        ProcessResearchLab(structure);
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.Manufactory)
                    {
                        ProcessManufactory(structure);
                    }
                    else if (FlatpackBlueprint.BluePrintType == BlueprintTypes.CommodityFactory)
                    {
                        ProcessCommodityFactory(structure);
                    }
                }
            }

            // Process refineries in tier order: normal (0) first, then S1 (1), then S2 (2)
            foreach (var structure in pendingRefineries.OrderBy(s =>
                RefiningRecipes.GetTier(s.RefiningResource, s.RefiningResourcePurity)))
            {
                ProcessRefinery(structure);
            }
        }

        private void ProcessMiningRig(ColonyStructure structure)
        {
            Survey survey = PlayerContext.getInstance().FindSurvey(structure.MiningSurvey);
            SurveyResource surveyResource = survey.Resources[structure.MiningSurveyResource];
            List<Item> items = Items.FindResource(surveyResource.Resource, surveyResource.Purity);
            int quantityInt = 0;
            Decimal leftOver = structure.MiningLeftOvers;
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
            double extractionMultiplier = 1.0 + GetOwnerSkillLevel(SkillName.ExtractionFocus) * 0.01;

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                Decimal quantity = (Decimal)(double.Parse(surveyResource.Amount) * extractionMultiplier) + leftOver;

                quantityInt += (int)quantity;

                leftOver += (quantity - quantityInt);
                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
            item.Quantity += quantityInt;
            structure.MiningLeftOvers = leftOver;
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
            double refiningMultiplier = 1.0 + GetOwnerSkillLevel(SkillName.RefiningFocus) * 0.02;
            int outputMultiplier;
            switch (structure.RefiningResourcePurity)
            {
                case "Low": outputMultiplier = 1; break;
                case "Medium": outputMultiplier = 3; break;
                case "High": outputMultiplier = 5; break;
                default: outputMultiplier = 1; break;
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

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
        }

        private void ProcessSyntheticRefinery(ColonyStructure structure, RefiningRecipe recipe)
        {
            List<Item> sourceItems = Items.FindResource(recipe.InputResource, recipe.InputPurity);

            // Per-unit cost: how many input resources per 1 output unit
            int perUnitCost = recipe.ConsumeRate / recipe.ProduceRate;
            // RefiningFocus: +2% per level
            double refiningMultiplier = 1.0 + GetOwnerSkillLevel(SkillName.RefiningFocus) * 0.02;

            while (structure.ProcessCompletionTime.IntervalsPassed > 0)
            {
                int available = 0;
                Item sourceItem = null;
                if (sourceItems.Count > 0)
                {
                    sourceItem = sourceItems[0];
                    available = sourceItem.Quantity;
                }

                // Only consume whole units � floor to nearest multiple of perUnitCost
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

                structure.ProcessCompletionTime.ConsumeIntervals(1);
            }
        }

        private void ProcessResearchLab(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ResearchingBlueprintUUID))
                return;

            PlayerContext pc = PlayerContext.getInstance();
            Blueprint sourceBp = pc.FindBlueprint(structure.ResearchingBlueprintUUID);
            if (sourceBp == null)
                return;

            // Research is a one-shot timer � check if time has expired
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
            newBp.baseBlueprintUUID = sourceBp.baseBlueprintUUID;
            newBp.Description = sourceBp.Description;
            newBp.NickName = "NEEDS SCANNED"; // Searchable marker for unscanned blueprints
            // Properties copied, resources left empty for user to import
            foreach (var prop in sourceBp.Properties.Properties)
            {
                newBp.Properties.setProperty(prop.Key, prop.Value);
            }
            // Resources intentionally empty � user imports via Blueprint Form

            // Add to player's blueprint list
            pc.blueprintList.Add(newBp);

            // Add an item to the colony warehouse
            Item bpItem = new Item(ItemType.ItemTypeEnum.Blueprint, newBp.Name);
            bpItem.UUID = System.Guid.NewGuid().ToString();
            bpItem.BaseItemTypeID = newBp.UUID;
            bpItem.Quantity = 1;
            bpItem.Volume = 0;
            Items.AddItem(bpItem);

            structure.ProcessCompletionTime.ConsumeIntervals(1);
        }

        private void ProcessManufactory(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID))
                return;

            PlayerContext pc = PlayerContext.getInstance();
            Blueprint sourceBp = pc.FindBlueprint(structure.ManufacturingBlueprintUUID);
            if (sourceBp == null)
                return;

            // Find the output item type from the BlueprintType
            EmpireContext ec = EmpireContext.getInstance();
            BlueprintType bpType = ec.FindBlueprintType(sourceBp.BluePrintType);
            ItemType.ItemTypeEnum outputType = ItemType.ItemTypeEnum.None;
            if (bpType != null && !string.IsNullOrEmpty(bpType.OutputItemType))
            {
                Enum.TryParse(bpType.OutputItemType, out outputType);
            }

            // Process each completed interval � one item per interval
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

                    double vol = 0;
                    sourceBp.Properties.getDouble("CargoVolumeSize", 0, out vol);
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
        }

        private void ProcessCommodityFactory(ColonyStructure structure)
        {
            if (string.IsNullOrEmpty(structure.ManufacturingCommodityName))
                return;

            Commodity commodity;
            if (!Commodity.ResourceMapByString.TryGetValue(structure.ManufacturingCommodityName, out commodity))
                return;

            // Process each completed cycle — 10 commodities per cycle
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
        }
    }
}
