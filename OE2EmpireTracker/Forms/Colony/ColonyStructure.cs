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
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class ColonyStructure : UserControl
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;
        public Baseline.Colony Colony { get; set; }
        public Baseline.ColonyStructure ColonyStructureData { get; set; }
        private Data.Blueprint FlatpackBlueprint { get; set; }
        private double PowerProvided { get; set; }
        private double PowerRequired { get; set; }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when colony structure state changes")]
        public event EventHandler ColonyStructureDataChanged;
        public ColonyStructure()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            this.SuspendLayout();
            populateStats();
            this.ResumeLayout();
        }

        public void UpdateData()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            this.SuspendLayout();
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

            chkBuilt.Checked = false;
            chkStaged.Checked = false;
            chkOnline.Checked = false;

            chkWorkDetail1.Visible = false;
            chkWorkDetail2.Visible = false;
            chkWorkDetail3.Visible = false;
            CheckBox[] checkControls = new CheckBox[3];
            checkControls[0] = chkWorkDetail1;
            checkControls[1] = chkWorkDetail2;
            checkControls[2] = chkWorkDetail3;
            int controlIndex = 0;

            if (ColonyStructureData != null)
            {
                bool built = false;
                ColonyStructureData.Properties.getBoolean("Built", false, out built);
                chkBuilt.Checked = built;
                bool staged = false;
                ColonyStructureData.Properties.getBoolean("Staged", false, out staged);
                chkStaged.Checked = staged;
                bool online = false;
                ColonyStructureData.Properties.getBoolean("Online", false, out online);
                chkOnline.Checked = online;

                int index = 1;
                string key = "BlueCollar1";

                while (ColonyStructureData.AssignedWorkers.ContainsKey(key))
                {
                    bool blueCollarAssigned = false;
                    ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "Blue Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = blueCollarAssigned;
                    controlIndex++;
                    index++;
                    key = "BlueCollar" + index;
                }

                index = 1;
                key = "WhiteCollar1";
                while (ColonyStructureData.AssignedWorkers.ContainsKey(key))
                {
                    bool whiteCollarAssigned = false;
                    ColonyStructureData.AssignedWorkers.getBoolean(key, false, out whiteCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "White Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = whiteCollarAssigned;
                    controlIndex++;
                    index++;
                    key = "WhiteCollar" + index;
                }

                index = 1;
                key = "Specialist1";
                while (ColonyStructureData.AssignedWorkers.ContainsKey(key))
                {
                    bool specialistAssigned = false;
                    ColonyStructureData.AssignedWorkers.getBoolean(key, false, out specialistAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = true;
                    checkControls[controlIndex].Text = "Specialist";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = specialistAssigned;
                    controlIndex++;
                    index++;
                    key = "Specialist" + index;
                }

                key = "UnassignedBlueCollarDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedBlueCollarPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - Blue Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedBlueCollarPresent;
                    controlIndex++;
                }

                key = "UnassignedWhiteCollarDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedWhiteCollarPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - White Collar";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedWhiteCollarPresent;
                    controlIndex++;
                }

                key = "UnassignedSpecialistDetail";
                if (FlatpackBlueprint.Properties.ContainsKey(key))
                {
                    bool unassignedSpecialistPresent = true;
                    //ColonyStructureData.AssignedWorkers.getBoolean(key, false, out blueCollarAssigned);
                    checkControls[controlIndex].Visible = true;
                    checkControls[controlIndex].Enabled = false;
                    checkControls[controlIndex].Text = "Support - Specialist";
                    checkControls[controlIndex].Tag = key;
                    checkControls[controlIndex].Checked = unassignedSpecialistPresent;
                    controlIndex++;
                }

                bool hasAllWorkers = true;
                for (index = 0; index < controlIndex; index++) {
                    if (checkControls[index].Checked == false)
                    {
                        hasAllWorkers = false;
                    }
                }
                if (staged)
                {
                    flpColonyStructure.BackColor = Color.Yellow;
                }
                else if (built)
                {
                    if (online == false)
                    {
                        flpColonyStructure.BackColor = Color.PaleVioletRed;
                    }
                    else
                    {
                        if (hasAllWorkers)
                        {
                            flpColonyStructure.BackColor = Color.Green;
                        }
                        else
                        {
                            flpColonyStructure.BackColor = Color.LightGreen;
                        }
                    }
                }
                else
                {
                    flpColonyStructure.BackColor = Color.White;
                }
            }

            populateStats();
            this.ResumeLayout();
            guard.release();
        }

        private void populateStats()
        {
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);

            rtbStatus.Text = "";

            if (ColonyStructureData != null)
            {
                ColonyStatusCalculator.AppendColoredText(rtbStatus, "#" + ColonyStructureData.gameSequence + " ", Color.Black);
            }
            if (FlatpackBlueprint != null)
            {
                ColonyStatusCalculator.AppendColoredText(rtbStatus, FlatpackBlueprint.ExtendedName, Color.Black);

                rtbStatus.AppendText("\n");
            }

            if (ColonyStructureData != null && ColonyStructureData.Statuses != null)
            {
                ColonyStructureData.Statuses.TryGetValue("Actual", out ColonyStructureStatus status);
                if (status != null)
                {
                    ColonyStatusCalculator.populateStatus(rtbStatus, status);
                }
            }

            /*
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
            */

            guard.release();
        }

        private void rtbStatus_ContentsResized(object sender, ContentsResizedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            // Adjust the height of the RichTextBox to fit the new content rectangle
            // An offset (+10 in this example) may be needed to account for borders/margins
            rtbStatus.Height = e.NewRectangle.Height + 10;
            rtbStatus.Width = e.NewRectangle.Width + 10;
        }

        private void chkWorkDetail1_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkWorkDetail1.Checked;
            string prop = chkWorkDetail1.Tag as string;
            ColonyStructureData.AssignedWorkers.setProperty(prop, state);

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail2_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkWorkDetail2.Checked;
            string prop = chkWorkDetail2.Tag as string;
            ColonyStructureData.AssignedWorkers.setProperty(prop, state);

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkWorkDetail3_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkWorkDetail3.Checked;
            string prop = chkWorkDetail3.Tag as string;
            ColonyStructureData.AssignedWorkers.setProperty(prop, state);

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkBuilt_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkBuilt.Checked;
            ColonyStructureData.Properties.setProperty("Built", state);

            if (state == true)
            {
                chkStaged.Checked = false;
            }
            if (state == false)
            {
                chkOnline.Checked = false;
            }

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkStaged_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkStaged.Checked;
            ColonyStructureData.Properties.setProperty("Staged", state);
            if (state == true)
            {
                chkBuilt.Checked = false;
                chkOnline.Checked = false;
            }

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        private void chkOnline_CheckStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            bool state = chkOnline.Checked;
            ColonyStructureData.Properties.setProperty("Online", state);
            if (state == true)
            {
                chkBuilt.Checked = true;
                chkStaged.Checked = false;
            }

            UpdateData();
            ColonyStructureDataChanged?.Invoke(this, e);
        }

        public class ProgramaticUpdateGuard
        {
            private ColonyStructure _parent;
            private bool _hasLocked;

            public ProgramaticUpdateGuard(ColonyStructure parent)
            {
                _parent = parent;
                _parent._isProgrammaticUpdate++;
                _hasLocked = true;
            }
            public void release()
            {
                if (_hasLocked)
                {
                    _parent._isProgrammaticUpdate--;
                    _hasLocked = false;
                }
            }
            ~ProgramaticUpdateGuard()
            {
                release();
            }
        }
    }
}