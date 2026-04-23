using System.Linq;

namespace OE2EmpireTracker.Services.Migration
{
    public static class RemapUUID
    {
        /// <summary>
        /// Walks all UUID references across the data model and replaces
        /// oldUUID with newUUID. No-op if oldUUID is not found anywhere.
        /// </summary>
        public static void Remap(
            EmpireContext ec,
            PlayerContext pc,
            string oldUUID,
            string newUUID)
        {
            // Blueprint.UUID (global + player)
            foreach (var bp in ec.GlobalBlueprintList)
                if (bp.UUID == oldUUID) bp.UUID = newUUID;
            foreach (var bp in pc.BlueprintList)
                if (bp.UUID == oldUUID) bp.UUID = newUUID;

            // Blueprint.BaseBlueprintUUID (evolution chains)
            foreach (var bp in ec.GlobalBlueprintList.Concat(pc.BlueprintList))
            {
                if (bp.BaseBlueprintUUID == oldUUID)
                {
                    bp.BaseBlueprintUUID = newUUID;
                }
            }

            // Colony.UUID
            foreach (var colony in pc.ColonyList)
            {
                if (colony.UUID == oldUUID) colony.UUID = newUUID;
            }

            // ColonyStructure references (3 fields)
            foreach (var colony in pc.ColonyList)
            {
                foreach (var s in colony.Structures)
                {
                    if (s.FlatpackBlueprintUUID == oldUUID)
                        s.FlatpackBlueprintUUID = newUUID;
                    if (s.ResearchingBlueprintUUID == oldUUID)
                        s.ResearchingBlueprintUUID = newUUID;
                    if (s.ManufacturingBlueprintUUID == oldUUID)
                        s.ManufacturingBlueprintUUID = newUUID;
                }
            }

            // RouteStop.ColonyUUID
            foreach (var route in pc.DeliveryRouteList)
            {
                foreach (var stop in route.Stops)
                {
                    if (stop.ColonyUUID == oldUUID)
                    {
                        stop.ColonyUUID = newUUID;
                }
                    }
            }

            // DeliveryPlanStop.ColonyUUID
            foreach (var plan in pc.DeliveryPlanList)
            {
                foreach (var stop in plan.Stops)
                {
                    if (stop.ColonyUUID == oldUUID)
                    {
                        stop.ColonyUUID = newUUID;
                }
                    }
            }
        }
    }
}
