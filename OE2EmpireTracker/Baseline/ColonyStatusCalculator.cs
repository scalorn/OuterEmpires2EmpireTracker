using Amazon.Runtime.Internal.Transform;
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

        public ColonyStatusCalculator(Baseline.Colony colony) {
            this.colony = colony;
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
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

                        if (flatpackBlueprint.Properties.ContainsKey("SpecialistDetail"))
                        {
                            //SpecialistDetail
                            double specialistDetail = 0;
                            flatpackBlueprint.Properties.getDouble("SpecialistDetail", 0, out specialistDetail);
                            bool specialistAssigned = false;
                            structure.AssignedWorkers.getBoolean("Specialist1", false, out specialistAssigned);
                            structure.AssignedWorkers.setProperty("Specialist1", specialistAssigned);
                            if (specialistAssigned)
                            {
                                //workers += 1;
                            }
                        } else
                        {
                            if (structure.AssignedWorkers.ContainsKey("Specialist1"))
                            {
                                structure.AssignedWorkers.Remove("Specialist1");
                            }
                        }
                    }
                }
            }

            PowerProvided = builtPowerProvided;
            PowerRequired = builtPowerRequired;
            HabitationProvision = builtHabitationProvision;
            HabitationRequired = builtHabitationRequired;
            FoodProvision = builtFoodProvision;
            FoodRequired = builtFoodRequired;
            EntertainmentProvided = builtEntertainmentProvided;
            EntertainmentRequired = builtEntertainmentRequired;
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
