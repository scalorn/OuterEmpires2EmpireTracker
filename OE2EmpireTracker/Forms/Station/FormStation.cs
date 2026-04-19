using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.Station
{
    public partial class FormStation : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private Models.Station _selectedStation;

        public FormStation()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwStations.View = View.Details;
            lvwStations.Columns.Add("Name", 120);
            lvwStations.Columns.Add("Type", 50);
            lvwStations.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwStations.FullRowSelect = true;
            lvwStations.MultiSelect = false;
            lvwStations.ItemSelectionChanged += lvwStations_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtName.TextChanged += txtName_TextChanged;
            cmbStationType.SelectedIndexChanged += cmbStationType_SelectedIndexChanged;
            cmbOwnership.SelectedIndexChanged += cmbOwnership_SelectedIndexChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            cmdHoldAdd.Click += cmdHoldAdd_Click;
            cmdHoldRemove.Click += cmdHoldRemove_Click;
            cmbHoldType.SelectedIndexChanged += cmbHoldType_SelectedIndexChanged;
            dgvHold.CellEndEdit += dgvHold_CellEndEdit;

            dgvComponents.CellEndEdit += dgvComponents_CellEndEdit;

            cmdMunAdd.Click += cmdMunAdd_Click;
            cmdMunRemove.Click += cmdMunRemove_Click;

            PopulateStationTypeCombo();
            PopulateOwnershipCombo();
            PopulateHoldTypeCombo();
            PopulateStationList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        // Layout
        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w2 - 232, h - 6);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwStations.Size = new System.Drawing.Size(w2 - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
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
            string selectedUUID = _selectedStation?.UUID;
            lvwStations.Items.Clear();

            var stations = playerContext.GetCurrentPlayerStations();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                stations = stations.Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            stations = stations.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

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

        private void txtFilter_TextChanged(object sender, EventArgs e) { PopulateStationList(); }

        private void lvwStations_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Models.Station station)
            { _selectedStation = station; PopulateForm(); }
            else if (!e.IsSelected && lvwStations.SelectedItems.Count == 0)
            { _selectedStation = null; ClearForm(); }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedStation == null) { ClearForm(); return; }
            txtName.Text = _selectedStation.Name;
            SelectComboEnum(cmbStationType, _selectedStation.StationType);
            SelectComboEnum(cmbOwnership, _selectedStation.Ownership);
            bool isPlayerOwned = _selectedStation.Ownership == StationOwnership.PlayerOwned;
            tabComponents.Enabled = isPlayerOwned;
            UpdateMunitionsTabVisibility();
            PopulateHoldGrid();
            PopulateComponentsGrid();
            PopulateMunitionsGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtName.Text = "";
            cmbStationType.SelectedIndex = -1;
            cmbOwnership.SelectedIndex = -1;
            dgvHold.Rows.Clear();
            dgvComponents.Rows.Clear();
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
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbStationType.Items.Clear();
            foreach (StationType st in Enum.GetValues(typeof(StationType)))
                cmbStationType.Items.Add(st);
        }

        private void PopulateOwnershipCombo()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbOwnership.Items.Clear();
            foreach (StationOwnership so in Enum.GetValues(typeof(StationOwnership)))
                cmbOwnership.Items.Add(so);
        }

        private void SelectComboEnum<T>(ComboBox cmb, T value)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is T v && v.Equals(value))
                { cmb.SelectedIndex = i; return; }
            }
            cmb.SelectedIndex = -1;
        }

        private void cmbStationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedStation == null) return;
            if (cmbStationType.SelectedItem is StationType st)
                _selectedStation.StationType = st;
        }

        private void cmbOwnership_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedStation == null) return;
            if (cmbOwnership.SelectedItem is StationOwnership so)
            {
                _selectedStation.Ownership = so;
                bool isPlayerOwned = so == StationOwnership.PlayerOwned;
                tabComponents.Enabled = isPlayerOwned;
                UpdateMunitionsTabVisibility();
            }
        }

        // Hold tab
        private ItemBag GetStationHold()
        {
            if (_selectedStation == null) return null;
            string playerUUID = playerContext.CurrentPlayerUUID;
            if (string.IsNullOrEmpty(playerUUID)) return null;
            if (!_selectedStation.Holds.TryGetValue(playerUUID, out ItemBag bag))
            {
                bag = new ItemBag();
                _selectedStation.Holds[playerUUID] = bag;
            }
            return bag;
        }

        private void PopulateHoldGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvHold.Rows.Clear();
            var bag = GetStationHold();
            if (bag == null) return;

            foreach (var kvp in bag.Items.OrderBy(k => k.Value.Name))
            {
                var item = kvp.Value;
                int rowIdx = dgvHold.Rows.Add(
                    item.ItemType.ToString(),
                    item.ExtendedName,
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
        }

        private void dgvHold_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedStation == null || e.RowIndex < 0) return;
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
            cmbHoldType.Items.Clear();
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Resource);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Commodity);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Flatpack);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Crate);
            cmbHoldType.Items.Add(ItemType.ItemTypeEnum.Munition);
            cmbHoldType.SelectedIndex = 0;
        }

        private void cmbHoldType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateHoldItemCombo();
            UpdateHoldPurityCombo();
        }

        private void PopulateHoldItemCombo()
        {
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

        private void cmdHoldAdd_Click(object sender, EventArgs e)
        {
            var bag = GetStationHold();
            if (bag == null || _selectedStation == null) return;
            if (!(cmbHoldType.SelectedItem is ItemType.ItemTypeEnum itemType)) return;
            string itemName = cmbHoldItem.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtHoldQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter a valid quantity.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string purity = cmbHoldPurity.Enabled ? (cmbHoldPurity.SelectedItem?.ToString() ?? "") : "";
            var newItem = new Item(itemType, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                ResourcePurity = purity,
                BaseItemTypeID = itemName
            };
            bag.AddItem(newItem);
            PopulateHoldGrid();
            Log.Info("Added hold item: {0} x{1}", itemName, qty);
        }

        private void cmdHoldRemove_Click(object sender, EventArgs e)
        {
            var bag = GetStationHold();
            if (bag == null || dgvHold.SelectedRows.Count == 0) return;
            var item = dgvHold.SelectedRows[0].Tag as Item;
            if (item == null) return;
            bag.Remove(item.UUID);
            PopulateHoldGrid();
            Log.Info("Removed hold item: {0}", item.Name);
        }

        // Components tab
        private void PopulateComponentsGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvComponents.Rows.Clear();
            if (_selectedStation == null) return;
            if (_selectedStation.Ownership != StationOwnership.PlayerOwned) return;

            // Hull row first
            var hullBp = playerContext.FindBlueprint(_selectedStation.StationBlueprintUUID);
            string hullName = hullBp?.ExtendedName ?? "(no hull)";
            int hullRow = dgvComponents.Rows.Add("Hull", hullName,
                _selectedStation.HullCurrentHP.ToString(),
                _selectedStation.HullMaxRepairPercent.ToString());
            dgvComponents.Rows[hullRow].Tag = "hull";
            dgvComponents.Rows[hullRow].Cells[colSlotType.Index].ReadOnly = true;
            dgvComponents.Rows[hullRow].Cells[colComponentName.Index].ReadOnly = true;

            // Component rows
            foreach (var slot in _selectedStation.Components)
            {
                var compBp = playerContext.FindBlueprint(slot.BlueprintUUID);
                string compName = compBp?.ExtendedName ?? slot.BlueprintUUID;
                int rowIdx = dgvComponents.Rows.Add(slot.SlotType, compName,
                    slot.CurrentHP.ToString(),
                    slot.MaxRepairPercent.ToString());
                dgvComponents.Rows[rowIdx].Tag = slot;
                dgvComponents.Rows[rowIdx].Cells[colSlotType.Index].ReadOnly = true;
                dgvComponents.Rows[rowIdx].Cells[colComponentName.Index].ReadOnly = true;
            }
        }

        private void dgvComponents_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedStation == null || e.RowIndex < 0) return;
            var row = dgvComponents.Rows[e.RowIndex];
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? "0";

            if (row.Tag is string s && s == "hull")
            {
                if (e.ColumnIndex == colCondition.Index)
                {
                    if (int.TryParse(valStr, out int hp)) _selectedStation.HullCurrentHP = hp;
                }
                else if (e.ColumnIndex == colMaxRepair.Index)
                {
                    if (decimal.TryParse(valStr, out decimal mr)) _selectedStation.HullMaxRepairPercent = mr;
                }
            }
            else if (row.Tag is ShipComponentSlot slot)
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

        // Munitions tab
        private void UpdateMunitionsTabVisibility()
        {
            if (_selectedStation == null) { tabMunitions.Enabled = false; return; }
            bool isPlayerOwned = _selectedStation.Ownership == StationOwnership.PlayerOwned;
            bool hasWeapons = _selectedStation.Components.Any(c =>
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponSmall ||
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponMedium ||
                c.SlotType == OE2EmpireTracker.Constants.SlotTypes.WeaponLarge);
            tabMunitions.Enabled = isPlayerOwned && hasWeapons;
        }

        private void PopulateMunitionsGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvMunitions.Rows.Clear();
            if (_selectedStation == null) return;
            if (!tabMunitions.Enabled) return;

            foreach (var kvp in _selectedStation.MunitionsHold.Items.OrderBy(k => k.Value.Name))
            {
                var item = kvp.Value;
                int rowIdx = dgvMunitions.Rows.Add(item.ExtendedName, item.Quantity.ToString());
                dgvMunitions.Rows[rowIdx].Tag = item;
            }
        }

        private void cmdMunAdd_Click(object sender, EventArgs e)
        {
            if (_selectedStation == null) return;
            string itemName = cmbMunItem.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtMunQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter a valid quantity.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var newItem = new Item(ItemType.ItemTypeEnum.Munition, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                BaseItemTypeID = itemName
            };
            _selectedStation.MunitionsHold.AddItem(newItem);
            PopulateMunitionsGrid();
            Log.Info("Added munition: {0} x{1}", itemName, qty);
        }

        private void cmdMunRemove_Click(object sender, EventArgs e)
        {
            if (_selectedStation == null || dgvMunitions.SelectedRows.Count == 0) return;
            var item = dgvMunitions.SelectedRows[0].Tag as Item;
            if (item == null) return;
            _selectedStation.MunitionsHold.Remove(item.UUID);
            PopulateMunitionsGrid();
            Log.Info("Removed munition: {0}", item.Name);
        }

        // CRUD
        private void cmdNew_Click(object sender, EventArgs e)
        {
            var station = new Models.Station
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Station",
                OwnerUUID = playerContext.CurrentPlayerUUID
            };
            playerContext.StationList.Add(station);
            playerContext.WriteContext();
            _selectedStation = station;
            PopulateStationList();
            PopulateForm();
            Log.Info("Created new station");
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedStation == null) return;

            var refCounter = new StationReferenceCounter(
                playerContext.DeliveryRouteList.ToList(),
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_selectedStation.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format("Cannot delete station \"{0}\" \u2014 it is referenced by {1} route stop(s), delivery plan(s), or build item(s).",
                        _selectedStation.Name, refs),
                    "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete station \"{0}\"?", _selectedStation.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.StationList.Remove(_selectedStation);
            playerContext.WriteContext();
            _selectedStation = null;
            PopulateStationList();
            ClearForm();
            Log.Info("Deleted station");
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedStation == null) return;
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            { MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _selectedStation.Name = name;
            playerContext.WriteContext();
            PopulateStationList();
            Log.Info("Saved station \"{0}\"", _selectedStation.Name);
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedStation == null) return;
            _selectedStation.Name = txtName.Text;
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            _selectedStation = null;
            PopulateStationList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }
    }
}
