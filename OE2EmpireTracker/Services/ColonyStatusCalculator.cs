using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Calculates the resource status (Power, Habitation, Food, Entertainment, Warehouse) 
    /// for a specific colony based on its structures and assigned workers.
    /// </summary>
    /// <remarks>
    /// <para>This class aggregates resource provisioning from built/online structures against 
    /// the requirements imposed by colony workers and structure demands.</para>
    /// <para>It relies on <see cref="EmpireContext"/> to retrieve blueprint definitions and player data.</para>
    /// </remarks>
    public class ColonyStatusCalculator
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The global context for the empire tracker instance. Used to access game engine states.
        /// </summary>
        private EmpireContext empireContext;

        /// <summary>
        /// The player context required to fetch blueprint definitions and player data.
        /// </summary>
        private PlayerContext playerContext;

        /// <summary>
        /// The current colony instance being calculated.
        /// </summary>
        private Colony colony;

        public ColonyStructureStatus finalActualStatus;
        public ColonyStructureStatus finalIdealStatus;

        /// <summary>
        /// Collection of workers assigned to structures within this colony.
        /// Populated during <see cref="CalculateBuilt"/>.
        /// </summary>
        //public List<ColonyWorker> ColonyWorkers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColonyStatusCalculator"/> class for a specific colony.
        /// Sets up references to global contexts and initializes the worker list.
        /// </summary>
        /// <param name="colony">The Colony instance to calculate status for.</param>
        public ColonyStatusCalculator(Colony colony)
        {
            this.colony = colony;
            // Uses Singleton pattern access to retrieve contexts from the global state.
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            //ColonyWorkers = new List<ColonyWorker>();
        }
        /// <summary>
        /// Calculates the resource status and worker assignments for the tracked colony.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item><description>Calls <c>CalculateBuilt</c> to aggregate stats.</description></item>
        /// <item><description>Resets all resource counters (Provided/Required) to 0.</description></item>
        /// <item><description>Clears existing worker list before repopulating.</description></item>
        /// </list>
        /// <para>The method iterates through every structure in the colony:</para>
        /// <list type="number">
        /// <item><description>Checks if a Blueprint exists for the structure.</description></item>
        /// <item><description>Evaluates properties to determine if a structure is 'Online' and 'Built'.</description></item>
        /// <item><description>Sums up Power, Habitation, Food, Entertainment, and Warehouse stats based on online status.</description></item>
        /// <item><description>Parses worker assignment details (Blue/White Collar, Specialists) from blueprint properties 
        /// and validates them against the structure's assigned workers list.</description></item>
        /// </list>
        /// <para>At the end of the loop:</para>
        /// <list type="bullet">
        /// <item><description><c>Required</c> values are derived from worker counts (based on current implementation logic).</description></item>
        /// <item><description><c>Provided/Capacity</c> values are the accumulated sums from online structures.</description></item>
        /// </list>
        /// </remarks>
        public void CalculateBuilt()
        {
            var workers = new ActualColonyStructureWorkers(colony);
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();
            Dictionary<string, int> StructureCounts = new Dictionary<string, int>();

            // Clear all existing worker locks -- will be rebuilt from current state
            ClearAllWorkerLocks();

            foreach (ColonyStructure structure in colony.Structures)
            {
                Models.Blueprint FlatpackBlueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (FlatpackBlueprint != null)
                {
                    int count = 0;
                    StructureCounts.TryGetValue(FlatpackBlueprint.BluePrintType, out count);
                    count++;
                    StructureCounts[FlatpackBlueprint.BluePrintType] = count;
                    structure.displaySequence = count;

                    // Lock assigned workers for this structure
                    LockAssignedWorkers(structure, FlatpackBlueprint);

                    // Lock manufacturing resources for active manufactories
                    LockManufacturingResources(structure, FlatpackBlueprint);

                    // Lock commodity factory resources
                    LockCommodityFactoryResources(structure);

                    // Lock flatpack for staged structures
                    LockStagedFlatpack(structure);
                }

                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, FlatpackBlueprint);
                structure.Statuses[GameConstants.StatusActual] = currentStatus;
                previousStatus = currentStatus;
            }
            finalActualStatus = previousStatus;
            finalActualStatus.WarehouseRequired = CalculateWarehouseRequired();

            // Lock unallocated workers against the colony
            LockUnallocatedWorkers(previousStatus);
        }

        public void CalculateIdeal()
        {
            var workers = new IdealColonyStructureWorkers();
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();

            foreach (ColonyStructure structure in colony.Structures)
            {
                Models.Blueprint FlatpackBlueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, FlatpackBlueprint);
                structure.Statuses[GameConstants.StatusIdeal] = currentStatus;
                previousStatus = currentStatus;
            }
            finalIdealStatus = previousStatus;
            finalIdealStatus.WarehouseRequired = CalculateWarehouseRequired();
        }

        private decimal CalculateWarehouseRequired()
        {
            decimal total = 0m;
            foreach (var item in colony.Items.Items.Values)
            {
                total += item.Quantity * item.Volume;
            }
            return total;
        }

        // -----------------------------------------------------------------------
        // Worker Lock Management
        // -----------------------------------------------------------------------

        private void ClearAllWorkerLocks()
        {
            if (colony.Locks == null) return;

            // Clear locks for each structure
            foreach (var structure in colony.Structures)
            {
                if (!string.IsNullOrEmpty(structure.UUID))
                    colony.Locks.ClearLocksForProcess(structure.UUID);
            }
            // Clear unallocated worker locks for the colony
            if (!string.IsNullOrEmpty(colony.UUID))
                colony.Locks.ClearLocksForProcess(colony.UUID);
        }

        private void LockAssignedWorkers(ColonyStructure structure, Models.Blueprint flatpackBlueprint)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;

            foreach (var wt in Models.WorkerDetail.WorkerTypes)
            {
                LockWorkerType(structure, flatpackBlueprint, wt);
            }
        }

        private void LockWorkerType(ColonyStructure structure, Models.Blueprint flatpackBlueprint,
            Models.WorkerTypeInfo wt)
        {
            if (!flatpackBlueprint.Properties.ContainsKey(wt.PropertyKey)) return;

            long count = 0;
            flatpackBlueprint.Properties.getLong(wt.PropertyKey, 0, out count);

            for (int i = 1; i <= count; i++)
            {
                string key = wt.WorkerPrefix + i;
                bool assigned = false;
                structure.AssignedWorkers.getBoolean(key, false, out assigned);
                if (assigned)
                {
                    EnsureWorkerItemExists(wt.DetailKey);
                    colony.Locks.LockItem(structure.UUID,
                        Models.ItemType.ItemTypeEnum.WorkDetail, wt.DetailKey, 1);
                }
            }
        }

        private void LockUnallocatedWorkers(ColonyStructureStatus finalStatus)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(colony.UUID)) return;

            if (finalStatus.UnallocatedBlueCollarPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdBlueCollar);
                colony.Locks.LockItem(colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail, GameConstants.WorkerIdBlueCollar, 1);
            }
            if (finalStatus.UnallocatedWhiteCollarPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdWhiteCollar);
                colony.Locks.LockItem(colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail, GameConstants.WorkerIdWhiteCollar, 1);
            }
            if (finalStatus.UnallocatedSpecialistPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdSpecialist);
                colony.Locks.LockItem(colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail, GameConstants.WorkerIdSpecialist, 1);
            }
        }

        private void EnsureWorkerItemExists(string workerDetailID)
        {
            var existing = colony.Items.FindByType(Models.ItemType.ItemTypeEnum.WorkDetail, workerDetailID);
            if (existing.Count == 0)
            {
                var workerItem = new Models.Item(Models.ItemType.ItemTypeEnum.WorkDetail, workerDetailID);
                workerItem.UUID = System.Guid.NewGuid().ToString();
                workerItem.BaseItemTypeID = workerDetailID;
                workerItem.Quantity = 0;
                workerItem.Volume = GameConstants.WorkerVolume;
                colony.Items.AddItem(workerItem);
            }
        }

        // -----------------------------------------------------------------------
        // Manufacturing Resource Lock Management
        // -----------------------------------------------------------------------

        private void LockManufacturingResources(ColonyStructure structure, Models.Blueprint flatpackBlueprint)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;
            if (string.IsNullOrEmpty(structure.ManufacturingBlueprintUUID)) return;
            if (structure.ProcessCompletionTime == null) return;

            Models.Blueprint mfgBlueprint = playerContext.FindBlueprint(structure.ManufacturingBlueprintUUID);
            if (mfgBlueprint == null || mfgBlueprint.Resources == null) return;

            int remaining = structure.ManufacturingQuantity - structure.ManufacturingCompleted;
            if (remaining <= 0) return;

            foreach (var resource in mfgBlueprint.Resources)
            {
                string resourceName = resource.Key;
                int perItem = 0;
                int.TryParse(resource.Value, out perItem);
                if (perItem <= 0) continue;

                int totalToLock = perItem * remaining;

                // Ensure the resource item exists in the warehouse
                var existing = colony.Items.FindResource(resourceName, GameConstants.PurityRefined);
                if (existing.Count == 0)
                {
                    var resourceItem = new Models.Item(Models.ItemType.ItemTypeEnum.Resource, resourceName);
                    resourceItem.UUID = System.Guid.NewGuid().ToString();
                    resourceItem.BaseItemTypeID = resourceName;
                    resourceItem.ResourcePurity = GameConstants.PurityRefined;
                    resourceItem.Quantity = 0;
                    resourceItem.Volume = 1;
                    colony.Items.AddItem(resourceItem);
                }

                colony.Locks.LockItem(structure.UUID,
                    Models.ItemType.ItemTypeEnum.Resource, resourceName, totalToLock);
            }
        }

        private void LockCommodityFactoryResources(ColonyStructure structure)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;
            if (string.IsNullOrEmpty(structure.ManufacturingCommodityName)) return;
            if (structure.ProcessCompletionTime == null) return;

            Models.Commodity commodity;
            if (!Models.Commodity.ResourceMapByString.TryGetValue(structure.ManufacturingCommodityName, out commodity))
                return;

            int remaining = structure.ManufacturingQuantity - structure.ManufacturingCompleted;
            if (remaining <= 0) return;

            foreach (var resource in commodity.ConstructionResources)
            {
                string resourceName = resource.Key;
                int perCycle = 0;
                int.TryParse(resource.Value, out perCycle);
                if (perCycle <= 0) continue;

                int totalToLock = perCycle * remaining;

                var existing = colony.Items.FindResource(resourceName, GameConstants.PurityRefined);
                if (existing.Count == 0)
                {
                    var resourceItem = new Models.Item(Models.ItemType.ItemTypeEnum.Resource, resourceName);
                    resourceItem.UUID = System.Guid.NewGuid().ToString();
                    resourceItem.BaseItemTypeID = resourceName;
                    resourceItem.ResourcePurity = GameConstants.PurityRefined;
                    resourceItem.Quantity = 0;
                    resourceItem.Volume = 1;
                    colony.Items.AddItem(resourceItem);
                }

                colony.Locks.LockItem(structure.UUID,
                    Models.ItemType.ItemTypeEnum.Resource, resourceName, totalToLock);
            }
        }

        private void LockStagedFlatpack(ColonyStructure structure)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;
            if (string.IsNullOrEmpty(structure.FlatpackBlueprintUUID)) return;

            bool isStaged = false;
            structure.Properties.getBoolean(GameConstants.PropStaged, false, out isStaged);
            if (!isStaged) return;

            // Ensure a flatpack item exists in the warehouse
            var existing = colony.Items.FindByType(Models.ItemType.ItemTypeEnum.Flatpack, structure.FlatpackBlueprintUUID);
            if (existing.Count == 0)
            {
                var blueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                var flatpackItem = new Models.Item(Models.ItemType.ItemTypeEnum.Flatpack, blueprint?.ExtendedName ?? "Flatpack");
                flatpackItem.UUID = System.Guid.NewGuid().ToString();
                flatpackItem.BaseItemTypeID = structure.FlatpackBlueprintUUID;
                flatpackItem.Quantity = 0;
                flatpackItem.Volume = 1;
                colony.Items.AddItem(flatpackItem);
            }

            colony.Locks.LockItem(structure.UUID,
                Models.ItemType.ItemTypeEnum.Flatpack, structure.FlatpackBlueprintUUID, 1);
        }

        public void CalculateBuilt(ColonyStructure structure, ColonyStructureStatus prevStatus, ColonyStructureStatus status, IColonyStructureWorkers workerSource, Models.Blueprint flatpackBlueprint)
        {
            // Aggregators for resource stats
            decimal builtPowerProvided = prevStatus.PowerProvided;
            decimal builtPowerRequired = prevStatus.PowerRequired;
            decimal builtHabitationProvision = prevStatus.HabitationProvision;
            decimal builtHabitationRequired = prevStatus.HabitationRequired;
            decimal builtFoodProvision = prevStatus.FoodProvision;
            decimal builtFoodRequired = prevStatus.FoodRequired;
            decimal builtEntertainmentProvided = prevStatus.EntertainmentProvided;
            decimal builtEntertainmentRequired = prevStatus.EntertainmentRequired;
            decimal builtWarehouseCapacity = prevStatus.WarehouseCapacity;
            decimal builtWarehouseRequired = prevStatus.WarehouseRequired;
            List <ColonyWorker> ColonyWorkers = new List<ColonyWorker>();
            var needUnallocated = new Dictionary<string, bool>();
            foreach (var wt in Models.WorkerDetail.WorkerTypes)
                needUnallocated[wt.DetailKey] = false;

            // Ensure the structure has a unique identifier for lookups
            if (structure.UUID == null || structure.UUID.Length == 0)
            {
                structure.UUID = Guid.NewGuid().ToString();
            }

            bool built = false;
            bool staged = false;
            bool online = false;
            workerSource.GetStructureState(structure, out built, out staged, out online);

            if (flatpackBlueprint != null)
            {
                // --- Resource Accumulation ---
                if (online)
                {
                    builtPowerProvided += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropPowerProvided);
                    builtPowerRequired += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropPowerRequired);
                    builtHabitationProvision += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropHabitationProvision);
                    builtEntertainmentProvided += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropEntertainmentProvided);
                    builtWarehouseCapacity += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropWarehouseCapacity);
                }
                // Food accumulates regardless of online state
                builtFoodProvision += GetBlueprintDecimal(flatpackBlueprint, GameConstants.PropFoodProvision);

                // --- Worker Assignment Parsing ---
                foreach (var wt in Models.WorkerDetail.WorkerTypes)
                {
                    if (flatpackBlueprint.Properties.ContainsKey(wt.PropertyKey))
                    {
                        long count = 0;
                        flatpackBlueprint.Properties.getLong(wt.PropertyKey, 0, out count);
                        for (int i = 1; i <= count; i++)
                        {
                            string key = wt.WorkerPrefix + i;
                            bool assigned = workerSource.IsWorkerAssigned(structure, key);
                            workerSource.SetWorkerAssigned(structure, key, assigned);
                            if (assigned)
                            {
                                ColonyWorkers.Add(new ColonyWorker(structure, key, assigned));
                            }
                        }
                    }
                    long unassignedCount = 0;
                    flatpackBlueprint.Properties.getLong(wt.UnassignedPropertyKey, 0, out unassignedCount);
                    if (unassignedCount > 0)
                    {
                        needUnallocated[wt.DetailKey] = true;
                    }
                }
            }


            int unallocatedWorkersAdded = 0;
            foreach (var wt in Models.WorkerDetail.WorkerTypes)
            {
                bool need = needUnallocated[wt.DetailKey];
                bool alreadyPresent = prevStatus.GetUnallocatedPresent(wt.DetailKey);
                if (need && !alreadyPresent && workerSource.IsUnassignedWorkerAvailable(wt.DetailKey))
                {
                    status.SetUnallocatedPresent(wt.DetailKey, true);
                    unallocatedWorkersAdded++;
                }
                else
                {
                    status.SetUnallocatedPresent(wt.DetailKey, alreadyPresent);
                }
            }

            // Assign aggregated values to public properties. 
            // Note: 'Required' stats are currently derived from the worker count, not direct blueprint sums.
            status.PowerProvided = builtPowerProvided;
            status.PowerRequired = builtPowerRequired;
            status.HabitationProvision = builtHabitationProvision;
            // Habitation required is calculated based on workers in current implementation
            status.HabitationRequired = builtHabitationRequired + ColonyWorkers.Count + unallocatedWorkersAdded;

            status.FoodProvision = builtFoodProvision;
            // Food required is calculated based on workers in current implementation
            status.FoodRequired = builtFoodRequired + ColonyWorkers.Count + unallocatedWorkersAdded;

            status.EntertainmentProvided = builtEntertainmentProvided;
            // Entertainment required is calculated based on workers in current implementation
            status.EntertainmentRequired = builtEntertainmentRequired + ColonyWorkers.Count + unallocatedWorkersAdded;

            status.WarehouseCapacity = builtWarehouseCapacity;
            // Warehouse required is calculated based on workers in current implementation
            status.WarehouseRequired = builtWarehouseRequired;
        }

        private static decimal GetBlueprintDecimal(Models.Blueprint blueprint, string propertyName)
        {
            decimal value = 0m;
            blueprint.Properties.getDecimal(propertyName, 0m, out value);
            return value;
        }

        /// <summary>
        /// Populates a RichTextBox with the colony status summary in a single RTF assignment.
        /// </summary>
        public static void PopulateStatus(RtfBuilder builder, ColonyStructureStatus status)
        {
            AppendStatus(builder, "Power:",
                status.PowerRequired > status.PowerProvided ? Color.Red : Color.Green,
                status.PowerRequired, status.PowerProvided);
            AppendStatus(builder, " Habitation: ",
                status.HabitationProvision < status.HabitationRequired ? Color.Red : Color.Green,
                status.HabitationRequired, status.HabitationProvision);
            AppendStatus(builder, " Food: ",
                status.FoodProvision < status.FoodRequired ? Color.Red : Color.Green,
                status.FoodRequired, status.FoodProvision);
            AppendStatus(builder, " Entertainment: ",
                status.EntertainmentProvided < status.EntertainmentRequired ? Color.Red : Color.Green,
                status.EntertainmentRequired, status.EntertainmentProvided);
            AppendStatus(builder, " Warehouse: ",
                status.WarehouseCapacity < status.WarehouseRequired ? Color.Red : Color.Green,
                status.WarehouseRequired, status.WarehouseCapacity);
        }

        private static void AppendStatus(RtfBuilder builder, string name, Color color, decimal required, decimal provided)
        {
            builder.Append(name, Color.Black);
            builder.Append("" + required, required > provided ? Color.Red : Color.Green);
            builder.Append("/", Color.Black);
            builder.Append("" + provided, Color.Black);
        }
    }
}
