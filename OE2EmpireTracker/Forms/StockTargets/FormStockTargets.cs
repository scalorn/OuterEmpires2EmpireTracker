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
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private StockPlan _selectedPlan;

        public FormStockTargets()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 140);
            lvwPlans.Columns.Add("Active", 50);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += lvwPlans_ItemSelectionChanged;

            txtFilter.TextChanged += txtFilter_TextChanged;
            txtPlanName.TextChanged += txtPlanName_TextChanged;
            chkActive.CheckedChanged += chkActive_CheckedChanged;
            cmbReplenishmentPlan.SelectedIndexChanged += cmbReplenishmentPlan_SelectedIndexChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;
            cmdAddTarget.Click += cmdAddTarget_Click;
            cmdRemoveTarget.Click += cmdRemoveTarget_Click;
            cmdCheckGenerate.Click += cmdCheckGenerate_Click;

            cmbScope.SelectedIndexChanged += cmbScope_SelectedIndexChanged;
            cmbTargetType.SelectedIndexChanged += cmbTargetType_SelectedIndexChanged;

            PopulateTargetTypeCombos();
            PopulatePlanList();
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
            flpSearchList.Size = new Size(220, h - 6);
            flpDetail.Size = new Size(w - 232, h - 6);
        }
        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSearchList.ClientSize.Width;
            int h = flpSearchList.ClientSize.Height;
            int listHeight = h - flpFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwPlans.Size = new Size(w - 6, listHeight);
        }
        private void flpDetail_Layout(object sender, LayoutEventArgs e)
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

        private void txtFilter_TextChanged(object sender, EventArgs e) { PopulatePlanList(); }

        private void lvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is StockPlan plan)
            { _selectedPlan = plan; PopulateForm(); }
            else if (!e.IsSelected && lvwPlans.SelectedItems.Count == 0)
            { _selectedPlan = null; ClearForm(); }
        }

        // Form Population
        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedPlan == null) { ClearForm(); return; }
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
            txtPlanName.Text = "";
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
        }

        private void PopulateReplenishmentCombo()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbReplenishmentPlan.DataSource = null;
            cmbReplenishmentPlan.Items.Clear();

            var buildPlans = playerContext.GetCurrentPlayerBuildPlans();
            var items = new List<KeyValuePair<string, string>>();
            items.Add(new KeyValuePair<string, string>("", "(none)"));
            foreach (var bp in buildPlans.OrderBy(p => p.Name))
                items.Add(new KeyValuePair<string, string>(bp.UUID, bp.Name));

            cmbReplenishmentPlan.DataSource = items;
            cmbReplenishmentPlan.DisplayMember = "Value";
            cmbReplenishmentPlan.ValueMember = "Key";

            if (_selectedPlan != null && !string.IsNullOrEmpty(_selectedPlan.ReplenishmentBuildPlanUUID))
                cmbReplenishmentPlan.SelectedValue = _selectedPlan.ReplenishmentBuildPlanUUID;
        }

        private void PopulateTargetItemCombo()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbTargetItem.DataSource = null;
            cmbTargetItem.Items.Clear();

            string type = cmbTargetType.SelectedItem?.ToString() ?? "";
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
                            items.Add(new KeyValuePair<string, string>(c.Name, c.Name));
                    break;
                case "Resource":
                    var resources = EmpireContext.GetInstance()?.ResourceList;
                    if (resources != null)
                        foreach (var r in resources.OrderBy(r => r.Name))
                            items.Add(new KeyValuePair<string, string>(r.Name, r.Name));
                    break;
                default: // ShipPart, ShipHull
                    var blueprints = playerContext.GetAllBlueprints();
                    if (blueprints != null)
                        foreach (var bp in blueprints.Where(b => !string.IsNullOrEmpty(b.Name)).OrderBy(b => b.ExtendedName))
                            items.Add(new KeyValuePair<string, string>(bp.UUID, bp.ExtendedName));
                    break;
            }

            if (items.Count > 0)
            {
                cmbTargetItem.DataSource = items;
                cmbTargetItem.DisplayMember = "Value";
                cmbTargetItem.ValueMember = "Key";
            }
        }

        private void PopulateLocationCombo()
        {
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
        }

        private void cmbScope_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateLocationCombo();
        }

        private void cmbTargetType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateTargetItemCombo();
        }
        // Targets grid
        private void PopulateTargetsGrid()
        {
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
                    target.Scope.ToString(), locationName, "", "");
                dgvTargets.Rows[rowIdx].Tag = target;
            }
        }

        private string ResolveLocationName(StockTargetScope scope, string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) return "";
            switch (scope)
            {
                case StockTargetScope.Colony:
                    var colony = playerContext.ColonyList.FirstOrDefault(c => c.UUID == uuid);
                    return colony?.ColonyName ?? uuid;
                case StockTargetScope.Station:
                    var station = playerContext.StationList.FirstOrDefault(s => s.UUID == uuid);
                    return station?.Name ?? uuid;
                default:
                    return "";
            }
        }

        // CRUD
        private void cmdNew_Click(object sender, EventArgs e)
        {
            var plan = new StockPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Stock Plan",
                OwnerUUID = playerContext.CurrentPlayerUUID ?? "",
                IsActive = true
            };
            playerContext.StockPlanList.Add(plan);
            playerContext.WriteContext();
            _selectedPlan = plan;
            PopulatePlanList();
            PopulateForm();
            Log.Info("Created new stock plan");
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            var result = MessageBox.Show(
                string.Format("Delete stock plan \"{0}\"?", _selectedPlan.Name),
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;
            playerContext.StockPlanList.Remove(_selectedPlan);
            playerContext.WriteContext();
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
            Log.Info("Deleted stock plan");
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            string name = txtPlanName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _selectedPlan.Name = name;
            playerContext.WriteContext();
            PopulatePlanList();
            Log.Info("Saved stock plan \"{0}\"", _selectedPlan.Name);
        }

        private void txtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Name = txtPlanName.Text;
        }

        private void chkActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.IsActive = chkActive.Checked;
        }

        private void cmbReplenishmentPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.ReplenishmentBuildPlanUUID = cmbReplenishmentPlan.SelectedValue?.ToString() ?? "";
        }
        private void cmdAddTarget_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;
            string typeStr = cmbTargetType.SelectedItem?.ToString() ?? "";
            string itemKey = cmbTargetItem.SelectedValue?.ToString() ?? "";
            string itemName = "";
            if (cmbTargetItem.SelectedItem is KeyValuePair<string, string> kvp)
                itemName = kvp.Value;
            if (string.IsNullOrWhiteSpace(itemKey)) return;
            if (!int.TryParse(txtTargetQty.Text.Trim(), out int qty) || qty <= 0)
            {
                MessageBox.Show("Enter a valid target quantity.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int.TryParse(txtCriticalThreshold.Text.Trim(), out int critical);

            var scope = cmbScope.SelectedItem is StockTargetScope s ? s : StockTargetScope.EmpireWide;
            string locationUUID = cmbTargetLocation.SelectedValue?.ToString() ?? "";

            var target = new StockTarget
            {
                UUID = Guid.NewGuid().ToString(),
                ItemType = MapItemType(typeStr),
                ItemReferenceID = itemKey,
                ItemName = itemName,
                ShipTemplateUUID = typeStr == "ShipTemplate" ? itemKey : "",
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

        private void cmdRemoveTarget_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null || dgvTargets.SelectedRows.Count == 0) return;
            var target = dgvTargets.SelectedRows[0].Tag as StockTarget;
            if (target == null) return;
            _selectedPlan.Targets.Remove(target);
            PopulateTargetsGrid();
            Log.Info("Removed target: {0}", target.ItemName);
        }

        private void cmdCheckGenerate_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            var colonies = playerContext.SnapshotColonyList();
            var stations = playerContext.StationList.ToList();
            string playerUUID = playerContext.CurrentPlayerUUID ?? "";

            var shortfalls = StockTargetService.CheckTargets(
                new[] { _selectedPlan }, playerUUID,
                uuid => playerContext.FindColony(uuid),
                uuid => playerContext.StationList.FirstOrDefault(st => st.UUID == uuid),
                uuid => playerContext.ShipTemplateList.FirstOrDefault(t => t.UUID == uuid),
                uuid => playerContext.FindBlueprint(uuid),
                colonies, stations);

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
                    row.Cells[colShortfall.Index].Value = shortfall > 0 ? shortfall.ToString() : "";

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
                MessageBox.Show("All targets are met.", "Stock Check",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Generate replenishment items if a build plan is assigned
            if (string.IsNullOrEmpty(_selectedPlan.ReplenishmentBuildPlanUUID))
            {
                MessageBox.Show(string.Format("{0} shortfall(s) found but no replenishment plan assigned.",
                    shortfalls.Count), "Stock Check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var buildPlan = playerContext.BuildPlanList
                .FirstOrDefault(bp => bp.UUID == _selectedPlan.ReplenishmentBuildPlanUUID);
            if (buildPlan == null)
            {
                MessageBox.Show("Replenishment build plan not found.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var newItems = StockTargetService.GenerateReplenishmentItems(shortfalls, new[] { buildPlan });
            if (newItems.Count > 0)
            {
                buildPlan.Items.AddRange(newItems);
                playerContext.WriteContext();
                playerContext.CascadeResourceCheckDirty = true;
                MessageBox.Show(string.Format("Generated {0} build item(s) in plan \"{1}\".",
                    newItems.Count, buildPlan.Name), "Orders Generated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("All shortfalls already have pending build items.", "Stock Check",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // Events
        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            { try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); } catch (ObjectDisposedException) { } return; }
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            base.OnFormClosed(e);
        }
    }
}