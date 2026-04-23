using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

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
        private bool _overflowDirty = false;

        // Structure_Pool (8.1)
        private readonly List<ColonyStructureV2> _pool = new List<ColonyStructureV2>();
        private int _poolInUse = 0;

        // Background calculation cancellation (7.1)
        private CancellationTokenSource _calcCts;
        private int _calcGeneration = 0;

        // Structure type filter (9.1)
        private readonly HashSet<string> _uncheckedStructureTypes = new HashSet<string>(StringComparer.Ordinal);
        private bool _structureTypesPopulated = false;

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // WmSetRedraw: suppress all painting until re-enabled
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WmSetRedraw = 0x000B;

        private static void SuspendDrawing(Control control)
        {
            SendMessage(control.Handle, WmSetRedraw, false, 0);
        }

        private static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, WmSetRedraw, true, 0);
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
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList,
                playerContext.SupplyChainList, playerContext.WarehouseOverflowRuleList);

            // Configure colony list
            lvwColonies.Columns.Add("Planet", 80);
            lvwColonies.Columns.Add("Name", 80);
            lvwColonies.Columns.Add("Refs", 40);
            lvwColonies.ColumnClick += LvwColonies_ColumnClick;
            lvwColonies.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(playerContext.GetCurrentPlayerColonies());

            // Wire filter handler
            txtColonyFilter.TextChanged += TxtColonyFilter_TextChanged;

            // Wire colony selection handler
            lvwColonies.ItemSelectionChanged += LvwColonies_ItemSelectionChanged;

            // Wire write-through handlers
            txtPlanetName.TextChanged += TxtPlanetName_TextChanged;
            txtColonyName.TextChanged += TxtColonyName_TextChanged;
            txtSystemName.TextChanged += TxtSystemName_TextChanged;

            // Wire tab change for deferred population
            tabDetailedData.SelectedIndexChanged += TabDetailedData_SelectedIndexChanged;

            // Wire flatpack filter and add button (8.3)
            txtFilterFlatpack.TextChanged += TxtFilterFlatpack_TextChanged;
            cmdAddFlatpack.Click += CmdAddFlatpack_Click;
            PopulateFlatpackCombo();

            // Structure type filter (9.1, 9.3)
            SeedUncheckedStructureTypes();

            // Wire commodity request handlers (19.1-19.8)
            txtCommodityRequestFilter.TextChanged += TxtCommodityRequestFilter_TextChanged;
            cmdAddCommodityRequest.Click += CmdAddCommodityRequest_Click;
            dgvCommodityRequests.CurrentCellDirtyStateChanged += DgvCommodityRequests_CurrentCellDirtyStateChanged;
            dgvCommodityRequests.CellValueChanged += DgvCommodityRequests_CellValueChanged;
            dgvCommodityRequests.CellValidating += DgvCommodityRequests_CellValidating;
            dgvCommodityRequests.SelectionChanged += DgvCommodityRequests_SelectionChanged;
            dgvCommodityRequests.KeyDown += DgvCommodityRequests_KeyDown;
            UpdateCommodityRequestList();

            // Enable owner-draw so tab BackColor renders with visual styles (11.4)
            tabDetailedData.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabDetailedData.DrawItem += TabDetailedData_DrawItem;

            // Wire warehouse handlers (20.1-20.6)
            cmbItemType.DataSource = Models.ItemType.ItemTypes;
            cmbItemType.DisplayMember = "Name";
            cmbItemType.SelectedIndexChanged += CmbItemType_SelectedIndexChanged;
            txtItemFilter.TextChanged += TxtItemFilter_TextChanged;
            cmbItem.SelectedIndexChanged += CmbItem_SelectedIndexChanged;
            cmbPurity.DataSource = Models.ResourcePurity.Purities;
            cmbPurity.DisplayMember = "Name";
            cmdAddItem.Click += CmdAddItem_Click;
            dgvItems.CellValidating += DgvItems_CellValidating;
            dgvItems.CellValueChanged += DgvItems_CellValueChanged;
            dgvItems.SelectionChanged += DgvItems_SelectionChanged;
            dgvItems.KeyDown += DgvItems_KeyDown;

            // Wire admin refresh timer (11.1)
            timerAdminRefresh.Tick += TimerAdminRefresh_Tick;
            timerAdminRefresh.Start();

            // Wire overflow tab handlers (task 38)
            cmbOverflowDestType.Items.Add(DestinationType.Colony);
            cmbOverflowDestType.Items.Add(DestinationType.Station);
            if (cmbOverflowDestType.Items.Count > 0) cmbOverflowDestType.SelectedIndex = 0;
            cmbOverflowDestType.SelectedIndexChanged += CmbOverflowDestType_SelectedIndexChanged;
            txtOverflowResourceFilter.TextChanged += (s, ev) => { if (_isProgrammaticUpdate == 0) PopulateOverflowResourceCombo(); };
            txtOverflowDestFilter.TextChanged += (s, ev) => { if (_isProgrammaticUpdate == 0) PopulateOverflowDestCombo(); };
            txtOverflowRouteFilter.TextChanged += (s, ev) => { if (_isProgrammaticUpdate == 0) PopulateOverflowRouteCombo(); };
            cmdAddOverflowRule.Click += CmdAddOverflowRule_Click;
            cmdRemoveOverflowRule.Click += CmdRemoveOverflowRule_Click;
            dgvOverflowRules.CurrentCellDirtyStateChanged += DgvOverflowRules_CurrentCellDirtyStateChanged;
            dgvOverflowRules.CellValueChanged += DgvOverflowRules_CellValueChanged;

            // Wire import handlers (21.1, 21.5)
            cmdImportColony.Click += CmdImportColony_Click;
            cmdImportClipboard.Click += CmdImportClipboard_Click;

            // Subscribe to context events
            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;

            // Diagnostic: log splitter state after construction
            Log.Info(
                "V2.Constructor: splitMain.SplitterDistance={0} SplitterWidth={1} Panel1.Width={2} Panel2.Width={3} " + "splitMain.Width={4} splitMain.Orientation={5} IsSplitterFixed={6} Panel1MinSize={7} Panel1.BorderStyle={8} " + "splitMain.BackColor={9} Panel1.BackColor={10}",
                splitMain.SplitterDistance,
                splitMain.SplitterWidth,
                splitMain.Panel1.Width,
                splitMain.Panel2.Width,
                splitMain.Width,
                splitMain.Orientation,
                splitMain.IsSplitterFixed,
                splitMain.Panel1MinSize,
                splitMain.Panel1.BorderStyle,
                splitMain.BackColor,
                splitMain.Panel1.BackColor);

            this.Shown += FormColonyV2_Shown;

            UpdateTitle();
        }

        // -------------------------------------------------------------------
        // Event lifecycle
        // -------------------------------------------------------------------

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Cancel any in-progress background calculation
            _calcCts?.Cancel();

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
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            Log.Debug("V2.OnCurrentPlayerChanged: player={0}", playerContext.CurrentPlayer?.Name ?? "(none)");

            using var guard = new ProgrammaticUpdateGuard(this);
            lvwColonies.Items.Clear();
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            _referenceCounter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList);
            PopulateListView(playerContext.GetCurrentPlayerColonies());
            txtPlanetName.Text = string.Empty;
            txtColonyName.Text = string.Empty;
            txtSystemName.Text = string.Empty;
            MarkAllTabsDirty();
            UpdateTabWarnings();
            UpdateTitle();
        }

        private void OnColonyDataChanged(object sender, ColonyDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnColonyDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

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

        private void TxtPlanetName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.PlanetName = txtPlanetName.Text;
        }

        private void TxtColonyName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            colonyViewModel.ColonyName = txtColonyName.Text;
        }

        private void TxtSystemName_TextChanged(object sender, EventArgs e)
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
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList);

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

            sw.Stop();
            Log.Info("PERF PopulateListView: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Filter (6.5)
        // -------------------------------------------------------------------

        private void TxtColonyFilter_TextChanged(object sender, EventArgs e)
        {
            string filter = txtColonyFilter.Text;
            var colonies = playerContext.GetCurrentPlayerColonies();
            if (!string.IsNullOrEmpty(filter))
            {
                colonies = colonies
                    .Where(c => (c.PlanetName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                             || (c.ColonyName ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }

            lvwColonies.Items.Clear();
            PopulateListView(colonies);
            lvwColonies.Sort();

            // If filter results in empty list, clear the form
            if (lvwColonies.Items.Count == 0)
            {
                using var guard = new ProgrammaticUpdateGuard(this);
                selectedColony = new Models.Colony();
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                txtPlanetName.Text = string.Empty;
                txtColonyName.Text = string.Empty;
                txtSystemName.Text = string.Empty;
                MarkAllTabsDirty();
                UpdateDeleteButtonState();
            }
        }

        // -------------------------------------------------------------------
        // Column sort (6.6)
        // -------------------------------------------------------------------

        private void LvwColonies_ColumnClick(object sender, ColumnClickEventArgs e)
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

        private void LvwColonies_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvwColonies.SelectedItems.Count == 1)
            {
                // Cancel any in-progress background calculation
                _calcCts?.Cancel();
                _calcCts = new CancellationTokenSource();
                var cts = _calcCts;
                int generation = Interlocked.Increment(ref _calcGeneration);

                selectedColony = lvwColonies.SelectedItems[0].Tag as Models.Colony;
                Log.Debug(
                    "V2.LvwColonies_ItemSelectionChanged: colony={0} uuid={1}",
                    selectedColony?.ColonyName ?? selectedColony?.PlanetName ?? "(null)",
                    selectedColony?.UUID ?? "(null)");
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);

                // Immediate: show identity fields on UI thread
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    txtPlanetName.Text = colonyViewModel.PlanetName;
                    txtColonyName.Text = colonyViewModel.ColonyName;
                    txtSystemName.Text = colonyViewModel.Data.SystemName ?? string.Empty;
                }

                // Show "Calculating..." indicator
                SetCalculatingState(true);

                // Queue expensive work on ThreadPool
                var colony = selectedColony;
                var vm = colonyViewModel;
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (cts.IsCancellationRequested) return;

                    // Acquire write lock for RecalculateStatus (mutates Locks, Statuses)
                    if (!colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
                    {
                        Log.Warn("Background calc: write lock timeout on colony {0}", colony.UUID);
                        return;
                    }

                    try
                    {
                        if (cts.IsCancellationRequested) return;
                        vm.RecalculateStatus();
                    }
                    finally
                    {
                        colony.ColonyLock.ExitWriteLock();
                    }

                    if (cts.IsCancellationRequested) return;

                    // Marshal results back to UI thread
                    try
                    {
                        BeginInvoke(new Action(() =>
                        {
                            if (cts.IsCancellationRequested) return;
                            if (generation != _calcGeneration) return; // stale

                            SetCalculatingState(false);
                            PopulateForm();
                            UpdateDeleteButtonState();
                            RefreshAdminReport();
                            UpdateTabWarnings();
                            UpdateTitle();
                        }));
                    }
                    catch (ObjectDisposedException)
                    {
                        /* form closed */
                    }
                    catch (InvalidOperationException)
                    {
                        /* handle not created */
                    }
                });
            }
        }

        private void SetCalculatingState(bool calculating)
        {
            if (calculating)
            {
                rtbStatusSummary.Text = "Calculating...";
            }
        }

        private void PopulateForm()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);

            if (selectedColony == null) return;

            var pfSw = System.Diagnostics.Stopwatch.StartNew();

            Log.Debug("V2.PopulateForm: colony={0}", selectedColony.ColonyName ?? selectedColony.PlanetName ?? "(null)");

            txtPlanetName.Text = colonyViewModel.PlanetName;
            txtColonyName.Text = colonyViewModel.ColonyName;
            txtSystemName.Text = colonyViewModel.Data.SystemName ?? string.Empty;

            long t0 = pfSw.ElapsedMilliseconds;

            MarkAllTabsDirty();

            // Immediately populate the currently visible tab
            PopulateActiveTab();

            pfSw.Stop();
            Log.Info(
                "V2.PopulateForm PERF: total={0}ms identity={1}ms activeTab={2}ms",
                pfSw.ElapsedMilliseconds,
                t0,
                pfSw.ElapsedMilliseconds - t0);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void MarkAllTabsDirty()
        {
            _structuresDirty = true;
            _warehouseDirty = true;
            _workersDirty = true;
            _adminDirty = true;
            _overflowDirty = true;
        }

        /// <summary>
        /// Populates whichever tab is currently selected, if it's dirty.
        /// Called after MarkAllTabsDirty to handle the case where the user
        /// is already on a tab and switches colonies.
        /// </summary>
        private void PopulateActiveTab()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            else if (tab == tabPOverflow && _overflowDirty)
            {
                PopulateOverflowGrid();
                _overflowDirty = false;
            }

            sw.Stop();
            Log.Info("PERF PopulateActiveTab: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TabDetailedData_SelectedIndexChanged(object sender, EventArgs e)
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
            else if (tab == tabPOverflow && _overflowDirty)
            {
                PopulateOverflowGrid();
                _overflowDirty = false;
            }
        }

        // -------------------------------------------------------------------
        // CRUD operations (6.9)
        // -------------------------------------------------------------------

        private void CmdNew_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            txtPlanetName.Text = string.Empty;
            txtColonyName.Text = string.Empty;
            txtSystemName.Text = string.Empty;
            lvwColonies.SelectedItems.Clear();
            MarkAllTabsDirty();
            UpdateDeleteButtonState();
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            using var guard = new ProgrammaticUpdateGuard(this);

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdSave_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(colonyViewModel.Data.OwnerUUID))
                {
                    colonyViewModel.Data.OwnerUUID = playerContext.CurrentPlayerUUID;
                }

                colonyViewModel.Save();
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            // Fire event and persist OUTSIDE the lock
            playerContext.OnColonyDataChanged(selectedColony.UUID);
            playerContext.WriteContext();
            // Refresh list with current filter
            TxtColonyFilter_TextChanged(sender, e);
            UpdateTitle();
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID)) return;

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList);
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

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdDelete_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                playerContext.RemoveColony(selectedColony);
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            // Persist and update UI OUTSIDE the lock
            playerContext.WriteContext();

            using var guard = new ProgrammaticUpdateGuard(this);
            selectedColony = new Models.Colony();
            colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
            txtPlanetName.Text = string.Empty;
            txtColonyName.Text = string.Empty;
            txtSystemName.Text = string.Empty;
            MarkAllTabsDirty();

            // Refresh list
            TxtColonyFilter_TextChanged(sender, e);
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
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList);
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
        // Structure panel population (8.2)
        // -------------------------------------------------------------------

        private void PopulateStructures()
        {
            if (selectedColony == null) return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            using var guard = new ProgrammaticUpdateGuard(this);

            Log.Debug("V2.PopulateStructures: starting");

            // Ensure structure type filter list is populated (9.1)
            PopulateStructureTypeFilter();

            // Suppress all painting until we're done updating controls
            SuspendDrawing(flpStructures);

            // Build set of checked types for filter (9.2)
            var checkedTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ListViewItem item in lvwStructureTypes.Items)
            {
                if (item.Checked)
                    checkedTypes.Add((string)item.Tag);
            }

            Log.Info(
                "V2.PopulateStructures: structureCount={0} filterActive={1} filterTypes={2}",
                selectedColony.Structures?.Count ?? 0,
                checkedTypes.Count > 0,
                checkedTypes.Count > 0 ? string.Join(", ", checkedTypes) : "(none)");

            flpStructures.SuspendLayout();

            // Snapshot structure view models under read lock
            IReadOnlyList<ColonyStructureViewModel> structureVMs;
            if (!selectedColony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateStructures: read lock timeout on colony {0}, using stale data", selectedColony.UUID);
                structureVMs = colonyViewModel.StructureViewModels;
            }
            else
            {
                try
                {
                    structureVMs = new List<ColonyStructureViewModel>(colonyViewModel.StructureViewModels);
                }
                finally
                {
                    selectedColony.ColonyLock.ExitReadLock();
                }
            }

            int needed = structureVMs.Count;

            long t0 = sw.ElapsedMilliseconds;

            // Grow pool if needed (without removing/re-adding to Controls)
            while (_pool.Count < needed)
            {
                var newCtrl = new ColonyStructureV2();
                newCtrl.ColonyStructureDataChanged += Structures_ColonyStructureDataChanged;
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
                    string typeId = bp?.BluePrintType ?? string.Empty;

                    ctrl.ViewModel = vm;
                    ctrl.Colony = selectedColony;
                    var udSw = System.Diagnostics.Stopwatch.StartNew();
                    ctrl.UpdateData(bp);
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
                        ctrl.ViewModel = null;
                        ctrl.Colony = null;
                        if (ctrl.TimerRunning)
                            ctrl.StopTimer();
                        rSw.Stop();
                        resetTotal += rSw.ElapsedMilliseconds;
                    }
                }
            }

            _poolInUse = needed;

            // Count visible structures for logging
            int visibleCount = 0;
            for (int j = 0; j < needed; j++)
                if (_pool[j].Visible) visibleCount++;

            long t2 = sw.ElapsedMilliseconds;

            flpStructures.ResumeLayout();
            ResumeDrawing(flpStructures);

            long t3 = sw.ElapsedMilliseconds;

            RefreshStatusSummary();

            sw.Stop();
            Log.Info(
                "V2.PopulateStructures PERF: total={0}ms pool={1}ms updateData={2}ms(x{3}) reset={4}ms layout={5}ms visible={6}/{3}",
                sw.ElapsedMilliseconds,
                t1 - t0,
                updateDataTotal,
                needed,
                resetTotal,
                t3 - t2,
                visibleCount);
            sw.Stop();
            Log.Info("PERF PopulateStructures: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Flatpack filter + combo + add (8.3)
        // -------------------------------------------------------------------

        private void PopulateFlatpackCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateFlatpackCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtFilterFlatpack_TextChanged(object sender, EventArgs e)
        {
            PopulateFlatpackCombo();
            cmbFlatpacks.DroppedDown = true;
        }

        private void CmdAddFlatpack_Click(object sender, EventArgs e)
        {
            if (cmbFlatpacks.SelectedValue == null || string.IsNullOrEmpty(cmbFlatpacks.SelectedValue.ToString()))
                return;

            string uuid = cmbFlatpacks.SelectedValue.ToString();
            Log.Debug("V2.CmdAddFlatpack_Click: blueprintUUID={0}", uuid);

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdAddFlatpack_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                colonyViewModel.AddStructure(uuid);
                colonyViewModel.RecalculateStatus();
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            PopulateStructures();

            // Save context and fire event OUTSIDE the lock
            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }
        }

        // -------------------------------------------------------------------
        // Structure change handlers (8.4, 8.5)
        // -------------------------------------------------------------------

        private void Structures_ColonyStructureDataChanged(object sender, ColonyStructureDataChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            var ctrl = sender as ColonyStructureV2;
            Log.Debug(
                "V2.Structures_ColonyStructureDataChanged: structural={0} sender={1}",
                e.IsStructural,
                ctrl?.ViewModel?.Data?.UUID ?? "(unknown)");

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("Structures_ColonyStructureDataChanged: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                if (e.IsStructural)
                {
                    // 8.4: Structural change — full rebuild
                    colonyViewModel.InvalidateStructureViewModels();
                    colonyViewModel.RecalculateStatus();
                }
                else
                {
                    // 8.5: Non-structural change — O(1) delta update
                    if (ctrl?.ViewModel != null)
                    {
                        colonyViewModel.Calculator.RecalculateStructure(ctrl.ViewModel.Data);
                    }
                }
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            if (e.IsStructural)
            {
                PopulateStructures();
            }
            else
            {
                ctrl?.UpdateBackgroundColor();
            }

            RefreshStatusSummary();
            UpdateTabWarnings();

            // Save context and fire event OUTSIDE the lock
            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }
        }

        // -------------------------------------------------------------------
        // Status summary (8.6)
        // -------------------------------------------------------------------

        private void RefreshStatusSummary()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var status = colonyViewModel.Calculator.FinalActualStatus;
            if (status == null)
            {
                rtbStatusSummary.Text = string.Empty;
                return;
            }

            var builder = new RtfBuilder();
            ColonyStatusCalculator.PopulateStatus(builder, status);
            rtbStatusSummary.Rtf = builder.ToRtf();
            sw.Stop();
            Log.Info("PERF RefreshStatusSummary: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Structures tab layout handler
        // -------------------------------------------------------------------

        private void TabPStructures_Layout(object sender, LayoutEventArgs e)
        {
            // Size pooled controls to match the structure panel width (inside splitStructures.Panel2)
            int w = flpStructures.ClientSize.Width;
            for (int i = 0; i < _poolInUse; i++)
            {
                _pool[i].Width = w - SystemInformation.VerticalScrollBarWidth - 6;
            }
        }

        // -------------------------------------------------------------------
        // Splitter visual indicator
        // -------------------------------------------------------------------

        private void FormColonyV2_Shown(object sender, EventArgs e)
        {
            Log.Info(
                "V2.Shown: splitMain.SplitterDistance={0} SplitterWidth={1} Panel1.Width={2} Panel2.Width={3} " + "splitMain.Width={4} splitMain.Height={5} SplitterRect={6} IsSplitterFixed={7} " + "Panel1.BorderStyle={8} Panel1.Visible={9} Panel2.Visible={10} splitMain.Visible={11}",
                splitMain.SplitterDistance,
                splitMain.SplitterWidth,
                splitMain.Panel1.Width,
                splitMain.Panel2.Width,
                splitMain.Width,
                splitMain.Height,
                splitMain.SplitterRectangle,
                splitMain.IsSplitterFixed,
                splitMain.Panel1.BorderStyle,
                splitMain.Panel1.Visible,
                splitMain.Panel2.Visible,
                splitMain.Visible);

            // Auto-select the first colony if none is selected (e.g. first open, no saved selection)
            // Clear any restored filter first so the full list is visible
            if (!string.IsNullOrEmpty(txtColonyFilter.Text))
            {
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    txtColonyFilter.Text = string.Empty;
                }

                TxtColonyFilter_TextChanged(this, EventArgs.Empty);
            }

            if (lvwColonies.SelectedItems.Count == 0 && lvwColonies.Items.Count > 0)
            {
                lvwColonies.Items[0].Selected = true;
                lvwColonies.EnsureVisible(0);
            }
        }

        private void SplitMain_Paint(object sender, PaintEventArgs e)
        {
            // Draw a visible bar on the splitter area so the user can find it
            var rect = splitMain.SplitterRectangle;
            Log.Info("SplitMain_Paint fired: SplitterRect={0}, ClipRect={1}", rect, e.ClipRectangle);
            using (var brush = new System.Drawing.SolidBrush(System.Drawing.SystemColors.ControlDark))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            // Draw grip dots in the center
            int midX = rect.X + rect.Width / 2;
            int midY = rect.Y + rect.Height / 2;
            using (var dotBrush = new System.Drawing.SolidBrush(System.Drawing.SystemColors.ControlDarkDark))
            {
                for (int dy = -20; dy <= 20; dy += 10)
                {
                    e.Graphics.FillEllipse(dotBrush, midX - 1, midY + dy - 1, 3, 3);
                }
            }
        }

        // -------------------------------------------------------------------
        // Layout handler
        // -------------------------------------------------------------------

        private void FlpColonyData_Layout(object sender, LayoutEventArgs e)
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
                rtbAdminReport.Rtf = string.Empty;
                return;
            }

            var arSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Acquire read lock to ensure consistent colony data for report
                if (!selectedColony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
                {
                    Log.Warn("RefreshAdminReport: read lock timeout on colony {0}, using stale data", selectedColony.UUID);
                }
                else
                {
                    try
                    {
                        // Lock held just to ensure consistent read — BuildReport reads colony data
                    }
                    finally
                    {
                        selectedColony.ColonyLock.ExitReadLock();
                    }
                }

                string rtf = ColonyAdminReportBuilder.BuildReport(selectedColony, playerContext);
                rtbAdminReport.Rtf = string.IsNullOrEmpty(rtf) ? string.Empty : rtf;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error building admin report");
            }

            arSw.Stop();
            if (arSw.ElapsedMilliseconds > 10)
                Log.Info("V2.RefreshAdminReport PERF: {0}ms", arSw.ElapsedMilliseconds);
            sw.Stop();
            Log.Info("PERF RefreshAdminReport: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TimerAdminRefresh_Tick(object sender, EventArgs e)
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

        private void CmdBootstrap_Click(object sender, EventArgs e)
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

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdBootstrap_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                var bootstrap = new ColonyBootstrap(playerContext);
                bootstrap.Bootstrap(selectedColony);

                // Refresh via structural change pattern
                colonyViewModel.InvalidateStructureViewModels();
                colonyViewModel.RecalculateStatus();
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();

            if (!string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }
        }

        private void CmdOptimize_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;

            Log.Info(
                "CmdOptimize_Click: colony={0} structureCount={1} filterActive={2}",
                selectedColony.ColonyName ?? selectedColony.PlanetName,
                selectedColony.Structures?.Count ?? 0,
                lvwStructureTypes.CheckedItems.Count > 0);

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdOptimize_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                var optimizer = new BuildOrderOptimizer(playerContext);
                var optimized = optimizer.Optimize(selectedColony);

                Log.Info(
                    "CmdOptimize_Click: optimizer returned {0} structures (input was {1})",
                    optimized.Count,
                    selectedColony.Structures.Count);

                selectedColony.Structures.Clear();
                selectedColony.Structures.AddRange(optimized);

                Log.Info(
                    "CmdOptimize_Click: colony now has {0} structures after replace",
                    selectedColony.Structures.Count);

                // Refresh via structural change pattern
                colonyViewModel.InvalidateStructureViewModels();
                colonyViewModel.RecalculateStatus();
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();

            if (!string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }
        }

        // -------------------------------------------------------------------
        // Generate Build Plan (11.1, 11.2, 11.3)
        // -------------------------------------------------------------------

        private void CmdGenerateBuildPlan_Click(object sender, EventArgs e)
        {
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID))
            {
                MessageBox.Show(
                    "Select a colony first.",
                    "No Colony",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Log.Info(
                "CmdGenerateBuildPlan_Click: colony={0} uuid={1}",
                selectedColony.ColonyName ?? selectedColony.PlanetName,
                selectedColony.UUID);

            // Show plan picker dialog
            BuildPlan targetPlan = ShowBuildPlanPickerDialog();
            if (targetPlan == null) return;

            int added = BuildPlanService.GenerateColonyBuildItems(
                selectedColony, targetPlan, playerContext.FindBlueprint);

            Log.Info(
                "CmdGenerateBuildPlan_Click: {0} items added to plan '{1}'",
                added,
                targetPlan.Name);

            // Persist new/updated plan
            if (!playerContext.BuildPlanList.Contains(targetPlan))
                playerContext.AddBuildPlan(targetPlan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(targetPlan.UUID);

            MessageBox.Show(
                string.Format("{0} flatpack build items added to plan '{1}'.", added, targetPlan.Name),
                "Build Plan Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows a dialog letting the user create a new build plan or pick an existing one.
        /// Returns the selected/created BuildPlan, or null if cancelled.
        /// </summary>
        private BuildPlan ShowBuildPlanPickerDialog()
        {
            var existingPlans = playerContext.GetCurrentPlayerBuildPlans();

            using (var form = new Form())
            {
                form.Text = "Generate Build Plan";
                form.ClientSize = new Size(400, 180);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var rbNew = new RadioButton
                {
                    Text = "Create new plan",
                    Left = 15, Top = 15, Width = 360,
                    Checked = true
                };

                var rbExisting = new RadioButton
                {
                    Text = "Add to existing plan",
                    Left = 15, Top = 40, Width = 360
                };

                var cmbPlans = new ComboBox
                {
                    Left = 35, Top = 65, Width = 340,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Enabled = false
                };

                foreach (var plan in existingPlans)
                    cmbPlans.Items.Add(plan);
                cmbPlans.DisplayMember = "Name";
                if (cmbPlans.Items.Count > 0)
                    cmbPlans.SelectedIndex = 0;

                // Disable existing option if no plans exist
                if (existingPlans.Count == 0)
                    rbExisting.Enabled = false;

                rbNew.CheckedChanged += (s, ev) => { cmbPlans.Enabled = !rbNew.Checked; };
                rbExisting.CheckedChanged += (s, ev) => { cmbPlans.Enabled = rbExisting.Checked; };

                var btnOk = new Button
                {
                    Text = "OK", Left = 210, Top = 110, Width = 75,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 295, Top = 110, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { rbNew, rbExisting, cmbPlans, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) != DialogResult.OK)
                    return null;

                if (rbNew.Checked)
                {
                    string planName = string.Format(
                        "{0} - Build Plan",
                        selectedColony.ColonyName ?? selectedColony.PlanetName ?? "Colony");
                    return new BuildPlan
                    {
                        UUID = Guid.NewGuid().ToString(),
                        Name = planName,
                        OwnerUUID = playerContext.CurrentPlayerUUID,
                        IsActive = true
                    };
                }
                else
                {
                    return cmbPlans.SelectedItem as BuildPlan;
                }
            }
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

        private void TabDetailedData_DrawItem(object sender, DrawItemEventArgs e)
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
            ApplyTabWarning(
                tabPStructures,
                TabWarningService.EvaluateStructureWarning(structureCount));

            ApplyTabWarning(
                tabPWorkers,
                TabWarningService.EvaluateWorkerWarning( selectedColony?.Commodities, SystemClock.UtcNow));

            ApplyTabWarning(
                tabPAdministration,
                TabWarningService.EvaluateColonyImportStalenessWarning( selectedColony?.LastImportDateTime, SystemClock.UtcNow));

            UpdateWorkerTabTitle();
            UpdateStructuresTabTitle();
        }

        // -------------------------------------------------------------------
        // Workers Tab — Commodity Requests (19.1-19.8)
        // -------------------------------------------------------------------

        private void CmdAddCommodityRequest_Click(object sender, EventArgs e)
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

        private void TxtCommodityRequestFilter_TextChanged(object sender, EventArgs e)
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

            var filteredSource = new BindingSource();
            filteredSource.DataSource = filteredList;

            cmbCommodityRequest.DataSource = null;
            cmbCommodityRequest.DisplayMember = "ExtendedName";
            cmbCommodityRequest.ValueMember = "Name";
            cmbCommodityRequest.DataSource = filteredSource;
        }

        private void PopulateCommodityRequestGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCommodityRequests.CellValidating -= DgvCommodityRequests_CellValidating;
            try
            {
                dgvCommodityRequests.EndEdit();
            }
            catch
            {
            }

            // Auto-cleanup expired fulfilled requests (19.6)
            int cleaned = colonyViewModel.CleanupExpiredCommodityRequests();
            if (cleaned > 0)
            {
                Log.Debug("Cleaned up {0} expired commodity requests", cleaned);
                if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
                    playerContext.WriteContext();
            }

            // Snapshot commodity requests under read lock
            IReadOnlyList<CommodityRequested> requests;
            if (selectedColony != null && !selectedColony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateCommodityRequestGrid: read lock timeout on colony {0}, using stale data", selectedColony?.UUID);
                requests = colonyViewModel.GetCommodityRequests();
            }
            else
            {
                try
                {
                    requests = new List<CommodityRequested>(colonyViewModel.GetCommodityRequests());
                }
                finally
                {
                    selectedColony?.ColonyLock.ExitReadLock();
                }
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

            dgvCommodityRequests.CellValidating += DgvCommodityRequests_CellValidating;

            UpdateWorkerTabTitle();
            sw.Stop();
            Log.Info("PERF PopulateCommodityRequestGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvCommodityRequests_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCommodityRequests.IsCurrentCellDirty)
            {
                dgvCommodityRequests.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvCommodityRequests_CellValueChanged(object sender, DataGridViewCellEventArgs e)
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
                string text = row.Cells[3].Value?.ToString() ?? string.Empty;
                DateTime? parsed = ParseCountdownToDateTime(text);
                if (parsed.HasValue)
                {
                    request.NeedBy = parsed.Value;
                    playerContext.WriteContext();
                }
            }

            UpdateTabWarnings();
        }

        private void DgvCommodityRequests_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
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
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = string.Empty;
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
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = string.Empty;
                }
            }

            // Column 3 = NeedBy (countdown format)
            else if (e.ColumnIndex == 3)
            {
                string value = e.FormattedValue?.ToString();
                if (string.IsNullOrEmpty(value))
                {
                    dgvCommodityRequests.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = string.Empty;
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
                    dgvCommodityRequests.Rows[e.RowIndex].ErrorText = string.Empty;
                }
            }
        }

        private void DgvCommodityRequests_SelectionChanged(object sender, EventArgs e)
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

        private void DgvCommodityRequests_KeyDown(object sender, KeyEventArgs e)
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

            var now = SystemClock.UtcNow;
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
            var match = Regex.Match(
                text.Trim(),
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
            return SystemClock.UtcNow.AddSeconds(totalSeconds);
        }

        /// <summary>
        /// Formats a NeedBy DateTime as a countdown string relative to now.
        /// Returns empty string for DateTime.MinValue. Shows "overdue" for past-due (19.7).
        /// </summary>
        private string FormatNeedByCountdown(DateTime needBy)
        {
            if (needBy == DateTime.MinValue) return string.Empty;
            var remaining = needBy - SystemClock.UtcNow;
            if (remaining.TotalSeconds <= 0)
                return "overdue";

            string result = string.Empty;
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvItems.CellValidating -= DgvItems_CellValidating;
            try
            {
                dgvItems.EndEdit();
            }
            catch
            {
            }

            // Snapshot items under read lock
            List<KeyValuePair<string, Item>> itemsSnapshot;
            if (selectedColony != null && !selectedColony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateItemGrid: read lock timeout on colony {0}, using stale data", selectedColony?.UUID);
                itemsSnapshot = new List<KeyValuePair<string, Item>>(colonyViewModel.GetItems());
            }
            else
            {
                try
                {
                    itemsSnapshot = new List<KeyValuePair<string, Item>>(colonyViewModel.GetItems());
                }
                finally
                {
                    selectedColony?.ColonyLock.ExitReadLock();
                }
            }

            // Index existing rows by item UUID for in-place update (20.6)
            var existingRows = new Dictionary<string, DataGridViewRow>();
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                var item = row.Tag as Item;
                if (item != null && !string.IsNullOrEmpty(item.UUID))
                    existingRows[item.UUID] = row;
            }

            var seen = new HashSet<string>();
            foreach (KeyValuePair<string, Item> itemEntry in itemsSnapshot)
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

            dgvItems.CellValidating += DgvItems_CellValidating;
            sw.Stop();
            Log.Info("PERF PopulateItemGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Refreshes only the Locked column (index 2) without rebuilding the grid.
        /// </summary>
        private void RefreshItemGridLocks()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            foreach (DataGridViewRow row in dgvItems.Rows)
            {
                Item item = row.Tag as Item;
                if (item == null) continue;
                int lockedQty = colonyViewModel.Data.Locks != null
                    ? colonyViewModel.Data.Locks.GetLockedQuantity(item.ItemType, item.BaseItemTypeID)
                    : 0;
                row.Cells[2].Value = lockedQty;
            }

            sw.Stop();
            Log.Info("PERF RefreshItemGridLocks: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Warehousing — Item Type Combo (20.2)
        // -------------------------------------------------------------------

        private void CmbItemType_SelectedIndexChanged(object sender, EventArgs e)
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

        private void CmbItem_SelectedIndexChanged(object sender, EventArgs e)
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

        private void TxtItemFilter_TextChanged(object sender, EventArgs e)
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithResources: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithCommodities()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithCommodities: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithWorkerDetails()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithWorkerDetails: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithSurveys()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithSurveys: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithBlueprints()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithBlueprints: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithBlueprintsByOutputType(Models.ItemType.ItemTypeEnum outputType)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
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
            sw.Stop();
            Log.Info("PERF PopulateItemWithBlueprintsByOutputType: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Warehousing — Add Item (20.3)
        // -------------------------------------------------------------------

        private void CmdAddItem_Click(object sender, EventArgs e)
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

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdAddItem_Click: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
                colonyViewModel.AddItem(item);
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            PopulateItemGrid();

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }
        }

        private static decimal GetItemVolume(Models.Item item, PlayerContext playerContext)
        {
            switch (item.ItemType)
            {
                case Models.ItemType.ItemTypeEnum.Resource:
                    return GameConstants.VolumeResource;
                case Models.ItemType.ItemTypeEnum.Commodity:
                    return GameConstants.VolumeCommodity;
                case Models.ItemType.ItemTypeEnum.WorkDetail:
                    return GameConstants.VolumeWorkDetail;
                case Models.ItemType.ItemTypeEnum.Blueprint:
                case Models.ItemType.ItemTypeEnum.Survey:
                    return GameConstants.VolumeBlueprint;
                default:
                    // Manufactured items: read CargoVolumeSize from blueprint
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID) && playerContext != null)
                    {
                        Models.Blueprint bp = playerContext.FindBlueprint(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            decimal vol = 0;
                            bp.Properties.GetDecimal(BlueprintPropertyKeys.CargoVolumeSize, 0, out vol);
                            return vol;
                        }
                    }

                    return 0.0m;
            }
        }

        // -------------------------------------------------------------------
        // Warehousing — Delete Item (20.4)
        // -------------------------------------------------------------------

        private void DgvItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (dgvItems.SelectedRows.Count == 0) return;

            if (!selectedColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("DgvItems_KeyDown: write lock timeout on colony {0}", selectedColony.UUID);
                return;
            }

            try
            {
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
            }
            finally
            {
                selectedColony.ColonyLock.ExitWriteLock();
            }

            PopulateItemGrid();

            if (selectedColony != null && !string.IsNullOrEmpty(selectedColony.UUID))
            {
                playerContext.OnColonyDataChanged(selectedColony.UUID);
                playerContext.WriteContext();
            }

            e.Handled = true;
        }

        // -------------------------------------------------------------------
        // Warehousing — Editable Amount Column (20.5)
        // -------------------------------------------------------------------

        private void DgvItems_CellValueChanged(object sender, DataGridViewCellEventArgs e)
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

        private void DgvItems_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // Only validate the Amount column (index 3)
            if (e.ColumnIndex != 3) return;
            if (e.RowIndex < 0) return;

            string value = e.FormattedValue?.ToString();

            if (string.IsNullOrEmpty(value))
            {
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = 0;
                dgvItems.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.White;
                dgvItems.Rows[e.RowIndex].ErrorText = string.Empty;
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
                dgvItems.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private void DgvItems_SelectionChanged(object sender, EventArgs e)
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

        private void CmdImportColony_Click(object sender, EventArgs e)
        {
            Log.Debug("V2.CmdImportColony_Click: starting import");
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show(
                    "No HTML content found on the clipboard.\n\nCopy colony data from the game browser first.",
                    "No HTML",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrEmpty(playerContext.CurrentPlayerUUID))
            {
                MessageBox.Show(
                    "No player selected. Select a player profile first.",
                    "No Player",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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
                    MessageBox.Show(
                        $"The clipboard contains {found}, not colony data.\n\nCopy the colony page from the game browser first.",
                        "Wrong Content",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                var parser = new ColonyParser();
                var tempColony = parser.ParseClipboardToTemp(empireContext, out string extractedHtml);

                if (tempColony == null)
                    return;

                Log.Info(
                    "Colony temp parse complete: PlanetName='{0}', SystemName='{1}', {2} structures",
                    tempColony.PlanetName ?? "(null)",
                    tempColony.SystemName ?? "(null)",
                    tempColony.Structures.Count);

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

                    Log.Info(
                        "Colony imported from clipboard (fallback): {0} ({1} structures, {2} commodity requests)",
                        selectedColony.PlanetName,
                        selectedColony.Structures.Count,
                        selectedColony.Commodities.Count);
                    return;
                }

                var existingColony = ColonyImportHelper.FindByPlanet(
                    playerContext.GetCurrentPlayerColonies(), tempColony.PlanetName, tempColony.SystemName);

                Log.Info(
                    "Colony dedup: {0} for planet '{1}'",
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

                    Log.Info(
                        "Colony updated via dedup: {0} ({1} structures, {2} commodity requests)",
                        existingColony.ColonyName,
                        existingColony.Structures.Count,
                        existingColony.Commodities.Count);
                }
                else
                {
                    var newColony = ColonyImportHelper.CreateFromTemp(tempColony, playerContext.CurrentPlayerUUID);
                    playerContext.AddColony(newColony);
                    selectedColony = newColony;

                    Log.Info(
                        "New colony created via dedup: {0} ({1} structures, {2} commodity requests)",
                        newColony.ColonyName,
                        newColony.Structures.Count,
                        newColony.Commodities.Count);
                }

                playerContext.WriteContext();
                playerContext.OnColonyDataChanged(selectedColony.UUID);

                // Refresh list view
                TxtColonyFilter_TextChanged(sender, e);

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
                MessageBox.Show(
                    "Failed to import colony: " + ex.Message,
                    "Import Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void CmdImportClipboard_Click(object sender, EventArgs e)
        {
            if (!Clipboard.ContainsText(TextDataFormat.Html))
            {
                MessageBox.Show(
                    "No HTML content found on the clipboard.\n\nCopy colony data from the game browser first.",
                    "No HTML",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
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
                    MessageBox.Show(
                        "Clipboard HTML saved to:\n" + dlg.FileName,
                        "Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error saving clipboard HTML to {0}", dlg.FileName);
                    MessageBox.Show(
                        "Failed to save: " + ex.Message,
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (!_structureTypesPopulated)
            {
                _structureTypesPopulated = true;
                BuildStructureTypeList();
            }

            sw.Stop();
            Log.Info("PERF PopulateStructureTypeFilter: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Builds the lvwStructureTypes list from all flatpack blueprint types,
        /// checking/unchecking based on _uncheckedStructureTypes.
        /// </summary>
        private void BuildStructureTypeList()
        {
            lvwStructureTypes.ItemChecked -= LvwStructureTypes_ItemChecked;
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

            lvwStructureTypes.ItemChecked += LvwStructureTypes_ItemChecked;
        }

        /// <summary>
        /// Handler for structure type checkbox changes — updates the unchecked set
        /// and re-applies the filter.
        /// </summary>
        private void LvwStructureTypes_ItemChecked(object sender, ItemCheckedEventArgs e)
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
                string typeId = bp?.BluePrintType ?? string.Empty;
                ctrl.Visible = checkedTypes.Count == 0 || checkedTypes.Contains(typeId);
            }

            flpStructures.ResumeLayout();
        }

        // -------------------------------------------------------------------
        // Overflow Tab (task 38)
        // -------------------------------------------------------------------

        private void PopulateOverflowGrid()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvOverflowRules.Rows.Clear();
            if (selectedColony == null) return;

            PopulateOverflowResourceCombo();
            PopulateOverflowPurityCombo();
            PopulateOverflowDestCombo();
            PopulateOverflowRouteCombo();

            var rules = playerContext.WarehouseOverflowRuleList
                .Where(r => r.ColonyUUID == selectedColony.UUID)
                .ToList();

            foreach (var rule in rules)
            {
                string destName = ResolveOverflowDestName(rule.DestinationType, rule.DestinationUUID);
                string routeName = string.Empty;
                if (!string.IsNullOrEmpty(rule.DeliveryRouteUUID))
                {
                    var route = playerContext.DeliveryRouteList.FirstOrDefault(r => r.UUID == rule.DeliveryRouteUUID);
                    routeName = route?.Name ?? rule.DeliveryRouteUUID;
                }

                int currentQty = 0;
                if (selectedColony.Items != null)
                {
                    var matchingItems = selectedColony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
                    currentQty = matchingItems.Sum(i => i.Quantity);
                }

                int rowIdx = dgvOverflowRules.Rows.Add(
                    rule.ResourceName,
                    rule.ResourcePurity,
                    rule.TriggerThreshold.ToString(),
                    currentQty.ToString(),
                    destName,
                    routeName,
                    rule.IsActive);
                dgvOverflowRules.Rows[rowIdx].Tag = rule;

                var currentCell = dgvOverflowRules.Rows[rowIdx].Cells[colOverflowCurrent.Index];
                if (currentQty >= rule.TriggerThreshold && rule.TriggerThreshold > 0)
                    currentCell.Style.ForeColor = System.Drawing.Color.Red;
                else if (rule.TriggerThreshold > 0 && currentQty >= rule.TriggerThreshold * 0.8)
                    currentCell.Style.ForeColor = System.Drawing.Color.DarkGoldenrod;
                else
                    currentCell.Style.ForeColor = System.Drawing.Color.Green;

                if (!rule.IsActive)
                {
                    for (int c = 0; c < dgvOverflowRules.Columns.Count; c++)
                    {
                        if (c != colOverflowActive.Index)
                            dgvOverflowRules.Rows[rowIdx].Cells[c].Style.ForeColor = System.Drawing.Color.Gray;
                    }
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateOverflowGrid: {0}ms rules={1}", sw.ElapsedMilliseconds, rules.Count);
        }

        private string ResolveOverflowDestName(DestinationType destType, string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return string.Empty;
            switch (destType)
            {
                case DestinationType.Colony:
                    var col = playerContext.ColonyList.FirstOrDefault(c => c.UUID == uuid);
                    return col?.ColonyName ?? uuid;
                case DestinationType.Station:
                    var stn = playerContext.StationList.FirstOrDefault(s => s.UUID == uuid);
                    return stn?.Name ?? uuid;
                default:
                    return uuid;
            }
        }

        private void PopulateOverflowResourceCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOverflowResource.Items.Clear();
            var resources = empireContext?.ResourceList;
            if (resources != null)
            {
                string filter = txtOverflowResourceFilter.Text.Trim();
                var filtered = resources.OrderBy(r => r.Name).AsEnumerable();
                if (!string.IsNullOrEmpty(filter))
                    filtered = filtered.Where(r => r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                foreach (var r in filtered)
                    cmbOverflowResource.Items.Add(r.Name);
            }

            if (cmbOverflowResource.Items.Count > 0) cmbOverflowResource.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateOverflowResourceCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateOverflowPurityCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOverflowPurity.Items.Clear();
            foreach (var p in ResourcePurity.Purities)
            {
                if (p.ID != ResourcePurity.PurityEnum.None)
                    cmbOverflowPurity.Items.Add(p.Name);
            }

            if (cmbOverflowPurity.Items.Count > 0) cmbOverflowPurity.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateOverflowPurityCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateOverflowDestCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOverflowDest.DataSource = null;
            cmbOverflowDest.Items.Clear();
            if (cmbOverflowDestType.SelectedItem == null) return;
            var destType = (DestinationType)cmbOverflowDestType.SelectedItem;
            string filter = txtOverflowDestFilter.Text.Trim();
            var items = new List<KeyValuePair<string, string>>();
            switch (destType)
            {
                case DestinationType.Colony:
                    foreach (var c in playerContext.ColonyList.OrderBy(c => c.ColonyName))
                        if (string.IsNullOrEmpty(filter) || c.ColonyName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            items.Add(new KeyValuePair<string, string>(c.UUID, c.ColonyName));
                        }

                    break;
                case DestinationType.Station:
                    foreach (var s in playerContext.StationList.OrderBy(s => s.Name))
                        if (string.IsNullOrEmpty(filter) || s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            items.Add(new KeyValuePair<string, string>(s.UUID, s.Name));
                        }

                    break;
            }

            if (items.Count > 0)
            {
                cmbOverflowDest.DataSource = items;
                cmbOverflowDest.DisplayMember = "Value";
                cmbOverflowDest.ValueMember = "Key";
            }

            sw.Stop();
            Log.Info("PERF PopulateOverflowDestCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateOverflowRouteCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOverflowRoute.DataSource = null;
            cmbOverflowRoute.Items.Clear();
            string filter = txtOverflowRouteFilter.Text.Trim();
            var routes = playerContext.DeliveryRouteList.OrderBy(r => r.Name).ToList();
            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var r in routes)
                if (string.IsNullOrEmpty(filter) || r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    items.Add(new KeyValuePair<string, string>(r.UUID, r.Name));
                }

            cmbOverflowRoute.DataSource = items;
            cmbOverflowRoute.DisplayMember = "Value";
            cmbOverflowRoute.ValueMember = "Key";
            sw.Stop();
            Log.Info("PERF PopulateOverflowRouteCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbOverflowDestType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateOverflowDestCombo();
        }

        private void CmdAddOverflowRule_Click(object sender, EventArgs e)
        {
            if (selectedColony == null) return;
            string resource = cmbOverflowResource.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resource)) return;
            string purity = cmbOverflowPurity.SelectedItem?.ToString() ?? string.Empty;
            if (!int.TryParse(txtOverflowThreshold.Text.Trim(), out int threshold) || threshold <= 0)
            {
                MessageBox.Show(
                    "Enter a valid threshold.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var destType = cmbOverflowDestType.SelectedItem is DestinationType dt ? dt : DestinationType.Station;
            string destUUID = cmbOverflowDest.SelectedValue?.ToString() ?? string.Empty;
            string routeUUID = cmbOverflowRoute.SelectedValue?.ToString() ?? string.Empty;

            var existing = playerContext.WarehouseOverflowRuleList
                .FirstOrDefault(r => r.ColonyUUID == selectedColony.UUID &&
                    r.ResourceName == resource && r.ResourcePurity == purity);
            if (existing != null)
            {
                MessageBox.Show(
                    "A rule for this resource and purity already exists.",
                    "Duplicate",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var rule = new WarehouseOverflowRule
            {
                UUID = Guid.NewGuid().ToString(),
                OwnerUUID = playerContext.CurrentPlayerUUID ?? string.Empty,
                ColonyUUID = selectedColony.UUID,
                ResourceName = resource,
                ResourcePurity = purity,
                TriggerThreshold = threshold,
                DestinationType = destType,
                DestinationUUID = destUUID,
                DeliveryRouteUUID = routeUUID,
                IsActive = true
            };

            playerContext.AddWarehouseOverflowRule(rule);
            playerContext.WriteContext();
            PopulateOverflowGrid();
            Log.Info("Added overflow rule: {0} ({1}) threshold={2}", resource, purity, threshold);
        }

        private void CmdRemoveOverflowRule_Click(object sender, EventArgs e)
        {
            if (selectedColony == null || dgvOverflowRules.SelectedRows.Count == 0) return;
            var rule = dgvOverflowRules.SelectedRows[0].Tag as WarehouseOverflowRule;
            if (rule == null) return;
            playerContext.RemoveWarehouseOverflowRule(rule);
            playerContext.WriteContext();
            PopulateOverflowGrid();
            Log.Info("Removed overflow rule: {0} ({1})", rule.ResourceName, rule.ResourcePurity);
        }

        private void DgvOverflowRules_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvOverflowRules.IsCurrentCellDirty)
                dgvOverflowRules.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvOverflowRules_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || e.RowIndex < 0) return;
            if (e.ColumnIndex != colOverflowActive.Index) return;
            var rule = dgvOverflowRules.Rows[e.RowIndex].Tag as WarehouseOverflowRule;
            if (rule == null) return;
            var val = dgvOverflowRules.Rows[e.RowIndex].Cells[colOverflowActive.Index].Value;
            rule.IsActive = val is bool b && b;
            Log.Info("Overflow rule \"{0}\" IsActive={1}", rule.ResourceName, rule.IsActive);
            PopulateOverflowGrid();
        }
    }
}
