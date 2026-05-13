using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.ShipTemplate
{
    public partial class FormShipTemplate : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private ShipTemplateViewModel _viewModel = new ShipTemplateViewModel();
        private ShipTemplateService _shipTemplateService;

        private List<string> _hullUUIDs = new List<string>();

        private List<string> _pricingPlanUUIDs = new List<string>();

        public FormShipTemplate()
        {
            // Guard against WindowStateHelper.RestoreState setting control values
            // (called by MainWindow between constructor and Show). Cleared in Shown event.
            _isProgrammaticUpdate++;

            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _shipTemplateService = new ShipTemplateService(playerContext);

            lvwTemplates.View = View.Details;
            lvwTemplates.Columns.Add("Name", 160);
            lvwTemplates.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwTemplates.FullRowSelect = true;
            lvwTemplates.MultiSelect = false;
            lvwTemplates.ItemSelectionChanged += LvwTemplates_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            cmbHull.SelectedItemChanged += CmbHull_SelectedItemChanged;
            cmbPricingPlan.SelectedItemChanged += CmbPricingPlan_SelectedItemChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;
            cmdOrderBuild.Click += CmdOrderBuild_Click;

            dgvSlots.CellValueChanged += DgvSlots_CellValueChanged;
            dgvSlots.CellMouseClick += DgvSlots_CellMouseClick;

            // Context menu event wiring
            tsmiClearSlot.Click += TsmiClearSlot_Click;
            cmsSlots.Opening += CmsSlots_Opening;
            dgvSlots.CurrentCellDirtyStateChanged += DgvSlots_CurrentCellDirtyStateChanged;
            dgvSlots.DataError += DgvSlots_DataError;
            dgvSlots.CellClick += DgvSlots_CellClick;

            PopulateHullCombo();
            PopulatePricingPlanCombo();
            PopulateTemplateList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.PricingDataChanged += OnPricingDataChanged;

            // Clear the programmatic guard. Then restore selection by matching
            // the plan name that WindowStateHelper put into txtName.
            Shown += (s, ev) =>
            {
                _isProgrammaticUpdate--;
                string restoredName = txtName.Text?.Trim();
                if (!string.IsNullOrEmpty(restoredName))
                {
                    foreach (ListViewItem item in lvwTemplates.Items)
                    {
                        if (item.Tag is ReadOnlyShipTemplate tmpl &&
                            string.Equals(tmpl.Name, restoredName, StringComparison.OrdinalIgnoreCase))
                        {
                            item.Selected = true;
                            item.EnsureVisible();
                            break;
                        }
                    }
                }
            };
        }

        private enum UnsavedAction
        {
            Save,
            Discard,
            Cancel,
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.PricingDataChanged -= OnPricingDataChanged;
            base.OnFormClosed(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var action = PromptUnsavedChanges();
                if (action == UnsavedAction.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (action == UnsavedAction.Save)
                {
                    if (!TrySaveCurrentTemplate())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            base.OnFormClosing(e);
        }

        // Layout
        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwTemplates.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int statsHeight = 185;
            int gridHeight = h - flpName.Height - flpHull.Height - flpPricing.Height - cmdSave.Height - statsHeight - 36;
            if (gridHeight < 50) gridHeight = 50;
            dgvSlots.Size = new System.Drawing.Size(w - 6, gridHeight);
            rtbStats.Size = new System.Drawing.Size(w - 6, statsHeight);
        }

        // Template List
        private void PopulateTemplateList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _viewModel.UUID;
            lvwTemplates.Items.Clear();

            var templates = CollectionSortHelper.OrderByName(
                playerContext.GetCurrentPlayerReadOnlyShipTemplates(),
                t => t.Name);
            string filter = txtFilter.Text.Trim();

            var refCounter = new ShipTemplateReferenceCounter(
                playerContext.SnapshotShipList(),
                playerContext.GetCurrentPlayerBuildPlans());

            foreach (var tmpl in templates)
            {
                if (!string.IsNullOrEmpty(filter)
                    && (tmpl.Name ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                int refs = refCounter.CountReferences(tmpl.UUID);
                var item = new ListViewItem(tmpl.Name) { Tag = tmpl };
                item.SubItems.Add(refs.ToString());
                lvwTemplates.Items.Add(item);
                if (tmpl.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateTemplateList: {0}ms items={1}", sw.ElapsedMilliseconds, lvwTemplates.Items.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulateTemplateList(); }

        private void LvwTemplates_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlyShipTemplate roTemplate)
            {
                if (_viewModel.IsDirty)
                {
                    var action = PromptUnsavedChanges();
                    if (action == UnsavedAction.Cancel)
                    {
                        using var guard = new ProgrammaticUpdateGuard(this);
                        e.Item.Selected = false;
                        SelectCurrentTemplateInList();
                        return;
                    }

                    if (action == UnsavedAction.Save)
                    {
                        if (!TrySaveCurrentTemplate())
                        {
                            using var guard = new ProgrammaticUpdateGuard(this);
                            e.Item.Selected = false;
                            SelectCurrentTemplateInList();
                            return;
                        }
                    }
                }

                _viewModel.LoadFrom(roTemplate);
                PopulateForm();
            }
            else if (!e.IsSelected && lvwTemplates.SelectedItems.Count == 0)
            {
                _viewModel.Reset();
                ClearForm();
            }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name))
            {
                ClearForm();
                return;
            }

            txtName.Text = _viewModel.Name;
            SelectHullInCombo(_viewModel.HullBlueprintUUID);
            PopulateSlotGrid();
            RefreshStats();
            UpdateTemplatePrice();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtName.Text = string.Empty;
            cmbHull.SetItems(cmbHull.Items, null);
            dgvSlots.Rows.Clear();
            rtbStats.Text = string.Empty;
            lblComputedPrice.Text = string.Empty;
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
            cmbHull.Enabled = enabled;
            cmbPricingPlan.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvSlots.Enabled = enabled;
        }

        // Hull Combo
        private void PopulateHullCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var names = new List<string>();
            _hullUUIDs = new List<string>();
            var hulls = CollectionSortHelper.OrderBlueprints(
                playerContext.GetAllBlueprints()
                .Where(bp => bp.BluePrintType == "Hull"));
            foreach (var bp in hulls)
            {
                names.Add(bp.ExtendedName);
                _hullUUIDs.Add(bp.UUID);
            }

            cmbHull.SetItems(names, null);
            sw.Stop();
            Log.Info("PERF PopulateHullCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectHullInCombo(string hullUUID)
        {
            if (string.IsNullOrEmpty(hullUUID))
            {
                cmbHull.SetItems(cmbHull.Items, null);
                return;
            }

            int idx = _hullUUIDs.IndexOf(hullUUID);
            if (idx >= 0)
                cmbHull.SetItems(cmbHull.Items, cmbHull.Items[idx]);
            else
                cmbHull.SetItems(cmbHull.Items, null);
        }

        private void CmbHull_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name)) return;
            int idx = cmbHull.SelectedFullIndex;
            string uuid = (idx >= 0 && idx < _hullUUIDs.Count) ? _hullUUIDs[idx] : string.Empty;
            _viewModel.HullBlueprintUUID = uuid;
            _viewModel.ClearComponents();
            PopulateSlotGrid();
            RefreshStats();
            UpdateTemplatePrice();
        }

        // Slot Grid
        private void PopulateSlotGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvSlots.Rows.Clear();
            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name))
            {
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            if (hullBp?.Properties == null) return;

            var slotDefs = GetSlotDefinitions(hullBp);
            int hullClass = hullBp.Class;
            foreach (var def in slotDefs)
            {
                for (int idx = 0; idx < def.MaxCount; idx++)
                {
                    var existing = _viewModel.Components
                        .FirstOrDefault(c => c.SlotType == def.SlotType && c.SlotIndex == idx);

                    int rowIdx = dgvSlots.Rows.Add(def.SlotType, idx);
                    var row = dgvSlots.Rows[rowIdx];

                    // Populate component combo for this row
                    var comboCell = (DataGridViewFilteredComboBoxCell)row.Cells[colComponent.Index];
                    var uuidByIndex = new List<string>();
                    var itemList = new List<string>();
                    itemList.Add("(empty)");
                    uuidByIndex.Add(string.Empty);
                    var eligibleBps = CollectionSortHelper.OrderBlueprints(
                        playerContext.GetAllBlueprints()
                        .Where(bp => def.BlueprintTypes.Contains(bp.BluePrintType) && bp.Class == hullClass));
                    foreach (var bp in eligibleBps)
                    {
                        itemList.Add(bp.ExtendedName);
                        uuidByIndex.Add(bp.UUID);
                    }

                    if (existing != null && !string.IsNullOrEmpty(existing.BlueprintUUID))
                    {
                        int matchIdx = uuidByIndex.IndexOf(existing.BlueprintUUID);
                        if (matchIdx >= 0)
                        {
                            comboCell.Items = itemList;
                            comboCell.Value = itemList[matchIdx];
                        }
                        else
                        {
                            var compBp = playerContext.FindBlueprint(existing.BlueprintUUID);
                            string fallback = compBp?.ExtendedName ?? "(unknown)";
                            itemList.Add(fallback);
                            uuidByIndex.Add(existing.BlueprintUUID);
                            comboCell.Items = itemList;
                            comboCell.Value = fallback;
                        }
                    }
                    else
                    {
                        comboCell.Items = itemList;
                        comboCell.Value = "(empty)";
                    }

                    row.Tag = new SlotInfo { SlotType = def.SlotType, SlotIndex = idx, UUIDByIndex = uuidByIndex };
                }
            }

            sw.Stop();
            Log.Info("PERF PopulateSlotGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvSlots_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvSlots.IsCurrentCellDirty)
                dgvSlots.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvSlots_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || e.RowIndex < 0) return;
            if (e.ColumnIndex != colComponent.Index) return;

            var row = dgvSlots.Rows[e.RowIndex];
            var info = row.Tag as SlotInfo;
            if (info == null) return;

            string bpUUID = string.Empty;
            var comboCell = (DataGridViewFilteredComboBoxCell)row.Cells[colComponent.Index];
            int selectedIdx = comboCell.Items != null ? comboCell.Items.IndexOf(comboCell.Value?.ToString()) : -1;
            if (selectedIdx > 0 && info.UUIDByIndex != null && selectedIdx < info.UUIDByIndex.Count)
                bpUUID = info.UUIDByIndex[selectedIdx];

            if (string.IsNullOrEmpty(bpUUID))
            {
                _viewModel.RemoveComponent(info.SlotType, info.SlotIndex);
            }
            else
            {
                _viewModel.SetComponent(info.SlotType, info.SlotIndex, bpUUID);
            }

            RefreshStats();
            UpdateTemplatePrice();
        }

        private void DgvSlots_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn("dgvSlots DataError at [{0}, {1}]: {2}", e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }

        private void DgvSlots_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == colComponent.Index)
            {
                dgvSlots.BeginEdit(true);
            }
        }

        private void DgvSlots_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvSlots.ClearSelection();
                dgvSlots.Rows[e.RowIndex].Selected = true;
                dgvSlots.CurrentCell = dgvSlots.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvSlots.ClearSelection();
            }
        }

        private void CmsSlots_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvSlots.CurrentRow != null;
            tsmiClearSlot.Enabled = hasSelection;
        }

        private void TsmiClearSlot_Click(object sender, EventArgs e)
        {
            if (dgvSlots.CurrentRow == null) return;
            var row = dgvSlots.CurrentRow;
            var comboCell = (DataGridViewFilteredComboBoxCell)row.Cells[colComponent.Index];
            if (comboCell.Items != null && comboCell.Items.Count > 0)
            {
                comboCell.Value = comboCell.Items[0];
            }
        }

        // Stats
        private void RefreshStats()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name))
            {
                rtbStats.Text = string.Empty;
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            if (hullBp == null)
            {
                rtbStats.Text = "Select a hull blueprint.";
                sw.Stop();
                return;
            }

            var stats = ShipBuildService.ComputeStats(
                hullBp,
                _viewModel.Components,
                uuid => playerContext.FindBlueprint(uuid));

            rtbStats.Text = string.Format(
                "Mass: {0}  |  Power: {1}/{2} (Balance: {3})\n" +
                "Cargo: {4}  |  Fuel: {5}  |  Hopper: {6}\n" +
                "Health: {7}  |  Shield: {8} (Regen: {9})\n" +
                "Defence — Energy: {10}  Kinetic: {11}  Missile: {12}\n" +
                "Accel: {13}  |  Rotation: {14}  |  Jump: {15} (Fuel/Jump: {16})\n" +
                "Mining Yield: {17}  |  Scan Level: {18}",
                stats.TotalMass,
                stats.PowerGenerated,
                stats.PowerConsumed,
                stats.PowerBalance,
                stats.CargoCapacity,
                stats.FuelCapacity,
                stats.HopperCapacity,
                stats.TotalHealth,
                stats.ShieldHitpoints,
                stats.ShieldRegen,
                stats.EnergyDefence,
                stats.KineticDefence,
                stats.MissileDefence,
                stats.Acceleration,
                stats.RotationalThrust,
                stats.MaxJumpDistance,
                stats.FuelPerJump,
                stats.MiningYield,
                stats.ScanLevel);
            sw.Stop();
            Log.Info("PERF RefreshStats: {0}ms", sw.ElapsedMilliseconds);
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var action = PromptUnsavedChanges();
                if (action == UnsavedAction.Cancel)
                {
                    return;
                }

                if (action == UnsavedAction.Save)
                {
                    if (!TrySaveCurrentTemplate())
                    {
                        return;
                    }
                }
            }

            _viewModel.Reset();
            _viewModel.Name = "New Template";
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                lvwTemplates.SelectedItems.Clear();
            }

            PopulateForm();
            SetDetailEnabled(true);
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew || string.IsNullOrEmpty(_viewModel.UUID)) return;

            var refCounter = new ShipTemplateReferenceCounter(
                playerContext.SnapshotShipList(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_viewModel.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format(
                        "Cannot delete template '{0}' — it is referenced by {1} ship(s) or build item(s).",
                        _viewModel.Name,
                        refs),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete template '{0}'?", _viewModel.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _shipTemplateService.Delete(_viewModel.UUID);
            _viewModel.Reset();
            PopulateTemplateList();
            ClearForm();
            Log.Info("Deleted template");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            TrySaveCurrentTemplate();
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.Name = txtName.Text;
        }

        private bool TrySaveCurrentTemplate()
        {
            string name = _viewModel.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _viewModel.Name = name;

            try
            {
                ReadOnlyShipTemplate saved;
                if (_viewModel.IsNew)
                {
                    saved = _shipTemplateService.Create(_viewModel.BuildCreateRequest());
                }
                else
                {
                    saved = _shipTemplateService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
                }

                _viewModel.LoadFrom(saved);
                PopulateTemplateList();
                Log.Info("Saved template '{0}'", _viewModel.Name);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save template");
                MessageBox.Show(
                    "Failed to save template: " + ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        // -------------------------------------------------------------------
        // Order Build (18.4)
        // -------------------------------------------------------------------

        private void CmdOrderBuild_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name)) return;
            if (string.IsNullOrEmpty(_viewModel.HullBlueprintUUID))
            {
                MessageBox.Show(
                    "Select a hull blueprint first.",
                    "No Hull",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            Log.Info(
                "CmdOrderBuild_Click: template={0} uuid={1}",
                _viewModel.Name,
                _viewModel.UUID);

            // Prompt for quantity
            int quantity = ShowQuantityDialog();
            if (quantity <= 0) return;

            // Prompt for assembly station
            var station = ShowStationPickerDialog();
            if (station == null) return;

            // Validate assembly location
            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            if (hullBp != null)
            {
                decimal shipClassVal = 0m;
                hullBp.Properties?.GetDecimal("Class", 0m, out shipClassVal);
                int shipClass = (int)shipClassVal;
                string validationError = ShipBuildService.ValidateAssemblyLocation(shipClass, station.StationType);
                if (validationError != null)
                {
                    MessageBox.Show(
                        validationError,
                        "Invalid Assembly Location",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            // Generate build items from ViewModel local state
            var tempTemplate = new Models.ShipTemplate
            {
                UUID = _viewModel.UUID,
                Name = _viewModel.Name,
                HullBlueprintUUID = _viewModel.HullBlueprintUUID,
                Components = _viewModel.Components,
            };
            var items = ShipBuildService.GenerateShipBuildItems(
                tempTemplate,
                quantity,
                DestinationType.Station,
                station.UUID,
                uuid => playerContext.FindBlueprint(uuid),
                uuid => 0);

            if (items.Count == 0)
            {
                MessageBox.Show(
                    "No build items needed (all components in stock).",
                    "Order Build",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Show plan picker
            BuildPlan targetPlan = ShowBuildPlanPickerDialog();
            if (targetPlan == null) return;

            // Add items to plan
            foreach (var item in items)
            {
                item.ShipTemplateUUID = _viewModel.UUID;
                targetPlan.Items.Add(item);
            }

            // Persist
            if (!playerContext.BuildPlanList.Contains(targetPlan))
                playerContext.AddBuildPlan(targetPlan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(targetPlan.UUID);

            Log.Info(
                "CmdOrderBuild_Click: {0} items added to plan '{1}'",
                items.Count,
                targetPlan.Name);

            MessageBox.Show(
                string.Format(
                    "{0} build items for {1}x '{2}' added to plan '{3}'.",
                    items.Count,
                    quantity,
                    _viewModel.Name,
                    targetPlan.Name),
                "Order Build",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private int ShowQuantityDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Order Build — Quantity";
                form.ClientSize = new System.Drawing.Size(300, 100);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Quantity:", Left = 15, Top = 18, Width = 60 };
                var nud = new NumericUpDown
                {
                    Left = 80, Top = 15, Width = 80,
                    Minimum = 1, Maximum = 100, Value = 1
                };

                var btnOk = new Button
                {
                    Text = "OK", Left = 120, Top = 55, Width = 75,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 200, Top = 55, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, nud, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) != DialogResult.OK) return 0;
                return (int)nud.Value;
            }
        }

        private Models.Station ShowStationPickerDialog()
        {
            var stations = playerContext.GetCurrentPlayerStations();
            if (stations.Count == 0)
            {
                MessageBox.Show(
                    "No stations available. Create a station first.",
                    "No Stations",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return null;
            }

            using (var form = new Form())
            {
                form.Text = "Order Build — Assembly Station";
                form.ClientSize = new System.Drawing.Size(350, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Assembly station:", Left = 15, Top = 18, Width = 100 };
                var cmb = new ComboBox
                {
                    Left = 120, Top = 15, Width = 210,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                foreach (var s in stations)
                    cmb.Items.Add(s);
                cmb.DisplayMember = "Name";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

                var btnOk = new Button
                {
                    Text = "OK", Left = 170, Top = 65, Width = 75,
                    DialogResult = DialogResult.OK
                };

                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 255, Top = 65, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, cmb, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) != DialogResult.OK) return null;
                return cmb.SelectedItem as Models.Station;
            }
        }

        private BuildPlan ShowBuildPlanPickerDialog()
        {
            var existingPlans = playerContext.GetCurrentPlayerBuildPlans();

            using (var form = new Form())
            {
                form.Text = "Order Build — Select Plan";
                form.ClientSize = new System.Drawing.Size(400, 180);
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
                    string planName = string.Format("{0} - Ship Build", _viewModel.Name ?? "Ship");
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
        // Unsaved Changes
        // -------------------------------------------------------------------

        private UnsavedAction PromptUnsavedChanges()
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Save before continuing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes:
                    return UnsavedAction.Save;
                case DialogResult.No:
                    return UnsavedAction.Discard;
                default:
                    return UnsavedAction.Cancel;
            }
        }

        private void SelectCurrentTemplateInList()
        {
            if (string.IsNullOrEmpty(_viewModel.UUID)) return;
            foreach (ListViewItem item in lvwTemplates.Items)
            {
                var tag = item.Tag as ReadOnlyShipTemplate;
                if (tag != null && tag.UUID == _viewModel.UUID)
                {
                    item.Selected = true;
                    return;
                }
            }
        }

        // Events
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

            _viewModel.Reset();
            PopulateHullCombo();
            PopulatePricingPlanCombo();
            PopulateTemplateList();
            ClearForm();
        }

        // -------------------------------------------------------------------
        // Pricing
        // -------------------------------------------------------------------

        private void PopulatePricingPlanCombo()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedName = cmbPricingPlan.SelectedItem;

            var plans = playerContext.GetCurrentPlayerPricingPlans();
            var planNames = new List<string>();
            _pricingPlanUUIDs = new List<string>();
            planNames.Add("(none)");
            _pricingPlanUUIDs.Add(string.Empty);
            foreach (var p in CollectionSortHelper.OrderPricingPlans(plans))
            {
                planNames.Add(p.Name);
                _pricingPlanUUIDs.Add(p.UUID);
            }

            cmbPricingPlan.SetItems(planNames, selectedName ?? "(none)");
            sw.Stop();
            Log.Info("PERF PopulatePricingPlanCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void UpdateTemplatePrice()
        {
            var sw = Stopwatch.StartNew();

            if (_viewModel.IsNew && string.IsNullOrEmpty(_viewModel.Name))
            {
                lblComputedPrice.Text = string.Empty;
                sw.Stop();
                return;
            }

            if (string.IsNullOrEmpty(_viewModel.HullBlueprintUUID))
            {
                lblComputedPrice.Text = string.Empty;
                sw.Stop();
                return;
            }

            int planIdx = cmbPricingPlan.SelectedFullIndex;
            string planUUID = planIdx >= 0 && planIdx < _pricingPlanUUIDs.Count ? _pricingPlanUUIDs[planIdx] : string.Empty;
            if (string.IsNullOrEmpty(planUUID))
            {
                lblComputedPrice.Text = string.Empty;
                sw.Stop();
                return;
            }

            var plan = playerContext.PricingPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (plan == null)
            {
                lblComputedPrice.Text = string.Empty;
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            if (hullBp == null)
            {
                lblComputedPrice.Text = string.Empty;
                sw.Stop();
                return;
            }

            decimal aggregatePrice = 0m;
            bool isComplete = true;

            // Hull price
            decimal hullMfgHours = 0m;
            string hullMfgTimeStr;
            hullBp.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, string.Empty, out hullMfgTimeStr);
            if (!string.IsNullOrEmpty(hullMfgTimeStr))
            {
                decimal seconds = EvolutionChainService.ParseTimeToSeconds(hullMfgTimeStr);
                hullMfgHours = seconds / 3600m;
            }

            var hullResult = PriceCalculator.ComputeBlueprintPrice(plan, hullBp, hullMfgHours);
            aggregatePrice += hullResult.Price;
            isComplete = isComplete && hullResult.IsComplete;

            // Component prices
            foreach (var slot in _viewModel.Components)
            {
                if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;

                var compBp = playerContext.FindBlueprint(slot.BlueprintUUID);
                if (compBp == null)
                {
                    isComplete = false;
                    continue;
                }

                decimal compMfgHours = 0m;
                string compMfgTimeStr;
                compBp.Properties.GetString(BlueprintPropertyKeys.ManufactureRunTime, string.Empty, out compMfgTimeStr);
                if (!string.IsNullOrEmpty(compMfgTimeStr))
                {
                    decimal seconds = EvolutionChainService.ParseTimeToSeconds(compMfgTimeStr);
                    compMfgHours = seconds / 3600m;
                }

                var compResult = PriceCalculator.ComputeBlueprintPrice(plan, compBp, compMfgHours);
                aggregatePrice += compResult.Price;
                isComplete = isComplete && compResult.IsComplete;
            }

            string priceText = aggregatePrice.ToString("N2");
            if (!isComplete)
            {
                priceText += " *";
            }

            lblComputedPrice.Text = priceText;
            sw.Stop();
            Log.Info("PERF UpdateTemplatePrice: {0}ms", sw.ElapsedMilliseconds);
        }

        private void RefreshPricing()
        {
            PopulatePricingPlanCombo();
            UpdateTemplatePrice();
        }

        private void CmbPricingPlan_SelectedItemChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            UpdateTemplatePrice();
        }

        private void OnPricingDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnPricingDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            RefreshPricing();
        }

        // Helpers
        private List<SlotDefinition> GetSlotDefinitions(ReadOnlyBlueprint hullBp)
        {
            // Build reverse map: slot type → list of BlueprintType IDs that fit that slot
            var slotToBpTypes = new Dictionary<string, List<string>>();
            foreach (var kvp in Constants.SlotTypes.BlueprintTypeToSlotType)
            {
                if (!slotToBpTypes.ContainsKey(kvp.Value))
                    slotToBpTypes[kvp.Value] = new List<string>();
                slotToBpTypes[kvp.Value].Add(kvp.Key);
            }

            var defs = new List<SlotDefinition>();
            foreach (var kvp in Constants.SlotTypes.HullPropertyToSlotType)
            {
                decimal maxVal;
                if (hullBp.Properties.GetDecimal(kvp.Key, 0m, out maxVal) && maxVal > 0)
                {
                    slotToBpTypes.TryGetValue(kvp.Value, out var bpTypes);
                    defs.Add(new SlotDefinition
                    {
                        SlotType = kvp.Value,
                        MaxCount = (int)maxVal,
                        BlueprintTypes = bpTypes ?? new List<string>()
                    });
                }
            }

            return defs;
        }

        private class SlotInfo
        {
            public string SlotType { get; set; }
            public int SlotIndex { get; set; }
            public List<string> UUIDByIndex { get; set; }
        }

        private class SlotDefinition
        {
            public string SlotType { get; set; }
            public int MaxCount { get; set; }
            public List<string> BlueprintTypes { get; set; }
        }
    }
}
