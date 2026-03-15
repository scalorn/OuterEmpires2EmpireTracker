using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Data;
using System;
using System.Collections;
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
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Baseline.Colony colony;

            if (selectedColony != null)
            {
                colony = selectedColony;
            }
            else
            {
                colony = new Baseline.Colony();
                Guid myUuid = Guid.NewGuid();
                colony.UUID = myUuid.ToString();
            }

            colony.PlanetName = txtPlanetName.Text;
            colony.ColonyName = txtColonyName.Text;


            if (selectedColony == null)
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
            colonyStructureData.Built = true; // FIXME: A cheat.
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
    }
}
