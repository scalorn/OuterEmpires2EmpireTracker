using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NLog;
using OE2EmpireTracker.Models;
using OE2EmpireTracker.Services;

namespace OE2EmpireTracker.Controls
{
    /// <summary>
    /// Shared helper for populating a delivery route ComboBox with filtering and selection persistence.
    /// Used by FormColonyDailyBuild and FormDeliveryExecution.
    /// </summary>
    public static class RouteDropdownHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Populates a route ComboBox from the given routes, applying an optional text filter.
        /// Preserves the previous selection if it still exists in the filtered list.
        /// </summary>
        /// <param name="cmbRoute">The ComboBox to populate.</param>
        /// <param name="routes">All available delivery routes.</param>
        /// <param name="filter">Text filter to apply to route names (empty = no filter).</param>
        /// <param name="previousUUID">The previously selected route UUID to restore.</param>
        /// <param name="selectedIndexChanged">The event handler to detach/reattach during population.</param>
        /// <returns>The UUID of the selected route after population, or empty string if none.</returns>
        public static string Populate(
            ComboBox cmbRoute,
            IList<DeliveryRoute> routes,
            string filter,
            string previousUUID,
            EventHandler selectedIndexChanged)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            cmbRoute.SelectedIndexChanged -= selectedIndexChanged;

            var items = new List<DropdownItem>();
            items.Add(new DropdownItem { UUID = string.Empty, Display = string.Empty });
            foreach (var route in CollectionSortHelper.OrderDeliveryRoutes(routes))
            {
                if (!string.IsNullOrEmpty(filter) && route.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                items.Add(new DropdownItem { UUID = route.UUID, Display = route.Name });
            }

            cmbRoute.DataSource = null;
            cmbRoute.DisplayMember = "Display";
            cmbRoute.ValueMember = "UUID";
            cmbRoute.DataSource = items;

            string resultUUID = string.Empty;
            if (!string.IsNullOrEmpty(previousUUID) && items.Any(i => i.UUID == previousUUID))
            {
                cmbRoute.SelectedValue = previousUUID;
                resultUUID = previousUUID;
            }

            cmbRoute.SelectedIndexChanged += selectedIndexChanged;
            sw.Stop();
            Log.Info("PERF PopulateRouteDropdown: {0}ms items={1}", sw.ElapsedMilliseconds, items.Count);
            return resultUUID;
        }

        public class DropdownItem
        {
            public string UUID { get; set; }
            public string Display { get; set; }
        }
    }
}
