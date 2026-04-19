using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using NLog;
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
                Blueprint bp = playerContext.FindBlueprint(structure.FlatpackBlueprintUUID);
                if (bp == null) return null;
                return string.IsNullOrEmpty(bp.OutputItemName) ? bp.Name : bp.OutputItemName;
            };

            lvwPlans.View = View.Details;
            lvwPlans.Columns.Add("Name", 200);
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
            cmbItemType.Items.AddRange(new object[] { "Manufactory", "Commodity" });
            cmbItemType.SelectedIndex = 0;
            cmbItemType.SelectedIndexChanged += cmbItemType_SelectedIndexChanged;
            txtItemFilter.TextChanged += txtItemFilter_TextChanged;
            cmdAddItem.Click += cmdAddItem_Click;
            cmdQueueCalc.Click += cmdQueueCalc_Click;
            cmdAllocate.Click += cmdAllocate_Click;
            dgvBuildItems.CellDoubleClick += dgvBuildItems_CellDoubleClick;
            dgvBuildItems.SelectionChanged += dgvBuildItems_SelectionChanged;

            PopulatePlanList();
            ClearForm();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpDetail.Layout += flpDetail_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged += OnBuildPlanDataChanged;
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

            foreach (var plan in plans)
            {
                var item = new ListViewItem(plan.Name) { Tag = plan };
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
        }

        private void PopulateBuildItemsGrid()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvBuildItems.Rows.Clear();

            if (_selectedPlan == null) return;

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

                int rowIdx = dgvBuildItems.Rows.Add(
                    item.ItemName,
                    item.ItemType.ToString(),
                    item.Quantity,
                    item.Status.ToString(),
                    location,
                    item.Notes);
                dgvBuildItems.Rows[rowIdx].Tag = item;
            }
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
        }

        /// <summary>
        /// Computes the total required resources for a build item (before comparing to inventory).
        /// </summary>
        private Dictionary<string, int> ComputeRequiredResources(BuildItem item)
        {
            var required = new Dictionary<string, int>();

            if (item.ItemType == BuildItemType.Manufactory)
            {
                Blueprint bp = playerContext.FindBlueprint(item.BlueprintUUID);
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

            return required;
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
                lblShortfallStatus.Text = "Item not allocated — cannot check resources.";
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
                    lblShortfallStatus.Text = "✔ All resources available.";
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
        }

        /// <summary>
        /// Computes the total required resources for a build item (before comparing to inventory).
        /// </summary>
        private Dictionary<string, int> ComputeRequiredResources(BuildItem item)
        {
            var required = new Dictionary<string, int>();

            if (item.ItemType == BuildItemType.Manufactory)
            {
                Blueprint bp = playerContext.FindBlueprint(item.BlueprintUUID);
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
            var result = MessageBox.Show(
                string.Format("Delete build plan '{0}'?", _selectedPlan.Name),
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

            if (cmbItem.Items.Count > 0)
                cmbItem.SelectedIndex = 0;
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

            string input = ShowInputDialog("Enter target duration (e.g. 2d 12h 0m 0s):",
                "Queue Calculator");
            if (input == null) return;

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
                buildItem.ItemType != BuildItemType.Commodity)
            {
                MessageBox.Show("Only Manufactory and Commodity items can be allocated to structures.",
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

                    playerContext.WriteContext();
                    playerContext.OnBuildPlanDataChanged(_selectedPlan.UUID);
                    PopulateBuildItemsGrid();

                    Log.Info("Allocated item '{0}' to colony {1} structure {2}",
                        buildItem.ItemName, dlg.SelectedColonyUUID, dlg.SelectedStructureUUID);
                }
            }
        }

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

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.BuildPlanDataChanged -= OnBuildPlanDataChanged;
            base.OnFormClosed(e);
        }
    }
}
