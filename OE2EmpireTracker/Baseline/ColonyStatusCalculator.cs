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
                Data.Blueprint FlatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);

                if (structure.Built == true)
                {
                    if (FlatpackBlueprint != null)
                    {
                        {
                            double powerProvided = 0;
                            string powerProvidedStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("PowerProvided", out powerProvidedStr);
                            Double.TryParse(powerProvidedStr, out powerProvided);
                            double powerRequired = 0;
                            string powerRequiredStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("PowerRequired", out powerRequiredStr);
                            Double.TryParse(powerRequiredStr, out powerRequired);

                            builtPowerProvided += powerProvided;
                            builtPowerRequired += powerRequired;
                        }

                        {
                            double habitationProvision = 0;
                            string habitationProvisionStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("HabitationProvision", out habitationProvisionStr);
                            Double.TryParse(habitationProvisionStr, out habitationProvision);

                            builtHabitationProvision += habitationProvision;
                        }

                        {
                            double foodProvision = 0;
                            string foodProvisionStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("FoodProvision", out foodProvisionStr);
                            Double.TryParse(foodProvisionStr, out foodProvision);

                            builtFoodProvision += foodProvision;
                        }

                        {
                            double entertainmentProvided = 0;
                            string entertainmentProvidedStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("EntertainmentProvided", out entertainmentProvidedStr);
                            Double.TryParse(entertainmentProvidedStr, out entertainmentProvided);

                            builtEntertainmentProvided += entertainmentProvided;
                        }

                        {
                            double warehouseCapacity = 0;
                            string warehouseCapacityStr = "";
                            FlatpackBlueprint.Properties.TryGetValue("WarehouseCapacity", out warehouseCapacityStr);
                            Double.TryParse(warehouseCapacityStr, out warehouseCapacity);

                            builtWarehouseCapacity += warehouseCapacity;
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
