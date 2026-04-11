using NLog;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.DeliveryExecution
{
    public partial class FormDeliveryExecution : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private int _isProgrammaticUpdate = 0;
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private DeliveryPlan selectedPlan;
        private string preSelectRouteUUID;
        private string preSelectPlanUUID;
        private Dictionary<DeliveryPlanStop, Button> _stopCompleteButtons = new Dictionary<DeliveryPlanStop, Button>();

        public FormDeliveryExecution()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.SelectedIndexChanged += cmbRoute_SelectedIndexChanged;

            cmbPlan.DisplayMember = "Display";
            cmbPlan.ValueMember = "UUID";
            cmbPlan.SelectedIndexChanged += cmbPlan_SelectedIndexChanged;

            txtRouteFilter.TextChanged += (s, ev) => PopulateRouteDropdown();
            txtPlanFilter.TextChanged += (s, ev) =>
            {
                string routeUUID = cmbRoute.SelectedValue as string;
                if (!string.IsNullOrEmpty(routeUUID)) PopulatePlanDropdown(routeUUID);
            };

            cmdCompletePlan.Click += cmdCompletePlan_Click;
            cmdDeletePlan.Click += cmdDeletePlan_Click;
            cmdCompletePlan.Visible = false;
            cmdDeletePlan.Visible = false;

            PopulateRouteDropdown();

            flpBase.Layout += flpBase_Layout;
            flpSelectors.Layout += flpSelectors_Layout;
            pnlExecution.Layout += pnlExecution_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.DeliveryDataChanged += OnDeliveryDataChanged;
        }

        /// <summary>
        /// Constructor for launching from route builder with pre-selected route and plan.
        /// </summary>
        public FormDeliveryExecution(string routeUUID, string planUUID) : this()
        {
            preSelectRouteUUID = routeUUID;
            preSelectPlanUUID = planUUID;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!string.IsNullOrEmpty(preSelectRouteUUID))
            {
                cmbRoute.SelectedValue = preSelectRouteUUID;
                if (!string.IsNullOrEmpty(preSelectPlanUUID))
                {
                    cmbPlan.SelectedValue = preSelectPlanUUID;
                }
            }
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void flpBase_Layout(object sender, LayoutEventArgs e)
        {
            pnlExecution.Size = new Size(
                flpBase.Size.Width - flpSelectors.Size.Width - flpSelectors.Margin.Right - flpSelectors.Margin.Left - pnlExecution.Margin.Left - pnlExecution.Margin.Right,
                flpBase.Size.Height - pnlExecution.Margin.Top - pnlExecution.Margin.Bottom);
            flpSelectors.Size = new Size(
                flpSelectors.Size.Width,
                flpBase.Size.Height - flpSelectors.Margin.Top - flpSelectors.Margin.Bottom);
        }

        private void pnlExecution_Layout(object sender, LayoutEventArgs e)
        {
            int w = pnlExecution.ClientSize.Width - dgvLoadList.Margin.Left - dgvLoadList.Margin.Right;
            dgvLoadList.Width = w;
            flpStops.Width = w;
        }

        private void flpSelectors_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSelectors.Size.Width - 6;
            txtRouteFilter.Size = new Size(w, txtRouteFilter.Size.Height);
            cmbRoute.Size = new Size(w, cmbRoute.Size.Height);
            txtPlanFilter.Size = new Size(w, txtPlanFilter.Size.Height);
            cmbPlan.Size = new Size(w, cmbPlan.Size.Height);
        }

        // -----------------------------------------------------------------------
        // Route / Plan Selection
        // -----------------------------------------------------------------------

        private class DropdownItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
        }

        private void PopulateRouteDropdown()
        {
            string previousUUID = cmbRoute.SelectedValue as string;
            string filter = txtRouteFilter.Text ?? "";

            cmbRoute.SelectedIndexChanged -= cmbRoute_SelectedIndexChanged;

            var routes = playerContext.GetCurrentPlayerRoutes();
            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = "", Display = "" });
            foreach (var route in routes.OrderBy(r => r.Name))
            {
                if (!string.IsNullOrEmpty(filter) && route.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                items.Add(new DropdownItem { UUID = route.UUID, Display = route.Name });
            }
            cmbRoute.DataSource = null;
            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.DataSource = items;
            if (!string.IsNullOrEmpty(previousUUID) && items.Any(i => i.UUID == previousUUID))
            {
                cmbRoute.SelectedValue = previousUUID;
                _lastRouteUUID = previousUUID;
            }
            else
            {
                _lastRouteUUID = "";
            }

            cmbRoute.SelectedIndexChanged += cmbRoute_SelectedIndexChanged;
        }

        private string _lastRouteUUID = "";

        private void cmbRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            string routeUUID = cmbRoute.SelectedValue as string ?? "";
            if (routeUUID == _lastRouteUUID) return; // Route didn't change
            _lastRouteUUID = routeUUID;

            if (string.IsNullOrEmpty(routeUUID))
            {
                cmbPlan.DataSource = null;
                ClearExecution();
                return;
            }
            PopulatePlanDropdown(routeUUID);
        }

        private void PopulatePlanDropdown(string routeUUID)
        {
            string previousUUID = cmbPlan.SelectedValue as string;
            string filter = txtPlanFilter.Text ?? "";
            var plans = playerContext.GetCurrentPlayerPlans()
                .Where(p => p.RouteUUID == routeUUID && !p.Completed)
                .Where(p => string.IsNullOrEmpty(filter) || (p.Name ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.Name)
                .ToList();

            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = "", Display = "" });
            foreach (var plan in plans)
            {
                items.Add(new DropdownItem { UUID = plan.UUID, Display = plan.Name });
            }
            cmbPlan.DataSource = null;
            cmbPlan.DisplayMember = "Display";
            cmbPlan.ValueMember = "UUID";
            cmbPlan.DataSource = items;
            if (!string.IsNullOrEmpty(previousUUID) && items.Any(i => i.UUID == previousUUID))
                cmbPlan.SelectedValue = previousUUID;
        }

        private void cmbPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            string planUUID = cmbPlan.SelectedValue as string;
            if (string.IsNullOrEmpty(planUUID))
            {
                selectedPlan = null;
                ClearExecution();
                cmdCompletePlan.Visible = false;
                cmdDeletePlan.Visible = false;
                return;
            }

            selectedPlan = playerContext.DeliveryPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (selectedPlan != null)
            {
                cmdCompletePlan.Visible = true;
                cmdDeletePlan.Visible = true;
                BuildExecution();
            }
        }

        // -----------------------------------------------------------------------
        // Execution Display
        // -----------------------------------------------------------------------

        private void ClearExecution()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            dgvLoadList.Rows.Clear();
            flpStops.Controls.Clear();
            _stopCompleteButtons.Clear();
            selectedPlan = null;
            cmdCompletePlan.Visible = false;
            cmdDeletePlan.Visible = false;
            lblLoadListHeader.Text = "Load Before Departure";
        }

        private void BuildExecution()
        {
            using var guard = new ProgrammaticUpdateGuard(this);
            var scrollPos = pnlExecution.AutoScrollPosition;
            this.SuspendLayout();
            pnlExecution.SuspendLayout();
            flpStops.SuspendLayout();
            dgvLoadList.Rows.Clear();
            flpStops.Controls.Clear();
            _stopCompleteButtons.Clear();

            if (selectedPlan == null)
            {
                flpStops.ResumeLayout();
                pnlExecution.ResumeLayout();
                this.ResumeLayout();
                return;
            }

            Log.Debug("BuildExecution: plan={0}, stops={1}", selectedPlan.Name, selectedPlan.Stops.Count);

            // Build consolidated load list
            var loadItems = selectedPlan.CalculateLoadList();
            Log.Debug("BuildExecution: loadItems={0}", loadItems.Count);
            int totalQuantity = 0;
            decimal totalVolume = 0m;
            foreach (var item in loadItems)
            {
                Log.Debug("  Load: {0} x{1}", item.BaseItemTypeID, item.Quantity);
                dgvLoadList.Rows.Add(item.ItemType.ToString(), item.BaseItemTypeID, item.ExtendedName, item.Quantity);
                totalQuantity += item.Quantity;
                totalVolume += item.Quantity * GetLoadItemVolume(item);
            }

            // Update header with totals
            if (loadItems.Count > 0)
            {
                lblLoadListHeader.Text = string.Format("Load Before Departure — {0} items, {1} qty, {2:N0} vol",
                    loadItems.Count, totalQuantity, totalVolume);
            }
            else
            {
                lblLoadListHeader.Text = "Load Before Departure";
            }

            // Build per-stop sections
            foreach (var stop in selectedPlan.Stops.OrderBy(s => s.Sequence))
            {
                // Skip completed stops
                if (stop.StopCompleted) continue;

                var colony = playerContext.FindColony(stop.ColonyUUID);
                string stopTitle = colony != null
                    ? $"Stop {stop.Sequence + 1}: {colony.PlanetName} - {colony.ColonyName}"
                    : $"Stop {stop.Sequence + 1}: (unknown)";

                // Header label
                var lblStop = new Label
                {
                    Text = stopTitle,
                    Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                    AutoSize = true,
                    Margin = new Padding(3, 10, 3, 3)
                };
                flpStops.Controls.Add(lblStop);

                // Drop-off items
                if (stop.DropOff.Count > 0)
                {
                    var lblDrop = new Label { Text = "  Drop Off:", AutoSize = true, Margin = new Padding(10, 2, 3, 2) };
                    flpStops.Controls.Add(lblDrop);

                    foreach (var item in stop.DropOff)
                    {
                        var chk = new CheckBox
                        {
                            Text = $"{item.ExtendedName} x{item.Quantity}",
                            Checked = item.Delivered,
                            AutoSize = true,
                            Margin = new Padding(20, 1, 3, 1),
                            Tag = item
                        };
                        chk.CheckedChanged += DeliveryItem_CheckedChanged;
                        flpStops.Controls.Add(chk);
                    }
                }

                // Pick-up items
                if (stop.PickUp.Count > 0)
                {
                    var lblPick = new Label { Text = "  Pick Up:", AutoSize = true, Margin = new Padding(10, 2, 3, 2) };
                    flpStops.Controls.Add(lblPick);

                    foreach (var item in stop.PickUp)
                    {
                        var chk = new CheckBox
                        {
                            Text = $"{item.ExtendedName} x{item.Quantity}",
                            Checked = item.Delivered,
                            AutoSize = true,
                            Margin = new Padding(20, 1, 3, 1),
                            Tag = item
                        };
                        chk.CheckedChanged += DeliveryItem_CheckedChanged;
                        flpStops.Controls.Add(chk);
                    }
                }

                // Show "Complete Stop" button if all items at this stop are delivered
                bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                    && (stop.DropOff.Count > 0 || stop.PickUp.Count > 0);
                if (allDelivered)
                {
                    var btnComplete = new Button
                    {
                        Text = "Complete Stop",
                        AutoSize = true,
                        Margin = new Padding(20, 3, 3, 3),
                        Tag = stop
                    };
                    btnComplete.Click += CompleteStop_Click;
                    flpStops.Controls.Add(btnComplete);
                    _stopCompleteButtons[stop] = btnComplete;
                }
            }

            flpStops.ResumeLayout();
            pnlExecution.ResumeLayout();
            this.ResumeLayout();

            // Restore scroll position
            pnlExecution.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
        }

        // -----------------------------------------------------------------------
        // Item Delivery Handling
        // -----------------------------------------------------------------------

        private void DeliveryItem_CheckedChanged(object sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null) return;

            var item = chk.Tag as DeliveryItem;
            if (item == null) return;

            item.Delivered = chk.Checked;

            // Commodity fulfillment: update CommodityRequested on the target colony
            if (item.ItemType == ItemType.ItemTypeEnum.Commodity && selectedPlan != null)
            {
                UpdateCommodityFulfillment(item, chk.Checked);
            }

            // Flatpack staging: mark matching colony structure as staged/unstaged
            if (item.ItemType == ItemType.ItemTypeEnum.Flatpack && selectedPlan != null)
            {
                UpdateFlatpackStaging(item, chk.Checked);
            }

            // Worker delivery: add/remove workers from colony warehouse
            if (item.ItemType == ItemType.ItemTypeEnum.WorkDetail && selectedPlan != null)
            {
                UpdateWorkerDelivery(item, chk.Checked);
            }

            playerContext.WriteContext();

            // Incrementally update the Complete Stop button for the affected stop
            var affectedStop = selectedPlan?.Stops.FirstOrDefault(s =>
                s.DropOff.Contains(item) || s.PickUp.Contains(item));
            UpdateStopCompleteButton(affectedStop);

            if (selectedPlan != null && IsAllDelivered(selectedPlan))
            {
                selectedPlan.Completed = true;
                playerContext.WriteContext();
                Log.Info("Delivery plan '{0}' marked as completed", selectedPlan.Name);
            }
        }

        private void UpdateCommodityFulfillment(DeliveryItem item, bool delivered)
        {
            // Find the stop containing this item
            var stop = selectedPlan.Stops.FirstOrDefault(s =>
                s.DropOff.Contains(item) || s.PickUp.Contains(item));
            if (stop == null) return;

            var colony = playerContext.FindColony(stop.ColonyUUID);
            if (colony == null)
            {
                Log.Warn("Colony not found for stop {0} during commodity fulfillment", stop.ColonyUUID);
                return;
            }

            DeliveryFulfillment.FulfillCommodity(colony, item.BaseItemTypeID, delivered);
            playerContext.OnColonyDataChanged(stop.ColonyUUID);
        }

        private void UpdateFlatpackStaging(DeliveryItem item, bool delivered)
        {
            var stop = selectedPlan.Stops.FirstOrDefault(s =>
                s.DropOff.Contains(item) || s.PickUp.Contains(item));
            if (stop == null) return;

            var colony = playerContext.FindColony(stop.ColonyUUID);
            if (colony == null)
            {
                Log.Warn("Colony not found for stop {0} during flatpack staging", stop.ColonyUUID);
                return;
            }

            DeliveryFulfillment.StageFlatpack(colony, item.BaseItemTypeID, delivered);
            playerContext.OnColonyDataChanged(stop.ColonyUUID);
        }

        private void UpdateWorkerDelivery(DeliveryItem item, bool delivered)
        {
            var stop = selectedPlan.Stops.FirstOrDefault(s =>
                s.DropOff.Contains(item) || s.PickUp.Contains(item));
            if (stop == null) return;

            var colony = playerContext.FindColony(stop.ColonyUUID);
            if (colony == null)
            {
                Log.Warn("Colony not found for stop {0} during worker delivery", stop.ColonyUUID);
                return;
            }

            DeliveryFulfillment.DeliverWorkers(colony, item.BaseItemTypeID, item.Name, item.Quantity, delivered);
            playerContext.OnColonyDataChanged(stop.ColonyUUID);
        }

        private void UpdateStopCompleteButton(DeliveryPlanStop stop)
        {
            if (stop == null) return;

            bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                && (stop.DropOff.Count > 0 || stop.PickUp.Count > 0);

            if (allDelivered && !_stopCompleteButtons.ContainsKey(stop))
            {
                // Find insert position before adding the button
                int insertAfter = FindLastControlIndexForStop(stop);

                var btnComplete = new Button
                {
                    Text = "Complete Stop",
                    AutoSize = true,
                    Margin = new Padding(20, 3, 3, 3),
                    Tag = stop
                };
                btnComplete.Click += CompleteStop_Click;

                flpStops.Controls.Add(btnComplete);

                // Position the button right after the last control for this stop
                if (insertAfter >= 0)
                    flpStops.Controls.SetChildIndex(btnComplete, insertAfter + 1);

                _stopCompleteButtons[stop] = btnComplete;
            }
            else if (!allDelivered && _stopCompleteButtons.ContainsKey(stop))
            {
                // Remove the Complete Stop button
                var btn = _stopCompleteButtons[stop];
                flpStops.Controls.Remove(btn);
                btn.Click -= CompleteStop_Click;
                btn.Dispose();
                _stopCompleteButtons.Remove(stop);
            }
        }

        /// <summary>
        /// Finds the index of the last control in flpStops that belongs to the given stop.
        /// Checks checkbox Tags (DeliveryItem) against the stop's DropOff/PickUp lists.
        /// </summary>
        private int FindLastControlIndexForStop(DeliveryPlanStop stop)
        {
            int lastIndex = -1;
            var stopItems = new HashSet<DeliveryItem>(stop.DropOff.Concat(stop.PickUp));

            for (int i = 0; i < flpStops.Controls.Count; i++)
            {
                var ctrl = flpStops.Controls[i];
                if (ctrl.Tag is DeliveryItem di && stopItems.Contains(di))
                    lastIndex = i;
                else if (ctrl.Tag == stop)
                    lastIndex = i;
            }
            return lastIndex;
        }

        private void CompleteStop_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var stop = btn.Tag as DeliveryPlanStop;
            if (stop == null) return;

            stop.StopCompleted = true;
            playerContext.WriteContext();
            BuildExecution();

            if (selectedPlan != null && IsAllDelivered(selectedPlan))
            {
                selectedPlan.Completed = true;
                playerContext.WriteContext();
                Log.Info("Delivery plan '{0}' marked as completed", selectedPlan.Name);
            }
        }

        private void cmdCompletePlan_Click(object sender, EventArgs e)
        {
            if (selectedPlan == null) return;

            selectedPlan.Completed = true;
            playerContext.WriteContext();
            Log.Info("Delivery plan '{0}' manually marked as completed", selectedPlan.Name);

            ClearExecution();
            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
        }

        private void cmdDeletePlan_Click(object sender, EventArgs e)
        {
            if (selectedPlan == null) return;

            var result = MessageBox.Show(
                $"Delete plan '{selectedPlan.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.DeliveryPlanList.Remove(selectedPlan);
            playerContext.WriteContext();
            Log.Info("Delivery plan '{0}' deleted", selectedPlan.Name);

            ClearExecution();
            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
        }

        private bool IsAllDelivered(DeliveryPlan plan)
        {
            foreach (var stop in plan.Stops)
            {
                foreach (var item in stop.DropOff)
                    if (!item.Delivered) return false;
                foreach (var item in stop.PickUp)
                    if (!item.Delivered) return false;
            }
            return true;
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            PopulateRouteDropdown();
            ClearExecution();
        }

        private void OnDeliveryDataChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnDeliveryDataChanged(sender, e))); }
                catch (ObjectDisposedException) { }
                return;
            }
            if (selectedPlan != null)
                BuildExecution();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.DeliveryDataChanged -= OnDeliveryDataChanged;
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Returns the per-unit cargo volume for a delivery item based on its type.
        /// Uses the same volume constants as the colony warehouse.
        /// </summary>
        private decimal GetLoadItemVolume(DeliveryItem item)
        {
            switch (item.ItemType)
            {
                case ItemType.ItemTypeEnum.Resource: return 1.0m;
                case ItemType.ItemTypeEnum.Commodity: return 10.0m;
                case ItemType.ItemTypeEnum.WorkDetail: return 50.0m;
                case ItemType.ItemTypeEnum.Blueprint:
                case ItemType.ItemTypeEnum.Survey: return 0.0m;
                default:
                    // Manufactured items: read CargoVolumeSize from blueprint
                    if (!string.IsNullOrEmpty(item.BaseItemTypeID))
                    {
                        var bp = playerContext.FindBlueprint(item.BaseItemTypeID);
                        if (bp != null)
                        {
                            bp.Properties.getDecimal("Cargo Volume Size", 0, out decimal vol);
                            return vol;
                        }
                    }
                    return 0.0m;
            }
        }
    }
}
