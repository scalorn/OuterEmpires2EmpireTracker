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


        public ColonyStatusCalculator(Baseline.Colony colony) {
            this.colony = colony;
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
        }

        public void CalculateBuilt()
        {
            double builtPowerProvided = 0;
            double builtPowerRequired = 0;

            foreach (ColonyStructure structure in colony.Structures)
            {
                Data.Blueprint FlatpackBlueprint = playerContext.findBlueprint(structure.FlatpackBlueprintUUID);

                if (structure.Built == true)
                {
                    if (FlatpackBlueprint != null)
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
                }
            }

            PowerProvided = builtPowerProvided;
            PowerRequired = builtPowerRequired;
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
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, " Food: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, " Entertainment: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);

            AppendColoredText(rtbStatus, " Warehouse: ", Color.Black);
            AppendColoredText(rtbStatus, "4", Color.Green);
            AppendColoredText(rtbStatus, "/", Color.Black);
            AppendColoredText(rtbStatus, "10 ", Color.Black);
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
