using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.StockTargets
{
    public partial class FormStockTargets : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private PlayerContext playerContext;

        private StockPlan _selectedPlan;

        private StockProfile _selectedProfile;

        public FormStockTargets()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 140);
            lvwPlans.Columns.Add("Active", 50);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += LvwPlans_ItemSelectionChanged;

            txtFilter.TextChanged += TxtFilter_TextChanged;
            txtPlanName.TextChanged += TxtPlanName_TextChanged;
            chkActive.CheckedChanged += ChkActive_CheckedChanged;
            cmbReplenishmentPlan.SelectedIndexChanged += CmbReplenishmentPlan_SelectedIndexChanged;

            cmdNew.Click += CmdNew_Click;
            cmdDelete.Click += CmdDelete_Click;
            cmdSave.Click += CmdSave_Click;
            cmdAddTarget.Click += CmdAddTarget_Click;
            cmdRemoveTarget.Click += CmdRemoveTarget_Click;
            cmdQuickAdd.Click += CmdQuickAdd_Click;
            cmdCheckGenerate.Click += CmdCheckGenerate_Click;

            cmbScope.SelectedIndexChanged += CmbScope_SelectedIndexChanged;
            cmbTargetType.SelectedIndexChanged += CmbTargetType_SelectedIndexChanged;
            dgvTargets.SelectionChanged += DgvTargets_SelectionChanged;

            PopulateTargetTypeCombos();
            PopulatePlanList();
            ClearForm();

            // Profiles tab wiring
            lvwProfiles.View = View.Details;
            lvwProfiles.Columns.Add("Name", 140);
            lvwProfiles.Columns.Add("Active", 50);
            lvwProfiles.FullRowSelect = true;
            lvwProfiles.MultiSelect = false;
            lvwProfiles.ItemSelectionChanged += LvwProfiles_ItemSelectionChanged;
            txtProfileFilter.TextChanged += TxtProfileFilter_TextChanged;
            txtProfileName.TextChanged += TxtProfileName_TextChanged;
            chkProfileActive.CheckedChanged += ChkProfileActive_CheckedChanged;
            cmdNewProfile.Click += CmdNewProfile_Click;
            cmdDeleteProfile.Click += CmdDeleteProfile_Click;
            cmdSaveProfile.Click += CmdSaveProfile_Click;
            cmdAddEntry.Click += CmdAddEntry_Click;
            cmdRemoveEntry.Click += CmdRemoveEntry_Click;
            txtEntryFilter.TextChanged += TxtEntryFilter_TextChanged;
            PopulateProfileList();
            ClearProfileForm();

            flpBase.Layout += FlpBase_Layout;
            flpSearchList.Layout += FlpSearchList_Layout;
            flpDetail.Layout += FlpDetail_Layout;
            tabProfiles.Layout += TabProfiles_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }

        // Layout
        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpBase.ClientSize.Width;
            int h = flpBase.ClientSize.Height;
            flpSearchList.Size = new Size(220, h - 6);
            tabMain.Size = new Size(w - 232, h - 6);
        }

        private void FlpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwPlans.Size = new Size(w - 6, listHeight);
        }

        private void FlpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            dgvTargets.Width = w - 6;
        }

        // Plan List
        private void PopulatePlanList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedPlan?.UUID;
            lvwPlans.Items.Clear();

            var plans = playerContext.GetCurrentPlayerStockPlans();
            string filter = txtFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                plans = plans.Where(p =>
                    p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            plans = plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var plan in plans)
            {
                var item = new ListViewItem(plan.Name) { Tag = plan };
                item.SubItems.Add(plan.IsActive ? "Yes" : "No");
                if (!plan.IsActive)
                {
                    item.ForeColor = Color.Gray;
                    item.Font = new Font(lvwPlans.Font, FontStyle.Italic);
                }

                lvwPlans.Items.Add(item);
                if (plan.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulatePlanList: {0}ms items={1}", sw.ElapsedMilliseconds, plans.Count);
        }

        private void TxtFilter_TextChanged(object sender, EventArgs e) { PopulatePlanList(); }

        private void LvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is StockPlan plan)
            {
                _selectedPlan = plan;
                PopulateForm();
            }
            else if (!e.IsSelected && lvwPlans.SelectedItems.Count == 0)
            {
                _selectedPlan = null;
                ClearForm();
            }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedPlan == null)
            {
                ClearForm();
                return;
            }

            txtPlanName.Text = _selectedPlan.Name;
            chkActive.Checked = _selectedPlan.IsActive;
            PopulateReplenishmentCombo();
            PopulateTargetsGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanName.Text = string.Empty;
            chkActive.Checked = true;
            cmbReplenishmentPlan.DataSource = null;
            cmbReplenishmentPlan.Items.Clear();
            dgvTargets.Rows.Clear();
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtPlanName.Enabled = enabled;
            chkActive.Enabled = enabled;
            cmbReplenishmentPlan.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvTargets.Enabled = enabled;
            cmdAddTarget.Enabled = enabled;
            cmdRemoveTarget.Enabled = enabled;
            cmdCheckGenerate.Enabled = enabled;
            cmbTargetType.Enabled = enabled;
            cmbTargetItem.Enabled = enabled;
            txtTargetQty.Enabled = enabled;
            txtCriticalThreshold.Enabled = enabled;
            cmbScope.Enabled = enabled;
            cmbTargetLocation.Enabled = enabled;
        }

        // Combo helpers
        private void PopulateTargetTypeCombos()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbTargetType.Items.Clear();
            cmbTargetType.Items.Add("Commodity");
            cmbTargetType.Items.Add("ShipPart");
            cmbTargetType.Items.Add("ShipHull");
            cmbTargetType.Items.Add("Resource");
            cmbTargetType.Items.Add("ShipTemplate");
            if (cmbTargetType.Items.Count > 0) cmbTargetType.SelectedIndex = 0;

            cmbScope.Items.Clear();
            foreach (var val in Enum.GetValues(typeof(StockTargetScope)))
                cmbScope.Items.Add(val);
            if (cmbScope.Items.Count > 0) cmbScope.SelectedIndex = 0;
            sw.Stop();
            Log.Info("PERF PopulateTargetTypeCombos: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateReplenishmentCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbReplenishmentPlan.DataSource = null;
            cmbReplenishmentPlan.Items.Clear();

            var buildPlans = playerContext.GetCurrentPlayerBuildPlans();
            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>(string.Empty, "(none)"));
            foreach (var bp in buildPlans.OrderBy(p => p.Name))
                items.Add(new KeyValuePair<string, string>(bp.UUID, bp.Name));

            cmbReplenishmentPlan.DataSource = items;
            cmbReplenishmentPlan.DisplayMember = "Value";
            cmbReplenishmentPlan.ValueMember = "Key";

            if (_selectedPlan != null && !string.IsNullOrEmpty(_selectedPlan.ReplenishmentBuildPlanUUID))
                cmbReplenishmentPlan.SelectedValue = _selectedPlan.ReplenishmentBuildPlanUUID;
            sw.Stop();
            Log.Info("PERF PopulateReplenishmentCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateTargetItemCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbTargetItem.DataSource = null;
            cmbTargetItem.Items.Clear();

            string type = cmbTargetType.SelectedItem?.ToString() ?? string.Empty;
            var items = new List<KeyValuePair<string, string>>();

            switch (type)
            {
                case "ShipTemplate":
                    foreach (var t in playerContext.GetCurrentPlayerShipTemplates().OrderBy(t => t.Name))
                        items.Add(new KeyValuePair<string, string>(t.UUID, t.Name));
                    break;
                case "Commodity":
                    var commodities = EmpireContext.GetInstance()?.CommodityList;
                    if (commodities != null)
                        foreach (var c in commodities.OrderBy(c => c.Name))
                        {
                            items.Add(new KeyValuePair<string, string>(c.Name, c.Name));
                        }

                    break;
                case "Resource":
                    var resources = EmpireContext.GetInstance()?.ResourceList;
                    if (resources != null)
                        foreach (var r in resources.OrderBy(r => r.Name))
                        {
                            items.Add(new KeyValuePair<string, string>(r.Name, r.Name));
                        }

                    break;
                default: // ShipPart, ShipHull
                    var blueprints = playerContext.GetAllBlueprints();
                    if (blueprints != null)
                        foreach (var bp in blueprints.Where(b => !string.IsNullOrEmpty(b.Name)).OrderBy(b => b.ExtendedName))
                        {
                            items.Add(new KeyValuePair<string, string>(bp.UUID, bp.ExtendedName));
                        }

                    break;
            }

            if (items.Count > 0)
            {
                cmbTargetItem.DataSource = items;
                cmbTargetItem.DisplayMember = "Value";
                cmbTargetItem.ValueMember = "Key";
            }

            sw.Stop();
            Log.Info("PERF PopulateTargetItemCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulateLocationCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbTargetLocation.DataSource = null;
            cmbTargetLocation.Items.Clear();

            if (cmbScope.SelectedItem == null) return;
            var scope = (StockTargetScope)cmbScope.SelectedItem;
            var items = new List<KeyValuePair<string, string>>();

            switch (scope)
            {
                case StockTargetScope.Colony:
                    foreach (var c in playerContext.ColonyList.OrderBy(c => c.ColonyName))
                        items.Add(new KeyValuePair<string, string>(c.UUID, c.ColonyName));
                    break;
                case StockTargetScope.Station:
                    foreach (var s in playerContext.StationList.OrderBy(s => s.Name))
                        items.Add(new KeyValuePair<string, string>(s.UUID, s.Name));
                    break;
            }

            if (items.Count > 0)
            {
                cmbTargetLocation.DataSource = items;
                cmbTargetLocation.DisplayMember = "Value";
                cmbTargetLocation.ValueMember = "Key";
            }

            sw.Stop();
            Log.Info("PERF PopulateLocationCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbScope_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateLocationCombo();
        }

        private void CmbTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateTargetItemCombo();
        }

        // Targets grid
        private void PopulateTargetsGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvTargets.Rows.Clear();
            if (_selectedPlan == null) return;

            foreach (var target in _selectedPlan.Targets)
            {
                string typeName = target.ItemType.ToString();
                if (!string.IsNullOrEmpty(target.ShipTemplateUUID))
                    typeName = "ShipTmpl";
                string locationName = ResolveLocationName(target.Scope, target.LocationUUID);

                int rowIdx = dgvTargets.Rows.Add(
                    typeName, target.ItemName,
                    target.TargetQuantity.ToString(), target.CriticalThreshold.ToString(),
                    target.Scope.ToString(), locationName, string.Empty, string.Empty);
                dgvTargets.Rows[rowIdx].Tag = target;
            }

            sw.Stop();
            Log.Info("PERF PopulateTargetsGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private string ResolveLocationName(StockTargetScope scope, string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return string.Empty;
            switch (scope)
            {
                case StockTargetScope.Colony:
                    var colony = playerContext.ColonyList.FirstOrDefault(c => c.UUID == uuid);
                    return colony?.ColonyName ?? uuid;
                case StockTargetScope.Station:
                    var station = playerContext.StationList.FirstOrDefault(s => s.UUID == uuid);
                    return station?.Name ?? uuid;
                default:
                    return string.Empty;
            }
        }

        // CRUD
        private void CmdNew_Click(object sender, EventArgs e)
        {
            var plan = new StockPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Stock Plan",
                OwnerUUID = playerContext.CurrentPlayerUUID ?? string.Empty,
                IsActive = true
            };

            playerContext.AddStockPlan(plan);
            playerContext.WriteContext();
            _selectedPlan = plan;
            PopulatePlanList();
            PopulateForm();
            Log.Info("Created new stock plan");
        }

        private void CmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            var result = MessageBox.Show(
                string.Format("Delete stock plan \"{0}\"?", _selectedPlan.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.RemoveStockPlan(_selectedPlan);
            playerContext.WriteContext();
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
            Log.Info("Deleted stock plan");
        }

        private void CmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            string name = txtPlanName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Name cannot be empty.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _selectedPlan.Name = name;
            playerContext.WriteContext();
            PopulatePlanList();
            Log.Info("Saved stock plan \"{0}\"", _selectedPlan.Name);
        }

        private void TxtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Name = txtPlanName.Text;
        }

        private void ChkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.IsActive = chkActive.Checked;
        }

        private void CmbReplenishmentPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.ReplenishmentBuildPlanUUID = cmbReplenishmentPlan.SelectedValue?.ToString() ?? string.Empty;
        }

        private void CmdAddTarget_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            string typeStr = cmbTargetType.SelectedItem?.ToString() ?? string.Empty;
            string itemKey = cmbTargetItem.SelectedValue?.ToString() ?? string.Empty;
            string itemName = string.Empty;
            if (cmbTargetItem.SelectedItem is KeyValuePair<string, string> kvp)
                itemName = kvp.Value;
            if (string.IsNullOrWhiteSpace(itemKey)) return;
            if (!int.TryParse(txtTargetQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show(
                    "Enter a valid target quantity.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            int.TryParse(txtCriticalThreshold.Text.Trim(), out int critical);

            var scope = cmbScope.SelectedItem is StockTargetScope s ? s : StockTargetScope.EmpireWide;
            string locationUUID = cmbTargetLocation.SelectedValue?.ToString() ?? string.Empty;

            var target = new StockTarget
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = MapItemType(typeStr),
                ItemReferenceID = itemKey,
                ItemName = itemName,
                ShipTemplateUUID = typeStr == "ShipTemplate" ? itemKey : string.Empty,
                TargetQuantity = qty,
                CriticalThreshold = critical,
                Scope = scope,
                LocationUUID = locationUUID
            };

            _selectedPlan.Targets.Add(target);
            PopulateTargetsGrid();
            Log.Info("Added target: {0} qty={1}", itemName, qty);
        }

        private ItemType.ItemTypeEnum MapItemType(string typeStr)
        {
            switch (typeStr)
            {
                case "Commodity": return ItemType.ItemTypeEnum.Commodity;
                case "ShipPart": return ItemType.ItemTypeEnum.ShipPart;
                case "ShipHull": return ItemType.ItemTypeEnum.ShipHull;
                case "Resource": return ItemType.ItemTypeEnum.Resource;
                case "ShipTemplate": return ItemType.ItemTypeEnum.ShipHull;
                default: return ItemType.ItemTypeEnum.None;
            }
        }

        private void CmdRemoveTarget_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null || dgvTargets.SelectedRows.Count == 0) return;
            var target = dgvTargets.SelectedRows[0].Tag as StockTarget;
            if (target == null) return;
            _selectedPlan.Targets.Remove(target);
            PopulateTargetsGrid();
            Log.Info("Removed target: {0}", target.ItemName);
        }

        private void CmdQuickAdd_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            // Quick Add: add common resource targets (all resources at 1000 qty, EmpireWide)
            var resources = EmpireContext.GetInstance()?.ResourceList;
            if (resources == null || resources.Count == 0)
            {
                MessageBox.Show(
                    "No resources available.",
                    "Quick Add",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            int added = 0;
            foreach (var r in resources.OrderBy(x => x.Name))
            {
                // Skip if already exists
                if (_selectedPlan.Targets.Any(t => t.ItemName == r.Name && t.ItemType == ItemType.ItemTypeEnum.Resource))
                    continue;
                var target = new StockTarget
                {
                    UUID = Guid.NewGuid().ToString(),
                    ItemType = ItemType.ItemTypeEnum.Resource,
                    ItemReferenceID = r.Name,
                    ItemName = r.Name,
                    TargetQuantity = 1000,
                    Scope = StockTargetScope.EmpireWide
                };

                _selectedPlan.Targets.Add(target);
                added++;
            }

            PopulateTargetsGrid();
            Log.Info("Quick Add: added {0} resource targets", added);
            MessageBox.Show(
                string.Format("Added {0} resource target(s).", added),
                "Quick Add",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void DgvTargets_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (dgvTargets.SelectedRows.Count == 0)
            {
                ClearExpandedComponents();
                return;
            }

            var target = dgvTargets.SelectedRows[0].Tag as StockTarget;
            if (target != null && !string.IsNullOrEmpty(target.ShipTemplateUUID))
                PopulateExpandedComponents(target);
            else
                ClearExpandedComponents();
        }

        private void PopulateExpandedComponents(StockTarget target)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvExpandedComponents.Rows.Clear();
            var template = playerContext.ShipTemplateList.FirstOrDefault(t => t.UUID == target.ShipTemplateUUID);
            if (template == null)
            {
                ClearExpandedComponents();
                return;
            }

            lblExpandedComponents.Text = string.Format("Components for {0} (x{1}):", template.Name, target.TargetQuantity);
            lblExpandedComponents.Visible = true;
            dgvExpandedComponents.Visible = true;

            // Hull
            var hullBp = playerContext.FindBlueprint(template.HullBlueprintUUID);
            if (hullBp != null)
                dgvExpandedComponents.Rows.Add("Hull: " + hullBp.ExtendedName, target.TargetQuantity.ToString());

            // Components
            foreach (var slot in template.Components)
            {
                if (string.IsNullOrEmpty(slot.BlueprintUUID)) continue;
                var bp = playerContext.FindBlueprint(slot.BlueprintUUID);
                string name = bp?.ExtendedName ?? "(unknown)";
                dgvExpandedComponents.Rows.Add(slot.SlotType + ": " + name, target.TargetQuantity.ToString());
            }

            sw.Stop();
            Log.Info("PERF PopulateExpandedComponents: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearExpandedComponents()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvExpandedComponents.Rows.Clear();
            lblExpandedComponents.Text = string.Empty;
            lblExpandedComponents.Visible = false;
            dgvExpandedComponents.Visible = false;
        }

        private void CmdCheckGenerate_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            var colonies = playerContext.SnapshotColonyList();
            var stations = playerContext.StationList.ToList();
            string playerUUID = playerContext.CurrentPlayerUUID ?? string.Empty;

            var shortfalls = StockTargetService.CheckTargets(
                new[] { _selectedPlan },
                playerUUID,
                uuid => playerContext.FindColony(uuid),
                uuid => playerContext.StationList.FirstOrDefault(st => st.UUID == uuid),
                uuid => playerContext.ShipTemplateList.FirstOrDefault(t => t.UUID == uuid),
                uuid => playerContext.FindBlueprint(uuid),
                colonies,
                stations);

            // Update grid with current quantities
            using (var guard = new ProgrammaticUpdateGuard(this))
            {
                foreach (DataGridViewRow row in dgvTargets.Rows)
                {
                    var target = row.Tag as StockTarget;
                    if (target == null) continue;
                    var sf = shortfalls.FirstOrDefault(s => s.Target == target);
                    int current = sf != null ? sf.CurrentQuantity : target.TargetQuantity;
                    int shortfall = sf != null ? sf.ShortfallQuantity : 0;
                    row.Cells[colCurrentQty.Index].Value = current.ToString();
                    row.Cells[colShortfall.Index].Value = shortfall > 0 ? shortfall.ToString() : string.Empty;

                    // Color code
                    if (sf != null && sf.IsCritical)
                        row.Cells[colShortfall.Index].Style.ForeColor = Color.Red;
                    else if (shortfall > 0)
                        row.Cells[colShortfall.Index].Style.ForeColor = Color.DarkGoldenrod;
                    else
                        row.Cells[colShortfall.Index].Style.ForeColor = Color.Green;
                }
            }

            if (shortfalls.Count == 0)
            {
                MessageBox.Show(
                    "All targets are met.",
                    "Stock Check",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // Generate replenishment items if a build plan is assigned
            if (string.IsNullOrEmpty(_selectedPlan.ReplenishmentBuildPlanUUID))
            {
                MessageBox.Show(string.Format(
                    "{0} shortfall(s) found but no replenishment plan assigned.",
                    shortfalls.Count), "Stock Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var buildPlan = playerContext.BuildPlanList
                .FirstOrDefault(bp => bp.UUID == _selectedPlan.ReplenishmentBuildPlanUUID);
            if (buildPlan == null)
            {
                MessageBox.Show(
                    "Replenishment build plan not found.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            var newItems = StockTargetService.GenerateReplenishmentItems(shortfalls, new[] { buildPlan });
            if (newItems.Count > 0)
            {
                buildPlan.Items.AddRange(newItems);
                playerContext.WriteContext();
                playerContext.CascadeResourceCheckDirty = true;
                MessageBox.Show(string.Format(
                    "Generated {0} build item(s) in plan \"{1}\".",
                    newItems.Count,
                    buildPlan.Name), "Orders Generated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(
                    "All shortfalls already have pending build items.",
                    "Stock Check",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        // === Profiles Tab ===

        private void TabProfiles_Layout(object sender, LayoutEventArgs e)
        {
            int w = tabProfiles.ClientSize.Width;
            int h = tabProfiles.ClientSize.Height;
            flpProfilesTab.Size = new Size(w, h);
            flpProfileList.Size = new Size(220, h - 6);
            flpProfileDetail.Size = new Size(w - 232, h - 6);
            int listH = h - 80;
            if (listH < 50) listH = 50;
            lvwProfiles.Size = new Size(214, listH);
            dgvEntries.Width = flpProfileDetail.Width - 6;
        }

        private void PopulateProfileList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedProfile?.UUID;
            lvwProfiles.Items.Clear();

            var profiles = playerContext.GetCurrentPlayerStockProfiles();
            string filter = txtProfileFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                profiles = profiles.Where(p =>
                    p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            profiles = profiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            foreach (var profile in profiles)
            {
                var item = new ListViewItem(profile.Name) { Tag = profile };
                item.SubItems.Add(profile.IsActive ? "Yes" : "No");
                if (!profile.IsActive)
                {
                    item.ForeColor = Color.Gray;
                    item.Font = new Font(lvwProfiles.Font, FontStyle.Italic);
                }

                lvwProfiles.Items.Add(item);
                if (profile.UUID == selectedUUID) item.Selected = true;
            }

            sw.Stop();
            Log.Info("PERF PopulateProfileList: {0}ms items={1}", sw.ElapsedMilliseconds, profiles.Count);
        }

        private void TxtProfileFilter_TextChanged(object sender, EventArgs e) { PopulateProfileList(); }

        private void LvwProfiles_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is StockProfile profile)
            {
                _selectedProfile = profile;
                PopulateProfileForm();
            }
            else if (!e.IsSelected && lvwProfiles.SelectedItems.Count == 0)
            {
                _selectedProfile = null;
                ClearProfileForm();
            }
        }

        private void PopulateProfileForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedProfile == null)
            {
                ClearProfileForm();
                return;
            }

            txtProfileName.Text = _selectedProfile.Name;
            chkProfileActive.Checked = _selectedProfile.IsActive;
            PopulateEntriesGrid();
            PopulateEntryCombo();
            SetProfileDetailEnabled(true);
            UpdateLogicSummary();
            sw.Stop();
            Log.Info("PERF PopulateProfileForm: {0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearProfileForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtProfileName.Text = string.Empty;
            chkProfileActive.Checked = true;
            dgvEntries.Rows.Clear();
            cmbEntry.DataSource = null;
            cmbEntry.Items.Clear();
            lblLogicSummary.Text = string.Empty;
            SetProfileDetailEnabled(false);
        }

        private void SetProfileDetailEnabled(bool enabled)
        {
            txtProfileName.Enabled = enabled;
            chkProfileActive.Enabled = enabled;
            cmdSaveProfile.Enabled = enabled;
            dgvEntries.Enabled = enabled;
            txtGroupID.Enabled = enabled;
            txtEntryFilter.Enabled = enabled;
            cmbEntry.Enabled = enabled;
            cmdAddEntry.Enabled = enabled;
            cmdRemoveEntry.Enabled = enabled;
        }

        private void PopulateEntriesGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvEntries.Rows.Clear();
            if (_selectedProfile == null) return;

            foreach (var entry in _selectedProfile.Entries)
            {
                string planName = ResolvePlanName(entry.StockPlanUUID);
                int rowIdx = dgvEntries.Rows.Add(entry.GroupID, planName);
                dgvEntries.Rows[rowIdx].Tag = entry;
            }

            sw.Stop();
            Log.Info("PERF PopulateEntriesGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        private string ResolvePlanName(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return string.Empty;
            var plan = playerContext.StockPlanList.FirstOrDefault(p => p.UUID == uuid);
            return plan?.Name ?? uuid;
        }

        private void PopulateEntryCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbEntry.DataSource = null;
            cmbEntry.Items.Clear();

            var plans = playerContext.GetCurrentPlayerStockPlans();
            string filter = txtEntryFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
                plans = plans.Where(p =>
                    p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            var items = new List<KeyValuePair<string, string>>();
            foreach (var plan in plans.OrderBy(p => p.Name))
                items.Add(new KeyValuePair<string, string>(plan.UUID, plan.Name));

            if (items.Count > 0)
            {
                cmbEntry.DataSource = items;
                cmbEntry.DisplayMember = "Value";
                cmbEntry.ValueMember = "Key";
            }

            sw.Stop();
            Log.Info("PERF PopulateEntryCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void TxtEntryFilter_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateEntryCombo();
        }

        private void UpdateLogicSummary()
        {
            if (_selectedProfile == null || _selectedProfile.Entries.Count == 0)
            {
                lblLogicSummary.Text = string.Empty;
                return;
            }

            var groups = _selectedProfile.Entries
                .GroupBy(e => e.GroupID)
                .OrderBy(g => g.Key);

            var parts = new List<string>();
            foreach (var g in groups)
            {
                var names = g.Select(e => ResolvePlanName(e.StockPlanUUID)).ToList();
                if (names.Count == 1)
                    parts.Add(string.Format("Group {0}: {1}", g.Key, names[0]));
                else
                    parts.Add(string.Format("Group {0} (OR): max({1})", g.Key, string.Join(", ", names)));
            }

            if (parts.Count == 1)
                lblLogicSummary.Text = "Logic: " + parts[0];
            else
                lblLogicSummary.Text = "Logic: " + string.Join(" + ", parts.Select(p => p));
        }

        private void CmdNewProfile_Click(object sender, EventArgs e)
        {
            var profile = new StockProfile
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Profile",
                OwnerUUID = playerContext.CurrentPlayerUUID ?? string.Empty,
                IsActive = true
            };

            playerContext.AddStockProfile(profile);
            playerContext.WriteContext();
            _selectedProfile = profile;
            PopulateProfileList();
            PopulateProfileForm();
            Log.Info("Created new stock profile");
        }

        private void CmdDeleteProfile_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;
            var result = MessageBox.Show(
                string.Format("Delete profile \"{0}\"?", _selectedProfile.Name),
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.RemoveStockProfile(_selectedProfile);
            playerContext.WriteContext();
            _selectedProfile = null;
            PopulateProfileList();
            ClearProfileForm();
            Log.Info("Deleted stock profile");
        }

        private void CmdSaveProfile_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;
            string name = txtProfileName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    "Name cannot be empty.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            _selectedProfile.Name = name;
            playerContext.WriteContext();
            PopulateProfileList();
            Log.Info("Saved stock profile \"{0}\"", _selectedProfile.Name);
        }

        private void TxtProfileName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedProfile == null) return;
            _selectedProfile.Name = txtProfileName.Text;
        }

        private void ChkProfileActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedProfile == null) return;
            _selectedProfile.IsActive = chkProfileActive.Checked;
        }

        private void CmdAddEntry_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;
            string planUUID = cmbEntry.SelectedValue?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(planUUID)) return;
            string groupID = txtGroupID.Text.Trim();
            if (string.IsNullOrWhiteSpace(groupID)) groupID = "A";

            var entry = new StockProfileEntry
            {
                GroupID = groupID,
                StockPlanUUID = planUUID
            };

            _selectedProfile.Entries.Add(entry);
            PopulateEntriesGrid();
            UpdateLogicSummary();
            Log.Info("Added profile entry: group={0} plan={1}", groupID, ResolvePlanName(planUUID));
        }

        private void CmdRemoveEntry_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null || dgvEntries.SelectedRows.Count == 0) return;
            var entry = dgvEntries.SelectedRows[0].Tag as StockProfileEntry;
            if (entry == null) return;
            _selectedProfile.Entries.Remove(entry);
            PopulateEntriesGrid();
            UpdateLogicSummary();
            Log.Info("Removed profile entry: group={0}", entry.GroupID);
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                } catch (ObjectDisposedException)
                {
                }

                return;
            }

            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
            _selectedProfile = null;
            PopulateProfileList();
            ClearProfileForm();
        }
    }
}