using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;

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

        /// <summary>
        /// Collection of workers assigned to structures within this colony.
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
        }

        public ColonyStructureStatus FinalActualStatus { get; set; }

        public ColonyStructureStatus FinalIdealStatus { get; set; }

        /// <summary>
        /// Populates a RichTextBox with the colony status summary in a single RTF assignment.
        /// </summary>
        public static void PopulateStatus(RtfBuilder builder, ColonyStructureStatus status)
        {
            AppendStatus(
                builder,
                "Power:",
                status.PowerRequired > status.PowerProvided ? Color.Red : Color.Green,
                status.PowerRequired,
                status.PowerProvided);
            AppendStatus(
                builder,
                " Habitation: ",
                status.HabitationProvision < status.HabitationRequired ? Color.Red : Color.Green,
                status.HabitationRequired,
                status.HabitationProvision);
            AppendStatus(
                builder,
                " Food: ",
                status.FoodProvision < status.FoodRequired ? Color.Red : Color.Green,
                status.FoodRequired,
                status.FoodProvision);
            AppendStatus(
                builder,
                " Entertainment: ",
                status.EntertainmentProvided < status.EntertainmentRequired ? Color.Red : Color.Green,
                status.EntertainmentRequired,
                status.EntertainmentProvided);
            AppendStatus(
                builder,
                " Warehouse: ",
                status.WarehouseCapacity < status.WarehouseRequired ? Color.Red : Color.Green,
                status.WarehouseRequired,
                status.WarehouseCapacity);
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
            Dictionary<string, int> structureCounts = new Dictionary<string, int>();
            var blueprintPassCache = new Dictionary<string, Blueprint>();

            // Clear all existing worker locks -- will be rebuilt from current state
            ClearAllWorkerLocks();

            // Invariant check: no structure should have both Staged=true and Built=true
            foreach (var structure in colony.Structures)
            {
                bool staged, built;
                structure.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
                structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
                if (staged && built)
                {
                    Log.Error(
                        "INVARIANT VIOLATION in CalculateBuilt: colony {0} structure {1} has Staged=true AND Built=true — auto-fixing to Staged=false",
                        colony.UUID, structure.UUID);
                    structure.Properties.SetProperty(GameConstants.PropStaged, false);
                }
            }

            // Sort by BuildQueueSequence — never trust the raw list order
            var orderedStructures = CollectionSortHelper.OrderStructures(colony.Structures);

            foreach (ColonyStructure structure in orderedStructures)
            {
                Models.Blueprint flatpackBlueprint = GetCachedBlueprint(structure.FlatpackBlueprintUUID, blueprintPassCache);
                if (flatpackBlueprint != null)
                {
                    int count = 0;
                    structureCounts.TryGetValue(flatpackBlueprint.BluePrintType, out count);
                    count++;
                    structureCounts[flatpackBlueprint.BluePrintType] = count;
                    structure.DisplaySequence = count;

                    // Lock assigned workers for this structure
                    LockAssignedWorkers(structure, flatpackBlueprint);

                    // Lock manufacturing resources for active manufactories
                    LockManufacturingResources(structure, flatpackBlueprint);

                    // Lock commodity factory resources
                    LockCommodityFactoryResources(structure);

                    // Lock flatpack for staged structures
                    LockStagedFlatpack(structure);
                }

                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, flatpackBlueprint);
                structure.Statuses[GameConstants.StatusActual] = currentStatus;

                // Compute and store per-structure delta for incremental recalculation
                structure.StatusDelta = ComputeStructureDelta(structure, flatpackBlueprint);

                previousStatus = currentStatus;
            }

            FinalActualStatus = previousStatus;
            FinalActualStatus.WarehouseRequired = CalculateWarehouseRequired();

            // Cross-check: sum all deltas into FinalActualStatus
            SumAllDeltas();

            // Lock unallocated workers against the colony
            LockUnallocatedWorkers(previousStatus);

            Log.Info(
                "CalculateBuilt ACTUAL: colony={0} structures={1} Power={2}/{3} Hab={4}/{5} Food={6}/{7} Ent={8}/{9} WH={10}/{11}",
                colony.ColonyName ?? colony.PlanetName ?? colony.UUID,
                colony.Structures.Count,
                FinalActualStatus.PowerProvided,
                FinalActualStatus.PowerRequired,
                FinalActualStatus.HabitationProvision,
                FinalActualStatus.HabitationRequired,
                FinalActualStatus.FoodProvision,
                FinalActualStatus.FoodRequired,
                FinalActualStatus.EntertainmentProvided,
                FinalActualStatus.EntertainmentRequired,
                FinalActualStatus.WarehouseCapacity,
                FinalActualStatus.WarehouseRequired);
        }

        public void CalculateIdeal()
        {
            var workers = new IdealColonyStructureWorkers();
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();

            // Sort by BuildQueueSequence — never trust the raw list order
            var orderedStructures = CollectionSortHelper.OrderStructures(colony.Structures);

            foreach (ColonyStructure structure in orderedStructures)
            {
                Models.Blueprint flatpackBlueprint = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, flatpackBlueprint);
                structure.Statuses[GameConstants.StatusIdeal] = currentStatus;
                previousStatus = currentStatus;
            }

            FinalIdealStatus = previousStatus;
            FinalIdealStatus.WarehouseRequired = CalculateWarehouseRequired();

            Log.Info(
                "CalculateIdeal IDEAL: colony={0} structures={1} Power={2}/{3} Hab={4}/{5} Food={6}/{7} Ent={8}/{9} WH={10}/{11}",
                colony.ColonyName ?? colony.PlanetName ?? colony.UUID,
                colony.Structures.Count,
                FinalIdealStatus.PowerProvided,
                FinalIdealStatus.PowerRequired,
                FinalIdealStatus.HabitationProvision,
                FinalIdealStatus.HabitationRequired,
                FinalIdealStatus.FoodProvision,
                FinalIdealStatus.FoodRequired,
                FinalIdealStatus.EntertainmentProvided,
                FinalIdealStatus.EntertainmentRequired,
                FinalIdealStatus.WarehouseCapacity,
                FinalIdealStatus.WarehouseRequired);
        }

        // -----------------------------------------------------------------------
        // Incremental Delta Calculation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Computes what a single structure contributes to colony totals.
        /// Does not modify any colony state -- pure computation.
        /// </summary>
        public StructureStatusDelta ComputeStructureDelta(ColonyStructure structure, Blueprint bp)
        {
            var delta = new StructureStatusDelta();
            if (bp == null) return delta;

            bool built = false, staged = false, online = false;
            structure.Properties.GetBoolean(GameConstants.PropBuilt, false, out built);
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out staged);
            structure.Properties.GetBoolean(GameConstants.PropOnline, false, out online);

            if (online)
            {
                delta.PowerProvided = GetBlueprintDecimal(bp, GameConstants.PropPowerProvided);
                delta.PowerRequired = GetBlueprintDecimal(bp, GameConstants.PropPowerRequired);
                delta.HabitationProvision = GetBlueprintDecimal(bp, GameConstants.PropHabitationProvision);
                delta.EntertainmentProvided = GetBlueprintDecimal(bp, GameConstants.PropEntertainmentProvided);
                delta.WarehouseCapacity = GetBlueprintDecimal(bp, GameConstants.PropWarehouseCapacity);
            }

            // Food accumulates regardless of online state
            delta.FoodProvision = GetBlueprintDecimal(bp, GameConstants.PropFoodProvision);

            // Count assigned workers — only for built structures
            int assignedCount = 0;
            if (built)
            {
            foreach (var wt in WorkerDetail.WorkerTypes)
            {
                if (bp.Properties.ContainsKey(wt.PropertyKey))
                {
                    long count = 0;
                    bp.Properties.GetLong(wt.PropertyKey, 0, out count);
                    for (int i = 1; i <= count; i++)
                    {
                        string key = wt.WorkerPrefix + i;
                        bool assigned = false;
                        structure.AssignedWorkers.GetBoolean(key, false, out assigned);
                        if (assigned)
                            assignedCount++;
                    }
                }
            }
            }

            delta.WorkerCount = assignedCount;

            // Count unallocated workers for this structure — only for built structures.
            // NOTE: UnallocatedCount is set to 0 here because unallocated workers are
            // a colony-wide concept (one per worker type), not per-structure. The inner
            // CalculateBuilt handles unallocated workers correctly via prevStatus tracking.
            // SumAllDeltas should not re-count them from per-structure deltas.
            delta.UnallocatedCount = 0;

            return delta;
        }

        /// <summary>
        /// Sums all per-structure StatusDelta values into FinalActualStatus.
        /// Called after CalculateBuilt() has computed deltas for every structure.
        /// Also sets Habitation/Food/Entertainment Required from worker counts.
        /// </summary>
        public void SumAllDeltas()
        {
            decimal powerProvided = 0m, powerRequired = 0m;
            decimal habitationProvision = 0m, foodProvision = 0m;
            decimal entertainmentProvided = 0m, warehouseCapacity = 0m;
            int totalWorkers = 0, totalUnallocated = 0;

            foreach (var structure in colony.Structures)
            {
                var d = structure.StatusDelta;
                if (d == null) continue;

                powerProvided += d.PowerProvided;
                powerRequired += d.PowerRequired;
                habitationProvision += d.HabitationProvision;
                foodProvision += d.FoodProvision;
                entertainmentProvided += d.EntertainmentProvided;
                warehouseCapacity += d.WarehouseCapacity;
                totalWorkers += d.WorkerCount;
                totalUnallocated += d.UnallocatedCount;
            }

            FinalActualStatus.PowerProvided = powerProvided;
            FinalActualStatus.PowerRequired = powerRequired;
            FinalActualStatus.HabitationProvision = habitationProvision;
            FinalActualStatus.FoodProvision = foodProvision;
            FinalActualStatus.EntertainmentProvided = entertainmentProvided;
            FinalActualStatus.WarehouseCapacity = warehouseCapacity;

            int totalPeople = totalWorkers + totalUnallocated;
            FinalActualStatus.HabitationRequired = totalPeople;
            FinalActualStatus.FoodRequired = totalPeople;
            FinalActualStatus.EntertainmentRequired = totalPeople * 2;
            FinalActualStatus.WarehouseRequired = CalculateWarehouseRequired();
        }

        /// <summary>
        /// Performs an O(1) single-structure status update.
        /// Subtracts the old delta, computes a new one, adds it, and re-locks resources.
        /// </summary>
        public void RecalculateStructure(ColonyStructure structure)
        {
            // If no full calculation has been done yet, do one now
            if (FinalActualStatus == null)
            {
                CalculateBuilt();
                CalculateIdeal();
                return;
            }

            var oldDelta = structure.StatusDelta ?? new StructureStatusDelta();

            Blueprint bp = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
            var newDelta = ComputeStructureDelta(structure, bp);
            structure.StatusDelta = newDelta;

            // Subtract old delta from totals
            FinalActualStatus.PowerProvided -= oldDelta.PowerProvided;
            FinalActualStatus.PowerRequired -= oldDelta.PowerRequired;
            FinalActualStatus.HabitationProvision -= oldDelta.HabitationProvision;
            FinalActualStatus.FoodProvision -= oldDelta.FoodProvision;
            FinalActualStatus.EntertainmentProvided -= oldDelta.EntertainmentProvided;
            FinalActualStatus.WarehouseCapacity -= oldDelta.WarehouseCapacity;

            int oldPeople = oldDelta.WorkerCount + oldDelta.UnallocatedCount;
            FinalActualStatus.HabitationRequired -= oldPeople;
            FinalActualStatus.FoodRequired -= oldPeople;
            FinalActualStatus.EntertainmentRequired -= oldPeople * 2;

            // Add new delta to totals
            FinalActualStatus.PowerProvided += newDelta.PowerProvided;
            FinalActualStatus.PowerRequired += newDelta.PowerRequired;
            FinalActualStatus.HabitationProvision += newDelta.HabitationProvision;
            FinalActualStatus.FoodProvision += newDelta.FoodProvision;
            FinalActualStatus.EntertainmentProvided += newDelta.EntertainmentProvided;
            FinalActualStatus.WarehouseCapacity += newDelta.WarehouseCapacity;

            int newPeople = newDelta.WorkerCount + newDelta.UnallocatedCount;
            FinalActualStatus.HabitationRequired += newPeople;
            FinalActualStatus.FoodRequired += newPeople;
            FinalActualStatus.EntertainmentRequired += newPeople * 2;

            // Re-lock resources for this structure only
            if (colony.Locks != null && !string.IsNullOrEmpty(structure.UUID))
            {
                colony.Locks.ClearLocksForProcess(structure.UUID);
                if (bp != null)
                {
                    LockAssignedWorkers(structure, bp);
                    LockManufacturingResources(structure, bp);
                    LockCommodityFactoryResources(structure);
                    LockStagedFlatpack(structure);
                }
            }
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
            int workerCount = 0;
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
                // Only count workers for built structures. Staged structures have no workers
                // consuming hab/food/ent even if worker data exists in the property bag.
                if (built)
                {
                foreach (var wt in Models.WorkerDetail.WorkerTypes)
                {
                    if (flatpackBlueprint.Properties.ContainsKey(wt.PropertyKey))
                    {
                        long count = 0;
                        flatpackBlueprint.Properties.GetLong(wt.PropertyKey, 0, out count);
                        for (int i = 1; i <= count; i++)
                        {
                            string key = wt.WorkerPrefix + i;
                            bool assigned = workerSource.IsWorkerAssigned(structure, key);
                            workerSource.SetWorkerAssigned(structure, key, assigned);
                            if (assigned)
                            {
                                workerCount++;
                            }
                        }
                    }

                    long unassignedCount = 0;
                    flatpackBlueprint.Properties.GetLong(wt.UnassignedPropertyKey, 0, out unassignedCount);
                    if (unassignedCount > 0)
                    {
                        needUnallocated[wt.DetailKey] = true;
                    }
                }
                } // end if (built)
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
            status.HabitationRequired = builtHabitationRequired + workerCount + unallocatedWorkersAdded;

            status.FoodProvision = builtFoodProvision;
            // Food required is calculated based on workers in current implementation
            status.FoodRequired = builtFoodRequired + workerCount + unallocatedWorkersAdded;

            status.EntertainmentProvided = builtEntertainmentProvided;
            // Entertainment required is 2 per worker (game rule)
            status.EntertainmentRequired = builtEntertainmentRequired + ((workerCount + unallocatedWorkersAdded) * 2);

            // Diagnostic: log per-structure worker accumulation
            var bp = flatpackBlueprint;
            string bpName = bp?.ExtendedName ?? structure.FlatpackBlueprintUUID ?? "?";
            Log.Info(
                "CalcBuilt structure [{0}] built={1} staged={2} online={3} workers={4} unalloc={5} " + "habProv={6} habReq={7} prevHabReq={8} bpType={9}",
                bpName,
                built,
                staged,
                online,
                workerCount,
                unallocatedWorkersAdded,
                status.HabitationProvision,
                status.HabitationRequired,
                builtHabitationRequired,
                bp?.BluePrintType ?? "null");

            status.WarehouseCapacity = builtWarehouseCapacity;
            // Warehouse required is calculated based on workers in current implementation
            status.WarehouseRequired = builtWarehouseRequired;
        }

        private static decimal GetBlueprintDecimal(Models.Blueprint blueprint, string propertyName)
        {
            decimal value = 0m;
            blueprint.Properties.GetDecimal(propertyName, 0m, out value);
            return value;
        }

        private static void AppendStatus(RtfBuilder builder, string name, Color color, decimal required, decimal provided)
        {
            builder.Append(name, Color.Black);
            builder.Append(string.Empty + required, required > provided ? Color.Red : Color.Green);
            builder.Append("/", Color.Black);
            builder.Append(string.Empty + provided, Color.Black);
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
        // Blueprint Cache
        // -----------------------------------------------------------------------

        /// <summary>
        /// Looks up a blueprint from the per-pass cache, falling back to playerContext.FindBlueprint()
        /// and caching the result for subsequent calls within the same calculation pass.
        /// </summary>
        private Blueprint GetCachedBlueprint(string uuid, Dictionary<string, Blueprint> cache)
        {
            if (string.IsNullOrEmpty(uuid)) return null;

            Blueprint bp;
            if (cache.TryGetValue(uuid, out bp))
                return bp;

            bp = playerContext.FindBlueprint(uuid);
            if (bp != null)
                cache[uuid] = bp;
            return bp;
        }

        // -----------------------------------------------------------------------
        // Worker Lock Management
        // -----------------------------------------------------------------------

        private void ClearAllWorkerLocks()
        {
            if (colony.Locks == null) return;

            // Clear ALL locks — they will be rebuilt from current state.
            // This handles orphaned locks from deleted structures.
            colony.Locks = new LockTracking();
        }

        private void LockAssignedWorkers(ColonyStructure structure, Models.Blueprint flatpackBlueprint)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;

            foreach (var wt in Models.WorkerDetail.WorkerTypes)
            {
                LockWorkerType(structure, flatpackBlueprint, wt);
            }
        }

        private void LockWorkerType(
            ColonyStructure structure,
            Models.Blueprint flatpackBlueprint,
            Models.WorkerTypeInfo wt)
        {
            if (!flatpackBlueprint.Properties.ContainsKey(wt.PropertyKey)) return;

            long count = 0;
            flatpackBlueprint.Properties.GetLong(wt.PropertyKey, 0, out count);

            for (int i = 1; i <= count; i++)
            {
                string key = wt.WorkerPrefix + i;
                bool assigned = false;
                structure.AssignedWorkers.GetBoolean(key, false, out assigned);
                if (assigned)
                {
                    EnsureWorkerItemExists(wt.DetailKey);
                    colony.Locks.LockItem(
                        structure.UUID,
                        Models.ItemType.ItemTypeEnum.WorkDetail,
                        wt.DetailKey,
                        1);
                }
            }
        }

        private void LockUnallocatedWorkers(ColonyStructureStatus finalStatus)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(colony.UUID)) return;

            if (finalStatus.UnallocatedBlueCollarPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdBlueCollar);
                colony.Locks.LockItem(
                    colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail,
                    GameConstants.WorkerIdBlueCollar,
                    1);
            }

            if (finalStatus.UnallocatedWhiteCollarPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdWhiteCollar);
                colony.Locks.LockItem(
                    colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail,
                    GameConstants.WorkerIdWhiteCollar,
                    1);
            }

            if (finalStatus.UnallocatedSpecialistPresent)
            {
                EnsureWorkerItemExists(GameConstants.WorkerIdSpecialist);
                colony.Locks.LockItem(
                    colony.UUID,
                    Models.ItemType.ItemTypeEnum.WorkDetail,
                    GameConstants.WorkerIdSpecialist,
                    1);
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

                colony.Locks.LockItem(
                    structure.UUID,
                    Models.ItemType.ItemTypeEnum.Resource,
                    resourceName,
                    totalToLock);
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

                colony.Locks.LockItem(
                    structure.UUID,
                    Models.ItemType.ItemTypeEnum.Resource,
                    resourceName,
                    totalToLock);
            }
        }

        private void LockStagedFlatpack(ColonyStructure structure)
        {
            if (colony.Locks == null || string.IsNullOrEmpty(structure.UUID)) return;
            if (string.IsNullOrEmpty(structure.FlatpackBlueprintUUID)) return;

            bool isStaged = false;
            structure.Properties.GetBoolean(GameConstants.PropStaged, false, out isStaged);
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

            colony.Locks.LockItem(
                structure.UUID,
                Models.ItemType.ItemTypeEnum.Flatpack,
                structure.FlatpackBlueprintUUID,
                1);
        }
    }
}
