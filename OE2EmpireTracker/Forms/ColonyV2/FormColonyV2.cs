using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.ColonyV2
{
    public partial class FormColonyV2 : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly EmpireContext empireContext;
        private readonly PlayerContext playerContext;
        private int _isProgrammaticUpdate = 0;

        private Models.Colony selectedColony;
        private ColonyViewModel colonyViewModel;
        private ColonyReferenceCounter _referenceCounter;

        // ListView sorting state
        private int _sortColumn = 0;
        private SortOrder _sortOrder = SortOrder.Ascending;

        // Deferred tab update flags
        private bool _structuresDirty = false;
        private bool _warehouseDirty = false;
        private bool _workersDirty = false;
        private bool _adminDirty = false;

        // Structure_Pool (8.1)
        private readonly List<ColonyStructureV2> _pool = new List<ColonyStructureV2>();
        private int _poolInUse = 0;

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        public FormColonyV2()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);

            _referenceCounter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList);

            // Configure colony list
            lvwColonies.Columns.Add("Planet", 80);
            lvwColonies.Columns.Add("Name", 80);
            lvwColonies.Columns.Add("Refs", 40);
            lvwColonies.ColumnClick += lvwColonies_ColumnClick;
            lvwColonies.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(playerContext.GetCurrentPlayerColonies());

            // Wire filter handler
            txtColonyFilter.TextChanged += txtColonyFilter_TextChanged;

            // Wire colony selection handler
            lvwColonies.ItemSelectionChanged += lvwColonies_ItemSelectionChanged;

            // Wire write-through handlers
            txtPlanetName.TextChanged += txtPlanetName_TextChanged;
            txtColonyName.TextChanged += txtColonyName_TextChanged;
            txtSystemName.TextChanged += txtSystemName_TextChanged;

            // Wire tab change for deferred population
            tabDetailedData.SelectedIndexChanged += tabDetailedData_SelectedIndexChanged;

            // Wire flatpack filter and add button (8.3)
            txtFilterFlatpack.TextChanged += txtFilterFlatpack_TextChanged;
            cmdAddFlatpack.Click += cmdAddFlatpack_Click;
            PopulateFlatpackCombo();

            // Subscribe to context events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;

            UpdateTitle();
        }

        // -------------------------------------------------------------------
        // Event lifecycle
        // -------------------------------------------------------------------

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
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

            using var guard = new ProgrammaticUpdateGuard(this);
            lvwColonies.Items.Clear();
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            _referenceCounter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList);
            PopulateListView(playerContext.GetCurrentPlayerColonies());
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";
            MarkAllTabsDirty();
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
            if (_isProgrammaticUpdate > 0) return;
            if (selectedColony != null && selectedColony.UUID == e.ColonyUUID)
            {
                colonyViewModel.RecalculateStatus();
                PopulateForm();
            }
        }

        // -------------------------------------------------------------------
        // Write-through handlers
        // -------------------------------------------------------------------

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

        // -------------------------------------------------------------------
        // Colony list population
        // -------------------------------------------------------------------

        private void PopulateListView(List<Models.Colony> colonies)
        {
            if (colonies == null) return;

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList);

            var viewableColonies = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwColonies.Items)
            {
                var col = item.Tag as Models.Colony;
                if (col != null)
                    viewableColonies[col.UUID] = item;
            }

            foreach (Models.Colony colony in colonies)
            {
                string refCount = counter.CountReferences(colony.UUID).TotalCount.ToString();
                ListViewItem item;
                bool found = viewableColonies.TryGetValue(colony.UUID, out item);

                if (!found)
                {
                    item = new ListViewItem(colony.PlanetName);
                    item.SubItems.Add(colony.ColonyName);
                    item.SubItems.Add(refCount);
                }
                else
                {
                    item.SubItems[0].Text = colony.PlanetName;
                    if (item.SubItems.Count > 1)
                        item.SubItems[1].Text = colony.ColonyName;
                    else
                        item.SubItems.Add(colony.ColonyName);
                    if (item.SubItems.Count > 2)
                        item.SubItems[2].Text = refCount;
                    else
                        item.SubItems.Add(refCount);
                }

                item.Tag = colony;
                item.SubItems[0].Tag = colony;

                if (!found)
                    lvwColonies.Items.Add(item);
                else
                    viewableColonies.Remove(colony.UUID);
            }

            // Remove items no longer in the list
            foreach (var remaining in viewableColonies)
            {
                lvwColonies.Items.Remove(remaining.Value);
            }
        }

        // -------------------------------------------------------------------
        // Filter (6.5)
        // -------------------------------------------------------------------

        private void txtColonyFilter_TextChanged(object sender, EventArgs e)
        {
            string filter = txtColonyFilter.Text;
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

        // -------------------------------------------------------------------
        // Column sort (6.6)
        // -------------------------------------------------------------------

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

        // -------------------------------------------------------------------
        // Title bar (6.7)
        // -------------------------------------------------------------------

        private void UpdateTitle()
        {
            var player = playerContext.CurrentPlayer;
            string playerName = player != null ? player.Name : "No Player";
            int colonyCount = playerContext.GetCurrentPlayerColonies()?.Count ?? 0;
            Text = $"Manage Colonies - {playerName} : {colonyCount}";
        }

        // -------------------------------------------------------------------
        // Colony selection (6.8)
        // -------------------------------------------------------------------

        private void lvwColonies_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvwColonies.SelectedItems.Count == 1)
            {
                selectedColony = lvwColonies.SelectedItems[0].Tag as Models.Colony;
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                PopulateForm();
                UpdateDeleteButtonState();
                UpdateTitle();
            }
        }

        private void PopulateForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            if (selectedColony == null) return;

            txtPlanetName.Text = colonyViewModel.PlanetName;
            txtColonyName.Text = colonyViewModel.ColonyName;
            txtSystemName.Text = colonyViewModel.Data.SystemName ?? "";

            MarkAllTabsDirty();
        }

        private void MarkAllTabsDirty()
        {
            _structuresDirty = true;
            _warehouseDirty = true;
            _workersDirty = true;
            _adminDirty = true;
        }

        private void tabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tab = tabDetailedData.SelectedTab;
            if (tab == tabPStructures && _structuresDirty)
            {
                PopulateStructures();
                _structuresDirty = false;
            }
            // Future tabs: tabPWarehousing, tabPWorkers, tabPAdministration
        }

        // -------------------------------------------------------------------
        // CRUD operations (6.9)
        // -------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";
            lvwColonies.SelectedItems.Clear();
            MarkAllTabsDirty();
            UpdateDeleteButtonState();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            if (string.IsNullOrEmpty(colonyViewModel.Data.OwnerUUID))
            {
                colonyViewModel.Data.OwnerUUID = playerContext.CurrentPlayerUUID;
            }
            colonyViewModel.Save();
            // Refresh list with current filter
            txtColonyFilter_TextChanged(sender, e);
            UpdateTitle();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID)) return;

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList);
            var report = counter.CountReferences(selectedColony.UUID);
            if (report.TotalCount > 0)
            {
                var msg = $"Cannot delete '{selectedColony.PlanetName}' -- it is referenced by {report.RouteCount} route(s) and {report.PlanCount} plan(s).";
                MessageBox.Show(msg, "Colony In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete colony '{selectedColony.PlanetName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.ColonyList.Remove(selectedColony);
            playerContext.WriteContext();

            using var guard = new ProgrammaticUpdateGuard(this);
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            txtPlanetName.Text = "";
            txtColonyName.Text = "";
            txtSystemName.Text = "";
            MarkAllTabsDirty();

            // Refresh list
            txtColonyFilter_TextChanged(sender, e);
            UpdateTitle();
            UpdateDeleteButtonState();
        }

        private void UpdateDeleteButtonState()
        {
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID))
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = "Delete";
                return;
            }

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList);
            var report = counter.CountReferences(selectedColony.UUID);
            if (report.TotalCount > 0)
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = $"In Use ({report.TotalCount})";
            }
            else
            {
                cmdDelete.Enabled = true;
                cmdDelete.Text = "Delete";
            }
        }

        // -------------------------------------------------------------------
        // Structure_Pool (8.1)
        // -------------------------------------------------------------------

        private ColonyStructureV2 AcquireStructureControl()
        {
            if (_poolInUse < _pool.Count)
            {
                var ctrl = _pool[_poolInUse];
                _poolInUse++;
                ctrl.Visible = true;
                return ctrl;
            }
            var newCtrl = new ColonyStructureV2();
            newCtrl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
            _pool.Add(newCtrl);
            _poolInUse++;
            return newCtrl;
        }

        private void ReturnAllToPool()
        {
            for (int i = 0; i < _poolInUse; i++)
            {
                _pool[i].Visible = false;
                _pool[i].Reset();
            }
            _poolInUse = 0;
        }

        // -------------------------------------------------------------------
        // Structure panel population (8.2)
        // -------------------------------------------------------------------

        private void PopulateStructures()
        {
            if (selectedColony == null) return;

            using var guard = new ProgrammaticUpdateGuard(this);

            flpStructures.SuspendLayout();
            ReturnAllToPool();
            flpStructures.Controls.Clear();

            var structureVMs = colonyViewModel.StructureViewModels;
            foreach (var vm in structureVMs)
            {
                var ctrl = AcquireStructureControl();
                ctrl.ViewModel = vm;
                ctrl.Colony = selectedColony;
                var bp = playerContext.FindBlueprint(vm.Data.FlatpackBlueprintUUID);
                ctrl.UpdateData(bp);
                flpStructures.Controls.Add(ctrl);
            }

            flpStructures.ResumeLayout();

            RefreshStatusSummary();
        }

        // -------------------------------------------------------------------
        // Flatpack filter + combo + add (8.3)
        // -------------------------------------------------------------------

        private void PopulateFlatpackCombo()
        {
            string searchText = txtFilterFlatpack.Text;
            var filteredList = new List<Models.Blueprint>(playerContext.GetAllBlueprints());

            filteredList = filteredList
                .Where(item => item.BluePrintType != null && item.BluePrintType.IsFlatpack())
                .ToList();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName != null &&
                                   item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            filteredList = filteredList.OrderBy(p => p.ExtendedName, StringComparer.OrdinalIgnoreCase).ToList();
            filteredList.Insert(0, new Models.Blueprint());

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbFlatpacks.DataSource = null;
            cmbFlatpacks.DisplayMember = "ExtendedName";
            cmbFlatpacks.ValueMember = "UUID";
            cmbFlatpacks.DataSource = bs;
        }

        private void txtFilterFlatpack_TextChanged(object sender, EventArgs e)
        {
            PopulateFlatpackCombo();
            cmbFlatpacks.DroppedDown = true;
        }

        private void cmdAddFlatpack_Click(object sender, EventArgs e)
        {
            if (cmbFlatpacks.SelectedValue == null || string.IsNullOrEmpty(cmbFlatpacks.SelectedValue.ToString()))
                return;

            string uuid = cmbFlatpacks.SelectedValue.ToString();
            colonyViewModel.AddStructure(uuid);
            colonyViewModel.RecalculateStatus();
            PopulateStructures();

            // Save context
            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        // -------------------------------------------------------------------
        // Structure change handlers (8.4, 8.5)
        // -------------------------------------------------------------------

        private void structures_ColonyStructureDataChanged(object sender, ColonyStructureDataChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (e.IsStructural)
            {
                // 8.4: Structural change — full rebuild
                colonyViewModel.InvalidateStructureViewModels();
                colonyViewModel.RecalculateStatus();
                PopulateStructures();
            }
            else
            {
                // 8.5: Non-structural change — O(1) delta update
                var ctrl = sender as ColonyStructureV2;
                if (ctrl?.ViewModel != null)
                {
                    colonyViewModel.Calculator.RecalculateStructure(ctrl.ViewModel.Data);
                    ctrl.UpdateBackgroundColor();
                }
            }

            RefreshStatusSummary();

            // Save context
            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        // -------------------------------------------------------------------
        // Status summary (8.6)
        // -------------------------------------------------------------------

        private void RefreshStatusSummary()
        {
            var status = colonyViewModel.Calculator.finalActualStatus;
            if (status == null)
            {
                rtbStatusSummary.Text = "";
                return;
            }

            var builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, status);
            rtbStatusSummary.Rtf = builder.ToRtf();
        }

        // -------------------------------------------------------------------
        // Structures tab layout handler
        // -------------------------------------------------------------------

        private void tabPStructures_Layout(object sender, LayoutEventArgs e)
        {
            int w = tabPStructures.ClientSize.Width - tabPStructures.Padding.Horizontal;
            flpStructures.Width = w;

            // Size each pooled control to match the panel width
            for (int i = 0; i < _poolInUse; i++)
            {
                _pool[i].Width = w - SystemInformation.VerticalScrollBarWidth - 6;
            }
        }

        // -------------------------------------------------------------------
        // Layout handler
        // -------------------------------------------------------------------

        private void flpColonyData_Layout(object sender, LayoutEventArgs e)
        {
            int totalHeight = flpColonyData.ClientSize.Height;
            int totalWidth = flpColonyData.ClientSize.Width;

            int identityHeight = flpIdentity.Height + flpIdentity.Margin.Vertical;
            int commandsHeight = flpCommands.Height + flpCommands.Margin.Vertical;
            int tabHeight = totalHeight - identityHeight - commandsHeight - tabDetailedData.Margin.Vertical;
            if (tabHeight < 50) tabHeight = 50;

            tabDetailedData.Size = new System.Drawing.Size(
                totalWidth - tabDetailedData.Margin.Horizontal,
                tabHeight);
        }
    }
}
