using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
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

        private Models.Colony selectedColony;
        private ColonyViewModel colonyViewModel;
        private ColonyStatusCalculator statusCalculator => colonyViewModel?.Calculator;

        // ListView sorting state
        private int _sortColumn = 0;
        private SortOrder _sortOrder = SortOrder.Ascending;

        // Deferred tab update flags
        private bool _structuresDirty = false;
        private bool _warehouseDirty = false;
        private bool _workersDirty = false;

        // Structure control pool — reuse controls instead of creating/disposing
        private readonly List<ColonyStructure> _structurePool = new List<ColonyStructure>();
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

            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            colonyViewModel.RecalculateStatus();

            lvwColonies.View = View.Details;
            lvwColonies.Columns.Add("Planet", 50);
            lvwColonies.Columns.Add("Name", 100);
            lvwColonies.ColumnClick += lvwColonies_ColumnClick;
            lvwColonies.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(playerContext.GetCurrentPlayerColonies());

            UpdateCommodityRequestList();
            UpdateTitle();

            // Wire filter handler
            txtColonyListFilter.TextChanged += txtColonyListFilter_TextChanged;

            // Enable owner-draw so tab BackColor renders with visual styles
            tabDetailedData.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabDetailedData.DrawItem += tabDetailedData_DrawItem;

            // Wire write-through handlers
            txtPlanetName.TextChanged += txtPlanetName_TextChanged;
            txtColonyName.TextChanged += txtColonyName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            lvwColonies.Items.Clear();
            // Return structure controls to pool
            while (flpColonyStructure.Controls.Count > 0)
            {
                var ctrl = flpColonyStructure.Controls[flpColonyStructure.Controls.Count - 1] as ColonyStructure;
                flpColonyStructure.Controls.RemoveAt(flpColonyStructure.Controls.Count - 1);
                if (ctrl != null)
                {
                    ctrl.Visible = false;
                    ctrl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                    if (!_structurePool.Contains(ctrl))
                        _structurePool.Add(ctrl);
                }
            }
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            colonyViewModel.RecalculateStatus();
            PopulateListView(playerContext.GetCurrentPlayerColonies());
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";
            UpdateTitle();
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnColonyDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            // Skip if this form triggered the change (via structures_ColonyStructureDataChanged)
            if (_isProgrammaticUpdate > 0) return;
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

            if (selectedColony != null &&
                ColonyImportHelper.IsDuplicateName(
                    playerContext.GetCurrentPlayerColonies(),
                    txtColonyName.Text,
                    selectedColony.UUID))
            {
                txtColonyName.SetError("Colony name already in use");
                cmdSave.Enabled = false;
            }
            else
            {
                txtColonyName.ClearError();
                cmdSave.Enabled = true;
            }
        }

        private void txtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.Data.SystemName = txtSystemName.Text;
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            if (string.IsNullOrEmpty(colonyViewModel.Data.OwnerUUID))
            {
                colonyViewModel.Data.OwnerUUID = playerContext.CurrentPlayerUUID;
            }
            colonyViewModel.Save();
            txtColonyListFilter_TextChanged(sender, e);
            UpdateTitle();
        }

        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwColonies.SelectedItems.Clear();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID)) return;

            var result = MessageBox.Show(
                $"Delete colony '{selectedColony.PlanetName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.colonyList.Remove(selectedColony);
            playerContext.writeContext();
            ClearForm();
            txtColonyListFilter_TextChanged(sender, e);
            UpdateTitle();
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";

            // Return structure controls to pool
            while (flpColonyStructure.Controls.Count > 0)
            {
                var ctrl = flpColonyStructure.Controls[flpColonyStructure.Controls.Count - 1] as ColonyStructure;
                flpColonyStructure.Controls.RemoveAt(flpColonyStructure.Controls.Count - 1);
                if (ctrl != null)
                {
                    ctrl.Visible = false;
                    ctrl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                    if (!_structurePool.Contains(ctrl))
                        _structurePool.Add(ctrl);
                }
            }

            dgvItems.Rows.Clear();
            dgvCommodityRequests.Rows.Clear();
            rtbStatus.Text = "";
            UpdateTabTitles();
            UpdateTabWarnings();
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
            ColonyStructure colonyStructureControl = GetPooledStructureControl();
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
            UpdateTabWarnings();
            this.ResumeLayout();
        }
        private void structures_ColonyStructureDataChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            using var guard = new ProgrammaticUpdateGuard(this);

            colonyViewModel.RecalculateStatus();

            // If the sender is a ColonyStructure control, this is an in-place property change
            // (worker toggle, built/online, etc.) — no need to rebuild all controls.
            bool isStructuralChange = !(sender is ColonyStructure);

            if (isStructuralChange)
            {
                flpColonyStructure.SuspendLayout();

                // Build a map of existing controls by their data reference
                var controlMap = new Dictionary<Models.ColonyStructure, ColonyStructure>();
                foreach (Control c in flpColonyStructure.Controls)
                {
                    if (c is ColonyStructure cs && cs.ColonyStructureData != null)
                    {
                        controlMap[cs.ColonyStructureData] = cs;
                    }
                }

                // Return orphaned controls to the pool instead of disposing
                foreach (var orphan in controlMap
                    .Where(kv => !selectedColony.Structures.Contains(kv.Key))
                    .Select(kv => kv.Value)
                    .ToList())
                {
                    flpColonyStructure.Controls.Remove(orphan);
                    orphan.Visible = false;
                    orphan.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                    if (!_structurePool.Contains(orphan))
                        _structurePool.Add(orphan);
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
                    Models.ColonyStructure structure = selectedColony.Structures[i];
                    ColonyStructure ctrl;
                    if (!controlMap.TryGetValue(structure, out ctrl))
                    {
                        // Pull from pool or create new
                        ctrl = GetPooledStructureControl();
                        ctrl.Colony = selectedColony;
                        ctrl.ColonyStructureData = structure;
                        ctrl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                        ctrl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                        flpColonyStructure.Controls.Add(ctrl);
                        ctrl.Visible = true;
                        controlIndexMap[ctrl] = flpColonyStructure.Controls.Count - 1;
                    }
                    int currentIndex;
                    if (controlIndexMap.TryGetValue(ctrl, out currentIndex) && currentIndex != i)
                    {
                        flpColonyStructure.Controls.SetChildIndex(ctrl, i);
                    }
                    ctrl.UpdateData();
                }

                flpColonyStructure.ResumeLayout();
            }

            // Defer item grid refresh if not on the Warehousing tab
            if (tabDetailedData.SelectedTab == tabPWarehousing)
            {
                PopulateItemGrid();
            }
            else
            {
                _warehouseDirty = true;
            }

            RtfBuilder builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
            rtbStatus.Rtf = builder.ToRtf();

            // Notify other forms (e.g. ColonyActivityForm) that colony data changed
            if (selectedColony != null)
                playerContext.OnColonyDataChanged(selectedColony.UUID);

            UpdateTabTitles();
            UpdateTabWarnings();
        }

        private void txtFilterFlatpack_TextChanged(object sender, EventArgs e)
        {
            UpdateFlatpackListBase();
            cmbFlatpacks.DroppedDown = true;
        }

        public void UpdateFlatpackListBase()
        {
            string searchText = txtFilterFlatpack.Text;
            var filteredList = new List<Models.Blueprint>(playerContext.GetAllBlueprints());

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

            filteredList.Insert(0, new Models.Blueprint());
            var filteredItemsBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredItemsBindingList.DataSource = filteredList;

            cmbFlatpacks.DataSource = filteredItemsBindingList;
        }

        public void UpdatePurityList()
        {
            IReadOnlyList<ResourcePurity> purities = Models.ResourcePurity.Purities;

            var filteredPurityBindingList = new BindingSource();
            // Set the in-memory list as the DataSource for the BindingSource
            filteredPurityBindingList.DataSource = purities;

            cmbPurity.DataSource = filteredPurityBindingList;
        }

        public void UpdateItemTypeList()
        {
            IReadOnlyList<ItemType> itemTypes = Models.ItemType.ItemTypes;

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

        private void txtColonyListFilter_TextChanged(object sender, EventArgs e)
        {
            string filter = txtColonyListFilter.Text;
            var colonies = playerContext.GetCurrentPlayerColonies();
            if (!string.IsNullOrEmpty(filter))
            {
                colonies = colonies
                    .Where(c => (c.PlanetName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                             || (c.ColonyName ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            lvwColonies.Items.Clear();
            PopulateListView(colonies);
            lvwColonies.Sort();
        }

        private void lvwColonies_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            Log.Debug("lvwBlueprints.SelectedItems.Count = " + lvwColonies.SelectedItems.Count);
            if (lvwColonies.SelectedItems.Count == 1)
            {
                Log.Debug("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Text);
                Log.Debug("Selected item = " + lvwColonies.SelectedItems[0].SubItems[0].Tag);
                selectedColony = lvwColonies.SelectedItems[0].SubItems[0].Tag as Models.Colony;
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                PopulateForm();
            }
        }
        void PopulateListView(List<Models.Colony> colonies)
        {
            if (colonies == null)
            {
                return;
            }

            Dictionary<string, ListViewItem> viewableColonies = new Dictionary<string, ListViewItem>();

            // First index what is viewable.
            foreach (ListViewItem item in lvwColonies.Items)
            {
                viewableColonies[(item.Tag as Models.Colony).UUID] = item;
            }

            // Now add or update what is viewable.
            foreach (Models.Colony colony in colonies)
            {
                ListViewItem item;
                bool found = viewableColonies.TryGetValue(colony.UUID, out item);
                if (!found)
                {
                    item = new ListViewItem(colony.PlanetName);
                    item.SubItems.Add(colony.ColonyName);
                }
                else
                {
                    item.SubItems[0].Text = colony.PlanetName;
                    if (item.SubItems.Count > 1)
                        item.SubItems[1].Text = colony.ColonyName;
                    else
                        item.SubItems.Add(colony.ColonyName);
                }
                item.Tag = colony;
                item.SubItems[0].Tag = colony;

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

            if (selectedColony == null)
            {
                return;
            }

            // Force creation of an item.
            if (selectedColony.Items.Count() == 0)
            {
                selectedColony.Items.AddItem(new Models.Item() { UUID = Guid.NewGuid().ToString() });
            }

            txtPlanetName.Text = colonyViewModel.PlanetName;
            txtColonyName.Text = colonyViewModel.ColonyName;
            txtSystemName.Text = colonyViewModel.Data.SystemName ?? "";

            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);

            // --- Structure control pool ---
            // Return all current controls to the pool and detach from the panel.
            flpColonyStructure.SuspendLayout();
            while (flpColonyStructure.Controls.Count > 0)
            {
                var ctrl = flpColonyStructure.Controls[flpColonyStructure.Controls.Count - 1] as ColonyStructure;
                flpColonyStructure.Controls.RemoveAt(flpColonyStructure.Controls.Count - 1);
                if (ctrl != null)
                {
                    ctrl.Visible = false;
                    ctrl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                    if (!_structurePool.Contains(ctrl))
                        _structurePool.Add(ctrl);
                }
            }

            bool structuresTabActive = tabDetailedData.SelectedTab == tabPStructures;

            foreach (Models.ColonyStructure structure in selectedColony.Structures)
            {
                ColonyStructure colonyStructureControl = GetPooledStructureControl();

                colonyStructureControl.ColonyStructureDataChanged -= structures_ColonyStructureDataChanged;
                colonyStructureControl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                colonyStructureControl.Colony = selectedColony;
                colonyStructureControl.ColonyStructureData = structure;
                if (structuresTabActive)
                {
                    colonyStructureControl.UpdateData();
                }
                flpColonyStructure.Controls.Add(colonyStructureControl);
                colonyStructureControl.Visible = true;
            }

            _structuresDirty = !structuresTabActive;
            flpColonyStructure.ResumeLayout();

            colonyViewModel.RecalculateStatus();
            RtfBuilder builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
            rtbStatus.Rtf = builder.ToRtf();

            // Defer item and commodity grids unless their tab is active
            if (tabDetailedData.SelectedTab == tabPWarehousing)
            {
                PopulateItemGrid();
                _warehouseDirty = false;
            }
            else
            {
                _warehouseDirty = true;
            }

            if (tabDetailedData.SelectedTab == tabPWorkers)
            {
                PopulateCommodityRequestGrid();
                _workersDirty = false;
            }
            else
            {
                _workersDirty = true;
                // Still update tab title with active request count (cheap)
                UpdateTabTitles();
            }

            UpdateTabWarnings();

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

        private void UpdateTitle()
        {
            var player = playerContext.CurrentPlayer;
            string playerName = player != null ? player.Name : "No Player";
            int colonyCount = playerContext.GetCurrentPlayerColonies()?.Count ?? 0;
            Text = $"Manage Colonies - {playerName} : {colonyCount}";
        }

        private void UpdateTabTitles()
        {
            int structureCount = selectedColony?.Structures?.Count ?? 0;
            tabPStructures.Text = $"Structures : {structureCount}";

            int activeRequests = 0;
            if (selectedColony?.Commodities != null)
            {
                var now = DateTime.Now;
                activeRequests = selectedColony.Commodities.Count(r =>
                    !r.Fulfilled && (r.NeedBy == DateTime.MinValue || r.NeedBy > now));
            }
            tabPWorkers.Text = $"Workers : {activeRequests}";
        }

        private void lvwColonies_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == _sortColumn)
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sortColumn = e.Column;
                _sortOrder = SortOrder.Ascending;
            }
            lvwColonies.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            lvwColonies.Sort();
        }

        /// <summary>
        /// Returns an unused control from the pool, or creates a new one if the pool
        /// has no free controls. A "free" control is one not currently parented to
        /// flpColonyStructure.
        /// </summary>
        private ColonyStructure GetPooledStructureControl()
        {
            foreach (var ctrl in _structurePool)
            {
                if (ctrl.Parent == null || ctrl.Parent != flpColonyStructure)
                    return ctrl;
            }
            var newCtrl = new ColonyStructure();
            _structurePool.Add(newCtrl);
            return newCtrl;
        }

        private void tabPStructures_Layout(object sender, LayoutEventArgs e)
        {

        }

        private void tabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (tabDetailedData.SelectedTab == tabPStructures && _structuresDirty)
            {
                flpColonyStructure.SuspendLayout();
                foreach (Control c in flpColonyStructure.Controls)
                {
                    if (c is ColonyStructure cs && cs.Visible)
                    {
                        cs.UpdateData();
                    }
                }
                flpColonyStructure.ResumeLayout();
                _structuresDirty = false;
            }
            else if (tabDetailedData.SelectedTab == tabPWarehousing && _warehouseDirty)
            {
                PopulateItemGrid();
                _warehouseDirty = false;
            }
            else if (tabDetailedData.SelectedTab == tabPWorkers && _workersDirty)
            {
                PopulateCommodityRequestGrid();
                _workersDirty = false;
            }
        }

        private void cmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null)
            {
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
                {
                    PopulateItemWithResources();
                    cmbPurity.Visible = true;
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
                {
                    PopulateItemWithCommodities();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
                {
                    PopulateItemWithWorkerDetails();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
                {
                    PopulateItemWithSurveys();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
                {
                    PopulateItemWithBlueprints();
                }

                // Blueprint-based item types: populate from blueprints whose BlueprintType.OutputItemType matches
                if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Share)
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
            Models.Item item = new Models.Item() { UUID = Guid.NewGuid().ToString() };
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            if (itemType != null)
            {
                item.ItemType = itemType.ID;

                if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
                {
                    Models.Resource resource = cmbItem.SelectedItem as Models.Resource;
                    if (resource != null)
                    {
                        item.BaseItemTypeID = resource.Name;
                        item.Name = resource.Name;
                    }
                    Models.ResourcePurity purity = cmbPurity.SelectedItem as Models.ResourcePurity;
                    if (purity != null)
                    {
                        item.ResourcePurity = purity.Name;
                    }
                    else
                    {
                        item.ResourcePurity = Models.ResourcePurity.ItemTypeMapByEnum[Models.ResourcePurity.PurityEnum.Refined].Name;
                    }
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
                {
                    Models.Commodity commodity = cmbItem.SelectedItem as Models.Commodity;
                    if (commodity != null)
                    {
                        item.BaseItemTypeID = commodity.Name;
                        item.Name = commodity.Name;
                    }
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
                {
                    Models.WorkerDetail workerDetail = cmbItem.SelectedItem as Models.WorkerDetail;
                    if (workerDetail != null)
                    {
                        item.BaseItemTypeID = workerDetail.ID;
                        item.Name = workerDetail.Name;
                    }
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
                {
                    Models.Survey survey = cmbItem.SelectedItem as Models.Survey;
                    if (survey != null)
                    {
                        item.BaseItemTypeID = survey.UUID;
                        item.Name = survey.Name;
                    }
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
                {
                    Models.Blueprint blueprint = cmbItem.SelectedItem as Models.Blueprint;
                    if (blueprint != null)
                    {
                        item.BaseItemTypeID = blueprint.UUID;
                        item.Name = blueprint.Name;
                    }
                }

                // Blueprint-based item types (ShipPart, ShipHull, Munition, Flatpack, etc.)
                if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Share)
                {
                    Models.Blueprint blueprint = cmbItem.SelectedItem as Models.Blueprint;
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

        private static double GetItemVolume(Models.Item item, PlayerContext playerContext)
        {
            switch (item.ItemType)
            {
                case Models.ItemType.ItemTypeEnum.Resource:
                    return 1.0;
                case Models.ItemType.ItemTypeEnum.Commodity:
                    return 10.0;
                case Models.ItemType.ItemTypeEnum.WorkDetail:
                    return 50.0;
                case Models.ItemType.ItemTypeEnum.Blueprint:
                case Models.ItemType.ItemTypeEnum.Survey:
                    return 0.0;
                default:
                    // Manufactured items: read CargoVolumeSize from blueprint
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID) && playerContext != null)
                    {
                        Models.Blueprint bp = playerContext.FindBlueprint(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            double vol = 0;
                            bp.Properties.getDouble("Cargo Volume Size", 0, out vol);
                            return vol;
                        }
                    }
                    return 0.0;
            }
        }

        private void PopulateItemGrid()
        {
            dgvItems.CellValidating -= dgvItems_CellValidating;
            try { dgvItems.EndEdit(); } catch { }

            // Index existing rows by item UUID for in-place update
            var existingRows = new Dictionary<string, DataGridViewRow>();
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                var item = row.Tag as Item;
                if (item != null && !string.IsNullOrEmpty(item.UUID))
                    existingRows[item.UUID] = row;
            }

            var seen = new HashSet<string>();
            foreach (KeyValuePair<string, Item> itemEntry in colonyViewModel.GetItems())
            {
                var itemValue = itemEntry.Value;
                seen.Add(itemValue.UUID);

                DataGridViewRow row;
                if (existingRows.TryGetValue(itemValue.UUID, out row))
                {
                    // Update in place
                    row.Cells[0].Value = itemValue.ItemType.ToString();
                    row.Cells[1].Value = itemValue.ExtendedName;
                }
                else
                {
                    // Add new row
                    int idx = dgvItems.Rows.Add();
                    row = dgvItems.Rows[idx];
                    row.Tag = itemValue;
                    row.Cells[0].Tag = itemValue;
                    row.Cells[0].Value = itemValue.ItemType.ToString();
                    row.Cells[1].Value = itemValue.ExtendedName;
                }

                int lockedQty = colonyViewModel.Data.Locks != null
                    ? colonyViewModel.Data.Locks.GetLockedQuantity(itemValue.ItemType, itemValue.BaseItemTypeID)
                    : 0;
                row.Cells[2].Value = lockedQty;
                row.Cells[3].Value = itemValue.Quantity;
            }

            // Remove rows for deleted items (iterate backwards)
            for (int i = dgvItems.Rows.Count - 1; i >= 0; i--)
            {
                var item = dgvItems.Rows[i].Tag as Item;
                if (item != null && !seen.Contains(item.UUID))
                    dgvItems.Rows.RemoveAt(i);
            }

            dgvItems.CellValidating += dgvItems_CellValidating;
        }

        /// <summary>
        /// Refreshes only the Locked column (index 2) in the item grid without
        /// rebuilding the entire grid. Used after worker assignment changes.
        /// </summary>
        private void RefreshItemGridLocks()
        {
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                Item item = row.Tag as Item;
                if (item == null) continue;
                int lockedQty = colonyViewModel.Data.Locks != null
                    ? colonyViewModel.Data.Locks.GetLockedQuantity(item.ItemType, item.BaseItemTypeID)
                    : 0;
                row.Cells[2].Value = lockedQty;
            }
        }

        private void cmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null)
            {
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
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
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            if (itemType != null)
            {
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
                {
                    PopulateItemWithResources();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
                {
                    PopulateItemWithCommodities();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
                {
                    PopulateItemWithWorkerDetails();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
                {
                    PopulateItemWithSurveys();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
                {
                    PopulateItemWithBlueprints();
                }
                if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.ShipHull ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Munition ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Flatpack ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                    itemType.ID == Models.ItemType.ItemTypeEnum.Share)
                {
                    PopulateItemWithBlueprintsByOutputType(itemType.ID);
                }
            }
            cmbItem.DroppedDown = true;
        }

        public void PopulateItemWithBlueprints()
        {
            string searchText = txtItemFilter.Text;

            List<Models.Blueprint> filteredList = new List<Models.Blueprint>(playerContext.GetAllBlueprints());
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(b => b.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(b => b.ExtendedName)
                    .ToList();
            }
            filteredList.Sort((x, y) => x.ExtendedName.CompareTo(y.ExtendedName));
            filteredList.Insert(0, new Models.Blueprint());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithBlueprintsByOutputType(Models.ItemType.ItemTypeEnum outputType)
        {
            string searchText = txtItemFilter.Text;
            string outputTypeName = outputType.ToString();

            List<Models.Blueprint> filteredList = new List<Models.Blueprint>();
            foreach (Models.Blueprint bp in playerContext.GetAllBlueprints())
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
            filteredList.Insert(0, new Models.Blueprint());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithSurveys()
        {
            string searchText = txtItemFilter.Text;

            List<Models.Survey> filteredList = new List<Models.Survey>(playerContext.surveyList);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(s => s.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                             || s.PlanetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(s => s.PlanetName)
                    .ToList();
            }
            filteredList.Insert(0, new Models.Survey());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbItem.DataSource = bindingList;
            cmbItem.ValueMember = "UUID";
            cmbItem.DisplayMember = "ExtendedName";
        }

        public void PopulateItemWithWorkerDetails()
        {
            string searchText = txtItemFilter.Text;

            List<WorkerDetail> filteredList = new List<WorkerDetail>(Models.WorkerDetail.WorkerDetails);
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
            List<Commodity> filteredList = new List<Commodity>(Models.Commodity.Commodities);
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
            List<Resource> filteredList = new List<Resource>(Models.Resource.Resources);
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
            Models.Commodity commodity = cmbCommodityRequest.SelectedItem as Models.Commodity;
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
            UpdateTabWarnings();
        }

        private void txtCommodityRequestFilter_TextChanged(object sender, EventArgs e)
        {
            UpdateCommodityRequestList();
            cmbCommodityRequest.DroppedDown = true;
        }

        private void UpdateCommodityRequestList()
        {
            string searchText = txtCommodityRequestFilter.Text;

            List<Models.Commodity> filteredList = new List<Models.Commodity>(Models.Commodity.Commodities);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(c => c.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(c => c.ExtendedName)
                    .ToList();
                filteredList.Insert(0, new Models.Commodity());
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

            // Auto-delete expired fulfilled requests
            int cleaned = colonyViewModel.CleanupExpiredCommodityRequests();
            if (cleaned > 0)
                Log.Debug("Cleaned up {0} expired commodity requests", cleaned);

            // Index existing rows by their CommodityRequested reference
            var existingRows = new Dictionary<CommodityRequested, DataGridViewRow>();
            foreach (DataGridViewRow row in dgvCommodityRequests.Rows)
            {
                var req = row.Tag as CommodityRequested;
                if (req != null)
                    existingRows[req] = row;
            }

            var seen = new HashSet<CommodityRequested>();
            var requests = colonyViewModel.GetCommodityRequests();
            foreach (CommodityRequested request in requests)
            {
                seen.Add(request);

                DataGridViewRow row;
                if (!existingRows.TryGetValue(request, out row))
                {
                    int idx = dgvCommodityRequests.Rows.Add();
                    row = dgvCommodityRequests.Rows[idx];
                    row.Tag = request;
                }

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

            // Remove rows for deleted/cleaned requests (iterate backwards)
            for (int i = dgvCommodityRequests.Rows.Count - 1; i >= 0; i--)
            {
                var req = dgvCommodityRequests.Rows[i].Tag as CommodityRequested;
                if (req != null && !seen.Contains(req))
                    dgvCommodityRequests.Rows.RemoveAt(i);
            }

            dgvCommodityRequests.CellValidating += dgvCommodityRequests_CellValidating;

            UpdateTabTitles();
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

            UpdateTabWarnings();
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
            UpdateTabWarnings();
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

                // Recalculate status without rebuilding the entire form
                colonyViewModel.RecalculateStatus();
                RtfBuilder builder = new RtfBuilder();
                ColonyStatusCalculator.PopulateStatus(builder, statusCalculator.finalActualStatus);
                rtbStatus.Rtf = builder.ToRtf();

                _structuresDirty = true;
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
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }

        private void cmdImportColony_Click(object sender, EventArgs e)
        {
            if (!System.Windows.Forms.Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML content found on the clipboard.\n\nCopy colony data from the game browser first.",
                    "No HTML", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                MessageBox.Show("No player selected. Select a player profile first.",
                    "No Player", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var parser = new ColonyParser();
                var tempColony = parser.ParseClipboardToTemp(empireContext, out string extractedHtml);

                if (tempColony == null)
                    return;

                if (string.IsNullOrEmpty(tempColony.ColonyName))
                {
                    // Fall back to current behavior when no colony name is parsed
                    parser.ProcessClipboard(selectedColony, empireContext);

                    if (string.IsNullOrEmpty(selectedColony.OwnerUUID))
                    {
                        selectedColony.OwnerUUID = playerContext.CurrentPlayerUUID;
                    }

                    colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                    PopulateForm();

                    Log.Info("Colony imported from clipboard (fallback): {0} ({1} structures, {2} commodity requests)",
                        selectedColony.PlanetName, selectedColony.Structures.Count, selectedColony.Commodities.Count);
                    return;
                }

                var existingColony = ColonyImportHelper.FindByName(
                    playerContext.GetCurrentPlayerColonies(), tempColony.ColonyName);

                if (existingColony != null)
                {
                    ColonyImportHelper.MergeIdentity(existingColony, tempColony);

                    parser.ProcessHtml(existingColony, extractedHtml, empireContext);

                    selectedColony = existingColony;

                    Log.Info("Colony updated via dedup: {0} ({1} structures, {2} commodity requests)",
                        existingColony.ColonyName, existingColony.Structures.Count, existingColony.Commodities.Count);
                }
                else
                {
                    var newColony = ColonyImportHelper.CreateFromTemp(tempColony, playerContext.CurrentPlayerUUID);
                    playerContext.colonyList.Add(newColony);
                    selectedColony = newColony;

                    Log.Info("New colony created via dedup: {0} ({1} structures, {2} commodity requests)",
                        newColony.ColonyName, newColony.Structures.Count, newColony.Commodities.Count);
                }

                playerContext.writeContext();
                playerContext.OnColonyDataChanged(selectedColony.UUID);

                // Refresh list view
                txtColonyListFilter_TextChanged(sender, e);

                // Select the imported colony in the list view
                foreach (ListViewItem item in lvwColonies.Items)
                {
                    if ((item.Tag as Models.Colony)?.UUID == selectedColony.UUID)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }

                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                PopulateForm();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error importing colony from clipboard");
                MessageBox.Show("Failed to import colony: " + ex.Message,
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cmdImportClipboard_Click(object sender, EventArgs e)
        {
            if (!System.Windows.Forms.Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML content found on the clipboard.\n\nCopy colony data from the game browser first.",
                    "No HTML", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string clipboardData = System.Windows.Forms.Clipboard.GetText(TextDataFormat.Html);

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
                dlg.DefaultExt = "html";
                dlg.FileName = "ColonyCapture.html";
                dlg.Title = "Save Clipboard HTML";

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    System.IO.File.WriteAllText(dlg.FileName, clipboardData, System.Text.Encoding.UTF8);
                    Log.Info("Clipboard HTML saved to {0} ({1} bytes)", dlg.FileName, clipboardData.Length);
                    MessageBox.Show("Clipboard HTML saved to:\n" + dlg.FileName,
                        "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving clipboard HTML to {0}", dlg.FileName);
                    MessageBox.Show("Failed to save: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Tab Warning Helpers
        // -----------------------------------------------------------------------

        private void ApplyTabWarning(TabPage tab, TabWarningLevel level)
        {
            switch (level)
            {
                case TabWarningLevel.Red:
                    tab.UseVisualStyleBackColor = false;
                    tab.BackColor = Color.LightCoral;
                    break;
                case TabWarningLevel.Yellow:
                    tab.UseVisualStyleBackColor = false;
                    tab.BackColor = Color.Yellow;
                    break;
                default:
                    tab.UseVisualStyleBackColor = true;
                    tab.BackColor = SystemColors.Control;
                    break;
            }
            tabDetailedData.Invalidate();
        }

        private void tabDetailedData_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabDetailedData.TabPages[e.Index];
            Color backColor = page.UseVisualStyleBackColor ? SystemColors.Control : page.BackColor;

            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string title = page.Text;
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;
            TextRenderer.DrawText(e.Graphics, title, e.Font, e.Bounds, page.ForeColor, flags);
        }

        private void UpdateTabWarnings()
        {
            int structureCount = selectedColony?.Structures?.Count ?? 0;
            ApplyTabWarning(tabPStructures,
                TabWarningService.EvaluateStructureWarning(structureCount));

            var commodities = selectedColony?.Commodities;
            ApplyTabWarning(tabPWorkers,
                TabWarningService.EvaluateWorkerWarning(
                    commodities ?? Enumerable.Empty<CommodityRequested>(), DateTime.Now));
        }
    }

    /// <summary>
    /// Compares ListView items by a specified column for sorting.
    /// </summary>
    internal class ListViewItemComparer : System.Collections.IComparer
    {
        private readonly int _column;
        private readonly SortOrder _order;

        public ListViewItemComparer(int column, SortOrder order)
        {
            _column = column;
            _order = order;
        }

        public int Compare(object x, object y)
        {
            string textX = ((ListViewItem)x).SubItems[_column].Text;
            string textY = ((ListViewItem)y).SubItems[_column].Text;

            int result;
            if (int.TryParse(textX, out int numX) && int.TryParse(textY, out int numY))
                result = numX.CompareTo(numY);
            else
                result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);

            return _order == SortOrder.Descending ? -result : result;
        }
    }
}
