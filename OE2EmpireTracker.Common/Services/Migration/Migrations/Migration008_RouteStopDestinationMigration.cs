using NLog;
using OE2EmpireTracker.Models;

namespace OE2EmpireTracker.Services.Migration
{
    /// <summary>
    /// Migration008: Migrate RouteStop and DeliveryPlanStop ColonyUUID to DestinationUUID.
    ///
    /// Existing RouteStop and DeliveryPlanStop entries use ColonyUUID to reference
    /// their destination. The new DestinationUUID + DestinationType fields provide
    /// a generic destination reference that supports colonies, stations, asteroids,
    /// and ships. This migration copies ColonyUUID into DestinationUUID and sets
    /// DestinationType = Colony for any stops that have a ColonyUUID but an empty
    /// DestinationUUID.
    /// </summary>
    public static class Migration008_RouteStopDestinationMigration
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Run(EmpireContext ec, PlayerContext pc)
        {
            int migratedRouteStops = 0;
            int migratedPlanStops = 0;

            foreach (var route in pc.DeliveryRouteList)
            {
                if (route.Stops == null) continue;
                foreach (var stop in route.Stops)
                {
                    if (!string.IsNullOrEmpty(stop.ColonyUUID) &&
                        string.IsNullOrEmpty(stop.DestinationUUID))
                    {
                        stop.DestinationUUID = stop.ColonyUUID;
                        stop.DestinationType = DestinationType.Colony;
                        migratedRouteStops++;
                    }
                }
            }

            foreach (var plan in pc.DeliveryPlanList)
            {
                if (plan.Stops == null) continue;
                foreach (var stop in plan.Stops)
                {
                    if (!string.IsNullOrEmpty(stop.ColonyUUID) &&
                        string.IsNullOrEmpty(stop.DestinationUUID))
                    {
                        stop.DestinationUUID = stop.ColonyUUID;
                        stop.DestinationType = DestinationType.Colony;
                        migratedPlanStops++;
                    }
                }
            }

            Log.Info(
                "Migration008: migrated {0} route stops and {1} delivery plan stops to DestinationUUID",
                migratedRouteStops,
                migratedPlanStops);
        }
    }
}
