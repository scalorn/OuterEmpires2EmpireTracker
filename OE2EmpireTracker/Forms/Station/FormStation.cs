using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.ViewModels;

namespace OE2EmpireTracker.Forms.Station
{
    public partial class FormStation : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;
        private StationViewModel _viewModel = new StationViewModel();
        private StationService _stationService;

        public FormStation()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;
            _stationService = new StationService(playerContext);

            lvwStations.View = View.Details;
            lvwStations.Columns.Add("Name", 120);
            lvwStations.Columns.Add("Type", 50);
            lvwStations.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwStations.FullRowSelect = true;
            lvwStations.MultiSelect = false;
            lvwStations.ItemSelectionChanged += LvwStations_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtName.TextChanged += TxtName_TextChanged;
            cmbStationType.SelectedIndexChanged += CmbStationType_SelectedIndexChanged;
            cmbOwnership.SelectedIndexChanged += CmbOwnership_SelectedIndexChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;

            cmdHoldAdd.Click += CmdHoldAdd_Click;
            cmdHoldRemove.Click += CmdHoldRemove_Click;
            cmbHoldType.SelectedIndexChanged += CmbHoldType_SelectedIndexChanged;
            dgvHold.CellEndEdit += DgvHold_CellEndEdit;
            dgvHold.SelectionChanged += DgvHold_SelectionChanged;

            dgvComponents.CellEndEdit += DgvComponents_CellEndEdit;
            cmbStationBlueprint.SelectedIndexChanged += CmbStationBlueprint_SelectedIndexChanged;

            cmdMunAdd.Click += CmdMunAdd_Click;
            cmdMunRemove.Click += CmdMunRemove_Click;

