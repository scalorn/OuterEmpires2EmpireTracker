using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Constants;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Parsers;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.BuildPlanner
{
    public partial class FormBuildPlanner : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        private PlayerContext playerContext;
        private BuildPlan _selectedPlan;

        /// <summary>
        /// Delegate to resolve a colony name from its UUID.
        /// Returns the colony name or null if not found.
        /// </summary>
        private Func<string, string> _colonyFinder;

        /// <summary>
        /// Resolves a structure name from its UUID within a colony.
        /// Returns the blueprint output name or null if not found.
        /// </summary>
        private Func<string, string, string> _structureFinder;

        public FormBuildPlanner()
        {
            InitializeComponent();
            playerContext = EmpireContext.PlayerContext;

            _colonyFinder = uuid =>
            {
                var colony = playerContext.GetCurrentPlayerColonies()
                    .FirstOrDefault(c => c.UUID == uuid);
                return colony?.ColonyName;
            };

            _structureFinder = (colonyUUID, structureUUID) =>
            {
                var colony = playerContext.GetCurrentPlayerColonies()
                    .FirstOrDefault(c => c.UUID == colonyUUID);
                if (colony == null) return null;
                var structure = colony.Structures.FirstOrDefault(s => s.UUID == structureUUID);
                if (structure == null) return null;
                Models.Blueprint bp = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null) return null;
                return string.IsNullOrEmpty(bp.OutputItemName) ? bp.Name : bp.OutputItemName;
            };

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 200);
            lvwPlans.Columns.Add("Refs", 40, HorizontalAlignment.Right);
            lvwPlans.FullRowSelect = true;
            lvwPlans.MultiSelect = false;
            lvwPlans.ItemSelectionChanged += lvwPlans_ItemSelectionChanged;

            txtPlanFilter.TextChanged += txtPlanFilter_TextChanged;
            txtPlanName.TextChanged += txtPlanName_TextChanged;
            txtDescription.TextChanged += txtDescription_TextChanged;
            chkIsActive.CheckedChanged += chkIsActive_CheckedChanged;

            cmdNew.Click += cmdNew_Click;
            cmdDelete.Click += cmdDelete_Click;
            cmdSave.Click += cmdSave_Click;

            // Add Item panel wiring
            cmbItemType.Items.AddRange(new object[] { "Manufactory", "Commodity", "Mining", "Refining", "Research" });
            cmbItemType.SelectedIndex = 0;
            cmbItemType.SelectedIndexChanged += cmbItemType_SelectedIndexChanged;
            txtItemFilter.TextChanged += txtItemFilter_TextChanged;
            cmdAddItem.Click += cmdAddItem_Click;
            cmdQueueCalc.Click += cmdQueueCalc_Click;
            cmdAllocate.Click += cmdAllocate_Click;
            cmdAutoAssign.Click += cmdAutoAssign_Click;
            dgvBuildItems.CellDoubleClick += dgvBuildItems_CellDoubleClick;
            dgvBuildItems.SelectionChanged += dgvBuildItems_SelectionChanged;

            // Dependency context menu wiring
            tsmiSetDependency.Click += tsmiSetDependency_Click;
            tsmiClearDependency.Click += tsmiClearDependency_Click;

            // Generate Delivery dropdown wiring
            cmdGenerateDelivery.Click += cmdGenerateDelivery_Click;
            tsmiResourceDelivery.Click += tsmiResourceDelivery_Click;
            tsmiConsolidatedDelivery.Click += tsmiConsolidatedDelivery_Click;
            tsmiFlatpackDelivery.Click += tsmiFlatpackDelivery_Click;

            PopulatePlanList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged += OnBuildPlanDataChanged;
            playerContext.ColonyDataChanged += OnColonyDataChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

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
            int listHeight = h - flpPlanFilter.Height - flpCommands.Height - 18;
            if (listHeight < 50) listHeight = 50;
            lvwPlans.Size = new System.Drawing.Size(w - 6, listHeight);
        }

        private void flpDetail_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpDetail.ClientSize.Width;
            int h = flpDetail.ClientSize.Height;
            int addItemHeight = flpAddItem.Height;
            int shortfallHeight = dgvShortfalls.Visible ? 120 : 0;
            int headerHeight = lblShortfallHeader.Height + lblShortfallStatus.Height + 6;
            int gridHeight = h - flpPlanName.Height - flpDescription.Height
                - flpIsActive.Height - cmdSave.Height - headerHeight
                - shortfallHeight - addItemHeight - 36;
            if (gridHeight < 50) gridHeight = 50;
            dgvBuildItems.Size = new System.Drawing.Size(w - 6, gridHeight);
            dgvShortfalls.Size = new System.Drawing.Size(w - 6, shortfallHeight > 0 ? shortfallHeight : 0);
            flpAddItem.Size = new System.Drawing.Size(w - 6, addItemHeight);
        }

        // -----------------------------------------------------------------------
        // Plan List
        // -----------------------------------------------------------------------

        private void PopulatePlanList()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            string selectedUUID = _selectedPlan?.UUID;
            lvwPlans.Items.Clear();

            var plans = playerContext.GetCurrentPlayerBuildPlans();
            string filter = txtPlanFilter.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                plans = plans.Where(p => p.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            plans = plans.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();

            var refCounter = new BuildPlanReferenceCounter(playerContext.StockPlanList);

            foreach (var plan in plans)
            {
                int refs = refCounter.CountReferences(plan.UUID);
                var item = new ListViewItem(plan.Name) { Tag = plan };
                item.SubItems.Add(refs > 0 ? refs.ToString() : "");
                if (!plan.IsActive)
                {
                    item.ForeColor = System.Drawing.SystemColors.GrayText;
                    item.Font = new System.Drawing.Font(lvwPlans.Font, System.Drawing.FontStyle.Italic);
                }
                lvwPlans.Items.Add(item);
                if (plan.UUID == selectedUUID)
                    item.Selected = true;
            }
            sw.Stop();
            Log.Info("PERF PopulatePlanList: total={0}ms items={1}",
                sw.ElapsedMilliseconds, plans.Count);
        }

        private void txtPlanFilter_TextChanged(object sender, EventArgs e)
        {
            PopulatePlanList();
        }

        private void lvwPlans_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            if (e.IsSelected && e.Item.Tag is BuildPlan plan)
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

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            if (_selectedPlan == null) { ClearForm(); return; }

            txtPlanName.Text = _selectedPlan.Name;
            txtDescription.Text = _selectedPlan.Description;
            chkIsActive.Checked = _selectedPlan.IsActive;

            PopulateBuildItemsGrid();
            SetDetailEnabled(true);
            sw.Stop();
            Log.Info("PERF PopulateForm: total={0}ms", sw.ElapsedMilliseconds);
        }

        private void ClearForm()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            txtPlanName.Text = "";
            txtDescription.Text = "";
            chkIsActive.Checked = true;
            dgvBuildItems.Rows.Clear();
            dgvShortfalls.Rows.Clear();
            dgvShortfalls.Visible = false;
            lblShortfallStatus.Text = "Select a build item to check resources.";
            lblShortfallStatus.ForeColor = System.Drawing.SystemColors.GrayText;
            SetDetailEnabled(false);
        }

        private void SetDetailEnabled(bool enabled)
        {
            txtPlanName.Enabled = enabled;
            txtDescription.Enabled = enabled;
            chkIsActive.Enabled = enabled;
            cmdSave.Enabled = enabled;
            dgvBuildItems.Enabled = enabled;
            cmbItemType.Enabled = enabled;
            txtItemFilter.Enabled = enabled;
            cmbItem.Enabled = enabled;
            txtQuantity.Enabled = enabled;
            txtRecipient.Enabled = enabled;
            cmdAddItem.Enabled = enabled;
            cmdQueueCalc.Enabled = enabled;
            cmdAllocate.Enabled = enabled;
            cmdAutoAssign.Enabled = enabled;
            cmdGenerateDelivery.Enabled = enabled;
        }

        private void PopulateBuildItemsGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvBuildItems.Rows.Clear();

            if (_selectedPlan == null) { sw.Stop(); return; }

            foreach (var item in _selectedPlan.Items)
            {
                string location;
                if (string.IsNullOrEmpty(item.BuildLocationUUID))
                {
                    location = "Unallocated";
                }
                else
                {
                    string colonyName = _colonyFinder(item.BuildLocationUUID);
                    string structureName = !string.IsNullOrEmpty(item.StructureUUID)
                        ? _structureFinder(item.BuildLocationUUID, item.StructureUUID)
                        : null;
                    if (colonyName != null && structureName != null)
                        location = colonyName + " / " + structureName;
                    else if (colonyName != null)
                        location = colonyName;
                    else
                        location = item.BuildLocationUUID;
                }

                // Resolve dependency name
                string dependsOnName = "";
                if (!string.IsNullOrEmpty(item.DependsOnUUID))
                {
                    var depItem = _selectedPlan.Items.FirstOrDefault(
                        i => i.UUID == item.DependsOnUUID);
                    dependsOnName = depItem?.ItemName ?? item.DependsOnUUID;
                }

                int rowIdx = dgvBuildItems.Rows.Add(
                    item.ItemName,
                    item.ItemType.ToString(),
                    item.Quantity,
                    item.Status.ToString(),
                    location,
                    item.Notes,
                    item.SequenceInStructure > 0 ? item.SequenceInStructure.ToString() : "",
                    dependsOnName);
                dgvBuildItems.Rows[rowIdx].Tag = item;

                // Color-code row based on build item status
                switch (item.Status)
                {
                    case BuildItemStatus.Delivering:
                        dgvBuildItems.Rows[rowIdx].DefaultCellStyle.BackColor = System.Drawing.Color.AliceBlue;
                        break;
                    case BuildItemStatus.Ready:
                        dgvBuildItems.Rows[rowIdx].DefaultCellStyle.BackColor = System.Drawing.Color.Honeydew;
                        break;
                    case BuildItemStatus.InProgress:
                        dgvBuildItems.Rows[rowIdx].DefaultCellStyle.BackColor = System.Drawing.Color.LemonChiffon;
                        break;
                    case BuildItemStatus.Completed:
                        dgvBuildItems.Rows[rowIdx].DefaultCellStyle.BackColor = System.Drawing.Color.WhiteSmoke;
                        dgvBuildItems.Rows[rowIdx].DefaultCellStyle.ForeColor = System.Drawing.Color.Gray;
                        break;
                    // Staged: default styling, no special color
                }
            }
            sw.Stop(); Log.Info("PERF PopulateBuildItemsGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Shortfall Display
        // -----------------------------------------------------------------------

        private void dgvBuildItems_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateShortfallGrid();
        }

        private void PopulateShortfallGrid()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvShortfalls.Rows.Clear();

            if (dgvBuildItems.CurrentRow == null || dgvBuildItems.CurrentRow.Tag == null)
            {
                dgvShortfalls.Visible = false;
                lblShortfallStatus.Text = "Select a build item to check resources.";
                lblShortfallStatus.ForeColor = System.Drawing.SystemColors.GrayText;
                return;
            }

            var buildItem = dgvBuildItems.CurrentRow.Tag as BuildItem;
            if (buildItem == null)
            {
                dgvShortfalls.Visible = false;
                lblShortfallStatus.Text = "Select a build item to check resources.";
                lblShortfallStatus.ForeColor = System.Drawing.SystemColors.GrayText;
                return;
            }

            if (string.IsNullOrEmpty(buildItem.BuildLocationUUID))
            {
                dgvShortfalls.Visible = false;
                lblShortfallStatus.Text = "Item not allocated \u2014 cannot check resources.";
                lblShortfallStatus.ForeColor = System.Drawing.Color.DarkOrange;
                return;
            }

            // Resolve the colony inventory
            var colony = playerContext.GetCurrentPlayerColonies()
                .FirstOrDefault(c => c.UUID == buildItem.BuildLocationUUID);
            if (colony == null)
            {
                dgvShortfalls.Visible = false;
                lblShortfallStatus.Text = "Build location not found.";
                lblShortfallStatus.ForeColor = System.Drawing.Color.Red;
                return;
            }

            try
            {
                var shortfalls = ResourceCheckService.ComputeShortfalls(
                    buildItem,
                    colony.Items,
                    uuid => playerContext.FindBlueprint(uuid));

                if (shortfalls.Count == 0)
                {
                    dgvShortfalls.Visible = false;
                    lblShortfallStatus.Text = "\u2714 All resources available.";
                    lblShortfallStatus.ForeColor = System.Drawing.Color.Green;
                    return;
                }

                // Compute required and available for each shortfall resource
                Dictionary<string, int> required = ComputeRequiredResources(buildItem);

                dgvShortfalls.Visible = true;
                lblShortfallStatus.Text = string.Format("{0} resource(s) short.", shortfalls.Count);
                lblShortfallStatus.ForeColor = System.Drawing.Color.Red;

                foreach (var entry in shortfalls.OrderBy(kv => kv.Key))
                {
                    int totalRequired = required.ContainsKey(entry.Key) ? required[entry.Key] : 0;
                    int available = totalRequired - entry.Value;
                    if (available < 0) available = 0;

                    int rowIdx = dgvShortfalls.Rows.Add(
                        entry.Key,
                        totalRequired,
                        available,
                        entry.Value);
                    dgvShortfalls.Rows[rowIdx].DefaultCellStyle.BackColor = System.Drawing.Color.MistyRose;
                    dgvShortfalls.Rows[rowIdx].DefaultCellStyle.ForeColor = System.Drawing.Color.DarkRed;
                }

                flpDetail.PerformLayout();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error computing shortfalls for item {0}", buildItem.UUID);
                dgvShortfalls.Visible = false;
                lblShortfallStatus.Text = "Error checking resources.";
                lblShortfallStatus.ForeColor = System.Drawing.Color.Red;
            }
            sw.Stop(); Log.Info("PERF PopulateShortfallGrid: {0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// Computes the total required resources for a build item (before comparing to inventory).
        /// </summary>
        private Dictionary<string, int> ComputeRequiredResources(BuildItem item)
        {
            var required = new Dictionary<string, int>();

            if (item.ItemType == BuildItemType.Manufactory)
            {
                Models.Blueprint bp = playerContext.FindBlueprint(item.BlueprintUUID);
                if (bp?.Resources != null)
                {
                    foreach (var entry in bp.Resources)
                    {
                        int perRun;
                        if (int.TryParse(entry.Value, out perRun) && perRun > 0)
                        {
                            required[entry.Key] = perRun * item.Quantity;
                        }
                    }
                }
            }
            else if (item.ItemType == BuildItemType.Commodity)
            {
                Commodity commodity;
                if (Commodity.ResourceMapByString.TryGetValue(item.CommodityName, out commodity)
                    && commodity.ConstructionResources != null)
                {
                    foreach (var entry in commodity.ConstructionResources)
                    {
                        int perCycle;
                        if (int.TryParse(entry.Value, out perCycle) && perCycle > 0)
                        {
                            required[entry.Key] = perCycle * item.Quantity;
                        }
                    }
                }
            }
            else if (item.ItemType == BuildItemType.Refining)
            {
                var recipe = RefiningRecipes.FindByOutput(item.RefiningResource);
                if (recipe != null)
                {
                    required[recipe.InputResource] = recipe.ConsumeRate * item.Quantity;
                }
                else if (!string.IsNullOrEmpty(item.RefiningResource))
                {
                    required[item.RefiningResource] = item.Quantity;
                }
            }
            else if (item.ItemType == BuildItemType.Research)
            {
                Models.Blueprint bp = playerContext.FindBlueprint(item.BlueprintUUID);
                if (bp?.Resources != null)
                {
                    foreach (var entry in bp.Resources)
                    {
                        int perRun;
                        if (int.TryParse(entry.Value, out perRun) && perRun > 0)
                        {
                            required[entry.Key] = perRun * item.Quantity;
                        }
                    }
                }
            }

            return required;
        }

        // -----------------------------------------------------------------------
        // CRUD Operations
        // -----------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            var plan = new BuildPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = "New Build Plan",
                OwnerUUID = playerContext.CurrentPlayerUUID,
                IsActive = true
            };
            playerContext.BuildPlanList.Add(plan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(plan.UUID);
            _selectedPlan = plan;
            PopulatePlanList();
            PopulateForm();
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            var refCounter = new BuildPlanReferenceCounter(playerContext.StockPlanList);
            int refs = refCounter.CountReferences(_selectedPlan.UUID);

            string message;
            if (refs > 0)
                message = string.Format("This plan is referenced by {0} stock plan(s). Delete anyway?", refs);
            else
                message = string.Format("Delete build plan '{0}'?", _selectedPlan.Name);

            var result = MessageBox.Show(
                message,
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            string uuid = _selectedPlan.UUID;
            playerContext.BuildPlanList.Remove(_selectedPlan);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(uuid);
            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null) return;

            string name = txtPlanName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Plan name cannot be empty.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedPlan.Name = name;
            _selectedPlan.Description = txtDescription.Text;
            _selectedPlan.IsActive = chkIsActive.Checked;

            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
            PopulatePlanList();
            Log.Info("Saved build plan '{0}'", _selectedPlan.Name);
        }

        // -----------------------------------------------------------------------
        // Data Model Write-Through
        // -----------------------------------------------------------------------

        private void txtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Name = txtPlanName.Text;
        }

        private void txtDescription_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.Description = txtDescription.Text;
        }

        private void chkIsActive_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0 || _selectedPlan == null) return;
            _selectedPlan.IsActive = chkIsActive.Checked;
            PopulatePlanList();
        }

        // -----------------------------------------------------------------------
        // Add Item Panel
        // -----------------------------------------------------------------------

        private void cmbItemType_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateItemCombo();
        }

        private void txtItemFilter_TextChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;
            PopulateItemCombo();
        }

        private void PopulateItemCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbItem.Items.Clear();

            string filter = txtItemFilter.Text.Trim();
            string itemType = cmbItemType.SelectedItem as string ?? "";

            if (itemType == "Manufactory")
            {
                var blueprints = playerContext.GetAllBlueprints()
                    .Where(bp => bp.UUID != null && !bp.BluePrintType.IsFlatpack())
                    .Where(bp =>
                    {
                        bool canMfg = true;
                        bp.Properties?.getBoolean("Can Manufacture", true, out canMfg);
                        return canMfg;
                    });

                if (!string.IsNullOrEmpty(filter))
                {
                    blueprints = blueprints.Where(bp =>
                        bp.ExtendedName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var bp in blueprints.OrderBy(bp => bp.ExtendedName))
                {
                    cmbItem.Items.Add(new ItemEntry { Display = bp.ExtendedName, ID = bp.UUID });
                }
            }
            else if (itemType == "Commodity")
            {
                var commodityNames = Commodity.ResourceMapByString.Keys.AsEnumerable();

                if (!string.IsNullOrEmpty(filter))
                {
                    commodityNames = commodityNames.Where(n =>
                        n.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var name in commodityNames.OrderBy(n => n))
                {
                    cmbItem.Items.Add(new ItemEntry { Display = name, ID = name });
                }
            }
            else if (itemType == "Mining" || itemType == "Refining")
            {
                var resources = Resource.Resources
                    .Where(r => r.ID != Resource.ResourceEnum.None);

                if (!string.IsNullOrEmpty(filter))
                {
                    resources = resources.Where(r =>
                        r.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var r in resources.OrderBy(r => r.Name))
                {
                    cmbItem.Items.Add(new ItemEntry { Display = r.Name, ID = r.Name });
                }
            }
            else if (itemType == "Research")
            {
                var blueprints = playerContext.GetAllBlueprints()
                    .Where(bp => bp.UUID != null);

                if (!string.IsNullOrEmpty(filter))
                {
                    blueprints = blueprints.Where(bp =>
                        bp.ExtendedName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                foreach (var bp in blueprints.OrderBy(bp => bp.ExtendedName))
                {
                    cmbItem.Items.Add(new ItemEntry { Display = bp.ExtendedName, ID = bp.UUID });
                }
            }

            if (cmbItem.Items.Count > 0)
                cmbItem.SelectedIndex = 0;

            UpdateMiningRefiningFieldVisibility();
            sw.Stop(); Log.Info("PERF PopulateItemCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void cmdAddItem_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null)
            {
                MessageBox.Show("Select a build plan first.", "Add Item",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string itemType = cmbItemType.SelectedItem as string ?? "";
            var selectedEntry = cmbItem.SelectedItem as ItemEntry;
            if (selectedEntry == null)
            {
                MessageBox.Show("Select an item.", "Add Item",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out int quantity) || quantity < 1)
            {
                MessageBox.Show("Quantity must be a positive integer.", "Add Item",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var buildItem = new BuildItem
            {
                UUID = Guid.NewGuid().ToString(),
                Status = BuildItemStatus.Staged,
                Quantity = quantity,
                Recipient = txtRecipient.Text.Trim()
            };

            if (itemType == "Manufactory")
            {
                buildItem.ItemType = BuildItemType.Manufactory;
                buildItem.BlueprintUUID = selectedEntry.ID;
                var bp = playerContext.GetAllBlueprints()
                    .FirstOrDefault(b => b.UUID == selectedEntry.ID);
                buildItem.ItemName = bp?.Name ?? selectedEntry.Display;
            }
            else if (itemType == "Commodity")
            {
                buildItem.ItemType = BuildItemType.Commodity;
                buildItem.CommodityName = selectedEntry.ID;
                buildItem.ItemName = selectedEntry.Display;
            }
            else if (itemType == "Mining")
            {
                buildItem.ItemType = BuildItemType.Mining;
                buildItem.MiningResource = selectedEntry.ID;
                buildItem.ItemName = "Mine: " + selectedEntry.Display;
                if (cmbSurvey.Visible && cmbSurvey.SelectedItem is ItemEntry surveyEntry
                    && !string.IsNullOrEmpty(surveyEntry.ID))
                {
                    buildItem.MiningSurveyUUID = surveyEntry.ID;
                }
            }
            else if (itemType == "Refining")
            {
                buildItem.ItemType = BuildItemType.Refining;
                buildItem.RefiningResource = selectedEntry.ID;
                buildItem.ItemName = "Refine: " + selectedEntry.Display;
                if (cmbPurity.Visible && cmbPurity.SelectedItem is ItemEntry purityEntry
                    && !string.IsNullOrEmpty(purityEntry.ID))
                {
                    buildItem.RefiningPurity = purityEntry.ID;
                }
            }
            else if (itemType == "Research")
            {
                buildItem.ItemType = BuildItemType.Research;
                buildItem.BlueprintUUID = selectedEntry.ID;
                var bp = playerContext.GetAllBlueprints()
                    .FirstOrDefault(b => b.UUID == selectedEntry.ID);
                buildItem.ItemName = "Research: " + (bp?.Name ?? selectedEntry.Display);
            }

            if (!BuildPlanService.ValidateBuildItem(buildItem))
            {
                MessageBox.Show("Invalid build item. Check type and selection.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _selectedPlan.Items.Add(buildItem);
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
            PopulateBuildItemsGrid();
            Log.Info("Added {0} item '{1}' x{2} to plan '{3}'",
                buildItem.ItemType, buildItem.ItemName, buildItem.Quantity, _selectedPlan.Name);
        }

        private void cmdQueueCalc_Click(object sender, EventArgs e)
        {
            string itemType = cmbItemType.SelectedItem as string ?? "";
            var selectedEntry = cmbItem.SelectedItem as ItemEntry;
            if (selectedEntry == null)
            {
                MessageBox.Show("Select an item first.", "Queue Calc",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string input = txtTargetDuration.Text.Trim();
            if (string.IsNullOrWhiteSpace(input))
            {
                MessageBox.Show("Enter a target duration (e.g. 2d 12h 0m 0s).",
                    "Queue Calc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!CountdownFormatParser.TryParse(input, out long totalSeconds) || totalSeconds <= 0)
            {
                MessageBox.Show("Could not parse duration. Use format like '2d 12h 0m 0s'.",
                    "Queue Calc", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int runs;
            if (itemType == "Manufactory")
            {
                var bp = playerContext.GetAllBlueprints()
                    .FirstOrDefault(b => b.UUID == selectedEntry.ID);
                if (bp == null)
                {
                    MessageBox.Show("Blueprint not found.", "Queue Calc",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                runs = QueueCalculator.ComputeManufactoryRuns(bp, (int)totalSeconds);
                if (runs < 0)
                {
                    MessageBox.Show("Blueprint has no manufacturing time set.", "Queue Calc",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            else
            {
                runs = QueueCalculator.ComputeCommodityRuns((int)totalSeconds);
            }

            txtQuantity.Text = runs.ToString();
            Log.Info("Queue Calc: {0} runs for '{1}' ({2}s target)",
                runs, selectedEntry.Display, totalSeconds);
        }

        // -----------------------------------------------------------------------
        // Structure Allocation
        // -----------------------------------------------------------------------

        private void cmdAllocate_Click(object sender, EventArgs e)
        {
            OpenAllocationDialog();
        }

        private void dgvBuildItems_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OpenAllocationDialog();
        }

        private void OpenAllocationDialog()
        {
            if (_selectedPlan == null) return;

            if (dgvBuildItems.CurrentRow == null || dgvBuildItems.CurrentRow.Tag == null)
            {
                MessageBox.Show("Select a build item to allocate.", "Allocate",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var buildItem = dgvBuildItems.CurrentRow.Tag as BuildItem;
            if (buildItem == null) return;

            if (buildItem.ItemType != BuildItemType.Manufactory &&
                buildItem.ItemType != BuildItemType.Commodity &&
                buildItem.ItemType != BuildItemType.Mining &&
                buildItem.ItemType != BuildItemType.Refining &&
                buildItem.ItemType != BuildItemType.Research)
            {
                MessageBox.Show("This item type cannot be allocated to structures.",
                    "Allocate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new FormStructureAllocation(buildItem))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    buildItem.BuildLocationType = DestinationType.Colony;
                    buildItem.BuildLocationUUID = dlg.SelectedColonyUUID;
                    buildItem.StructureUUID = dlg.SelectedStructureUUID;

                    // Set SequenceInStructure: count existing items on this structure
                    int existingCount = _selectedPlan.Items.Count(i =>
                        i.UUID != buildItem.UUID &&
                        i.StructureUUID == dlg.SelectedStructureUUID &&
                        !string.IsNullOrEmpty(i.StructureUUID));
                    buildItem.SequenceInStructure = existingCount;

                    playerContext.WriteContext();
                    playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
                    PopulateBuildItemsGrid();

                    Log.Info("Allocated item '{0}' to colony {1} structure {2}",
                        buildItem.ItemName, dlg.SelectedColonyUUID, dlg.SelectedStructureUUID);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Auto-Assign
        // -----------------------------------------------------------------------

        private void cmdAutoAssign_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null)
            {
                MessageBox.Show("Select a build plan first.", "Auto-Assign",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var route = PickDeliveryRoute();
            if (route == null) return;

            try
            {
                var proposals = AutoAssignService.ProposeAssignments(
                    _selectedPlan,
                    route,
                    uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                    uuid => (Ship)null,
                    uuid => (Models.Station)null,
                    uuid => playerContext.FindBlueprint(uuid));

                if (proposals.Count == 0)
                {
                    MessageBox.Show("No unallocated items or no eligible structures.",
                        "Auto-Assign", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (ShowAutoAssignReview(proposals))
                {
                    ApplyAutoAssignProposals(proposals);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error during auto-assign for plan '{0}'", _selectedPlan.Name);
                MessageBox.Show("Error during auto-assign: " + ex.Message,
                    "Auto-Assign", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows a review dialog with proposed assignments. Returns true if user clicks Apply.
        /// </summary>
        private bool ShowAutoAssignReview(List<AssignmentProposal> proposals)
        {
            using (var form = new Form())
            {
                form.Text = "Auto-Assign Review";
                form.ClientSize = new System.Drawing.Size(700, 400);
                form.FormBorderStyle = FormBorderStyle.Sizable;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimumSize = new System.Drawing.Size(500, 300);

                var dgv = new DataGridView
                {
                    Dock = DockStyle.Fill,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                    AllowUserToOrderColumns = true
                };

                dgv.Columns.Add("colItemName", "Item Name");
                dgv.Columns.Add("colColony", "Colony");
                dgv.Columns.Add("colStructure", "Structure");
                dgv.Columns.Add("colSeq", "Sequence");
                dgv.Columns.Add("colReason", "Reason");

                dgv.Columns["colItemName"].Width = 160;
                dgv.Columns["colColony"].Width = 120;
                dgv.Columns["colStructure"].Width = 120;
                dgv.Columns["colSeq"].Width = 60;
                dgv.Columns["colReason"].Width = 200;

                foreach (var p in proposals)
                {
                    var item = _selectedPlan.Items.FirstOrDefault(i => i.UUID == p.BuildItemUUID);
                    string itemName = item?.ItemName ?? p.BuildItemUUID;
                    string colonyName = _colonyFinder(p.BuildLocationUUID) ?? p.BuildLocationUUID;
                    string structureName = _structureFinder(p.BuildLocationUUID, p.StructureUUID)
                        ?? p.StructureUUID;

                    dgv.Rows.Add(itemName, colonyName, structureName,
                        p.SequenceInStructure, p.Reason);
                }

                var pnlButtons = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    Height = 35,
                    Padding = new Padding(5)
                };

                var btnCancel = new Button
                {
                    Text = "Cancel", Width = 75,
                    DialogResult = DialogResult.Cancel
                };
                var btnApply = new Button
                {
                    Text = "Apply", Width = 75,
                    DialogResult = DialogResult.OK
                };

                pnlButtons.Controls.Add(btnCancel);
                pnlButtons.Controls.Add(btnApply);

                form.Controls.Add(dgv);
                form.Controls.Add(pnlButtons);
                form.AcceptButton = btnApply;
                form.CancelButton = btnCancel;

                return form.ShowDialog(this) == DialogResult.OK;
            }
        }

        /// <summary>
        /// Applies accepted auto-assign proposals to the build items.
        /// </summary>
        private void ApplyAutoAssignProposals(List<AssignmentProposal> proposals)
        {
            int applied = 0;
            foreach (var p in proposals)
            {
                var item = _selectedPlan.Items.FirstOrDefault(i => i.UUID == p.BuildItemUUID);
                if (item == null) continue;

                item.BuildLocationType = p.BuildLocationType;
                item.BuildLocationUUID = p.BuildLocationUUID;
                item.StructureUUID = p.StructureUUID;
                item.SequenceInStructure = p.SequenceInStructure;
                applied++;
            }

            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
            PopulateBuildItemsGrid();

            Log.Info("Auto-assign applied {0} of {1} proposals to plan '{2}'",
                applied, proposals.Count, _selectedPlan.Name);
        }

        // -----------------------------------------------------------------------
        // Generate Delivery
        // -----------------------------------------------------------------------

        private void cmdGenerateDelivery_Click(object sender, EventArgs e)
        {
            cmsGenerateDelivery.Show(cmdGenerateDelivery,
                new System.Drawing.Point(0, cmdGenerateDelivery.Height));
        }

        /// <summary>
        /// Shows a route picker dialog and returns the selected route, or null if cancelled.
        /// </summary>
        private Models.DeliveryRoute PickDeliveryRoute()
        {
            var routes = playerContext.GetCurrentPlayerRoutes();
            if (routes.Count == 0)
            {
                MessageBox.Show("No delivery routes found. Create a route first.",
                    "Generate Delivery", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using (var form = new Form())
            {
                form.Text = "Select Delivery Route";
                form.ClientSize = new System.Drawing.Size(350, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Route:", Left = 10, Top = 12, Width = 50 };
                var cmb = new ComboBox
                {
                    Left = 65, Top = 10, Width = 270,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                foreach (var r in routes.OrderBy(r => r.Name))
                    cmb.Items.Add(r);
                cmb.DisplayMember = "Name";
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

                var btnOk = new Button
                {
                    Text = "OK", Left = 180, Top = 70, Width = 75,
                    DialogResult = DialogResult.OK
                };
                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 265, Top = 70, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, cmb, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK && cmb.SelectedItem is Models.DeliveryRoute route)
                    return route;
                return null;
            }
        }

        /// <summary>
        /// Shows a plan checklist dialog and returns the selected plans, or null if cancelled.
        /// </summary>
        private List<BuildPlan> PickBuildPlans(string title)
        {
            var allPlans = playerContext.GetCurrentPlayerBuildPlans();
            if (allPlans.Count == 0)
            {
                MessageBox.Show("No build plans found.", title,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            using (var form = new Form())
            {
                form.Text = title;
                form.ClientSize = new System.Drawing.Size(350, 300);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Select plans:", Left = 10, Top = 8, Width = 330 };
                var clb = new CheckedListBox
                {
                    Left = 10, Top = 28, Width = 330, Height = 220,
                    CheckOnClick = true
                };
                foreach (var p in allPlans.OrderBy(p => p.Name))
                    clb.Items.Add(p.Name, false);

                var btnOk = new Button
                {
                    Text = "OK", Left = 180, Top = 260, Width = 75,
                    DialogResult = DialogResult.OK
                };
                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 265, Top = 260, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, clb, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) != DialogResult.OK)
                    return null;

                var selected = new List<BuildPlan>();
                var orderedPlans = allPlans.OrderBy(p => p.Name).ToList();
                for (int i = 0; i < clb.Items.Count; i++)
                {
                    if (clb.GetItemChecked(i))
                        selected.Add(orderedPlans[i]);
                }

                if (selected.Count == 0)
                {
                    MessageBox.Show("No plans selected.", title,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                return selected;
            }
        }

        private void tsmiResourceDelivery_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null)
            {
                MessageBox.Show("Select a build plan first.", "Resource Delivery",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var route = PickDeliveryRoute();
            if (route == null) return;

            try
            {
                var shortfalls = ResourceCheckService.ComputePlanShortfalls(
                    _selectedPlan,
                    uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                    uuid => (Ship)null,
                    uuid => (Models.Station)null,
                    playerContext.CurrentPlayerUUID,
                    uuid => playerContext.FindBlueprint(uuid));

                if (shortfalls.Count == 0)
                {
                    MessageBox.Show("No resource shortfalls found for this plan.",
                        "Resource Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var plan = DeliveryGenerationService.GenerateDeliveryPlan(
                    _selectedPlan,
                    route,
                    shortfalls,
                    uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                    playerContext);

                playerContext.WriteContext();
                playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
                PopulateBuildItemsGrid();

                MessageBox.Show(
                    string.Format("Delivery plan '{0}' created with {1} stop(s).",
                        plan.Name, plan.Stops.Count),
                    "Resource Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Log.Info("Generated resource delivery plan '{0}' ({1}) with {2} stops for plan '{3}'",
                    plan.Name, plan.UUID, plan.Stops.Count, _selectedPlan.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating resource delivery for plan '{0}'", _selectedPlan.Name);
                MessageBox.Show("Error generating delivery: " + ex.Message,
                    "Resource Delivery", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tsmiConsolidatedDelivery_Click(object sender, EventArgs e)
        {
            var route = PickDeliveryRoute();
            if (route == null) return;

            var selectedPlans = PickBuildPlans("Consolidated Resource Delivery");
            if (selectedPlans == null) return;

            try
            {
                Func<BuildPlan, Dictionary<string, Dictionary<string, int>>> shortfallProvider = plan =>
                    ResourceCheckService.ComputePlanShortfalls(
                        plan,
                        uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                        uuid => (Ship)null,
                        uuid => (Models.Station)null,
                        playerContext.CurrentPlayerUUID,
                        uuid => playerContext.FindBlueprint(uuid));

                string planName = string.Format("Consolidated: {0}",
                    string.Join(", ", selectedPlans.Select(p => p.Name)));
                if (planName.Length > 80)
                    planName = planName.Substring(0, 77) + "...";

                var plan = DeliveryGenerationService.GenerateConsolidatedDeliveryPlan(
                    selectedPlans,
                    route,
                    shortfallProvider,
                    uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                    playerContext,
                    planName);

                playerContext.WriteContext();

                MessageBox.Show(
                    string.Format("Consolidated delivery plan '{0}' created with {1} stop(s).",
                        plan.Name, plan.Stops.Count),
                    "Consolidated Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Log.Info("Generated consolidated delivery plan '{0}' ({1}) with {2} stops from {3} plans",
                    plan.Name, plan.UUID, plan.Stops.Count, selectedPlans.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating consolidated delivery");
                MessageBox.Show("Error generating delivery: " + ex.Message,
                    "Consolidated Delivery", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void tsmiFlatpackDelivery_Click(object sender, EventArgs e)
        {
            var route = PickDeliveryRoute();
            if (route == null) return;

            var selectedPlans = PickBuildPlans("Flatpack Delivery");
            if (selectedPlans == null) return;

            try
            {
                string planName = string.Format("Flatpack: {0}",
                    string.Join(", ", selectedPlans.Select(p => p.Name)));
                if (planName.Length > 80)
                    planName = planName.Substring(0, 77) + "...";

                var plan = DeliveryGenerationService.GenerateFlatpackDeliveryPlan(
                    selectedPlans,
                    route,
                    uuid => playerContext.GetCurrentPlayerColonies().FirstOrDefault(c => c.UUID == uuid),
                    uuid => playerContext.FindBlueprint(uuid),
                    playerContext,
                    planName);

                playerContext.WriteContext();

                MessageBox.Show(
                    string.Format("Flatpack delivery plan '{0}' created with {1} stop(s).",
                        plan.Name, plan.Stops.Count),
                    "Flatpack Delivery", MessageBoxButtons.OK, MessageBoxIcon.Information);

                Log.Info("Generated flatpack delivery plan '{0}' ({1}) with {2} stops from {3} plans",
                    plan.Name, plan.UUID, plan.Stops.Count, selectedPlans.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating flatpack delivery");
                MessageBox.Show("Error generating delivery: " + ex.Message,
                    "Flatpack Delivery", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // -----------------------------------------------------------------------
        // Utility Dialogs
        // -----------------------------------------------------------------------

        /// <summary>
        /// Shows a simple input dialog and returns the user's text, or null if cancelled.
        /// </summary>
        private static string ShowInputDialog(string prompt, string title)
        {
            using (var form = new Form())
            {
                form.Text = title;
                form.ClientSize = new System.Drawing.Size(350, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = prompt, Left = 10, Top = 10, Width = 330 };
                var txt = new TextBox { Left = 10, Top = 35, Width = 330 };
                var btnOk = new Button
                {
                    Text = "OK", Left = 180, Top = 70, Width = 75,
                    DialogResult = DialogResult.OK
                };
                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 265, Top = 70, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }

        /// <summary>
        /// Simple helper class for combo box items with a display name and ID.
        /// </summary>
        private class ItemEntry
        {
            public string Display { get; set; }
            public string ID { get; set; }
            public override string ToString() => Display;
        }

        // -----------------------------------------------------------------------
        // Mining/Refining Field Visibility
        // -----------------------------------------------------------------------

        private void UpdateMiningRefiningFieldVisibility()
        {
            string itemType = cmbItemType.SelectedItem as string ?? "";

            bool isMining = itemType == "Mining";
            bool isRefining = itemType == "Refining";

            lblSurvey.Visible = isMining;
            cmbSurvey.Visible = isMining;

            lblPurity.Visible = isRefining;
            cmbPurity.Visible = isRefining;

            lblResource.Visible = false;
            cmbResource.Visible = false;

            if (isMining)
                PopulateSurveyCombo();
            else if (isRefining)
                PopulatePurityCombo();
        }


        private void PopulateSurveyCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbSurvey.Items.Clear();
            cmbSurvey.Items.Add(new ItemEntry { Display = "(none)", ID = "" });

            var surveys = playerContext.GetCurrentPlayerSurveys();
            foreach (var s in surveys.OrderBy(s => s.Name))
                cmbSurvey.Items.Add(new ItemEntry { Display = s.Name, ID = s.UUID });
            cmbSurvey.SelectedIndex = 0;
            sw.Stop(); Log.Info("PERF PopulateSurveyCombo: {0}ms", sw.ElapsedMilliseconds);
        }

        private void PopulatePurityCombo()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            cmbPurity.Items.Clear();

            foreach (var p in ResourcePurity.Purities.Where(p => p.ID != ResourcePurity.PurityEnum.None))
                cmbPurity.Items.Add(new ItemEntry { Display = p.Name, ID = p.Name });
            if (cmbPurity.Items.Count > 0)
                cmbPurity.SelectedIndex = 0;
            sw.Stop(); Log.Info("PERF PopulatePurityCombo: {0}ms", sw.ElapsedMilliseconds);
        }


        // -----------------------------------------------------------------------
        // Dependency Tracking
        // -----------------------------------------------------------------------

        private void tsmiSetDependency_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null || dgvBuildItems.CurrentRow == null) return;

            var buildItem = dgvBuildItems.CurrentRow.Tag as BuildItem;
            if (buildItem == null) return;

            var otherItems = _selectedPlan.Items
                .Where(i => i.UUID != buildItem.UUID)
                .OrderBy(i => i.ItemName)
                .ToList();

            if (otherItems.Count == 0)
            {
                MessageBox.Show("No other items in this plan to depend on.",
                    "Set Dependency", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Set Dependency";
                form.ClientSize = new System.Drawing.Size(350, 120);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Depends on:", Left = 10, Top = 12, Width = 70 };
                var cmb = new ComboBox
                {
                    Left = 85, Top = 10, Width = 250,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                foreach (var item in otherItems)
                    cmb.Items.Add(new ItemEntry { Display = item.ItemName, ID = item.UUID });
                if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

                var btnOk = new Button
                {
                    Text = "OK", Left = 180, Top = 70, Width = 75,
                    DialogResult = DialogResult.OK
                };
                var btnCancel = new Button
                {
                    Text = "Cancel", Left = 265, Top = 70, Width = 75,
                    DialogResult = DialogResult.Cancel
                };

                form.Controls.AddRange(new Control[] { lbl, cmb, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK && cmb.SelectedItem is ItemEntry entry)
                {
                    buildItem.DependsOnUUID = entry.ID;
                    playerContext.WriteContext();
                    playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
                    PopulateBuildItemsGrid();
                    Log.Info("Set dependency: '{0}' depends on '{1}'",
                        buildItem.ItemName, entry.Display);
                }
            }
        }

        private void tsmiClearDependency_Click(object sender, EventArgs e)
        {
            if (_selectedPlan == null || dgvBuildItems.CurrentRow == null) return;

            var buildItem = dgvBuildItems.CurrentRow.Tag as BuildItem;
            if (buildItem == null) return;

            if (string.IsNullOrEmpty(buildItem.DependsOnUUID))
            {
                MessageBox.Show("This item has no dependency set.",
                    "Clear Dependency", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            buildItem.DependsOnUUID = string.Empty;
            playerContext.WriteContext();
            playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
            PopulateBuildItemsGrid();
            Log.Info("Cleared dependency on item '{0}'", buildItem.ItemName);
        }

        // -----------------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            _selectedPlan = null;
            PopulatePlanList();
            ClearForm();
        }

        private void OnBuildPlanDataChanged(object sender, BuildPlanDataChangedEventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnBuildPlanDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }

            PopulatePlanList();
            if (_selectedPlan != null)
            {
                PopulateForm();
            }
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

            // Refresh shortfall display if a colony changed that affects the selected item
            if (_selectedPlan != null)
            {
                PopulateShortfallGrid();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged -= OnBuildPlanDataChanged;
            playerContext.ColonyDataChanged -= OnColonyDataChanged;
            base.OnFormClosed(e);
        }
    }
}
