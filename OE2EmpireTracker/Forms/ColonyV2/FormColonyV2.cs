using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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

        // Structure type filter (9.1)
        private readonly HashSet<string> _uncheckedStructureTypes = new HashSet<string>(StringComparer.Ordinal);
        private bool _structureTypesPopulated = false;

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // WM_SETREDRAW: suppress all painting until re-enabled
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 0x000B;

        private static void SuspendDrawing(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, false, 0);
        }

        private static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, WM_SETREDRAW, true, 0);
            control.Refresh();
        }

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

            // Structure type filter (9.1, 9.3)
            SeedUncheckedStructureTypes();

            // Wire commodity request handlers (19.1-19.8)
            txtCommodityRequestFilter.TextChanged += txtCommodityRequestFilter_TextChanged;
            cmdAddCommodityRequest.Click += cmdAddCommodityRequest_Click;
            dgvCommodityRequests.CurrentCellDirtyStateChanged += dgvCommodityRequests_CurrentCellDirtyStateChanged;
            dgvCommodityRequests.CellValueChanged += dgvCommodityRequests_CellValueChanged;
            dgvCommodityRequests.CellValidating += dgvCommodityRequests_CellValidating;
            dgvCommodityRequests.SelectionChanged += dgvCommodityRequests_SelectionChanged;
            dgvCommodityRequests.KeyDown += dgvCommodityRequests_KeyDown;
            UpdateCommodityRequestList();

            // Enable owner-draw so tab BackColor renders with visual styles (11.4)
            tabDetailedData.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabDetailedData.DrawItem += tabDetailedData_DrawItem;

            // Wire warehouse handlers (20.1-20.6)
            cmbItemType.DataSource = Models.ItemType.ItemTypes;
            cmbItemType.DisplayMember = "Name";
            cmbItemType.SelectedIndexChanged += cmbItemType_SelectedIndexChanged;
            txtItemFilter.TextChanged += txtItemFilter_TextChanged;
            cmbItem.SelectedIndexChanged += cmbItem_SelectedIndexChanged;
            cmbPurity.DataSource = Models.ResourcePurity.Purities;
            cmbPurity.DisplayMember = "Name";
            cmdAddItem.Click += cmdAddItem_Click;
            dgvItems.CellValidating += dgvItems_CellValidating;
            dgvItems.CellValueChanged += dgvItems_CellValueChanged;
            dgvItems.SelectionChanged += dgvItems_SelectionChanged;
            dgvItems.KeyDown += dgvItems_KeyDown;

            // Wire admin refresh timer (11.1)
            timerAdminRefresh.Tick += timerAdminRefresh_Tick;
            timerAdminRefresh.Start();

            // Wire import handlers (21.1, 21.5)
            cmdImportColony.Click += cmdImportColony_Click;
            cmdImportClipboard.Click += cmdImportClipboard_Click;

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
            // Save window state including structure type filter (9.3)
            int windowNumber = Tag is int n ? n : 1;
            WindowStateHelper.SaveState(this, GetType().Name, windowNumber);

            timerAdminRefresh.Stop();
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

            Log.Debug("V2.OnCurrentPlayerChanged: player={0}", playerContext.CurrentPlayer?.Name ?? "(none)");

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
            UpdateTabWarnings();
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
            Log.Debug("V2.OnColonyDataChanged: colonyUUID={0}", e.ColonyUUID);
            if (selectedColony != null && selectedColony.UUID == e.ColonyUUID)
            {
                colonyViewModel.RecalculateStatus();
                PopulateForm();
                RefreshAdminReport();
                UpdateTabWarnings();
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
                var sw = System.Diagnostics.Stopwatch.StartNew();
                selectedColony = lvwColonies.SelectedItems[0].Tag as Models.Colony;
                Log.Debug("V2.lvwColonies_ItemSelectionChanged: colony={0} uuid={1}",
                    selectedColony?.ColonyName ?? selectedColony?.PlanetName ?? "(null)",
                    selectedColony?.UUID ?? "(null)");
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                long tViewModel = sw.ElapsedMilliseconds;
                colonyViewModel.RecalculateStatus();
                long tRecalc = sw.ElapsedMilliseconds;
                PopulateForm();
                long tPopulate = sw.ElapsedMilliseconds;
                UpdateDeleteButtonState();
                long tDelete = sw.ElapsedMilliseconds;
                RefreshAdminReport();
                long tAdmin = sw.ElapsedMilliseconds;
                UpdateTabWarnings();
                long tWarnings = sw.ElapsedMilliseconds;
                UpdateTitle();
                sw.Stop();
                Log.Info("V2.ColonySelection PERF: total={0}ms viewModel={1}ms recalcStatus={2}ms populateForm={3}ms deleteBtn={4}ms adminReport={5}ms tabWarnings={6}ms",
                    sw.ElapsedMilliseconds, tViewModel, tRecalc - tViewModel, tPopulate - tRecalc, tDelete - tPopulate, tAdmin - tDelete, tWarnings - tAdmin);
            }
        }

        private void PopulateForm()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            if (selectedColony == null) return;

            Log.Debug("V2.PopulateForm: colony={0}", selectedColony.ColonyName ?? selectedColony.PlanetName ?? "(null)");

            txtPlanetName.Text = colonyViewModel.PlanetName;
            txtColonyName.Text = colonyViewModel.ColonyName;
            txtSystemName.Text = colonyViewModel.Data.SystemName ?? "";
            long tIdentity = sw.ElapsedMilliseconds;

            MarkAllTabsDirty();

            // Immediately populate the currently visible tab
            PopulateActiveTab();
            long tActiveTab = sw.ElapsedMilliseconds;

            sw.Stop();
            Log.Info("V2.PopulateForm PERF: total={0}ms identity={1}ms activeTab={2}ms",
                sw.ElapsedMilliseconds, tIdentity, tActiveTab - tIdentity);
        }

        private void MarkAllTabsDirty()
        {
            _structuresDirty = true;
            _warehouseDirty = true;
            _workersDirty = true;
            _adminDirty = true;
        }

        /// <summary>
        /// Populates whichever tab is currently selected, if it's dirty.
        /// Called after MarkAllTabsDirty to handle the case where the user
        /// is already on a tab and switches colonies.
        /// </summary>
        private void PopulateActiveTab()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string tabName = "none";
            var tab = tabDetailedData.SelectedTab;
            if (tab == tabPStructures && _structuresDirty)
            {
                tabName = "Structures";
                PopulateStructures();
                _structuresDirty = false;
            }
            else if (tab == tabPAdministration && _adminDirty)
            {
                tabName = "Administration";
                RefreshAdminReport();
                _adminDirty = false;
            }
            else if (tab == tabPWorkers && _workersDirty)
            {
                tabName = "Workers";
                PopulateCommodityRequestGrid();
                _workersDirty = false;
            }
            else if (tab == tabPWarehousing && _warehouseDirty)
            {
                tabName = "Warehousing";
                PopulateItemGrid();
                _warehouseDirty = false;
            }
            sw.Stop();
            Log.Info("V2.PopulateActiveTab PERF: tab={0} elapsed={1}ms", tabName, sw.ElapsedMilliseconds);
        }

        private void tabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
        {
            var tab = tabDetailedData.SelectedTab;
            if (tab == tabPStructures && _structuresDirty)
            {
                PopulateStructures();
                _structuresDirty = false;
            }
            else if (tab == tabPAdministration && _adminDirty)
            {
                RefreshAdminReport();
                _adminDirty = false;
            }
            else if (tab == tabPWorkers && _workersDirty)
            {
                PopulateCommodityRequestGrid();
                _workersDirty = false;
            }
            else if (tab == tabPWarehousing && _warehouseDirty)
            {
                PopulateItemGrid();
                _warehouseDirty = false;
            }
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
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID))
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = "Delete";
                sw.Stop();
                Log.Info("V2.UpdateDeleteButtonState PERF: total={0}ms (no colony)", sw.ElapsedMilliseconds);
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

            sw.Stop();
            Log.Info("V2.UpdateDeleteButtonState PERF: total={0}ms", sw.ElapsedMilliseconds);
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

            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var guard = new ProgrammaticUpdateGuard(this);

            Log.Debug("V2.PopulateStructures: count={0}", selectedColony.Structures?.Count ?? 0);

            // Ensure structure type filter list is populated (9.1)
            PopulateStructureTypeFilter();

            // Suppress all painting until we're done updating controls
            SuspendDrawing(flpStructures);
            long tSuspend = sw.ElapsedMilliseconds;

            // Build set of checked types for filter (9.2)
            var checkedTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ListViewItem item in lvwStructureTypes.Items)
            {
                if (item.Checked)
                    checkedTypes.Add((string)item.Tag);
            }

            flpStructures.SuspendLayout();

            var structureVMs = colonyViewModel.StructureViewModels;
            int needed = structureVMs.Count;

            long t0 = sw.ElapsedMilliseconds;

            // Grow pool if needed (without removing/re-adding to Controls)
            while (_pool.Count < needed)
            {
                var newCtrl = new ColonyStructureV2();
                newCtrl.ColonyStructureDataChanged += structures_ColonyStructureDataChanged;
                newCtrl.Visible = false;
                _pool.Add(newCtrl);
                flpStructures.Controls.Add(newCtrl);
            }

            // Ensure all needed controls are in the FlowLayoutPanel
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!flpStructures.Controls.Contains(_pool[i]))
                    flpStructures.Controls.Add(_pool[i]);
            }

            long t1 = sw.ElapsedMilliseconds;

            // Assign data to active controls, hide extras
            long updateDataTotal = 0;
            long resetTotal = 0;
            for (int i = 0; i < _pool.Count; i++)
            {
                var ctrl = _pool[i];
                if (i < needed)
                {
                    var vm = structureVMs[i];
                    var bp = playerContext.FindBlueprint(vm.Data.FlatpackBlueprintUUID);
                    string typeId = bp?.BluePrintType ?? "";

                    ctrl.ViewModel = vm;
                    ctrl.Colony = selectedColony;
                    var udSw = System.Diagnostics.Stopwatch.StartNew();
                    ctrl.UpdateDataFast(bp);
                    udSw.Stop();
                    updateDataTotal += udSw.ElapsedMilliseconds;
                    ctrl.Visible = checkedTypes.Count == 0 || checkedTypes.Contains(typeId);
                }
                else
                {
                    if (ctrl.Visible || ctrl.ViewModel != null)
                    {
                        var rSw = System.Diagnostics.Stopwatch.StartNew();
                        ctrl.Visible = false;
                        ctrl.Reset();
                        rSw.Stop();
                        resetTotal += rSw.ElapsedMilliseconds;
                    }
                }
            }
            _poolInUse = needed;

            long t2 = sw.ElapsedMilliseconds;

            flpStructures.ResumeLayout();
            ResumeDrawing(flpStructures);
            long tResume = sw.ElapsedMilliseconds;

            long tLayoutOverhead = tResume - t2;

            RefreshStatusSummary();
            long tStatusSummary = sw.ElapsedMilliseconds;

            sw.Stop();
            Log.Info("V2.PopulateStructures PERF: total={0}ms suspend={1}ms pool={2}ms updateData={3}ms(x{4}) reset={5}ms layout={6}ms statusSummary={7}ms",
                sw.ElapsedMilliseconds, tSuspend, t1 - t0, updateDataTotal, needed, resetTotal, tLayoutOverhead, tStatusSummary - tResume);
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
            Log.Debug("V2.cmdAddFlatpack_Click: blueprintUUID={0}", uuid);
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

            var ctrl = sender as ColonyStructureV2;
            Log.Debug("V2.structures_ColonyStructureDataChanged: structural={0} sender={1}",
                e.IsStructural, ctrl?.ViewModel?.Data?.UUID ?? "(unknown)");

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
                if (ctrl?.ViewModel != null)
                {
                    colonyViewModel.Calculator.RecalculateStructure(ctrl.ViewModel.Data);
                    ctrl.UpdateBackgroundColor();
                }
            }

            RefreshStatusSummary();
            UpdateTabWarnings();

            // Save context
            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        // -------------------------------------------------------------------
        // Status summary (8.6)
        // -------------------------------------------------------------------

        private void RefreshStatusSummary()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var status = colonyViewModel.Calculator.finalActualStatus;
            if (status == null)
            {
                rtbStatusSummary.Text = "";
                sw.Stop();
                Log.Info("V2.RefreshStatusSummary PERF: total={0}ms (no status)", sw.ElapsedMilliseconds);
                return;
            }

            var builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, status);
            rtbStatusSummary.Rtf = builder.ToRtf();
            sw.Stop();
            Log.Info("V2.RefreshStatusSummary PERF: total={0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Structures tab layout handler
        // -------------------------------------------------------------------

        private void tabPStructures_Layout(object sender, LayoutEventArgs e)
        {
            // Size pooled controls to match the structure panel width (inside splitStructures.Panel2)
            int w = flpStructures.ClientSize.Width;
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

        // -------------------------------------------------------------------
        // Administration tab — Admin Report (11.1)
        // -------------------------------------------------------------------

        private void RefreshAdminReport()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID))
            {
                rtbAdminReport.Rtf = "";
                sw.Stop();
                Log.Info("V2.RefreshAdminReport PERF: total={0}ms (no colony)", sw.ElapsedMilliseconds);
                return;
            }

            try
            {
                string rtf = ColonyAdminReportBuilder.BuildReport(selectedColony, playerContext);
                rtbAdminReport.Rtf = string.IsNullOrEmpty(rtf) ? "" : rtf;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building admin report");
            }

            sw.Stop();
            Log.Info("V2.RefreshAdminReport PERF: total={0}ms", sw.ElapsedMilliseconds);
        }

        private void timerAdminRefresh_Tick(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            int intervalMs = (int)(PreferencesStore.GetInstance().Preferences.Thresholds.AdminRefreshIntervalSeconds * 1000);
            timerAdminRefresh.Interval = Math.Max(intervalMs, 1000);
            RefreshAdminReport();
            UpdateTabWarnings();
        }

        // -------------------------------------------------------------------
        // Administration tab — Bootstrap / Optimize (11.2, 11.3)
        // -------------------------------------------------------------------

        private void cmdBootstrap_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;
            if (string.IsNullOrEmpty(selectedColony.PlanetName))
            {
                MessageBox.Show(
                    "Set a planet name before bootstrapping.",
                    "No Planet",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var bootstrap = new ColonyBootstrap(playerContext);
            bootstrap.Bootstrap(selectedColony);

            // Refresh via structural change pattern
            colonyViewModel.InvalidateStructureViewModels();
            colonyViewModel.RecalculateStatus();
            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();

            if (!string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        private void cmdOptimize_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;

            var optimizer = new BuildOrderOptimizer(playerContext);
            var optimized = optimizer.Optimize(selectedColony);

            selectedColony.Structures.Clear();
            selectedColony.Structures.AddRange(optimized);

            // Refresh via structural change pattern
            colonyViewModel.InvalidateStructureViewModels();
            colonyViewModel.RecalculateStatus();
            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();

            if (!string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        // -------------------------------------------------------------------
        // Tab Warning Indicators (11.4)
        // -------------------------------------------------------------------

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
            var sw = System.Diagnostics.Stopwatch.StartNew();

            int structureCount = selectedColony?.Structures?.Count ?? 0;
            ApplyTabWarning(tabPStructures,
                TabWarningService.EvaluateStructureWarning(structureCount));

            ApplyTabWarning(tabPWorkers,
                TabWarningService.EvaluateWorkerWarning(
                    selectedColony?.Commodities, DateTime.UtcNow));

            ApplyTabWarning(tabPAdministration,
                TabWarningService.EvaluateColonyImportStalenessWarning(
                    selectedColony?.LastImportDateTime, DateTime.UtcNow));

            UpdateWorkerTabTitle();
            UpdateStructuresTabTitle();

            sw.Stop();
            Log.Info("V2.UpdateTabWarnings PERF: total={0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Workers Tab — Commodity Requests (19.1-19.8)
        // -------------------------------------------------------------------

        private void cmdAddCommodityRequest_Click(object sender, EventArgs e)
        {
            Models.Commodity commodity = cmbCommodityRequest.SelectedItem as Models.Commodity;
            if (commodity == null || string.IsNullOrEmpty(commodity.ID)) return;

            int qty;
            int.TryParse(txtCommodityRequestQty.Text, out qty);

            DateTime? needBy = null;
            string needByText = txtCommodityRequestNeedBy.Text?.Trim();
            if (!string.IsNullOrEmpty(needByText))
            {
                needBy = ParseCountdownToDateTime(needByText);
            }

            colonyViewModel.AddCommodityRequest(commodity.Name, qty, needBy);
            PopulateCommodityRequestGrid();
            UpdateTabWarnings();

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
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
            }
            filteredList.Insert(0, new Models.Commodity());

            var bindingList = new BindingSource();
            bindingList.DataSource = filteredList;

            cmbCommodityRequest.DataSource = null;
            cmbCommodityRequest.DisplayMember = "ExtendedName";
            cmbCommodityRequest.ValueMember = "Name";
            cmbCommodityRequest.DataSource = bindingList;
        }

        private void PopulateCommodityRequestGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCommodityRequests.CellValidating -= dgvCommodityRequests_CellValidating;
            try { dgvCommodityRequests.EndEdit(); } catch { }

            // Auto-cleanup expired fulfilled requests (19.6)
            int cleaned = colonyViewModel.CleanupExpiredCommodityRequests();
            if (cleaned > 0)
            {
                Log.Debug("Cleaned up {0} expired commodity requests", cleaned);
                if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                    playerContext.WriteContext();
            }

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

                // Strikethrough styling for fulfilled requests (19.4)
                if (request.Fulfilled)
                {
                    row.DefaultCellStyle.Font = new Font(dgvCommodityRequests.Font, FontStyle.Strikeout);
                    row.DefaultCellStyle.ForeColor = Color.Gray;
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

            UpdateWorkerTabTitle();
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

            // Column 1 = Amount (Requested) (19.3)
            if (e.ColumnIndex == 1)
            {
                int value;
                if (int.TryParse(row.Cells[1].Value?.ToString(), out value))
                    request.Requested = value;
                playerContext.WriteContext();
            }
            // Column 2 = Fulfilled (checkbox) (19.4)
            else if (e.ColumnIndex == 2)
            {
                bool fulfilled = row.Cells[2].Value is bool b && b;
                request.Fulfilled = fulfilled;
                if (fulfilled)
                    request.Delivered = request.Requested;
                else
                    request.Delivered = 0;
                playerContext.WriteContext();
                // Refresh to update strikethrough
                PopulateCommodityRequestGrid();
            }
            // Column 3 = NeedBy (countdown format) (19.3)
            else if (e.ColumnIndex == 3)
            {
                string text = row.Cells[3].Value?.ToString() ?? "";
                DateTime? parsed = ParseCountdownToDateTime(text);
                if (parsed.HasValue)
                {
                    request.NeedBy = parsed.Value;
                    playerContext.WriteContext();
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
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                    return;
                }
                if (!int.TryParse(value, out _))
                {
                    e.Cancel = true;
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
                }
                else
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                }
            }
            // Column 3 = NeedBy (countdown format)
            else if (e.ColumnIndex == 3)
            {
                string value = e.FormattedValue?.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                    return;
                }
                if (ParseCountdownToDateTime(value) == null)
                {
                    e.Cancel = true;
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "Use countdown format: e.g. 2d 6h 30m";
                }
                else
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = "";
                }
            }
        }

        private void dgvCommodityRequests_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCommodityRequests.CurrentCell == null) return;
            if (dgvCommodityRequests.SelectedRows.Count > 0) return;

            // Redirect Name column (0) clicks to Amount (1) (19.1)
            int col = dgvCommodityRequests.CurrentCell.ColumnIndex;
            if (col == 0)
            {
                using var guard = new ProgrammaticUpdateGuard(this);
                dgvCommodityRequests.CurrentCell = dgvCommodityRequests.Rows[dgvCommodityRequests.CurrentCell.RowIndex].Cells[1];
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

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();

            e.Handled = true;
        }

        // -------------------------------------------------------------------
        // Workers Tab — Tab Title and Countdown Helpers (19.7)
        // -------------------------------------------------------------------

        private void UpdateWorkerTabTitle()
        {
            if (selectedColony == null)
            {
                tabPWorkers.Text = "Workers";
                return;
            }

            var now = DateTime.UtcNow;
            int activeCount = selectedColony.Commodities
                .Count(r => !r.Fulfilled && (r.NeedBy == DateTime.MinValue || r.NeedBy > now));
            tabPWorkers.Text = activeCount > 0 ? $"Workers : {activeCount}" : "Workers";
        }

        private void UpdateStructuresTabTitle()
        {
            int structureCount = selectedColony?.Structures?.Count ?? 0;
            tabPStructures.Text = structureCount > 0 ? $"Structures : {structureCount}" : "Structures";
        }

        /// <summary>
        /// Parses a countdown string (e.g. "2d 6h 30m") into a DateTime (now + duration).
        /// Returns null if the format is invalid.
        /// </summary>
        private DateTime? ParseCountdownToDateTime(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var match = Regex.Match(text.Trim(),
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
            return DateTime.UtcNow.AddSeconds(totalSeconds);
        }

        /// <summary>
        /// Formats a NeedBy DateTime as a countdown string relative to now.
        /// Returns empty string for DateTime.MinValue. Shows "overdue" for past-due (19.7).
        /// </summary>
        private string FormatNeedByCountdown(DateTime needBy)
        {
            if (needBy == DateTime.MinValue) return "";
            var remaining = needBy - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0)
                return "overdue";

            string result = "";
            if (remaining.Days > 0) result += $"{remaining.Days}d ";
            if (remaining.Hours > 0 || remaining.Days > 0) result += $"{remaining.Hours}h ";
            if (remaining.Minutes > 0 || remaining.Hours > 0 || remaining.Days > 0) result += $"{remaining.Minutes}m";
            else result += $"{remaining.Seconds}s";
            return result.Trim();
        }

        // -------------------------------------------------------------------
        // Warehousing Tab — Item Grid and Add Controls (20.1-20.6)
        // -------------------------------------------------------------------

        private void PopulateItemGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvItems.CellValidating -= dgvItems_CellValidating;
            try { dgvItems.EndEdit(); } catch { }

            // Index existing rows by item UUID for in-place update (20.6)
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
        /// Refreshes only the Locked column (index 2) without rebuilding the grid.
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

        // -------------------------------------------------------------------
        // Warehousing — Item Type Combo (20.2)
        // -------------------------------------------------------------------

        private void cmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            cmbPurity.Visible = false;
            if (itemType == null) return;

            if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
            {
                PopulateItemWithResources();
                cmbPurity.Visible = true;
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
            {
                PopulateItemWithCommodities();
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
            {
                PopulateItemWithWorkerDetails();
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
            {
                PopulateItemWithSurveys();
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
            {
                PopulateItemWithBlueprints();
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.ShipHull ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Munition ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Flatpack ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Share)
            {
                PopulateItemWithBlueprintsByOutputType(itemType.ID);
            }
        }

        private void cmbItem_SelectedIndexChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null && itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
            {
                Resource resource = cmbItem.SelectedItem as Resource;
                if (resource != null && !string.IsNullOrEmpty(resource.Name))
                {
                    // Hide purity for synthetic resources
                    if (ResourceGroup.ResourceGroupMapByEnum[resource.ResourceGroup].Synthetic)
                        cmbPurity.Visible = false;
                    else
                        cmbPurity.Visible = true;
                }
            }
        }

        private void txtItemFilter_TextChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            if (itemType == null) return;

            if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
                PopulateItemWithResources();
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
                PopulateItemWithCommodities();
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
                PopulateItemWithWorkerDetails();
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
                PopulateItemWithSurveys();
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
                PopulateItemWithBlueprints();
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.ShipHull ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Munition ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Flatpack ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.SpaceBuildPackage ||
                     itemType.ID == Models.ItemType.ItemTypeEnum.Share)
                PopulateItemWithBlueprintsByOutputType(itemType.ID);

            cmbItem.DroppedDown = true;
        }

        private void PopulateItemWithResources()
        {
            string searchText = txtItemFilter.Text;
            List<Resource> filteredList = new List<Resource>(Models.Resource.Resources);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.Name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(p => p.Name)
                    .ToList();
                filteredList.Insert(0, new Resource());
            }

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "Name";
            cmbItem.ValueMember = "Name";
            cmbItem.DataSource = bs;
        }

        private void PopulateItemWithCommodities()
        {
            string searchText = txtItemFilter.Text;
            List<Commodity> filteredList = new List<Commodity>(Models.Commodity.Commodities);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(item => item.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(p => p.ExtendedName)
                    .ToList();
                filteredList.Insert(0, new Commodity());
            }

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "ExtendedName";
            cmbItem.ValueMember = "Name";
            cmbItem.DataSource = bs;
        }

        private void PopulateItemWithWorkerDetails()
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

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "Name";
            cmbItem.ValueMember = "ID";
            cmbItem.DataSource = bs;
        }

        private void PopulateItemWithSurveys()
        {
            string searchText = txtItemFilter.Text;
            List<Models.Survey> filteredList = new List<Models.Survey>(playerContext.SurveyList);
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(s => s.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0
                             || s.PlanetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(s => s.PlanetName)
                    .ToList();
            }
            filteredList.Insert(0, new Models.Survey());

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "ExtendedName";
            cmbItem.ValueMember = "UUID";
            cmbItem.DataSource = bs;
        }

        private void PopulateItemWithBlueprints()
        {
            string searchText = txtItemFilter.Text;
            List<Models.Blueprint> filteredList = new List<Models.Blueprint>(playerContext.GetAllBlueprints());
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredList = filteredList
                    .Where(b => b.ExtendedName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            filteredList.Sort((x, y) => x.ExtendedName.CompareTo(y.ExtendedName));
            filteredList.Insert(0, new Models.Blueprint());

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "ExtendedName";
            cmbItem.ValueMember = "UUID";
            cmbItem.DataSource = bs;
        }

        private void PopulateItemWithBlueprintsByOutputType(Models.ItemType.ItemTypeEnum outputType)
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

            var bs = new BindingSource();
            bs.DataSource = filteredList;

            cmbItem.DataSource = null;
            cmbItem.DisplayMember = "ExtendedName";
            cmbItem.ValueMember = "UUID";
            cmbItem.DataSource = bs;
        }

        // -------------------------------------------------------------------
        // Warehousing — Add Item (20.3)
        // -------------------------------------------------------------------

        private void cmdAddItem_Click(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            if (itemType == null || itemType.ID == Models.ItemType.ItemTypeEnum.None) return;

            Models.Item item = new Models.Item() { UUID = Guid.NewGuid().ToString() };
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
                    item.ResourcePurity = purity.Name;
                else
                    item.ResourcePurity = Models.ResourcePurity.ItemTypeMapByEnum[Models.ResourcePurity.PurityEnum.Refined].Name;
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Commodity)
            {
                Models.Commodity commodity = cmbItem.SelectedItem as Models.Commodity;
                if (commodity != null)
                {
                    item.BaseItemTypeID = commodity.Name;
                    item.Name = commodity.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
            {
                Models.WorkerDetail workerDetail = cmbItem.SelectedItem as Models.WorkerDetail;
                if (workerDetail != null)
                {
                    item.BaseItemTypeID = workerDetail.ID;
                    item.Name = workerDetail.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
            {
                Models.Survey survey = cmbItem.SelectedItem as Models.Survey;
                if (survey != null)
                {
                    item.BaseItemTypeID = survey.UUID;
                    item.Name = survey.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
            {
                Models.Blueprint blueprint = cmbItem.SelectedItem as Models.Blueprint;
                if (blueprint != null)
                {
                    item.BaseItemTypeID = blueprint.UUID;
                    item.Name = blueprint.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.ShipPart ||
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
            if (!string.IsNullOrEmpty(quantityStr))
            {
                int quantity = 0;
                int.TryParse(quantityStr, out quantity);
                item.Quantity = quantity;
            }

            item.Volume = GetItemVolume(item, playerContext);

            colonyViewModel.AddItem(item);
            PopulateItemGrid();

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();
        }

        private static decimal GetItemVolume(Models.Item item, PlayerContext playerContext)
        {
            switch (item.ItemType)
            {
                case Models.ItemType.ItemTypeEnum.Resource:
                    return 1.0m;
                case Models.ItemType.ItemTypeEnum.Commodity:
                    return 10.0m;
                case Models.ItemType.ItemTypeEnum.WorkDetail:
                    return 50.0m;
                case Models.ItemType.ItemTypeEnum.Blueprint:
                case Models.ItemType.ItemTypeEnum.Survey:
                    return 0.0m;
                default:
                    // Manufactured items: read CargoVolumeSize from blueprint
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID) && playerContext != null)
                    {
                        Models.Blueprint bp = playerContext.FindBlueprint(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            decimal vol = 0;
                            bp.Properties.getDecimal("Cargo Volume Size", 0, out vol);
                            return vol;
                        }
                    }
                    return 0.0m;
            }
        }

        // -------------------------------------------------------------------
        // Warehousing — Delete Item (20.4)
        // -------------------------------------------------------------------

        private void dgvItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (dgvItems.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvItems.SelectedRows)
            {
                Item item = row.Tag as Item;
                if (item != null)
                {
                    int locked = colonyViewModel.Data.Locks != null
                        ? colonyViewModel.Data.Locks.GetLockedQuantity(item.ItemType, item.BaseItemTypeID)
                        : 0;
                    if (locked > 0)
                    {
                        MessageBox.Show(
                            $"Cannot delete '{item.ExtendedName}' -- {locked} locked by structures.",
                            "Item Locked",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        continue;
                    }
                    colonyViewModel.RemoveItem(item.UUID);
                }
            }
            PopulateItemGrid();

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                playerContext.WriteContext();

            e.Handled = true;
        }

        // -------------------------------------------------------------------
        // Warehousing — Editable Amount Column (20.5)
        // -------------------------------------------------------------------

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

                colonyViewModel.RecalculateStatus();
                RefreshStatusSummary();
                _structuresDirty = true;

                if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                    playerContext.WriteContext();
            }
        }

        private void dgvItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // Only validate the Amount column (index 3)
            if (e.ColumnIndex != 3) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvItems.Rows[e.RowIndex].ErrorText = "";
                return;
            }

            if (!int.TryParse(value, out _))
            {
                e.Cancel = true;
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.LightCoral;
                dgvItems.Rows[e.RowIndex].ErrorText = "Amount must be an integer";
            }
            else
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvItems.Rows[e.RowIndex].ErrorText = "";
            }
        }

        private void dgvItems_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvItems.CurrentCell == null) return;
            if (dgvItems.SelectedRows.Count > 0) return;

            // Redirect non-Amount column clicks to Amount (index 3)
            if (dgvItems.Columns[dgvItems.CurrentCell.ColumnIndex].Name != "colItemAmount")
            {
                using var guard = new ProgrammaticUpdateGuard(this);
                dgvItems.CurrentCell = dgvItems.Rows[dgvItems.CurrentCell.RowIndex].Cells[3];
            }
        }

        // -------------------------------------------------------------------
        // Colony Import (21.1-21.5)
        // -------------------------------------------------------------------

        private void cmdImportColony_Click(object sender, EventArgs e)
        {
            Log.Debug("V2.cmdImportColony_Click: starting import");
            if (!Clipboard.ContainsText(TextDataFormat.Html))
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
                // Validate clipboard contains colony data
                string clipboardData = Clipboard.GetText(TextDataFormat.Html);
                string htmlFragment = ClipboardHelper.ExtractHtmlFragment(clipboardData);
                var detected = ClipboardContentDetector.Detect(htmlFragment);
                if (detected != ClipboardContentDetector.ContentType.Colony)
                {
                    string found = ClipboardContentDetector.GetDescription(detected);
                    MessageBox.Show($"The clipboard contains {found}, not colony data.\n\nCopy the colony page from the game browser first.",
                        "Wrong Content", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var parser = new ColonyParser();
                var tempColony = parser.ParseClipboardToTemp(empireContext, out string extractedHtml);

                if (tempColony == null)
                    return;

                Log.Info("Colony temp parse complete: PlanetName='{0}', SystemName='{1}', {2} structures",
                    tempColony.PlanetName ?? "(null)", tempColony.SystemName ?? "(null)", tempColony.Structures.Count);

                if (string.IsNullOrEmpty(tempColony.PlanetName))
                {
                    // Fall back to current behavior when no planet name is parsed
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

                var existingColony = ColonyImportHelper.FindByPlanet(
                    playerContext.GetCurrentPlayerColonies(), tempColony.PlanetName, tempColony.SystemName);

                Log.Info("Colony dedup: {0} for planet '{1}'",
                    existingColony != null ? "found existing colony UUID=" + existingColony.UUID : "no existing colony, creating new",
                    tempColony.PlanetName);

                if (existingColony != null)
                {
                    ColonyImportHelper.MergeIdentity(existingColony, tempColony);

                    // Save ColonyName before ProcessHtml -- the parser's ParsePlanetOverview
                    // overwrites ColonyName with the game's (potentially truncated) value.
                    string preservedColonyName = existingColony.ColonyName;
                    parser.ProcessHtml(existingColony, extractedHtml, empireContext);
                    existingColony.ColonyName = preservedColonyName;

                    selectedColony = existingColony;

                    Log.Info("Colony updated via dedup: {0} ({1} structures, {2} commodity requests)",
                        existingColony.ColonyName, existingColony.Structures.Count, existingColony.Commodities.Count);
                }
                else
                {
                    var newColony = ColonyImportHelper.CreateFromTemp(tempColony, playerContext.CurrentPlayerUUID);
                    playerContext.ColonyList.Add(newColony);
                    selectedColony = newColony;

                    Log.Info("New colony created via dedup: {0} ({1} structures, {2} commodity requests)",
                        newColony.ColonyName, newColony.Structures.Count, newColony.Commodities.Count);
                }

                playerContext.WriteContext();
                playerContext.OnColonyDataChanged(selectedColony.UUID);

                // Refresh list view
                txtColonyFilter_TextChanged(sender, e);

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
                UpdateTitle();
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
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show("No HTML content found on the clipboard.\n\nCopy colony data from the game browser first.",
                    "No HTML", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string clipboardData = Clipboard.GetText(TextDataFormat.Html);

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
                    File.WriteAllText(dlg.FileName, clipboardData, System.Text.Encoding.UTF8);
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

        // -------------------------------------------------------------------
        // Structure Type Filter (9.1, 9.2, 9.3)
        // -------------------------------------------------------------------

        /// <summary>
        /// Seeds _uncheckedStructureTypes from saved preferences so the filter
        /// is restored before the first PopulateStructureTypeFilter call.
        /// </summary>
        private void SeedUncheckedStructureTypes()
        {
            var store = PreferencesStore.GetInstance();
            string formTypeKey = GetType().Name;
            int windowNumber = Tag is int n ? n : 1;
            string stateKey = windowNumber.ToString();

            if (store.Preferences.Forms.TryGetValue(formTypeKey, out var windows) &&
                windows.TryGetValue(stateKey, out var windowState) &&
                windowState.FormState?.ListViews != null &&
                windowState.FormState.ListViews.TryGetValue("lvwStructureTypes", out var lvState) &&
                lvState.UncheckedItems != null)
            {
                foreach (var typeId in lvState.UncheckedItems)
                    _uncheckedStructureTypes.Add(typeId);
            }
        }

        /// <summary>
        /// Populates the structure type filter on first call, then applies the filter.
        /// </summary>
        private void PopulateStructureTypeFilter()
        {
            if (!_structureTypesPopulated)
            {
                _structureTypesPopulated = true;
                BuildStructureTypeList();
            }
        }

        /// <summary>
        /// Builds the lvwStructureTypes list from all flatpack blueprint types,
        /// checking/unchecking based on _uncheckedStructureTypes.
        /// </summary>
        private void BuildStructureTypeList()
        {
            lvwStructureTypes.ItemChecked -= lvwStructureTypes_ItemChecked;
            lvwStructureTypes.Items.Clear();

            var flatpackTypes = empireContext.BlueprintTypeList
                .Where(bt => bt.Id.IsFlatpack())
                .OrderBy(bt => bt.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var bpType in flatpackTypes)
            {
                var item = new ListViewItem(bpType.Name);
                item.Tag = bpType.Id;
                item.Checked = !_uncheckedStructureTypes.Contains(bpType.Id);
                lvwStructureTypes.Items.Add(item);
            }

            lvwStructureTypes.ItemChecked += lvwStructureTypes_ItemChecked;
        }

        /// <summary>
        /// Handler for structure type checkbox changes — updates the unchecked set
        /// and re-applies the filter.
        /// </summary>
        private void lvwStructureTypes_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (selectedColony == null) return;

            string typeId = e.Item.Tag as string;
            if (typeId != null)
            {
                if (e.Item.Checked)
                    _uncheckedStructureTypes.Remove(typeId);
                else
                    _uncheckedStructureTypes.Add(typeId);
            }

            ApplyStructureTypeFilter();
        }

        /// <summary>
        /// Shows/hides structure controls based on the current checked types.
        /// Lightweight — just toggles Visible on existing controls instead of full rebuild.
        /// </summary>
        private void ApplyStructureTypeFilter()
        {
            if (selectedColony == null) return;

            // Build set of checked types
            var checkedTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ListViewItem item in lvwStructureTypes.Items)
            {
                if (item.Checked)
                    checkedTypes.Add((string)item.Tag);
            }

            flpStructures.SuspendLayout();
            for (int i = 0; i < _poolInUse; i++)
            {
                var ctrl = _pool[i];
                if (ctrl.ViewModel == null) continue;
                var bp = playerContext.FindBlueprint(ctrl.ViewModel.Data.FlatpackBlueprintUUID);
                string typeId = bp?.BluePrintType ?? "";
                ctrl.Visible = checkedTypes.Count == 0 || checkedTypes.Contains(typeId);
            }
            flpStructures.ResumeLayout();
        }
    }
}
