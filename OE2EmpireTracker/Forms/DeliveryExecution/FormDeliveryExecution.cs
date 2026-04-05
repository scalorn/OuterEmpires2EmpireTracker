using NLog;
using OE2EmpireTracker.Baseline;
using OE2EmpireTracker.Controls;
using OE2EmpireTracker.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace OE2EmpireTracker.Forms.DeliveryExecution
{
    public partial class FormDeliveryExecution : Form
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private EmpireContext empireContext;
        private PlayerContext playerContext;
        private DeliveryPlan selectedPlan;
        private string preSelectRouteUUID;
        private string preSelectPlanUUID;

        public FormDeliveryExecution()
        {
            InitializeComponent();
            empireContext = EmpireContext.getInstance();
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

            PopulateRouteDropdown();

            flpBase.Layout += flpBase_Layout;
            flpSelectors.Layout += flpSelectors_Layout;

            playerContext.CurrentPlayerChanged += (s, ev) =>
            {
                PopulateRouteDropdown();
                ClearExecution();
            };
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
                return;
            }

            selectedPlan = playerContext.deliveryPlanList.FirstOrDefault(p => p.UUID == planUUID);
            if (selectedPlan != null)
            {
                BuildExecution();
            }
        }

        // -----------------------------------------------------------------------
        // Execution Display
        // -----------------------------------------------------------------------

        private void ClearExecution()
        {
            dgvLoadList.Rows.Clear();
            flpStops.Controls.Clear();
            selectedPlan = null;
        }

        private void BuildExecution()
        {
            dgvLoadList.Rows.Clear();
            flpStops.Controls.Clear();

            if (selectedPlan == null) return;

            Log.Debug("BuildExecution: plan={0}, stops={1}", selectedPlan.Name, selectedPlan.Stops.Count);

            // Build consolidated load list
            var loadItems = CalculateLoadList(selectedPlan);
            Log.Debug("BuildExecution: loadItems={0}", loadItems.Count);
            foreach (var item in loadItems)
            {
                Log.Debug("  Load: {0} x{1}", item.BaseItemTypeID, item.Quantity);
                dgvLoadList.Rows.Add(item.ItemType.ToString(), item.BaseItemTypeID, item.Quantity);
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
                            Text = $"{item.BaseItemTypeID} x{item.Quantity}",
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
                            Text = $"{item.BaseItemTypeID} x{item.Quantity}",
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
                }
            }
        }

        // -----------------------------------------------------------------------
        // Load List Calculation
        // -----------------------------------------------------------------------

        /// <summary>
        /// Calculates what needs to be loaded before departure.
        /// A drop-off item needs pre-loading if it hasn't been picked up
        /// at an earlier stop in sufficient quantity.
        /// </summary>
        private List<DeliveryItem> CalculateLoadList(DeliveryPlan plan)
        {
            var pickedUp = new Dictionary<string, int>(); // key: "Type|BaseID"
            var needed = new Dictionary<string, DeliveryItem>();

            foreach (var stop in plan.Stops.OrderBy(s => s.Sequence))
            {
                // Check drop-offs against what's been picked up so far
                foreach (var dropItem in stop.DropOff)
                {
                    string key = $"{dropItem.ItemType}|{dropItem.BaseItemTypeID}";
                    int available = 0;
                    pickedUp.TryGetValue(key, out available);

                    int shortfall = dropItem.Quantity - available;
                    if (shortfall > 0)
                    {
                        if (needed.ContainsKey(key))
                        {
                            needed[key].Quantity += shortfall;
                        }
                        else
                        {
                            needed[key] = new DeliveryItem
                            {
                                ItemType = dropItem.ItemType,
                                BaseItemTypeID = dropItem.BaseItemTypeID,
                                Name = dropItem.Name,
                                Quantity = shortfall
                            };
                        }
                        // Consume available
                        if (available > 0)
                            pickedUp[key] = 0;
                    }
                    else
                    {
                        pickedUp[key] = available - dropItem.Quantity;
                    }
                }

                // Add pick-ups to running total
                foreach (var pickItem in stop.PickUp)
                {
                    string key = $"{pickItem.ItemType}|{pickItem.BaseItemTypeID}";
                    int current = 0;
                    pickedUp.TryGetValue(key, out current);
                    pickedUp[key] = current + pickItem.Quantity;
                }
            }

            return needed.Values.OrderBy(i => i.Name).ToList();
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
            playerContext.writeContext();

            // Rebuild to show/hide "Complete Stop" buttons
            BuildExecution();

            if (selectedPlan != null && IsAllDelivered(selectedPlan))
            {
                selectedPlan.Completed = true;
                playerContext.writeContext();
                Log.Info("Delivery plan '{0}' marked as completed", selectedPlan.Name);
            }
        }

        private void CompleteStop_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var stop = btn.Tag as DeliveryPlanStop;
            if (stop == null) return;

            stop.StopCompleted = true;
            playerContext.writeContext();
            BuildExecution();

            if (selectedPlan != null && IsAllDelivered(selectedPlan))
            {
                selectedPlan.Completed = true;
                playerContext.writeContext();
                Log.Info("Delivery plan '{0}' marked as completed", selectedPlan.Name);
            }
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
    }
}