            PopulateStationTypeCombo();
            PopulateOwnershipCombo();
            PopulateHoldTypeCombo();
            PopulateStationList();
            ClearForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = ShowUnsavedChangesDialog();
                if (result == DialogResult.Yes)
                {
                    if (!TrySave())
                    {
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
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwStations.Size = new System.Drawing.Size(w2 - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int tabHeight = h - flpName.Height - flpStationType.Height - cmdSave.Height - 24;
            if (tabHeight < 100) tabHeight = 100;
            tabControl.Size = new System.Drawing.Size(w2 - 6, tabHeight);
        }

        // Station List
        private void PopulateStationList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _viewModel.UUID;
            lvwStations.Items.Clear();

            var stations = playerContext.GetCurrentPlayerReadOnlyStations();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                stations = stations.Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            stations = CollectionSortHelper.OrderReadOnlyStations(stations).ToList();

            var refCounter = new StationReferenceCounter(
                playerContext.DeliveryRouteList.ToList(),
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());

            foreach (var station in stations)
            {
                int refs = refCounter.CountReferences(station.UUID);
                var item = new ListViewItem(station.Name) { Tag = station };
                item.SubItems.Add(station.StationType.ToString());
                item.SubItems.Add(refs.ToString());
                lvwStations.Items.Add(item);
                if (station.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateStationList: {0}ms items={1}", sw.ElapsedMilliseconds, stations.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulateStationList(); }

        private void LvwStations_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is ReadOnlyStation roStation)
            {
                if (_viewModel.IsDirty)
                {
                    var result = ShowUnsavedChangesDialog();
                    if (result == DialogResult.Yes)
                    {
                        if (!TrySave())
                        {
                            RestoreSelection();
                            return;
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        RestoreSelection();
                        return;
                    }
                }

                _viewModel.LoadFrom(roStation, playerContext.CurrentPlayerUUID);
                PopulateForm();
            }
            else if (!e.IsSelected && lvwStations.SelectedItems.Count == 0)
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
            SelectComboEnum(cmbStationType, _viewModel.StationType);
            SelectComboEnum(cmbOwnership, _viewModel.Ownership);
            bool isPlayerOwned = _viewModel.Ownership == StationOwnership.PlayerOwned;
            tabComponents.Enabled = isPlayerOwned;
            UpdateMunitionsTabVisibility();
            PopulateHoldGrid();
            PopulateStationBlueprintCombo();
            PopulateComponentsGrid();
            RefreshStationStats();
            PopulateMunitionsGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtName.Text = string.Empty;
            cmbStationType.SelectedIndex = -1;
            cmbOwnership.SelectedIndex = -1;
            dgvHold.Rows.Clear();
            dgvComponents.Rows.Clear();
            rtbStationStats.Text = string.Empty;
            cmbStationBlueprint.DataSource = null;
            cmbStationBlueprint.Items.Clear();
            dgvMunitions.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
            cmbStationType.Enabled = enabled;
            cmbOwnership.Enabled = enabled;
            cmdSave.Enabled = enabled;
            tabControl.Enabled = enabled;
        }

        // Combo helpers
        private void PopulateStationTypeCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbStationType.Items.Clear();
            foreach (StationType st in Enum.GetValues(typeof(StationType)))
                cmbStationType.Items.Add(st);
            sw.Stop();
            Log.Info("PERF PopulateStationTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateOwnershipCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOwnership.Items.Clear();
            foreach (StationOwnership so in Enum.GetValues(typeof(StationOwnership)))
                cmbOwnership.Items.Add(so);
            sw.Stop();
            Log.Info("PERF PopulateOwnershipCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectComboEnum<T>(ComboBox cmb, T value)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is T v && v.Equals(value))
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }

            cmb.SelectedIndex = -1;
        }

        private void CmbStationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            if (cmbStationType.SelectedItem is StationType st)
                _viewModel.StationType = st;
        }

        private void CmbOwnership_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            if (cmbOwnership.SelectedItem is StationOwnership so)
            {
                _viewModel.Ownership = so;
                bool isPlayerOwned = so == StationOwnership.PlayerOwned;
                tabComponents.Enabled = isPlayerOwned;
                UpdateMunitionsTabVisibility();
            }
        }

        // Hold tab
        private void PopulateHoldGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvHold.Rows.Clear();
            dgvHoldCrateContents.Rows.Clear();
            lblHoldCrateContents.Text = string.Empty;
            dgvHoldCrateContents.Visible = false;
            lblHoldCrateContents.Visible = false;
            var bag = _viewModel.Hold;
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

                int rowIdx = dgvHold.Rows.Add(
                    typeName,
                    displayName,
                    item.ResourcePurity,
                    item.Quantity.ToString(),
                    item.CurrentHP.ToString(),
                    item.MaxRepairPercent.ToString());
                dgvHold.Rows[rowIdx].Tag = item;
                dgvHold.Rows[rowIdx].Cells[colHoldType.Index].ReadOnly = true;
                dgvHold.Rows[rowIdx].Cells[colHoldName.Index].ReadOnly = true;
                dgvHold.Rows[rowIdx].Cells[colHoldPurity.Index].ReadOnly = true;
                dgvHold.Rows[rowIdx].Cells[colHoldQty.Index].ReadOnly = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateHoldGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void DgvHold_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvHold.SelectedRows.Count == 0)
            {
                ClearHoldCrateContents();
                return;
            }

