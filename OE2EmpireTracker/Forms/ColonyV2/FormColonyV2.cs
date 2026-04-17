using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

            // Wire admin refresh timer (11.1)
            timerAdminRefresh.Tick += timerAdminRefresh_Tick;
            timerAdminRefresh.Start();

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
                selectedColony = lvwColonies.SelectedItems[0].Tag as Models.Colony;
                colonyViewModel = new ColonyViewModel(selectedColony, playerContext);
                PopulateForm();
                UpdateDeleteButtonState();
                RefreshAdminReport();
                UpdateTabWarnings();
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
            // Future tabs: tabPWarehousing
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

            // Ensure structure type filter list is populated (9.1)
            PopulateStructureTypeFilter();

            flpStructures.SuspendLayout();
            ReturnAllToPool();
            flpStructures.Controls.Clear();

            // Build set of checked types for filter (9.2)
            var checkedTypes = new HashSet<string>(StringComparer.Ordinal);
            foreach (ListViewItem item in lvwStructureTypes.Items)
            {
                if (item.Checked)
                    checkedTypes.Add((string)item.Tag);
            }

            var structureVMs = colonyViewModel.StructureViewModels;
            foreach (var vm in structureVMs)
            {
                var bp = playerContext.FindBlueprint(vm.Data.FlatpackBlueprintUUID);
                string typeId = bp?.BluePrintType ?? "";

                // Skip structures whose type is unchecked (9.2)
                if (checkedTypes.Count > 0 && !checkedTypes.Contains(typeId))
                    continue;

                var ctrl = AcquireStructureControl();
                ctrl.ViewModel = vm;
                ctrl.Colony = selectedColony;
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
            if (selectedColony == null || string.IsNullOrEmpty(selectedColony.UUID))
            {
                rtbAdminReport.Rtf = "";
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

            int activeCount = selectedColony.Commodities
                .Count(cr => !cr.Fulfilled);
            tabPWorkers.Text = activeCount > 0 ? $"Workers : {activeCount}" : "Workers";
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
        /// </summary>
        private void ApplyStructureTypeFilter()
        {
            if (selectedColony == null) return;

            // Rebuild with the filter applied
            PopulateStructures();
        }
    }
}
