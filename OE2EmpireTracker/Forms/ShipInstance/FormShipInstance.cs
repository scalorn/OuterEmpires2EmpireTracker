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

namespace OE2EmpireTracker.Forms.ShipInstance
{
    public partial class FormShipInstance : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private ShipViewModel _viewModel = new ShipViewModel();
        private ShipService _shipService;

        private List<string> _hullUUIDs = new List<string>();

        public FormShipInstance()
        {
            // Guard against WindowStateHelper.RestoreState setting control values.
            _isProgrammaticUpdate++;

            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _shipService = new ShipService(playerContext);

            lvwShips.View = View.Details;
            lvwShips.Columns.Add("Name", 160);
            lvwShips.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwShips.FullRowSelect = true;
            lvwShips.MultiSelect = false;
            lvwShips.ItemSelectionChanged += LvwShips_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            cmbLocationType.SelectedIndexChanged += CmbLocationType_SelectedIndexChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;
            cmdFromTemplate.Click += CmdFromTemplate_Click;
            cmbHull.SelectedItemChanged += CmbHull_SelectedItemChanged;
            rbCargoHold.CheckedChanged += RbCargo_CheckedChanged;
            rbHopper.CheckedChanged += RbCargo_CheckedChanged;
            cmdAddItem.Click += CmdAddItem_Click;
            cmdRemoveItem.Click += CmdRemoveItem_Click;
            dgvCargo.SelectionChanged += DgvCargo_SelectionChanged;
            cmbAddType.SelectedIndexChanged += CmbAddType_SelectedIndexChanged;

            dgvComponents.CellEndEdit += DgvComponents_CellEndEdit;
            dgvComponents.CellValueChanged += DgvComponents_CellValueChanged;
            dgvComponents.CurrentCellDirtyStateChanged += DgvComponents_CurrentCellDirtyStateChanged;
            dgvComponents.DataError += DgvComponents_DataError;
            dgvComponents.CellClick += DgvComponents_CellClick;

            // Context menu event wiring
            tsmiClearSlotComponent.Click += TsmiClearSlotComponent_Click;
            tsmiAddCargo.Click += CmdAddItem_Click;
            tsmiRemoveCargo.Click += CmdRemoveItem_Click;
            dgvComponents.CellMouseClick += DgvComponents_CellMouseClick;
            dgvCargo.CellMouseClick += DgvCargo_CellMouseClick;
            cmsComponents.Opening += CmsComponents_Opening;
            cmsCargo.Opening += CmsCargo_Opening;

            PopulateLocationTypeCombo();
            PopulateHullCombo();
            PopulateAddTypeCombo();
            PopulateShipList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;

            // Clear guard and restore selection by matching restored txtName.
            Shown += (s, ev) =>
            {
                _isProgrammaticUpdate--;
                string restoredName = txtName.Text?.Trim();
                if (!string.IsNullOrEmpty(restoredName))
                {
                    foreach (ListViewItem item in lvwShips.Items)
                    {
                        if (item.Tag is ReadOnlyShip ship &&
                            string.Equals(ship.Name, restoredName, StringComparison.OrdinalIgnoreCase))
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
                    if (!TrySaveCurrentShip())
                    {
                        e.Cancel = true;
                        return;
                    }
                }
            }

            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // Layout
        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w2 - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - 12;
            if (listHeight < 50) listHeight = 50;
            lvwShips.Size = new System.Drawing.Size(w2 - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int tabHeight = h - flpName.Height - flpHull.Height - flpLocation.Height - flpCommands.Height - 30;
            if (tabHeight < 100) tabHeight = 100;
            tabControl.Size = new System.Drawing.Size(w2 - 6, tabHeight);
        }

        // Ship List
        private void PopulateShipList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _viewModel.UUID;
            lvwShips.Items.Clear();

            var roShips = playerContext.GetCurrentPlayerReadOnlyShips();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                roShips = roShips.Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            roShips = CollectionSortHelper.OrderReadOnlyShips(roShips).ToList();

            var refCounter = new ShipReferenceCounter(
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());

            foreach (var roShip in roShips)
            {
                int refs = refCounter.CountReferences(roShip.UUID);
                var item = new ListViewItem(roShip.Name) { Tag = roShip };
                item.SubItems.Add(refs.ToString());
                lvwShips.Items.Add(item);
                if (roShip.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateShipList: {0}ms items={1}", sw.ElapsedMilliseconds, roShips.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulateShipList(); }

        private void LvwShips_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlyShip roShip)
            {
                if (_viewModel.IsDirty)
                {
                    var action = PromptUnsavedChanges();
                    if (action == UnsavedAction.Cancel)
                    {
                        using var guard = new ProgrammaticUpdateGuard(this);
                        e.Item.Selected = false;
                        SelectCurrentShipInList();
                        return;
                    }

                    if (action == UnsavedAction.Save)
                    {
                        if (!TrySaveCurrentShip())
                        {
                            using var guard = new ProgrammaticUpdateGuard(this);
                            e.Item.Selected = false;
                            SelectCurrentShipInList();
                            return;
                        }
                    }
                }

                _viewModel.LoadFrom(roShip);
                PopulateForm();
            }
            else if (!e.IsSelected && lvwShips.SelectedItems.Count == 0)
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
            if (_viewModel.IsNew)
            {
                ClearForm();
                return;
            }

            txtName.Text = _viewModel.Name;
            SelectHullInCombo(_viewModel.HullBlueprintUUID);
            SelectLocationType(_viewModel.LocationType);
            PopulateLocationUUIDCombo(_viewModel.LocationType);
            SelectLocationUUID(_viewModel.LocationUUID);
            PopulateOverviewGrid();
            RefreshStats();
            PopulateCargoGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtName.Text = string.Empty;
            cmbHull.SetItems(cmbHull.Items, null);
            cmbLocationType.SelectedIndex = -1;
            cmbLocationUUID.Items.Clear();
            dgvComponents.Rows.Clear();
            rtbStats.Text = string.Empty;
            dgvCargo.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
            cmbHull.Enabled = enabled;
            cmbLocationType.Enabled = enabled;
            cmbLocationUUID.Enabled = enabled;
            cmdSave.Enabled = enabled;
            tabControl.Enabled = enabled;
        }

        // Location combos
        private void PopulateLocationTypeCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbLocationType.Items.Clear();
            foreach (DestinationType dt in Enum.GetValues(typeof(DestinationType)))
                cmbLocationType.Items.Add(dt);
            sw.Stop();
            Log.Info("PERF PopulateLocationTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectLocationType(DestinationType dt)
        {
            for (int i = 0; i < cmbLocationType.Items.Count; i++)
            {
                if ((DestinationType)cmbLocationType.Items[i] == dt)
                {
                    cmbLocationType.SelectedIndex = i;
                    return;
                }
            }

            cmbLocationType.SelectedIndex = -1;
        }

        private void CmbLocationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            if (cmbLocationType.SelectedItem is DestinationType dt)
            {
                _viewModel.LocationType = dt;
                PopulateLocationUUIDCombo(dt);
            }
        }

        private void PopulateLocationUUIDCombo(DestinationType dt)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbLocationUUID.Items.Clear();
            switch (dt)
            {
                case DestinationType.Colony:
                    foreach (var c in playerContext.GetCurrentPlayerColonies())
                        cmbLocationUUID.Items.Add(new LocationEntry { Display = c.ColonyName, UUID = c.UUID });
                    break;
                case DestinationType.Station:
                    foreach (var s in playerContext.GetCurrentPlayerStations())
                        cmbLocationUUID.Items.Add(new LocationEntry { Display = s.Name, UUID = s.UUID });
                    break;
            }

            sw.Stop();
            Log.Info("PERF PopulateLocationUUIDCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectLocationUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid))
            {
                cmbLocationUUID.SelectedIndex = -1;
                return;
            }

            for (int i = 0; i < cmbLocationUUID.Items.Count; i++)
            {
                if (cmbLocationUUID.Items[i] is LocationEntry le && le.UUID == uuid)
                {
                    cmbLocationUUID.SelectedIndex = i;
                    return;
                }
            }

            cmbLocationUUID.SelectedIndex = -1;
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
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            int idx = cmbHull.SelectedFullIndex;
            string uuid = (idx >= 0 && idx < _hullUUIDs.Count) ? _hullUUIDs[idx] : string.Empty;
            _viewModel.HullBlueprintUUID = uuid;
            _viewModel.ClearComponents();
            PopulateOverviewGrid();
            RefreshStats();
        }

        // Hull Combo — GetSlotDefinitions
        private List<SlotDefinition> GetSlotDefinitions(ReadOnlyBlueprint hullBp)
        {
            var slotToBpTypes = new Dictionary<string, List<string>>();
            foreach (var kvp in SlotTypes.BlueprintTypeToSlotType)
            {
                if (!slotToBpTypes.ContainsKey(kvp.Value))
                    slotToBpTypes[kvp.Value] = new List<string>();
                slotToBpTypes[kvp.Value].Add(kvp.Key);
            }

            var defs = new List<SlotDefinition>();
            foreach (var kvp in SlotTypes.HullPropertyToSlotType)
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

        // Overview tab — component grid
        private void PopulateOverviewGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvComponents.Rows.Clear();
            if (_viewModel.IsNew)
            {
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            string hullName = hullBp?.ExtendedName ?? "(no hull)";

            // Hull row (first row, component cell read-only)
            int hullRow = dgvComponents.Rows.Add(
                "Hull",
                string.Empty,
                hullName,
                _viewModel.HullCurrentHP.ToString(),
                _viewModel.HullMaxRepairPercent.ToString());
            dgvComponents.Rows[hullRow].Tag = "hull";
            dgvComponents.Rows[hullRow].Cells[colSlotType.Index].ReadOnly = true;
            dgvComponents.Rows[hullRow].Cells[colComponent.Index].ReadOnly = true;

            if (hullBp?.Properties == null)
            {
                sw.Stop();
                Log.Info("PERF PopulateOverviewGrid: {0}ms", sw.ElapsedMilliseconds);
                return;
            }

            var slotDefs = GetSlotDefinitions(hullBp);
            int hullClass = hullBp.Class;
            foreach (var def in slotDefs)
            {
                for (int idx = 0; idx < def.MaxCount; idx++)
                {
                    var existing = _viewModel.Components
                        .FirstOrDefault(c => c.SlotType == def.SlotType && c.SlotIndex == idx);

                    int rowIdx = dgvComponents.Rows.Add(def.SlotType, idx.ToString());
                    var row = dgvComponents.Rows[rowIdx];
                    row.Cells[colSlotType.Index].ReadOnly = true;
                    row.Cells[colSlotIndex.Index].ReadOnly = true;

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

                        row.Cells[colCondition.Index].Value = existing.CurrentHP.ToString();
                        row.Cells[colMaxRepair.Index].Value = existing.MaxRepairPercent.ToString();
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
            Log.Info("PERF PopulateOverviewGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvComponents_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew || e.RowIndex < 0) return;
            var row = dgvComponents.Rows[e.RowIndex];
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? "0";

            if (row.Tag is string s && s == "hull")
            {
                if (e.ColumnIndex == colCondition.Index)
                {
                    if (int.TryParse(valStr, out int hp)) _viewModel.HullCurrentHP = hp;
                }
                else if (e.ColumnIndex == colMaxRepair.Index)
                {
                    if (decimal.TryParse(valStr, out decimal mr)) _viewModel.HullMaxRepairPercent = mr;
                }
            }
            else if (row.Tag is SlotInfo info)
            {
                var slot = _viewModel.Components
                    .FirstOrDefault(c => c.SlotType == info.SlotType && c.SlotIndex == info.SlotIndex);
                if (slot != null)
                {
                    if (e.ColumnIndex == colCondition.Index)
                    {
                        if (int.TryParse(valStr, out int hp)) slot.CurrentHP = hp;
                    }
                    else if (e.ColumnIndex == colMaxRepair.Index)
                    {
                        if (decimal.TryParse(valStr, out decimal mr)) slot.MaxRepairPercent = mr;
                    }
                }
            }
        }

        private void DgvComponents_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || e.RowIndex < 0) return;
            if (e.ColumnIndex != colComponent.Index) return;
            if (_viewModel.IsNew) return;

            var row = dgvComponents.Rows[e.RowIndex];
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
                var existing = _viewModel.Components
                    .FirstOrDefault(c => c.SlotType == info.SlotType && c.SlotIndex == info.SlotIndex);
                row.Cells[colCondition.Index].Value = existing.CurrentHP.ToString();
                row.Cells[colMaxRepair.Index].Value = existing.MaxRepairPercent.ToString();
            }

            RefreshStats();
        }

        private void DgvComponents_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvComponents.IsCurrentCellDirty)
                dgvComponents.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvComponents_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Error("dgvComponents DataError at [{0}, {1}]: {2}", e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }

        private void DgvComponents_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == colComponent.Index)
                dgvComponents.BeginEdit(true);
        }

