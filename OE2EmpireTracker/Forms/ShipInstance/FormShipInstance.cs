using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.ShipInstance
{
    public partial class FormShipInstance : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private Ship _selectedShip;

        public FormShipInstance()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwShips.View = View.Details;
            lvwShips.Columns.Add("Name", 160);
            lvwShips.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwShips.FullRowSelect = true;
            lvwShips.MultiSelect = false;
            lvwShips.ItemSelectionChanged += lvwShips_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtName.TextChanged += txtName_TextChanged;
            cmbLocationType.SelectedIndexChanged += cmbLocationType_SelectedIndexChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;
            cmdFromTemplate.Click += cmdFromTemplate_Click;
            cmdSwapComponent.Click += cmdSwapComponent_Click;

            rbCargoHold.CheckedChanged += rbCargo_CheckedChanged;
            rbHopper.CheckedChanged += rbCargo_CheckedChanged;
            cmdAddItem.Click += cmdAddItem_Click;
            cmdRemoveItem.Click += cmdRemoveItem_Click;
            dgvCargo.SelectionChanged += dgvCargo_SelectionChanged;
            cmbAddType.SelectedIndexChanged += cmbAddType_SelectedIndexChanged;

            dgvComponents.CellEndEdit += dgvComponents_CellEndEdit;

            PopulateLocationTypeCombo();
            PopulateAddTypeCombo();
            PopulateShipList();
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
            lvwShips.Size = new System.Drawing.Size(w2 - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w2 = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int tabHeight = h - flpName.Height - flpLocation.Height - cmdSave.Height - 24;
            if (tabHeight < 100) tabHeight = 100;
            tabControl.Size = new System.Drawing.Size(w2 - 6, tabHeight);
        }

        // Ship List
        private void PopulateShipList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedShip?.UUID;
            lvwShips.Items.Clear();

            var ships = playerContext.GetCurrentPlayerShips();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                ships = ships.Where(s => s.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            ships = ships.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

            var refCounter = new ShipReferenceCounter(
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());

            foreach (var ship in ships)
            {
                int refs = refCounter.CountReferences(ship.UUID);
                var item = new ListViewItem(ship.Name) { Tag = ship };
                item.SubItems.Add(refs.ToString());
                lvwShips.Items.Add(item);
                if (ship.UUID == selectedUUID) item.Selected = true;
            }
            sw.Stop();
            Log.Info("PERF PopulateShipList: {0}ms items={1}", sw.ElapsedMilliseconds, ships.Count);
        }

        private void txtFilter_TextChanged(object sender, EventArgs e) { PopulateShipList(); }

        private void lvwShips_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Ship ship)
            { _selectedShip = ship; PopulateForm(); }
            else if (!e.IsSelected && lvwShips.SelectedItems.Count == 0)
            { _selectedShip = null; ClearForm(); }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedShip == null) { ClearForm(); return; }
            txtName.Text = _selectedShip.Name;
            SelectLocationType(_selectedShip.LocationType);
            PopulateLocationUUIDCombo(_selectedShip.LocationType);
            SelectLocationUUID(_selectedShip.LocationUUID);
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
            txtName.Text = "";
            cmbLocationType.SelectedIndex = -1;
            cmbLocationUUID.Items.Clear();
            dgvComponents.Rows.Clear();
            rtbStats.Text = "";
            dgvCargo.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
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
            sw.Stop(); Log.Info("PERF PopulateLocationTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectLocationType(DestinationType dt)
        {
            for (int i = 0; i < cmbLocationType.Items.Count; i++)
            {
                if ((DestinationType)cmbLocationType.Items[i] == dt)
                { cmbLocationType.SelectedIndex = i; return; }
            }
            cmbLocationType.SelectedIndex = -1;
        }

        private void cmbLocationType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedShip == null) return;
            if (cmbLocationType.SelectedItem is DestinationType dt)
            {
                _selectedShip.LocationType = dt;
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
            sw.Stop(); Log.Info("PERF PopulateLocationUUIDCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void SelectLocationUUID(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) { cmbLocationUUID.SelectedIndex = -1; return; }
            for (int i = 0; i < cmbLocationUUID.Items.Count; i++)
            {
                if (cmbLocationUUID.Items[i] is LocationEntry le && le.UUID == uuid)
                { cmbLocationUUID.SelectedIndex = i; return; }
            }
            cmbLocationUUID.SelectedIndex = -1;
        }

        // Overview tab — component grid
        private void PopulateOverviewGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvComponents.Rows.Clear();
            if (_selectedShip == null) { sw.Stop(); return; }

            // Hull row first
            var hullBp = playerContext.FindBlueprint(_selectedShip.HullBlueprintUUID);
            string hullName = hullBp?.ExtendedName ?? "(no hull)";
            int hullRow = dgvComponents.Rows.Add("Hull", hullName,
                _selectedShip.HullCurrentHP.ToString(),
                _selectedShip.HullMaxRepairPercent.ToString());
            dgvComponents.Rows[hullRow].Tag = "hull";
            dgvComponents.Rows[hullRow].Cells[colSlotType.Index].ReadOnly = true;
            dgvComponents.Rows[hullRow].Cells[colComponentName.Index].ReadOnly = true;

            // Component rows
            foreach (var slot in _selectedShip.Components)
            {
                var compBp = playerContext.FindBlueprint(slot.BlueprintUUID);
                string compName = compBp?.ExtendedName ?? "(unknown)";
                int rowIdx = dgvComponents.Rows.Add(slot.SlotType, compName,
                    slot.CurrentHP.ToString(),
                    slot.MaxRepairPercent.ToString());
                dgvComponents.Rows[rowIdx].Tag = slot;
                dgvComponents.Rows[rowIdx].Cells[colSlotType.Index].ReadOnly = true;
                dgvComponents.Rows[rowIdx].Cells[colComponentName.Index].ReadOnly = true;
            }
            sw.Stop(); Log.Info("PERF PopulateOverviewGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void dgvComponents_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedShip == null || e.RowIndex < 0) return;
            var row = dgvComponents.Rows[e.RowIndex];
            string valStr = row.Cells[e.ColumnIndex].Value?.ToString() ?? "0";

            if (row.Tag is string s && s == "hull")
            {
                if (e.ColumnIndex == colCondition.Index)
                {
                    if (int.TryParse(valStr, out int hp)) _selectedShip.HullCurrentHP = hp;
                }
                else if (e.ColumnIndex == colMaxRepair.Index)
                {
                    if (decimal.TryParse(valStr, out decimal mr)) _selectedShip.HullMaxRepairPercent = mr;
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

        private void RefreshStats()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_selectedShip == null) { rtbStats.Text = ""; sw.Stop(); return; }
            var hullBp = playerContext.FindBlueprint(_selectedShip.HullBlueprintUUID);
            if (hullBp == null) { rtbStats.Text = "No hull blueprint."; sw.Stop(); return; }

            var stats = ShipBuildService.ComputeStats(hullBp, _selectedShip.Components,
                uuid => playerContext.FindBlueprint(uuid));

            rtbStats.Text = string.Format(
                "Mass: {0}  |  Power: {1}/{2} (Balance: {3})\n" +
                "Cargo: {4}  |  Fuel: {5}  |  Hopper: {6}\n" +
                "Health: {7}  |  Shield: {8} (Regen: {9})\n" +
                "Defence \u2014 Energy: {10}  Kinetic: {11}  Missile: {12}\n" +
                "Accel: {13}  |  Rotation: {14}  |  Jump: {15} (Fuel/Jump: {16})\n" +
                "Mining Yield: {17}  |  Scan Level: {18}",
                stats.TotalMass, stats.PowerGenerated, stats.PowerConsumed, stats.PowerBalance,
                stats.CargoCapacity, stats.FuelCapacity, stats.HopperCapacity,
                stats.TotalHealth, stats.ShieldHitpoints, stats.ShieldRegen,
                stats.EnergyDefence, stats.KineticDefence, stats.MissileDefence,
                stats.Acceleration, stats.RotationalThrust, stats.MaxJumpDistance, stats.FuelPerJump,
                stats.MiningYield, stats.ScanLevel);
            sw.Stop(); Log.Info("PERF RefreshStats: {0}ms", sw.ElapsedMilliseconds);
        }

        private void cmdSwapComponent_Click(object sender, EventArgs e)
        {
            if (_selectedShip == null || dgvComponents.SelectedRows.Count == 0) return;
            var row = dgvComponents.SelectedRows[0];
            if (!(row.Tag is ShipComponentSlot slot)) return;

            var blueprints = playerContext.GetAllBlueprints()
                .Where(bp => bp.BluePrintType == slot.SlotType)
                .OrderBy(bp => bp.ExtendedName)
                .ToList();

            if (blueprints.Count == 0)
            {
                MessageBox.Show(string.Format("No blueprints found for slot type \"{0}\".", slot.SlotType),
                    "No Blueprints", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var items = blueprints.Select(bp => bp.ExtendedName).ToArray();
            using (var dlg = new Form())
            {
                dlg.Text = "Swap Component \u2014 " + slot.SlotType;
                dlg.Size = new System.Drawing.Size(400, 350);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lb = new ListBox { Dock = DockStyle.Fill };
                lb.Items.AddRange(items);
                var btnOk = new Button { Text = "OK", DialogResult = DialogResult.OK, Dock = DockStyle.Bottom };
                dlg.Controls.Add(lb);
                dlg.Controls.Add(btnOk);
                dlg.AcceptButton = btnOk;

                if (dlg.ShowDialog(this) == DialogResult.OK && lb.SelectedIndex >= 0)
                {
                    var selected = blueprints[lb.SelectedIndex];
                    slot.BlueprintUUID = selected.UUID;
                    PopulateOverviewGrid();
                    RefreshStats();
                    Log.Info("Swapped component in slot {0} to {1}", slot.SlotType, selected.ExtendedName);
                }
            }
        }

        // Cargo tab
        private ItemBag GetSelectedBag()
        {
            if (_selectedShip == null) return null;
            return rbHopper.Checked ? _selectedShip.Hopper : _selectedShip.Cargo;
        }

        private void rbCargo_CheckedChanged(object sender, EventArgs e)
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
            lblCrateContents.Text = "";
            dgvCrateContents.Visible = false;
            lblCrateContents.Visible = false;
            var bag = GetSelectedBag();
            if (bag == null) { sw.Stop(); return; }

            foreach (var kvp in bag.Items.OrderBy(k => k.Value.Name))
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
            sw.Stop(); Log.Info("PERF PopulateCargoGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private void dgvCargo_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvCargo.SelectedRows.Count == 0) { ClearCrateContents(); return; }
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
            foreach (var kvp in crate.Contents.Items.OrderBy(k => k.Value.Name))
            {
                var item = kvp.Value;
                dgvCrateContents.Rows.Add(item.ItemType.ToString(), item.ExtendedName, item.ResourcePurity, item.Quantity.ToString());
            }
            sw.Stop(); Log.Info("PERF PopulateCrateContents: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearCrateContents()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvCrateContents.Rows.Clear();
            lblCrateContents.Text = "";
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
            sw.Stop(); Log.Info("PERF PopulateAddTypeCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void cmbAddType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateAddItemCombo();
            UpdatePurityComboForHopper();
        }

        private void PopulateAddItemCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbAddItem.Items.Clear();
            if (!(cmbAddType.SelectedItem is ItemType.ItemTypeEnum selectedType)) return;

            if (selectedType == ItemType.ItemTypeEnum.Resource)
            {
                var resources = EmpireContext.GetInstance()?.ResourceList;
                if (resources != null)
                {
                    foreach (var r in resources.OrderBy(r => r.Name))
                        cmbAddItem.Items.Add(r.Name);
                }
            }
            else if (selectedType == ItemType.ItemTypeEnum.Commodity)
            {
                foreach (var c in Commodity.ResourceMapByEnum.Values.OrderBy(c => c.ExtendedName))
                    cmbAddItem.Items.Add(c.ExtendedName);
            }
            if (cmbAddItem.Items.Count > 0) cmbAddItem.SelectedIndex = 0;
            sw.Stop(); Log.Info("PERF PopulateAddItemCombo: {0}ms", sw.ElapsedMilliseconds);
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

        private void cmdAddItem_Click(object sender, EventArgs e)
        {
            var bag = GetSelectedBag();
            if (bag == null || _selectedShip == null) return;

            if (!(cmbAddType.SelectedItem is ItemType.ItemTypeEnum itemType)) return;
            string itemName = cmbAddItem.SelectedItem?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(itemName)) return;
            if (!int.TryParse(txtAddQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter a valid quantity.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string purity = cmbAddPurity.Enabled ? (cmbAddPurity.SelectedItem?.ToString() ?? "") : "";

            var newItem = new Item(itemType, itemName)
            {
                UUID = Guid.NewGuid().ToString(),
                Quantity = qty,
                ResourcePurity = purity,
                BaseItemTypeID = itemName
            };
            bag.AddItem(newItem);
            PopulateCargoGrid();
            Log.Info("Added cargo item: {0} x{1} to {2}",
                itemName, qty, rbHopper.Checked ? "Hopper" : "Cargo");
        }

        private void cmdRemoveItem_Click(object sender, EventArgs e)
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
        private void cmdFromTemplate_Click(object sender, EventArgs e)
        {
            var templates = playerContext.GetCurrentPlayerShipTemplates();
            if (templates.Count == 0)
            {
                MessageBox.Show("No ship templates available. Create a template first.",
                    "No Templates", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                foreach (var t in templates.OrderBy(t => t.Name))
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

                var ship = new Ship
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = tmpl.Name,
                    OwnerUUID = playerContext.CurrentPlayerUUID,
                    TemplateUUID = tmpl.UUID,
                    HullBlueprintUUID = tmpl.HullBlueprintUUID,
                    Components = tmpl.Components.Select(c => new ShipComponentSlot
                    {
                        SlotType = c.SlotType,
                        SlotIndex = c.SlotIndex,
                        BlueprintUUID = c.BlueprintUUID,
                        CurrentHP = c.CurrentHP,
                        MaxHP = c.MaxHP,
                        MaxRepairPercent = c.MaxRepairPercent
                    }).ToList()
                };
                playerContext.AddShip(ship);
                playerContext.WriteContext();
                _selectedShip = ship;
                PopulateShipList();
                PopulateForm();
                Log.Info("Created ship \"{0}\" from template \"{1}\"", ship.Name, tmpl.Name);
            }
        }

        // CRUD
        private void cmdNew_Click(object sender, EventArgs e)
        {
            // Ships always need a hull — route through the template picker
            cmdFromTemplate_Click(sender, e);
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedShip == null) return;

            var refCounter = new ShipReferenceCounter(
                playerContext.GetCurrentPlayerPlans(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_selectedShip.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format("Cannot delete ship \"{0}\" — it is referenced by {1} delivery plan(s) or build item(s).",
                        _selectedShip.Name, refs),
                    "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete ship \"{0}\"?", _selectedShip.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.RemoveShip(_selectedShip);
            playerContext.WriteContext();
            _selectedShip = null;
            PopulateShipList();
            ClearForm();
            Log.Info("Deleted ship");
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedShip == null) return;
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            { MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _selectedShip.Name = name;
            if (cmbLocationUUID.SelectedItem is LocationEntry le)
                _selectedShip.LocationUUID = le.UUID;
            playerContext.WriteContext();
            PopulateShipList();
            Log.Info("Saved ship \"{0}\"", _selectedShip.Name);
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedShip == null) return;
            _selectedShip.Name = txtName.Text;
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            _selectedShip = null;
            PopulateShipList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // Helpers
        private class LocationEntry { public string Display; public string UUID; public override string ToString() => Display; }
    }
}
