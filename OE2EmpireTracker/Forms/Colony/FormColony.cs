using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using NLog;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.Colony
{
    public partial class FormColony : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;

        private Baseline.Colony selectedColony;
        private ColonyViewModel colonyViewModel;
        private ColonyStatusCalculator statusCalculator => colonyViewModel?.Calculator;
        public FormColony()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;

            UpdateItemTypeList();
            UpdatePurityList();

            cmbFlatpacks.DisplayMember = "Name";
            cmbFlatpacks.ValueMember = "UUID";
            UpdateFlatpackListBase();
            cmbFlatpacks.SelectedIndex = -1;

            flpColonyStructure.Controls.Clear();

            selectedColony = new Baseline.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            colonyViewModel.RecalculateStatus();

            lvwColonies.View = View.Details;
            lvwColonies.Columns.Add("Planet", 50);
            lvwColonies.Columns.Add("Name", 100);
            PopulateListView(playerContext.GetCurrentPlayerColonies());

            UpdateCommodityRequestList();

            // Wire write-through handlers
            txtPlanetName.TextChanged += txtPlanetName_TextChanged;
            txtColonyName.TextChanged += txtColonyName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            lvwColonies.Items.Clear();
            flpColonyStructure.Controls.Clear();
            selectedColony = new Baseline.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            colonyViewModel.RecalculateStatus();
            PopulateListView(playerContext.GetCurrentPlayerColonies());
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (selectedColony != null && selectedColony.UUID == e.ColonyUUID)
            {
                colonyViewModel.RecalculateStatus();
                PopulateForm();
            }
        }

        private void txtPlanetName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.PlanetName = txtPlanetName.Text;
        }

        private void txtColonyName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.ColonyName = txtColonyName.Text;
        }

        private void txtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.Data.SystemName = txtSystemName.Text;
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(colonyViewModel.Data.OwnerUUID))
            {
                colonyViewModel.Data.OwnerUUID = playerContext.CurrentPlayerUUID;
            }
            colonyViewModel.Save();
        }

        private void cmdOptimize_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;

            var optimizer = new BuildOrderOptimizer(playerContext);
            var optimized = optimizer.Optimize(selectedColony);

            // Replace the colony's structure list with the optimized order
            selectedColony.Structures.Clear();
            selectedColony.Structures.AddRange(optimized);

            // Refresh the UI
            structures_ColonyStructureDataChanged(sender, e);
        }

        private void cmdBootstrap_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;
            if (string.IsNullOrEmpty(selectedColony.PlanetName))
            {
                System.Windows.Forms.MessageBox.Show(
                    "Set a planet name before bootstrapping.",
                    "No Planet",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                return;
            }

            var bootstrap = new ColonyBootstrap(playerContext);
            bootstrap.Bootstrap(selectedColony);

            structures_ColonyStructureDataChanged(sender, e);
        }

        private void cmdAddFlatpack_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();

            var structureViewModel = colonyViewModel.AddStructure(cmbFlatpacks.SelectedValue.ToString());
            ColonyStructure colonyStructureControl = new ColonyStructure();
            colonyStructureControl.Visible = false;
            colonyStructureControl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
            colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
            colonyStructureControl.Colony = selectedColony;
            colonyStructureControl.ColonyStructureData = structureViewModel.Data;
            colonyStructureControl.UpdateData();
            flpColonyStructure.Controls.Add(colonyStructureControl);

            colonyViewModel.RecalculateStatus();
            colonyStructureControl.UpdateData();

            RtfBuilder builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
            rtbStatus.Rtf = builder.ToRtf();

            colonyStructureControl.Visible = true;
            this.ResumeLayout();
        }
        private void structures_ColonyStructureDataChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            using var guard = new ProgrammaticUpdateGuard(this);
            this.SuspendLayout();

            colonyViewModel.RecalculateStatus();

            flpColonyStructure.SuspendLayout();

            // Build a map of existing controls by their data reference
            var controlMap = new Dictionary<Baseline.ColonyStructure, ColonyStructure>();
            foreach (Control c in flpColonyStructure.Controls)
            {
                if (c is ColonyStructure cs && cs.ColonyStructureData != null)
                {
                    controlMap[cs.ColonyStructureData] = cs;
                }
            }

            // Remove controls whose structure was deleted
            foreach (var orphan in controlMap
                .Where(kv => !selectedColony.Structures.Contains(kv.Key))
                .Select(kv => kv.Value)
                .ToList())
            {
                flpColonyStructure.Controls.Remove(orphan);
                orphan.Dispose();
            }

            var controlIndexMap = new Dictionary<Control, int>();
            int index = 0;
            foreach (Control c in flpColonyStructure.Controls)
            {
                controlIndexMap[c] = index++;
            }

            // Reorder and update existing controls to match selectedColony.Structures order
            for (int i = 0; i < selectedColony.Structures.Count; i++)
            {
                Baseline.ColonyStructure structure = selectedColony.Structures[i];
                ColonyStructure ctrl;
                if (!controlMap.TryGetValue(structure, out ctrl))
                {
                    // New structure ï¿½ create a control for it
                    ctrl = new ColonyStructure();
                    ctrl.Colony = selectedColony;
                    ctrl.ColonyStructureData = structure;
                    ctrl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                    ctrl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                    flpColonyStructure.Controls.Add(ctrl);
                    // New control goes to end; SetChildIndex will move it into place
                    controlIndexMap[ctrl] = flpColonyStructure.Controls.Count - 1;
                }
                // Move to correct position without removing/re-adding
                int currentIndex;
                if (controlIndexMap.TryGetValue(ctrl, out currentIndex) && currentIndex != i)
                {
                    flpColonyStructure.Controls.SetChildIndex(ctrl, i);
                }
                ctrl.UpdateData();
            }

            flpColonyStructure.ResumeLayout();

            RtfBuilder builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
            rtbStatus.Rtf = builder.ToRtf();

            PopulateItemGrid();

            this.ResumeLayout();
        }

        private void txtFilterFlatpack_TextChanged(object sender, EventArgs e)
        {
            UpdateFlatpackListBase();
            cmbFlatpacks.DroppedDown = true;
        }

        public void UpdateFlatpackListBase()
        {
            string searchText = txtFilterFlatpack.Text;
            var filteredList = new List<Data.Blueprint>(playerContext.GetAllBlueprints());

            filteredList = filteredList
                .Where(item => item.BluePrintType.IsFlatpack())
                .ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            filteredList = filteredList.OrderBy(p => p.Name).ToList();

            filteredList.Insert(0, new Data.Blueprint());
            var filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = filteredList;

            cmbFlatpacks.DataSource = filteredItemsBindingList;
        }

        public void UpdatePurityList()
        {
            IReadOnlyList<ResourcePurity> purities = Data.ResourcePurity.Purities;

            var filteredPurityBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredPurityBindingList.DataSource = purities;

            cmbPurity.DataSource = filteredPurityBindingList;
        }

        public void UpdateItemTypeList()
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
            Log.Debug("lvwBlueprints.SelectedItems.Count = " + lvwColonies.SelectedItems.Count);
            if (lvwColonies.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Text);
                Log.Debug("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Tag);
                selectedColony = lvwColonies.SelectedItems[0].SubItems[0].Tag as Baseline.Colony;
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                PopulateForm();
            }
        }
        void PopulateListView(List<Baseline.Colony> colonies)
        {
            if (colonies == null)
            {
                return;
            }

            Dictionary<string, ListViewItem> viewableColonies = new Dictionary<string, ListViewItem>();

            // First index what is viewable.
            foreach (ListViewItem item in lvwColonies.Items)
            {
                viewableColonies[(item.Tag as Data.Blueprint).UUID] = item;
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
        private void PopulateForm()
        {
            Log.Debug("PopulateForm called! selectedColony = " + (selectedColony != null ? selectedColony.PlanetName : "null"));
            using var guard = new ProgrammaticUpdateGuard(this);
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

            txtPlanetName.Text = colonyViewModel.PlanetName;
            txtColonyName.Text = colonyViewModel.ColonyName;
            txtSystemName.Text = colonyViewModel.Data.SystemName ?? "";



            Log.Debug("populatForm: Hiding excess controls started");
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
            Log.Debug("populatForm: Hiding excess controls finished");

            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);

            this.DoubleBuffered = true;
            List<ColonyStructure> structureControls = new List<ColonyStructure>();
            controlIndex = 0;
            foreach (Baseline.ColonyStructure structure in selectedColony.Structures)
            {
                Log.Debug("populatForm: processing structure started");
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
                colonyStructureControl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                colonyStructureControl.Colony = selectedColony;
                colonyStructureControl.ColonyStructureData = structure;
                colonyStructureControl.UpdateData();
                if (addControl)
                {
                    structureControls.Add(colonyStructureControl);
                }
                controlIndex++;
                Log.Debug("populatForm: processing structure finished");
            }
            Log.Debug("populatForm: Adding new controls started");
            flpColonyStructure.Controls.AddRange(structureControls.ToArray());
            Log.Debug("populatForm: Adding new controls finished");
            Log.Debug("populatForm: Making new controls visible started");
            foreach (ColonyStructure structureControl in structureControls)
            {
                structureControl.Visible = true;
            }
            Log.Debug("populatForm: Making new controls visible finished");

            Log.Debug("PopulateForm: Calling CalculateBuilt started");
            colonyViewModel.RecalculateStatus();
            Log.Debug("PopulateForm: Calling CalculateBuilt finished");
            Log.Debug("PopulateForm: Calling PopulateStatus started");
            RtfBuilder builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
            rtbStatus.Rtf = builder.ToRtf();
            Log.Debug("PopulateForm: Calling PopulateStatus finished");

            tabDetailedData.Visible = true;

            PopulateItemGrid();
            PopulateCommodityRequestGrid();

            this.ResumeLayout();
            Log.Debug("PopulateForm completed!");
        }

        private void tlpBase_Layout(object sender, LayoutEventArgs e)
        {
        }

        private void tlpBase_Resize(object sender, EventArgs e)
        {
            flpColonyData.Size = new System.Drawing.Size(tlpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left, flpColonyData.Size.Height);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwColonies.Size = new System.Drawing.Size(lvwColonies.Size.Width, flpSearchList.Size.Height - flpBlueprintSearch.Size.Height - flpBlueprintSearch.Margin.Top - flpBlueprintSearch.Margin.Bottom - lvwColonies.Margin.Top - lvwColonies.Margin.Bottom);
        }

        private void flpSearchList_Resize(object sender, EventArgs e)
        {
        }

        private void flpColonyData_Layout(object sender, LayoutEventArgs e)
        {
            tabDetailedData.Size = new System.Drawing.Size(flpColonyData.Size.Width - tabDetailedData.Margin.Right - tabDetailedData.Margin.Left, flpColonyData.Size.Height - flpBaseDetails.Size.Height - flpBaseDetails.Margin.Top - flpBaseDetails.Margin.Bottom - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom - tabDetailedData.Margin.Top - tabDetailedData.Margin.Bottom);
        }

        private void flpStructures_Layout(object sender, LayoutEventArgs e)
        {
            flpStructureData.Size = new System.Drawing.Size(flpStructures.Size.Width - lvwStructureTypes.Size.Width - lvwStructureTypes.Margin.Right - lvwStructureTypes.Margin.Left, flpStructures.Size.Height - flpStructureData.Margin.Top - flpStructureData.Margin.Bottom);
            lvwStructureTypes.Size = new System.Drawing.Size(lvwStructureTypes.Size.Width, flpStructures.Size.Height - lvwStructureTypes.Margin.Top - lvwStructureTypes.Margin.Bottom);
        }

        private void flpStructureData_Layout(object sender, LayoutEventArgs e)
        {
            flpColonyStructure.Size = new System.Drawing.Size(flpStructureData.Size.Width - flpColonyStructure.Margin.Left - flpColonyStructure.Margin.Right, flpStructureData.Size.Height - flpStatus.Size.Height - flpStatus.Margin.Top - flpStatus.Margin.Bottom - flpAddBox.Size.Height - flpAddBox.Margin.Top - flpAddBox.Margin.Bottom - flpColonyStructure.Margin.Top - flpColonyStructure.Margin.Bottom);
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private void tabPStructures_Layout(object sender, LayoutEventArgs e)
        {

        }

        private void tabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            // When switching to the Structures tab, refresh all structure controls
            // so their selection combos reflect current warehouse state
            if (tabDetailedData.SelectedTab == tabPStructures)
            {
                foreach (Control c in flpColonyStructure.Controls)
                {
                    if (c is ColonyStructure cs && cs.Visible)
                    {
                        cs.UpdateData();
                    }
                }
            }
        }

        private void cmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Data.ItemType itemType = cmbItemType.SelectedItem as Data.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null)
            {
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Resource)
                {
                    PopulateItemWithResources();
                    cmbPurity.Visible = true;
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Commodity)
                {
                    PopulateItemWithCommodities();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.WorkDetail)
                {
                    PopulateItemWithWorkerDetails();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Survey)
                {
                    PopulateItemWithSurveys();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Blueprint)
                {
                    PopulateItemWithBlueprints();
                }

                // Blueprint-based item types: populate from blueprints whose BlueprintType.OutputItemType matches
                if (itemType.ID == Data.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Share)
                {
                    PopulateItemWithBlueprintsByOutputType(itemType.ID);
                }
            }
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
                if (itemType.ID == Data.ItemType.ItemTypeEnum.WorkDetail)
                {
                    Data.WorkerDetail workerDetail = cmbItem.SelectedItem as Data.WorkerDetail;
                    if (workerDetail != null)
                    {
                        item.BaseItemTypeID = workerDetail.ID;
                        item.Name = workerDetail.Name;
                    }
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Survey)
                {
                    Baseline.Survey survey = cmbItem.SelectedItem as Baseline.Survey;
                    if (survey != null)
                    {
                        item.BaseItemTypeID = survey.UUID;
                        item.Name = survey.Name;
                    }
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Blueprint)
                {
                    Data.Blueprint blueprint = cmbItem.SelectedItem as Data.Blueprint;
                    if (blueprint != null)
                    {
                        item.BaseItemTypeID = blueprint.UUID;
                        item.Name = blueprint.Name;
                    }
                }

                // Blueprint-based item types (ShipPart, ShipHull, Munition, Flatpack, etc.)
                if (itemType.ID == Data.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Share)
                {
                    Data.Blueprint blueprint = cmbItem.SelectedItem as Data.Blueprint;
                    if (blueprint != null)
                    {
                        item.BaseItemTypeID = blueprint.UUID;
                        item.Name = blueprint.Name;
                    }
                }

                string quantityStr = txtQuantity.Text;
                if (quantityStr != null && quantityStr.Length > 0)
                {
                    int quantity = 0;
                    int.TryParse(quantityStr, out quantity);
                    item.Quantity = quantity;
                }

                // Set volume based on item type
                item.Volume = GetItemVolume(item, playerContext);

                colonyViewModel.AddItem(item);
                PopulateItemGrid();
            }
        }

        private static double GetItemVolume(Data.Item item, PlayerContext playerContext)
        {
            switch (item.ItemType)
            {
                case Data.ItemType.ItemTypeEnum.Resource:
                    return 1.0;
                case Data.ItemType.ItemTypeEnum.Commodity:
                    return 10.0;
                case Data.ItemType.ItemTypeEnum.WorkDetail:
                    return 50.0;
                case Data.ItemType.ItemTypeEnum.Blueprint:
                case Data.ItemType.ItemTypeEnum.Survey:
                    return 0.0;
                default:
                    // Manufactured items: read CargoVolumeSize from blueprint
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID) && playerContext != null)
                    {
                        Data.Blueprint bp = playerContext.FindBlueprint(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            double vol = 0;
                            bp.Properties.getDouble("CargoVolumeSize", 0, out vol);
                            return vol;
                        }
                    }
                    return 0.0;
            }
        }

        private void PopulateItemGrid()
        {
            // Populate item grid colonies items.
            dgvItems.CellValidating -= dgvItems_CellValidating;
            try { dgvItems.EndEdit(); } catch { }
            dgvItems.Rows.Clear();
            dgvItems.CellValidating += dgvItems_CellValidating;
            foreach (KeyValuePair<string, Item> itemEntry in colonyViewModel.GetItems())
            {
                dgvItems.Rows.Add();
                DataGridViewRow row = dgvItems.Rows[dgvItems.RowCount - 2];
                row.Tag = itemEntry.Value;
                row.Cells[0].Tag = itemEntry.Value;
                row.Cells[0].Value = itemEntry.Value.ItemType.ToString();
                row.Cells[1].Value = itemEntry.Value.ExtendedName;
                int lockedQty = colonyViewModel.Data.Locks != null
                    ? colonyViewModel.Data.Locks.GetLockedQuantity(itemEntry.Value.ItemType, itemEntry.Value.BaseItemTypeID)
                    : 0;
                row.Cells[2].Value = lockedQty;
                row.Cells[3].Value = itemEntry.Value.Quantity;
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
            Data.ItemType itemType = cmbItemType.SelectedItem as Data.ItemType;
            if (itemType != null)
            {
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Resource)
                {
                    PopulateItemWithResources();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Commodity)
                {
                    PopulateItemWithCommodities();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.WorkDetail)
                {
                    PopulateItemWithWorkerDetails();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Survey)
                {
                    PopulateItemWithSurveys();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.Blueprint)
                {
                    PopulateItemWithBlueprints();
                }
                if (itemType.ID == Data.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Data.ItemType.ItemTypeEnum.Share)
                {
                    PopulateItemWithBlueprintsByOutputType(itemType.ID);
                }
            }
            cmbItem.DroppedDown = true;
        }

        public void PopulateItemWithBlueprints()
        {
            string searchText = txtItemFilter.Text;

            List<Data.Blueprint> filteredList = new List<Data.Blueprint>(playerContext.GetAllBlueprints());
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(b => b.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(b => b.ExtendedName)
                    .ToList();
            }
            filteredList.Sort((x, y) => x.ExtendedName.CompareTo(y.ExtendedName));
            filteredList.Insert(0, new Data.Blueprint());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithBlueprintsByOutputType(Data.ItemType.ItemTypeEnum outputType)
        {
            string searchText = txtItemFilter.Text;
            string outputTypeName = outputType.ToString();

            List<Data.Blueprint> filteredList = new List<Data.Blueprint>();
            foreach (Data.Blueprint bp in playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                BlueprintType bpType = empireContext.FindBlueprintType(bp.BluePrintType);
                if (bpType == null || bpType.OutputItemType != outputTypeName) continue;
                filteredList.Add(bp);
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(b => b.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            filteredList.Sort((x, y) => x.ExtendedName.CompareTo(y.ExtendedName));
            filteredList.Insert(0, new Data.Blueprint());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithSurveys()
        {
            string searchText = txtItemFilter.Text;

            List<Baseline.Survey> filteredList = new List<Baseline.Survey>(playerContext.surveyList);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(s => s.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                             || s.PlanetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(s => s.PlanetName)
                    .ToList();
            }
            filteredList.Insert(0, new Baseline.Survey());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithWorkerDetails()
        {
            string searchText = txtItemFilter.Text;

            List<WorkerDetail> filteredList = new List<WorkerDetail>(Data.WorkerDetail.WorkerDetails);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(p => p.Name)
                    .ToList();
                filteredList.Insert(0, new WorkerDetail());
            }

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "ID";
            cmbItem.DisplayMember = "Name";
        }

        public void PopulateItemWithCommodities()
        {
            string searchText = txtItemFilter.Text;

            var filteredCommoditiesBindingList = new BindingSource();

            // Filter the complete commodity collection to only include items 
            // whose extended names contain that text (case-insensitive matching)
            bool addEmpty = false;
            List<Commodity> filteredList = new List<Commodity>(Data.Commodity.Commodities);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.ExtendedName)
                .ToList();
                addEmpty = true;
            }

            // Insert an empty blank entry at the beginning to allow the user to deselect their current selection
            if (addEmpty)
            {
                filteredList.Insert(0, new Commodity());
            }

            // Set the in-memory list as the DataSource for the BindingSource
            filteredCommoditiesBindingList.DataSource = filteredList;

            cmbItem.DataSource = filteredCommoditiesBindingList;
            cmbItem.ValueMember = "Name";
            cmbItem.DisplayMember = "ExtendedName";

        }
        public void PopulateItemWithResources()
        {
            string searchText = txtItemFilter.Text;

            var filteredResourcesBindingList = new BindingSource();

            // Filter the complete commodity collection to only include items 
            // whose extended names contain that text (case-insensitive matching)
            bool addEmpty = false;
            List<Resource> filteredList = new List<Resource>(Data.Resource.Resources);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.Name)
                .ToList();
                addEmpty = true;
            }

            // Insert an empty blank entry at the beginning to allow the user to deselect their current selection
            if (addEmpty)
            {
                filteredList.Insert(0, new Resource());
            }

            // Set the in-memory list as the DataSource for the BindingSource
            filteredResourcesBindingList.DataSource = filteredList;

            cmbItem.DataSource = filteredResourcesBindingList;
            cmbItem.ValueMember = "Name";
            cmbItem.DisplayMember = "Name";
        }

        private void dgvItems_SelectionChanged(object sender, EventArgs e)
        {
            // Prevent the SelectionChanged event from triggering an error if the current cell is null
            if (dgvItems.CurrentCell == null)
                return;

            if (dgvItems.SelectedRows.Count > 0) return;

            // Check if the current cell is not in the "Amount" column.
            if (dgvItems.Columns[dgvItems.CurrentCell.ColumnIndex].Name != "Amount")
            {
                // Programmatically deselect the cell
                dgvItems.CurrentCell.Selected = false;

                // Focus the "Amount" cell in the same row, if it exists.
                if (dgvItems.Rows[dgvItems.CurrentCell.RowIndex].Cells.Count > 1)
                {
                    dgvItems.CurrentCell = dgvItems.Rows[dgvItems.CurrentCell.RowIndex].Cells[3];
                }
            }
        }

        private void cmdAddCommodityRequest_Click(object sender, EventArgs e)
        {
            Data.Commodity commodity = cmbCommodityRequest.SelectedItem as Data.Commodity;
            if (commodity == null || string.IsNullOrEmpty(commodity.ID)) return;

            int qty;
            int.TryParse(txtCommodityRequestQuantity.Text, out qty);

            DateTime? needBy = null;
            string needByText = txtCommodityRequestNeedBy.Text?.Trim();
            if (!string.IsNullOrEmpty(needByText))
            {
                needBy = ParseCountdownToDateTime(needByText);
            }

            colonyViewModel.AddCommodityRequest(commodity.Name, qty, needBy);
            PopulateCommodityRequestGrid();
        }

        private void txtCommodityRequestFilter_TextChanged(object sender, EventArgs e)
        {
            UpdateCommodityRequestList();
            cmbCommodityRequest.DroppedDown = true;
        }

        private void UpdateCommodityRequestList()
        {
            string searchText = txtCommodityRequestFilter.Text;

            List<Data.Commodity> filteredList = new List<Data.Commodity>(Data.Commodity.Commodities);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(c => c.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(c => c.ExtendedName)
                    .ToList();
                filteredList.Insert(0, new Data.Commodity());
            }

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbCommodityRequest.DataSource = bindingList;
            cmbCommodityRequest.ValueMember = "Name";
            cmbCommodityRequest.DisplayMember = "ExtendedName";
        }

        private void PopulateCommodityRequestGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCommodityRequests.CellValidating -= dgvCommodityRequests_CellValidating;
            try { dgvCommodityRequests.EndEdit(); } catch { }
            dgvCommodityRequests.Rows.Clear();
            dgvCommodityRequests.CellValidating += dgvCommodityRequests_CellValidating;

            // Auto-delete expired fulfilled requests
            int cleaned = colonyViewModel.CleanupExpiredCommodityRequests();
            if (cleaned > 0)
                Log.Debug("Cleaned up {0} expired commodity requests", cleaned);

            foreach (CommodityRequested request in colonyViewModel.GetCommodityRequests())
            {
                int idx = dgvCommodityRequests.Rows.Add();
                DataGridViewRow row = dgvCommodityRequests.Rows[idx];
                row.Tag = request;
                row.Cells[0].Value = request.Name;
                row.Cells[1].Value = request.Requested;
                row.Cells[2].Value = request.Fulfilled;
                row.Cells[3].Value = FormatNeedByCountdown(request.NeedBy);

                if (request.Fulfilled)
                {
                    row.DefaultCellStyle.Font = new System.Drawing.Font(dgvCommodityRequests.Font, System.Drawing.FontStyle.Strikeout);
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
                }
                else
                {
                    row.DefaultCellStyle.Font = dgvCommodityRequests.Font;
                    row.DefaultCellStyle.ForeColor = dgvCommodityRequests.ForeColor;
                }
            }
        }

        private void dgvCommodityRequests_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCommodityRequests.IsCurrentCellDirty)
            {
                dgvCommodityRequests.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void dgvCommodityRequests_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvCommodityRequests.Rows[e.RowIndex];
            CommodityRequested request = row.Tag as CommodityRequested;
            if (request == null) return;

            // Column 1 = Amount (Requested)
            if (e.ColumnIndex == 1)
            {
                int value;
                if (int.TryParse(row.Cells[1].Value?.ToString(), out value))
                    request.Requested = value;
                playerContext.writeContext();
            }
            // Column 2 = Fulfilled (checkbox)
            else if (e.ColumnIndex == 2)
            {
                bool fulfilled = row.Cells[2].Value is bool b && b;
                request.Fulfilled = fulfilled;
                if (fulfilled)
                    request.Delivered = request.Requested;
                else
                    request.Delivered = 0;
                playerContext.writeContext();
                // Refresh to update strikethrough
                PopulateCommodityRequestGrid();
            }
            // Column 3 = NeedBy (countdown format)
            else if (e.ColumnIndex == 3)
            {
                string text = row.Cells[3].Value?.ToString() ?? "";
                DateTime? parsed = ParseCountdownToDateTime(text);
                if (parsed.HasValue)
                {
                    request.NeedBy = parsed.Value;
                    playerContext.writeContext();
                }
            }
        }

        private void dgvCommodityRequests_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Column 1 = Amount
            if (e.ColumnIndex == 1)
            {
                string value = e.FormattedValue?.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                    return;
                }
                if (!int.TryParse(value, out _))
                {
                    e.Cancel = true;
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
                }
                else
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                }
            }
            // Column 3 = NeedBy (countdown format)
            else if (e.ColumnIndex == 3)
            {
                string value = e.FormattedValue?.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                    return;
                }
                if (ParseCountdownToDateTime(value) == null)
                {
                    e.Cancel = true;
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "Use countdown format: e.g. 2d 6h 30m";
                }
                else
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                }
            }
        }

        private void dgvCommodityRequests_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (dgvCommodityRequests.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvCommodityRequests.SelectedRows)
            {
                CommodityRequested request = row.Tag as CommodityRequested;
                if (request != null)
                    colonyViewModel.RemoveCommodityRequest(request);
            }
            PopulateCommodityRequestGrid();
            e.Handled = true;
        }

        // -----------------------------------------------------------------------
        // Countdown Format Helpers
        // -----------------------------------------------------------------------

        /// <summary>
        /// Parses a countdown string (e.g. "2d 6h 30m") into a DateTime (now + duration).
        /// Returns null if the format is invalid.
        /// </summary>
        private DateTime? ParseCountdownToDateTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(text.Trim(),
                @"^(?:(\d+)d\s*)?(?:(\d+)h\s*)?(?:(\d+)m\s*)?(?:(\d+)s)?$");
            if (!match.Success) return null;
            if (!match.Groups[1].Success && !match.Groups[2].Success && !match.Groups[3].Success && !match.Groups[4].Success)
                return null;

            int days = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0;
            int hours = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 0;
            int minutes = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;
            int seconds = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;

            long totalSeconds = ((long)days * 24 + hours) * 3600 + minutes * 60 + seconds;
            if (totalSeconds <= 0) return null;
            return DateTime.Now.AddSeconds(totalSeconds);
        }

        /// <summary>
        /// Formats a NeedBy DateTime as a countdown string relative to now.
        /// Returns empty string for DateTime.MinValue. Shows negative values as "overdue".
        /// </summary>
        private string FormatNeedByCountdown(DateTime needBy)
        {
            if (needBy == DateTime.MinValue) return "";
            var remaining = needBy - DateTime.Now;
            if (remaining.TotalSeconds <= 0)
                return "overdue";

            string result = "";
            if (remaining.Days > 0) result += $"{remaining.Days}d ";
            if (remaining.Hours > 0 || remaining.Days > 0) result += $"{remaining.Hours}h ";
            if (remaining.Minutes > 0 || remaining.Hours > 0 || remaining.Days > 0) result += $"{remaining.Minutes}m";
            else result += $"{remaining.Seconds}s";
            return result.Trim();
        }

        private void dgvItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (dgvItems.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvItems.SelectedRows)
            {
                Item item = row.Tag as Item;
                if (item != null)
                {
                    // Prevent deletion of items with locked quantities
                    int locked = colonyViewModel.Data.Locks != null
                        ? colonyViewModel.Data.Locks.GetLockedQuantity(item.ItemType, item.BaseItemTypeID)
                        : 0;
                    if (locked > 0)
                    {
                        System.Windows.Forms.MessageBox.Show(
                            $"Cannot delete '{item.ExtendedName}' â€” {locked} locked by structures.",
                            "Item Locked",
                            System.Windows.Forms.MessageBoxButtons.OK,
                            System.Windows.Forms.MessageBoxIcon.Warning);
                        continue;
                    }
                    colonyViewModel.RemoveItem(item.UUID);
                }
            }
            PopulateItemGrid();
            e.Handled = true;
        }

        private void dgvItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dgvItems.Rows[e.RowIndex];
            Item item = row.Tag as Item;
            if (item == null) return;

            // Amount column is index 3
            if (e.ColumnIndex == 3)
            {
                int qty = 0;
                int.TryParse(row.Cells[3].Value?.ToString(), out qty);
                item.Quantity = qty;

                // Recalculate status so locks and unallocated workers update
                structures_ColonyStructureDataChanged(sender, EventArgs.Empty);
            }
        }

        private void dgvItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // Only validate the Amount column (index 3)
            if (e.ColumnIndex != 3) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            // Treat empty as 0
            if (string.IsNullOrEmpty(value))
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvItems.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!int.TryParse(value, out _))
            {
                e.Cancel = true;
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.LightCoral;
                dgvItems.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
            }
            else
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = System.Drawing.Color.White;
                dgvItems.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void dgvCommodityRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCommodityRequests.CurrentCell == null) return;
            if (dgvCommodityRequests.SelectedRows.Count > 0) return;

            // Allow editing Amount (1), Fulfilled (2), and NeedBy (3) columns.
            // Redirect Name column (0) clicks to Amount.
            int col = dgvCommodityRequests.CurrentCell.ColumnIndex;
            if (col == 0)
            {
                using var guard = new ProgrammaticUpdateGuard(this);
                dgvCommodityRequests.CurrentCell = dgvCommodityRequests.Rows[dgvCommodityRequests.CurrentCell.RowIndex].Cells[1];
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }
    }
}
