using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Persistence;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Forms.DeliveryExecution
{
    public partial class FormDeliveryExecution : Form, IProgrammaticUpdateSource
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private int _isProgrammaticUpdate = 0;

        private EmpireContext empireContext;

        private PlayerContext playerContext;

        private DeliveryPlanService _deliveryPlanService;

        private string selectedPlanUUID;

        private Ship selectedShip;

        private decimal currentCargoCapacity;

        private Dictionary<DeliveryPlanStop, Button> _stopCompleteButtons = new Dictionary<DeliveryPlanStop, Button>();

        private Dictionary<DeliveryPlanStop, CheckBox> _refuelCheckboxes = new Dictionary<DeliveryPlanStop, CheckBox>();

        private string _lastRouteUUID = string.Empty;

        public FormDeliveryExecution()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;
            _deliveryPlanService = new DeliveryPlanService(playerContext);

            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.SelectedIndexChanged += CmbRoute_SelectedIndexChanged;

            cmbPlan.DisplayMember = "Display";
            cmbPlan.ValueMember = "UUID";
            cmbPlan.SelectedIndexChanged += CmbPlan_SelectedIndexChanged;

            cmbShip.DisplayMember = "Display";
            cmbShip.ValueMember = "UUID";
            cmbShip.SelectedIndexChanged += CmbShip_SelectedIndexChanged;

            txtRouteFilter.TextChanged += (s, ev) => PopulateRouteDropdown();
            txtPlanFilter.TextChanged += (s, ev) =>
            {
                string routeUUID = cmbRoute.SelectedValue as string;
                if (!string.IsNullOrEmpty(routeUUID)) PopulatePlanDropdown(routeUUID);
            };

            cmdCompletePlan.Click += CmdCompletePlan_Click;
            cmdDeletePlan.Click += CmdDeletePlan_Click;
            cmdSplitTrips.Click += CmdSplitTrips_Click;
            cmdCompletePlan.Visible = false;
            cmdDeletePlan.Visible = false;
            cmdSplitTrips.Visible = false;

            PopulateRouteDropdown();

            flpBase.Layout += FlpBase_Layout;
            flpSelectors.Layout += FlpSelectors_Layout;
            pnlLoadList.Layout += PnlLoadList_Layout;

            // Wire context menu events (grid-context-menus spec, task 5.2)
            tsmiMarkAllDelivered.Click += TsmiMarkAllDelivered_Click;
            tsmiMarkAllUndelivered.Click += TsmiMarkAllUndelivered_Click;
            dgvLoadList.CellMouseClick += DgvLoadList_CellMouseClick;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.DeliveryDataChanged += OnDeliveryDataChanged;
        }

        public string PreSelectRouteUUID { get; set; }

        public string PreSelectPlanUUID { get; set; }

        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }

        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }

        /// <summary>
        /// Apply pre-selected route and plan after the form is already shown.
        /// Called by FormDeliveryRoute when launching via the Execute button.
        /// </summary>
        public void ApplyPreSelection()
        {
            if (!string.IsNullOrEmpty(PreSelectRouteUUID))
            {
                cmbRoute.SelectedValue = PreSelectRouteUUID;
                if (!string.IsNullOrEmpty(PreSelectPlanUUID))
                {
                    cmbPlan.SelectedValue = PreSelectPlanUUID;
                }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyPreSelection();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            WindowStateHelper.SaveState(this, this.GetType().Name, (int)this.Tag);
            playerContext.CurrentPlayerChanged -= OnCurrentPlayerChanged;
            playerContext.DeliveryDataChanged -= OnDeliveryDataChanged;
            base.OnFormClosed(e);
        }

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            splitExecution.Size = new Size(
                flpBase.Size.Width - flpSelectors.Size.Width - flpSelectors.Margin.Right - flpSelectors.Margin.Left - splitExecution.Margin.Left - splitExecution.Margin.Right,
                flpBase.Size.Height - splitExecution.Margin.Top - splitExecution.Margin.Bottom);
            flpSelectors.Size = new Size(
                flpSelectors.Size.Width,
                flpBase.Size.Height - flpSelectors.Margin.Top - flpSelectors.Margin.Bottom);
        }

        private void PnlLoadList_Layout(object sender, LayoutEventArgs e)
        {
            int w = pnlLoadList.ClientSize.Width - dgvLoadList.Margin.Left - dgvLoadList.Margin.Right;
            int h = pnlLoadList.ClientSize.Height;
            dgvLoadList.Width = w;

            // Fill remaining height with the grid
            int usedHeight = lblLoadListHeader.Height + lblLoadListHeader.Margin.Vertical
                + lblCargoVolume.Height + lblCargoVolume.Margin.Vertical
                + lblCargoMass.Height + lblCargoMass.Margin.Vertical;
            if (cmdSplitTrips.Visible)
                usedHeight += cmdSplitTrips.Height + cmdSplitTrips.Margin.Vertical;
            int gridHeight = h - usedHeight - dgvLoadList.Margin.Vertical - 4;
            if (gridHeight < 50) gridHeight = 50;
            dgvLoadList.Height = gridHeight;
        }

        private void FlpSelectors_Layout(object sender, LayoutEventArgs e)
        {
            int w = flpSelectors.Size.Width - 6;
            txtRouteFilter.Size = new Size(w, txtRouteFilter.Size.Height);
            cmbRoute.Size = new Size(w, cmbRoute.Size.Height);
            txtPlanFilter.Size = new Size(w, txtPlanFilter.Size.Height);
            cmbPlan.Size = new Size(w, cmbPlan.Size.Height);
            cmbShip.Size = new Size(w, cmbShip.Size.Height);
        }

        private void PopulateRouteDropdown()
        {
            _lastRouteUUID = RouteDropdownHelper.Populate(cmbRoute, playerContext.GetCurrentPlayerRoutes(), txtRouteFilter.Text ?? string.Empty, cmbRoute.SelectedValue as string, CmbRoute_SelectedIndexChanged);
        }

        private void CmbRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            string routeUUID = cmbRoute.SelectedValue as string ?? string.Empty;
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
            var sw = System.Diagnostics.Stopwatch.StartNew();
            string previousUUID = cmbPlan.SelectedValue as string;
            string filter = txtPlanFilter.Text ?? string.Empty;
            var plans = playerContext.GetCurrentPlayerReadOnlyPlans()
                .Where(p => p.RouteUUID == routeUUID && !p.Completed)
                .Where(p => string.IsNullOrEmpty(filter) || (p.Name ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            plans = CollectionSortHelper.OrderDeliveryPlans(plans).ToList();

            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = string.Empty, Display = string.Empty });
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
            sw.Stop();
            Log.Info("PERF PopulatePlanDropdown: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbPlan_SelectedIndexChanged(object sender, EventArgs e)
        {
            string planUUID = cmbPlan.SelectedValue as string;
            if (string.IsNullOrEmpty(planUUID))
            {
                selectedPlanUUID = null;
                ClearExecution();
                cmdCompletePlan.Visible = false;
                cmdDeletePlan.Visible = false;
                return;
            }

            selectedPlanUUID = planUUID;
            var readOnlyPlan = playerContext.GetCurrentPlayerReadOnlyPlans()
                .FirstOrDefault(p => p.UUID == planUUID);
            if (readOnlyPlan != null)
            {
                cmdCompletePlan.Visible = true;
                cmdDeletePlan.Visible = true;
                PopulateShipDropdown();
                BuildExecution();
            }
        }

        // -----------------------------------------------------------------------
        // Ship Selection
        // -----------------------------------------------------------------------

        private void PopulateShipDropdown()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbShip.SelectedIndexChanged -= CmbShip_SelectedIndexChanged;

            var ships = playerContext.GetCurrentPlayerShips();
            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = string.Empty, Display = "(no ship)" });
            foreach (var ship in CollectionSortHelper.OrderShips(ships))
            {
                items.Add(new DropdownItem { UUID = ship.UUID, Display = ship.Name });
            }

            cmbShip.DataSource = null;
            cmbShip.DisplayMember = "Display";
            cmbShip.ValueMember = "UUID";
            cmbShip.DataSource = items;

            // Pre-select the ship stored on the plan
            var currentPlan = playerContext.GetCurrentPlayerReadOnlyPlans()
                .FirstOrDefault(p => p.UUID == selectedPlanUUID);
            if (currentPlan != null && !string.IsNullOrEmpty(currentPlan.ShipUUID)
                && items.Any(i => i.UUID == currentPlan.ShipUUID))
            {
                cmbShip.SelectedValue = currentPlan.ShipUUID;
            }

            cmbShip.SelectedIndexChanged += CmbShip_SelectedIndexChanged;
            UpdateShipSelection();
            sw.Stop();
            Log.Info("PERF PopulateShipDropdown: {0}ms", sw.ElapsedMilliseconds);
        }

        private void CmbShip_SelectedIndexChanged(object sender, EventArgs e)
        {
            string shipUUID = cmbShip.SelectedValue as string ?? string.Empty;

            // Persist ship assignment on the plan
            if (!string.IsNullOrEmpty(selectedPlanUUID))
            {
                _deliveryPlanService.SetShipUUID(selectedPlanUUID, shipUUID);
                Log.Info("Ship '{0}' assigned to plan '{1}'", shipUUID, selectedPlanUUID);
            }

            UpdateShipSelection();
            UpdateCargoDisplay();
        }

        private void UpdateShipSelection()
        {
            string shipUUID = cmbShip.SelectedValue as string ?? string.Empty;
            selectedShip = null;
            currentCargoCapacity = 0m;

            if (!string.IsNullOrEmpty(shipUUID))
            {
                selectedShip = playerContext.FindShip(shipUUID);
                if (selectedShip != null)
                {
                    var hullBp = playerContext.FindBlueprint(selectedShip.HullBlueprintUUID);
                    if (hullBp != null)
                    {
                        var stats = ShipBuildService.ComputeStats(
                            hullBp,
                            selectedShip.Components,
                            uuid => playerContext.FindBlueprint(uuid));
                        currentCargoCapacity = stats.CargoCapacity;
                    }

                    lblShipCapacity.Text = string.Format("Cargo: {0:N0}", currentCargoCapacity);
                }
                else
                {
                    lblShipCapacity.Text = string.Empty;
                }
            }
            else
            {
                lblShipCapacity.Text = string.Empty;
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
            _refuelCheckboxes.Clear();
            selectedPlanUUID = null;
            cmdCompletePlan.Visible = false;
            cmdDeletePlan.Visible = false;
            cmdSplitTrips.Visible = false;
            lblLoadListHeader.Text = "Load Before Departure";
            lblCargoVolume.Text = string.Empty;
            lblCargoMass.Text = string.Empty;
            lblCargoVolume.ForeColor = System.Drawing.SystemColors.ControlText;
        }

        private void BuildExecution()
        {
            var sw = Stopwatch.StartNew();
            using var guard = new ProgrammaticUpdateGuard(this);
            var scrollPos = pnlExecution.AutoScrollPosition;
            this.SuspendLayout();
            pnlExecution.SuspendLayout();
            flpStops.SuspendLayout();
            dgvLoadList.Rows.Clear();
            flpStops.Controls.Clear();
            _stopCompleteButtons.Clear();
            _refuelCheckboxes.Clear();

            var mutablePlan = playerContext.FindMutableDeliveryPlan(selectedPlanUUID);
            if (mutablePlan == null)
            {
                flpStops.ResumeLayout();
                pnlExecution.ResumeLayout();
                this.ResumeLayout();
                return;
            }

            Log.Debug("BuildExecution: plan={0}, stops={1}", mutablePlan.Name, mutablePlan.Stops.Count);

            // Repair any duplicate stops (legacy data from autofill bug)
            _deliveryPlanService.RepairDuplicateStops(selectedPlanUUID);

            // Build consolidated load list (uses mutable plan for CalculateLoadList computation)
            var loadItems = mutablePlan.CalculateLoadList();
            Log.Debug("BuildExecution: loadItems={0}", loadItems.Count);
            int totalQuantity = 0;
            foreach (var item in loadItems)
            {
                Log.Debug("  Load: {0} x{1}", item.BaseItemTypeID, item.Quantity);
                dgvLoadList.Rows.Add(item.ItemType.ToString(), item.BaseItemTypeID, item.ExtendedName, item.Quantity);
                totalQuantity += item.Quantity;
            }

            // Compute volume and mass via CargoVolumeService
            Func<string, ReadOnlyBlueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
            var cargoResult = CargoVolumeService.ComputeLoadVolume(loadItems, bpFinder);

            // Update header with totals
            if (loadItems.Count > 0)
            {
                lblLoadListHeader.Text = string.Format(
                    "Load Before Departure -- {0} items, {1} qty, {2:N0} vol",
                    loadItems.Count,
                    totalQuantity,
                    cargoResult.TotalVolume);
            }
            else
            {
                lblLoadListHeader.Text = "Load Before Departure";
            }

            // Update cargo volume/mass display
            UpdateCargoDisplayFromResult(cargoResult);

            // Look up the route for stop purposes
            var route = playerContext.GetCurrentPlayerRoutes()
                .FirstOrDefault(r => r.UUID == mutablePlan.RouteUUID);

            // Build per-stop sections
            foreach (var stop in CollectionSortHelper.OrderPlanStops(mutablePlan.Stops))
            {
                // Skip completed stops
                if (stop.StopCompleted) continue;

                // Determine if this is a refuel stop (needed before deciding whether to show the stop)
                RouteStop matchingRouteStop = null;
                if (route != null)
                    matchingRouteStop = route.Stops.FirstOrDefault(rs => rs.Sequence == stop.Sequence);
                bool isRefuelStop = matchingRouteStop != null &&
                    (matchingRouteStop.Purpose == RouteStopPurpose.Refuel || matchingRouteStop.Purpose == RouteStopPurpose.CargoAndRefuel);

                // Skip stops with nothing to do (no drop-offs, no pick-ups, not a refuel stop)
                if (stop.DropOff.Count == 0 && stop.PickUp.Count == 0 && !isRefuelStop)
                    continue;

                string stopTitle;
                if (stop.DestinationType == DestinationType.Station)
                {
                    var station = playerContext.FindStation(stop.DestinationUUID);
                    stopTitle = station != null
                        ? $"Stop {stop.Sequence + 1}: {station.Name} [Station]"
                        : $"Stop {stop.Sequence + 1}: (unknown station)";
                }
                else if (stop.DestinationType == DestinationType.Asteroid)
                {
                    var asteroid = playerContext.FindAsteroid(stop.DestinationUUID);
                    stopTitle = asteroid != null
                        ? $"Stop {stop.Sequence + 1}: {asteroid.Name} [Asteroid]"
                        : $"Stop {stop.Sequence + 1}: (unknown asteroid)";
                }
                else
                {
                    string colUUID = !string.IsNullOrEmpty(stop.DestinationUUID) ? stop.DestinationUUID : stop.ColonyUUID;
                    var colony = playerContext.FindColony(colUUID);
                    stopTitle = colony != null
                        ? $"Stop {stop.Sequence + 1}: {colony.PlanetName} - {colony.ColonyName}"
                        : $"Stop {stop.Sequence + 1}: (unknown)";
                }

                // Header label
                var lblStop = new Label
                {
                    Text = stopTitle,
                    Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                    AutoSize = true,
                    Margin = new Padding(3, 10, 3, 3),
                    Tag = stop
                };

                flpStops.Controls.Add(lblStop);

                // Drop-off items
                if (stop.DropOff.Count > 0)
                {
                    var lblDrop = new Label { Text = "  Drop Off:", AutoSize = true, Margin = new Padding(10, 2, 3, 2), Tag = stop };
                    flpStops.Controls.Add(lblDrop);

                    for (int di = 0; di < stop.DropOff.Count; di++)
                    {
                        var item = stop.DropOff[di];
                        var chk = new CheckBox
                        {
                            Text = $"{item.ExtendedName} x{item.Quantity}",
                            Checked = item.Delivered,
                            AutoSize = true,
                            Margin = new Padding(20, 1, 3, 1),
                            Tag = new DeliveryItemTag(stop.Sequence, di, "DropOff", stop)
                        };

                        chk.CheckedChanged += DeliveryItem_CheckedChanged;
                        flpStops.Controls.Add(chk);
                    }
                }

                // Pick-up items
                if (stop.PickUp.Count > 0)
                {
                    var lblPick = new Label { Text = "  Pick Up:", AutoSize = true, Margin = new Padding(10, 2, 3, 2), Tag = stop };
                    flpStops.Controls.Add(lblPick);

                    for (int pi = 0; pi < stop.PickUp.Count; pi++)
                    {
                        var item = stop.PickUp[pi];
                        var chk = new CheckBox
                        {
                            Text = $"{item.ExtendedName} x{item.Quantity}",
                            Checked = item.Delivered,
                            AutoSize = true,
                            Margin = new Padding(20, 1, 3, 1),
                            Tag = new DeliveryItemTag(stop.Sequence, pi, "PickUp", stop)
                        };

                        chk.CheckedChanged += DeliveryItem_CheckedChanged;
                        flpStops.Controls.Add(chk);
                    }
                }

                // Refuel checklist item for Refuel or CargoAndRefuel stops
                if (isRefuelStop)
                {
                    var lblRefuel = new Label { Text = "  Refuel:", AutoSize = true, Margin = new Padding(10, 2, 3, 2), Tag = stop };
                    flpStops.Controls.Add(lblRefuel);

                    var chkRefuel = new CheckBox
                    {
                        Text = "Refuel at this stop",
                        AutoSize = true,
                        Margin = new Padding(20, 1, 3, 1),
                        Tag = stop
                    };

                    chkRefuel.CheckedChanged += RefuelItem_CheckedChanged;
                    flpStops.Controls.Add(chkRefuel);
                    _refuelCheckboxes[stop] = chkRefuel;
                }

                // Always show "Complete Stop" button — enabled only when all items delivered
                bool hasItems = stop.DropOff.Count > 0 || stop.PickUp.Count > 0 || isRefuelStop;
                bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                    && (!isRefuelStop || (_refuelCheckboxes.TryGetValue(stop, out var refChk) && refChk.Checked))
                    && hasItems;

                var btnComplete = new Button
                {
                    Text = "Complete Stop",
                    AutoSize = true,
                    Margin = new Padding(20, 3, 3, 3),
                    Tag = stop,
                    Enabled = allDelivered
                };

                btnComplete.Click += CompleteStop_Click;
                flpStops.Controls.Add(btnComplete);
                _stopCompleteButtons[stop] = btnComplete;
            }

            flpStops.ResumeLayout();
            pnlExecution.ResumeLayout();
            this.ResumeLayout();

            // Restore scroll position
            pnlExecution.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
            sw.Stop();
            Log.Info("BuildExecution PERF: total={0}ms", sw.ElapsedMilliseconds);
        }

        // -----------------------------------------------------------------------
        // Item Delivery Handling
        // -----------------------------------------------------------------------

        private void RefuelItem_CheckedChanged(object sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null) return;

            var stop = chk.Tag as DeliveryPlanStop;
            if (stop == null) return;

            // Update the Complete Stop button for the affected stop
            UpdateStopCompleteButton(stop);
        }

        private void DeliveryItem_CheckedChanged(object sender, EventArgs e)
        {
            if (_isProgrammaticUpdate > 0) return;

            var chk = sender as CheckBox;
            if (chk == null) return;

            var tag = chk.Tag as DeliveryItemTag;
            if (tag == null) return;

            if (string.IsNullOrEmpty(selectedPlanUUID)) return;

            // Mark item delivered via service (service handles all side effects:
            // commodity fulfillment, flatpack staging, worker/resource delivery, station holds)
            _deliveryPlanService.MarkItemDelivered(
                selectedPlanUUID, tag.StopSequence, tag.ItemIndex, tag.ListType, chk.Checked);

            // Update the Complete Stop button for the affected stop (show when all items checked)
            UpdateStopCompleteButton(tag.Stop);
        }

        private void UpdateStopCompleteButton(DeliveryPlanStop stop)
        {
            if (stop == null) return;

            bool isRefuelStop = _refuelCheckboxes.ContainsKey(stop);
            bool hasItems = stop.DropOff.Count > 0 || stop.PickUp.Count > 0 || isRefuelStop;
            bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                && (!isRefuelStop || (_refuelCheckboxes.TryGetValue(stop, out var refChk) && refChk.Checked))
                && hasItems;

            if (_stopCompleteButtons.TryGetValue(stop, out var btn))
            {
                btn.Enabled = allDelivered;
            }
        }

        private void CompleteStop_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var stop = btn.Tag as DeliveryPlanStop;
            if (stop == null || string.IsNullOrEmpty(selectedPlanUUID)) return;

            _deliveryPlanService.MarkStopComplete(selectedPlanUUID, stop.Sequence);
            playerContext.CascadeResourceCheckDirty = true;

            // Remove just this stop's controls from the panel instead of full rebuild
            RemoveStopControls(stop);
        }

        /// <summary>
        /// Removes all controls belonging to a specific stop from flpStops.
        /// Controls are identified by their Tag (DeliveryPlanStop reference or DeliveryItemTag.Stop).
        /// </summary>
        private void RemoveStopControls(DeliveryPlanStop stop)
        {
            flpStops.SuspendLayout();

            var toRemove = new List<Control>();

            for (int i = 0; i < flpStops.Controls.Count; i++)
            {
                var ctrl = flpStops.Controls[i];

                // Header and section labels have the stop directly as Tag
                if (ctrl.Tag is DeliveryPlanStop tagStop && tagStop == stop)
                {
                    toRemove.Add(ctrl);
                    continue;
                }

                // Delivery item checkboxes have DeliveryItemTag with Stop reference
                if (ctrl.Tag is DeliveryItemTag dit && dit.Stop == stop)
                {
                    toRemove.Add(ctrl);
                    continue;
                }
            }

            // Also remove the Complete Stop button
            if (_stopCompleteButtons.TryGetValue(stop, out var completeBtn))
            {
                toRemove.Add(completeBtn);
                completeBtn.Click -= CompleteStop_Click;
                _stopCompleteButtons.Remove(stop);
            }

            foreach (var ctrl in toRemove)
            {
                flpStops.Controls.Remove(ctrl);
                ctrl.Dispose();
            }

            // Clean up refuel checkbox reference
            _refuelCheckboxes.Remove(stop);

            flpStops.ResumeLayout();
        }

        private void CmdCompletePlan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPlanUUID)) return;

            _deliveryPlanService.MarkPlanComplete(selectedPlanUUID);
            playerContext.CascadeResourceCheckDirty = true;
            Log.Info("Delivery plan '{0}' manually marked as completed", selectedPlanUUID);

            ClearExecution();
            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
        }

        private void CmdDeletePlan_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPlanUUID)) return;

            var currentPlan = playerContext.GetCurrentPlayerReadOnlyPlans()
                .FirstOrDefault(p => p.UUID == selectedPlanUUID);
            string planName = currentPlan != null ? currentPlan.Name : selectedPlanUUID;

            var result = MessageBox.Show(
                $"Delete plan '{planName}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            _deliveryPlanService.Delete(selectedPlanUUID);
            Log.Info("Delivery plan '{0}' deleted", planName);

            ClearExecution();
            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
        }

        private void OnCurrentPlayerChanged(object sender, EventArgs e)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => OnCurrentPlayerChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

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
                try
                {
                    BeginInvoke(new Action(() => OnDeliveryDataChanged(sender, e)));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            // Refresh plan dropdown so new/deleted plans appear (BL-042)
            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);

            // If the selected plan was deleted externally, clear the view (BL-041)
            if (!string.IsNullOrEmpty(selectedPlanUUID) &&
                !playerContext.DeliveryPlanList.Any(p => p.UUID == selectedPlanUUID))
            {
                selectedPlanUUID = null;
                ClearExecution();
                cmdCompletePlan.Visible = false;
                cmdDeletePlan.Visible = false;
                return;
            }

            if (!string.IsNullOrEmpty(selectedPlanUUID))
                BuildExecution();
        }

        // -----------------------------------------------------------------------
        // Cargo Volume / Mass Display
        // -----------------------------------------------------------------------

        private void UpdateCargoDisplay()
        {
            var mutablePlan = playerContext.FindMutableDeliveryPlan(selectedPlanUUID);
            if (mutablePlan == null)
            {
                lblCargoVolume.Text = string.Empty;
                lblCargoMass.Text = string.Empty;
                cmdSplitTrips.Visible = false;
                return;
            }

            var loadItems = mutablePlan.CalculateLoadList();
            Func<string, ReadOnlyBlueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
            var cargoResult = CargoVolumeService.ComputeLoadVolume(loadItems, bpFinder);
            UpdateCargoDisplayFromResult(cargoResult);
        }

        private void UpdateCargoDisplayFromResult(CargoVolumeService.CargoLoadResult cargoResult)
        {
            if (selectedShip != null && currentCargoCapacity > 0)
            {
                decimal pct = (cargoResult.TotalVolume / currentCargoCapacity) * 100m;
                bool overCapacity = cargoResult.TotalVolume > currentCargoCapacity;

                if (overCapacity)
                {
                    lblCargoVolume.Text = string.Format(
                        "Volume: {0:N0} / {1:N0} ({2:N0}%) -- OVER CAPACITY",
                        cargoResult.TotalVolume,
                        currentCargoCapacity,
                        pct);
                    lblCargoVolume.ForeColor = Color.Red;
                }
                else
                {
                    lblCargoVolume.Text = string.Format(
                        "Volume: {0:N0} / {1:N0} ({2:N0}%)",
                        cargoResult.TotalVolume,
                        currentCargoCapacity,
                        pct);
                    lblCargoVolume.ForeColor = SystemColors.ControlText;
                }

                lblCargoMass.Text = string.Format("Mass: {0:N0}", cargoResult.TotalMass);
                cmdSplitTrips.Visible = overCapacity;
            }
            else
            {
                if (cargoResult.TotalVolume > 0 || cargoResult.TotalMass > 0)
                {
                    lblCargoVolume.Text = string.Format("Volume: {0:N0}", cargoResult.TotalVolume);
                    lblCargoMass.Text = string.Format("Mass: {0:N0}", cargoResult.TotalMass);
                }
                else
                {
                    lblCargoVolume.Text = string.Empty;
                    lblCargoMass.Text = string.Empty;
                }

                lblCargoVolume.ForeColor = SystemColors.ControlText;
                cmdSplitTrips.Visible = false;
            }
        }

        // -----------------------------------------------------------------------
        // Trip Splitting
        // -----------------------------------------------------------------------

        private void CmdSplitTrips_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPlanUUID) || currentCargoCapacity <= 0) return;

            var splitPlan = playerContext.FindMutableDeliveryPlan(selectedPlanUUID);
            if (splitPlan == null) return;

            var loadItems = splitPlan.CalculateLoadList();
            Func<string, ReadOnlyBlueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
            var trips = CargoVolumeService.SplitIntoTrips(
                loadItems, currentCargoCapacity, bpFinder);

            if (trips.Count <= 1)
            {
                MessageBox.Show(
                    "Load fits in a single trip.",
                    "Split Trips",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format(
                "Load will be split into {0} trips:", trips.Count));
            sb.AppendLine();
            for (int i = 0; i < trips.Count; i++)
            {
                var tripResult = CargoVolumeService.ComputeLoadVolume(
                    trips[i], bpFinder);
                sb.AppendLine(string.Format(
                    "Trip {0}: {1} items, Vol: {2:N0}/{3:N0}, Mass: {4:N0}",
                    i + 1,
                    trips[i].Count,
                    tripResult.TotalVolume,
                    currentCargoCapacity,
                    tripResult.TotalMass));
                foreach (var item in trips[i])
                {
                    sb.AppendLine(string.Format(
                        "  {0} x{1}", item.ExtendedName, item.Quantity));
                }

                sb.AppendLine();
            }

            sb.AppendLine("Accept? Creates additional delivery plans.");

            var result = MessageBox.Show(
                sb.ToString(),
                "Split Trips",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (result != DialogResult.OK) return;

            _deliveryPlanService.SplitTrips(selectedPlanUUID, currentCargoCapacity, bpFinder);

            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
            BuildExecution();
        }

        // -----------------------------------------------------------------------
        // Context Menu Handlers (grid-context-menus spec, task 5.2)
        // -----------------------------------------------------------------------

        private void TsmiMarkAllDelivered_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPlanUUID)) return;

            var mutablePlan = playerContext.FindMutableDeliveryPlan(selectedPlanUUID);
            if (mutablePlan == null) return;

            foreach (var stop in mutablePlan.Stops)
            {
                for (int i = 0; i < stop.DropOff.Count; i++)
                {
                    if (!stop.DropOff[i].Delivered)
                    {
                        _deliveryPlanService.MarkItemDelivered(
                            selectedPlanUUID, stop.Sequence, i, "DropOff", true);
                    }
                }

                for (int i = 0; i < stop.PickUp.Count; i++)
                {
                    if (!stop.PickUp[i].Delivered)
                    {
                        _deliveryPlanService.MarkItemDelivered(
                            selectedPlanUUID, stop.Sequence, i, "PickUp", true);
                    }
                }
            }

            BuildExecution();
        }

        private void TsmiMarkAllUndelivered_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedPlanUUID)) return;

            var mutablePlan = playerContext.FindMutableDeliveryPlan(selectedPlanUUID);
            if (mutablePlan == null) return;

            foreach (var stop in mutablePlan.Stops)
            {
                for (int i = 0; i < stop.DropOff.Count; i++)
                {
                    if (stop.DropOff[i].Delivered)
                    {
                        _deliveryPlanService.MarkItemDelivered(
                            selectedPlanUUID, stop.Sequence, i, "DropOff", false);
                    }
                }

                for (int i = 0; i < stop.PickUp.Count; i++)
                {
                    if (stop.PickUp[i].Delivered)
                    {
                        _deliveryPlanService.MarkItemDelivered(
                            selectedPlanUUID, stop.Sequence, i, "PickUp", false);
                    }
                }
            }

            BuildExecution();
        }

        private void DgvLoadList_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (e.RowIndex >= 0)
            {
                dgvLoadList.ClearSelection();
                dgvLoadList.Rows[e.RowIndex].Selected = true;
                dgvLoadList.CurrentCell = dgvLoadList.Rows[e.RowIndex].Cells[0];
            }
            else
            {
                dgvLoadList.ClearSelection();
            }
        }

        // -----------------------------------------------------------------------
        // Route / Plan Selection
        // -----------------------------------------------------------------------

        /// <summary>
        /// Tag object for delivery item checkboxes, carrying identification info for service calls.
        /// </summary>
        private class DeliveryItemTag
        {
            public DeliveryItemTag(int stopSequence, int itemIndex, string listType, DeliveryPlanStop stop)
            {
                StopSequence = stopSequence;
                ItemIndex = itemIndex;
                ListType = listType;
                Stop = stop;
            }

            public int StopSequence { get; }

            public int ItemIndex { get; }

            public string ListType { get; }

            public DeliveryPlanStop Stop { get; }
        }

        private class DropdownItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display ?? string.Empty;
        }
    }
}
