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
        public void BeginProgrammaticUpdate() { _isProgrammaticUpdate++; }
        public void EndProgrammaticUpdate() { _isProgrammaticUpdate--; }
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private DeliveryPlan selectedPlan;
        private Ship selectedShip;
        private decimal currentCargoCapacity;
        private Dictionary<DeliveryPlanStop, Button> _stopCompleteButtons = new Dictionary<DeliveryPlanStop, Button>();
        private Dictionary<DeliveryPlanStop, CheckBox> _refuelCheckboxes = new Dictionary<DeliveryPlanStop, CheckBox>();

        public FormDeliveryExecution()
        {
            InitializeComponent();
            empireContext = EmpireContext.GetInstance();
            playerContext = EmpireContext.PlayerContext;

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
            pnlExecution.Layout += PnlExecution_Layout;

            playerContext.CurrentPlayerChanged += OnCurrentPlayerChanged;
            playerContext.DeliveryDataChanged += OnDeliveryDataChanged;
        }

        public string PreSelectRouteUUID { get; set; }
        public string PreSelectPlanUUID { get; set; }

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

        // -----------------------------------------------------------------------
        // Layout
        // -----------------------------------------------------------------------

        private void FlpBase_Layout(object sender, LayoutEventArgs e)
        {
            pnlExecution.Size = new Size(
                flpBase.Size.Width - flpSelectors.Size.Width - flpSelectors.Margin.Right - flpSelectors.Margin.Left - pnlExecution.Margin.Left - pnlExecution.Margin.Right,
                flpBase.Size.Height - pnlExecution.Margin.Top - pnlExecution.Margin.Bottom);
            flpSelectors.Size = new Size(
                flpSelectors.Size.Width,
                flpBase.Size.Height - flpSelectors.Margin.Top - flpSelectors.Margin.Bottom);
        }

        private void PnlExecution_Layout(object sender, LayoutEventArgs e)
        {
            int w = pnlExecution.ClientSize.Width - dgvLoadList.Margin.Left - dgvLoadList.Margin.Right;
            dgvLoadList.Width = w;
            flpStops.Width = w;
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

        // -----------------------------------------------------------------------
        // Route / Plan Selection
        // -----------------------------------------------------------------------

        private class DropdownItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
            public override string ToString() => Display ?? string.Empty;
        }

        private void PopulateRouteDropdown()
        {
            _lastRouteUUID = RouteDropdownHelper.Populate(cmbRoute, playerContext.GetCurrentPlayerRoutes(), txtRouteFilter.Text ?? string.Empty, cmbRoute.SelectedValue as string, CmbRoute_SelectedIndexChanged);
        }

        private string _lastRouteUUID = string.Empty;

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
            var plans = playerContext.GetCurrentPlayerPlans()
                .Where(p => p.RouteUUID == routeUUID && !p.Completed)
                .Where(p => string.IsNullOrEmpty(filter) || (p.Name ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(p => p.Name)
                .ToList();

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
            foreach (var ship in ships.OrderBy(s => s.Name))
            {
                items.Add(new DropdownItem { UUID = ship.UUID, Display = ship.Name });
            }

            cmbShip.DataSource = null;
            cmbShip.DisplayMember = "Display";
            cmbShip.ValueMember = "UUID";
            cmbShip.DataSource = items;

            // Pre-select the ship stored on the plan
            if (selectedPlan != null && !string.IsNullOrEmpty(selectedPlan.ShipUUID)
                && items.Any(i => i.UUID == selectedPlan.ShipUUID))
            {
                cmbShip.SelectedValue = selectedPlan.ShipUUID;
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
            if (selectedPlan != null)
            {
                selectedPlan.ShipUUID = shipUUID;
                playerContext.WriteContext();
                Log.Info("Ship '{0}' assigned to plan '{1}'", shipUUID, selectedPlan.Name);
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
            selectedPlan = null;
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
            foreach (var item in loadItems)
            {
                Log.Debug("  Load: {0} x{1}", item.BaseItemTypeID, item.Quantity);
                dgvLoadList.Rows.Add(item.ItemType.ToString(), item.BaseItemTypeID, item.ExtendedName, item.Quantity);
                totalQuantity += item.Quantity;
            }

            // Compute volume and mass via CargoVolumeService
            Func<string, Models.Blueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
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
                .FirstOrDefault(r => r.UUID == selectedPlan.RouteUUID);

            // Build per-stop sections
            foreach (var stop in selectedPlan.Stops.OrderBy(s => s.Sequence))
            {
                // Skip completed stops
                if (stop.StopCompleted) continue;

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

                // Refuel checklist item for Refuel or CargoAndRefuel stops
                RouteStop matchingRouteStop = null;
                if (route != null)
                    matchingRouteStop = route.Stops.FirstOrDefault(rs => rs.Sequence == stop.Sequence);
                bool isRefuelStop = matchingRouteStop != null &&
                    (matchingRouteStop.Purpose == RouteStopPurpose.Refuel || matchingRouteStop.Purpose == RouteStopPurpose.CargoAndRefuel);

                if (isRefuelStop)
                {
                    var lblRefuel = new Label { Text = "  Refuel:", AutoSize = true, Margin = new Padding(10, 2, 3, 2) };
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

                // Show "Complete Stop" button if all items at this stop are delivered
                bool hasItems = stop.DropOff.Count > 0 || stop.PickUp.Count > 0 || isRefuelStop;
                bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                    && (!isRefuelStop || (_refuelCheckboxes.TryGetValue(stop, out var refChk) && refChk.Checked))
                    && hasItems;
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

            // Station hold operations: add/remove items from station holds
            if (selectedPlan != null)
            {
                UpdateStationHold(item, chk.Checked);
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

        /// <summary>
        /// Updates station holds when delivery items are checked at station stops.
        /// Drop-offs add items to the station hold; pick-ups remove items.
        /// </summary>
        private void UpdateStationHold(DeliveryItem item, bool delivered)
        {
            var stop = selectedPlan.Stops.FirstOrDefault(s =>
                s.DropOff.Contains(item) || s.PickUp.Contains(item));
            if (stop == null || stop.DestinationType != DestinationType.Station) return;

            var station = playerContext.FindStation(stop.DestinationUUID);
            if (station == null)
            {
                Log.Warn("Station not found for stop {0} during hold update", stop.DestinationUUID);
                return;
            }

            string playerUUID = playerContext.CurrentPlayerUUID;
            if (string.IsNullOrEmpty(playerUUID)) return;

            ItemBag hold;
            if (!station.Holds.TryGetValue(playerUUID, out hold))
            {
                hold = new ItemBag();
                station.Holds[playerUUID] = hold;
            }

            bool isDropOff = stop.DropOff.Contains(item);

            if (isDropOff && delivered)
            {
                // Drop-off: add items to station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity += item.Quantity;
                }
                else
                {
                    var newItem = new Item(item.ItemType, item.BaseItemTypeID);
                    newItem.UUID = Guid.NewGuid().ToString();
                    newItem.BaseItemTypeID = item.BaseItemTypeID;
                    newItem.Name = item.Name;
                    newItem.Quantity = item.Quantity;
                    newItem.ResourcePurity = item.ResourcePurity;
                    hold.AddItem(newItem);
                }
            }
            else if (isDropOff && !delivered)
            {
                // Undo drop-off: remove items from station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity = Math.Max(0, existing[0].Quantity - item.Quantity);
                }
            }
            else if (!isDropOff && delivered)
            {
                // Pick-up: remove items from station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity = Math.Max(0, existing[0].Quantity - item.Quantity);
                }
            }
            else if (!isDropOff && !delivered)
            {
                // Undo pick-up: add items back to station hold
                var existing = hold.FindByType(item.ItemType, item.BaseItemTypeID);
                if (existing.Count > 0)
                {
                    existing[0].Quantity += item.Quantity;
                }
                else
                {
                    var newItem = new Item(item.ItemType, item.BaseItemTypeID);
                    newItem.UUID = Guid.NewGuid().ToString();
                    newItem.BaseItemTypeID = item.BaseItemTypeID;
                    newItem.Name = item.Name;
                    newItem.Quantity = item.Quantity;
                    newItem.ResourcePurity = item.ResourcePurity;
                    hold.AddItem(newItem);
                }
            }

            Log.Info(
                "Station hold updated: station={0}, player={1}, item={2}, delivered={3}, isDropOff={4}",
                station.Name,
                playerUUID,
                item.BaseItemTypeID,
                delivered,
                isDropOff);
        }

        private void UpdateStopCompleteButton(DeliveryPlanStop stop)
        {
            if (stop == null) return;

            bool isRefuelStop = _refuelCheckboxes.ContainsKey(stop);
            bool hasItems = stop.DropOff.Count > 0 || stop.PickUp.Count > 0 || isRefuelStop;
            bool allDelivered = stop.DropOff.All(i => i.Delivered) && stop.PickUp.All(i => i.Delivered)
                && (!isRefuelStop || (_refuelCheckboxes.TryGetValue(stop, out var refChk) && refChk.Checked))
                && hasItems;

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

        private void CmdCompletePlan_Click(object sender, EventArgs e)
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

        private void CmdDeletePlan_Click(object sender, EventArgs e)
        {
            if (selectedPlan == null) return;

            var result = MessageBox.Show(
                $"Delete plan '{selectedPlan.Name}'?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            playerContext.RemoveDeliveryPlan(selectedPlan);
            playerContext.WriteContext();
            playerContext.OnDeliveryDataChanged();
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
            if (selectedPlan != null &&
                !playerContext.DeliveryPlanList.Any(p => p.UUID == selectedPlan.UUID))
            {
                selectedPlan = null;
                ClearExecution();
                cmdCompletePlan.Visible = false;
                cmdDeletePlan.Visible = false;
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

        // -----------------------------------------------------------------------
        // Cargo Volume / Mass Display
        // -----------------------------------------------------------------------

        private void UpdateCargoDisplay()
        {
            if (selectedPlan == null)
            {
                lblCargoVolume.Text = string.Empty;
                lblCargoMass.Text = string.Empty;
                cmdSplitTrips.Visible = false;
                return;
            }

            var loadItems = selectedPlan.CalculateLoadList();
            Func<string, Models.Blueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
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
                        cargoResult.TotalVolume, currentCargoCapacity, pct);
                    lblCargoVolume.ForeColor = Color.Red;
                }
                else
                {
                    lblCargoVolume.Text = string.Format(
                        "Volume: {0:N0} / {1:N0} ({2:N0}%)",
                        cargoResult.TotalVolume, currentCargoCapacity, pct);
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
            if (selectedPlan == null || currentCargoCapacity <= 0) return;

            var loadItems = selectedPlan.CalculateLoadList();
            Func<string, Models.Blueprint> bpFinder = uuid => playerContext.FindBlueprint(uuid);
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
                    i + 1, trips[i].Count, tripResult.TotalVolume,
                    currentCargoCapacity, tripResult.TotalMass));
                foreach (var item in trips[i])
                    sb.AppendLine(string.Format(
                        "  {0} x{1}", item.ExtendedName, item.Quantity));
                sb.AppendLine();
            }

            sb.AppendLine("Accept? Creates additional delivery plans.");

            var result = MessageBox.Show(
                sb.ToString(),
                "Split Trips",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question);
            if (result != DialogResult.OK) return;

            CreateSplitTripPlans(trips);
        }

        private void CreateSplitTripPlans(List<List<DeliveryItem>> trips)
        {
            for (int i = 1; i < trips.Count; i++)
            {
                var newPlan = new DeliveryPlan
                {
                    UUID = Guid.NewGuid().ToString(),
                    Name = string.Format(
                        "{0} (Trip {1})",
                        selectedPlan.Name,
                        i + 1),
                    OwnerUUID = selectedPlan.OwnerUUID,
                    RouteUUID = selectedPlan.RouteUUID,
                    ShipUUID = selectedPlan.ShipUUID
                };

                foreach (var stop in selectedPlan.Stops
                    .OrderBy(s => s.Sequence))
                {
                    var newStop = new DeliveryPlanStop
                    {
                        ColonyUUID = stop.ColonyUUID,
                        Sequence = stop.Sequence,
                        DestinationType = stop.DestinationType,
                        DestinationUUID = stop.DestinationUUID
                    };

                    foreach (var dropItem in stop.DropOff)
                    {
                        var tripItem = trips[i].FirstOrDefault(t =>
                            t.ItemType == dropItem.ItemType &&
                            t.BaseItemTypeID == dropItem.BaseItemTypeID &&
                            t.ResourcePurity == dropItem.ResourcePurity);
                        if (tripItem != null && tripItem.Quantity > 0)
                        {
                            int qty = Math.Min(
                                tripItem.Quantity, dropItem.Quantity);
                            newStop.DropOff.Add(new DeliveryItem
                            {
                                ItemType = dropItem.ItemType,
                                BaseItemTypeID = dropItem.BaseItemTypeID,
                                Name = dropItem.Name,
                                ResourcePurity = dropItem.ResourcePurity,
                                Quantity = qty
                            });
                            tripItem.Quantity -= qty;
                        }
                    }

                    foreach (var pickItem in stop.PickUp)
                    {
                        newStop.PickUp.Add(new DeliveryItem
                        {
                            ItemType = pickItem.ItemType,
                            BaseItemTypeID = pickItem.BaseItemTypeID,
                            Name = pickItem.Name,
                            ResourcePurity = pickItem.ResourcePurity,
                            Quantity = pickItem.Quantity
                        });
                    }

                    if (newStop.DropOff.Count > 0 || newStop.PickUp.Count > 0)
                        newPlan.Stops.Add(newStop);
                }

                playerContext.AddDeliveryPlan(newPlan);
                Log.Info(
                    "Created split trip plan '{0}' (UUID={1})",
                    newPlan.Name,
                    newPlan.UUID);
            }

            if (!selectedPlan.Name.Contains("(Trip"))
                selectedPlan.Name = string.Format(
                    "{0} (Trip 1)", selectedPlan.Name);

            playerContext.WriteContext();
            playerContext.OnDeliveryDataChanged();

            string routeUUID = cmbRoute.SelectedValue as string;
            if (!string.IsNullOrEmpty(routeUUID))
                PopulatePlanDropdown(routeUUID);
            BuildExecution();
        }
    }
}