        private void DgvComponents_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvComponents.ClearSelection();
                dgvComponents.Rows[e.RowIndex].Selected = true;
                dgvComponents.CurrentCell = dgvComponents.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvComponents.ClearSelection();
            }
        }

        private void DgvCargo_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvCargo.ClearSelection();
                dgvCargo.Rows[e.RowIndex].Selected = true;
                dgvCargo.CurrentCell = dgvCargo.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvCargo.ClearSelection();
            }
        }

        private void CmsComponents_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvComponents.CurrentRow != null;
            tsmiClearSlotComponent.Enabled = hasSelection;
        }

        private void CmsCargo_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = dgvCargo.CurrentRow != null;
            tsmiRemoveCargo.Enabled = hasSelection;
        }

        private void TsmiClearSlotComponent_Click(object sender, EventArgs e)
        {
            if (dgvComponents.CurrentRow == null) return;
            var row = dgvComponents.CurrentRow;
            var comboCell = row.Cells[colComponent.Index] as Controls.DataGridViewFilteredComboBoxCell;
            if (comboCell != null && comboCell.Items != null && comboCell.Items.Count > 0)
            {
                comboCell.Value = comboCell.Items[0];
            }
        }

        private void RefreshStats()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_viewModel.IsNew)
            {
                rtbStats.Text = string.Empty;
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.HullBlueprintUUID);
            if (hullBp == null)
            {
                rtbStats.Text = "No hull blueprint.";
                sw.Stop();
                return;
            }

            var stats = ShipBuildService.ComputeStats(
                hullBp,
                _viewModel.Components,
                uuid => playerContext.FindBlueprint(uuid));

            var sb = new System.Text.StringBuilder();

            // Identity
            sb.AppendFormat("{0} - Class {1}\n", stats.ShipType, stats.ShipClass);

            // Engineering
            string engPrefix = stats.EngCapacityUsed > stats.EngCapacityAvailable ? "!! " : string.Empty;
            sb.AppendFormat("{0}Engineering: {1} / {2}\n", engPrefix, stats.EngCapacityUsed, stats.EngCapacityAvailable);

            // Capacity
            sb.AppendFormat("--- Capacity ---\n");
            sb.AppendFormat("  Cargo: {0}  |  Fuel: {1}  |  Hopper: {2}\n", stats.CargoCapacity, stats.FuelCapacity, stats.HopperCapacity);

            // Defence
            sb.AppendFormat("--- Defence ---\n");
            sb.AppendFormat("  Health: {0}  |  Shield: {1} HP (Regen: {2}/s)\n", stats.TotalHealth, stats.ShieldHitpoints, stats.ShieldRegen);
            sb.AppendFormat("  Energy: {0}  |  Kinetic: {1}  |  Missile: {2}\n", stats.EnergyDefence, stats.KineticDefence, stats.MissileDefence);

            // Propulsion
            sb.AppendFormat("--- Propulsion ---\n");
            sb.AppendFormat("  Accel Factor: {0:F2}  (Raw: {1})\n", stats.AccelerationFactor, stats.Acceleration);
            sb.AppendFormat("  Turn Rate: {0:F2} deg/s  (Raw: {1})\n", stats.TurnRate, stats.RotationalThrust);

            // Jump
            sb.AppendFormat("--- Jump ---\n");
            sb.AppendFormat("  Range: {0:F2} JAS  |  Single Hop: {1} JAS  |  Fuel/JAS: {2:F2}\n", stats.JumpFuelRange, stats.MaxJumpDistance, stats.JumpFuelPerJAS);
            sb.AppendFormat("  Charge Time: {0:F2}s\n", stats.JumpChargeTime);

            // Power
            sb.AppendFormat("--- Power ---\n");
            sb.AppendFormat("  Capacitor: {0} MW  |  Regen: {1:F2} MW/s\n", stats.PowerProvided, stats.PowerRegenRate);
            string shieldSustain = stats.ShieldPowerDraw > 0
                ? (stats.ShieldUptime == -1m ? "Sustainable" : string.Format("{0:F2}s uptime", stats.ShieldUptime))
                : "N/A";
            sb.AppendFormat("  Shield Draw: {0:F2} MW/s -> {1}\n", stats.ShieldPowerDraw, shieldSustain);

            // Mining
            sb.AppendFormat("--- Mining ---\n");
            sb.AppendFormat("  Yield: {0}  |  Cycle: {1}s\n", stats.MiningYield, stats.MiningCycleTime);
            if (stats.MiningSustainByType.Count > 0)
            {
                foreach (var entry in stats.MiningSustainByType)
                {
                    sb.AppendFormat("  {0}: {1} installed, {2:F2} sustainable\n", entry.LaserType, entry.Count, entry.SustainableCount);
                }
            }

            // Weapons
            if (stats.WeaponSustainByType.Count > 0)
            {
                sb.AppendFormat("--- Weapons ---\n");
                foreach (var entry in stats.WeaponSustainByType)
                {
                    sb.AppendFormat("  {0}: {1} installed, {2:F2} sustainable\n", entry.WeaponType, entry.Count, entry.SustainableCount);
                }

                string weaponSustain = stats.WeaponSustainTime == -1m ? "Sustainable" : string.Format("{0:F2}s sustain", stats.WeaponSustainTime);
                sb.AppendFormat("  Total Draw: {0:F2} MW/s -> {1}\n", stats.TotalWeaponPowerDraw, weaponSustain);
            }

            // Scanning
            sb.AppendFormat("--- Scanning ---\n");
            sb.AppendFormat("  Scan Level: {0}", stats.ScanLevel);

            rtbStats.Text = sb.ToString();
            sw.Stop();
            Log.Info("PERF RefreshStats: {0}ms", sw.ElapsedMilliseconds);
        }

        // Cargo tab
        private ItemBag GetSelectedBag()
        {
            if (_viewModel.IsNew) return null;
            return _viewModel.GetSelectedBag(rbHopper.Checked);
        }

        private void RbCargo_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateCargoGrid();
            UpdatePurityComboForHopper();
        }

        private void PopulateCargoGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCargo.Rows.Clear();
            dgvCrateContents.Rows.Clear();
            lblCrateContents.Text = string.Empty;
            dgvCrateContents.Visible = false;
            lblCrateContents.Visible = false;
            var bag = GetSelectedBag();
            if (bag == null)
            {
                sw.Stop();
                return;
            }

            foreach (var kvp in CollectionSortHelper.OrderItemBagEntries(bag.Items))
            {
                var item = kvp.Value;
                string typeName = item.ItemType.ToString();
                string displayName = item.ExtendedName;
                if (item.ItemType == ItemType.ItemTypeEnum.Crate)
                {
                    typeName = "[Crate]";
                    int count = item.Contents?.Count() ?? 0;
                    displayName = string.Format("{0} ({1} items)", item.Name, count);
                }

                dgvCargo.Rows.Add(typeName, displayName, item.ResourcePurity, item.Quantity.ToString());
                dgvCargo.Rows[dgvCargo.Rows.Count - 1].Tag = item;
            }

            sw.Stop();
            Log.Info("PERF PopulateCargoGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvCargo_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCargo.SelectedRows.Count == 0)
            {
                ClearCrateContents();
                return;
            }

            var item = dgvCargo.SelectedRows[0].Tag as Item;
            if (item != null && item.ItemType == ItemType.ItemTypeEnum.Crate && item.Contents != null)
                PopulateCrateContents(item);
            else
                ClearCrateContents();
        }

        private void PopulateCrateContents(Item crate)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCrateContents.Rows.Clear();
            lblCrateContents.Text = string.Format("Crate Contents ({0}):", crate.Name);
            lblCrateContents.Visible = true;
            dgvCrateContents.Visible = true;
            foreach (var kvp in CollectionSortHelper.OrderItemBagEntries(crate.Contents.Items))
            {
                var item = kvp.Value;
                dgvCrateContents.Rows.Add(item.ItemType.ToString(), item.ExtendedName, item.ResourcePurity, item.Quantity.ToString());
            }

            sw.Stop();
            Log.Info("PERF PopulateCrateContents: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearCrateContents()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCrateContents.Rows.Clear();
            lblCrateContents.Text = string.Empty;
            lblCrateContents.Visible = false;
            dgvCrateContents.Visible = false;
        }

        private void PopulateAddTypeCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbAddType.Items.Clear();
            cmbAddType.Items.Add(ItemType.ItemTypeEnum.Resource);
            cmbAddType.Items.Add(ItemType.ItemTypeEnum.Commodity);
            cmbAddType.Items.Add(ItemType.ItemTypeEnum.Flatpack);
            cmbAddType.Items.Add(ItemType.ItemTypeEnum.Crate);
            cmbAddType.Items.Add(ItemType.ItemTypeEnum.Munition);
            cmbAddType.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateAddTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbAddType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateAddItemCombo();
            UpdatePurityComboForHopper();
        }

        private void PopulateAddItemCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var names = new List<string>();
            if (!(cmbAddType.SelectedItem is ItemType.ItemTypeEnum selectedType))
            {
                cmbAddItem.SetItems(names, string.Empty);
                sw.Stop();
                Log.Info("PERF PopulateAddItemCombo: {0}ms", sw.ElapsedMilliseconds);
                return;
            }

            if (selectedType == ItemType.ItemTypeEnum.Resource)
            {
                var resources = EmpireContext.GetInstance()?.ResourceList;
                if (resources != null)
                {
                    foreach (var r in resources.OrderBy(r => r.Name))
                        names.Add(r.Name);
                }
            }
            else if (selectedType == ItemType.ItemTypeEnum.Commodity)
            {
                foreach (var c in Commodity.ResourceMapByEnum.Values.OrderBy(c => c.ExtendedName))
                    names.Add(c.ExtendedName);
            }

            cmbAddItem.SetItems(names, string.Empty);
            sw.Stop();
            Log.Info("PERF PopulateAddItemCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void UpdatePurityComboForHopper()
        {
            cmbAddPurity.Items.Clear();
            bool isHopper = rbHopper.Checked;
            bool isResource = cmbAddType.SelectedItem is ItemType.ItemTypeEnum t
                && t == ItemType.ItemTypeEnum.Resource;

            if (isResource)
            {
                if (isHopper)
                {
                    // Hopper: only unrefined purities
                    cmbAddPurity.Items.Add(GameConstants.PurityHigh);
                    cmbAddPurity.Items.Add(GameConstants.PurityMedium);
                    cmbAddPurity.Items.Add(GameConstants.PurityLow);
                }
                else
                {
                    foreach (var p in ResourcePurity.Purities)
                    {
                        if (p.ID != ResourcePurity.PurityEnum.None)
                            cmbAddPurity.Items.Add(p.Name);
                    }
                }

                if (cmbAddPurity.Items.Count > 0) cmbAddPurity.SelectedIndex = 0;
                cmbAddPurity.Enabled = true;
            }
            else
            {
                cmbAddPurity.Enabled = false;
            }
        }

        private void CmdAddItem_Click(object sender, EventArgs e)
        {
            var bag = GetSelectedBag();
            if (bag == null || _viewModel.IsNew) return;

            if (!(cmbAddType.SelectedItem is ItemType.ItemTypeEnum itemType)) return;
            string itemName = cmbAddItem.SelectedItem ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtAddQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show(
                    "Enter a valid quantity.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string purity = cmbAddPurity.Enabled ? (cmbAddPurity.SelectedItem?.ToString() ?? string.Empty) : string.Empty;

            var newItem = new Item(itemType, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                ResourcePurity = purity,
                BaseItemTypeID = itemName
            };

            bag.AddItem(newItem);
            PopulateCargoGrid();
            Log.Info(
                "Added cargo item: {0} x{1} to {2}",
                itemName,
                qty,
                rbHopper.Checked ? "Hopper" : "Cargo");
        }

        private void CmdRemoveItem_Click(object sender, EventArgs e)
        {
            var bag = GetSelectedBag();
            if (bag == null || dgvCargo.SelectedRows.Count == 0) return;
            var item = dgvCargo.SelectedRows[0].Tag as Item;
            if (item == null) return;
            bag.Remove(item.UUID);
            PopulateCargoGrid();
            Log.Info("Removed cargo item: {0}", item.Name);
        }

        // Create from Template (19.4)
        private void CmdFromTemplate_Click(object sender, EventArgs e)
        {
            var templates = playerContext.GetCurrentPlayerShipTemplates();
            if (templates.Count == 0)
            {
                MessageBox.Show(
                    "No ship templates available. Create a template first.",
                    "No Templates",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Create Ship from Template";
                form.ClientSize = new System.Drawing.Size(350, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Template:", Left = 15, Top = 18, Width = 60 };
                var cmb = new ComboBox
                {
                    Left = 80, Top = 15, Width = 250,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                foreach (var t in CollectionSortHelper.OrderShipTemplates(templates))
                    cmb.Items.Add(t);
                cmb.DisplayMember = "Name";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

                var btnOk = new Button { Text = "OK", Left = 170, Top = 65, Width = 75, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 255, Top = 65, Width = 75, DialogResult = DialogResult.Cancel };
                form.Controls.AddRange(new Control[] { lbl, cmb, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) != DialogResult.OK) return;
                var tmpl = cmb.SelectedItem as Models.ShipTemplate;
                if (tmpl == null) return;

                var roShip = _shipService.CreateFromTemplate(tmpl.UUID);
                _viewModel.LoadFrom(roShip);
                PopulateShipList();
                PopulateForm();
                Log.Info("Created ship \"{0}\" from template \"{1}\"", roShip.Name, tmpl.Name);
            }
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var action = PromptUnsavedChanges();
                if (action == UnsavedAction.Cancel) return;
                if (action == UnsavedAction.Save && !TrySaveCurrentShip()) return;
            }

            var roShip = _shipService.Create(new ShipCreateRequest { Name = "New Ship" });
            _viewModel.LoadFrom(roShip);
            PopulateShipList();
            PopulateForm();
            Log.Info("Created blank ship \"{0}\"", roShip.Name);
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;

            var refCounter = new ShipReferenceCounter(
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_viewModel.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format(
                        "Cannot delete ship \"{0}\" — it is referenced by {1} delivery plan(s) or build item(s).",
                        _viewModel.Name,
                        refs),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete ship \"{0}\"?", _viewModel.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            _shipService.Delete(_viewModel.UUID);
            _viewModel.Reset();
            PopulateShipList();
            ClearForm();
            Log.Info("Deleted ship");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew && string.IsNullOrWhiteSpace(_viewModel.Name)) return;
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbLocationUUID.SelectedItem is LocationEntry le)
                _viewModel.LocationUUID = le.UUID;

            ReadOnlyShip roShip;
            if (_viewModel.IsNew)
            {
                roShip = _shipService.Create(_viewModel.BuildCreateRequest());
            }
            else
            {
                roShip = _shipService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
            }

            _viewModel.LoadFrom(roShip);
            PopulateShipList();
            PopulateForm();
            Log.Info("Saved ship \"{0}\"", roShip.Name);
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            _viewModel.Name = txtName.Text;
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
            PopulateShipList();
            ClearForm();
        }

        // Unsaved changes helpers
        private UnsavedAction PromptUnsavedChanges()
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Save before continuing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);

            switch (result)
            {
                case DialogResult.Yes: return UnsavedAction.Save;
                case DialogResult.No: return UnsavedAction.Discard;
                default: return UnsavedAction.Cancel;
            }
        }

        private bool TrySaveCurrentShip()
        {
            try
            {
                if (cmbLocationUUID.SelectedItem is LocationEntry le)
                    _viewModel.LocationUUID = le.UUID;

                ReadOnlyShip roShip;
                if (_viewModel.IsNew)
                    roShip = _shipService.Create(_viewModel.BuildCreateRequest());
                else
                    roShip = _shipService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());

                _viewModel.LoadFrom(roShip);
                PopulateShipList();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save ship");
                MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void SelectCurrentShipInList()
        {
            if (_viewModel.UUID == null) return;
            foreach (ListViewItem item in lvwShips.Items)
            {
                if (item.Tag is ReadOnlyShip ro && ro.UUID == _viewModel.UUID)
                {
                    item.Selected = true;
                    return;
                }
            }
        }

        // Inner classes for component grid
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

        // Helpers
        private class LocationEntry
        {
            public string Display { get; set; }
            public string UUID { get; set; }
            public override string ToString() => Display;
        }
    }
}
