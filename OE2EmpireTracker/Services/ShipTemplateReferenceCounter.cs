using NLog;
using OE2EmpireTracker.Models;
using System.Collections.Generic;
using System.Linq;

namespace OE2EmpireTracker.Services
{
    /// <summary>
    /// Counts references to a ShipTemplate from Ships and BuildItems.
    /// Used to prevent deletion of templates that are still in use.
    /// </summary>
    public class ShipTemplateReferenceCounter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, int> _shipMap;
        private readonly Dictionary<string, int> _buildItemMap;

        public ShipTemplateReferenceCounter(
            IEnumerable<Ship> ships,
            IEnumerable<BuildPlan> buildPlans)
        {
            var shipList = ships ?? Enumerable.Empty<Ship>();
            var buildPlanList = buildPlans ?? Enumerable.Empty<BuildPlan>();

            _shipMap = new Dictionary<string, int>();
            foreach (var ship in shipList)
            {
                if (!string.IsNullOrEmpty(ship.TemplateUUID))
                {
                    _shipMap.TryGetValue(ship.TemplateUUID, out int c);
                    _shipMap[ship.TemplateUUID] = c + 1;
                }
            }

            _buildItemMap = new Dictionary<string, int>();
            foreach (var plan in buildPlanList)
            {
                if (plan.Items == null) continue;
                foreach (var item in plan.Items)
                {
                    if (!string.IsNullOrEmpty(item.ShipTemplateUUID))
                    {
                        _buildItemMap.TryGetValue(item.ShipTemplateUUID, out int c);
                        _buildItemMap[item.ShipTemplateUUID] = c + 1;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of entities referencing the given ship template UUID.
        /// Counts Ship.TemplateUUID and BuildItem.ShipTemplateUUID matches.
        /// </summary>
        public int CountReferences(string templateUUID)
        {
            if (string.IsNullOrEmpty(templateUUID))
                return 0;

            _shipMap.TryGetValue(templateUUID, out int shipCount);
            _buildItemMap.TryGetValue(templateUUID, out int buildItemCount);

            return shipCount + buildItemCount;
        }
    }
}