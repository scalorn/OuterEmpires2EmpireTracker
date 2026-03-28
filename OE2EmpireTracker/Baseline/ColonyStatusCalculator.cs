using Amazon.Runtime.Internal.Transform;
using OE2EmpireTracker.Baseline;
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
        public List<ColonyWorker> ColonyWorkers { get; set; }

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
            ColonyWorkers = new List<ColonyWorker>();
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
            ColonyWorkers.Clear();

            foreach (ColonyStructure structure in colony.Structures)
            {
                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers);
                structure.Statuses["Actual"] = currentStatus;
                previousStatus = currentStatus;
            }
            finalActualStatus = previousStatus;
        }

        public void CalculateIdeal()
        {
            var workers = new IdealColonyStructureWorkers();
            ColonyStructureStatus previousStatus = new ColonyStructureStatus();

            foreach (ColonyStructure structure in colony.Structures)
            {
                ColonyStructureStatus currentStatus = new ColonyStructureStatus();
                CalculateBuilt(structure, previousStatus, currentStatus, workers);
                structure.Statuses["Ideal"] = currentStatus;
                previousStatus = currentStatus;
            }
            finalIdealStatus = previousStatus;
        }

        public void CalculateBuilt(ColonyStructure structure, ColonyStructureStatus prevStatus, ColonyStructureStatus status, IColonyStructureWorkers workerSource)
        {
            // Aggregators for resource stats
            double builtPowerProvided = prevStatus.PowerProvided;
            double builtPowerRequired = prevStatus.PowerRequired;
            double builtHabitationProvision = prevStatus.HabitationProvision;
            double builtHabitationRequired = prevStatus.HabitationRequired;
            double builtFoodProvision = prevStatus.FoodProvision;
            double builtFoodRequired = prevStatus.FoodRequired;
            double builtEntertainmentProvided = prevStatus.EntertainmentProvided;
            double builtEntertainmentRequired = prevStatus.EntertainmentProvided;
            double builtWarehouseCapacity = prevStatus.WarehouseCapacity;
            double builtWarehouseRequired = prevStatus.WarehouseCapacity;

            // Ensure the structure has a unique identifier for lookups
            if (structure.UUID == null || structure.UUID.Length == 0)
            {
                structure.UUID = Guid.NewGuid().ToString();
            }

            Data.Blueprint flatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);

            bool built = false;
            // Reads raw property flags from the structure
            structure.Properties.getBoolean("Built", false, out built);
            bool staged = false;
            structure.Properties.getBoolean("Staged", false, out staged);
            bool online = false;
            structure.Properties.getBoolean("Online", false, out online);

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
                        structure.AssignedWorkers.setProperty(key, blueCollarAssigned);
                        if (blueCollarAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, blueCollarAssigned));
                        }
                    }
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
                        structure.AssignedWorkers.setProperty(key, whiteCollarAssigned);
                        if (whiteCollarAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, whiteCollarAssigned));
                        }
                    }
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
                        structure.AssignedWorkers.setProperty(key, specialistAssigned);
                        if (specialistAssigned)
                        {
                            ColonyWorkers.Add(new ColonyWorker(structure, key, specialistAssigned));
                        }
                    }
                }
            }

            // Assign aggregated values to public properties. 
            // Note: 'Required' stats are currently derived from the worker count, not direct blueprint sums.
            status.PowerProvided = builtPowerProvided;
            status.PowerRequired = builtPowerRequired;
            status.HabitationProvision = builtHabitationProvision;
            // Habitation required is calculated based on workers in current implementation
            status.HabitationRequired = builtHabitationRequired + ColonyWorkers.Count;

            status.FoodProvision = builtFoodProvision;
            // Food required is calculated based on workers in current implementation
            status.FoodRequired = builtFoodRequired + ColonyWorkers.Count;

            status.EntertainmentProvided = builtEntertainmentProvided;
            // Entertainment required is calculated based on workers in current implementation
            status.EntertainmentRequired = builtEntertainmentRequired + ColonyWorkers.Count;

            status.WarehouseCapacity = builtWarehouseCapacity;
            // Warehouse required is calculated based on workers in current implementation
            status.WarehouseRequired = builtWarehouseRequired;
        }

        /// <summary>
        /// Populates a RichTextBox UI component with the current colony status summary.
        /// </summary>
        /// <param name="rtbStatus">The RichTextBox control to update.</param>
        /// <remarks>
        /// <para>Calls <c>AppendColoredText</c> for each statistic category (Power, Habitation, Food, Entertainment, Warehouse).</para>
        /// <para>Color Coding:</para>
        /// <list type="bullet">
        /// <item><description><b>Red Text:</b> Statistic is in deficit (e.g., Power Required > Power Provided).</description></item>
        /// <item><description><b>Green Text:</b> Statistic meets the requirement (e.g., Provision >= Requirement).</description></item>
        /// </list>
        /// <para>The UI text follows the format: "Name : [Required] / [Provided]" or similar based on property availability.</para>
        /// </remarks>
        public static void populateStatus(RichTextBox rtbStatus, ColonyStructureStatus status)
        {
            // Clear previous status to start fresh
            //rtbStatus.Text = "";

            // --- Power Status ---
            UpdateStatus(rtbStatus, "Power:", Color.Black, status.PowerRequired, status.PowerProvided);

            // --- Habitation Status ---
            UpdateStatus(rtbStatus, " Habitation: ",
                ((status.HabitationProvision < status.HabitationRequired) ? Color.Red : Color.Green), // Simplified logic check based on property names below
                status.HabitationRequired, status.HabitationProvision);

            // --- Food Status ---
            UpdateStatus(rtbStatus, " Food: ",
                status.FoodProvision < status.FoodRequired ? Color.Red : Color.Green,
                status.FoodRequired, status.FoodProvision);

            // --- Entertainment Status ---
            UpdateStatus(rtbStatus, " Entertainment: ",
                status.EntertainmentProvided < status.EntertainmentRequired ? Color.Red : Color.Green,
                status.EntertainmentRequired, status.EntertainmentProvided);

            // --- Warehouse Status ---
            UpdateStatus(rtbStatus, " Warehouse: ",
                status.WarehouseCapacity < status.WarehouseRequired ? Color.Red : Color.Green,
                status.WarehouseRequired, status.WarehouseCapacity);
        }

        /// <summary>
        /// Helper to append text with color status checks.
        /// </summary>
        private static void UpdateStatus(RichTextBox rtb, string name, Color errorColor, double required, double provided)
        {
            AppendColoredText(rtb, name, Color.Black); // Just keeping logic simple for now or refactor

            // Logic simplified to use existing AppendColoredText pattern
            Color statusColor = Color.Green;
            if (required > provided)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtb, "" + required, statusColor);
            AppendColoredText(rtb, "/", Color.Black);
            AppendColoredText(rtb, "" + provided, Color.Black);
        }

        /// <summary>
        /// Helper method to append text to a RichTextBox with color encoding based on current selection.
        /// </summary>
        /// <param name="box">The control to append to.</param>
        /// <param name="text">The string content to add.</param>
        /// <param name="color">The drawing color (e.g., Red for deficit, Black/Green for normal).</param>
        public static void AppendColoredText(RichTextBox box, string text, Color color)
        {
            // Set the selection point to the end of the existing text
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            // Set the color for the text to be appended
            box.SelectionColor = color;

            // Append the new text
            box.AppendText(text);

            // Reset the selection color to the default (e.g., black) for future user input
            box.SelectionColor = box.ForeColor;
        }

        /// <summary>
        /// Static method to simplify status checks in main loop.
        /// </summary>
    }
}

