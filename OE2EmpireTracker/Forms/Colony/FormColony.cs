using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class FormColony : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private Baseline.Colony selectedColony;
        //private List<Baseline.ColonyStructure> colonyStructures = new List<Baseline.ColonyStructure>();
        private ColonyStatusCalculator statusCalculator;
        public FormColony()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            cmbFlatpacks.DisplayMember = "Name";
            cmbFlatpacks.ValueMember = "UUID";
            updateFlatpackListBase();
            cmbFlatpacks.SelectedIndex = -1;

            flpColonyStructure.Controls.Clear();

            selectedColony = new Baseline.Colony();
            statusCalculator = new ColonyStatusCalculator(selectedColony);
            statusCalculator.CalculateBuilt();

            //ColonyStructure colonyStructure = new ColonyStructure();
            //flpColonyStructure.Controls.Add(colonyStructure);
            //ColonyStructure colonyStructure2 = new ColonyStructure();
            //flpColonyStructure.Controls.Add(colonyStructure2);
            //ColonyStructure colonyStructure3 = new ColonyStructure();
            //flpColonyStructure.Controls.Add(colonyStructure3);

            lvwColonies.View = View.Details;
            lvwColonies.Columns.Add("Planet", 50);
            lvwColonies.Columns.Add("Name", 100);
            populateListView(new List<Baseline.Colony>(playerContext.colonyList));

        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            Baseline.Colony colony;

            if (selectedColony != null)
            {
                colony = selectedColony;
            }
            else
            {
                colony = new Baseline.Colony();
            }

            if (colony.UUID == null)
            {
                Guid myUuid = Guid.NewGuid();
                colony.UUID = myUuid.ToString();
            }

            colony.PlanetName = txtPlanetName.Text;
            colony.ColonyName = txtColonyName.Text;


            if (playerContext.colonyList.Contains(colony) == false)
            {
                playerContext.colonyList.Add(colony);
            }
            playerContext.writeContext();
        }

        private void cmdAddFlatpack_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();

            Baseline.ColonyStructure colonyStructureData = new Baseline.ColonyStructure();
            colonyStructureData.FlatpackBlueprintUUID = cmbFlatpacks.SelectedValue.ToString();
            selectedColony.Structures.Add(colonyStructureData);
            ColonyStructure colonyStructureControl = new ColonyStructure();
            colonyStructureControl.Visible = false;
            colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
            colonyStructureControl.ColonyStructureData = colonyStructureData;
            colonyStructureControl.UpdateData();
            flpColonyStructure.Controls.Add(colonyStructureControl);

            statusCalculator.CalculateBuilt();
            colonyStructureControl.UpdateData();
            statusCalculator.populateStatus(rtbStatus);

            colonyStructureControl.Visible = true;
            this.ResumeLayout();
        }
        private void structures_ColonyStructureDataChanged(object sender, EventArgs e)
        {
            statusCalculator.CalculateBuilt();
            statusCalculator.populateStatus(rtbStatus);
        }

        private void txtFilterFlatpack_TextChanged(object sender, EventArgs e)
        {
            updateFlatpackListBase();
            cmbFlatpacks.DroppedDown = true;
        }

        public void updateFlatpackListBase()
        {
            string searchText = txtFilterFlatpack.Text;
            List<Blueprint> filteredList = new List<Blueprint>(playerContext.blueprintList);

            filteredList = filteredList
                .Where(item => item.BluePrintType.IndexOf("Flatpacks/", StringComparison.OrdinalIgnoreCase) == 0)
                .ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            filteredList = filteredList.OrderBy(p => p.Name).ToList();

            filteredList.Insert(0, new Blueprint());
            var filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = filteredList;

            cmbFlatpacks.DataSource = filteredItemsBindingList;
        }


        private void cmbFlatpacks_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lvwColonies_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void lvwColonies_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Debug.Print("lvwBlueprints.SelectedItems.Count = " + lvwColonies.SelectedItems.Count);
            if (lvwColonies.SelectedItems.Count == 1)
            {
                Debug.Print("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Text);
                Debug.Print("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Tag);
                selectedColony = lvwColonies.SelectedItems[0].SubItems[0].Tag as Baseline.Colony;
                populateForm();
            }
        }
        void populateListView(List<Baseline.Colony> colonies)
        {
            if (colonies == null)
            {
                return;
            }
            lvwColonies.Items.Clear();

            Dictionary<string, ListViewItem> viewableColonies = new Dictionary<string, ListViewItem>();

            // First index what is viewable.
            foreach (ListViewItem item in lvwColonies.Items)
            {
                viewableColonies[(item.Tag as Blueprint).UUID] = item;
            }

            // Now add or update what is viewable.
            foreach (Baseline.Colony colony in colonies)
            {
                ListViewItem item;
                bool found = viewableColonies.TryGetValue(colony.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(colony.PlanetName); // Main item text (first column)
                }
                item.Tag = colony;
                item.SubItems[0].Tag = colony;
                item.SubItems.Add(colony.ColonyName);

                if (!found)
                {
                    lvwColonies.Items.Add(item); // Add the item to the ListView
                }
                else
                {
                    viewableColonies.Remove(colony.UUID);
                }
            }

            // Remove what is left over (deleted or filtered out)
            foreach (KeyValuePair<string, ListViewItem> viewableColony in viewableColonies)
            {
                lvwColonies.Items.Remove(viewableColony.Value);
            }
        }
        private void populateForm()
        {
            if (selectedColony == null)
            {
                return;
            }
            //txtFilterBlueprintType.Text = "";
            //updateBlueprintTypeListBase();
            //cmbBlueprintType.SelectedItem = empireContext.findBlueprintType(selectedBlueprint.BluePrintType);
            //updatePropertyGrid();
            //cmbShipClass.SelectedItem = empireContext.findShipClass(selectedBlueprint.Class);
            //cmbTechLevel.SelectedItem = empireContext.findTechLevel(selectedBlueprint.TechLevel);
            //cmbEvolution.SelectedItem = empireContext.findEvolution(selectedBlueprint.Evolution);

            txtPlanetName.Text = selectedColony.PlanetName;
            txtColonyName.Text = selectedColony.ColonyName;


            this.SuspendLayout();

            //flpColonyStructure.Controls.Clear();
            int controlIndex = 0;
            foreach (Control control in flpColonyStructure.Controls)
            {
                if (controlIndex < selectedColony.Structures.Count)
                {
                    control.Visible = true;
                } else
                {
                    control.Visible = false;
                }
                controlIndex++;
            }

            statusCalculator = new ColonyStatusCalculator(selectedColony);
            //statusCalculator.CalculateBuilt();

            flpColonyStructure.Visible = false;
            this.DoubleBuffered = true;
            List<ColonyStructure> structureControls = new List<ColonyStructure>();
            controlIndex = 0;
            foreach (Baseline.ColonyStructure structure in selectedColony.Structures)
            {
                ColonyStructure colonyStructureControl = null;
                if (controlIndex < flpColonyStructure.Controls.Count)
                {
                    colonyStructureControl = flpColonyStructure.Controls[controlIndex] as ColonyStructure;
                }
                else
                {
                    colonyStructureControl = new ColonyStructure();
                }
                colonyStructureControl.Visible = false;
                colonyStructureControl.SuspendLayout();
                colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                colonyStructureControl.ColonyStructureData = structure;
                colonyStructureControl.UpdateData();
                //flpColonyStructure.Controls.Add(colonyStructureControl);
                structureControls.Add(colonyStructureControl);
                colonyStructureControl.ResumeLayout();
                colonyStructureControl.Visible = true;
                controlIndex++;
            }
            flpColonyStructure.Controls.AddRange(structureControls.ToArray());
            flpColonyStructure.Visible = true;
            statusCalculator.CalculateBuilt();
            statusCalculator.populateStatus(rtbStatus);

            this.ResumeLayout();

        }
    }
}
