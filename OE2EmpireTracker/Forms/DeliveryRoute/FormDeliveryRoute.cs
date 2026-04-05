using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using OE2EmpireTracker.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.DeliveryRoute
{
    public partial class FormDeliveryRoute : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private DeliveryRouteViewModel viewModel;
        private DeliveryPlanViewModel planViewModel;
        private DeliveryPlanStop selectedPlanStop;

        public FormDeliveryRoute()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
            playerContext = EmpireContext.PlayerContext;
            viewModel = new DeliveryRouteViewModel(new Baseline.DeliveryRoute(), playerContext);

            lvwRoutes.View = View.Details;
            lvwRoutes.Columns.Add("Name", 200);
            lvwRoutes.FullRowSelect = true;
            lvwRoutes.MultiSelect = false;
            lvwRoutes.ItemSelectionChanged += lvwRoutes_ItemSelectionChanged;

            txtRouteFilter.TextChanged += txtRouteFilter_TextChanged;

            cmbColony.DisplayMember = "Display";
            cmbColony.ValueMember = "UUID";
            PopulateColonyPicker();

            cmdAddStop.Click += cmdAddStop_Click;
            cmdUp.Click += cmdUp_Click;
            cmdDown.Click += cmdDown_Click;
            cmdRemoveStop.Click += cmdRemoveStop_Click;
            chkPreventDuplicates.CheckedChanged += (s, ev) => PopulateColonyPicker();
            cmdNew.Click += cmdNew_Click;
            cmdSave.Click += cmdSave_Click;
            cmdDelete.Click += cmdDelete_Click;

            // Plan tab wiring
            PopulateItemTypeCombos();
            cmbDropItemType.SelectedIndexChanged += (s, ev) => { PopulateItemPicker(cmbDropItemType, txtDropFilter, cmbDropItem); UpdatePurityVisibility(cmbDropItemType, cmbDropPurity); };
            cmbPickItemType.SelectedIndexChanged += (s, ev) => { PopulateItemPicker(cmbPickItemType, txtPickFilter, cmbPickItem); UpdatePurityVisibility(cmbPickItemType, cmbPickPurity); };
            txtDropFilter.TextChanged += (s, ev) => PopulateItemPicker(cmbDropItemType, txtDropFilter, cmbDropItem);
            txtPickFilter.TextChanged += (s, ev) => PopulateItemPicker(cmbPickItemType, txtPickFilter, cmbPickItem);
            cmdAddDropOff.Click += cmdAddDropOff_Click;
            cmdAddPickUp.Click += cmdAddPickUp_Click;
            cmdRemoveDropOff.Click += cmdRemoveDropOff_Click;
            cmdRemovePickUp.Click += cmdRemovePickUp_Click;
            dgvStops.SelectionChanged += dgvStops_SelectionChanged;

            // Plan selector wiring
            PopulatePurityCombos();
            cmbPlan.DisplayMember = "Display";
            cmbPlan.ValueMember = "UUID";
            cmbPlan.SelectedIndexChanged += cmbPlan_SelectedIndexChanged;
            txtPlanFilter.TextChanged += (s, ev) => PopulatePlanDropdown();
            chkShowCompleted.CheckedChanged += (s, ev) => PopulatePlanDropdown();
            cmdNewPlan.Click += cmdNewPlan_Click;
            cmdDeletePlan.Click += cmdDeletePlan_Click;
            txtPlanName.TextChanged += txtPlanName_TextChanged;
            cmdExecutePlan.Click += cmdExecutePlan_Click;
            cmdAutoFill.Click += cmdAutoFill_Click;
            cmdAutoFill.Visible = false;

            PopulateRouteList();

            flpBase.Layout += flpBase_Layout;
            flpSearchList.Layout += flpSearchList_Layout;
            flpRouteData.Layout += flpRouteData_Layout;
            tabStops.Layout += tabStops_Layout;
            flpPlanContent.Layout += flpPlanContent_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            flpRouteData.Size = new Size(
                flpBase.Size.Width - flpSearchList.Size.Width - flpSearchList.Margin.Right - flpSearchList.Margin.Left - flpRouteData.Margin.Left - flpRouteData.Margin.Right,
                flpBase.Size.Height - flpRouteData.Margin.Top - flpRouteData.Margin.Bottom);
            flpSearchList.Size = new Size(
                flpSearchList.Size.Width,
                flpBase.Size.Height - flpSearchList.Margin.Top - flpSearchList.Margin.Bottom);
        }

        private void flpSearchList_Layout(object sender, LayoutEventArgs e)
        {
            lvwRoutes.Size = new Size(
                lvwRoutes.Size.Width,
                flpSearchList.Size.Height - flpRouteFilter.Size.Height - flpRouteFilter.Margin.Top - flpRouteFilter.Margin.Bottom - lvwRoutes.Margin.Top - lvwRoutes.Margin.Bottom);
        }

        private void flpRouteData_Layout(object sender, LayoutEventArgs e)
        {
            int tabHeight = flpRouteData.Size.Height
                - flpRouteName.Size.Height - flpRouteName.Margin.Top - flpRouteName.Margin.Bottom
                - flpCommands.Size.Height - flpCommands.Margin.Top - flpCommands.Margin.Bottom
                - tabRouteDetail.Margin.Top - tabRouteDetail.Margin.Bottom;
            if (tabHeight < 100) tabHeight = 100;
            tabRouteDetail.Size = new Size(
                flpRouteData.Size.Width - tabRouteDetail.Margin.Left - tabRouteDetail.Margin.Right,
                tabHeight);
        }

        private void tabStops_Layout(object sender, LayoutEventArgs e)
        {
            flpAddStop.Dock = System.Windows.Forms.DockStyle.Bottom;
            dgvStops.Dock = System.Windows.Forms.DockStyle.Fill;
        }

        private void flpPlanContent_Layout(object sender, LayoutEventArgs e)
        {
            int availWidth = flpPlanContent.ClientSize.Width;
            int availHeight = flpPlanContent.ClientSize.Height;

            int fixedHeight = flpPlanSelector.Size.Height + flpPlanSelector.Margin.Top + flpPlanSelector.Margin.Bottom
                + flpPlanName.Size.Height + flpPlanName.Margin.Top + flpPlanName.Margin.Bottom
                + lblPlanStop.Size.Height + lblPlanStop.Margin.Top + lblPlanStop.Margin.Bottom
                + lblDropOff.Size.Height + lblDropOff.Margin.Top + lblDropOff.Margin.Bottom
                + flpDropOffAdd.Size.Height + flpDropOffAdd.Margin.Top + flpDropOffAdd.Margin.Bottom
                + lblPickUp.Size.Height + lblPickUp.Margin.Top + lblPickUp.Margin.Bottom
                + flpPickUpAdd.Size.Height + flpPickUpAdd.Margin.Top + flpPickUpAdd.Margin.Bottom
                + dgvDropOff.Margin.Top + dgvDropOff.Margin.Bottom
                + dgvPickUp.Margin.Top + dgvPickUp.Margin.Bottom;

            int gridSpace = availHeight - fixedHeight;
            int perGrid = Math.Max(50, gridSpace / 2);

            dgvDropOff.Size = new Size(availWidth - dgvDropOff.Margin.Left - dgvDropOff.Margin.Right, perGrid);
            dgvPickUp.Size = new Size(availWidth - dgvPickUp.Margin.Left - dgvPickUp.Margin.Right, perGrid);
        }

        // -----------------------------------------------------------------------
        // Route List
        // -----------------------------------------------------------------------

        private void PopulateRouteList()
        {
            lvwRoutes.Items.Clear();
            var routes = viewModel.GetFilteredRoutes(txtRouteFilter.Text);
            foreach (var route in routes)
            {
                var item = new ListViewItem(route.Name);
                item.Tag = route;
                lvwRoutes.Items.Add(item);
            }
        }

        private void txtRouteFilter_TextChanged(object sender, EventArgs e)
        {
            PopulateRouteList();
        }

        private void lvwRoutes_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (lvwRoutes.SelectedItems.Count == 1)
            {
                var route = lvwRoutes.SelectedItems[0].Tag as Baseline.DeliveryRoute;
                viewModel.SelectRoute(route);
                PopulateForm();

                // Load or create the delivery plan for this route
                planViewModel = null;
                selectedPlanStop = null;
                PopulatePlanDropdown();

                // Auto-select the first open (non-completed) plan
                var firstOpenPlan = playerContext.GetCurrentPlayerPlans()
                    .Where(p => p.RouteUUID == route.UUID && !p.Completed)
                    .OrderBy(p => p.Name)
                    .FirstOrDefault();
                if (firstOpenPlan != null)
                {
                    cmbPlan.SelectedValue = firstOpenPlan.UUID;
                }

                // Trigger plan tab update for the currently selected stop
                dgvStops_SelectionChanged(sender, e);
            }
        }

        // -----------------------------------------------------------------------
        // Colony Picker
        // -----------------------------------------------------------------------

        private void PopulateColonyPicker()
        {
            var colonies = playerContext.GetCurrentPlayerColonies()
                .OrderBy(c => c.PlanetName)
                .ToList();

            // Filter out colonies already in the route when "No Duplicates" is checked
            if (chkPreventDuplicates.Checked)
            {
                var existingUUIDs = new HashSet<string>(viewModel.Stops.Select(s => s.ColonyUUID));
                colonies = colonies.Where(c => !existingUUIDs.Contains(c.UUID)).ToList();
            }

            var items = new List<ColonyPickerItem>();
            items.Add(new ColonyPickerItem { UUID = "", Display = "" });
            foreach (var colony in colonies)
            {
                string display = $"{colony.PlanetName} - {colony.ColonyName}";
                if (!string.IsNullOrEmpty(colony.SystemName))
                    display += $" ({colony.SystemName})";
                items.Add(new ColonyPickerItem { UUID = colony.UUID, Display = display });
            }

            cmbColony.DataSource = null;
            cmbColony.DisplayMember = "Display";
            cmbColony.ValueMember = "UUID";
            cmbColony.DataSource = items;
        }

        private class ColonyPickerItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
        }

        // -----------------------------------------------------------------------
        // Form Population
        // -----------------------------------------------------------------------

        private void PopulateForm()
        {
            txtRouteName.Text = viewModel.Name ?? "";
            PopulateStopsGrid();
        }

        private void PopulateStopsGrid()
        {
            var guard = new ProgrammaticUpdateGuard(this);
            dgvStops.Rows.Clear();
            foreach (var stop in viewModel.Stops)
            {
                var colony = playerContext.FindColony(stop.ColonyUUID);
                int rowIndex = dgvStops.Rows.Add(
                    stop.Sequence + 1,
                    colony?.ColonyName ?? "(unknown)",
                    colony?.PlanetName ?? "",
                    colony?.SystemName ?? "");
                dgvStops.Rows[rowIndex].Tag = stop;
            }
        }

        private void ClearForm()
        {
            viewModel.Reset();
            txtRouteName.Text = "";
            dgvStops.Rows.Clear();
            PopulateColonyPicker();
            planViewModel = null;
            selectedPlanStop = null;
            txtPlanName.Text = "";
            txtPlanName.SetError("Plan name is required");
            cmbPlan.DataSource = null;
            ClearPlanGrids();
        }

        // -----------------------------------------------------------------------
        // Stop Management
        // -----------------------------------------------------------------------

        private void cmdAddStop_Click(object sender, EventArgs e)
        {
            string colonyUUID = cmbColony.SelectedValue as string;
            if (string.IsNullOrEmpty(colonyUUID)) return;

            viewModel.AddStop(colonyUUID);
            PopulateStopsGrid();
            if (chkPreventDuplicates.Checked) PopulateColonyPicker();
        }

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            var newIndices = viewModel.MoveStopsUp(indices);
            PopulateStopsGrid();
            dgvStops.ClearSelection();
            foreach (int i in newIndices)
                if (i >= 0 && i < dgvStops.Rows.Count)
                    dgvStops.Rows[i].Selected = true;
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            var newIndices = viewModel.MoveStopsDown(indices);
            PopulateStopsGrid();
            dgvStops.ClearSelection();
            foreach (int i in newIndices)
                if (i >= 0 && i < dgvStops.Rows.Count)
                    dgvStops.Rows[i].Selected = true;
        }

        private void cmdRemoveStop_Click(object sender, EventArgs e)
        {
            if (dgvStops.SelectedRows.Count == 0) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvStops.SelectedRows)
                indices.Add(row.Index);
            viewModel.RemoveStops(indices);
            PopulateStopsGrid();
            if (chkPreventDuplicates.Checked) PopulateColonyPicker();
        }

        // -----------------------------------------------------------------------
        // Commands
        // -----------------------------------------------------------------------

        private void cmdNew_Click(object sender, EventArgs e)
        {
            ClearForm();
            lvwRoutes.SelectedItems.Clear();
        }

        private void cmdSave_Click(object sender, EventArgs e)
        {
            viewModel.Name = txtRouteName.Text;
            viewModel.Save();

            // Save the plan name if a plan is selected
            string selectedPlanUUID = null;
            if (planViewModel != null && !string.IsNullOrEmpty(planViewModel.UUID))
            {
                planViewModel.Data.Name = txtPlanName.Text;
                planViewModel.Save();
                selectedPlanUUID = planViewModel.UUID;
            }

            PopulateRouteList();
            PopulatePlanDropdown();
            if (selectedPlanUUID != null)
            {
                cmbPlan.SelectedValue = selectedPlanUUID;
            }
        }

        private void cmdDelete_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.UUID)) return;

            var result = MessageBox.Show(
                $"Delete route '{viewModel.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            viewModel.Delete();
            ClearForm();
            PopulateRouteList();
        }

        // -----------------------------------------------------------------------
        // Plan Tab
        // -----------------------------------------------------------------------

        // -----------------------------------------------------------------------
        // Plan Selector
        // -----------------------------------------------------------------------

        private class PlanDropdownItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
        }

        private void PopulatePlanDropdown()
        {
            string routeUUID = viewModel.UUID;
            if (string.IsNullOrEmpty(routeUUID))
            {
                cmbPlan.DataSource = null;
                return;
            }

            // Preserve current selection
            string previousUUID = cmbPlan.SelectedValue as string;

            string filter = txtPlanFilter.Text ?? "";
            bool showCompleted = chkShowCompleted.Checked;

            var plans = playerContext.GetCurrentPlayerPlans()
                .Where(p => p.RouteUUID == routeUUID)
                .Where(p => showCompleted || !p.Completed)
                .Where(p => string.IsNullOrEmpty(filter) || (p.Name ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.Name)
                .ToList();

            var items = new List<PlanDropdownItem>();
            items.Add(new PlanDropdownItem { UUID = "", Display = "" });
            foreach (var plan in plans)
            {
                string display = plan.Name;
                if (plan.Completed) display += " [Done]";
                items.Add(new PlanDropdownItem { UUID = plan.UUID, Display = display });
            }

            cmbPlan.DataSource = null;
            cmbPlan.DisplayMember = "Display";
            cmbPlan.ValueMember = "UUID";
            cmbPlan.DataSource = items;

            // Restore previous selection if still in the list
            if (!string.IsNullOrEmpty(previousUUID) && items.Any(i => i.UUID == previousUUID))
            {
                cmbPlan.SelectedValue = previousUUID;
            }
        }

        private void cmbPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            string planUUID = cmbPlan.SelectedValue as string;
            if (string.IsNullOrEmpty(planUUID))
            {
                planViewModel = null;
                selectedPlanStop = null;
                txtPlanName.Text = "";
                txtPlanName.SetError("Plan name is required");
                cmdAutoFill.Visible = false;
                ClearPlanGrids();
                return;
            }

            var plan = playerContext.deliveryPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (plan != null)
            {
                planViewModel = new DeliveryPlanViewModel(plan, playerContext);
                txtPlanName.Text = plan.Name ?? "";
                cmdAutoFill.Visible = true;
                // Re-trigger stop selection to load plan items
                dgvStops_SelectionChanged(sender, e);
            }
        }

        private void txtPlanName_TextChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlanName.Text))
            {
                txtPlanName.SetError("Plan name is required");
            }
            else
            {
                txtPlanName.ClearError();
            }
        }

        private void cmdExecutePlan_Click(object sender, EventArgs e)
        {
            if (planViewModel == null || string.IsNullOrEmpty(planViewModel.UUID) || string.IsNullOrEmpty(viewModel.UUID))
            {
                MessageBox.Show("Select a plan to execute.", "No Plan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var execution = new DeliveryExecution.FormDeliveryExecution(viewModel.UUID, planViewModel.UUID);
            execution.MdiParent = this.MdiParent;
            execution.Show();
        }

        private void cmdAutoFill_Click(object sender, EventArgs e)
        {
            if (planViewModel == null || string.IsNullOrEmpty(planViewModel.UUID)) return;

            using (var dlg = new FormAutoFill())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                int added = 0;
                Func<string, Baseline.Colony> colonyFinder = uuid => playerContext.FindColony(uuid);

                if (dlg.IncludeCommodities)
                    added += planViewModel.AutoFillCommodities(viewModel.Stops, colonyFinder);

                if (dlg.IncludeFlatpacks)
                    added += planViewModel.AutoFillFlatpacks(viewModel.Stops, colonyFinder);

                if (dlg.IncludeResources)
                    added += planViewModel.AutoFillManufacturingResources(viewModel.Stops, colonyFinder,
                        uuid => playerContext.FindBlueprint(uuid));

                if (dlg.IncludeWorkers)
                    added += planViewModel.AutoFillWorkers(viewModel.Stops, colonyFinder, playerContext);

                if (added > 0)
                {
                    planViewModel.Save();
                    PopulatePlanGrids();
                }
            }
        }

        private void cmdNewPlan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(viewModel.UUID))
            {
                MessageBox.Show("Save the route first.", "No Route", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Use the name the user already typed, or auto-suggest
            string name = txtPlanName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                name = $"{viewModel.Data.Name} - {DateTime.Now:yyyy-MM-dd}";
            }

            var plan = new DeliveryPlan
            {
                UUID = Guid.NewGuid().ToString(),
                Name = name,
                OwnerUUID = playerContext.CurrentPlayerUUID,
                RouteUUID = viewModel.UUID
            };
            playerContext.deliveryPlanList.Add(plan);
            playerContext.writeContext();

            PopulatePlanDropdown();
            cmbPlan.SelectedValue = plan.UUID;
        }

        private void cmdDeletePlan_Click(object sender, EventArgs e)
        {
            if (planViewModel == null || string.IsNullOrEmpty(planViewModel.UUID)) return;

            var result = MessageBox.Show(
                $"Delete plan '{planViewModel.Data.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.deliveryPlanList.Remove(planViewModel.Data);
            playerContext.writeContext();
            planViewModel = null;
            selectedPlanStop = null;
            txtPlanName.Text = "";
            ClearPlanGrids();
            PopulatePlanDropdown();

            // Select next open plan if one exists
            var remaining = playerContext.GetCurrentPlayerPlans()
                .Where(p => p.RouteUUID == viewModel.UUID && !p.Completed)
                .FirstOrDefault();
            if (remaining != null)
            {
                cmbPlan.SelectedValue = remaining.UUID;
            }
        }

        private void ClearPlanGrids()
        {
            var guard = new ProgrammaticUpdateGuard(this);
            lblPlanStop.Text = "(select a stop on Stops tab)";
            dgvDropOff.Rows.Clear();
            dgvPickUp.Rows.Clear();
        }

        /// <summary>
        /// Ensures a plan exists for the current route. If no plan is selected,
        /// auto-creates one (like clicking New). Returns true if a plan is ready.
        /// </summary>
        private bool EnsurePlanExists()
        {
            if (planViewModel != null && !string.IsNullOrEmpty(planViewModel.UUID))
                return true;

            if (string.IsNullOrEmpty(viewModel.UUID))
                return false;

            // Auto-create a plan
            cmdNewPlan_Click(this, EventArgs.Empty);
            return planViewModel != null && !string.IsNullOrEmpty(planViewModel.UUID);
        }

        // -----------------------------------------------------------------------
        // Plan Tab — Stop Items
        // -----------------------------------------------------------------------

        private void dgvStops_SelectionChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            if (planViewModel == null)
            {
                selectedPlanStop = null;
                ClearPlanGrids();
                return;
            }

            if (dgvStops.SelectedRows.Count != 1)
            {
                selectedPlanStop = null;
                ClearPlanGrids();
                return;
            }

            var routeStop = dgvStops.SelectedRows[0].Tag as RouteStop;
            if (routeStop == null) return;

            selectedPlanStop = planViewModel.GetOrCreateStop(routeStop.ColonyUUID, routeStop.Sequence);
            var colony = playerContext.FindColony(routeStop.ColonyUUID);
            lblPlanStop.Text = colony != null
                ? $"{colony.PlanetName} - {colony.ColonyName}"
                : "(unknown colony)";
            PopulatePlanGrids();
        }

        private void PopulateItemTypeCombos()
        {
            IReadOnlyList<ItemType> itemTypes = Data.ItemType.ItemTypes;
            cmbDropItemType.DataSource = new List<ItemType>(itemTypes);
            cmbDropItemType.DisplayMember = "Name";
            cmbDropItemType.ValueMember = "ID";
            cmbPickItemType.DataSource = new List<ItemType>(itemTypes);
            cmbPickItemType.DisplayMember = "Name";
            cmbPickItemType.ValueMember = "ID";
        }

        private void PopulatePurityCombos()
        {
            var purities = new List<Data.ResourcePurity>(Data.ResourcePurity.Purities);
            cmbDropPurity.DataSource = new List<Data.ResourcePurity>(purities);
            cmbDropPurity.DisplayMember = "Name";
            cmbDropPurity.ValueMember = "Name";
            cmbPickPurity.DataSource = new List<Data.ResourcePurity>(purities);
            cmbPickPurity.DisplayMember = "Name";
            cmbPickPurity.ValueMember = "Name";
        }

        private void UpdatePurityVisibility(ComboBox typeCombo, ComboBox purityCombo)
        {
            var itemType = typeCombo.SelectedItem as ItemType;
            purityCombo.Visible = itemType != null && itemType.ID == ItemType.ItemTypeEnum.Resource;
        }

        private void PopulateItemPicker(ComboBox typeCombo, ValidatedTextBox filterBox, ComboBox itemCombo)
        {
            var itemType = typeCombo.SelectedItem as ItemType;
            if (itemType == null) return;

            string filter = filterBox.Text ?? "";
            itemCombo.DataSource = null;
            var items = GetItemsForType(itemType.ID);
            if (!string.IsNullOrEmpty(filter))
            {
                items = items.Where(i =>
                    string.IsNullOrEmpty(i.Display) ||
                    i.Display.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
            itemCombo.DisplayMember = "Display";
            itemCombo.ValueMember = "ID";
            itemCombo.DataSource = items;
        }

        private List<ItemPickerEntry> GetItemsForType(ItemType.ItemTypeEnum typeEnum)
        {
            var result = new List<ItemPickerEntry>();
            result.Add(new ItemPickerEntry { ID = "", Display = "" });

            switch (typeEnum)
            {
                case ItemType.ItemTypeEnum.Resource:
                    foreach (var r in Data.Resource.Resources)
                        if (!string.IsNullOrEmpty(r.Name))
                            result.Add(new ItemPickerEntry { ID = r.Name, Display = r.Name });
                    break;
                case ItemType.ItemTypeEnum.Commodity:
                    foreach (var c in Data.Commodity.Commodities)
                        if (!string.IsNullOrEmpty(c.Name))
                            result.Add(new ItemPickerEntry { ID = c.Name, Display = c.ExtendedName });
                    break;
                case ItemType.ItemTypeEnum.WorkDetail:
                    foreach (var w in Data.WorkerDetail.WorkerDetails)
                        if (!string.IsNullOrEmpty(w.ID))
                            result.Add(new ItemPickerEntry { ID = w.ID, Display = w.Name });
                    break;
                default:
                    // Blueprint-based types
                    foreach (var bp in playerContext.GetAllBlueprints())
                        if (bp.UUID != null)
                            result.Add(new ItemPickerEntry { ID = bp.UUID, Display = bp.ExtendedName });
                    break;
            }
            return result;
        }

        private class ItemPickerEntry
        {
            public string ID { get; set; }
            public string Display { get; set; }
        }

        private void PopulatePlanGrids()
        {
            var guard = new ProgrammaticUpdateGuard(this);
            dgvDropOff.Rows.Clear();
            dgvPickUp.Rows.Clear();
            if (selectedPlanStop == null) return;

            foreach (var item in selectedPlanStop.DropOff)
            {
                int idx = dgvDropOff.Rows.Add(item.ItemType.ToString(), item.ExtendedName, item.Quantity);
                dgvDropOff.Rows[idx].Tag = item;
            }
            foreach (var item in selectedPlanStop.PickUp)
            {
                int idx = dgvPickUp.Rows.Add(item.ItemType.ToString(), item.ExtendedName, item.Quantity);
                dgvPickUp.Rows[idx].Tag = item;
            }
        }

        private void cmdAddDropOff_Click(object sender, EventArgs e)
        {
            if (!EnsurePlanExists()) return;
            if (selectedPlanStop == null)
            {
                // Try to get the stop from the grid selection
                dgvStops_SelectionChanged(sender, e);
                if (selectedPlanStop == null) return;
            }
            var entry = cmbDropItem.SelectedItem as ItemPickerEntry;
            if (entry == null || string.IsNullOrEmpty(entry.ID)) return;
            var itemType = cmbDropItemType.SelectedItem as ItemType;
            int qty = 1;
            int.TryParse(txtDropQty.Text, out qty);
            if (qty <= 0) qty = 1;

            planViewModel.AddDropOffItem(selectedPlanStop, itemType.ID, entry.ID, entry.Display, qty,
                itemType.ID == ItemType.ItemTypeEnum.Resource ? (cmbDropPurity.SelectedItem as Data.ResourcePurity)?.Name ?? "" : "");
            PopulatePlanGrids();
        }

        private void cmdAddPickUp_Click(object sender, EventArgs e)
        {
            if (!EnsurePlanExists()) return;
            if (selectedPlanStop == null)
            {
                dgvStops_SelectionChanged(sender, e);
                if (selectedPlanStop == null) return;
            }
            var entry = cmbPickItem.SelectedItem as ItemPickerEntry;
            if (entry == null || string.IsNullOrEmpty(entry.ID)) return;
            var itemType = cmbPickItemType.SelectedItem as ItemType;
            int qty = 1;
            int.TryParse(txtPickQty.Text, out qty);
            if (qty <= 0) qty = 1;

            planViewModel.AddPickUpItem(selectedPlanStop, itemType.ID, entry.ID, entry.Display, qty,
                itemType.ID == ItemType.ItemTypeEnum.Resource ? (cmbPickPurity.SelectedItem as Data.ResourcePurity)?.Name ?? "" : "");
            PopulatePlanGrids();
        }

        private void cmdRemoveDropOff_Click(object sender, EventArgs e)
        {
            if (selectedPlanStop == null || planViewModel == null) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvDropOff.SelectedRows)
                indices.Add(row.Index);
            planViewModel.RemoveDropOffItems(selectedPlanStop, indices);
            PopulatePlanGrids();
        }

        private void cmdRemovePickUp_Click(object sender, EventArgs e)
        {
            if (selectedPlanStop == null || planViewModel == null) return;
            var indices = new List<int>();
            foreach (DataGridViewRow row in dgvPickUp.SelectedRows)
                indices.Add(row.Index);
            planViewModel.RemovePickUpItems(selectedPlanStop, indices);
            PopulatePlanGrids();
        }

        // -----------------------------------------------------------------------
        // Player Change
        // -----------------------------------------------------------------------

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            ClearForm();
            PopulateRouteList();
            PopulateColonyPicker();
        }
    }
}
