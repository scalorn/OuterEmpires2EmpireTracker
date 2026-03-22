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
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class FormColony : Form
    {
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;

        private Baseline.Colony selectedColony;
        //private List<Baseline.ColonyStructure> colonyStructures = new List<Baseline.ColonyStructure>();
        private ColonyStatusCalculator statusCalculator;
        public FormColony()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            updateItemTypeList();
            updatePurityList();

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

            //flpColonyData.BackColor = Color.LightCoral;
            //tlpBase.BackColor = Color.LightBlue;

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
            colonyStructureData.UUID = Guid.NewGuid().ToString();
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
            if (_isProgrammaticUpdate > 0) return;

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

        public void updatePurityList()
        {
            IReadOnlyList<ResourcePurity> purities = Data.ResourcePurity.Purities;

            var filteredPurityBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredPurityBindingList.DataSource = purities;

            cmbPurity.DataSource = filteredPurityBindingList;
        }

        public void updateItemTypeList()
        {
            IReadOnlyList<ItemType> itemTypes = Data.ItemType.ItemTypes;

            var filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = itemTypes;

            cmbItemType.DataSource = filteredItemsBindingList;
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
            //lvwColonies.Items.Clear();

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
            Debug.Print("populateForm called! selectedColony = " + (selectedColony != null ? selectedColony.PlanetName : "null"));
            ProgramaticUpdateGuard guard = new ProgramaticUpdateGuard(this);
            this.SuspendLayout();
            tabDetailedData.Visible = false;

            if (selectedColony == null)
            {
                return;
            }

            // Force creation of an item.
            if (selectedColony.Items.Count() == 0)
            {
                selectedColony.Items.AddItem(new Data.Item() { UUID = Guid.NewGuid().ToString() });
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



            //flpColonyStructure.Controls.Clear();
            Debug.Print("populatForm: Hiding excess controls started");
            int controlIndex = 0;
            foreach (Control control in flpColonyStructure.Controls)
            {
                if (controlIndex < selectedColony.Structures.Count)
                {
                    control.Visible = true;
                }
                else
                {
                    control.Visible = false;
                }
                controlIndex++;
            }
            Debug.Print("populatForm: Hiding excess controls finished");

            statusCalculator = new ColonyStatusCalculator(selectedColony);
            //statusCalculator.CalculateBuilt();

            //flpColonyStructure.Visible = false;
            this.DoubleBuffered = true;
            List<ColonyStructure> structureControls = new List<ColonyStructure>();
            controlIndex = 0;
            foreach (Baseline.ColonyStructure structure in selectedColony.Structures)
            {
                Debug.Print("populatForm: processing structure started");
                ColonyStructure colonyStructureControl = null;
                bool addControl = false;
                if (controlIndex < flpColonyStructure.Controls.Count)
                {
                    colonyStructureControl = flpColonyStructure.Controls[controlIndex] as ColonyStructure;
                }
                else
                {
                    colonyStructureControl = new ColonyStructure();
                    addControl = true;
                }
                //colonyStructureControl.Visible = false;
                //colonyStructureControl.SuspendLayout();
                colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                colonyStructureControl.ColonyStructureData = structure;
                colonyStructureControl.UpdateData();
                //flpColonyStructure.Controls.Add(colonyStructureControl);
                if (addControl)
                {
                    structureControls.Add(colonyStructureControl);
                }
                //colonyStructureControl.ResumeLayout();
                //colonyStructureControl.Visible = true;
                controlIndex++;
                Debug.Print("populatForm: processing structure finished");
            }
            Debug.Print("populatForm: Adding new controls started");
            flpColonyStructure.Controls.AddRange(structureControls.ToArray());
            Debug.Print("populatForm: Adding new controls finished");
            Debug.Print("populatForm: Making new controls visible started");
            foreach (ColonyStructure structureControl in structureControls)
            {
                structureControl.Visible = true;
            }
            Debug.Print("populatForm: Making new controls visible finished");
            //flpColonyStructure.Visible = true;

            Debug.Print("populateForm: Calling CalculateBuilt started");
            statusCalculator.CalculateBuilt();
            Debug.Print("populateForm: Calling CalculateBuilt finished");
            Debug.Print("populateForm: Calling populateStatus started");
            statusCalculator.populateStatus(rtbStatus);
            Debug.Print("populateForm: Calling populateStatus finished");

            tabDetailedData.Visible = true;
            this.ResumeLayout();
            guard.release();
            Debug.Print("populateForm completed!");
        }

        private void tlpBase_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.Print("tlpBase_Layout called! tlpBase = " + tlpBase.Size.Width + " " + tlpBase.Size.Height);
            //Debug.Print("tlpBase_Layout called!flpSearchList =  " + flpSearchList.Size.Width + " " + flpSearchList.Size.Height);
        }

        private void tlpBase_Resize(object sender, EventArgs e)
        {
            //Debug.Print("tlpBase_Resize called! tlpBase = " + tlpBase.Size.Width + " " + tlpBase.Size.Height);
            //Debug.Print("tlpBase_Resize called!flpSearchList =  " + flpSearchList.Size.Width + " " + flpSearchList.Size.Height);
            flpColonyData.Size = new System.Drawing.Size(tlpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left, flpColonyData.Size.Height);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.Print("flpSearchList_Layout called! tlpBase = " + tlpBase.Size.Width + " " + tlpBase.Size.Height);
            //Debug.Print("flpSearchList_Layout called!flpSearchList =  " + flpSearchList.Size.Width + " " + flpSearchList.Size.Height);
            lvwColonies.Size = new System.Drawing.Size(lvwColonies.Size.Width, flpSearchList.Size.Height - flpBlueprintSearch.Size.Height - flpBlueprintSearch.Margin.Top - flpBlueprintSearch.Margin.Bottom - lvwColonies.Margin.Top - lvwColonies.Margin.Bottom);
            //lvwColonies.Size.Height = flpSearchList.Size.Height - flpBlueprintSearch.Size.Height;
        }

        private void flpSearchList_Resize(object sender, EventArgs e)
        {
            //Debug.Print("flpSearchList_Resize called! tlpBase = " + tlpBase.Size.Width + " " + tlpBase.Size.Height);
            //Debug.Print("flpSearchList_Resize called!flpSearchList =  " + flpSearchList.Size.Width + " " + flpSearchList.Size.Height);
        }

        private void flpColonyData_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.Print("flpColonyData_Layout called!");
            tabDetailedData.Size = new System.Drawing.Size(flpColonyData.Size.Width - tabDetailedData.Margin.Right - tabDetailedData.Margin.Left, flpColonyData.Size.Height - flpBaseDetails.Size.Height - flpBaseDetails.Margin.Top - flpBaseDetails.Margin.Bottom - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom - tabDetailedData.Margin.Top - tabDetailedData.Margin.Bottom);
        }

        private void flpStructures_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.Print("flpStructures_Layout called!");
            flpStructureData.Size = new System.Drawing.Size(flpStructures.Size.Width - lvwStructureTypes.Size.Width - lvwStructureTypes.Margin.Right - lvwStructureTypes.Margin.Left, flpStructures.Size.Height - flpStructureData.Margin.Top - flpStructureData.Margin.Bottom);
            lvwStructureTypes.Size = new System.Drawing.Size(lvwStructureTypes.Size.Width, flpStructures.Size.Height - lvwStructureTypes.Margin.Top - lvwStructureTypes.Margin.Bottom);
        }

        private void flpStructureData_Layout(object sender, LayoutEventArgs e)
        {
            //Debug.Print("flpStructureData_Layout called!");
            flpColonyStructure.Size = new System.Drawing.Size(flpStructureData.Size.Width - flpColonyStructure.Margin.Left - flpColonyStructure.Margin.Right, flpStructureData.Size.Height - flpStatus.Size.Height - flpStatus.Margin.Top - flpStatus.Margin.Bottom - flpAddBox.Size.Height - flpAddBox.Margin.Top - flpAddBox.Margin.Bottom - flpColonyStructure.Margin.Top - flpColonyStructure.Margin.Bottom);
        }
        public class ProgramaticUpdateGuard
        {
            private FormColony _parent;
            private bool _hasLocked;

            public ProgramaticUpdateGuard(FormColony parent)
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

        private void tabPStructures_Layout(object sender, LayoutEventArgs e)
        {

        }

        private void cmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Data.ItemType itemType = cmbItemType.SelectedItem as Data.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null)
            {
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Resource)
                {
                    populateItemWithResources();
                    cmbPurity.Visible = true;
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Commodity)
                {
                    populateItemWithCommodities();
                }
            }
        }
        public void populateItemWithResources()
        {
            IReadOnlyList<Resource> resources = Data.Resource.Resources;

            var filteredItemBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemBindingList.DataSource = resources;

            cmbItem.DataSource = filteredItemBindingList;
            cmbItem.ValueMember = "Name";
            cmbItem.DisplayMember = "Name";
        }
        public void populateItemWithCommodities()
        {
            IReadOnlyList<Commodity> commodities = Data.Commodity.Commodities;

            var filteredItemBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemBindingList.DataSource = commodities;

            cmbItem.DataSource = filteredItemBindingList;
            cmbItem.ValueMember = "Name";
            cmbItem.DisplayMember = "Name";
        }

        private void cmbPurity_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmdAdd_Click(object sender, EventArgs e)
        {
            Data.Item item = new Data.Item() { UUID = Guid.NewGuid().ToString() };
            Data.ItemType itemType = cmbItemType.SelectedItem as Data.ItemType;
            if (itemType != null)
            {
                item.ItemType = itemType.ID;

                if (itemType.ID == Data.ItemType.ItemTypeEnum.Resource)
                {
                    Data.Resource resource = cmbItem.SelectedItem as Data.Resource;
                    if (resource != null)
                    {
                        item.BaseItemTypeID = resource.Name;
                        item.Name = resource.Name;
                    }
                    Data.ResourcePurity purity = cmbPurity.SelectedItem as Data.ResourcePurity;
                    if (purity != null)
                    {
                        item.ResourcePurity = purity.Name;
                    }
                    else
                    {
                        item.ResourcePurity = Data.ResourcePurity.ItemTypeMapByEnum[Data.ResourcePurity.PurityEnum.Refined].Name;
                    }
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Commodity)
                {
                    Data.Commodity commodity = cmbItem.SelectedItem as Data.Commodity;
                    if (commodity != null)
                    {
                        item.BaseItemTypeID = commodity.Name;
                        item.Name = commodity.Name;
                    }
                }

                string quantityStr = txtQuantity.Text;
                if (quantityStr != null && quantityStr.Length > 0)
                {
                    int quantity = 0;
                    int.TryParse(quantityStr, out quantity);
                    item.Quantity = quantity;
                }

                selectedColony.Items.AddItem(item);
            }
        }

        private void cmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            Data.ItemType itemType = cmbItemType.SelectedItem as Data.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null)
            {
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Resource)
                {
                    Resource resource = cmbItem.SelectedItem as Resource;
                    if (resource != null)
                    {
                        // Hide purity for synthetic resources, as they don't have purity.
                        if (ResourceGroup.ResourceGroupMapByEnum[resource.ResourceGroup].Synthetic)
                        {
                            cmbPurity.Visible = false;
                        }
                        else
                        {
                            cmbPurity.Visible = true;
                        }
                    }
                }
            }
        }

        private void txtItemFilter_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
