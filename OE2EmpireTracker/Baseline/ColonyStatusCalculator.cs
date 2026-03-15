using Amazon.Runtime.Internal.Transform;
using OE2EmpireTracker.Baseline;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Baseline
{
    public class ColonyStatusCalculator
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;

        private Baseline.Colony colony;
        public double PowerProvided { get; set; }
        public double PowerRequired { get; set; }
        public double HabitationProvision { get; set; }
        public double HabitationRequired { get; set; }
        public double FoodProvision { get; set; }
        public double FoodRequired { get; set; }
        public double EntertainmentProvided { get; set; }
        public double EntertainmentRequired { get; set; }
        public double WarehouseCapacity { get; set; }
        public double WarehouseRequired { get; set; }

        public List<ColonyWorker> ColonyWorkers { get; set; }

        public ColonyStatusCalculator(Baseline.Colony colony) {
            this.colony = colony;
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            ColonyWorkers = new List<ColonyWorker>();
        }

        public void CalculateBuilt()
        {
            double builtPowerProvided = 0;
            double builtPowerRequired = 0;
            double builtHabitationProvision = 0;
            double builtHabitationRequired = 0;
            double builtFoodProvision = 0;
            double builtFoodRequired = 0;
            double builtEntertainmentProvided = 0;
            double builtEntertainmentRequired = 0;
            double builtWarehouseCapacity = 0;
            double builtWarehouseRequired = 0;
            ColonyWorkers.Clear();

            foreach (ColonyStructure structure in colony.Structures)
            {
                Data.Blueprint flatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);

                if (structure.Built == true)
                {
                    if (flatpackBlueprint != null)
                    {
                        {
                            double powerProvided = 0;
                            flatpackBlueprint.Properties.getDouble("PowerProvided", 0, out powerProvided);
                            double powerRequired = 0;
                            flatpackBlueprint.Properties.getDouble("PowerRequired", 0, out powerRequired);

                            builtPowerProvided += powerProvided;
                            builtPowerRequired += powerRequired;
                        }

                        {
                            double habitationProvision = 0;
                            flatpackBlueprint.Properties.getDouble("HabitationProvision", 0, out habitationProvision);

                            builtHabitationProvision += habitationProvision;
                        }

                        {
                            double foodProvision = 0;
                            flatpackBlueprint.Properties.getDouble("FoodProvision", 0, out foodProvision);

                            builtFoodProvision += foodProvision;
                        }

                        {
                            double entertainmentProvided = 0;
                            flatpackBlueprint.Properties.getDouble("EntertainmentProvided", 0, out entertainmentProvided);

                            builtEntertainmentProvided += entertainmentProvided;
                        }

                        {
                            double warehouseCapacity = 0;
                            flatpackBlueprint.Properties.getDouble("WarehouseCapacity", 0, out warehouseCapacity);

                            builtWarehouseCapacity += warehouseCapacity;
                        }

                        if (flatpackBlueprint.Properties.ContainsKey("BlueCollarDetail"))
                        {
                            //BlueCollar
                            long blueCollarDetail = 0;
                            flatpackBlueprint.Properties.getLong("BlueCollarDetail", 0, out blueCollarDetail);
                            for (int i = 1; i <= blueCollarDetail; i++)
                            {
                                bool blueCollarAssigned = false;
                                structure.AssignedWorkers.getBoolean("BlueCollar" + i, false, out blueCollarAssigned);
                                structure.AssignedWorkers.setProperty("BlueCollar" + i, blueCollarAssigned);
                                if (blueCollarAssigned)
                                {
                                    ColonyWorkers.Add(new ColonyWorker(structure, "BlueCollar" + i, blueCollarAssigned));
                                }
                            }
                        }
                        if (flatpackBlueprint.Properties.ContainsKey("WhiteCollarDetail"))
                        {
                            //WhiteCollar
                            long whiteCollarDetail = 0;
                            flatpackBlueprint.Properties.getLong("WhiteCollarDetail", 0, out whiteCollarDetail);
                            for (int i = 1; i <= whiteCollarDetail; i++)
                            {
                                bool whiteCollarAssigned = false;
                                structure.AssignedWorkers.getBoolean("WhiteCollar" + i, false, out whiteCollarAssigned);
                                structure.AssignedWorkers.setProperty("WhiteCollar" + i, whiteCollarAssigned);
                                if (whiteCollarAssigned)
                                {
                                    ColonyWorkers.Add(new ColonyWorker(structure, "WhiteCollar" + i, whiteCollarAssigned));
                                }
                            }
                        }
                        if (flatpackBlueprint.Properties.ContainsKey("SpecialistDetail"))
                        {
                            //SpecialistDetail
                            long specialistDetail = 0;
                            flatpackBlueprint.Properties.getLong("SpecialistDetail", 0, out specialistDetail);
                            for (int i = 1; i <= specialistDetail; i++)
                            {
                                bool specialistAssigned = false;
                                structure.AssignedWorkers.getBoolean("Specialist" + i, false, out specialistAssigned);
                                structure.AssignedWorkers.setProperty("Specialist" + i, specialistAssigned);
                                if (specialistAssigned)
                                {
                                    ColonyWorkers.Add(new ColonyWorker(structure, "Specialist" + i, specialistAssigned));
                                }
                            }
                        }
                    }
                }
            }

            PowerProvided = builtPowerProvided;
            PowerRequired = builtPowerRequired;
            HabitationProvision = builtHabitationProvision;
            HabitationRequired = builtHabitationRequired + ColonyWorkers.Count;
            FoodProvision = builtFoodProvision;
            FoodRequired = builtFoodRequired + ColonyWorkers.Count;
            EntertainmentProvided = builtEntertainmentProvided;
            EntertainmentRequired = builtEntertainmentRequired + ColonyWorkers.Count;
            WarehouseCapacity = builtWarehouseCapacity;
            WarehouseRequired = builtWarehouseRequired;
        }

        public void populateStatus(RichTextBox rtbStatus)
        {
            rtbStatus.Text = "";

            // Power
            AppendColoredText(rtbStatus, "Power: ", Color.Black);
            Color statusColor = Color.Green;
            if (PowerRequired > PowerProvided)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtbStatus, "" + PowerRequired, statusColor);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "" + PowerProvided, Color.Black);

            AppendColoredText(rtbStatus, " Habitation: ", Color.Black);
            statusColor = Color.Green;
            if (HabitationRequired > HabitationProvision)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtbStatus, "" + HabitationRequired, statusColor);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "" + HabitationProvision, Color.Black);

            AppendColoredText(rtbStatus, " Food: ", Color.Black);
            statusColor = Color.Green;
            if (FoodRequired > FoodProvision)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtbStatus, "" + FoodRequired, statusColor);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "" + FoodProvision, Color.Black);

            AppendColoredText(rtbStatus, " Entertainment: ", Color.Black);
            statusColor = Color.Green;
            if (EntertainmentRequired > EntertainmentProvided)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtbStatus, "" + EntertainmentRequired, Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "" + EntertainmentProvided, Color.Black);

            AppendColoredText(rtbStatus, " Warehouse: ", Color.Black);
            statusColor = Color.Green;
            if (WarehouseRequired > WarehouseCapacity)
            {
                statusColor = Color.Red;
            }
            AppendColoredText(rtbStatus, "" + WarehouseRequired, statusColor);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "" + WarehouseCapacity, Color.Black);
        }

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


    }
}

public class ColonyWorker
{
    public ColonyStructure Structure { get; set; }
    public string WorkerType { get; set; }
    public bool Assigned { get; set; }

    public ColonyWorker(ColonyStructure structure, string workerType, bool assigned)
    {
        Structure = structure;
        WorkerType = workerType;
        Assigned = assigned;
    }
}
