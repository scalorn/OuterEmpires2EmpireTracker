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
            cmbRoute.Size = new Size(
                flpSelectors.Size.Width - cmbRoute.Margin.Left - cmbRoute.Margin.Right,
                cmbRoute.Size.Height);
            cmbPlan.Size = new Size(
                flpSelectors.Size.Width - cmbPlan.Margin.Left - cmbPlan.Margin.Right,
                cmbPlan.Size.Height);
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
            var routes = playerContext.GetCurrentPlayerRoutes();
            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = "", Display = "" });
            foreach (var route in routes.OrderBy(r => r.Name))
            {
                items.Add(new DropdownItem { UUID = route.UUID, Display = route.Name });
            }
            cmbRoute.DataSource = null;
            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.DataSource = items;
        }

        private void cmbRoute_SelectedIndexChanged(object sender, EventArgs e)
        {
            string routeUUID = cmbRoute.SelectedValue as string;
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
            var plans = playerContext.GetCurrentPlayerPlans()
                .Where(p => p.RouteUUID == routeUUID && !p.Completed)
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

            // Build consolidated load list
            var loadItems = CalculateLoadList(selectedPlan);
            foreach (var item in loadItems)
            {
                dgvLoadList.Rows.Add(item.ItemType.ToString(), item.Name, item.Quantity);
            }

            // Build per-stop sections
            foreach (var stop in selectedPlan.Stops.OrderBy(s => s.Sequence))
            {
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
                            Text = $"{item.Name} x{item.Quantity}",
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
                            Text = $"{item.Name} x{item.Quantity}",
                            Checked = item.Delivered,
                            AutoSize = true,
                            Margin = new Padding(20, 1, 3, 1),
                            Tag = item
                        };
                        chk.CheckedChanged += DeliveryItem_CheckedChanged;
                        flpStops.Controls.Add(chk);
                    }
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

            // Check if all items are delivered — mark plan as completed
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