            var item = dgvHold.SelectedRows[0].Tag as Item;
            if (item != null && item.ItemType == ItemType.ItemTypeEnum.Crate && item.Contents != null)
                PopulateHoldCrateContents(item);
            else
                ClearHoldCrateContents();
        }

        private void PopulateHoldCrateContents(Item crate)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvHoldCrateContents.Rows.Clear();
            lblHoldCrateContents.Text = string.Format("Crate Contents ({0}):", crate.Name);
            lblHoldCrateContents.Visible = true;
            dgvHoldCrateContents.Visible = true;
            foreach (var kvp in CollectionSortHelper.OrderItemBagEntries(crate.Contents.Items))
            {
                var item = kvp.Value;
                dgvHoldCrateContents.Rows.Add(item.ItemType.ToString(), item.ExtendedName, item.ResourcePurity, item.Quantity.ToString());
            }

            sw.Stop();
            Log.Info("PERF PopulateHoldCrateContents: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearHoldCrateContents()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvHoldCrateContents.Rows.Clear();
            lblHoldCrateContents.Text = string.Empty;
            lblHoldCrateContents.Visible = false;
            dgvHoldCrateContents.Visible = false;
        }

        private void DgvHold_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew || e.RowIndex < 0) return;
            var row = dgvHold.Rows[e.RowIndex];
            if (!(row.Tag is Item item)) return;
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? "0";

            if (e.ColumnIndex == colHoldCondition.Index)
            {
                if (int.TryParse(valStr, out int hp)) item.CurrentHP = hp;
            }
            else if (e.ColumnIndex == colHoldMaxRepair.Index)
            {
                if (decimal.TryParse(valStr, out decimal mr)) item.MaxRepairPercent = mr;
            }
        }

        private void PopulateHoldTypeCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbHoldType.Items.Clear();
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Resource);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Commodity);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Flatpack);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Crate);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Munition);
            cmbHoldType.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateHoldTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbHoldType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateHoldItemCombo();
            UpdateHoldPurityCombo();
        }

        private void PopulateHoldItemCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbHoldItem.Items.Clear();
            if (!(cmbHoldType.SelectedItem is ItemType.ItemTypeEnum selectedType)) return;

            if (selectedType == ItemType.ItemTypeEnum.Resource)
            {
                var resources = EmpireContext.GetInstance()?.ResourceList;
                if (resources != null)
                {
                    foreach (var r in resources.OrderBy(r => r.Name))
                        cmbHoldItem.Items.Add(r.Name);
                }
            }
            else if (selectedType == ItemType.ItemTypeEnum.Commodity)
            {
                foreach (var c in Commodity.ResourceMapByEnum.Values.OrderBy(c => c.ExtendedName))
                    cmbHoldItem.Items.Add(c.ExtendedName);
            }

            if (cmbHoldItem.Items.Count > 0) cmbHoldItem.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateHoldItemCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void UpdateHoldPurityCombo()
        {
            cmbHoldPurity.Items.Clear();
            bool isResource = cmbHoldType.SelectedItem is ItemType.ItemTypeEnum t
                && t == ItemType.ItemTypeEnum.Resource;
            if (isResource)
            {
                foreach (var p in ResourcePurity.Purities)
                {
                    if (p.ID != ResourcePurity.PurityEnum.None)
                        cmbHoldPurity.Items.Add(p.Name);
                }

                if (cmbHoldPurity.Items.Count > 0) cmbHoldPurity.SelectedIndex = 0;
                cmbHoldPurity.Enabled = true;
            }
            else
            {
                cmbHoldPurity.Enabled = false;
            }
        }

        private void CmdHoldAdd_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;
            if (!(cmbHoldType.SelectedItem is ItemType.ItemTypeEnum itemType)) return;
            string itemName = cmbHoldItem.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtHoldQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show(
                    "Enter a valid quantity.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string purity = cmbHoldPurity.Enabled ? (cmbHoldPurity.SelectedItem?.ToString() ?? string.Empty) : string.Empty;
            var newItem = new Item(itemType, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                ResourcePurity = purity,
                BaseItemTypeID = itemName
            };

            _viewModel.AddHoldItem(newItem);
            PopulateHoldGrid();
            Log.Info("Added hold item: {0} x{1}", itemName, qty);
        }

        private void CmdHoldRemove_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew || dgvHold.SelectedRows.Count == 0) return;
            var item = dgvHold.SelectedRows[0].Tag as Item;
            if (item == null) return;
            _viewModel.RemoveHoldItem(item.UUID);
            PopulateHoldGrid();
            Log.Info("Removed hold item: {0}", item.Name);
        }

        // Components tab
        private void PopulateComponentsGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvComponents.Rows.Clear();
            if (_viewModel.IsNew)
            {
                sw.Stop();
                return;
            }

            if (_viewModel.Ownership != StationOwnership.PlayerOwned)
            {
                sw.Stop();
                return;
            }

            // Hull row first
            var hullBp = playerContext.FindBlueprint(_viewModel.StationBlueprintUUID);
            string hullName = hullBp?.ExtendedName ?? "(no hull)";
            int hullRow = dgvComponents.Rows.Add(
                "Hull",
                hullName,
                _viewModel.HullCurrentHP.ToString(),
                _viewModel.HullMaxRepairPercent.ToString());
            dgvComponents.Rows[hullRow].Tag = "hull";
            dgvComponents.Rows[hullRow].Cells[colSlotType.Index].ReadOnly = true;
            dgvComponents.Rows[hullRow].Cells[colComponentName.Index].ReadOnly = true;

            // Component rows
            foreach (var slot in _viewModel.Components)
            {
                var compBp = playerContext.FindBlueprint(slot.BlueprintUUID);
                string compName = compBp?.ExtendedName ?? "(unknown)";
                int rowIdx = dgvComponents.Rows.Add(
                    slot.SlotType,
                    compName,
                    slot.CurrentHP.ToString(),
                    slot.MaxRepairPercent.ToString());
                dgvComponents.Rows[rowIdx].Tag = slot;
                dgvComponents.Rows[rowIdx].Cells[colSlotType.Index].ReadOnly = true;
                dgvComponents.Rows[rowIdx].Cells[colComponentName.Index].ReadOnly = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateComponentsGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateStationBlueprintCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbStationBlueprint.DataSource = null;
            cmbStationBlueprint.Items.Clear();

            var blueprints = CollectionSortHelper.OrderBlueprints(
                playerContext.GetAllBlueprints()
                .Where(bp => !string.IsNullOrEmpty(bp.Name)))
                .ToList();

            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var bp in blueprints)
                items.Add(new KeyValuePair<string, string>(bp.UUID, bp.ExtendedName));

            cmbStationBlueprint.DataSource = items;
            cmbStationBlueprint.DisplayMember = "Value";
            cmbStationBlueprint.ValueMember = "Key";

            if (!_viewModel.IsNew && !string.IsNullOrEmpty(_viewModel.StationBlueprintUUID))
                cmbStationBlueprint.SelectedValue = _viewModel.StationBlueprintUUID;
            sw.Stop();
            Log.Info("PERF PopulateStationBlueprintCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbStationBlueprint_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _viewModel.IsNew) return;
            string uuid = cmbStationBlueprint.SelectedValue?.ToString() ?? string.Empty;
            _viewModel.StationBlueprintUUID = uuid;
            _viewModel.ClearComponents();
            PopulateComponentsGrid();
            RefreshStationStats();
        }

        private void RefreshStationStats()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_viewModel.IsNew)
            {
                rtbStationStats.Text = string.Empty;
                sw.Stop();
                return;
            }

            var hullBp = playerContext.FindBlueprint(_viewModel.StationBlueprintUUID);
            if (hullBp == null)
            {
                rtbStationStats.Text = "No station blueprint selected.";
                sw.Stop();
                return;
            }

            var stats = ShipBuildService.ComputeStationStats(
                hullBp,
                _viewModel.Components,
                uuid => playerContext.FindBlueprint(uuid));

            rtbStationStats.Text = string.Format(
                "Mass: {0}  |  Power: {1}/{2} (Balance: {3})\n" +
                "Health: {4}  |  Shield: {5} (Regen: {6})\n" +
                "Defence \u2014 Energy: {7}  Kinetic: {8}  Missile: {9}\n" +
                "Weapons \u2014 Small: {10}  Medium: {11}  Large: {12}",
                stats.TotalMass,
                stats.PowerGenerated,
                stats.PowerConsumed,
                stats.PowerBalance,
                stats.TotalHealth,
                stats.ShieldHitpoints,
                stats.ShieldRegen,
                stats.EnergyDefence,
                stats.KineticDefence,
                stats.MissileDefence,
                stats.SmallWeaponsInstalled,
                stats.MediumWeaponsInstalled,
                stats.LargeWeaponsInstalled);
            sw.Stop();
            Log.Info("PERF RefreshStationStats: {0}ms", sw.ElapsedMilliseconds);
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
                    if (int.TryParse(valStr, out int hp))
                        _viewModel.SetHullComponent(hp, _viewModel.HullMaxRepairPercent);
                }
                else if (e.ColumnIndex == colMaxRepair.Index)
                {
                    if (decimal.TryParse(valStr, out decimal mr))
                        _viewModel.SetHullComponent(_viewModel.HullCurrentHP, mr);
                }
            }
            else if (row.Tag is ShipComponentSlot slot)
            {
                if (e.ColumnIndex == colCondition.Index)
                {
                    if (int.TryParse(valStr, out int hp))
                        _viewModel.SetComponentCondition(slot.SlotType, slot.SlotIndex, hp, slot.MaxRepairPercent);
                }
                else if (e.ColumnIndex == colMaxRepair.Index)
                {
                    if (decimal.TryParse(valStr, out decimal mr))
                        _viewModel.SetComponentCondition(slot.SlotType, slot.SlotIndex, slot.CurrentHP, mr);
                }
            }
        }

        // Munitions tab
        private void UpdateMunitionsTabVisibility()
        {
            if (_viewModel.IsNew)
            {
                tabMunitions.Enabled = false;
                return;
            }

            bool isPlayerOwned = _viewModel.Ownership == StationOwnership.PlayerOwned;
            bool hasWeapons = _viewModel.Components.Any(c =>
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponSmall ||
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponMedium ||
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponLarge);
            tabMunitions.Enabled = isPlayerOwned && hasWeapons;
        }

        private void PopulateMunitionsGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvMunitions.Rows.Clear();
            if (_viewModel.IsNew) return;
            if (!tabMunitions.Enabled) return;

            foreach (var kvp in CollectionSortHelper.OrderItemBagEntries(_viewModel.MunitionsHold.Items))
            {
                var item = kvp.Value;
                int rowIdx = dgvMunitions.Rows.Add(item.ExtendedName, item.Quantity.ToString());
                dgvMunitions.Rows[rowIdx].Tag = item;
            }

            sw.Stop();
            Log.Info("PERF PopulateMunitionsGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmdMunAdd_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew) return;
            string itemName = cmbMunItem.SelectedItem?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtMunQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show(
                    "Enter a valid quantity.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var newItem = new Item(ItemType.ItemTypeEnum.Munition, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                BaseItemTypeID = itemName
            };

            _viewModel.AddMunitionsItem(newItem);
            PopulateMunitionsGrid();
            Log.Info("Added munition: {0} x{1}", itemName, qty);
        }

        private void CmdMunRemove_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew || dgvMunitions.SelectedRows.Count == 0) return;
            var item = dgvMunitions.SelectedRows[0].Tag as Item;
            if (item == null) return;
            _viewModel.RemoveMunitionsItem(item.UUID);
            PopulateMunitionsGrid();
            Log.Info("Removed munition: {0}", item.Name);
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsDirty)
            {
                var result = ShowUnsavedChangesDialog();
                if (result == DialogResult.Yes)
                {
                    if (!TrySave()) return;
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            _viewModel.Reset();
            _viewModel.Name = "New Station";
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                txtName.Text = _viewModel.Name;
                cmbStationType.SelectedIndex = -1;
                cmbOwnership.SelectedIndex = -1;
                dgvHold.Rows.Clear();
                dgvComponents.Rows.Clear();
                rtbStationStats.Text = string.Empty;
                dgvMunitions.Rows.Clear();
            }

            SetDetailEnabled(true);
            txtName.Focus();
            Log.Info("New station form prepared");
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_viewModel.IsNew || string.IsNullOrEmpty(_viewModel.UUID)) return;

            var refCounter = new StationReferenceCounter(
                playerContext.DeliveryRouteList.ToList(),
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_viewModel.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format(
                        "Cannot delete station \"{0}\" \u2014 it is referenced by {1} route stop(s), delivery plan(s), or build item(s).",
                        _viewModel.Name,
                        refs),
                    "Delete Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var confirmResult = MessageBox.Show(
                string.Format("Delete station \"{0}\"?", _viewModel.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (confirmResult != DialogResult.Yes) return;

            _stationService.Delete(_viewModel.UUID);
            _viewModel.Reset();
            PopulateStationList();
            ClearForm();
            Log.Info("Deleted station");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            TrySave();
        }

        private bool TrySave()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            _viewModel.Name = name;

            try
            {
                ReadOnlyStation result;
                if (_viewModel.IsNew)
                {
                    result = _stationService.Create(_viewModel.BuildCreateRequest());
                }
                else
                {
                    result = _stationService.Update(_viewModel.UUID, _viewModel.BuildUpdateRequest());
                }

                _viewModel.LoadFrom(result, playerContext.CurrentPlayerUUID);
                PopulateStationList();
                PopulateForm();
                Log.Info("Saved station \"{0}\"", _viewModel.Name);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to save station");
                MessageBox.Show("Failed to save: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void TxtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            _viewModel.Name = txtName.Text;
        }

        // Unsaved changes dialog
        private DialogResult ShowUnsavedChangesDialog()
        {
            return MessageBox.Show(
                "You have unsaved changes. Save before continuing?",
                "Unsaved Changes",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
        }

        private void RestoreSelection()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            if (!string.IsNullOrEmpty(_viewModel.UUID))
            {
                foreach (ListViewItem item in lvwStations.Items)
                {
                    if (item.Tag is ReadOnlyStation ro && ro.UUID == _viewModel.UUID)
                    {
                        item.Selected = true;
                        return;
                    }
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
            PopulateStationList();
            ClearForm();
        }
    }
}
