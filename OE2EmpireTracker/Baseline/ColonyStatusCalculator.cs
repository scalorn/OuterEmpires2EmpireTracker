using Amazon.Runtime.Internal.Transform;
using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace OE2EmpireTracker.Baseline
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
        private Baseline.Colony colony;

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
        /// <param name="colony">The Baseline.Colony instance to calculate status for.</param>
        public ColonyStatusCalculator(Baseline.Colony colony)
        {
            this.colony = colony;
            // Uses Singleton pattern access to retrieve contexts from the global state.
            empireContext = EmpireContext.getInstance();
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
            var workers = new ActualColonyStructureWorkers();
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();
            Dictionary<string, int> StructureCounts = new Dictionary<string, int>();

            foreach (ColonyStructure structure in colony.Structures)
            {
                Data.Blueprint FlatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);
                if (FlatpackBlueprint != null)
                {
                    int count = 0;
                    StructureCounts.TryGetValue(FlatpackBlueprint.BluePrintType, out count);
                    count++;
                    StructureCounts[FlatpackBlueprint.BluePrintType] = count;
                    structure.gameSequence = count;
                }

                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, FlatpackBlueprint);
                structure.Statuses["Actual"] = currentStatus;
                previousStatus = currentStatus;
            }
            finalActualStatus = previousStatus;
            finalActualStatus.WarehouseRequired = CalculateWarehouseRequired();
        }

        public void CalculateIdeal()
        {
            var workers = new IdealColonyStructureWorkers();
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();

            foreach (ColonyStructure structure in colony.Structures)
            {
                Data.Blueprint FlatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);
                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers, FlatpackBlueprint);
                structure.Statuses["Ideal"] = currentStatus;
                previousStatus = currentStatus;
            }
            finalIdealStatus = previousStatus;
            finalIdealStatus.WarehouseRequired = CalculateWarehouseRequired();
        }

        private double CalculateWarehouseRequired()
        {
            double total = 0;
            foreach (var item in colony.Items.Items.Values)
            {
                total += item.Quantity * item.Volume;
            }
            return total;
        }

        public void CalculateBuilt(ColonyStructure structure, ColonyStructureStatus prevStatus, ColonyStructureStatus status, IColonyStructureWorkers workerSource, Data.Blueprint flatpackBlueprint)
        {
            // Aggregators for resource stats
            double builtPowerProvided = prevStatus.PowerProvided;
            double builtPowerRequired = prevStatus.PowerRequired;
            double builtHabitationProvision = prevStatus.HabitationProvision;
            double builtHabitationRequired = prevStatus.HabitationRequired;
            double builtFoodProvision = prevStatus.FoodProvision;
            double builtFoodRequired = prevStatus.FoodRequired;
            double builtEntertainmentProvided = prevStatus.EntertainmentProvided;
            double builtEntertainmentRequired = prevStatus.EntertainmentRequired;
            double builtWarehouseCapacity = prevStatus.WarehouseCapacity;
            double builtWarehouseRequired = prevStatus.WarehouseRequired;
            bool unallocatedBlueCollarPresent = prevStatus.UnallocatedBlueCollarPresent;
            bool unallocatedWhiteCollarPresent = prevStatus.UnallocatedWhiteCollarPresent;
            bool unallocatedSpecialistPresent = prevStatus.UnallocatedSpecialistPresent;

            List <ColonyWorker> ColonyWorkers = new List<ColonyWorker>();
            bool needUnallocatedBlueCollar = false;
            bool needUnallocatedWhiteCollar = false;
            bool needUnallocatedSpecialist = false;

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
                // --- Power Logic ---
                if (online)
                {
                    double powerProvided = 0;
                    flatpackBlueprint.Properties.getDouble("PowerProvided", 0, out powerProvided);
                    double powerRequired = 0;
                    flatpackBlueprint.Properties.getDouble("PowerRequired", 0, out powerRequired);

                    builtPowerProvided += powerProvided;
                    builtPowerRequired += powerRequired;
                }

                // --- Habitation Logic ---
                if (online)
                {
                    double habitationProvision = 0;
                    flatpackBlueprint.Properties.getDouble("HabitationProvision", 0, out habitationProvision);

                    builtHabitationProvision += habitationProvision;
                }

                // --- Food Logic ---
                {
                    double foodProvision = 0;
                    flatpackBlueprint.Properties.getDouble("FoodProvision", 0, out foodProvision);

                    builtFoodProvision += foodProvision;
                }

                // --- Entertainment Logic ---
                if (online)
                {
                    double entertainmentProvided = 0;
                    flatpackBlueprint.Properties.getDouble("EntertainmentProvided", 0, out entertainmentProvided);

                    builtEntertainmentProvided += entertainmentProvided;
                }

                // --- Warehouse Logic ---
                if (online)
                {
                    double warehouseCapacity = 0;
                    flatpackBlueprint.Properties.getDouble("WarehouseCapacity", 0, out warehouseCapacity);

                    builtWarehouseCapacity += warehouseCapacity;
                }

                // --- Worker Assignment Parsing (Blue Collar) ---
                if (flatpackBlueprint.Properties.ContainsKey("BlueCollarDetail"))
                {
                    long blueCollarDetail = 0;
                    flatpackBlueprint.Properties.getLong("BlueCollarDetail", 0, out blueCollarDetail);
                    for (int i = 1; i <= blueCollarDetail; i++)
                    {
                        string key = "BlueCollar" + i;
                        bool blueCollarAssigned = workerSource.IsWorkerAssigned(structure, key);
                        workerSource.SetWorkerAssigned(structure, key, blueCollarAssigned);
                        if (blueCollarAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, blueCollarAssigned));
                        }
                    }
                }
                long unassignedBlueCollarDetail = 0;
                flatpackBlueprint.Properties.getLong("UnassignedBlueCollarDetail", 0, out unassignedBlueCollarDetail);
                if (unassignedBlueCollarDetail > 0)
                {
                    needUnallocatedBlueCollar = true;
                }

                // --- Worker Assignment Parsing (White Collar) ---
                if (flatpackBlueprint.Properties.ContainsKey("WhiteCollarDetail"))
                {
                    long whiteCollarDetail = 0;
                    flatpackBlueprint.Properties.getLong("WhiteCollarDetail", 0, out whiteCollarDetail);
                    for (int i = 1; i <= whiteCollarDetail; i++)
                    {
                        string key = "WhiteCollar" + i;
                        bool whiteCollarAssigned = workerSource.IsWorkerAssigned(structure, key);
                        workerSource.SetWorkerAssigned(structure, key, whiteCollarAssigned);
                        if (whiteCollarAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, whiteCollarAssigned));
                        }
                    }
                }
                long unassignedWhiteCollarDetail = 0;
                flatpackBlueprint.Properties.getLong("UnassignedWhiteCollarDetail", 0, out unassignedWhiteCollarDetail);
                if (unassignedWhiteCollarDetail > 0)
                {
                    needUnallocatedWhiteCollar = true;
                }

                // --- Worker Assignment Parsing (Specialists) ---
                if (flatpackBlueprint.Properties.ContainsKey("SpecialistDetail"))
                {
                    long specialistDetail = 0;
                    flatpackBlueprint.Properties.getLong("SpecialistDetail", 0, out specialistDetail);
                    for (int i = 1; i <= specialistDetail; i++)
                    {
                        string key = "Specialist" + i;
                        bool specialistAssigned = workerSource.IsWorkerAssigned(structure, key);
                        workerSource.SetWorkerAssigned(structure, key, specialistAssigned);
                        if (specialistAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, specialistAssigned));
                        }
                    }
                }
                long unassignedSpecialistDetail = 0;
                flatpackBlueprint.Properties.getLong("UnassignedSpecialistDetail", 0, out unassignedSpecialistDetail);
                if (unassignedSpecialistDetail > 0)
                {
                    needUnallocatedSpecialist = true;
                }
            }


            int unallocatedWorkersAdded = 0;
            if (needUnallocatedBlueCollar && !unallocatedBlueCollarPresent && workerSource.IsUnassignedWorkerAvailable("BlueCollar"))
            {
                unallocatedBlueCollarPresent = true;
                unallocatedWorkersAdded++;
            }
            if (needUnallocatedWhiteCollar && !unallocatedWhiteCollarPresent && workerSource.IsUnassignedWorkerAvailable("WhiteCollar"))
            {
                unallocatedWhiteCollarPresent = true;
                unallocatedWorkersAdded++;
            }
            if (needUnallocatedSpecialist && !unallocatedSpecialistPresent && workerSource.IsUnassignedWorkerAvailable("Specialist"))
            {
                unallocatedSpecialistPresent = true;
                unallocatedWorkersAdded++;
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

            status.UnallocatedBlueCollarPresent = unallocatedBlueCollarPresent;
            status.UnallocatedWhiteCollarPresent = unallocatedWhiteCollarPresent;
            status.UnallocatedSpecialistPresent = unallocatedSpecialistPresent;
        }

        /// <summary>
        /// Populates a RichTextBox with the colony status summary in a single RTF assignment.
        /// </summary>
        public static void populateStatus(RtfBuilder builder, ColonyStructureStatus status)
        {
            AppendStatus(builder, "Power:", Color.Black, status.PowerRequired, status.PowerProvided);
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

        private static void AppendStatus(RtfBuilder builder, string name, Color color, double required, double provided)
        {
            builder.Append(name, Color.Black);
            builder.Append("" + required, required > provided ? Color.Red : Color.Green);
            builder.Append("/", Color.Black);
            builder.Append("" + provided, Color.Black);
        }
    }
}
