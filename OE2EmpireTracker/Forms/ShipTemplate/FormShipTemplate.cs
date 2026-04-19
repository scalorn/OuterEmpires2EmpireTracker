using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.ShipTemplate
{
    public partial class FormShipTemplate : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private Models.ShipTemplate _selectedTemplate;

        public FormShipTemplate()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwTemplates.View = View.Details;
            lvwTemplates.Columns.Add("Name", 200);
            lvwTemplates.FullRowSelect = true;
            lvwTemplates.MultiSelect = false;
            lvwTemplates.ItemSelectionChanged += lvwTemplates_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtName.TextChanged += txtName_TextChanged;
            cmbHull.SelectedIndexChanged += cmbHull_SelectedIndexChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            dgvSlots.CellValueChanged += dgvSlots_CellValueChanged;
            dgvSlots.CurrentCellDirtyStateChanged += dgvSlots_CurrentCellDirtyStateChanged;

            PopulateHullCombo();
            PopulateTemplateList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        // Layout
        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new System.Drawing.Size(220, h - 6);
            flpDetail.Size = new System.Drawing.Size(w - 232, h - 6);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwTemplates.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int statsHeight = 185;
            int gridHeight = h - flpName.Height - flpHull.Height - cmdSave.Height - statsHeight - 30;
            if (gridHeight < 50) gridHeight = 50;
            dgvSlots.Size = new System.Drawing.Size(w - 6, gridHeight);
            rtbStats.Size = new System.Drawing.Size(w - 6, statsHeight);
        }

        // Template List
        private void PopulateTemplateList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedTemplate?.UUID;
            lvwTemplates.Items.Clear();

            var templates = playerContext.ShipTemplateList
                .Where(t => t.OwnerUUID == playerContext.CurrentPlayerUUID).ToList();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                templates = templates.Where(t => t.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            templates = templates.OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var tmpl in templates)
            {
                var item = new ListViewItem(tmpl.Name) { Tag = tmpl };
                lvwTemplates.Items.Add(item);
                if (tmpl.UUID == selectedUUID) item.Selected = true;
            }
            sw.Stop();
            Log.Info("PERF PopulateTemplateList: {0}ms items={1}", sw.ElapsedMilliseconds, templates.Count);
        }

        private void txtFilter_TextChanged(object sender, EventArgs e) { PopulateTemplateList(); }

        private void lvwTemplates_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is Models.ShipTemplate tmpl)
            { _selectedTemplate = tmpl; PopulateForm(); }
            else if (!e.IsSelected && lvwTemplates.SelectedItems.Count == 0)
            { _selectedTemplate = null; ClearForm(); }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedTemplate == null) { ClearForm(); return; }
            txtName.Text = _selectedTemplate.Name;
            SelectHullInCombo(_selectedTemplate.HullBlueprintUUID);
            PopulateSlotGrid();
            RefreshStats();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtName.Text = "";
            cmbHull.SelectedIndex = -1;
            dgvSlots.Rows.Clear();
            rtbStats.Text = "";
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtName.Enabled = enabled;
            cmbHull.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvSlots.Enabled = enabled;
        }

        // Hull Combo
        private void PopulateHullCombo()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbHull.Items.Clear();
            var hulls = playerContext.GetAllBlueprints()
                .Where(bp => bp.BluePrintType == "Hull")
                .OrderBy(bp => bp.ExtendedName);
            foreach (var bp in hulls)
                cmbHull.Items.Add(new HullEntry { Display = bp.ExtendedName, UUID = bp.UUID });
        }

        private void SelectHullInCombo(string hullUUID)
        {
            if (string.IsNullOrEmpty(hullUUID)) { cmbHull.SelectedIndex = -1; return; }
            for (int i = 0; i < cmbHull.Items.Count; i++)
            {
                if (cmbHull.Items[i] is HullEntry he && he.UUID == hullUUID)
                { cmbHull.SelectedIndex = i; return; }
            }
            cmbHull.SelectedIndex = -1;
        }

        private void cmbHull_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedTemplate == null) return;
            var entry = cmbHull.SelectedItem as HullEntry;
            _selectedTemplate.HullBlueprintUUID = entry?.UUID ?? "";
            _selectedTemplate.Components.Clear();
            PopulateSlotGrid();
            RefreshStats();
        }

        // Slot Grid
        private void PopulateSlotGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvSlots.Rows.Clear();
            if (_selectedTemplate == null) return;

            var hullBp = playerContext.FindBlueprint(_selectedTemplate.HullBlueprintUUID);
            if (hullBp?.Properties == null) return;

            var slotDefs = GetSlotDefinitions(hullBp);
            foreach (var def in slotDefs)
            {
                for (int idx = 0; idx < def.MaxCount; idx++)
                {
                    var existing = _selectedTemplate.Components
                        .FirstOrDefault(c => c.SlotType == def.SlotType && c.SlotIndex == idx);

                    int rowIdx = dgvSlots.Rows.Add(def.SlotType, idx);
                    var row = dgvSlots.Rows[rowIdx];

                    // Populate component combo for this row
                    var comboCell = (DataGridViewComboBoxCell)row.Cells[colComponent.Index];
                    comboCell.Items.Clear();
                    comboCell.Items.Add("(empty)");
                    var eligibleBps = playerContext.GetAllBlueprints()
                        .Where(bp => bp.BluePrintType == def.BlueprintType)
                        .OrderBy(bp => bp.ExtendedName);
                    foreach (var bp in eligibleBps)
                        comboCell.Items.Add(bp.ExtendedName + "|" + bp.UUID);

                    if (existing != null && !string.IsNullOrEmpty(existing.BlueprintUUID))
                    {
                        var compBp = playerContext.FindBlueprint(existing.BlueprintUUID);
                        string display = (compBp?.ExtendedName ?? existing.BlueprintUUID) + "|" + existing.BlueprintUUID;
                        if (!comboCell.Items.Contains(display)) comboCell.Items.Add(display);
                        comboCell.Value = display;
                    }
                    else
                    {
                        comboCell.Value = "(empty)";
                    }

                    row.Tag = new SlotInfo { SlotType = def.SlotType, SlotIndex = idx };
                }
            }
        }

        private void dgvSlots_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgvSlots.IsCurrentCellDirty)
                dgvSlots.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void dgvSlots_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || e.RowIndex < 0) return;
            if (e.ColumnIndex != colComponent.Index) return;
            if (_selectedTemplate == null) return;

            var row = dgvSlots.Rows[e.RowIndex];
            var info = row.Tag as SlotInfo;
            if (info == null) return;

            string val = row.Cells[colComponent.Index].Value?.ToString() ?? "(empty)";
            string bpUUID = "";
            if (val != "(empty)" && val.Contains("|"))
                bpUUID = val.Substring(val.LastIndexOf('|') + 1);

            var existing = _selectedTemplate.Components
                .FirstOrDefault(c => c.SlotType == info.SlotType && c.SlotIndex == info.SlotIndex);

            if (string.IsNullOrEmpty(bpUUID))
            {
                if (existing != null) _selectedTemplate.Components.Remove(existing);
            }
            else
            {
                if (existing == null)
                {
                    existing = new ShipComponentSlot { SlotType = info.SlotType, SlotIndex = info.SlotIndex };
                    _selectedTemplate.Components.Add(existing);
                }
                existing.BlueprintUUID = bpUUID;
            }

            RefreshStats();
        }

        // Stats
        private void RefreshStats()
        {
            if (_selectedTemplate == null) { rtbStats.Text = ""; return; }
            var hullBp = playerContext.FindBlueprint(_selectedTemplate.HullBlueprintUUID);
            if (hullBp == null) { rtbStats.Text = "Select a hull blueprint."; return; }

            var stats = ShipBuildService.ComputeStats(hullBp, _selectedTemplate.Components,
                uuid => playerContext.FindBlueprint(uuid));

            rtbStats.Text = string.Format(
                "Mass: {0}  |  Power: {1}/{2} (Balance: {3})\n" +
                "Cargo: {4}  |  Fuel: {5}  |  Hopper: {6}\n" +
                "Health: {7}  |  Shield: {8} (Regen: {9})\n" +
                "Defence — Energy: {10}  Kinetic: {11}  Missile: {12}\n" +
                "Accel: {13}  |  Rotation: {14}  |  Jump: {15} (Fuel/Jump: {16})\n" +
                "Mining Yield: {17}  |  Scan Level: {18}",
                stats.TotalMass, stats.PowerGenerated, stats.PowerConsumed, stats.PowerBalance,
                stats.CargoCapacity, stats.FuelCapacity, stats.HopperCapacity,
                stats.TotalHealth, stats.ShieldHitpoints, stats.ShieldRegen,
                stats.EnergyDefence, stats.KineticDefence, stats.MissileDefence,
                stats.Acceleration, stats.RotationalThrust, stats.MaxJumpDistance, stats.FuelPerJump,
                stats.MiningYield, stats.ScanLevel);
        }

        // CRUD
        private void cmdNew_Click(object sender, EventArgs e)
        {
            var tmpl = new Models.ShipTemplate
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Template",
                OwnerUUID = playerContext.CurrentPlayerUUID
            };
            playerContext.ShipTemplateList.Add(tmpl);
            playerContext.WriteContext();
            _selectedTemplate = tmpl;
            PopulateTemplateList();
            PopulateForm();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;
            var result = MessageBox.Show(
                string.Format("Delete template '{0}'?", _selectedTemplate.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.ShipTemplateList.Remove(_selectedTemplate);
            playerContext.WriteContext();
            _selectedTemplate = null;
            PopulateTemplateList();
            ClearForm();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            { MessageBox.Show("Name cannot be empty.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            _selectedTemplate.Name = name;
            playerContext.WriteContext();
            PopulateTemplateList();
            Log.Info("Saved template '{0}'", _selectedTemplate.Name);
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedTemplate == null) return;
            _selectedTemplate.Name = txtName.Text;
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            _selectedTemplate = null;
            PopulateHullCombo();
            PopulateTemplateList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // Helpers
        private List<SlotDefinition> GetSlotDefinitions(Models.Blueprint hullBp)
        {
            var defs = new List<SlotDefinition>();
            AddSlotDef(defs, hullBp, "Max Reactors", "Reactor", "Reactor");
            AddSlotDef(defs, hullBp, "Max Main Drives", "MainDrive", "MainDrive");
            AddSlotDef(defs, hullBp, "Max Thrusters", "Thruster", "Thruster");
            AddSlotDef(defs, hullBp, "Max Cargo Pods", "CargoPod", "CargoPod");
            AddSlotDef(defs, hullBp, "Max Fuel Tanks", "FuelTank", "FuelTank");
            AddSlotDef(defs, hullBp, "Max Shields", "Shield", "Shield");
            AddSlotDef(defs, hullBp, "Max Jump Drives", "JumpDrive", "JumpDrive");
            AddSlotDef(defs, hullBp, "Max Small Weapons", "SmallWeapon", "SmallWeapon");
            AddSlotDef(defs, hullBp, "Max Medium Weapons", "MediumWeapon", "MediumWeapon");
            AddSlotDef(defs, hullBp, "Max Large Weapons", "LargeWeapon", "LargeWeapon");
            AddSlotDef(defs, hullBp, "Max Hull Plating", "HullPlating", "HullPlating");
            AddSlotDef(defs, hullBp, "Max Hull Reinforcement", "HullReinforcement", "HullReinforcement");
            AddSlotDef(defs, hullBp, "Max Mining Lasers", "MiningLaser", Constants.BlueprintTypes.MiningLaser);
            AddSlotDef(defs, hullBp, "Max Ore Hoppers", "OreHopper", Constants.BlueprintTypes.OreHopper);
            AddSlotDef(defs, hullBp, "Max Nav Comps", "NavComp", "NavComp");
            return defs;
        }

        private void AddSlotDef(List<SlotDefinition> defs, Models.Blueprint hullBp,
            string propName, string slotType, string blueprintType)
        {
            decimal maxVal;
            if (hullBp.Properties.getDecimal(propName, 0m, out maxVal) && maxVal > 0)
                defs.Add(new SlotDefinition { SlotType = slotType, MaxCount = (int)maxVal, BlueprintType = blueprintType });
        }

        private class HullEntry { public string Display; public string UUID; public override string ToString() => Display; }
        private class SlotInfo { public string SlotType; public int SlotIndex; }
        private class SlotDefinition { public string SlotType; public int MaxCount; public string BlueprintType; }
    }
}
