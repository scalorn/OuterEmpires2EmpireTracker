using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using NLog;
using OE2EmpireTracker.Client;
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
        private const int WmSetRedraw = 0x000B;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly EmpireContext empireContext;

        private readonly PlayerContext playerContext;

        // Structure_Pool (8.1)
        private readonly List<ColonyStructureV2> _pool = new List<ColonyStructureV2>();

        // Structure type filter (9.1)
        private readonly HashSet<string> _uncheckedStructureTypes = new HashSet<string>(StringComparer.Ordinal);

        private readonly ColonyService _colonyService;

        private int _isProgrammaticUpdate = 0;

        private string _selectedColonyUUID;

        // Tracks the previously selected colony UUID for unsaved-changes cancel/restore (9.1)
        private string _previousSelectedUUID;

        private ColonyViewModel _viewModel = new ColonyViewModel();

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

        private int _poolInUse = 0;

        // Background calculation cancellation (7.1)
        private CancellationTokenSource _calcCts;

        private int _calcGeneration = 0;

        private bool _structureTypesPopulated = false;

        private bool _syncCooldownActive = false;

        /// <summary>Parallel list of Blueprint objects matching cmbFlatpacks display items, for UUID lookup via SelectedFullIndex.</summary>
        private List<Models.Blueprint> _flatpackBlueprints = new List<Models.Blueprint>();

        /// <summary>Parallel list of objects matching cmbItem display items, for lookup via SelectedFullIndex.</summary>
        private List<object> _itemPickerObjects = new List<object>();

        /// <summary>Parallel list of destination UUIDs matching cmbOverflowDest display items.</summary>
        private List<string> _overflowDestUUIDs = new List<string>();

        /// <summary>Parallel list of route UUIDs matching cmbOverflowRoute display items.</summary>
        private List<string> _overflowRouteUUIDs = new List<string>();

        public FormColonyV2()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            _colonyService = new ColonyService(playerContext);

            _referenceCounter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList,
                playerContext.DeliveryPlanList,
                playerContext.BuildPlanList,
                playerContext.SupplyChainList,
                playerContext.WarehouseOverflowRuleList);

            // Configure colony list
            lvwColonies.Columns.Add("Planet", 80);
            lvwColonies.Columns.Add("Name", 80);
            lvwColonies.Columns.Add("Refs", 40);
            lvwColonies.ColumnClick += LvwColonies_ColumnClick;
            lvwColonies.ListViewItemSorter = new ListViewItemComparer(_sortColumn, _sortOrder);
            PopulateListView(playerContext.GetCurrentPlayerReadOnlyColonies());

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

            // Wire flatpack add button (8.3)
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
            cmbItem.SelectedItemChanged += CmbItem_SelectedItemChanged;
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

            // Wire sync button and timers (colony-sync-wiring, task 9)
            cmdSync.Click += CmdSync_Click;
            timerSyncCooldown.Tick += TimerSyncCooldown_Tick;
            timerSyncStatus.Tick += TimerSyncStatus_Tick;
            UpdateSyncButtonState();

            // Wire overflow tab handlers (task 38)
            cmbOverflowDestType.Items.Add(DestinationType.Colony);
            cmbOverflowDestType.Items.Add(DestinationType.Station);
            if (cmbOverflowDestType.Items.Count > 0) cmbOverflowDestType.SelectedIndex = 0;
            cmbOverflowDestType.SelectedIndexChanged += CmbOverflowDestType_SelectedIndexChanged;
            cmbOverflowResource.SelectedItemChanged += (s, ev) => { /* resource selection */ };
            cmbOverflowDest.SelectedItemChanged += (s, ev) => { /* dest selection */ };
            cmbOverflowRoute.SelectedItemChanged += (s, ev) => { /* route selection */ };
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

            // Wire context menu events (grid-context-menus spec, task 1.2)
            tsmiAddItem.Click += CmdAddItem_Click;
            tsmiRemoveItem.Click += TsmiRemoveItem_Click;
            tsmiAddCommodityRequest.Click += CmdAddCommodityRequest_Click;
            tsmiRemoveCommodityRequest.Click += TsmiRemoveCommodityRequest_Click;
            tsmiAddOverflowRule.Click += CmdAddOverflowRule_Click;
            tsmiRemoveOverflowRule.Click += CmdRemoveOverflowRule_Click;
            dgvItems.CellMouseClick += DgvItems_CellMouseClick;
            dgvCommodityRequests.CellMouseClick += DgvCommodityRequests_CellMouseClick;
            dgvOverflowRules.CellMouseClick += DgvOverflowRules_CellMouseClick;
            cmsItems.Opening += CmsItems_Opening;
            cmsCommodityRequests.Opening += CmsCommodityRequests_Opening;
            cmsOverflowRules.Opening += CmsOverflowRules_Opening;

            UpdateTitle();
        }

        private enum SyncStatus
        {
            Done,
            Error,
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        // -------------------------------------------------------------------
        // Event lifecycle
        // -------------------------------------------------------------------

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Prompt for unsaved changes before closing (9.4)
            Log.Debug("OnFormClosing: checking IsDirty={0} UUID={1}", _viewModel.IsDirty, _viewModel.UUID ?? "(null)");
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        SaveCurrentColony();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving during form close");
                        MessageBox.Show(
                            "Failed to save: " + ex.Message,
                            "Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        e.Cancel = true;
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // Cancel any in-progress background calculation
            _calcCts?.Cancel();

            // Persist the last-selected colony UUID so it can be restored on next open
            int windowNumber = Tag is int n ? n : 1;
            var store = PreferencesStore.GetInstance();
            string formTypeKey = GetType().Name;
            var ws = store.GetWindowState(formTypeKey, windowNumber);
            if (ws.FormState == null) ws.FormState = new Models.FormControlState();
            ws.FormState.FilterTexts["__selectedColonyUUID"] = _selectedColonyUUID ?? string.Empty;

            // Save window state including structure type filter (9.3)
            WindowStateHelper.SaveState(this, GetType().Name, windowNumber);

            timerAdminRefresh.Stop();
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }

        // WmSetRedraw: suppress all painting until re-enabled
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);

        private static void SuspendDrawing(Control control)
        {
            SendMessage(control.Handle, WmSetRedraw, false, 0);
        }

        private static void ResumeDrawing(Control control)
        {
            SendMessage(control.Handle, WmSetRedraw, true, 0);
            control.Refresh();
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
                        var bp = playerContext.FindBlueprint(item.BaseItemTypeID);
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
            _selectedColonyUUID = null;
            _previousSelectedUUID = null;
            _viewModel.Reset();
            _referenceCounter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList,
                playerContext.SupplyChainList, playerContext.WarehouseOverflowRuleList);
            PopulateListView(playerContext.GetCurrentPlayerReadOnlyColonies());
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

            // Refresh if the event is for the selected colony OR if it's a bulk update (empty UUID = all colonies changed)
            if (!string.IsNullOrEmpty(_selectedColonyUUID) &&
                (string.IsNullOrEmpty(e.ColonyUUID) || _selectedColonyUUID == e.ColonyUUID))
            {
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
            _viewModel.PlanetName = txtPlanetName.Text;
        }

        private void TxtColonyName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.ColonyName = txtColonyName.Text;
        }

        private void TxtSystemName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.SystemName = txtSystemName.Text;
        }

        // -------------------------------------------------------------------
        // Colony list population
        // -------------------------------------------------------------------

        private void PopulateListView(List<ReadOnlyColony> colonies)
        {
            if (colonies == null) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList,
                playerContext.SupplyChainList, playerContext.WarehouseOverflowRuleList);

            var viewableColonies = new Dictionary<string, ListViewItem>();
            foreach (ListViewItem item in lvwColonies.Items)
            {
                var col = item.Tag as ReadOnlyColony;
                if (col != null)
                    viewableColonies[col.UUID] = item;
            }

            foreach (ReadOnlyColony colony in colonies)
            {
                if (string.IsNullOrEmpty(colony.UUID))
                {
                    Log.Warn("Skipping colony with null/empty UUID (PlanetName={0})", colony.PlanetName);
                    continue;
                }

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
            var colonies = playerContext.GetCurrentPlayerReadOnlyColonies();
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
                _selectedColonyUUID = null;
                _previousSelectedUUID = null;
                _viewModel.Reset();
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
            {
                _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
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
                // Prompt for unsaved changes before switching (9.1)
                Log.Debug("LvwColonies_ItemSelectionChanged: checking IsDirty={0} UUID={1} prevUUID={2}", _viewModel.IsDirty, _viewModel.UUID ?? "(null)", _previousSelectedUUID ?? "(null)");
                if (_viewModel.IsDirty)
                {
                    var result = PromptUnsavedChanges();
                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            SaveCurrentColony();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error saving during unsaved changes prompt");
                            MessageBox.Show(
                                "Failed to save: " + ex.Message,
                                "Save Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);

                            // Cancel the selection change
                            lvwColonies.ItemSelectionChanged -= LvwColonies_ItemSelectionChanged;
                            lvwColonies.SelectedItems.Clear();
                            if (!string.IsNullOrEmpty(_previousSelectedUUID))
                            {
                                foreach (ListViewItem item in lvwColonies.Items)
                                {
                                    if ((item.Tag as ReadOnlyColony)?.UUID == _previousSelectedUUID)
                                    {
                                        item.Selected = true;
                                        item.EnsureVisible();
                                        break;
                                    }
                                }
                            }

                            lvwColonies.ItemSelectionChanged += LvwColonies_ItemSelectionChanged;
                            return;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        // Restore previous selection
                        lvwColonies.ItemSelectionChanged -= LvwColonies_ItemSelectionChanged;
                        lvwColonies.SelectedItems.Clear();
                        if (!string.IsNullOrEmpty(_previousSelectedUUID))
                        {
                            foreach (ListViewItem item in lvwColonies.Items)
                            {
                                if ((item.Tag as ReadOnlyColony)?.UUID == _previousSelectedUUID)
                                {
                                    item.Selected = true;
                                    item.EnsureVisible();
                                    break;
                                }
                            }
                        }

                        lvwColonies.ItemSelectionChanged += LvwColonies_ItemSelectionChanged;
                        return;
                    }

                    // DialogResult.No - discard, fall through to load new
                }

                // Cancel any in-progress background calculation
                _calcCts?.Cancel();
                _calcCts = new CancellationTokenSource();
                var cts = _calcCts;
                int generation = Interlocked.Increment(ref _calcGeneration);

                var ro = lvwColonies.SelectedItems[0].Tag as ReadOnlyColony;
                if (ro == null) return;

                _selectedColonyUUID = ro.UUID;
                _previousSelectedUUID = ro.UUID;
                _viewModel.LoadFrom(ro);

                Log.Debug(
                    "V2.LvwColonies_ItemSelectionChanged: colony={0} uuid={1}",
                    ro.ColonyName ?? ro.PlanetName ?? "(null)",
                    ro.UUID ?? "(null)");

                // Immediate: show identity fields on UI thread
                using (var guard = new ProgrammaticUpdateGuard(this))
                {
                    txtPlanetName.Text = _viewModel.PlanetName;
                    txtColonyName.Text = _viewModel.ColonyName;
                    txtSystemName.Text = _viewModel.SystemName;
                }

                // Show "Calculating..." indicator
                SetCalculatingState(true);

                // Queue expensive work on ThreadPool
                string colonyUUID = _selectedColonyUUID;
                var mutableColony = playerContext.FindMutableColony(colonyUUID);
                if (mutableColony == null)
                {
                    SetCalculatingState(false);
                    PopulateForm();
                    UpdateDeleteButtonState();
                    UpdateSyncButtonState();
                    UpdateTitle();
                    return;
                }

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    if (cts.IsCancellationRequested) return;

                    // Acquire write lock for RecalculateStatus (mutates Locks, Statuses)
                    if (!mutableColony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
                    {
                        Log.Warn("Background calc: write lock timeout on colony {0}", colonyUUID);
                        return;
                    }

                    try
                    {
                        if (cts.IsCancellationRequested) return;
                        var calc = new ColonyStatusCalculator(mutableColony);
                        calc.CalculateBuilt();
                        calc.CalculateIdeal();
                    }
                    finally
                    {
                        mutableColony.ColonyLock.ExitWriteLock();
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
                            UpdateSyncButtonState();
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

            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var pfSw = System.Diagnostics.Stopwatch.StartNew();

            Log.Debug("V2.PopulateForm: colony={0} uuid={1}", _viewModel.ColonyName ?? _viewModel.PlanetName ?? "(null)", _selectedColonyUUID);

            txtPlanetName.Text = _viewModel.PlanetName;
            txtColonyName.Text = _viewModel.ColonyName;
            txtSystemName.Text = _viewModel.SystemName;

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
        // Last-selected colony persistence
        // -------------------------------------------------------------------

        /// <summary>
        /// Retrieves the last-selected colony UUID from UI preferences.
        /// Returns null if no saved selection exists.
        /// </summary>
        private string GetSavedSelectedColonyUUID()
        {
            int windowNumber = Tag is int n ? n : 1;
            var store = PreferencesStore.GetInstance();
            string formTypeKey = GetType().Name;
            string stateKey = windowNumber.ToString();

            if (store.Preferences.Forms.TryGetValue(formTypeKey, out var windows) &&
                windows.TryGetValue(stateKey, out var windowState) &&
                windowState.FormState?.FilterTexts != null &&
                windowState.FormState.FilterTexts.TryGetValue("__selectedColonyUUID", out var uuid) &&
                !string.IsNullOrEmpty(uuid))
            {
                return uuid;
            }

            return null;
        }

        // -------------------------------------------------------------------
        // Unsaved changes helpers (9.1-9.4)
        // -------------------------------------------------------------------

        /// <summary>
        /// Prompts the user to save, discard, or cancel when there are unsaved changes.
        /// Returns Yes (save), No (discard), or Cancel.
        /// </summary>
        private DialogResult PromptUnsavedChanges()
        {
            Log.Info(
                "PromptUnsavedChanges: IsDirty={0} IsNew={1} UUID={2} PlanetName='{3}' ColonyName='{4}' SystemName='{5}' OrigPlanet='{6}' OrigColony='{7}' OrigSystem='{8}'",
                _viewModel.IsDirty,
                _viewModel.IsNew,
                _viewModel.UUID ?? "(null)",
                _viewModel.PlanetName ?? "(null)",
                _viewModel.ColonyName ?? "(null)",
                _viewModel.SystemName ?? "(null)",
                _viewModel.Original?.PlanetName ?? "(null)",
                _viewModel.Original?.ColonyName ?? "(null)",
                _viewModel.Original?.SystemName ?? "(null)");

            var result = MessageBox.Show(
                string.Format("Save changes to '{0}'?", _viewModel.PlanetName),
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            Log.Info("PromptUnsavedChanges: user chose {0}", result);
            return result;
        }

        /// <summary>
        /// Saves the current colony via the service (Create or Update) and reloads the ViewModel.
        /// </summary>
        private void SaveCurrentColony()
        {
            Log.Info(
                "SaveCurrentColony: IsNew={0} UUID={1} PlanetName='{2}' ColonyName='{3}' SystemName='{4}'",
                _viewModel.IsNew,
                _viewModel.UUID ?? "(null)",
                _viewModel.PlanetName ?? "(null)",
                _viewModel.ColonyName ?? "(null)",
                _viewModel.SystemName ?? "(null)");

            using var guard = new ProgrammaticUpdateGuard(this);

            ReadOnlyColony saved;
            if (_viewModel.IsNew)
            {
                saved = _colonyService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _colonyService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _selectedColonyUUID = saved.UUID;
            _previousSelectedUUID = saved.UUID;
            _viewModel.LoadFrom(saved);

            // Refresh list with current filter
            TxtColonyFilter_TextChanged(this, EventArgs.Empty);
            UpdateTitle();
        }

        // -------------------------------------------------------------------
        // CRUD operations (6.9)
        // -------------------------------------------------------------------

        private void CmdNew_Click(object sender, EventArgs e)
        {
            // Prompt for unsaved changes before clearing (9.2)
            Log.Debug("CmdNew_Click: checking IsDirty={0} UUID={1}", _viewModel.IsDirty, _viewModel.UUID ?? "(null)");
            if (_viewModel.IsDirty)
            {
                var result = PromptUnsavedChanges();
                if (result == DialogResult.Yes)
                {
                    try
                    {
                        SaveCurrentColony();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving during unsaved changes prompt");
                        MessageBox.Show(
                            "Failed to save: " + ex.Message,
                            "Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            using var guard = new ProgrammaticUpdateGuard(this);
            _selectedColonyUUID = null;
            _previousSelectedUUID = null;
            _viewModel.Reset();
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

            ReadOnlyColony saved;
            if (_viewModel.IsNew)
            {
                saved = _colonyService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                saved = _colonyService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _selectedColonyUUID = saved.UUID;
            _previousSelectedUUID = saved.UUID;
            _viewModel.LoadFrom(saved);

            // Refresh list with current filter
            TxtColonyFilter_TextChanged(sender, e);
            UpdateTitle();
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList,
                playerContext.SupplyChainList, playerContext.WarehouseOverflowRuleList);
            var report = counter.CountReferences(_selectedColonyUUID);
            if (report.TotalCount > 0)
            {
                var msg = $"Cannot delete '{_viewModel.PlanetName}' -- it is referenced by {report.RouteCount} route(s), {report.PlanCount} plan(s), {report.BuildItemCount} build item(s), {report.SupplyChainCount} supply chain(s), and {report.OverflowCount} overflow rule(s).";
                MessageBox.Show(msg, "Colony In Use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Delete colony '{_viewModel.PlanetName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _colonyService.Delete(_selectedColonyUUID);

            using var guard = new ProgrammaticUpdateGuard(this);
            _selectedColonyUUID = null;
            _previousSelectedUUID = null;
            _viewModel.Reset();
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
            if (string.IsNullOrEmpty(_selectedColonyUUID))
            {
                cmdDelete.Enabled = false;
                cmdDelete.Text = "Delete";
                return;
            }

            var counter = new ColonyReferenceCounter(
                playerContext.DeliveryRouteList, playerContext.DeliveryPlanList, playerContext.BuildPlanList,
                playerContext.SupplyChainList, playerContext.WarehouseOverflowRuleList);
            var report = counter.CountReferences(_selectedColonyUUID);
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
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null) return;

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
                colony.Structures?.Count ?? 0,
                checkedTypes.Count > 0,
                checkedTypes.Count > 0 ? string.Join(", ", checkedTypes) : "(none)");

            flpStructures.SuspendLayout();

            // Snapshot structure view models under read lock
            IReadOnlyList<ColonyStructureViewModel> structureVMs;
            if (!colony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateStructures: read lock timeout on colony {0}, using stale data", _selectedColonyUUID);
                structureVMs = CollectionSortHelper.OrderStructures(colony.Structures)
                    .Select(s => new ColonyStructureViewModel(s, playerContext))
                    .ToList();
            }
            else
            {
                try
                {
                    structureVMs = CollectionSortHelper.OrderStructures(colony.Structures)
                        .Select(s => new ColonyStructureViewModel(s, playerContext))
                        .ToList();
                }
                finally
                {
                    colony.ColonyLock.ExitReadLock();
                }
            }

            // Structures are already sorted by BuildQueueSequence via
            // ColonyViewModel.StructureViewModels â†’ CollectionSortHelper.OrderStructures().
            // No additional sort needed here.

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
                    ctrl.Colony = colony;
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
            var filteredList = new List<Models.Blueprint>(playerContext.GetAllBlueprints());

            filteredList = filteredList
                .Where(item => item.BluePrintType != null && item.BluePrintType.IsFlatpack())
                .ToList();

            filteredList = CollectionSortHelper.OrderBlueprints(filteredList).ToList();
            filteredList.Insert(0, new Models.Blueprint());

            _flatpackBlueprints = filteredList;
            var names = filteredList.Select(b => b.ExtendedName ?? string.Empty).ToList();
            cmbFlatpacks.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateFlatpackCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmdAddFlatpack_Click(object sender, EventArgs e)
        {
            int idx = cmbFlatpacks.SelectedFullIndex;
            if (idx < 0 || idx >= _flatpackBlueprints.Count) return;
            string uuid = _flatpackBlueprints[idx].UUID;
            if (string.IsNullOrEmpty(uuid)) return;

            Log.Debug("V2.CmdAddFlatpack_Click: blueprintUUID={0}", uuid);

            _colonyService.AddStructure(_selectedColonyUUID, uuid);
            PopulateStructures();
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

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null) return;

            if (!colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("Structures_ColonyStructureDataChanged: write lock timeout on colony {0}", _selectedColonyUUID);
                return;
            }

            try
            {
                if (e.IsStructural)
                {
                    // 8.4: Structural change â€” full rebuild
                    var calc = new ColonyStatusCalculator(colony);
                    calc.CalculateBuilt();
                    calc.CalculateIdeal();
                }
                else
                {
                    // 8.5: Non-structural change â€” O(1) delta update
                    if (ctrl?.ViewModel != null)
                    {
                        var calc = new ColonyStatusCalculator(colony);
                        calc.RecalculateStructure(ctrl.ViewModel.Data);
                    }
                }
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            if (e.IsStructural)
            {
                PopulateStructures();

                // Scroll to the moved structure after rebuild (MoveUp/MoveDown)
                if (!string.IsNullOrEmpty(e.ScrollToStructureUUID))
                {
                    for (int i = 0; i < _poolInUse; i++)
                    {
                        var ctrl2 = _pool[i];
                        if (ctrl2.Visible && ctrl2.ViewModel?.Data?.UUID == e.ScrollToStructureUUID)
                        {
                            flpStructures.ScrollControlIntoView(ctrl2);
                            ctrl2.Focus();
                            break;
                        }
                    }
                }
            }
            else
            {
                ctrl?.UpdateBackgroundColor();
            }

            RefreshStatusSummary();
            UpdateTabWarnings();
            _warehouseDirty = true;

            // Save context and fire event OUTSIDE the lock
            if (!string.IsNullOrEmpty(_selectedColonyUUID) && !string.IsNullOrEmpty(_selectedColonyUUID))
            {
                playerContext.OnColonyDataChanged(_selectedColonyUUID);
                playerContext.WriteContext();
            }
        }

        // -------------------------------------------------------------------
        // Status summary (8.6)
        // -------------------------------------------------------------------

        private void RefreshStatusSummary()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            var calc = colony != null ? new ColonyStatusCalculator(colony) : null;
            var status = calc?.FinalActualStatus;
            if (status == null)
            {
                rtbStatusSummary.Text = string.Empty;
                return;
            }

            var builder = new RtfBuilder();
            RtfBuilder.AppendColonyStatus(builder, status);
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

            // WindowStateHelper.RestoreState restores ALL TextBox values including
            // identity fields (txtPlanetName, txtColonyName, txtSystemName). Those
            // write through to the ViewModel via TextChanged handlers, making it
            // appear dirty before any colony is loaded. Reset the ViewModel and
            // clear the identity fields so the first selection doesn't trigger a
            // spurious "save changes?" prompt.
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                _viewModel.Reset();
                txtPlanetName.Text = string.Empty;
                txtColonyName.Text = string.Empty;
                txtSystemName.Text = string.Empty;
            }

            // Restore last-selected colony from preferences, or fall back to first item
            // Clear any restored filter first so the full list is visible
            if (!string.IsNullOrEmpty(txtColonyFilter.Text))
            {
                using (var guard2 = new ProgrammaticUpdateGuard(this))
                {
                    txtColonyFilter.Text = string.Empty;
                }

                TxtColonyFilter_TextChanged(this, EventArgs.Empty);
            }

            string savedUUID = GetSavedSelectedColonyUUID();
            bool restored = false;
            if (!string.IsNullOrEmpty(savedUUID))
            {
                foreach (ListViewItem item in lvwColonies.Items)
                {
                    if ((item.Tag as ReadOnlyColony)?.UUID == savedUUID)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        restored = true;
                        break;
                    }
                }
            }

            if (!restored && lvwColonies.SelectedItems.Count == 0 && lvwColonies.Items.Count > 0)
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
            int midX = rect.X + (rect.Width / 2);
            int midY = rect.Y + (rect.Height / 2);
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
        // Administration tab â€” Admin Report (11.1)
        // -------------------------------------------------------------------

        private void RefreshAdminReport()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (string.IsNullOrEmpty(_selectedColonyUUID) || string.IsNullOrEmpty(_selectedColonyUUID))
            {
                rtbAdminReport.Rtf = string.Empty;
                return;
            }

            var arSw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Acquire read lock to ensure consistent colony data for report
                if (!colony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
                {
                    Log.Warn("RefreshAdminReport: read lock timeout on colony {0}, using stale data", _selectedColonyUUID);
                }
                else
                {
                    try
                    {
                        // Lock held just to ensure consistent read â€” BuildReport reads colony data
                    }
                    finally
                    {
                        colony.ColonyLock.ExitReadLock();
                    }
                }

                string rtf = ColonyAdminReportBuilder.BuildReport(colony, playerContext);
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
        // Sync button — UI state and timers (colony-sync-wiring, task 9)
        // -------------------------------------------------------------------

        private void UpdateSyncButtonState()
        {
            bool colonySelected = !string.IsNullOrEmpty(_selectedColonyUUID);
            bool apiAvailable = GameApiContext.Instance != null;
            bool hasColonyId = false;

            if (colonySelected)
            {
                var colony = playerContext.FindMutableColony(_selectedColonyUUID);
                hasColonyId = colony != null && colony.ColonyId != 0;
            }

            cmdSync.Enabled = colonySelected && apiAvailable && hasColonyId && !_syncCooldownActive;
        }

        private async void CmdSync_Click(object sender, EventArgs e)
        {
            await SyncSelectedColonyAsync().ConfigureAwait(true);
        }

        private async Task SyncSelectedColonyAsync()
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID))
            {
                return;
            }

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null)
            {
                return;
            }

            if (colony.ColonyId == 0)
            {
                MessageBox.Show(
                    "This colony has not been synced from the API yet.",
                    "Sync Not Available",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var gameApi = GameApiContext.Instance;
            if (gameApi == null)
            {
                return;
            }

            // Disable button and show syncing status
            cmdSync.Enabled = false;
            cmdSync.Text = "Syncing...";

            string colonyUUID = _selectedColonyUUID;
            int colonyId = colony.ColonyId;
            string ownerUUID = colony.OwnerUUID;

            try
            {
                var result = await Task.Run(() => ExecuteSyncAsync(gameApi, colonyId, ownerUUID, colonyUUID)).ConfigureAwait(true);
                ApplySyncResult(result, colonyUUID);
            }
            catch (ObjectDisposedException)
            {
                // Form closed during sync — discard
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manual colony sync failed for colony {0}", colonyUUID);
                try
                {
                    ShowSyncStatus("Error");
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        private void TimerSyncCooldown_Tick(object sender, EventArgs e)
        {
            timerSyncCooldown.Stop();
            _syncCooldownActive = false;
            UpdateSyncButtonState();
        }

        private void TimerSyncStatus_Tick(object sender, EventArgs e)
        {
            timerSyncStatus.Stop();
            cmdSync.Text = "Sync";
        }

        private async Task<SyncResult> ExecuteSyncAsync(GameApiContext gameApi, int colonyId, string ownerUUID, string colonyUUID)
        {
            var settings = PreferencesStore.GetInstance().Preferences.GameApiConnection;
            string appId = settings.AppId;
            string clientId = settings.ClientId;

            // Get secret for the colony owner
            SecureString secureSecret = gameApi.CredentialManager.GetKey(ownerUUID);
            if (secureSecret == null)
            {
                Log.Warn("Manual sync: no secret found for owner {0}", ownerUUID);
                return new SyncResult { Status = SyncStatus.Error };
            }

            string secret = CredentialStore.SecureStringToString(secureSecret);
            secureSecret.Dispose();

            // Exchange token
            var tokenResult = await gameApi.Client.ExchangeTokenAsync(appId, clientId, secret).ConfigureAwait(false);
            if (!tokenResult.Success)
            {
                if (tokenResult.ErrorMessage != null && tokenResult.ErrorMessage.Contains("401"))
                {
                    Log.Warn("Manual sync: token exchange failed (HTTP 401) for colony {0}", colonyId);
                    return new SyncResult { Status = SyncStatus.Error, TransitionToInvalidKey = true };
                }

                Log.Warn("Manual sync: token exchange failed for colony {0}: {1}", colonyId, tokenResult.ErrorMessage);
                return new SyncResult { Status = SyncStatus.Error };
            }

            string accessToken = tokenResult.Token.AccessToken;
            bool anyChanges = false;
            bool buildingsSucceeded = false;
            bool warehouseSucceeded = false;

            // Fetch buildings
            var buildingsResult = await gameApi.Client.GetColonyBuildingsAsync(appId, accessToken, colonyId).ConfigureAwait(false);
            if (buildingsResult.Success)
            {
                try
                {
                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyBuildingsResponse>>(buildingsResult.Json);
                    if (envelope?.Data?.Buildings != null)
                    {
                        var colony = playerContext.FindMutableColony(colonyUUID);
                        if (colony != null)
                        {
                            bool changed = ColonyMergeService.MergeBuildings(envelope.Data.Buildings, colony);
                            if (changed)
                            {
                                anyChanges = true;
                            }

                            buildingsSucceeded = true;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Manual sync: malformed buildings JSON for colony {0}", colonyId);
                }
            }
            else if (buildingsResult.Json == "401")
            {
                Log.Warn("Manual sync: buildings fetch returned 401 for colony {0}", colonyId);
                return new SyncResult { Status = SyncStatus.Error, AnyChanges = anyChanges, TransitionToInvalidKey = true };
            }
            else if (buildingsResult.Json == "403")
            {
                Log.Warn("Manual sync: buildings scope not granted for colony {0}", colonyId);
                return new SyncResult { Status = SyncStatus.Error, AnyChanges = anyChanges };
            }
            else if (buildingsResult.Json == "404")
            {
                Log.Warn("Manual sync: colony {0} not found on server", colonyId);
                return new SyncResult { Status = SyncStatus.Error, AnyChanges = anyChanges };
            }

            // Fetch warehouse
            var warehouseResult = await gameApi.Client.GetColonyWarehouseAsync(appId, accessToken, colonyId).ConfigureAwait(false);
            if (warehouseResult.Success)
            {
                try
                {
                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWarehouseResponse>>(warehouseResult.Json);
                    if (envelope?.Data?.Contents != null)
                    {
                        var colony = playerContext.FindMutableColony(colonyUUID);
                        if (colony != null)
                        {
                            bool changed = ColonyMergeService.MergeWarehouse(envelope.Data.Contents, colony);
                            if (changed)
                            {
                                anyChanges = true;
                            }

                            warehouseSucceeded = true;
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Manual sync: malformed warehouse JSON for colony {0}", colonyId);
                }
            }
            else
            {
                Log.Warn("Manual sync: warehouse fetch failed for colony {0} (status={1})", colonyId, warehouseResult.Json);
            }

            // Fetch workers
            var workersResult = await gameApi.Client.GetColonyWorkersAsync(appId, accessToken, colonyId).ConfigureAwait(false);
            if (workersResult.Success)
            {
                try
                {
                    var envelope = JsonConvert.DeserializeObject<GameApiServiceResponse<GameApiColonyWorkersResponse>>(workersResult.Json);
                    if (envelope?.Data != null)
                    {
                        var colony = playerContext.FindMutableColony(colonyUUID);
                        if (colony != null)
                        {
                            bool changed = ColonyMergeService.MergeWorkers(envelope.Data, colony);
                            if (changed)
                            {
                                anyChanges = true;
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Log.Error(ex, "Manual sync: malformed workers JSON for colony {0}", colonyId);
                }
            }
            else
            {
                Log.Warn("Manual sync: workers fetch failed for colony {0} (status={1})", colonyId, workersResult.Json);
            }

            // Persist if any changes occurred (partial success: persist whatever succeeded)
            if (anyChanges)
            {
                playerContext.WriteContext();
                playerContext.OnColonyDataChanged(colonyUUID);
            }

            bool allSucceeded = buildingsSucceeded && warehouseSucceeded;
            return new SyncResult
            {
                Status = allSucceeded ? SyncStatus.Done : (buildingsSucceeded ? SyncStatus.Done : SyncStatus.Error),
                AnyChanges = anyChanges,
            };
        }

        private void ApplySyncResult(SyncResult result, string colonyUUID)
        {
            if (IsDisposed)
            {
                return;
            }

            if (result.TransitionToInvalidKey)
            {
                var gameApi = GameApiContext.Instance;
                gameApi?.ConnectionMonitor.TransitionTo(
                    GameApiConnectionMonitor.ConnectionState.DisconnectedInvalidKey,
                    "Credentials are invalid (HTTP 401)");
            }

            string statusText = result.Status == SyncStatus.Done ? "Done" : "Error";
            ShowSyncStatus(statusText);
        }

        private void ShowSyncStatus(string statusText)
        {
            cmdSync.Text = statusText;
            _syncCooldownActive = true;
            timerSyncCooldown.Start();
            timerSyncStatus.Start();
        }

        // -------------------------------------------------------------------
        // Administration tab â€” Bootstrap / Optimize (11.2, 11.3)
        // -------------------------------------------------------------------

        private void CmdBootstrap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null) return;
            if (string.IsNullOrEmpty(colony.PlanetName))
            {
                MessageBox.Show(
                    "Set a planet name before bootstrapping.",
                    "No Planet",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdBootstrap_Click: write lock timeout on colony {0}", _selectedColonyUUID);
                return;
            }

            try
            {
                var bootstrap = new ColonyBootstrap(playerContext);
                bootstrap.Bootstrap(colony);

                // Refresh via structural change pattern
                var calc = new ColonyStatusCalculator(colony);
                calc.CalculateBuilt();
                calc.CalculateIdeal();
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            playerContext.WriteContext();
            playerContext.OnColonyDataChanged(_selectedColonyUUID);
            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();
        }

        private void CmdOptimize_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null) return;

            Log.Info(
                "CmdOptimize_Click: colony={0} structureCount={1} filterActive={2}",
                colony.ColonyName ?? colony.PlanetName,
                colony.Structures?.Count ?? 0,
                lvwStructureTypes.CheckedItems.Count > 0);

            if (!colony.ColonyLock.TryEnterWriteLock(Models.Colony.WriteLockTimeoutMs))
            {
                Log.Warn("CmdOptimize_Click: write lock timeout on colony {0}", _selectedColonyUUID);
                return;
            }

            try
            {
                var optimizer = new BuildOrderOptimizer(playerContext);
                var optimized = optimizer.Optimize(colony);

                Log.Info(
                    "CmdOptimize_Click: optimizer returned {0} structures (input was {1})",
                    optimized.Count,
                    colony.Structures.Count);

                colony.Structures.Clear();
                colony.Structures.AddRange(optimized);
                colony.StampBuildQueueSequence();

                Log.Info(
                    "CmdOptimize_Click: colony now has {0} structures after replace",
                    colony.Structures.Count);

                // Refresh via structural change pattern
                var calc = new ColonyStatusCalculator(colony);
                calc.CalculateBuilt();
                calc.CalculateIdeal();
            }
            finally
            {
                colony.ColonyLock.ExitWriteLock();
            }

            playerContext.WriteContext();
            playerContext.OnColonyDataChanged(_selectedColonyUUID);
            PopulateStructures();
            RefreshStatusSummary();
            UpdateTabWarnings();
        }

        // -------------------------------------------------------------------
        // Generate Build Plan (11.1, 11.2, 11.3)
        // -------------------------------------------------------------------

        private void CmdGenerateBuildPlan_Click(object sender, EventArgs e)
        {
            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (string.IsNullOrEmpty(_selectedColonyUUID) || string.IsNullOrEmpty(_selectedColonyUUID))
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
                colony.ColonyName ?? colony.PlanetName,
                _selectedColonyUUID);

            // Show plan picker dialog
            BuildPlan targetPlan = ShowBuildPlanPickerDialog();
            if (targetPlan == null) return;

            int added = BuildPlanService.GenerateColonyBuildItems(
                colony, targetPlan, playerContext.FindBlueprint);

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
                        _viewModel.ColonyName ?? _viewModel.PlanetName ?? "Colony");
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
            int structureCount = playerContext.FindMutableColony(_selectedColonyUUID)?.Structures?.Count ?? 0;
            ApplyTabWarning(
                tabPStructures,
                TabWarningService.EvaluateStructureWarning(structureCount));

            ApplyTabWarning(
                tabPWorkers,
                TabWarningService.EvaluateWorkerWarning(playerContext.FindMutableColony(_selectedColonyUUID)?.Commodities, SystemClock.UtcNow));

            ApplyTabWarning(
                tabPAdministration,
                TabWarningService.EvaluateColonyImportStalenessWarning(playerContext.FindMutableColony(_selectedColonyUUID)?.LastImportDateTime, SystemClock.UtcNow));

            UpdateWorkerTabTitle();
            UpdateStructuresTabTitle();
        }

        // -------------------------------------------------------------------
        // Workers Tab â€” Commodity Requests (19.1-19.8)
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

            _colonyService.AddCommodityRequest(_selectedColonyUUID, commodity.Name, qty, needBy);
            PopulateCommodityRequestGrid();
            UpdateTabWarnings();
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
            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
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
            int cleaned = 0;
            if (colony != null)
            {
                var now = SystemClock.UtcNow;
                var expired = colony.Commodities
                    .Where(cr => cr.Fulfilled && cr.NeedBy != DateTime.MinValue && (now - cr.NeedBy).TotalDays > 3)
                    .ToList();
                foreach (var cr in expired)
                    colony.Commodities.Remove(cr);
                cleaned = expired.Count;
            }

            if (cleaned > 0)
            {
                Log.Debug("Cleaned up {0} expired commodity requests", cleaned);
                if (!string.IsNullOrEmpty(_selectedColonyUUID) && !string.IsNullOrEmpty(_selectedColonyUUID))
                    playerContext.WriteContext();
            }

            // Snapshot commodity requests under read lock
            IReadOnlyList<CommodityRequested> requests;
            if (!string.IsNullOrEmpty(_selectedColonyUUID) && !colony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateCommodityRequestGrid: read lock timeout on colony {0}, using stale data", _selectedColonyUUID);
                requests = colony?.Commodities ?? new List<CommodityRequested>();
            }
            else
            {
                try
                {
                    requests = new List<CommodityRequested>(colony?.Commodities ?? new List<CommodityRequested>());
                }
                finally
                {
                    colony?.ColonyLock.ExitReadLock();
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
                {
                    _colonyService.UpdateCommodityRequest(_selectedColonyUUID, request.Name, value, request.Delivered, request.NeedBy, request.Fulfilled);
                }
            }

            // Column 2 = Fulfilled (checkbox) (19.4)
            else if (e.ColumnIndex == 2)
            {
                bool fulfilled = row.Cells[2].Value is bool b && b;
                int delivered = fulfilled ? request.Requested : 0;
                _colonyService.UpdateCommodityRequest(_selectedColonyUUID, request.Name, request.Requested, delivered, request.NeedBy, fulfilled);
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
                    _colonyService.UpdateCommodityRequest(_selectedColonyUUID, request.Name, request.Requested, request.Delivered, parsed.Value, request.Fulfilled);
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
                    _colonyService.RemoveCommodityRequest(_selectedColonyUUID, request.Name);
            }

            PopulateCommodityRequestGrid();
            UpdateTabWarnings();

            e.Handled = true;
        }

        // -------------------------------------------------------------------
        // Workers Tab â€” Tab Title and Countdown Helpers (19.7)
        // -------------------------------------------------------------------

        private void UpdateWorkerTabTitle()
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID))
            {
                tabPWorkers.Text = "Workers";
                return;
            }

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null)
            {
                tabPWorkers.Text = "Workers";
                return;
            }

            var now = SystemClock.UtcNow;
            int activeCount = colony.Commodities
                .Count(r => !r.Fulfilled && (r.NeedBy == DateTime.MinValue || r.NeedBy > now));
            tabPWorkers.Text = activeCount > 0 ? $"Workers : {activeCount}" : "Workers";
        }

        private void UpdateStructuresTabTitle()
        {
            int structureCount = playerContext.FindMutableColony(_selectedColonyUUID)?.Structures?.Count ?? 0;
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

            long totalSeconds = ((((long)days * 24) + hours) * 3600) + (minutes * 60) + seconds;
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
        // Warehousing Tab â€” Item Grid and Add Controls (20.1-20.6)
        // -------------------------------------------------------------------

        private void PopulateItemGrid()
        {
            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
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
            if (!string.IsNullOrEmpty(_selectedColonyUUID) && !colony.ColonyLock.TryEnterReadLock(Models.Colony.ReadLockTimeoutMs))
            {
                Log.Warn("PopulateItemGrid: read lock timeout on colony {0}, using stale data", _selectedColonyUUID);
                itemsSnapshot = colony != null ? new List<KeyValuePair<string, Item>>(colony.Items.Items) : new List<KeyValuePair<string, Item>>();
            }
            else
            {
                try
                {
                    itemsSnapshot = colony != null ? new List<KeyValuePair<string, Item>>(colony.Items.Items) : new List<KeyValuePair<string, Item>>();
                }
                finally
                {
                    colony?.ColonyLock.ExitReadLock();
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

                int lockedQty = colony?.Locks != null
                    ? colony.Locks.GetLockedQuantity(itemValue.ItemType, GetLockKey(itemValue))
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
        /// Returns the lock key for an item. Resources include purity in the key
        /// (e.g. "Iron|Refined") because manufacturing only locks refined resources.
        /// Other item types use BaseItemTypeID directly.
        /// </summary>
        private string GetLockKey(Item item)
        {
            if (item.ItemType == ItemType.ItemTypeEnum.Resource && !string.IsNullOrEmpty(item.ResourcePurity))
            {
                return item.BaseItemTypeID + "|" + item.ResourcePurity;
            }

            return item.BaseItemTypeID;
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
                var colony2 = playerContext.FindMutableColony(_selectedColonyUUID);
                int lockedQty = colony2?.Locks != null
                    ? colony2.Locks.GetLockedQuantity(item.ItemType, GetLockKey(item))
                    : 0;
                row.Cells[2].Value = lockedQty;
            }

            sw.Stop();
            Log.Info("PERF RefreshItemGridLocks: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Warehousing â€” Item Type Combo (20.2)
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

        private void CmbItem_SelectedItemChanged(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            cmbPurity.Visible = false;
            if (itemType != null && itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
            {
                int idx = cmbItem.SelectedFullIndex;
                Resource resource = (idx >= 0 && idx < _itemPickerObjects.Count) ? _itemPickerObjects[idx] as Resource : null;
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

        private void PopulateItemWithResources()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sortedList = new List<Resource>(CollectionSortHelper.OrderByName(Models.Resource.Resources, r => r.Name));
            sortedList.Insert(0, new Resource());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(r => r.Name ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithResources: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithCommodities()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sortedList = new List<Commodity>(CollectionSortHelper.OrderByName(Models.Commodity.Commodities, c => c.ExtendedName));
            sortedList.Insert(0, new Commodity());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(c => c.ExtendedName ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithCommodities: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithWorkerDetails()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sortedList = new List<WorkerDetail>(CollectionSortHelper.OrderByName(Models.WorkerDetail.WorkerDetails, w => w.Name));
            sortedList.Insert(0, new WorkerDetail());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(w => w.Name ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithWorkerDetails: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithSurveys()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sortedList = CollectionSortHelper.OrderSurveys(playerContext.SurveyList).ToList();
            sortedList.Insert(0, new Models.Survey());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(s => s.ExtendedName ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithSurveys: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithBlueprints()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sortedList = new List<Models.Blueprint>(CollectionSortHelper.OrderBlueprints(playerContext.GetAllBlueprints()));
            sortedList.Insert(0, new Models.Blueprint());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(b => b.ExtendedName ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithBlueprints: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateItemWithBlueprintsByOutputType(Models.ItemType.ItemTypeEnum outputType)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string outputTypeName = outputType.ToString();

            List<Models.Blueprint> filteredList = new List<Models.Blueprint>();
            foreach (Models.Blueprint bp in playerContext.GetAllBlueprints())
            {
                if (bp.UUID == null) continue;
                BlueprintType bpType = empireContext.FindBlueprintType(bp.BluePrintType);
                if (bpType == null || bpType.OutputItemType != outputTypeName) continue;
                filteredList.Add(bp);
            }

            var sortedList = new List<Models.Blueprint>(CollectionSortHelper.OrderBlueprints(filteredList));
            sortedList.Insert(0, new Models.Blueprint());

            _itemPickerObjects = sortedList.Cast<object>().ToList();
            var names = sortedList.Select(b => b.ExtendedName ?? string.Empty).ToList();
            cmbItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateItemWithBlueprintsByOutputType: {0}ms", sw.ElapsedMilliseconds);
        }

        // -------------------------------------------------------------------
        // Warehousing â€” Add Item (20.3)
        // -------------------------------------------------------------------

        private void CmdAddItem_Click(object sender, EventArgs e)
        {
            Models.ItemType itemType = cmbItemType.SelectedItem as Models.ItemType;
            if (itemType == null || itemType.ID == Models.ItemType.ItemTypeEnum.None) return;

            Models.Item item = new Models.Item() { UUID = Guid.NewGuid().ToString() };
            item.ItemType = itemType.ID;

            int idx = cmbItem.SelectedFullIndex;
            object selectedObj = (idx >= 0 && idx < _itemPickerObjects.Count) ? _itemPickerObjects[idx] : null;

            if (itemType.ID == Models.ItemType.ItemTypeEnum.Resource)
            {
                Models.Resource resource = selectedObj as Models.Resource;
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
                Models.Commodity commodity = selectedObj as Models.Commodity;
                if (commodity != null)
                {
                    item.BaseItemTypeID = commodity.Name;
                    item.Name = commodity.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.WorkDetail)
            {
                Models.WorkerDetail workerDetail = selectedObj as Models.WorkerDetail;
                if (workerDetail != null)
                {
                    item.BaseItemTypeID = workerDetail.ID;
                    item.Name = workerDetail.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Survey)
            {
                Models.Survey survey = selectedObj as Models.Survey;
                if (survey != null)
                {
                    item.BaseItemTypeID = survey.UUID;
                    item.Name = survey.Name;
                }
            }
            else if (itemType.ID == Models.ItemType.ItemTypeEnum.Blueprint)
            {
                Models.Blueprint blueprint = selectedObj as Models.Blueprint;
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
                Models.Blueprint blueprint = selectedObj as Models.Blueprint;
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

            _colonyService.AddItem(_selectedColonyUUID, item);
            PopulateItemGrid();
        }

        // -------------------------------------------------------------------
        // Warehousing â€” Delete Item (20.4)
        // -------------------------------------------------------------------

        private void DgvItems_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete) return;
            if (dgvItems.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvItems.SelectedRows)
            {
                Item item = row.Tag as Item;
                if (item != null)
                {
                    var colony = playerContext.FindMutableColony(_selectedColonyUUID);
                    int locked = colony?.Locks != null
                        ? colony.Locks.GetLockedQuantity(item.ItemType, GetLockKey(item))
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

                    _colonyService.RemoveItem(_selectedColonyUUID, item.UUID);
                }
            }

            PopulateItemGrid();

            e.Handled = true;
        }

        // -------------------------------------------------------------------
        // Warehousing â€” Editable Amount Column (20.5)
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
                _colonyService.UpdateItem(_selectedColonyUUID, item.UUID, qty);
                RefreshStatusSummary();
                _structuresDirty = true;
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

            // Prompt for unsaved changes before import (9.3)
            Log.Debug("CmdImportColony_Click: checking IsDirty={0} UUID={1}", _viewModel.IsDirty, _viewModel.UUID ?? "(null)");
            if (_viewModel.IsDirty)
            {
                var dirtyResult = PromptUnsavedChanges();
                if (dirtyResult == DialogResult.Yes)
                {
                    try
                    {
                        SaveCurrentColony();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error saving during unsaved changes prompt");
                        MessageBox.Show(
                            "Failed to save: " + ex.Message,
                            "Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                }
                else if (dirtyResult == DialogResult.Cancel)
                {
                    return;
                }
            }

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
                var dedupeResult = parser.ImportFromHtml(htmlFragment, empireContext, _colonyService);

                if (dedupeResult == null)
                {
                    Log.Warn("Colony import: no planet name parsed, skipping import");
                    MessageBox.Show(
                        "Could not determine the planet name from the clipboard data.",
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                ReadOnlyColony imported = _colonyService.PersistImport(dedupeResult);

                Log.Info(
                    "Colony imported via service: planet='{0}' uuid={1}",
                    imported.PlanetName,
                    imported.UUID);

                _selectedColonyUUID = imported.UUID;
                _previousSelectedUUID = imported.UUID;
                _viewModel.LoadFrom(imported);

                // Refresh list view
                TxtColonyFilter_TextChanged(sender, e);

                // Select the imported colony in the list view
                foreach (ListViewItem item in lvwColonies.Items)
                {
                    if ((item.Tag as ReadOnlyColony)?.UUID == _selectedColonyUUID)
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break;
                    }
                }

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
        /// Handler for structure type checkbox changes â€” updates the unchecked set
        /// and re-applies the filter.
        /// </summary>
        private void LvwStructureTypes_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

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
        /// Lightweight â€” just toggles Visible on existing controls instead of full rebuild.
        /// </summary>
        private void ApplyStructureTypeFilter()
        {
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

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
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;

            var colony = playerContext.FindMutableColony(_selectedColonyUUID);
            if (colony == null) return;

            PopulateOverflowResourceCombo();
            PopulateOverflowPurityCombo();
            PopulateOverflowDestCombo();
            PopulateOverflowRouteCombo();

            var rules = playerContext.WarehouseOverflowRuleList
                .Where(r => r.ColonyUUID == _selectedColonyUUID)
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
                if (colony.Items != null)
                {
                    var matchingItems = colony.Items.FindResource(rule.ResourceName, rule.ResourcePurity);
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
            var resources = empireContext?.ResourceList;
            var names = new List<string>();
            if (resources != null)
            {
                foreach (var r in resources.OrderBy(r => r.Name))
                    names.Add(r.Name);
            }

            cmbOverflowResource.SetItems(names, names.Count > 0 ? names[0] : null);
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
            if (cmbOverflowDestType.SelectedItem == null) return;
            var destType = (DestinationType)cmbOverflowDestType.SelectedItem;
            var names = new List<string>();
            _overflowDestUUIDs = new List<string>();
            switch (destType)
            {
                case DestinationType.Colony:
                    foreach (var c in CollectionSortHelper.OrderColonies(playerContext.ColonyList))
                    {
                        names.Add(c.ColonyName);
                        _overflowDestUUIDs.Add(c.UUID);
                    }

                    break;
                case DestinationType.Station:
                    foreach (var s in CollectionSortHelper.OrderStations(playerContext.StationList))
                    {
                        names.Add(s.Name);
                        _overflowDestUUIDs.Add(s.UUID);
                    }

                    break;
            }

            cmbOverflowDest.SetItems(names, names.Count > 0 ? names[0] : null);

            sw.Stop();
            Log.Info("PERF PopulateOverflowDestCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateOverflowRouteCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var routes = CollectionSortHelper.OrderDeliveryRoutes(playerContext.DeliveryRouteList).ToList();
            var names = new List<string>();
            _overflowRouteUUIDs = new List<string>();
            names.Add("(none)");
            _overflowRouteUUIDs.Add(string.Empty);
            foreach (var r in routes)
            {
                names.Add(r.Name);
                _overflowRouteUUIDs.Add(r.UUID);
            }

            cmbOverflowRoute.SetItems(names, names.Count > 0 ? names[0] : null);
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
            if (string.IsNullOrEmpty(_selectedColonyUUID)) return;
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
            string destUUID = string.Empty;
            int destIdx = cmbOverflowDest.SelectedFullIndex;
            if (destIdx >= 0 && destIdx < _overflowDestUUIDs.Count)
                destUUID = _overflowDestUUIDs[destIdx];
            string routeUUID = string.Empty;
            int routeIdx = cmbOverflowRoute.SelectedFullIndex;
            if (routeIdx >= 0 && routeIdx < _overflowRouteUUIDs.Count)
                routeUUID = _overflowRouteUUIDs[routeIdx];

            var existing = playerContext.WarehouseOverflowRuleList
                .FirstOrDefault(r => r.ColonyUUID == _selectedColonyUUID &&
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
                ColonyUUID = _selectedColonyUUID,
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
            if (string.IsNullOrEmpty(_selectedColonyUUID) || dgvOverflowRules.SelectedRows.Count == 0) return;
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

        // -------------------------------------------------------------------
        // Context Menu Handlers (grid-context-menus spec, task 1.2)
        // -------------------------------------------------------------------

        private void TsmiRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvItems.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvItems.SelectedRows)
            {
                Item item = row.Tag as Item;
                if (item != null)
                {
                    var colony = playerContext.FindMutableColony(_selectedColonyUUID);
                    int locked = colony?.Locks != null
                        ? colony.Locks.GetLockedQuantity(item.ItemType, GetLockKey(item))
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

                    _colonyService.RemoveItem(_selectedColonyUUID, item.UUID);
                }
            }

            PopulateItemGrid();
        }

        private void TsmiRemoveCommodityRequest_Click(object sender, EventArgs e)
        {
            if (dgvCommodityRequests.SelectedRows.Count == 0) return;

            foreach (DataGridViewRow row in dgvCommodityRequests.SelectedRows)
            {
                CommodityRequested request = row.Tag as CommodityRequested;
                if (request != null)
                {
                    _colonyService.RemoveCommodityRequest(_selectedColonyUUID, request.Name);
                }
            }

            PopulateCommodityRequestGrid();
            UpdateTabWarnings();
        }

        private void DgvItems_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvItems.ClearSelection();
                dgvItems.Rows[e.RowIndex].Selected = true;
                dgvItems.CurrentCell = dgvItems.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvItems.ClearSelection();
            }
        }

        private void DgvCommodityRequests_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvCommodityRequests.ClearSelection();
                dgvCommodityRequests.Rows[e.RowIndex].Selected = true;
                dgvCommodityRequests.CurrentCell = dgvCommodityRequests.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvCommodityRequests.ClearSelection();
            }
        }

        private void DgvOverflowRules_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvOverflowRules.ClearSelection();
                dgvOverflowRules.Rows[e.RowIndex].Selected = true;
                dgvOverflowRules.CurrentCell = dgvOverflowRules.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvOverflowRules.ClearSelection();
            }
        }

        private void CmsItems_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvItems.CurrentRow != null;
            tsmiRemoveItem.Enabled = hasSelection;
        }

        private void CmsCommodityRequests_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvCommodityRequests.CurrentRow != null;
            tsmiRemoveCommodityRequest.Enabled = hasSelection;
        }

        private void CmsOverflowRules_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvOverflowRules.CurrentRow != null;
            tsmiRemoveOverflowRule.Enabled = hasSelection;
        }

        private class SyncResult
        {
            public SyncStatus Status { get; set; }

            public bool AnyChanges { get; set; }

            public bool TransitionToInvalidKey { get; set; }
        }
    }
}
