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
            lvwTemplates.Columns.Add("Name", 160);
            lvwTemplates.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwTemplates.FullRowSelect = true;
            lvwTemplates.MultiSelect = false;
            lvwTemplates.ItemSelectionChanged += lvwTemplates_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtName.TextChanged += txtName_TextChanged;
            cmbHull.SelectedIndexChanged += cmbHull_SelectedIndexChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;
            cmdOrderBuild.Click += cmdOrderBuild_Click;

            dgvSlots.CellValueChanged += dgvSlots_CellValueChanged;
            dgvSlots.CurrentCellDirtyStateChanged += dgvSlots_CurrentCellDirtyStateChanged;
            dgvSlots.DataError += dgvSlots_DataError;

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

            var refCounter = new ShipTemplateReferenceCounter(
                playerContext.SnapshotShipList(),
                playerContext.GetCurrentPlayerBuildPlans());

            foreach (var tmpl in templates)
            {
                int refs = refCounter.CountReferences(tmpl.UUID);
                var item = new ListViewItem(tmpl.Name) { Tag = tmpl };
                item.SubItems.Add(refs.ToString());
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbHull.Items.Clear();
            var hulls = playerContext.GetAllBlueprints()
                .Where(bp => bp.BluePrintType == "Hull")
                .OrderBy(bp => bp.ExtendedName);
            foreach (var bp in hulls)
                cmbHull.Items.Add(new HullEntry { Display = bp.ExtendedName, UUID = bp.UUID });
            sw.Stop(); Log.Info("PERF PopulateHullCombo: {0}ms", sw.ElapsedMilliseconds);
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvSlots.Rows.Clear();
            if (_selectedTemplate == null) { sw.Stop(); return; }

            var hullBp = playerContext.FindBlueprint(_selectedTemplate.HullBlueprintUUID);
            if (hullBp?.Properties == null) return;

            var slotDefs = GetSlotDefinitions(hullBp);
            int hullClass = hullBp.Class;
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
                    comboCell.ValueType = typeof(ComponentEntry);
                    comboCell.Items.Add(new ComponentEntry { Display = "(empty)", UUID = "" });
                    var eligibleBps = playerContext.GetAllBlueprints()
                        .Where(bp => def.BlueprintTypes.Contains(bp.BluePrintType) && bp.Class == hullClass)
                        .OrderBy(bp => bp.ExtendedName);
                    foreach (var bp in eligibleBps)
                        comboCell.Items.Add(new ComponentEntry { Display = bp.ExtendedName, UUID = bp.UUID });

                    if (existing != null && !string.IsNullOrEmpty(existing.BlueprintUUID))
                    {
                        bool found = false;
                        foreach (ComponentEntry item in comboCell.Items)
                        {
                            if (item.UUID == existing.BlueprintUUID)
                            { comboCell.Value = item; found = true; break; }
                        }
                        if (!found)
                        {
                            var compBp = playerContext.FindBlueprint(existing.BlueprintUUID);
                            var fallback = new ComponentEntry
                            {
                                Display = compBp?.ExtendedName ?? existing.BlueprintUUID,
                                UUID = existing.BlueprintUUID
                            };
                            comboCell.Items.Add(fallback);
                            comboCell.Value = fallback;
                        }
                    }
                    else
                    {
                        comboCell.Value = comboCell.Items[0];
                    }

                    row.Tag = new SlotInfo { SlotType = def.SlotType, SlotIndex = idx };
                }
            }
            sw.Stop(); Log.Info("PERF PopulateSlotGrid: {0}ms", sw.ElapsedMilliseconds);
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

            string bpUUID = "";
            var cellValue = row.Cells[colComponent.Index].Value;
            if (cellValue is ComponentEntry ce && !string.IsNullOrEmpty(ce.UUID))
                bpUUID = ce.UUID;

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

        private void dgvSlots_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            Log.Warn("dgvSlots DataError at [{0},{1}]: {2}", e.RowIndex, e.ColumnIndex, e.Exception?.Message);
            e.ThrowException = false;
        }

        // Stats
        private void RefreshStats()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (_selectedTemplate == null) { rtbStats.Text = ""; sw.Stop(); return; }
            var hullBp = playerContext.FindBlueprint(_selectedTemplate.HullBlueprintUUID);
            if (hullBp == null) { rtbStats.Text = "Select a hull blueprint."; sw.Stop(); return; }

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
            sw.Stop(); Log.Info("PERF RefreshStats: {0}ms", sw.ElapsedMilliseconds);
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
            playerContext.AddShipTemplate(tmpl);
            playerContext.WriteContext();
            _selectedTemplate = tmpl;
            PopulateTemplateList();
            PopulateForm();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;

            var refCounter = new ShipTemplateReferenceCounter(
                playerContext.SnapshotShipList(),
                playerContext.GetCurrentPlayerBuildPlans());
            int refs = refCounter.CountReferences(_selectedTemplate.UUID);
            if (refs > 0)
            {
                MessageBox.Show(
                    string.Format("Cannot delete template '{0}' — it is referenced by {1} ship(s) or build item(s).",
                        _selectedTemplate.Name, refs),
                    "Delete Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                string.Format("Delete template '{0}'?", _selectedTemplate.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.RemoveShipTemplate(_selectedTemplate);
            playerContext.WriteContext();
            _selectedTemplate = null;
            PopulateTemplateList();
            ClearForm();
            Log.Info("Deleted template");
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

        // -------------------------------------------------------------------
        // Order Build (18.4)
        // -------------------------------------------------------------------

        private void cmdOrderBuild_Click(object sender, EventArgs e)
        {
            if (_selectedTemplate == null) return;
            if (string.IsNullOrEmpty(_selectedTemplate.HullBlueprintUUID))
            {
                MessageBox.Show("Select a hull blueprint first.", "No Hull",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Log.Info("cmdOrderBuild_Click: template={0} uuid={1}",
                _selectedTemplate.Name, _selectedTemplate.UUID);

            // Prompt for quantity
            int quantity = ShowQuantityDialog();
            if (quantity <= 0) return;

            // Prompt for assembly station
            var station = ShowStationPickerDialog();
            if (station == null) return;

            // Validate assembly location
            var hullBp = playerContext.FindBlueprint(_selectedTemplate.HullBlueprintUUID);
            if (hullBp != null)
            {
                decimal shipClassVal = 0m;
                hullBp.Properties?.getDecimal("Class", 0m, out shipClassVal);
                int shipClass = (int)shipClassVal;
                string validationError = ShipBuildService.ValidateAssemblyLocation(shipClass, station.StationType);
                if (validationError != null)
                {
                    MessageBox.Show(validationError, "Invalid Assembly Location",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // Generate build items
            var items = ShipBuildService.GenerateShipBuildItems(
                _selectedTemplate, quantity,
                DestinationType.Station, station.UUID,
                uuid => playerContext.FindBlueprint(uuid),
                uuid => 0);

            if (items.Count == 0)
            {
                MessageBox.Show("No build items needed (all components in stock).",
                    "Order Build", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Show plan picker
            BuildPlan targetPlan = ShowBuildPlanPickerDialog();
            if (targetPlan == null) return;

            // Add items to plan
            foreach (var item in items)
            {
                item.ShipTemplateUUID = _selectedTemplate.UUID;
                targetPlan.Items.Add(item);
            }

            // Persist
            if (!playerContext.BuildPlanList.Contains(targetPlan))
                playerContext.AddBuildPlan(targetPlan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(targetPlan.UUID);

            Log.Info("cmdOrderBuild_Click: {0} items added to plan '{1}'",
                items.Count, targetPlan.Name);

            MessageBox.Show(
                string.Format("{0} build items for {1}x '{2}' added to plan '{3}'.",
                    items.Count, quantity, _selectedTemplate.Name, targetPlan.Name),
                "Order Build", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                MessageBox.Show("No stations available. Create a station first.",
                    "No Stations", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    string planName = string.Format("{0} - Ship Build", _selectedTemplate.Name ?? "Ship");
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
                if (hullBp.Properties.getDecimal(kvp.Key, 0m, out maxVal) && maxVal > 0)
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

        private class HullEntry { public string Display; public string UUID; public override string ToString() => Display; }
        private class SlotInfo { public string SlotType; public int SlotIndex; }
        private class SlotDefinition { public string SlotType; public int MaxCount; public List<string> BlueprintTypes; }
        private struct ComponentEntry
        {
            public string Display;
            public string UUID;
            public override string ToString() => Display;
        }
    }
}