public class ColonyWorker
{
    /// <summary>
    /// Represents a single worker unit assigned to a colony structure.
    /// Used for tracking labor capacity within the <see cref="ColonyStatusCalculator"/>.
    /// </summary>

    /// <summary>
    /// The structure that this worker is currently assigned to.
    /// Holds the structural context (building UUID, blueprint reference, etc.).
    /// </summary>
    public ColonyStructure Structure { get; set; }

    /// <summary>
    /// Categorization of the worker role (e.g., "BlueCollar1", "WhiteCollar1", "Specialist").
    /// This string helps identify what job function the worker performs.
    /// </summary>
    public string WorkerType { get; set; }

    /// <summary>
    /// Indicates if this worker slot is currently active/assigned to a task.
    /// Used to distinguish between potential slots and actual labor being performed.
    /// </summary>
    public bool Assigned { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ColonyWorker"/> class.
    /// </summary>
    /// <param name="structure">The structure entity the worker belongs to.</param>
    /// <param name="workerType">Identifier for the role (e.g., "BlueCollar1").</param>
    /// <param name="assigned">Current state of assignment (true = assigned).</param>
    public ColonyWorker(ColonyStructure structure, string workerType, bool assigned)
    {
        Structure = structure;
        WorkerType = workerType;
        Assigned = assigned;
    }
}
