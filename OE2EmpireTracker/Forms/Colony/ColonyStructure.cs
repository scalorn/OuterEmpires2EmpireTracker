using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class ColonyStructure : UserControl
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        public Baseline.ColonyStructure ColonyStructureData { get; set; }
        private Blueprint FlatpackBlueprint { get; set; }
        private double PowerProvided { get; set; }
        private double PowerRequired { get; set; }
        public ColonyStructure()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            populateStats();
        }

        public void UpdateData()
        {
            FlatpackBlueprint = playerContext.findBlueprint(ColonyStructureData.FlatpackBlueprintUUID);

            if (FlatpackBlueprint != null)
            {
                double powerProvided = 0;
                FlatpackBlueprint.Properties.getDouble("PowerProvided", 0, out powerProvided);
                double powerRequired = 0;
                FlatpackBlueprint.Properties.getDouble("PowerRequired", 0, out powerRequired);

                PowerProvided = powerProvided;
                PowerRequired = powerRequired;
            }

            chkWorkDetail1.Visible = false;
            chkWorkDetail2.Visible = false;
            chkWorkDetail3.Visible = false;

            if (ColonyStructureData != null)
            {
                if (ColonyStructureData.AssignedWorkers.ContainsKey("Specialist1"))
                {
                    bool specialistAssigned = false;
                    ColonyStructureData.AssignedWorkers.getBoolean("Specialist1", false, out specialistAssigned);
                    chkWorkDetail1.Visible = true;
                    chkWorkDetail1.Text = "Specialist";
                    chkWorkDetail1.Checked = specialistAssigned;
                }
            }

            populateStats();
        }

        private void populateStats()
        {
            rtbStatus.Text = "";

            if (ColonyStructureData != null)
            {
                ColonyStatusCalculator.AppendColoredText(rtbStatus, "#" + ColonyStructureData.gameSequence + " ", Color.Black);
            }
            if (FlatpackBlueprint != null) {
                ColonyStatusCalculator.AppendColoredText(rtbStatus, FlatpackBlueprint.ExtendedName, Color.Black);
            }

            // Power
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "\nPower: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "" + PowerRequired, Color.Red);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "" + PowerProvided, Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Habitation: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Food: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Entertainment: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);

            ColonyStatusCalculator.AppendColoredText(rtbStatus, " Warehouse: ", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "4", Color.Green);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "/", Color.Black);
            ColonyStatusCalculator.AppendColoredText(rtbStatus, "10 ", Color.Black);
        }

        private void rtbStatus_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            // Adjust the height of the RichTextBox to fit the new content rectangle
            // An offset (+10 in this example) may be needed to account for borders/margins
            rtbStatus.Height = e.NewRectangle.Height + 10;
            rtbStatus.Width = e.NewRectangle.Width + 10;
        }
    }
}
